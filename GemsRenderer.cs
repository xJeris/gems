using System.Collections.Generic;
using UnityEngine;

namespace ErenshorGems
{
    public static class GemsRenderer
    {
        private static Dictionary<GemType, Texture2D> _gemTextures;
        private static Texture2D _gridBgTexture;
        private static Texture2D _windowBgTexture;
        private static Texture2D _sidebarBgTexture;
        private static Texture2D _borderTexture;
        private static Texture2D _titleBarTexture;
        private static Texture2D _buttonNormalTexture;
        private static Texture2D _buttonHoverTexture;
        private static Texture2D _buttonActiveTexture;

        private static bool _initialized;

        // Gem dimensions match the cell: oval shape
        public const int GemWidth = 38;
        public const int GemHeight = 30;
        // Ring colors
        private static readonly Color BlueRing = new Color(0.20f, 0.35f, 0.90f);
        private static readonly Color RedRing = new Color(0.85f, 0.15f, 0.15f);

        // Fallback procedural icon drawers per gem type
        private static readonly Dictionary<GemType, IconDrawer> _fallbackIcons = new Dictionary<GemType, IconDrawer>
        {
            { GemType.BlueSword, DrawSword },
            { GemType.BlueShield, DrawShield },
            { GemType.BlueStar, DrawStarIcon },
            { GemType.BlueArrow, DrawArrow },
            { GemType.BlueCrescent, DrawCrescent },
            { GemType.BlueOrb, DrawOrb },
            { GemType.RedWhirlwind, DrawWhirlwind },
            { GemType.RedMirror, DrawMirror },
            { GemType.RedShadow, DrawShadowEye },
            { GemType.RedChaos, DrawChaos },
            { GemType.RedHaste, DrawHourglass },
            { GemType.RedVoid, DrawVoid },
        };

        public static void Initialize()
        {
            if (_initialized) return;

            // Load game icon catalog first
            GameIconCatalog.Initialize();

            _gemTextures = new Dictionary<GemType, Texture2D>();

            // Blue-ringed gems
            foreach (var type in GemTypeUtil.BlueTypes)
                _gemTextures[type] = CreateGemWithGameIcon(BlueRing, type);

            // Red-ringed gems
            foreach (var type in GemTypeUtil.RedTypes)
                _gemTextures[type] = CreateGemWithGameIcon(RedRing, type);

            // EQ-style mottled dark stone backgrounds
            _gridBgTexture = CreateMottledTexture(64, 64, new Color(0.08f, 0.09f, 0.14f), 0.03f, 7);
            _windowBgTexture = CreateMottledTexture(32, 32, new Color(0.12f, 0.13f, 0.18f), 0.025f, 5);
            _sidebarBgTexture = CreateMottledTexture(32, 32, new Color(0.10f, 0.11f, 0.16f), 0.025f, 5);
            _borderTexture = CreateBeveledBorderTexture();
            _titleBarTexture = CreateTitleBarTexture();
            _buttonNormalTexture = CreateButtonTexture(new Color(0.22f, 0.22f, 0.30f), new Color(0.35f, 0.35f, 0.45f), new Color(0.12f, 0.12f, 0.18f));
            _buttonHoverTexture = CreateButtonTexture(new Color(0.28f, 0.28f, 0.38f), new Color(0.42f, 0.42f, 0.52f), new Color(0.15f, 0.15f, 0.22f));
            _buttonActiveTexture = CreateButtonTexture(new Color(0.15f, 0.15f, 0.22f), new Color(0.12f, 0.12f, 0.18f), new Color(0.30f, 0.30f, 0.40f));

            _initialized = true;
        }

        public static Texture2D GetGemTexture(GemType type)
        {
            Initialize();
            if (type == GemType.None) return null;
            return _gemTextures.TryGetValue(type, out var tex) ? tex : null;
        }

