using UnityEngine;

namespace ErenshorGems
{
    public class GemsWindow
    {
        // Window dimensions
        private const int WindowWidth = 486;
        private const int WindowHeight = 450;
        private const int TitleBarHeight = 26;
        private const int BorderSize = 3;
        private const int ContentTop = TitleBarHeight + BorderSize;
        private const int GameViewWidth = 380;
        private const int GameViewHeight = 394;
        private const int CellWidth = 38;
        private const int CellHeight = 30;
        private const int GemOffsetX = 0;
        private const int GemOffsetY = 0;
        private const int SidebarX = BorderSize + GameViewWidth + 5;

        private const int WindowId = 98234;

        public bool IsVisible { get; private set; }

        private Rect _windowRect;
        private GemsBoard _board;
        private GemPiece _currentPiece;
        private GemType _nextType;

        private enum GameState
        {
            Instructions,
            Playing,
            Paused,
            GameOver
        }

        private GameState _state = GameState.Instructions;

        // Timing
        private float _dropAccumulator;
        private bool _fastDrop;

        // Input repeat
        private float _lastMoveTime;
        private bool _moveHeld;
        private const float MoveInitialDelay = 0.38f;
        private const float MoveRepeatDelay = 0.18f;

        // Styles
        private GUIStyle _titleStyle;
        private GUIStyle _titleBarStyle;
        private GUIStyle _scoreStyle;
        private GUIStyle _pausedStyle;
        private GUIStyle _gameOverStyle;
        private GUIStyle _waveAnnouncementStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _instructionStyle;
        private GUIStyle _specialLabelStyle;
        private bool _stylesInitialized;

        // Track if we were blocking input
        private bool _wasBlockingInput;

        // Wave announcement
        private float _waveAnnouncementTime;
        private int _announcedWave;
        private const float WaveAnnouncementDuration = 2f;

        // Blue orb cascade score bonus tracking
        private float _bonusScoreEndTime;
        private const float BonusScoreDuration = 20f;

        private bool _needsPositioning = true;

        public GemsWindow()
        {
            // Defer positioning to first Draw() call when Screen dimensions are valid
            _windowRect = new Rect(0, 0, WindowWidth, WindowHeight);

            _board = new GemsBoard();
            _nextType = _board.RandomGem();
        }

        private void ClampToScreen()
        {
            float x = Mathf.Clamp(_windowRect.x, 0, Mathf.Max(0, Screen.width - WindowWidth));
            float y = Mathf.Clamp(_windowRect.y, 0, Mathf.Max(0, Screen.height - WindowHeight));
            _windowRect = new Rect(x, y, WindowWidth, WindowHeight);
        }

        public void Toggle()
        {
            if (IsVisible)
            {
                if (_state == GameState.Playing)
                    _state = GameState.Paused;
                IsVisible = false;
                ReleaseInput();
            }
            else
            {
                IsVisible = true;
                CaptureInput();
            }
        }

        public void Draw()
        {
            if (!IsVisible) return;

            // Position from config on first draw (Screen dimensions are valid here)
            if (_needsPositioning)
            {
                float x = Plugin.WindowX.Value;
                float y = Plugin.WindowY.Value;

                if (x < 0 || y < 0)
                {
                    x = (Screen.width - WindowWidth) / 2f;
                    y = (Screen.height - WindowHeight) / 2f;
                }

                _windowRect = new Rect(x, y, WindowWidth, WindowHeight);
                _needsPositioning = false;
            }

            InitStyles();
            GemsRenderer.Initialize();

            _windowRect = GUI.Window(WindowId, _windowRect, DrawWindow, "");

            // Keep window on screen after dragging or resolution changes
            ClampToScreen();

            // Save position if it changed
            if (_windowRect.x != Plugin.WindowX.Value || _windowRect.y != Plugin.WindowY.Value)
            {
                Plugin.WindowX.Value = _windowRect.x;
                Plugin.WindowY.Value = _windowRect.y;
            }

            if (_windowRect.Contains(Event.current.mousePosition))
            {
                GameData.DraggingUIElement = true;
            }
        }

