using FishingIdle.Game.Visual;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// Drawing pieces of the top menu (M24-T43, A-169): the dark strip with its wooden trim, the five big coloured tabs,
    /// the small hanging plaque, the round shortcuts on the right and their red badges. Presentation only.
    /// </summary>
    /// <remarks>
    /// ASSET_PENDENTE (docs/ASSETS_PENDENTES.md, "Menu de cima"): tab icons ico_aba_*.png, shortcut icons ico_atalho_*.png and
    /// the round frame UI/ui_atalho_moldura.png. Until each file exists, the game's current icons and the drawing below are used.
    /// </remarks>
    internal static class TopMenuArt
    {
        public const string ShortcutFrameArt = "UI/ui_atalho_moldura";

        // Wood of the Píer (the plaque and the trim of the strip).
        private static readonly Color WoodLight = new Color32(0x9B, 0x67, 0x3D, 0xFF);
        private static readonly Color WoodDark = new Color32(0x7A, 0x4E, 0x2D, 0xFF);
        private static readonly Color WoodEdge = new Color32(0x4B, 0x30, 0x1A, 0xFF);
        private static readonly Color Rope = new Color32(0xC9, 0xA8, 0x6A, 0xFF);

        // The tab colours (one per group, like the cards of the Lago de Bolso): top and bottom of the gradient.
        private static readonly Color[] TabTop =
        {
            new Color32(0x2F, 0xB7, 0xB3, 0xFF), new Color32(0x2F, 0x7F, 0xD0, 0xFF), new Color32(0x8F, 0x55, 0xD6, 0xFF),
            new Color32(0xD0, 0x8A, 0x2A, 0xFF), new Color32(0x4D, 0x66, 0x80, 0xFF),
        };

        private static readonly Color[] TabBottom =
        {
            new Color32(0x16, 0x70, 0x6E, 0xFF), new Color32(0x1A, 0x43, 0x77, 0xFF), new Color32(0x4A, 0x2A, 0x7D, 0xFF),
            new Color32(0x7A, 0x4A, 0x14, 0xFF), new Color32(0x26, 0x37, 0x4B, 0xFF),
        };

        public enum Round { Blue, Pass, Gold }

        private static Texture2D[] _tabFaces;
        private static Texture2D _strip, _roundBlue, _roundPass, _roundGold;
        private static GUIStyle _tabLabel, _roundLabel, _tinyGold;

        private static GUIStyle TabLabel(UiSkin skin) => _tabLabel ?? (_tabLabel = new GUIStyle(skin.BodyBold) { alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Clip, normal = { textColor = Color.white } });

        public static GUIStyle RoundLabel(UiSkin skin) => _roundLabel ?? (_roundLabel = new GUIStyle(skin.SmallBold) { alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Overflow, normal = { textColor = Color.white } });

        private static GUIStyle TinyGold(UiSkin skin) => _tinyGold ?? (_tinyGold = new GUIStyle(skin.SmallGoldCenter) { clipping = TextClipping.Overflow });

        /// <summary>Width a tab needs to show its label on one line.</summary>
        public static float TabLabelWidth(UiSkin skin, string label) => TabLabel(skin).CalcSize(new GUIContent(label)).x;

        /// <summary>The dark strip across the top, with a wooden trim along its bottom edge and a soft shade under it.</summary>
        public static void Strip(UiSkin skin, Rect bar)
        {
            if (_strip == null)
            {
                _strip = Gradient(new Color(0.055f, 0.125f, 0.22f, 0.96f), new Color(0.07f, 0.165f, 0.28f, 0.93f));
            }

            GUI.DrawTexture(bar, _strip, ScaleMode.StretchToFill, true);
            GUI.DrawTexture(new Rect(bar.x, bar.yMax + 6f, bar.width, 10f), skin.White, ScaleMode.StretchToFill, true, 0, new Color(0f, 0f, 0f, 0.22f), 0, 0);
            GUI.DrawTexture(new Rect(bar.x, bar.yMax - 2f, bar.width, 2f), skin.White, ScaleMode.StretchToFill, true, 0, WoodEdge, 0, 0);
            var trim = new Rect(bar.x, bar.yMax, bar.width, 6f);
            GUI.DrawTexture(trim, skin.White, ScaleMode.StretchToFill, true, 0, WoodLight, 0, 0);
            for (var x = trim.x + 64f; x < trim.xMax; x += 66f)
            {
                GUI.DrawTexture(new Rect(x, trim.y, 2f, trim.height), skin.White, ScaleMode.StretchToFill, true, 0, WoodDark, 0, 0);
            }

            GUI.DrawTexture(new Rect(trim.x, trim.yMax - 1f, trim.width, 1f), skin.White, ScaleMode.StretchToFill, true, 0, WoodEdge, 0, 0);
        }

        /// <summary>A big coloured tab: icon over the label (or the icon alone). Returns true when clicked.</summary>
        public static bool Tab(UiSkin skin, Rect rect, int group, string icon, string label, bool active)
        {
            EnsureTabFaces();
            var hover = GUI.enabled && rect.Contains(Event.current.mousePosition);
            var r = rect;
            if (active)
            {
                // The active tab grows a little and gets a white border.
                const float grow = 0.07f;
                r = new Rect(rect.x - rect.width * grow / 2f, rect.y - rect.height * grow / 2f, rect.width * (1f + grow), rect.height * (1f + grow));
                skin.DrawGlow(r, Color.white, 0.25f);
            }

            var previous = GUI.color;
            var alpha = active || hover ? 1f : 0.82f;
            if (!GUI.enabled)
            {
                alpha *= 0.6f;
            }

            GUI.color = previous * new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(new Rect(r.x, r.y + 4f, r.width, r.height), skin.White, ScaleMode.StretchToFill, true, 0, new Color(0f, 0f, 0f, 0.35f), 0, 12);
            GUI.DrawTexture(r, _tabFaces[Mathf.Clamp(group, 0, _tabFaces.Length - 1)], ScaleMode.StretchToFill, true, 0, Color.white, 0, 12);
            GUI.DrawTexture(r, skin.White, ScaleMode.StretchToFill, true, 0, active ? Color.white : new Color(1f, 1f, 1f, 0.22f), active ? 2.5f : 2f, 12);
            if (string.IsNullOrEmpty(label))
            {
                skin.DrawIcon(new Rect(r.center.x - 19f, r.center.y - 19f, 38f, 38f), icon, Color.white);
            }
            else
            {
                skin.DrawIcon(new Rect(r.center.x - 16f, r.y + 6f, 32f, 32f), icon, Color.white);
                var labelRect = new Rect(r.x + 4f, r.y + 38f, r.width - 8f, r.height - 42f);
                UiSkin.ShadowLabel(labelRect, label, TabLabel(skin));
            }

            GUI.color = previous;
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        /// <summary>A small rounded sub-tab (the windows of a group). Returns true when clicked.</summary>
        public static bool SubTab(UiSkin skin, Rect rect, string label, bool active)
        {
            return GUI.Button(rect, label, active ? skin.ChipActive : skin.Chip);
        }

        /// <summary>The small wooden plaque hanging from the strip by two ropes.</summary>
        public static void Plaque(UiSkin skin, Rect rect, float ropeTop)
        {
            foreach (var x in new[] { rect.x + rect.width * 0.16f, rect.xMax - rect.width * 0.16f })
            {
                GUI.DrawTexture(new Rect(x - 2f, ropeTop, 4f, rect.y - ropeTop + 6f), skin.White, ScaleMode.StretchToFill, true, 0, Rope, 0, 0);
            }

            GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 6f, rect.width, rect.height), skin.White, ScaleMode.StretchToFill, true, 0, new Color(0f, 0f, 0f, 0.35f), 0, 10);
            GUI.DrawTexture(rect, skin.White, ScaleMode.StretchToFill, true, 0, WoodLight, 0, 10);
            for (var y = rect.y + rect.height / 3f; y < rect.yMax - 4f; y += rect.height / 3f)
            {
                GUI.DrawTexture(new Rect(rect.x + 3f, y, rect.width - 6f, 2f), skin.White, ScaleMode.StretchToFill, true, 0, WoodDark, 0, 0);
            }

            GUI.DrawTexture(rect, skin.White, ScaleMode.StretchToFill, true, 0, WoodEdge, 3f, 10);
        }

        /// <summary>A round shortcut with its label under it. Returns true when clicked.</summary>
        public static bool RoundButton(UiSkin skin, Rect circle, string icon, string label, Round kind, bool active)
        {
            EnsureRounds();
            var hover = GUI.enabled && circle.Contains(Event.current.mousePosition);
            var c = circle;
            if (kind == Round.Gold)
            {
                // Gold and breathing while the event is on.
                var pulse = 1f + 0.05f * Mathf.Sin(Time.unscaledTime * 4f);
                c = new Rect(circle.center.x - circle.width * pulse / 2f, circle.center.y - circle.height * pulse / 2f, circle.width * pulse, circle.height * pulse);
                skin.DrawGlow(c, UiSkin.Gold, 0.45f + 0.2f * Mathf.Sin(Time.unscaledTime * 4f));
            }
            else if (active || hover)
            {
                skin.DrawGlow(c, UiSkin.Accent, active ? 0.35f : 0.18f);
            }

            var radius = c.width / 2f;
            var face = kind == Round.Gold ? _roundGold : kind == Round.Pass ? _roundPass : _roundBlue;
            Color border = kind == Round.Gold ? new Color32(0xFF, 0xF1, 0xC4, 0xFF) : kind == Round.Pass ? new Color32(0x9F, 0xF0, 0xD2, 0xFF) : new Color32(0x8C, 0xC4, 0xEE, 0xFF);
            GUI.DrawTexture(new Rect(c.x, c.y + 4f, c.width, c.height), skin.White, ScaleMode.StretchToFill, true, 0, new Color(0f, 0f, 0f, 0.4f), 0, radius);
            GUI.DrawTexture(c, face, ScaleMode.StretchToFill, true, 0, Color.white, 0, radius);
            var frame = ArtAssets.Texture(ShortcutFrameArt);
            if (frame != null)
            {
                GUI.DrawTexture(new Rect(c.x - 4f, c.y - 4f, c.width + 8f, c.height + 8f), frame, ScaleMode.ScaleToFit, true);
            }
            else
            {
                GUI.DrawTexture(c, skin.White, ScaleMode.StretchToFill, true, 0, border, 3f, radius);
            }

            var iconSize = c.width * 0.5f;
            skin.DrawIcon(new Rect(c.center.x - iconSize / 2f, c.center.y - iconSize / 2f, iconSize, iconSize), icon, kind == Round.Gold ? (Color)new Color32(0x3A, 0x23, 0x06, 0xFF) : Color.white);
            if (!string.IsNullOrEmpty(label))
            {
                UiSkin.ShadowLabel(new Rect(circle.x - 30f, circle.yMax + 4f, circle.width + 60f, UiSkin.SmallLine), label, RoundLabel(skin));
            }

            return GUI.Button(circle, GUIContent.none, GUIStyle.none);
        }

        /// <summary>The time left of an event in a dark pill over the top of a round shortcut.</summary>
        public static void TimePill(UiSkin skin, Rect circle, string text)
        {
            var w = TinyGold(skin).CalcSize(new GUIContent(text)).x + 16f;
            var pill = new Rect(circle.center.x - w / 2f, circle.y - 14f, w, 22f);
            GUI.DrawTexture(pill, skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.04f, 0.08f, 0.14f, 0.92f), 0, 11);
            GUI.DrawTexture(pill, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Gold, 1.5f, 11);
            GUI.Label(pill, text, TinyGold(skin));
        }

        /// <summary>A small dark pill with a short text over the bottom of a round shortcut (the Aquário's "12/40").</summary>
        public static void BottomPill(UiSkin skin, Rect circle, string text)
        {
            var style = RoundLabel(skin);
            var w = style.CalcSize(new GUIContent(text)).x + 12f;
            var pill = new Rect(circle.center.x - w / 2f, circle.yMax - 14f, w, 20f);
            GUI.DrawTexture(pill, skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.04f, 0.08f, 0.14f, 0.9f), 0, 10);
            GUI.Label(pill, text, style);
        }

        /// <summary>A red dot (no count) or a red pill with the count, on the top-right corner of a button.</summary>
        public static void Badge(UiSkin skin, Rect target, int count, Color color)
        {
            if (count <= 0)
            {
                var dot = new Rect(target.xMax - 12f, target.y - 2f, 14f, 14f);
                GUI.DrawTexture(dot, skin.White, ScaleMode.StretchToFill, true, 0, Color.white, 0, 7f);
                GUI.DrawTexture(new Rect(dot.x + 2f, dot.y + 2f, 10f, 10f), skin.White, ScaleMode.StretchToFill, true, 0, color, 0, 5f);
                return;
            }

            var text = count > 9 ? "9+" : count.ToString();
            var pill = new Rect(target.xMax - 18f, target.y - 6f, text.Length > 1 ? 28f : 22f, 22f);
            GUI.DrawTexture(pill, skin.White, ScaleMode.StretchToFill, true, 0, Color.white, 0, 11f);
            var inner = new Rect(pill.x + 2f, pill.y + 2f, pill.width - 4f, pill.height - 4f);
            GUI.DrawTexture(inner, skin.White, ScaleMode.StretchToFill, true, 0, color, 0, 9f);
            GUI.Label(inner, text, skin.PillText);
        }

        private static void EnsureTabFaces()
        {
            if (_tabFaces != null)
            {
                return;
            }

            _tabFaces = new Texture2D[TabTop.Length];
            for (var i = 0; i < TabTop.Length; i++)
            {
                _tabFaces[i] = Gradient(TabTop[i], TabBottom[i]);
            }
        }

        private static void EnsureRounds()
        {
            if (_roundBlue != null)
            {
                return;
            }

            _roundBlue = Gradient(new Color32(0x2F, 0x6A, 0x9E, 0xFF), new Color32(0x14, 0x30, 0x4F, 0xFF));
            _roundPass = Gradient(new Color32(0x3A, 0x8F, 0x7A, 0xFF), new Color32(0x14, 0x47, 0x3C, 0xFF));
            _roundGold = Gradient(new Color32(0xFF, 0xE0, 0x8A, 0xFF), new Color32(0xD9, 0x95, 0x1E, 0xFF));
        }

        /// <summary>A 1×32 vertical gradient (top colour at the top), stretched over a rect.</summary>
        private static Texture2D Gradient(Color top, Color bottom)
        {
            const int height = 32;
            var tex = new Texture2D(1, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };
            for (var y = 0; y < height; y++)
            {
                tex.SetPixel(0, y, Color.Lerp(bottom, top, y / (float)(height - 1)));
            }

            tex.Apply();
            return tex;
        }
    }
}
