using System;
using System.Collections.Generic;

namespace ErenshorGems
{
    /// <summary>
    /// Gem types: 6 blue-ringed + 6 red-ringed = 12 total.
    /// Matching is by GemType (icon). Ring color determines special triggers.
    /// Blue and red gems have completely different icons (zero overlap).
    /// </summary>
    public enum GemType
    {
        None = 0,

        // Blue-ringed gems (3+ match cancels red effects; 4+ match triggers positive effect)
        BlueSword,      // sword/cleave - clears an entire row
        BlueShield,     // shield - temporarily slows drop speed
        BlueStar,       // star/nova - clears all gems adjacent to the match
        BlueArrow,      // arrow - clears an entire column
        BlueCrescent,   // crescent/purify - removes all red gems from the field
        BlueOrb,        // orb/cascade - bonus score multiplier on next match

        // Red-ringed gems (bad specials on 3+ match - each icon depicts its effect)
        RedWhirlwind,   // whirlwind - scrambles placed gems
        RedMirror,      // mirror/reverse arrows - reverses controls
        RedShadow,      // shadow/eye - hides next gem preview
        RedChaos,       // chaos/scatter - adds random gems to field
        RedHaste,       // hourglass/speed - speeds up gameplay
        RedVoid,        // void/crack - removes random blue gems from field
    }

    public struct MatchResult
    {
        public List<(int col, int row)> Positions;
        public GemType Type;
        public int Length;
    }

    public static class GemTypeUtil
    {
        public static bool IsBlue(GemType type)
        {
            return type >= GemType.BlueSword && type <= GemType.BlueOrb;
        }

        public static bool IsRed(GemType type)
        {
            return type >= GemType.RedWhirlwind && type <= GemType.RedVoid;
        }

        public const int BlueCount = 6;
        public const int RedCount = 6;
        public const int TotalCount = 12;

        // Indices for random selection
        public static readonly GemType[] BlueTypes = {
            GemType.BlueSword, GemType.BlueShield, GemType.BlueStar,
            GemType.BlueArrow, GemType.BlueCrescent, GemType.BlueOrb
        };

        public static readonly GemType[] RedTypes = {
            GemType.RedWhirlwind, GemType.RedMirror, GemType.RedShadow,
            GemType.RedChaos, GemType.RedHaste, GemType.RedVoid
        };
    }

    /// <summary>
    /// Tracks which red special effects are currently active.
    /// Blue matches (3+ or 4+) cancel all active red effects.
    /// </summary>
    public class ActiveEffects
    {
        // Red (negative) effects
        public bool ControlsReversed { get; set; }
        public bool NextHidden { get; set; }
        public bool SpeedBoosted { get; set; }
        public float SpeedBoostEndTime { get; set; }

        // Blue (positive) effects
        public bool SpeedSlowed { get; set; }
        public float SpeedSlowEndTime { get; set; }
        public float BonusScoreMultiplier { get; set; } = 1f;

        public bool AnyRedActive =>
            ControlsReversed || NextHidden || SpeedBoosted;

        public bool AnyBlueActive =>
            SpeedSlowed || BonusScoreMultiplier > 1f;

        public void ClearRedEffects()
        {
            ControlsReversed = false;
            NextHidden = false;
            SpeedBoosted = false;
            SpeedBoostEndTime = 0f;
        }

        public void ClearAll()
        {
            ClearRedEffects();
            SpeedSlowed = false;
            SpeedSlowEndTime = 0f;
            BonusScoreMultiplier = 1f;
        }
    }

    public class GemsBoard
    {
        public const int Columns = 10;
        public const int Rows = 13;

        public GemType[,] Grid { get; private set; }

        private Random _rng = new Random();

        public int Score { get; set; }
        public int Wave { get; private set; }
        public int GemsCleared { get; private set; }
        public int ComboCount { get; private set; }

        // Wave progression
        public const int GemsPerWave = 50;
        public const float BaseDropInterval = 0.8f;
        public const float SpeedMultiplierPerWave = 0.88f;
        public const float MinDropInterval = 0.15f;