        public void GameTick()
        {
            if (!IsVisible) return;

            // Check timed effect expiry
            var fx = _board.Effects;

            // Red speed boost expiry
            if (fx.SpeedBoosted && Time.time >= fx.SpeedBoostEndTime)
            {
                fx.SpeedBoosted = false;
            }

            // Blue speed slow expiry
            if (fx.SpeedSlowed && Time.time >= fx.SpeedSlowEndTime)
            {
                fx.SpeedSlowed = false;
            }

            // Blue orb score bonus expiry
            if (fx.BonusScoreMultiplier > 1f && Time.time >= _bonusScoreEndTime)
            {
                fx.BonusScoreMultiplier = 1f;
            }

            if (_state != GameState.Playing) return;

            CaptureInput();
            HandleInput();
            UpdateFalling();
        }

        private void HandleInput()
        {
            if (_currentPiece == null) return;

            _fastDrop = Input.GetKey(KeyCode.DownArrow);

            bool reversed = _board.Effects.ControlsReversed;

            // Determine effective left/right based on reversed controls
            KeyCode leftKey = reversed ? KeyCode.RightArrow : KeyCode.LeftArrow;
            KeyCode rightKey = reversed ? KeyCode.LeftArrow : KeyCode.RightArrow;

            bool leftDown = Input.GetKeyDown(leftKey);
            bool rightDown = Input.GetKeyDown(rightKey);
            bool leftHeld = Input.GetKey(leftKey);
            bool rightHeld = Input.GetKey(rightKey);

            // On fresh key press, move immediately and start initial delay
            if (leftDown)
            {
                if (_currentPiece.CanMoveTo(_board, _currentPiece.Column - 1))
                    _currentPiece.MoveLeft();
                _lastMoveTime = Time.time;
                _moveHeld = false;
            }
            else if (rightDown)
            {
                if (_currentPiece.CanMoveTo(_board, _currentPiece.Column + 1))
                    _currentPiece.MoveRight();
                _lastMoveTime = Time.time;
                _moveHeld = false;
            }
            else if (leftHeld || rightHeld)
            {
                // After initial delay, switch to repeat rate
                float delay = _moveHeld ? MoveRepeatDelay : MoveInitialDelay;
                if (Time.time - _lastMoveTime >= delay)
                {
                    _moveHeld = true;
                    if (leftHeld && _currentPiece.CanMoveTo(_board, _currentPiece.Column - 1))
                        _currentPiece.MoveLeft();
                    else if (rightHeld && _currentPiece.CanMoveTo(_board, _currentPiece.Column + 1))
                        _currentPiece.MoveRight();
                    _lastMoveTime = Time.time;
                }
            }
            else
            {
                _moveHeld = false;
            }
        }

        private void UpdateFalling()
        {
            if (_currentPiece == null)
            {
                SpawnPiece();
                return;
            }

            float interval = _board.GetDropInterval();
            if (_fastDrop)
                interval *= 0.1f;

            _dropAccumulator += Time.deltaTime;
            if (_dropAccumulator < interval) return;
            _dropAccumulator = 0;

            int nextRow = _currentPiece.Row + 1;

            if (nextRow >= GemsBoard.Rows || _board.IsCellOccupied(_currentPiece.Column, nextRow))
            {
                LandPiece();
            }
            else
            {
                _currentPiece.FractionalRow = nextRow;
            }
        }

        private void SpawnPiece()
        {
            GemType type = _nextType;
            _nextType = _board.RandomGem();
            int startCol = GemsBoard.Columns / 2;

            if (_board.IsCellOccupied(startCol, 0))
            {
                _state = GameState.GameOver;
                ReleaseInput();
                return;
            }

            _currentPiece = new GemPiece(type, startCol);
            _dropAccumulator = 0;
        }