        public static Texture2D GetGridBgTexture() { Initialize(); return _gridBgTexture; }
        public static Texture2D GetWindowBgTexture() { Initialize(); return _windowBgTexture; }
        public static Texture2D GetSidebarBgTexture() { Initialize(); return _sidebarBgTexture; }
        public static Texture2D GetBorderTexture() { Initialize(); return _borderTexture; }
        public static Texture2D GetTitleBarTexture() { Initialize(); return _titleBarTexture; }
        public static Texture2D GetButtonNormalTexture() { Initialize(); return _buttonNormalTexture; }
        public static Texture2D GetButtonHoverTexture() { Initialize(); return _buttonHoverTexture; }
        public static Texture2D GetButtonActiveTexture() { Initialize(); return _buttonActiveTexture; }

        // --- Gem creation (oval shape) ---

        private delegate void IconDrawer(Texture2D tex, float cx, float cy, float r);

        private static Texture2D CreateGem(Color ringColor, IconDrawer drawIcon)
        {
            int w = GemWidth;
            int h = GemHeight;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            float cx = w / 2f;
            float cy = h / 2f;
            // Radii for the oval
            float outerRx = w / 2f - 0.5f;
            float outerRy = h / 2f - 0.5f;
            float ringWidth = 3.5f;
            float innerRx = outerRx - ringWidth;
            float innerRy = outerRy - ringWidth;

            Color ringDark = ringColor * 0.50f; ringDark.a = 1f;
            Color ringLight = Color.Lerp(ringColor, Color.white, 0.40f); ringLight.a = 1f;
            Color bgColor = new Color(0.10f, 0.10f, 0.16f, 1f);

            // Clear
            var clearPixels = new Color[w * h];
            for (int i = 0; i < clearPixels.Length; i++) clearPixels[i] = Color.clear;
            tex.SetPixels(clearPixels);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    // Normalized elliptical distance
                    float outerDist = (dx * dx) / (outerRx * outerRx) + (dy * dy) / (outerRy * outerRy);
                    float innerDist = (dx * dx) / (innerRx * innerRx) + (dy * dy) / (innerRy * innerRy);

                    if (outerDist > 1.15f) continue;

                    Color pixel;

                    if (innerDist >= 1f)
                    {
                        // Ring with beveled 3D look
                        float angle = Mathf.Atan2(-dy, -dx);
                        float lightAngle = Mathf.Atan2(1f, -1f); // light from top-left
                        float angleDiff = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, lightAngle * Mathf.Rad2Deg)) / 180f;
                        float bevel = (1f - angleDiff) * 0.45f;
                        pixel = Color.Lerp(ringDark, ringColor, 0.5f + bevel);
                        pixel = Color.Lerp(pixel, ringLight, bevel * 0.55f);
                        pixel.a = 1f;

