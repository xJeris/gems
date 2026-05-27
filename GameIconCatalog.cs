using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ErenshorGems
{
    /// <summary>
    /// Loads all game icon sprites (Spells, Skills, Items) into a catalog
    /// and provides a configurable mapping from GemType to specific icons.
    /// </summary>
    public static class GameIconCatalog
    {
        private static bool _initialized;
        private static readonly Dictionary<string, Sprite> _allSprites = new Dictionary<string, Sprite>();

        // GemType -> catalog key mapping (user-configurable)
        private static readonly Dictionary<GemType, string> _gemIconMapping = new Dictionary<GemType, string>
        {
            // Blue-ringed gems
            { GemType.BlueSword,     "Skill:Cleave" },
            { GemType.BlueShield,    "Spell:DUEL - SLOW Ocean's Lull" },
            { GemType.BlueStar,      "Spell:DRU ARC STC - Magic Bolt" },
            { GemType.BlueArrow,     "Spell:STC - Arcstorm" },
            { GemType.BlueCrescent,  "Spell:DRU PAL ARC DUEL - Antidote" },
            { GemType.BlueOrb,       "Skill:Double Attack" },

            // Red-ringed gems
            { GemType.RedWhirlwind,  "Spell:DUEL - Vithean Breeze" },
            { GemType.RedMirror,     "Spell:ARC - Dazzle" },
            { GemType.RedShadow,     "Spell:PAL - TAUNT Lunar Madness" },
            { GemType.RedChaos,      "Spell:ARC - Brax's Fury" },
            { GemType.RedHaste,      "Spell:REAV - Affinity for Suffering" },
            { GemType.RedVoid,       "Spell:REAV - Wasting" },
        };

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            LoadSpells();
            LoadSkills();
            LoadItems();
        }

        private static void LoadSpells()
        {
            var spells = Resources.LoadAll<Spell>("Spells");
            if (spells == null) return;

            foreach (var spell in spells)
            {
                if (spell == null || spell.SpellIcon == null) continue;
                string key = "Spell:" + spell.name;
                if (!_allSprites.ContainsKey(key))
                    _allSprites[key] = spell.SpellIcon;
            }
        }

        private static void LoadSkills()
        {
            var skills = Resources.LoadAll<Skill>("Skills");
            if (skills == null) return;

            foreach (var skill in skills)
            {
                if (skill == null || skill.SkillIcon == null) continue;
                string key = "Skill:" + skill.name;
                if (!_allSprites.ContainsKey(key))
                    _allSprites[key] = skill.SkillIcon;
            }
        }

        private static void LoadItems()
        {
            var items = Resources.LoadAll<Item>("Items");
            if (items == null) return;

            foreach (var item in items)
            {
                if (item == null || item.ItemIcon == null) continue;
                string key = "Item:" + item.name;
                if (!_allSprites.ContainsKey(key))
                    _allSprites[key] = item.ItemIcon;
            }
        }

        /// <summary>
        /// Get the sprite mapped to a specific GemType, or null if not mapped/found.
        /// </summary>
        public static Sprite GetSpriteForGem(GemType type)
        {
            if (!_gemIconMapping.TryGetValue(type, out var key) || string.IsNullOrEmpty(key))
                return null;
            _allSprites.TryGetValue(key, out var sprite);
            return sprite;
        }

        /// <summary>
        /// Get a sprite by its catalog key (e.g. "Spell:Fireball").
        /// </summary>
        public static Sprite GetSprite(string key)
        {
            _allSprites.TryGetValue(key, out var sprite);
            return sprite;
        }

        /// <summary>
        /// Get all catalog keys for browsing.
        /// </summary>
        public static IEnumerable<string> GetAllKeys() => _allSprites.Keys.OrderBy(k => k);

        /// <summary>
        /// Update the icon mapping for a gem type at runtime.
        /// </summary>
        public static void SetGemIcon(GemType type, string catalogKey)
        {
            _gemIconMapping[type] = catalogKey;
        }

        /// <summary>
        /// Get the current mapping for a gem type.
        /// </summary>
        public static string GetGemIconKey(GemType type)
        {
            return _gemIconMapping.TryGetValue(type, out var key) ? key : "";
        }

        /// <summary>
        /// Convert a Sprite to a Texture2D by reading its texture region.
        /// Uses RenderTexture blit as fallback if texture is not readable.
        /// </summary>
        public static Texture2D SpriteToTexture2D(Sprite sprite)
        {
            if (sprite == null) return null;

            var rect = sprite.textureRect;
            int x = (int)rect.x;
            int y = (int)rect.y;
            int w = (int)rect.width;
            int h = (int)rect.height;

            if (w <= 0 || h <= 0) return null;

            // Try direct pixel read first (works if texture is readable)
            try
            {
                var pixels = sprite.texture.GetPixels(x, y, w, h);
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                tex.SetPixels(pixels);
                tex.Apply();
                return tex;
            }
            catch
            {
                // Texture not readable — use RenderTexture blit fallback
            }

            return SpriteToTexture2DViaBlit(sprite);
        }

        private static Texture2D SpriteToTexture2DViaBlit(Sprite sprite)
        {
            var srcTex = sprite.texture;
            var rect = sprite.textureRect;

            var rt = RenderTexture.GetTemporary(srcTex.width, srcTex.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(srcTex, rt);

            var prev = RenderTexture.active;
            RenderTexture.active = rt;

            int x = (int)rect.x;
            int y = (int)rect.y;
            int w = (int)rect.width;
            int h = (int)rect.height;

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.ReadPixels(new Rect(x, y, w, h), 0, 0);
            tex.Apply();

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            return tex;
        }
    }
}