        private void LandPiece()
        {
            if (_currentPiece == null) return;

            int landRow = _currentPiece.Row;
            _board.PlaceGem(_currentPiece.Column, landRow, _currentPiece.Type);
            _currentPiece = null;

            int cleared = _board.ProcessMatchCycle();

            // Track blue orb bonus timing
            if (_board.Effects.BonusScoreMultiplier > 1f && _bonusScoreEndTime < Time.time)
            {
                _bonusScoreEndTime = Time.time + BonusScoreDuration;
            }

            // Check wave announcement
            if (_board.Wave > _announcedWave)
            {
                _announcedWave = _board.Wave;
                _waveAnnouncementTime = Time.time;
            }

            if (_board.IsGameOver())
            {
                _state = GameState.GameOver;
                ReleaseInput();
            }
        }

        private void StartGame()
        {
            _board.Reset();
            _currentPiece = null;
            _nextType = _board.RandomGem();
            _state = GameState.Instructions;
            _dropAccumulator = 0;
            _announcedWave = 1;
            _waveAnnouncementTime = 0;
            _bonusScoreEndTime = 0;
            CaptureInput();
        }

        private void CaptureInput()
        {
            if (!_wasBlockingInput)
            {
                GameData.PlayerTyping = true;
                _wasBlockingInput = true;
            }
        }

        private void ReleaseInput()
        {
            if (_wasBlockingInput)
            {
                GameData.PlayerTyping = false;
                GameData.DraggingUIElement = false;
                _wasBlockingInput = false;
            }
        }

        // ------- Drawing -------

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
            };
            _titleStyle.normal.textColor = Color.white;

            _titleBarStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _titleBarStyle.normal.textColor = new Color(0.85f, 0.78f, 0.55f);

