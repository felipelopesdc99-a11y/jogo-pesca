using System.Globalization;
using System.Text;
using FishingIdle.Game.Visual;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The "search by fish name" box of the Fishing Box, Aquarium and Market (addendum A-084): a text
    /// field with a magnifier, a hint while empty and an ✕ to clear. The match ignores upper/lower
    /// case and accents, so "tilapia" finds "Tilápia". Presentation only: it narrows what is shown.
    /// </summary>
    public static class NameSearch
    {
        private static GUIStyle _field;

        /// <summary>Draws the box and returns the (possibly edited) text.</summary>
        public static string Field(UiSkin skin, Rect rect, string text, string controlName)
        {
            if (_field == null)
            {
                _field = new GUIStyle(GUI.skin.textField)
                {
                    font = skin.BodyFont,
                    fontSize = 14,
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(34, 30, 4, 4),
                    border = skin.Chip.border,
                };
                _field.normal.background = skin.Chip.normal.background;
                _field.hover.background = skin.Chip.hover.background;
                _field.focused.background = skin.Chip.hover.background;
                _field.active.background = skin.Chip.hover.background;
                _field.normal.textColor = _field.hover.textColor = _field.focused.textColor = _field.active.textColor = UiSkin.Text;
            }

            text = text ?? string.Empty;
            GUI.SetNextControlName(controlName);
            var edited = GUI.TextField(rect, text, 40, _field);
            skin.DrawIcon(new Rect(rect.x + 10, rect.y + (rect.height - 16f) / 2f, 16, 16), Icons.Search, UiSkin.Muted);
            if (edited.Length == 0 && GUI.GetNameOfFocusedControl() != controlName)
            {
                GUI.Label(new Rect(rect.x + 34, rect.y, rect.width - 40, rect.height), GameTexts.Search.Placeholder, skin.SmallMuted);
            }

            if (edited.Length > 0 && GUI.Button(new Rect(rect.xMax - 28, rect.y + (rect.height - 22f) / 2f, 22, 22), GUIContent.none, GUIStyle.none))
            {
                edited = string.Empty;
                GUI.FocusControl(null);
            }

            if (edited.Length > 0)
            {
                skin.DrawIcon(new Rect(rect.xMax - 25, rect.y + (rect.height - 14f) / 2f, 14, 14), Icons.Close, UiSkin.Muted);
            }

            return edited;
        }

        /// <summary>Whether a fish name contains the search, ignoring case and accents. An empty search matches everything.</summary>
        public static bool Matches(string name, string search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return true;
            }

            return !string.IsNullOrEmpty(name) && Fold(name).Contains(Fold(search.Trim()));
        }

        private static string Fold(string s)
        {
            var decomposed = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);
            foreach (var c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(char.ToLowerInvariant(c));
                }
            }

            return sb.ToString();
        }
    }
}
