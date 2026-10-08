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
        private static GUIStyle _placeholder;

        /// <summary>Draws the box and returns the (possibly edited) text.</summary>
        public static string Field(UiSkin skin, Rect rect, string text, string controlName)
        {
            if (_placeholder == null)
            {
                _placeholder = new GUIStyle(skin.Body) { alignment = TextAnchor.MiddleLeft, wordWrap = false, clipping = TextClipping.Clip };
                _placeholder.normal.textColor = UiSkin.Muted;
            }

            text = text ?? string.Empty;

            // The ✕ is handled before the field: IMGUI gives a click to the first control that takes it,
            // and the field (drawn over the same place) would otherwise swallow it.
            var clearRect = new Rect(rect.xMax - 28, rect.y + (rect.height - 22f) / 2f, 22, 22);
            if (text.Length > 0 && GUI.Button(clearRect, GUIContent.none, GUIStyle.none))
            {
                text = string.Empty;
                GUI.FocusControl(null);
            }

            GUI.SetNextControlName(controlName);
            var edited = GUI.TextField(rect, text, 40, skin.SearchField);
            skin.DrawIcon(new Rect(rect.x + 12, rect.y + (rect.height - 18f) / 2f, 18, 18), Icons.Search, UiSkin.Accent);
            if (edited.Length == 0 && GUI.GetNameOfFocusedControl() != controlName)
            {
                GUI.Label(new Rect(rect.x + 38, rect.y, rect.width - 44, rect.height), GameTexts.Search.Placeholder, _placeholder);
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