        // Active special effects
        public ActiveEffects Effects { get; private set; } = new ActiveEffects();

        // Pending specials detected during match processing
        public bool HasPendingBlueCancel { get; private set; }   // any blue 3+ cancels red effects
        public List<GemType> PendingBlueSpecials { get; private set; } = new List<GemType>(); // blue 4+ triggers positive
        public List<GemType> PendingRedSpecials { get; private set; } = new List<GemType>();

        // Red gem spawn probability (rest are blue)
        private const float RedGemChance = 0.35f;

        public GemsBoard()
        {
            Grid = new GemType[Columns, Rows];
            Reset();
        }

        public void Reset()
        {
            for (int c = 0; c < Columns; c++)
                for (int r = 0; r < Rows; r++)
                    Grid[c, r] = GemType.None;

            Score = 0;
            Wave = 1;
            GemsCleared = 0;
            ComboCount = 0;
            HasPendingBlueCancel = false;
            PendingBlueSpecials.Clear();
            PendingRedSpecials.Clear();
            Effects.ClearAll();
        }

        public float GetDropInterval()
        {
            float interval = BaseDropInterval * (float)Math.Pow(SpeedMultiplierPerWave, Wave - 1);
            if (Effects.SpeedBoosted)
                interval *= 0.5f; // red: faster
            if (Effects.SpeedSlowed)
                interval *= 2.0f; // blue: slower
            return Math.Max(interval, MinDropInterval);
        }

        /// <summary>
        /// Generate a random gem. ~65% blue, ~35% red.
        /// </summary>
        public GemType RandomGem()
        {
            if (_rng.NextDouble() < RedGemChance)
                return GemTypeUtil.RedTypes[_rng.Next(GemTypeUtil.RedCount)];
            else
                return GemTypeUtil.BlueTypes[_rng.Next(GemTypeUtil.BlueCount)];
        }

        public bool IsGameOver()
        {
            for (int c = 0; c < Columns; c++)
            {
                if (Grid[c, 0] != GemType.None)
                    return true;
            }
            return false;
        }

        public void PlaceGem(int col, int row, GemType type)
        {
            if (col >= 0 && col < Columns && row >= 0 && row < Rows)
                Grid[col, row] = type;
        }

        public bool IsCellOccupied(int col, int row)
        {
            if (col < 0 || col >= Columns || row < 0 || row >= Rows)
                return true;
            return Grid[col, row] != GemType.None;
        }

        /// <summary>
        /// Find all matches of 3+ in horizontal, vertical, and diagonal lines.
        /// Matching is by GemType (icon), not by ring color.
        /// </summary>
        public List<MatchResult> FindMatches()
        {
            var matches = new List<MatchResult>();

            // Horizontal
            for (int r = 0; r < Rows; r++)
            {
                int runStart = 0;
                for (int c = 1; c <= Columns; c++)
                {
                    if (c < Columns && Grid[c, r] != GemType.None && Grid[c, r] == Grid[runStart, r])
                        continue;

                    int runLen = c - runStart;
                    if (runLen >= 3 && Grid[runStart, r] != GemType.None)
                    {
                        var positions = new List<(int, int)>();
                        for (int k = runStart; k < c; k++)
                            positions.Add((k, r));
                        matches.Add(new MatchResult { Positions = positions, Type = Grid[runStart, r], Length = runLen });
                    }
                    runStart = c;
                }
            }

            // Vertical
            for (int c = 0; c < Columns; c++)
            {
                int runStart = 0;
                for (int r = 1; r <= Rows; r++)
                {
                    if (r < Rows && Grid[c, r] != GemType.None && Grid[c, r] == Grid[c, runStart])
                        continue;

                    int runLen = r - runStart;
                    if (runLen >= 3 && Grid[c, runStart] != GemType.None)
                    {
                        var positions = new List<(int, int)>();
                        for (int k = runStart; k < r; k++)
                            positions.Add((c, k));
                        matches.Add(new MatchResult { Positions = positions, Type = Grid[c, runStart], Length = runLen });
                    }
                    runStart = r;
                }
            }

            // Diagonal (top-left to bottom-right)
            FindDiagonalMatches(matches, 1, 1);
            // Diagonal (top-right to bottom-left)
            FindDiagonalMatches(matches, -1, 1);

            return matches;
        }

