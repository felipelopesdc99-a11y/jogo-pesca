using System.Linq;
using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Scene;
using FishingIdle.Game.Visual;
using FishingIdle.GameService.Crew;
using FishingIdle.GameService.Fishing;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The simple return screen of offline fishing (GDD section 10 / START HERE M5), with what the Crew earned while
    /// the game was closed (M24-T05, A-154) in the same summary. Either part may be missing: fishing was off, or no
    /// one is hired yet.
    /// </summary>
    public static class WelcomeBackDialog
    {
        /// <summary>Draws the dialog while there is a report. Returns true when the player asked for the Fishing Box.</summary>
        public static bool Draw(UiSkin skin, GameRoot root, float screenWidth, float screenHeight)
        {
            var report = root.WelcomeBack;
            var crew = root.CrewWelcome;
            if (report == null && crew == null)
            {
                return false;
            }

            var update = report?.Update ?? new FishingUpdate();
            var best = update.NewCatches.Where(c => c.IsImportant).OrderByDescending(c => c.SalePriceCoins).Take(4).ToList();
            var count = update.NewCatches.Count;
            var newSpecies = update.NewCatches.Count(c => c.IsNewSpecies);

            // The height follows the lines actually drawn below (24 px each), so none runs into the note.
            var lines = 0;
            if (report != null)
            {
                lines = (report.Capped ? 1 : 0) + (update.Escapes.Count > 0 ? 1 : 0) + (update.SkippedBoxFull > 0 ? 1 : 0) + (update.BaitRanOut != null ? 1 : 0);
                if (count > 0)
                {
                    lines += 1 + (newSpecies > 0 ? 1 : 0) + (update.ShellsGained > 0 ? 1 : 0) + (update.LevelsReached.Count > 0 ? 1 : 0);
                }
            }

            // The Crew's part: a gap, its title and Moedas (56), XP, a level reached, the cap, and its 2-line note (46).
            var crewHeight = crew == null ? 0f
                : 16f + 56f + ((crew.XpGained > 0 ? 1 : 0) + (crew.LevelsReached.Count > 0 ? 1 : 0) + (crew.Capped ? 1 : 0)) * 24f + 46f;

            // Title and time away (92), the fishing heading (40), each optional line, the best catches (128), the Crew,
            // the note and buttons (124).
            var height = 92f + (report != null ? 40f : 0f) + lines * 24f + (best.Count > 0 ? 128f : 0f) + crewHeight + 124f;
            var rect = WindowFrame.Dialog(skin, screenWidth, screenHeight, Mathf.Max(360f, height), 720f);
            var x = rect.x + 30;
            var w = rect.width - 60;
            var y = rect.y + 24;

            // One summary instead of dozens of popups (Art Bible, section 16.8).
            skin.IconBadge(new Rect(rect.xMax - 88, rect.y + 20, 58, 58), Icons.Clock, UiSkin.Accent);
            GUI.Label(new Rect(x, y, w - 80, 32), GameTexts.Offline.Title, skin.Title);
            y += 42;
            GUI.Label(new Rect(x, y, w, 22), GameTexts.Offline.Away(Format.Duration((report != null ? report.AwayMs : crew.AwayMs) / 1000.0)), skin.Body);
            y += 26;
            if (report != null)
            {
                y = DrawFishing(skin, report, update, best, count, newSpecies, x, w, y);
            }

            if (crew != null)
            {
                DrawCrew(skin, crew, x, w, y + 16f);
            }

            var note = report != null
                ? GameTexts.Offline.Note(Format.Duration(report.CycleSeconds), GameTexts.Offline.Hours(report.CapHours))
                : string.Empty;
            GUI.Label(new Rect(x, rect.yMax - 114, w, 44), note, skin.SmallMuted);
            var openBox = false;
            if (count > 0 && skin.IconButton(new Rect(x, rect.yMax - 62, 280, 42), Icons.Box, GameTexts.Offline.OpenBox, skin.Button))
            {
                root.WelcomeBack = null;
                root.CrewWelcome = null;
                openBox = true;
            }

            if (skin.IconButton(new Rect(rect.xMax - 230, rect.yMax - 62, 200, 42), Icons.Play, GameTexts.Offline.Continue, skin.ButtonPrimary))
            {
                root.WelcomeBack = null;
                root.CrewWelcome = null;
            }

            return openBox;
        }

        /// <summary>The fishing part: the cap, the catches and escapes, XP, species, Conchas, levels and the best catches.</summary>
        private static float DrawFishing(UiSkin skin, OfflineReport report, FishingUpdate update, System.Collections.Generic.List<CatchView> best, int count, int newSpecies, float x, float w, float y)
        {
            if (report.Capped)
            {
                GUI.Label(new Rect(x, y, w, 22), GameTexts.Offline.CappedAt(Format.Duration(report.CountedMs / 1000.0)), skin.SmallGold);
                y += 24;
            }

            y += 8;
            GUI.contentColor = count > 0 ? UiSkin.GoldLight : Color.white;
            GUI.Label(new Rect(x, y, w, 28), count == 0 ? GameTexts.Offline.NothingCaught : GameTexts.Offline.Caught(count), skin.Heading);
            GUI.contentColor = Color.white;
            y += 32;
            if (update.Escapes.Count > 0)
            {
                GUI.Label(new Rect(x, y, w, 22), GameTexts.Offline.Escaped(update.Escapes.Count), skin.SmallMuted);
                y += 24;
            }

            if (update.SkippedBoxFull > 0)
            {
                GUI.Label(new Rect(x, y, w, 22), GameTexts.Box.SkippedFull(update.SkippedBoxFull), skin.SmallGold);
                y += 24;
            }

            if (update.BaitRanOut != null)
            {
                GUI.Label(new Rect(x, y, w, 22), GameTexts.Fishing.BaitRanOut(update.BaitRanOut), skin.SmallGold);
                y += 24;
            }

            if (count > 0)
            {
                GUI.Label(new Rect(x, y, w, 22), update.VipXpGained > 0 ? GameTexts.Offline.XpWithVip(Format.Number(update.XpGained), Format.Number(update.VipXpGained)) : GameTexts.Offline.Xp(Format.Number(update.XpGained)), skin.Body);
                y += 24;
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
                    GUI.Label(new Rect(x, y, w, 22), update.DollarsGained > 0 ? GameTexts.Offline.LevelsWithDollars(update.LevelsReached.Max(), Format.Number(update.DollarsGained)) : GameTexts.Offline.Levels(update.LevelsReached.Max()), skin.SmallGold);
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
                    GUI.Label(new Rect(x + i * cw, y + 58, cw - 10, 20), FishCard.Fit(c.SpeciesName, skin.Small, cw - 10), skin.Small);
                    // The rarity in words next to the size, not only as the glow colour (M22-T12).
                    var line = string.IsNullOrEmpty(c.RarityName) ? Format.SizeCm(c.SizeCm) : c.RarityName + " · " + Format.SizeCm(c.SizeCm);
                    GUI.Label(new Rect(x + i * cw, y + 78, cw - 10, 20), FishCard.Fit(line, skin.SmallMuted, cw - 10), skin.SmallMuted);
                }

                y += 100;
            }

            return y;
        }

        /// <summary>What the Crew earned while the game was closed: Moedas, XP, a level reached, the cap and the rule.</summary>
        private static void DrawCrew(UiSkin skin, CrewOfflineReport crew, float x, float w, float y)
        {
            skin.Divider(new Rect(x, y - 8f, w, 1f));
            GUI.contentColor = UiSkin.GoldLight;
            GUI.Label(new Rect(x, y, w, 28), GameTexts.Offline.CrewTitle, skin.Heading);
            GUI.contentColor = Color.white;
            y += 32;
            skin.CoinIcon(new Rect(x, y, 20, 20));
            GUI.Label(new Rect(x + 26, y, w - 26, 22), GameTexts.Offline.CrewCoins(Format.Short(crew.CoinsGained)), skin.Body);
            y += 24;
            if (crew.XpGained > 0)
            {
                GUI.Label(new Rect(x, y, w, 22), GameTexts.Offline.CrewXp(Format.Number(crew.XpGained)), skin.Body);
                y += 24;
            }

            if (crew.LevelsReached.Count > 0)
            {
                GUI.Label(new Rect(x, y, w, 22), crew.DollarsGained > 0 ? GameTexts.Offline.LevelsWithDollars(crew.LevelsReached.Max(), Format.Number(crew.DollarsGained)) : GameTexts.Offline.Levels(crew.LevelsReached.Max()), skin.SmallGold);
                y += 24;
            }

            if (crew.Capped)
            {
                GUI.Label(new Rect(x, y, w, 22), GameTexts.Offline.CrewCapped(GameTexts.Offline.Hours(crew.MaxHours)), skin.SmallGold);
                y += 24;
            }

            GUI.Label(new Rect(x, y + 2, w, 44), GameTexts.Offline.CrewNote(GameTexts.Offline.Hours(crew.FullRateHours), Format.Percent(crew.ReducedRate, 0), GameTexts.Offline.Hours(crew.MaxHours)), skin.SmallMuted);
        }
    }
}