            _scoreStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.UpperLeft,
            };
            _scoreStyle.normal.textColor = Color.white;

            _pausedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _pausedStyle.normal.textColor = new Color(1f, 0.78f, 0f);

            _gameOverStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _gameOverStyle.normal.textColor = new Color(0.2f, 0.2f, 1f);

            _waveAnnouncementStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _waveAnnouncementStyle.normal.textColor = new Color(1f, 0f, 0f);

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
            };
            _buttonStyle.normal.background = GemsRenderer.GetButtonNormalTexture();
            _buttonStyle.normal.textColor = new Color(0.85f, 0.82f, 0.72f);
            _buttonStyle.hover.background = GemsRenderer.GetButtonHoverTexture();
            _buttonStyle.hover.textColor = new Color(0.95f, 0.92f, 0.82f);
            _buttonStyle.active.background = GemsRenderer.GetButtonActiveTexture();
            _buttonStyle.active.textColor = new Color(0.75f, 0.72f, 0.62f);

            _instructionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
            };
            _instructionStyle.normal.textColor = Color.white;

            _specialLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _specialLabelStyle.normal.textColor = Color.white;

            _stylesInitialized = true;
        }

        private void DrawWindow(int id)
        {
            // Border
            GUI.DrawTexture(new Rect(0, 0, WindowWidth, WindowHeight), GemsRenderer.GetBorderTexture());

            // Title bar background
            GUI.DrawTexture(new Rect(BorderSize, BorderSize, WindowWidth - BorderSize * 2, TitleBarHeight),
                GemsRenderer.GetTitleBarTexture());

            // Title text
            GUI.Label(new Rect(BorderSize, BorderSize, WindowWidth - BorderSize * 2, TitleBarHeight),
                "Erenshor Gems", _titleBarStyle);

            // Window background (content area)
            GUI.DrawTexture(new Rect(BorderSize, ContentTop, WindowWidth - BorderSize * 2, WindowHeight - ContentTop - BorderSize),
                GemsRenderer.GetWindowBgTexture());

            switch (_state)
            {
                case GameState.Instructions:
                    DrawInstructions();
                    break;
                case GameState.Playing:
                    DrawGameView();
                    DrawSidebar();
                    break;
                case GameState.Paused:
                    DrawGameView();
                    DrawSidebar();
                    DrawPausedOverlay();
                    break;
                case GameState.GameOver:
                    DrawGameView();
                    DrawSidebar();
                    DrawGameOverOverlay();
                    break;
            }

            DrawButtons();

            // Handle pause click on game view area
            if (_state == GameState.Playing || _state == GameState.Paused)
            {
                var gameViewRect = new Rect(BorderSize, ContentTop, GameViewWidth, GameViewHeight);
                if (Event.current.type == EventType.MouseDown && gameViewRect.Contains(Event.current.mousePosition))
                {
                    if (_state == GameState.Playing)
                    {
                        _state = GameState.Paused;
                        ReleaseInput();
                    }
                    else if (_state == GameState.Paused)
                    {
                        _state = GameState.Playing;
                        CaptureInput();
                    }
                    Event.current.Use();
                }
            }

            // Drag from title bar
            GUI.DragWindow(new Rect(0, 0, WindowWidth, ContentTop));
        }

        private void DrawInstructions()
        {
            float top = ContentTop + 15;
            GUI.Label(new Rect(50, top, 400, 20), "Welcome to Erenshor Gems", _titleStyle);

            string instructions = "Match 3 or more gems in a horizontal, vertical, or diagonal line " +
                "to score points and remove those matching gems from play. The more gems matched at " +
                "once, the higher your score.\n\n" +
                "Match 4 or more blue gems to activate good special play modes. " +
                "Any blue match of 3 or more cancels active red effects.\n\n" +
                "Matching 3 or more red gems activates bad special play modes. " +
                "Red matches also cancel active blue effects.\n\n" +
                "The game ends when a column reaches the top of the playfield.";

            GUI.Label(new Rect(50, top + 30, 400, 280), instructions, _instructionStyle);
            GUI.Label(new Rect(50, top + 270, 400, 20), "Click the Start button to play.", _instructionStyle);
        }

        private void DrawGameView()
        {
            GUI.DrawTexture(new Rect(BorderSize, ContentTop, GameViewWidth, GameViewHeight), GemsRenderer.GetGridBgTexture());

            for (int c = 0; c < GemsBoard.Columns; c++)
            {
                for (int r = 0; r < GemsBoard.Rows; r++)
                {
                    var type = _board.Grid[c, r];
                    if (type != GemType.None)
                    {
                        float x = BorderSize + c * CellWidth;
                        float y = ContentTop + r * CellHeight;
                        var tex = GemsRenderer.GetGemTexture(type);
                        if (tex != null)
                        {
                            GUI.DrawTexture(new Rect(x + GemOffsetX, y + GemOffsetY,
                                GemsRenderer.GemWidth, GemsRenderer.GemHeight), tex);
                        }
                    }
                }
            }

            // Draw current falling piece
            if (_currentPiece != null && _state == GameState.Playing)
            {
                float px = BorderSize + _currentPiece.Column * CellWidth;
                float py = ContentTop + _currentPiece.Row * CellHeight;

                var tex = GemsRenderer.GetGemTexture(_currentPiece.Type);
                if (tex != null)
                {
                    GUI.DrawTexture(new Rect(px + GemOffsetX, py + GemOffsetY,
                        GemsRenderer.GemWidth, GemsRenderer.GemHeight), tex);
                }
            }

            // Wave announcement overlay
            if (Time.time - _waveAnnouncementTime < WaveAnnouncementDuration && _announcedWave > 1)
            {
                GUI.Label(new Rect(100, ContentTop + 190, 200, 40), "Wave: " + _announcedWave, _waveAnnouncementStyle);
            }
        }

        private void DrawSidebar()
        {
            GUI.DrawTexture(new Rect(SidebarX - 3, ContentTop, WindowWidth - SidebarX - BorderSize + 3, GameViewHeight),
                GemsRenderer.GetSidebarBgTexture());

            float sideY = ContentTop + 5;

            // Score
            GUI.Label(new Rect(SidebarX, sideY, 90, 20), "Score:", _scoreStyle);
            GUI.Label(new Rect(SidebarX, sideY + 17, 90, 20), _board.Score.ToString(), _scoreStyle);

            // Wave
            GUI.Label(new Rect(SidebarX, sideY + 42, 50, 20), "Wave:", _scoreStyle);
            GUI.Label(new Rect(SidebarX + 45, sideY + 42, 45, 20), _board.Wave.ToString(), _scoreStyle);

            // NEXT preview (hidden if RedShadow effect is active)
            GUI.Label(new Rect(SidebarX + 20, sideY + 72, 60, 14), "NEXT", _specialLabelStyle);
            if (_board.Effects.NextHidden)
            {
                GUI.Label(new Rect(SidebarX + 22, sideY + 90, 36, 36), "???", _specialLabelStyle);
            }
            else
            {
                var nextTex = GemsRenderer.GetGemTexture(_nextType);
                if (nextTex != null)
                {
                    GUI.DrawTexture(new Rect(SidebarX + 22, sideY + 90,
                        GemsRenderer.GemWidth, GemsRenderer.GemHeight), nextTex);
                }
            }

            // SPECIAL indicator - show active effects
            GUI.Label(new Rect(SidebarX + 10, sideY + 145, 80, 14), "SPECIAL", _specialLabelStyle);

            float specialY = sideY + 163;
            var fx = _board.Effects;

            // Red (negative) effects
            if (fx.ControlsReversed)
            {
                DrawEffectLabel(ref specialY, "REVERSED", new Color(1f, 0.3f, 0.3f));
            }
            if (fx.NextHidden)
            {
                DrawEffectLabel(ref specialY, "HIDDEN", new Color(1f, 0.3f, 0.3f));
            }
            if (fx.SpeedBoosted)
            {
                float remaining = fx.SpeedBoostEndTime - Time.time;
                DrawEffectLabel(ref specialY, "FAST " + Mathf.CeilToInt(remaining) + "s", new Color(1f, 0.3f, 0.3f));
            }

            // Blue (positive) effects
            if (fx.SpeedSlowed)
            {
                float remaining = fx.SpeedSlowEndTime - Time.time;
                DrawEffectLabel(ref specialY, "SLOW " + Mathf.CeilToInt(remaining) + "s", new Color(0.3f, 0.7f, 1f));
            }
            if (fx.BonusScoreMultiplier > 1f)
            {
                float remaining = _bonusScoreEndTime - Time.time;
                DrawEffectLabel(ref specialY, "SCORE x2 " + Mathf.CeilToInt(remaining) + "s", new Color(0.3f, 0.7f, 1f));
            }

            // Combo display
            if (_board.ComboCount > 1)
            {
                var comboStyle = new GUIStyle(_specialLabelStyle);
                comboStyle.normal.textColor = new Color(1f, 1f, 0f);
                GUI.Label(new Rect(SidebarX + 5, specialY + 10, 80, 14),
                    "COMBO x" + _board.ComboCount, comboStyle);
            }
        }

        private void DrawEffectLabel(ref float y, string text, Color color)
        {
            var style = new GUIStyle(_specialLabelStyle);
            style.normal.textColor = color;
            style.fontSize = 10;
            GUI.Label(new Rect(SidebarX + 2, y, 90, 14), text, style);
            y += 16;
        }

        private void DrawPausedOverlay()
        {
            GUI.Label(new Rect(100, ContentTop + 10, 200, 40), "PAUSED", _pausedStyle);
        }

        private void DrawGameOverOverlay()
        {
            GUI.Label(new Rect(80, ContentTop + 10, 240, 40), "GAME OVER", _gameOverStyle);
        }

        private void DrawButtons()
        {
            float btnY = ContentTop + GameViewHeight - 90;
            if (GUI.Button(new Rect(SidebarX, btnY, 82, 24), "Start", _buttonStyle))
            {
                if (_state == GameState.Instructions)
                    _state = GameState.Playing;
                else if (_state == GameState.Paused)
                    _state = GameState.Playing;
            }

            if (_state != GameState.Instructions)
            {
                if (GUI.Button(new Rect(SidebarX, btnY + 30, 82, 24), "Reset", _buttonStyle))
                {
                    StartGame();
                }
            }

            if (GUI.Button(new Rect(SidebarX, btnY + 60, 82, 24), "Done", _buttonStyle))
            {
                if (_state == GameState.Playing)
                    _state = GameState.Paused;
                IsVisible = false;
                ReleaseInput();
            }
        }
    }
}