        private void FindDiagonalMatches(List<MatchResult> matches, int dc, int dr)
        {
            for (int startC = 0; startC < Columns; startC++)
            {
                for (int startR = 0; startR < Rows; startR++)
                {
                    if (Grid[startC, startR] == GemType.None)
                        continue;

                    int prevC = startC - dc;
                    int prevR = startR - dr;
                    if (prevC >= 0 && prevC < Columns && prevR >= 0 && prevR < Rows &&
                        Grid[prevC, prevR] == Grid[startC, startR])
                        continue;

                    var positions = new List<(int, int)>();
                    int c = startC, r = startR;
                    GemType type = Grid[startC, startR];
                    while (c >= 0 && c < Columns && r >= 0 && r < Rows && Grid[c, r] == type)
                    {
                        positions.Add((c, r));
                        c += dc;
                        r += dr;
                    }

                    if (positions.Count >= 3)
                    {
                        matches.Add(new MatchResult { Positions = positions, Type = type, Length = positions.Count });
                    }
                }
            }
        }

        /// <summary>
        /// Clear matched gems, update score, detect specials.
        /// Returns number of gems cleared.
        /// </summary>
        public int ClearMatches(List<MatchResult> matches)
        {
            if (matches.Count == 0) return 0;

            HasPendingBlueCancel = false;
            PendingBlueSpecials.Clear();
            PendingRedSpecials.Clear();

            var clearedPositions = new HashSet<(int, int)>();

            foreach (var match in matches)
            {
                foreach (var pos in match.Positions)
                    clearedPositions.Add(pos);

                if (GemTypeUtil.IsBlue(match.Type))
                {
                    // Blue 3+ cancels active red effects
                    if (match.Length >= 3)
                        HasPendingBlueCancel = true;
                    // Blue 4+ also triggers a positive special
                    if (match.Length >= 4)
                        PendingBlueSpecials.Add(match.Type);
                }

                // Red gem match of 3+ triggers that specific red effect
                if (GemTypeUtil.IsRed(match.Type) && match.Length >= 3)
                    PendingRedSpecials.Add(match.Type);

                // Score: base 100 per gem, with size bonus
                float multiplier = 1f;
                if (match.Length == 4) multiplier = 1.5f;
                else if (match.Length == 5) multiplier = 2f;
                else if (match.Length >= 6) multiplier = 3f;

                int matchScore = (int)(match.Length * 100 * multiplier);

                // Combo bonus
                if (ComboCount > 1)
                    matchScore = (int)(matchScore * ComboCount);

                // Blue orb cascade bonus
                matchScore = (int)(matchScore * Effects.BonusScoreMultiplier);

                Score += matchScore;
            }

            foreach (var (c, r) in clearedPositions)
                Grid[c, r] = GemType.None;

            int cleared = clearedPositions.Count;
            GemsCleared += cleared;

            // Check wave advancement
            int newWave = (GemsCleared / GemsPerWave) + 1;
            if (newWave > Wave)
                Wave = newWave;

            return cleared;
        }

        public bool ApplyGravity()
        {
            bool moved = false;

            for (int c = 0; c < Columns; c++)
            {
                int writeRow = Rows - 1;
                for (int r = Rows - 1; r >= 0; r--)
                {
                    if (Grid[c, r] != GemType.None)
                    {
                        if (r != writeRow)
                        {
                            Grid[c, writeRow] = Grid[c, r];
                            Grid[c, r] = GemType.None;
                            moved = true;
                        }
                        writeRow--;
                    }
                }
            }

            return moved;
        }

