using System.Linq;
using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Scene;
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
            var rect = WindowFrame.Dialog(skin, screenWidth, screenHeight, 360f + (best.Count > 0 ? 120f : 0f), 720f);
            var x = rect.x + 30;
            var w = rect.width - 60;
            var y = rect.y + 24;

            GUI.Label(new Rect(x, y, w, 32), GameTexts.Offline.Title, skin.Title);
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
            GUI.Label(new Rect(x, y, w, 28), count == 0 ? GameTexts.Offline.NothingCaught : GameTexts.Offline.Caught(count), skin.Heading);
            y += 32;
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
                    GUI.DrawTexture(new Rect(x + i * cw, y, cw - 10, 56), Art.FishTexture(c.SpeciesId), ScaleMode.ScaleToFit, true);
                    GUI.Label(new Rect(x + i * cw, y + 58, cw - 10, 20), c.SpeciesName, skin.Small);
                    GUI.Label(new Rect(x + i * cw, y + 76, cw - 10, 20), Format.SizeCm(c.SizeCm), skin.SmallMuted);
                }
            }

            GUI.Label(new Rect(x, rect.yMax - 112, w, 40), GameTexts.Offline.Note, skin.SmallMuted);
            var openBox = false;
            if (count > 0 && GUI.Button(new Rect(x, rect.yMax - 62, 280, 42), GameTexts.Offline.OpenBox, skin.Button))
            {
                root.WelcomeBack = null;
                openBox = true;
            }

            if (GUI.Button(new Rect(rect.xMax - 230, rect.yMax - 62, 200, 42), GameTexts.Offline.Continue, skin.ButtonPrimary))
            {
                root.WelcomeBack = null;
            }

            return openBox;
        }
    }
}