                        // Specular highlight on upper-left of ring
                        float ringPos = Mathf.Clamp01((Mathf.Sqrt(outerDist) - Mathf.Sqrt(innerDist)) /
                            (1f - Mathf.Sqrt(innerDist) + 0.001f));
                        float specAngle = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, 135f)) / 55f;
                        if (specAngle < 1f)
                        {
                            float specIntensity = (1f - specAngle) * (1f - Mathf.Abs(ringPos - 0.45f) * 2.2f) * 0.45f;
                            pixel = Color.Lerp(pixel, Color.white, Mathf.Max(0, specIntensity));
                        }
                    }
                    else
                    {
                        pixel = bgColor;
                    }

                    // Anti-alias outer edge
                    if (outerDist > 0.92f)
                    {
                        float edgeFade = Mathf.Clamp01((1.08f - outerDist) / 0.16f);
                        pixel.a *= edgeFade;
                    }

                    tex.SetPixel(x, y, pixel);
                }
            }

            // Draw icon inside the oval - use the smaller radius for icon scaling
            float iconR = Mathf.Min(innerRx, innerRy) - 1f;
            drawIcon(tex, cx, cy, iconR);

            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Create a gem texture using a game sprite icon if available, falling back to procedural drawing.
        /// </summary>
        private static Texture2D CreateGemWithGameIcon(Color ringColor, GemType type)
        {
            var sprite = GameIconCatalog.GetSpriteForGem(type);
            if (sprite != null)
            {
                var iconTex = GameIconCatalog.SpriteToTexture2D(sprite);
                if (iconTex != null)
                    return CreateGemWithSprite(ringColor, iconTex);
            }

            // Fallback to procedural icon
            if (_fallbackIcons.TryGetValue(type, out var drawer))
                return CreateGem(ringColor, drawer);

            return CreateGem(ringColor, DrawOrb); // ultimate fallback
        }

        /// <summary>
        /// Create a gem texture with a sprite composited into the oval interior.
        /// </summary>
        private static Texture2D CreateGemWithSprite(Color ringColor, Texture2D iconSource)
        {
            int w = GemWidth;
            int h = GemHeight;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            float cx = w / 2f;
            float cy = h / 2f;
            float outerRx = w / 2f - 0.5f;
            float outerRy = h / 2f - 0.5f;
            float ringWidth = 3.5f;
            float innerRx = outerRx - ringWidth;
            float innerRy = outerRy - ringWidth;

            Color ringDark = ringColor * 0.50f; ringDark.a = 1f;
            Color ringLight = Color.Lerp(ringColor, Color.white, 0.40f); ringLight.a = 1f;

            // Clear
            var clearPixels = new Color[w * h];
            for (int i = 0; i < clearPixels.Length; i++) clearPixels[i] = Color.clear;
            tex.SetPixels(clearPixels);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float outerDist = (dx * dx) / (outerRx * outerRx) + (dy * dy) / (outerRy * outerRy);
                    float innerDist = (dx * dx) / (innerRx * innerRx) + (dy * dy) / (innerRy * innerRy);

                    if (outerDist > 1.15f) continue;

                    Color pixel;

                    if (innerDist >= 1f)
                    {
                        // Ring with beveled 3D look (same as CreateGem)
                        float angle = Mathf.Atan2(-dy, -dx);
                        float lightAngle = Mathf.Atan2(1f, -1f);
                        float angleDiff = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, lightAngle * Mathf.Rad2Deg)) / 180f;
                        float bevel = (1f - angleDiff) * 0.45f;
                        pixel = Color.Lerp(ringDark, ringColor, 0.5f + bevel);
                        pixel = Color.Lerp(pixel, ringLight, bevel * 0.55f);
                        pixel.a = 1f;

                        float ringPos = Mathf.Clamp01((Mathf.Sqrt(outerDist) - Mathf.Sqrt(innerDist)) /
                            (1f - Mathf.Sqrt(innerDist) + 0.001f));
                        float specAngle = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, 135f)) / 55f;
                        if (specAngle < 1f)
                        {
                            float specIntensity = (1f - specAngle) * (1f - Mathf.Abs(ringPos - 0.45f) * 2.2f) * 0.45f;
                            pixel = Color.Lerp(pixel, Color.white, Mathf.Max(0, specIntensity));
                        }
                    }
                    else
                    {
                        // Interior: sample from icon texture, scaled to fit inner ellipse
                        // Map pixel position to icon UV coordinates
                        float u = (dx / innerRx + 1f) * 0.5f; // 0..1 across inner width
                        float v = (dy / innerRy + 1f) * 0.5f; // 0..1 across inner height

                        int srcX = Mathf.Clamp((int)(u * iconSource.width), 0, iconSource.width - 1);
                        int srcY = Mathf.Clamp((int)((1f - v) * iconSource.height), 0, iconSource.height - 1);
                        pixel = iconSource.GetPixel(srcX, srcY);

                        // If icon pixel is transparent, show dark background
                        if (pixel.a < 0.01f)
                        {
                            pixel = new Color(0.10f, 0.10f, 0.16f, 1f);
                        }
                        else
                        {
                            // Blend with dark background for semi-transparent pixels
                            Color bg = new Color(0.10f, 0.10f, 0.16f, 1f);
                            pixel = Color.Lerp(bg, pixel, pixel.a);
                            pixel.a = 1f;
                        }
                    }

                    // Anti-alias outer edge
                    if (outerDist > 0.92f)
                    {
                        float edgeFade = Mathf.Clamp01((1.08f - outerDist) / 0.16f);
                        pixel.a *= edgeFade;
                    }

                    tex.SetPixel(x, y, pixel);
                }
            }

            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Rebuild all gem textures (call after changing icon mappings).
        /// </summary>
        public static void RebuildGemTextures()
        {
            if (_gemTextures == null) return;

            foreach (var type in GemTypeUtil.BlueTypes)
                _gemTextures[type] = CreateGemWithGameIcon(BlueRing, type);
            foreach (var type in GemTypeUtil.RedTypes)
                _gemTextures[type] = CreateGemWithGameIcon(RedRing, type);
        }

        // --- Background texture generation ---

        private static Texture2D CreateMottledTexture(int w, int h, Color baseColor, float variation, int seed)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            var rng = new System.Random(seed);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * variation;
                    Color c = new Color(
                        Mathf.Clamp01(baseColor.r + noise),
                        Mathf.Clamp01(baseColor.g + noise * 0.9f),
                        Mathf.Clamp01(baseColor.b + noise * 1.1f),
                        1f
                    );
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateBeveledBorderTexture()
        {
            // A simple beveled border strip - lighter on top/left, darker on bottom/right
            int size = 8;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color mid = new Color(0.30f, 0.30f, 0.38f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, mid);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateTitleBarTexture()
        {
            // EQ-style title bar: dark stone with subtle gold trim line
            int w = 256;
            int h = 26;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var rng = new System.Random(99);

            Color baseColor = new Color(0.14f, 0.14f, 0.20f);
            Color goldTrim = new Color(0.55f, 0.48f, 0.30f);
            Color goldTrimDark = new Color(0.35f, 0.30f, 0.18f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.02f;
                    Color c;

                    if (y <= 1)
                    {
                        // Bottom gold trim line
                        c = y == 0 ? goldTrimDark : goldTrim;
                    }
                    else if (y >= h - 2)
                    {
                        // Top gold trim line
                        c = y == h - 1 ? goldTrimDark : goldTrim;
                    }
                    else
                    {
                        // Subtle vertical gradient - slightly lighter in middle
                        float gradT = (float)(y - 2) / (h - 4);
                        float brightness = 1f + Mathf.Sin(gradT * Mathf.PI) * 0.08f;
                        c = new Color(
                            Mathf.Clamp01(baseColor.r * brightness + noise),
                            Mathf.Clamp01(baseColor.g * brightness + noise),
                            Mathf.Clamp01(baseColor.b * brightness + noise * 1.2f),
                            1f
                        );
                    }

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateButtonTexture(Color face, Color highlight, Color shadow)
        {
            // EQ-style beveled button: light top-left edges, dark bottom-right, stone face
            int w = 82;
            int h = 24;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var rng = new System.Random(42);
            int bevel = 2;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c;
                    // Top edge
                    if (y >= h - bevel)
                        c = highlight;
                    // Bottom edge
                    else if (y < bevel)
                        c = shadow;
                    // Left edge
                    else if (x < bevel)
                        c = highlight;
                    // Right edge
                    else if (x >= w - bevel)
                        c = shadow;
                    else
                    {
                        // Face with subtle noise
                        float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.015f;
                        c = new Color(
                            Mathf.Clamp01(face.r + noise),
                            Mathf.Clamp01(face.g + noise),
                            Mathf.Clamp01(face.b + noise * 1.1f),
                            1f
                        );
                    }
                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        // --- Blue gem icons ---

        private static void DrawSword(Texture2D tex, float cx, float cy, float r)
        {
            Color color = new Color(0.6f, 0.8f, 1.0f);
            DrawLineOnTexture(tex, cx, cy - r * 0.8f, cx, cy + r * 0.5f, 1.5f, color);
            DrawLineOnTexture(tex, cx - r * 0.4f, cy - r * 0.1f, cx + r * 0.4f, cy - r * 0.1f, 1.5f, color);
            DrawLineOnTexture(tex, cx, cy + r * 0.5f, cx, cy + r * 0.7f, 2f, color);
        }

        private static void DrawShield(Texture2D tex, float cx, float cy, float r)
        {
            Color color = new Color(0.5f, 0.75f, 1.0f);
            int w = tex.width;
            int h = tex.height;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - cx) / r;
                    float dy = (y - cy) / r;
                    float width = 0.65f * (1f - Mathf.Max(0, dy) * 0.7f);
                    if (dy < -0.7f) width *= (0.7f + dy) / -0.0001f;
                    width = Mathf.Max(0, width);
                    if (dy > -0.7f && dy < 0.8f && Mathf.Abs(dx) < width)
                    {
                        float alpha = Mathf.Clamp01((width - Mathf.Abs(dx)) * 4f) * 0.8f;
                        BlendPixel(tex, x, y, color, alpha);
                    }
                }
            }
        }

        private static void DrawStarIcon(Texture2D tex, float cx, float cy, float r)
        {
            Color color = new Color(0.6f, 0.85f, 1.0f);
            DrawStar(tex, cx, cy, r * 0.7f, 5, color);
        }

        private static void DrawArrow(Texture2D tex, float cx, float cy, float r)
        {
            Color color = new Color(0.5f, 0.8f, 1.0f);
            DrawLineOnTexture(tex, cx, cy - r * 0.7f, cx, cy + r * 0.6f, 1.3f, color);
            DrawLineOnTexture(tex, cx, cy - r * 0.7f, cx - r * 0.35f, cy - r * 0.2f, 1.3f, color);
            DrawLineOnTexture(tex, cx, cy - r * 0.7f, cx + r * 0.35f, cy - r * 0.2f, 1.3f, color);
        }

        private static void DrawCrescent(Texture2D tex, float cx, float cy, float r)
        {
            Color color = new Color(0.6f, 0.8f, 1.0f);
            int w = tex.width;
            int h = tex.height;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist1 = Mathf.Sqrt(dx * dx + dy * dy);
                    float dist2 = Mathf.Sqrt((dx + r * 0.35f) * (dx + r * 0.35f) + dy * dy);

                    if (dist1 < r * 0.75f && dist2 > r * 0.65f)
                    {
                        float alpha = Mathf.Clamp01((r * 0.75f - dist1) * 3f) * 0.85f;
                        alpha *= Mathf.Clamp01((dist2 - r * 0.65f) * 3f);
                        BlendPixel(tex, x, y, color, alpha);
                    }
                }
            }
        }

        private static void DrawOrb(Texture2D tex, float cx, float cy, float r)
        {
            Color color = new Color(0.5f, 0.7f, 1.0f);
            int w = tex.width;
            int h = tex.height;
            float orbR = r * 0.55f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist < orbR)
                    {
                        float nd = dist / orbR;
                        float alpha = (1f - nd * nd) * 0.85f;
                        Color c = Color.Lerp(Color.white, color, nd);
                        BlendPixel(tex, x, y, c, alpha);
                    }
                }
            }
        }

        // --- Red gem icons ---

        private static void DrawWhirlwind(Texture2D tex, float cx, float cy, float r)
        {
            Color color = new Color(1.0f, 0.6f, 0.5f);
            float lineW = 1.3f;
            float prevX = cx, prevY = cy;
            int steps = 25;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                float angle = t * Mathf.PI * 3f;
                float spiralR = t * r * 0.8f;
                float nx = cx + Mathf.Cos(angle) * spiralR;
                float ny = cy + Mathf.Sin(angle) * spiralR;
                DrawLineOnTexture(tex, prevX, prevY, nx, ny, lineW, color);
                prevX = nx; prevY = ny;
            }
        }

        private static void DrawMirror(Texture2D tex, float cx, float cy, float r)
        {
            Color color = new Color(1.0f, 0.55f, 0.55f);
            DrawLineOnTexture(tex, cx - r * 0.6f, cy - r * 0.25f, cx + r * 0.2f, cy - r * 0.25f, 1.3f, color);
            DrawLineOnTexture(tex, cx + r * 0.2f, cy - r * 0.25f, cx - r * 0.1f, cy - r * 0.55f, 1.3f, color);
            DrawLineOnTexture(tex, cx + r * 0.2f, cy - r * 0.25f, cx - r * 0.1f, cy + r * 0.05f, 1.3f, color);
            DrawLineOnTexture(tex, cx + r * 0.6f, cy + r * 0.25f, cx - r * 0.2f, cy + r * 0.25f, 1.3f, color);
            DrawLineOnTexture(tex, cx - r * 0.2f, cy + r * 0.25f, cx + r * 0.1f, cy - r * 0.05f, 1.3f, color);
            DrawLineOnTexture(tex, cx - r * 0.2f, cy + r * 0.25f, cx + r * 0.1f, cy + r * 0.55f, 1.3f, color);
        }

        private static void DrawShadowEye(Texture2D tex, float cx, float cy, float r)
        {
            Color color = new Color(1.0f, 0.5f, 0.5f);
            int w = tex.width;
            int h = tex.height;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - cx) / (r * 0.75f);
                    float dy = (y - cy) / (r * 0.45f);
                    float eyeDist = dx * dx + dy * dy;

                    if (eyeDist < 1f && eyeDist > 0.6f)
                    {
                        float alpha = Mathf.Clamp01((1f - eyeDist) * 5f) * Mathf.Clamp01((eyeDist - 0.6f) * 5f) * 0.85f;
                        BlendPixel(tex, x, y, color, alpha);
                    }

                    float pupilDist = dx * dx * 4f + dy * dy * 4f;
                    if (pupilDist < 0.4f)
                    {
                        float alpha = Mathf.Clamp01((0.4f - pupilDist) * 5f) * 0.9f;
                        BlendPixel(tex, x, y, color, alpha);
                    }
                }
            }
        }

        private static void DrawChaos(Texture2D tex, float cx, float cy, float r)
        {
            Color color = new Color(1.0f, 0.55f, 0.45f);
            var rng = new System.Random(42);
            for (int i = 0; i < 8; i++)
            {
                float px = cx + (float)(rng.NextDouble() - 0.5) * r * 1.4f;
                float py = cy + (float)(rng.NextDouble() - 0.5) * r * 1.4f;
                float dotR = 1.2f + (float)rng.NextDouble() * 0.8f;
                DrawDot(tex, px, py, dotR, color);
            }
        }

        private static void DrawHourglass(Texture2D tex, float cx, float cy, float r)
        {
            Color color = new Color(1.0f, 0.6f, 0.45f);
            DrawLineOnTexture(tex, cx - r * 0.45f, cy - r * 0.7f, cx + r * 0.45f, cy - r * 0.7f, 1.3f, color);
            DrawLineOnTexture(tex, cx - r * 0.45f, cy - r * 0.7f, cx, cy, 1.3f, color);
            DrawLineOnTexture(tex, cx + r * 0.45f, cy - r * 0.7f, cx, cy, 1.3f, color);
            DrawLineOnTexture(tex, cx - r * 0.45f, cy + r * 0.7f, cx + r * 0.45f, cy + r * 0.7f, 1.3f, color);
            DrawLineOnTexture(tex, cx - r * 0.45f, cy + r * 0.7f, cx, cy, 1.3f, color);
            DrawLineOnTexture(tex, cx + r * 0.45f, cy + r * 0.7f, cx, cy, 1.3f, color);
        }

        private static void DrawVoid(Texture2D tex, float cx, float cy, float r)
        {
            Color color = new Color(1.0f, 0.45f, 0.45f);
            DrawLineOnTexture(tex, cx - r * 0.55f, cy - r * 0.55f, cx + r * 0.55f, cy + r * 0.55f, 1.8f, color);
            DrawLineOnTexture(tex, cx + r * 0.55f, cy - r * 0.55f, cx - r * 0.55f, cy + r * 0.55f, 1.8f, color);
        }

        // --- Drawing helpers ---

        private static void DrawStar(Texture2D tex, float cx, float cy, float r, int points, Color color)
        {
            int w = tex.width;
            int h = tex.height;
            float innerR = r * 0.35f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > r + 1f) continue;

                    float angle = Mathf.Atan2(dy, dx);
                    float segAngle = Mathf.PI * 2f / points;
                    float localAngle = ((angle % segAngle) + segAngle) % segAngle;
                    if (localAngle > segAngle / 2f) localAngle = segAngle - localAngle;

                    float t = localAngle / (segAngle / 2f);
                    float pointR = Mathf.Lerp(r, innerR, t);

                    if (dist < pointR)
                    {
                        float alpha = Mathf.Clamp01((pointR - dist) * 2.5f) * 0.85f;
                        BlendPixel(tex, x, y, color, alpha);
                    }
                }
            }
        }

        private static void DrawDot(Texture2D tex, float cx, float cy, float r, Color color)
        {
            int minX = Mathf.Max(0, (int)(cx - r - 1));
            int maxX = Mathf.Min(tex.width - 1, (int)(cx + r + 1));
            int minY = Mathf.Max(0, (int)(cy - r - 1));
            int maxY = Mathf.Min(tex.height - 1, (int)(cy + r + 1));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist < r + 0.5f)
                    {
                        float alpha = Mathf.Clamp01(r + 0.5f - dist) * 0.85f;
                        BlendPixel(tex, x, y, color, alpha);
                    }
                }
            }
        }

        private static void BlendPixel(Texture2D tex, int x, int y, Color color, float alpha)
        {
            if (x < 0 || x >= tex.width || y < 0 || y >= tex.height) return;
            Color existing = tex.GetPixel(x, y);
            Color blended = Color.Lerp(existing, color, alpha);
            blended.a = Mathf.Max(existing.a, alpha);
            tex.SetPixel(x, y, blended);
        }

        private static void DrawLineOnTexture(Texture2D tex, float x0, float y0, float x1, float y1, float width, Color color)
        {
            int w = tex.width;
            int h = tex.height;
            float halfW = width / 2f;

            int minX = Mathf.Max(0, (int)(Mathf.Min(x0, x1) - halfW - 1));
            int maxX = Mathf.Min(w - 1, (int)(Mathf.Max(x0, x1) + halfW + 1));
            int minY = Mathf.Max(0, (int)(Mathf.Min(y0, y1) - halfW - 1));
            int maxY = Mathf.Min(h - 1, (int)(Mathf.Max(y0, y1) + halfW + 1));

            float ldx = x1 - x0;
            float ldy = y1 - y0;
            float segLen = Mathf.Sqrt(ldx * ldx + ldy * ldy);
            if (segLen < 0.001f) return;

            for (int py = minY; py <= maxY; py++)
            {
                for (int px = minX; px <= maxX; px++)
                {
                    float pdx = px - x0;
                    float pdy = py - y0;

                    float t = (pdx * ldx + pdy * ldy) / (segLen * segLen);
                    t = Mathf.Clamp01(t);

                    float closestX = x0 + t * ldx;
                    float closestY = y0 + t * ldy;
                    float distToLine = Mathf.Sqrt((px - closestX) * (px - closestX) + (py - closestY) * (py - closestY));

                    if (distToLine < halfW + 0.5f)
                    {
                        float alpha = Mathf.Clamp01(halfW + 0.5f - distToLine) * 0.9f;
                        BlendPixel(tex, px, py, color, alpha);
                    }
                }
            }
        }

        private static Texture2D CreateSolidTexture(Color color)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
    }
}