        /// <summary>
        /// Process a full match-gravity-chain cycle after a piece lands.
        /// Returns total gems cleared across all chain steps.
        /// </summary>
        public int ProcessMatchCycle()
        {
            int totalCleared = 0;
            ComboCount = 0;
            HasPendingBlueCancel = false;
            PendingBlueSpecials.Clear();
            PendingRedSpecials.Clear();

            // Track specials across the whole chain
            bool anyBlueCancel = false;
            var allBlueSpecials = new List<GemType>();
            var allRedSpecials = new List<GemType>();

            while (true)
            {
                var matches = FindMatches();
                if (matches.Count == 0)
                    break;

                ComboCount++;
                int cleared = ClearMatches(matches);
                totalCleared += cleared;

                if (HasPendingBlueCancel)
                    anyBlueCancel = true;
                allBlueSpecials.AddRange(PendingBlueSpecials);
                allRedSpecials.AddRange(PendingRedSpecials);

                ApplyGravity();
            }

            // Blue 3+ match cancels ALL active red effects (including newly triggered ones).
            if (anyBlueCancel)
            {
                Effects.ClearRedEffects();
                allRedSpecials.Clear();
            }

            // Apply red specials (if not cancelled)
            foreach (var redType in allRedSpecials)
            {
                ApplyRedSpecial(redType);
            }

            // Apply blue 4+ positive specials
            foreach (var blueType in allBlueSpecials)
            {
                ApplyBlueSpecial(blueType);
            }

            // Store final state for UI
            HasPendingBlueCancel = anyBlueCancel;

            return totalCleared;
        }

        // --- Blue Special Effects (4+ match) ---
        // Each blue gem type triggers its own positive effect.
        // Blue effects expire after a set duration OR when a red match overwrites them.

        private void ApplyBlueSpecial(GemType blueType)
        {
            switch (blueType)
            {
                case GemType.BlueSword:
                    // Cleave: clear an entire row (the lowest non-empty row)
                    for (int r = Rows - 1; r >= 0; r--)
                    {
                        bool hasGem = false;
                        for (int c = 0; c < Columns; c++)
                            if (Grid[c, r] != GemType.None) { hasGem = true; break; }
                        if (hasGem)
                        {
                            for (int c = 0; c < Columns; c++)
                                Grid[c, r] = GemType.None;
                            ApplyGravity();
                            break;
                        }
                    }
                    break;

                case GemType.BlueShield:
                    // Shield: temporarily slow drop speed (15 seconds)
                    Effects.SpeedSlowed = true;
                    Effects.SpeedSlowEndTime = UnityEngine.Time.time + 15f;
                    break;

                case GemType.BlueStar:
                    // Nova: clear all gems in a 3x3 area around a random occupied cell
                    ClearRandomArea(3);
                    break;

                case GemType.BlueArrow:
                    // Precision: clear an entire column (the most populated one)
                    ClearMostPopulatedColumn();
                    break;

                case GemType.BlueCrescent:
                    // Purify: remove all red gems from the field
                    RemoveAllRedGems();
                    break;

                case GemType.BlueOrb:
                    // Cascade: 2x score multiplier for 20 seconds
                    Effects.BonusScoreMultiplier = 2f;
                    // Duration handled via SpeedSlowEndTime reuse - but let's use a dedicated approach
                    // We'll expire this in GameTick by checking time
                    break;
            }
        }

        private void ClearRandomArea(int radius)
        {
            // Find a random occupied cell
            var occupied = new List<(int, int)>();
            for (int c = 0; c < Columns; c++)
                for (int r = 0; r < Rows; r++)
                    if (Grid[c, r] != GemType.None)
                        occupied.Add((c, r));

            if (occupied.Count == 0) return;

            var (centerC, centerR) = occupied[_rng.Next(occupied.Count)];

            int halfR = radius / 2;
            for (int c = centerC - halfR; c <= centerC + halfR; c++)
                for (int r = centerR - halfR; r <= centerR + halfR; r++)
                    if (c >= 0 && c < Columns && r >= 0 && r < Rows)
                        Grid[c, r] = GemType.None;

            ApplyGravity();
        }

