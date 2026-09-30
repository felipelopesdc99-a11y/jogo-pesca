using System.Linq;
using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Scene;
using FishingIdle.Game.Visual;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>The simple return screen of offline fishing (GDD section 10 / START HERE M5).</summary>
    public static class WelcomeBackDialog
    {
        /// <summary>Draws the dialog while there is a report. Returns true when the player asked for the Fishing Box.</summary>
        public static bool Draw(UiSkin skin, GameRoot root, float screenWidth, float screenHeight)
        {
            var report = root.WelcomeBack;
            if (report == null)
            {
                return false;
            }

            var update = report.Update;
            var best = update.NewCatches.Where(c => c.IsImportant).OrderByDescending(c => c.SalePriceCoins).Take(4).ToList();
            var extra = (update.Escapes.Count > 0 ? 24f : 0f) + (update.BaitRanOut != null ? 24f : 0f);
            var rect = WindowFrame.Dialog(skin, screenWidth, screenHeight, 360f + extra + (best.Count > 0 ? 120f : 0f), 720f);
            var x = rect.x + 30;
            var w = rect.width - 60;
            var y = rect.y + 24;

            // One summary instead of dozens of popups (Art Bible, section 16.8).
            skin.IconBadge(new Rect(rect.xMax - 88, rect.y + 20, 58, 58), Icons.Clock, UiSkin.Accent);
            GUI.Label(new Rect(x, y, w - 80, 32), GameTexts.Offline.Title, skin.Title);
            y += 42;
            GUI.Label(new Rect(x, y, w, 22), GameTexts.Offline.Away(Format.Duration(report.AwayMs / 1000.0)), skin.Body);
            y += 26;
            if (report.Capped)
            {
                GUI.Label(new Rect(x, y, w, 22), GameTexts.Offline.CappedAt(Format.Duration(report.CountedMs / 1000.0)), skin.SmallGold);
                y += 24;
            }

            y += 8;
            var count = update.NewCatches.Count;
            GUI.contentColor = count > 0 ? UiSkin.GoldLight : Color.white;
            GUI.Label(new Rect(x, y, w, 28), count == 0 ? GameTexts.Offline.NothingCaught : GameTexts.Offline.Caught(count), skin.Heading);
            GUI.contentColor = Color.white;
            y += 32;
            if (update.Escapes.Count > 0)
            {
                GUI.Label(new Rect(x, y, w, 22), GameTexts.Offline.Escaped(update.Escapes.Count), skin.SmallMuted);
                y += 24;
            }

            if (update.BaitRanOut != null)
            {
                GUI.Label(new Rect(x, y, w, 22), GameTexts.Fishing.BaitRanOut(update.BaitRanOut), skin.SmallGold);
                y += 24;
            }

            if (count > 0)
            {
                GUI.Label(new Rect(x, y, w, 22), GameTexts.Offline.Xp(Format.Number(update.XpGained)), skin.Body);
                y += 24;
                var newSpecies = update.NewCatches.Count(c => c.IsNewSpecies);
                if (newSpecies > 0)
                {
                    GUI.Label(new Rect(x, y, w, 22), GameTexts.Offline.NewSpecies(newSpecies), skin.Body);
                    y += 24;
                }

                if (update.ShellsGained > 0)
                {
                    GUI.Label(new Rect(x, y, w, 22), GameTexts.Toasts.Shells(Format.Number(update.ShellsGained)), skin.Body);
                    y += 24;
                }

                if (update.LevelsReached.Count > 0)
                {
                    GUI.Label(new Rect(x, y, w, 22), GameTexts.Offline.Levels(update.LevelsReached.Max()), skin.SmallGold);
                    y += 24;
                }
            }

            if (best.Count > 0)
            {
                y += 6;
                GUI.Label(new Rect(x, y, w, 20), GameTexts.Offline.Best, skin.SmallMuted);
                y += 22;
                var cw = w / 4f;
                for (var i = 0; i < best.Count; i++)
                {
                    var c = best[i];
                    var accent = VisualTheme.IsSpecialSize(c.SizeCategoryId) ? UiSkin.SizeColor(c.SizeCategoryId) : UiSkin.RarityColor(c.RarityId);
                    skin.DrawGlow(new Rect(x + i * cw + cw * 0.2f, y + 14, cw * 0.5f, 28), accent, 0.3f);
                    GUI.DrawTexture(new Rect(x + i * cw, y, cw - 10, 56), Art.FishTexture(c.SpeciesId), ScaleMode.ScaleToFit, true);
                    GUI.Label(new Rect(x + i * cw, y + 58, cw - 10, 20), c.SpeciesName, skin.Small);
                    GUI.Label(new Rect(x + i * cw, y + 76, cw - 10, 20), Format.SizeCm(c.SizeCm), skin.SmallMuted);
                }
            }

            GUI.Label(new Rect(x, rect.yMax - 112, w, 40), GameTexts.Offline.Note, skin.SmallMuted);
            var openBox = false;
            if (count > 0 && skin.IconButton(new Rect(x, rect.yMax - 62, 280, 42), Icons.Box, GameTexts.Offline.OpenBox, skin.Button))
            {
                root.WelcomeBack = null;
                openBox = true;
            }

            if (skin.IconButton(new Rect(rect.xMax - 230, rect.yMax - 62, 200, 42), Icons.Play, GameTexts.Offline.Continue, skin.ButtonPrimary))
            {
                root.WelcomeBack = null;
            }

            return openBox;
        }
    }
}