        private void ClearMostPopulatedColumn()
        {
            int bestCol = 0, bestCount = 0;
            for (int c = 0; c < Columns; c++)
            {
                int count = 0;
                for (int r = 0; r < Rows; r++)
                    if (Grid[c, r] != GemType.None) count++;
                if (count > bestCount) { bestCount = count; bestCol = c; }
            }

            for (int r = 0; r < Rows; r++)
                Grid[bestCol, r] = GemType.None;

            ApplyGravity();
        }

        private void RemoveAllRedGems()
        {
            bool removed = false;
            for (int c = 0; c < Columns; c++)
                for (int r = 0; r < Rows; r++)
                    if (GemTypeUtil.IsRed(Grid[c, r]))
                    {
                        Grid[c, r] = GemType.None;
                        removed = true;
                    }
            if (removed)
                ApplyGravity();
        }

        // --- Red Special Effects ---
        // Each red gem type triggers its own negative effect (depicted by its icon).
        // Red effects also cancel any active blue effects.

        private void ApplyRedSpecial(GemType redType)
        {
            // Red effects cancel active blue effects
            Effects.SpeedSlowed = false;
            Effects.BonusScoreMultiplier = 1f;

            switch (redType)
            {
                case GemType.RedWhirlwind:
                    // Scramble all placed gems' positions
                    ScrambleBoard();
                    break;

                case GemType.RedMirror:
                    // Reverse left/right controls
                    Effects.ControlsReversed = true;
                    break;

                case GemType.RedShadow:
                    // Hide NEXT gem preview
                    Effects.NextHidden = true;
                    break;

                case GemType.RedChaos:
                    // Add random gems to empty spots on the field
                    AddRandomGems(5);
                    break;

                case GemType.RedHaste:
                    // Speed up gameplay temporarily
                    Effects.SpeedBoosted = true;
                    Effects.SpeedBoostEndTime = UnityEngine.Time.time + 12f;
                    break;

                case GemType.RedVoid:
                    // Remove random blue gems from the field
                    RemoveRandomBlueGems(4);
                    break;
            }
        }

        private void ScrambleBoard()
        {
            // Collect all non-empty gem types
            var gems = new List<GemType>();
            for (int c = 0; c < Columns; c++)
                for (int r = 0; r < Rows; r++)
                    if (Grid[c, r] != GemType.None)
                        gems.Add(Grid[c, r]);

            // Fisher-Yates shuffle
            for (int i = gems.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                var tmp = gems[i];
                gems[i] = gems[j];
                gems[j] = tmp;
            }

            // Place back in original occupied positions
            int idx = 0;
            for (int c = 0; c < Columns; c++)
            {
                for (int r = 0; r < Rows; r++)
                {
                    if (Grid[c, r] != GemType.None)
                    {
                        Grid[c, r] = gems[idx++];
                    }
                }
            }
        }

        private void AddRandomGems(int count)
        {
            // Find empty cells in the bottom half of the board
            var emptyCells = new List<(int, int)>();
            for (int c = 0; c < Columns; c++)
                for (int r = Rows / 2; r < Rows; r++)
                    if (Grid[c, r] == GemType.None)
                        emptyCells.Add((c, r));

            int toAdd = Math.Min(count, emptyCells.Count);
            for (int i = 0; i < toAdd; i++)
            {
                int idx = _rng.Next(emptyCells.Count);
                var (c, r) = emptyCells[idx];
                Grid[c, r] = RandomGem();
                emptyCells.RemoveAt(idx);
            }
        }

        private void RemoveRandomBlueGems(int count)
        {
            var bluePositions = new List<(int, int)>();
            for (int c = 0; c < Columns; c++)
                for (int r = 0; r < Rows; r++)
                    if (GemTypeUtil.IsBlue(Grid[c, r]))
                        bluePositions.Add((c, r));

            int toRemove = Math.Min(count, bluePositions.Count);
            for (int i = 0; i < toRemove; i++)
            {
                int idx = _rng.Next(bluePositions.Count);
                var (c, r) = bluePositions[idx];
                Grid[c, r] = GemType.None;
                bluePositions.RemoveAt(idx);
            }

            if (toRemove > 0)
                ApplyGravity();
        }
    }
}
