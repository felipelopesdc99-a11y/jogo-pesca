using System.Collections.Generic;
using System.Linq;
using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Scene;
using FishingIdle.Game.Visual;
using FishingIdle.GameService.Fishing;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The "Bem-vindo de volta" report of offline fishing (GDD section 10 / START HERE M5), redone as a report in M24-T51
    /// (A-175) and simplified in M25-T24 (A-191): the scene strip with the time away, a row of big numbers with their icon
    /// (fish, sale value, XP, and the Conchas when some came), the catches by rarity, the highlights, then the fragments
    /// and the warnings as plain lines, one primary button. No card around a number and no glow. Every number comes from the service's <see cref="OfflineReport"/>; the screen only lays them out. What it
    /// shows is built once per report; only the short count-up at the opening formats numbers frame by frame.
    /// </summary>
    public static class WelcomeBackDialog
    {
        // Layout on the HUD's 1080-tall canvas (A-149, grid of 8 since A-190): the frame is 920 wide (content 864) on every
        // screen, ~1200×600 and ~1320×840 alike; only its height follows the report, up to the canvas less 120. Four text
        // sizes (A-191): the window title 40, the numbers 42, the body 24 (headings and the scene title in Bold) and 21.
        private const float MaxWidth = 920f, Gap = 16f, SectionGap = 32f, HeadingHeight = 32f;
        private const float StatHeight = 96f, BestHeight = 184f, GearHeight = 56f, WarningHeight = 40f, FooterHeight = 56f;
        private const float SceneHeight = 120f, RarityBar = 16f, LegendRow = 24f;

        // Three highlights (M25-T24, A-191): 272 wide cards keep the rarity seal and the "Nova espécie" tag on one row.
        private const int BestCount = 3;

        // Presentation only: the count-up at the opening, and from when the box counts as "almost full" for the warning.
        private const float CountUpSeconds = 0.9f, AlmostFullRatio = 0.9f;

        // bemvindo_cena (1600×700, received 13/10/2026); a scene drawn by the code only if the file is missing.
        private const string SceneArt = "UI/bemvindo_cena";
        private const string FragmentFolder = "Fragmentos/frag_", ClothingFragmentArt = "Fragmentos/frag_roupa";

        private sealed class Stat
        {
            public string Icon, Label, Final, FinalShort, Sub;
            public long Value;
            public bool Currency, Highlight;
        }

        private sealed class Gear
        {
            public string Art, Icon, Name, Kind, Gained, Count, Tag;
            public float Fill = -1f;
            public bool KindGold;
        }

        private sealed class Warning
        {
            public string Icon, Text;
            public bool Strong;
            public float Fill = -1f;
        }

        private sealed class RarityEntry
        {
            public string Name, Count;
            public int Amount;
            public Color Color;
            public float NameWidth, CountWidth;
        }

        // What the report shows, built once per report.
        private static OfflineReport _built;
        private static float _openedAt;
        private static Vector2 _scroll;
        private static readonly List<Stat> Stats = new List<Stat>();
        private static readonly List<RarityEntry> Rarities = new List<RarityEntry>();
        private static readonly List<FishCardModel> Best = new List<FishCardModel>();
        private static readonly List<Gear> Gears = new List<Gear>();
        private static readonly List<Warning> Warnings = new List<Warning>();
        private static int _rarityTotal, _count;
        private static string _sceneTitle, _sceneLine, _bestNote, _clothingLevels, _note;
        private static readonly GUIContent Measure = new GUIContent();

        /// <summary>Draws the report while there is one. Returns true when the player asked for the Fishing Box.</summary>
        public static bool Draw(UiSkin skin, GameRoot root, float screenWidth, float screenHeight)
        {
            var report = root.WelcomeBack;
            if (report == null)
            {
                return false;
            }

            if (!ReferenceEquals(report, _built))
            {
                Build(skin, report);
            }

            var ease = 1f - Mathf.Pow(1f - Mathf.Clamp01((Time.unscaledTime - _openedAt) / CountUpSeconds), 3f);
            const float sceneHeight = SceneHeight;

            // The frame is as tall as the report (never taller than the screen; the body scrolls instead).
            var width = Mathf.Min(MaxWidth, screenWidth - 280f) - 56f;
            var wanted = 80f + BodyHeight(skin, width, sceneHeight) + 16f + FooterHeight + 8f + 24f;
            var panel = WindowFrame.Panel(skin, screenWidth, screenHeight, MaxWidth, Mathf.Max(420f, wanted));
            var closed = WindowFrame.Header(skin, panel, GameTexts.Offline.Title, null, Icons.Clock);
            var content = WindowFrame.Content(panel);

            var area = new Rect(content.x, content.y, content.width, content.height - FooterHeight - 16f);
            var body = BodyHeight(skin, area.width, sceneHeight);
            var scrolls = body > area.height + 1f;
            var openBox = false;
            if (scrolls)
            {
                body = BodyHeight(skin, area.width - 20f, sceneHeight);
                _scroll = GUI.BeginScrollView(area, _scroll, new Rect(0f, 0f, area.width - 20f, body));
                openBox = DrawBody(skin, report, 0f, 0f, area.width - 20f, sceneHeight, ease);
                GUI.EndScrollView();
            }
            else
            {
                openBox = DrawBody(skin, report, area.x, area.y, area.width, sceneHeight, ease);
            }

            // One primary button (A-190 rule 8): "Abrir a Caixa de Pesca" when fish wait in it, otherwise "Continuar". Both
            // measured with their icon, so the label is never cut.
            var footer = WindowFrame.Footer(skin, content, FooterHeight);
            var buttonY = footer.y + (footer.height - 48f) / 2f;
            var hasBox = report.FishingBoxCount > 0;
            var continueWidth = ButtonWidth(skin, GameTexts.Offline.Continue);
            var right = footer.xMax;
            if (hasBox)
            {
                var boxWidth = ButtonWidth(skin, GameTexts.Offline.OpenBox);
                if (skin.IconButton(new Rect(right - boxWidth, buttonY, boxWidth, 48f), Icons.Box, GameTexts.Offline.OpenBox, skin.ButtonPrimary))
                {
                    openBox = true;
                }

                right -= boxWidth + Gap;
            }

            if (skin.IconButton(new Rect(right - continueWidth, buttonY, continueWidth, 48f), Icons.Play, GameTexts.Offline.Continue, hasBox ? skin.Button : skin.ButtonPrimary))
            {
                closed = true;
            }

            if (closed || openBox)
            {
                root.WelcomeBack = null;
            }

            return openBox;
        }

        // ------------------------------------------------------------------ built once per report

        private static void Build(UiSkin skin, OfflineReport report)
        {
            _built = report;
            _openedAt = Time.unscaledTime;
            _scroll = Vector2.zero;
            var update = report.Update ?? new FishingUpdate();
            var catches = update.NewCatches;
            _count = catches.Count;

            _sceneTitle = GameTexts.Offline.SceneAway(Format.Duration(report.AwayMs / 1000.0));
            _sceneLine = report.Capped ? GameTexts.Offline.SceneCapped(Format.Duration(report.CountedMs / 1000.0))
                : _count > 0 ? GameTexts.Offline.SceneKept : GameTexts.Offline.SceneNothing;

            // The big numbers.
            Stats.Clear();
            if (_count > 0)
            {
                var newSpecies = catches.Count(c => c.IsNewSpecies);
                Stats.Add(NewStat(Icons.Fish, GameTexts.Offline.StatFish, _count, newSpecies > 0 ? GameTexts.Offline.NewSpecies(newSpecies) : null));
                var value = NewStat(Icons.Coin, GameTexts.Offline.StatValue, report.SaleValueCoins, GameTexts.Offline.StatValueSub);
                value.Currency = true;
                Stats.Add(value);
            }

            if (update.XpGained > 0)
            {
                var levelled = report.FisherLevelAfter > report.FisherLevelBefore;
                var xp = NewStat(Icons.Level, GameTexts.Offline.StatXp, update.XpGained,
                    levelled ? GameTexts.Offline.StatLevel(report.FisherLevelBefore, report.FisherLevelAfter)
                        : update.VipXpGained > 0 ? GameTexts.Offline.StatXpVip(Format.Number(update.VipXpGained)) : null);
                xp.Highlight = levelled;
                Stats.Add(xp);
            }

            if (update.ShellsGained > 0)
            {
                var shells = NewStat(Icons.Shell, GameTexts.Offline.StatShells, update.ShellsGained, null);
                shells.Currency = true;
                Stats.Add(shells);
            }

            // The catches by rarity, in the service's order.
            Rarities.Clear();
            _rarityTotal = 0;
            foreach (var r in report.Rarities)
            {
                // The rarity colour as a fill; a colour too close to the panel is made readable first.
                var color = VisualTheme.Current.Rarity(r.RarityId);
                if (UiSkin.Contrast(color, UiSkin.PanelColor) < 1.6f)
                {
                    color = UiSkin.Readable(color);
                }

                var entry = new RarityEntry { Name = r.RarityName, Count = Format.Number(r.Count), Amount = r.Count, Color = color };
                entry.NameWidth = Width(skin.SmallMuted, entry.Name);
                entry.CountWidth = Width(skin.SmallBold, entry.Count);
                Rarities.Add(entry);
                _rarityTotal += r.Count;
            }

            // The highlights: the important catches as fish cards, the best genes (5 stars, M24-T52) first, then the most valuable.
            Best.Clear();
            foreach (var c in catches.Where(c => c.IsImportant)
                         .OrderByDescending(c => c.Genes != null && c.Genes.IsCelebrated)
                         .ThenByDescending(c => c.Genes != null && c.Genes.IsCelebrated ? c.Genes.Total : 0)
                         .ThenByDescending(c => c.SalePriceCoins).Take(BestCount))
            {
                var model = new FishCardModel
                {
                    SpeciesId = c.SpeciesId,
                    Name = c.SpeciesName,
                    Line = VisualTheme.IsSpecialSize(c.SizeCategoryId) ? Format.SizeCm(c.SizeCm) : Format.SizeCm(c.SizeCm) + GameTexts.Offline.Separator + c.SizeCategoryName,
                    RarityId = c.RarityId,
                    RarityName = c.RarityName,
                    SizeCategoryId = c.SizeCategoryId,
                    SizeCategoryName = c.SizeCategoryName,
                    Coins = Format.Short(c.SalePriceCoins),
                    Genes = c.Genes,
                };
                FishCard.StatusBadge(model, c.IsNewSpecies, c.IsPersonalRecord);
                Best.Add(model);
            }

            var species = catches.Count(c => c.IsNewSpecies);
            var records = catches.Count(c => c.IsPersonalRecord);
            var topGenes = catches.Where(c => c.Genes != null && c.Genes.IsCelebrated).ToList();
            var notes = new List<string>();
            if (species > 0) notes.Add(GameTexts.Offline.NewSpecies(species));
            if (records > 0) notes.Add(GameTexts.Offline.Records(records));
            if (topGenes.Count > 0) notes.Add(GameTexts.Offline.TopGenes(topGenes.Count, topGenes.Min(c => c.Genes.Stars)));
            _bestNote = notes.Count > 0 ? string.Join(GameTexts.Offline.Separator, notes) : null;

            BuildGear(report, update);
            BuildWarnings(report, update);

            _note = report.VipCycleSeconds > 0
                ? GameTexts.Offline.NoteWithVip(Format.Duration(report.VipCycleSeconds), Format.Duration(report.CycleSeconds), GameTexts.Offline.Hours(report.CapHours))
                : GameTexts.Offline.Note(Format.Duration(report.CycleSeconds), GameTexts.Offline.Hours(report.CapHours));
        }

        private static Stat NewStat(string icon, string label, long value, string sub)
        {
            return new Stat { Icon = icon, Label = label, Value = value, Final = Format.Number(value), FinalShort = Format.Short(value), Sub = sub };
        }

        /// <summary>One card per boat and rod that dropped fragments (the last drop tells the progress), and the clothing.</summary>
        private static void BuildGear(OfflineReport report, FishingUpdate update)
        {
            Gears.Clear();
            foreach (var boat in update.Fragments.GroupBy(f => f.BoatId))
            {
                var last = boat.Last();
                var assembled = boat.Any(f => f.Ready);
                Gears.Add(FragmentGear(FragmentFolder + boat.Key, Icons.Boat, last.BoatName, GameTexts.Offline.BoatFragments, boat.Sum(f => f.Amount),
                    last.Have, last.Required, last.Owned, assembled, GameTexts.Offline.BoatReadyTag, GameTexts.Offline.BoatReadyKind));
            }

            foreach (var rod in update.RodFragments.GroupBy(f => f.RodId))
            {
                var last = rod.Last();
                var assembled = rod.Any(f => f.Ready);
                Gears.Add(FragmentGear(FragmentFolder + rod.Key, Icons.Rod, last.RodName, GameTexts.Offline.RodFragments, rod.Sum(f => f.Amount),
                    last.Have, last.Required, last.Owned, assembled, GameTexts.Offline.RodReadyTag, GameTexts.Offline.RodReadyKind));
            }

            if (update.ClothingFragments > 0)
            {
                var gear = new Gear
                {
                    Art = ClothingFragmentArt,
                    Icon = Icons.Star,
                    Name = GameTexts.Offline.ClothingFragmentsName,
                    Kind = GameTexts.Offline.ClothingFragmentsKind,
                    Gained = GameTexts.Offline.Gained(Format.Number(update.ClothingFragments)),
                };
                var need = report.ClothingFragmentsNeed;
                if (need <= 0)
                {
                    gear.Kind = GameTexts.Offline.ClothingAllAssembled;
                    gear.Count = Format.Number(report.ClothingFragmentsHave);
                }
                else
                {
                    gear.Fill = Mathf.Clamp01(report.ClothingFragmentsHave / (float)need);
                    gear.Count = GameTexts.Offline.Progress(Format.Number(report.ClothingFragmentsHave), Format.Number(need));
                    if (report.ClothingFragmentsHave >= need)
                    {
                        gear.Kind = GameTexts.Offline.ClothingReady;
                        gear.KindGold = true;
                    }
                }

                Gears.Add(gear);
            }

            _clothingLevels = update.ClothingLevels.Count == 0 ? null
                : GameTexts.Offline.ClothingLevels(string.Join(", ", update.ClothingLevels.GroupBy(l => l.PieceName).Select(g => GameTexts.Offline.PieceLevel(g.Key, g.Max(l => l.Level)))));
        }

        private static Gear FragmentGear(string art, string icon, string name, string kind, long amount, long have, int required, bool owned, bool assembled, string tag, string assembledKind)
        {
            var gear = new Gear { Art = art, Icon = icon, Name = name, Kind = kind, Gained = GameTexts.Offline.Gained(Format.Number(amount)) };
            if (assembled)
            {
                gear.Tag = tag;
                gear.Kind = assembledKind;
                gear.KindGold = true;
                gear.Fill = 1f;
            }
            else if (owned)
            {
                gear.Kind = GameTexts.Offline.Extras(Format.Number(have));
            }
            else if (required > 0)
            {
                gear.Fill = Mathf.Clamp01(have / (float)required);
                gear.Count = GameTexts.Offline.Progress(Format.Number(have), Format.Number(required));
            }

            return gear;
        }

        private static void BuildWarnings(OfflineReport report, FishingUpdate update)
        {
            Warnings.Clear();
            var cap = report.FishingBoxCapacity;
            if (cap > 0 && (report.FishingBoxFull || report.FishingBoxCount >= cap * AlmostFullRatio))
            {
                var count = Format.Number(report.FishingBoxCount);
                var capacity = Format.Number(cap);
                Warnings.Add(new Warning
                {
                    Icon = Icons.Box,
                    Text = report.FishingBoxFull ? GameTexts.Offline.BoxFull(count, capacity) : GameTexts.Offline.BoxAlmostFull(count, capacity),
                    Strong = true,
                    Fill = Mathf.Clamp01(report.FishingBoxCount / (float)cap),
                });
            }

            if (update.SkippedBoxFull > 0)
            {
                Warnings.Add(new Warning { Icon = Icons.Warning, Text = GameTexts.Box.SkippedFull(update.SkippedBoxFull), Strong = true });
            }

            if (update.BaitRanOut != null)
            {
                Warnings.Add(new Warning { Icon = Icons.Bait, Text = GameTexts.Fishing.BaitRanOut(update.BaitRanOut), Strong = true });
            }

            if (update.Escapes.Count > 0)
            {
                Warnings.Add(new Warning { Icon = Icons.Fish, Text = GameTexts.Offline.Escaped(update.Escapes.Count) });
            }
        }

        // ------------------------------------------------------------------ layout

        private static float BodyHeight(UiSkin skin, float width, float sceneHeight)
        {
            var h = sceneHeight;
            if (Stats.Count > 0)
            {
                h += SectionGap + StatHeight;
            }

            if (Rarities.Count > 0)
            {
                h += SectionGap + HeadingHeight + RarityBar + 8f + LegendRows(width) * LegendRow;
            }

            if (Best.Count > 0)
            {
                h += SectionGap + HeadingHeight + BestHeight;
            }

            if (Gears.Count > 0 || _clothingLevels != null)
            {
                h += SectionGap + HeadingHeight + Gears.Count * GearHeight + (_clothingLevels != null ? LegendRow : 0f);
            }

            if (Warnings.Count > 0)
            {
                h += SectionGap + HeadingHeight + Warnings.Count * WarningHeight;
            }

            Measure.text = _note;
            return h + SectionGap + skin.SmallMuted.CalcHeight(Measure, width) + 8f;
        }

        /// <summary>The rows the rarity legend takes at this width (a dot, the name and the count per rarity).</summary>
        private static int LegendRows(float width)
        {
            var rows = 1;
            var x = 0f;
            foreach (var r in Rarities)
            {
                var w = LegendWidth(r);
                if (x > 0f && x + w > width)
                {
                    rows++;
                    x = 0f;
                }

                x += w + 24f;
            }

            return rows;
        }

        private static float LegendWidth(RarityEntry r) => 16f + r.NameWidth + 6f + r.CountWidth;

        // ------------------------------------------------------------------ drawing

        private static bool DrawBody(UiSkin skin, OfflineReport report, float x, float y, float w, float sceneHeight, float ease)
        {
            var openBox = false;
            DrawScene(skin, new Rect(x, y, w, sceneHeight));
            y += sceneHeight;

            if (Stats.Count > 0)
            {
                y += SectionGap;
                var cw = (w - Gap * (Stats.Count - 1)) / Stats.Count;
                for (var i = 0; i < Stats.Count; i++)
                {
                    DrawStat(skin, new Rect(x + i * (cw + Gap), y, cw, StatHeight), Stats[i], ease);
                }

                y += StatHeight;
            }

            if (Rarities.Count > 0)
            {
                y += SectionGap;
                Heading(skin, x, y, w, GameTexts.Offline.Rarities, null);
                y += HeadingHeight;
                y = DrawRarities(skin, x, y, w, ease);
            }

            if (Best.Count > 0)
            {
                y += SectionGap;
                Heading(skin, x, y, w, GameTexts.Offline.Best, _bestNote);
                y += HeadingHeight;
                var cw = (w - Gap * (BestCount - 1)) / BestCount;
                for (var i = 0; i < Best.Count; i++)
                {
                    FishCard.Draw(skin, new Rect(x + i * (cw + Gap), y, cw, BestHeight), Best[i]);
                }

                y += BestHeight;
            }

            if (Gears.Count > 0 || _clothingLevels != null)
            {
                y += SectionGap;
                Heading(skin, x, y, w, GameTexts.Offline.Equipment, null);
                y += HeadingHeight;
                foreach (var gear in Gears)
                {
                    DrawGear(skin, new Rect(x, y, w, GearHeight), gear, ease);
                    y += GearHeight;
                }

                if (_clothingLevels != null)
                {
                    UiSkin.ColoredLabel(new Rect(x, y, w, UiSkin.SmallLine), FishCard.Fit(_clothingLevels, skin.SmallBold, w), skin.SmallBold, UiSkin.Gold);
                    y += LegendRow;
                }
            }

            if (Warnings.Count > 0)
            {
                y += SectionGap;
                Heading(skin, x, y, w, GameTexts.Offline.Warnings, null);
                y += HeadingHeight;
                foreach (var warning in Warnings)
                {
                    DrawWarning(skin, new Rect(x, y, w, WarningHeight), warning);
                    y += WarningHeight;
                }
            }

            y += SectionGap;
            Measure.text = _note;
            GUI.Label(new Rect(x, y, w, skin.SmallMuted.CalcHeight(Measure, w) + 8f), _note, skin.SmallMuted);
            return openBox;
        }

        /// <summary>The scene strip, set on the sheet like a photo: the art when it exists, a drawn lake at dusk otherwise.</summary>
        private static void DrawScene(UiSkin skin, Rect r)
        {
            WindowFrame.Photo(skin, r);
            var art = ArtAssets.Texture(SceneArt);
            if (art != null)
            {
                // The pixel scene at a whole scale, cut at the edges, the boat in the middle (M25-T15).
                PixelDraw.Cover(r, art, Color.white, 0.5f, 0.5f);
            }
            else
            {
                DrawDrawnScene(skin, r);
            }

            // A soft dark band on the left keeps the light text legible over any art.
            const int bands = 10;
            var bandWidth = r.width * 0.62f / bands;
            for (var i = 0; i < bands; i++)
            {
                var alpha = 0.55f * (1f - i / (float)bands);
                // Bands on whole pixels that meet edge to edge (no overlap, so no darker seams).
                var x0 = Mathf.Round(r.x + i * bandWidth);
                var x1 = Mathf.Round(r.x + (i + 1) * bandWidth);
                Fill(skin, new Rect(x0, r.y, x1 - x0, r.height), new Color(0.02f, 0.06f, 0.12f, alpha), 0f);
            }

            {
                // The body size in Bold over the small line (A-191: no fifth text size on this screen).
                const float titleHeight = 32f;
                var top = r.y + (r.height - titleHeight - UiSkin.SmallLine) / 2f;
                UiSkin.ShadowLabel(new Rect(r.x + 24f, top, r.width - 48f, titleHeight), FishCard.Fit(_sceneTitle, skin.Heading, r.width - 48f), skin.Heading);
                UiSkin.ShadowLabel(new Rect(r.x + 24f, top + titleHeight, r.width - 48f, UiSkin.SmallLine), FishCard.Fit(_sceneLine, skin.Small, r.width - 48f), skin.Small);
            }
        }

        /// <summary>The fallback scene (if bemvindo_cena is missing): a calm lake at dusk, a low sun, a few glints.</summary>
        private static void DrawDrawnScene(UiSkin skin, Rect r)
        {
            var sky = new Color(0.086f, 0.188f, 0.306f);
            var dusk = new Color(0.96f, 0.66f, 0.36f);
            var water = new Color(0.055f, 0.227f, 0.31f);
            var horizon = r.y + r.height * 0.62f;
            const int steps = 8;
            var stepHeight = (horizon - r.y) / steps;
            for (var i = 0; i < steps; i++)
            {
                // Square bands of colour (pixel art: no rounding, no gradient).
                Fill(skin, UiSkin.Snap(new Rect(r.x, r.y + i * stepHeight, r.width, stepHeight + 1f)), Color.Lerp(sky, dusk, i / (float)(steps - 1)), 0f);
            }

            var sun = r.height * 0.42f;
            var sunRect = new Rect(r.x + r.width * 0.74f, horizon - sun * 0.62f, sun, sun);
            skin.DrawGlow(sunRect, UiSkin.Gold, 0.35f);
            PixelDraw.Fit(sunRect, Art.Circle.texture, new Color(1f, 0.83f, 0.45f));
            Fill(skin, UiSkin.Snap(new Rect(r.x, horizon, r.width, r.yMax - horizon)), water, 0f);

            // The sun's glints on the water, drifting slowly.
            var t = Time.unscaledTime;
            for (var i = 0; i < 4; i++)
            {
                var gy = horizon + 6f + i * (r.yMax - horizon - 10f) / 4f;
                var gw = sun * (1.1f - i * 0.18f);
                var gx = sunRect.center.x - gw / 2f + Mathf.Sin(t * 0.8f + i) * 4f;
                Fill(skin, UiSkin.Snap(new Rect(gx, gy, gw, UiSkin.ArcPixel)), new Color(1f, 0.85f, 0.55f, 0.55f - i * 0.1f), 0f);
            }
        }

        /// <summary>
        /// A big number with its icon (A-190 rule 7), no card: the number counting up at the opening, its name under it in
        /// the small text and the short line (new species, the levels) under that, gold when it is a level up.
        /// </summary>
        private static void DrawStat(UiSkin skin, Rect r, Stat stat, float ease)
        {
            var icon = new Rect(r.x, r.y + 12f, 32f, 32f);
            if (stat.Currency)
            {
                skin.CurrencyIcon(icon, stat.Icon);
            }
            else
            {
                skin.DrawIcon(icon, stat.Icon, UiSkin.Accent);
            }

            var x = icon.xMax + 8f;
            var w = r.xMax - x;
            var text = ease >= 1f ? stat.Final : Format.Number((long)(stat.Value * (double)ease));
            if (Width(skin.NumberBig, stat.Final) > w)
            {
                text = ease >= 1f ? stat.FinalShort : Format.Short((long)(stat.Value * (double)ease));
            }

            GUI.Label(new Rect(x, r.y, w, 56f), text, skin.NumberBig);
            GUI.Label(new Rect(x, r.y + 48f, w, UiSkin.SmallLine), FishCard.Fit(stat.Label, skin.SmallMuted, w), skin.SmallMuted);
            if (!string.IsNullOrEmpty(stat.Sub))
            {
                var style = stat.Highlight ? skin.SmallBold : skin.SmallMuted;
                var line = FishCard.Fit(stat.Sub, style, w);
                UiSkin.ColoredLabel(new Rect(x, r.y + 72f, w, UiSkin.SmallLine), line, style, stat.Highlight ? UiSkin.Gold : UiSkin.Muted);
            }
        }

        /// <summary>One bar split by rarity (it grows at the opening) and the legend under it.</summary>
        private static float DrawRarities(UiSkin skin, float x, float y, float w, float ease)
        {
            var track = new Rect(x, y, w, RarityBar);
            GUI.DrawTexture(track, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.FieldColor, 0, 0f);

            // Every rarity keeps at least 8 px, so one Lendário among hundreds of Comuns is still seen.
            const float minimum = 8f, seam = 2f;
            var free = Mathf.Max(0f, w - Rarities.Count * minimum - (Rarities.Count - 1) * seam);
            var sx = x;
            for (var i = 0; i < Rarities.Count; i++)
            {
                var r = Rarities[i];
                var sw = (minimum + free * r.Amount / Mathf.Max(1f, _rarityTotal)) * ease;
                // Square pixel segments (A-184).
                GUI.DrawTexture(UiSkin.Snap(new Rect(sx, y, sw, RarityBar)), skin.White, ScaleMode.StretchToFill, true, 0, r.Color, 0, 0f);
                sx += sw + seam * ease;
            }

            y += RarityBar + 8f;
            var lx = x;
            foreach (var r in Rarities)
            {
                var lw = LegendWidth(r);
                if (lx > x && lx + lw > x + w)
                {
                    lx = x;
                    y += LegendRow;
                }

                GUI.DrawTexture(new Rect(lx, y + 7f, 10f, 10f), skin.White, ScaleMode.StretchToFill, true, 0, r.Color, 0, 0f);
                GUI.Label(new Rect(lx + 16f, y, r.NameWidth + 2f, UiSkin.SmallLine), r.Name, skin.SmallMuted);
                GUI.Label(new Rect(lx + 16f + r.NameWidth + 6f, y, r.CountWidth + 2f, UiSkin.SmallLine), r.Count, skin.SmallBold);
                lx += lw + 24f;
            }

            return y + LegendRow;
        }

        /// <summary>
        /// A fragment line (M25-T24: a line, not a card): the fragment's icon, its name over what it is, the progress bar
        /// with "12/25" and, on the right, how many came ("+3") in gold.
        /// </summary>
        private static void DrawGear(UiSkin skin, Rect r, Gear gear, float ease)
        {
            var icon = new Rect(r.x, r.y + (r.height - 40f) / 2f, 40f, 40f);
            var art = ArtAssets.Texture(gear.Art);
            if (art != null)
            {
                PixelDraw.Fit(icon, art);
            }
            else
            {
                skin.DrawIcon(new Rect(icon.x + 8f, icon.y + 8f, 24f, 24f), gear.Icon, UiSkin.Accent);
            }

            var gainedWidth = Width(skin.Heading, gear.Gained) + 4f;
            UiSkin.ColoredLabel(new Rect(r.xMax - gainedWidth, r.y + (r.height - 28f) / 2f, gainedWidth, 28f), gear.Gained, skin.Heading, UiSkin.Gold);

            // The progress column: the bar and "12/25" (or the "Completo!" tag), 200 wide, left of the gain.
            const float progressWidth = 200f;
            var progressX = r.xMax - gainedWidth - Gap - progressWidth;
            if (gear.Tag != null)
            {
                var tw = Width(skin.Badge, gear.Tag) + 12f;
                skin.Tag(new Rect(progressX + progressWidth - tw, r.y + (r.height - 22f) / 2f, tw, 22f), gear.Tag, UiSkin.Gold);
            }
            else if (gear.Fill >= 0f)
            {
                var countWidth = gear.Count != null ? Width(skin.SmallMuted, gear.Count) + 4f : 0f;
                skin.Bar(new Rect(progressX, r.y + (r.height - 8f) / 2f, progressWidth - countWidth - 8f, 8f), gear.Fill * ease, gear.KindGold ? UiSkin.Gold : UiSkin.Accent);
                if (gear.Count != null)
                {
                    GUI.Label(new Rect(progressX + progressWidth - countWidth, r.y + (r.height - UiSkin.SmallLine) / 2f, countWidth, UiSkin.SmallLine), gear.Count, skin.SmallMuted);
                }
            }

            var x = icon.xMax + 16f;
            var w = Mathf.Max(40f, progressX - 16f - x);
            GUI.Label(new Rect(x, r.y + 2f, w, 28f), FishCard.Fit(gear.Name, skin.BodyBold, w), skin.BodyBold);
            var kind = FishCard.Fit(gear.Kind, gear.KindGold ? skin.SmallBold : skin.SmallMuted, w);
            UiSkin.ColoredLabel(new Rect(x, r.y + 30f, w, UiSkin.SmallLine), kind, gear.KindGold ? skin.SmallBold : skin.SmallMuted, gear.KindGold ? UiSkin.Gold : UiSkin.Muted);
        }

        /// <summary>
        /// A warning line: an icon and the text, and the box's fill bar on the right. The Fishing Box opens from the footer
        /// button (M25-T24: no second "Abrir" here).
        /// </summary>
        private static void DrawWarning(UiSkin skin, Rect r, Warning warning)
        {
            skin.DrawIcon(new Rect(r.x + 4f, r.y + (r.height - 24f) / 2f, 24f, 24f), warning.Icon, warning.Strong ? UiSkin.Gold : UiSkin.Muted);
            var right = r.xMax;
            if (warning.Fill >= 0f)
            {
                skin.Bar(new Rect(right - 160f, r.y + (r.height - 8f) / 2f, 160f, 8f), warning.Fill, warning.Fill >= 1f ? UiSkin.Danger : UiSkin.Gold);
                right -= 160f + Gap;
            }

            var x = r.x + 40f;
            var style = warning.Strong ? skin.SmallBold : skin.SmallMuted;
            GUI.Label(new Rect(x, r.y + (r.height - UiSkin.SmallLine) / 2f, right - x, UiSkin.SmallLine), FishCard.Fit(warning.Text, style, right - x), style);
        }

        private static void Heading(UiSkin skin, float x, float y, float w, string title, string note)
        {
            var tw = Width(skin.Heading, title) + 4f;
            GUI.Label(new Rect(x, y, tw, 28f), title, skin.Heading);
            if (!string.IsNullOrEmpty(note))
            {
                UiSkin.ColoredLabel(new Rect(x + tw + 16f, y + 2f, w - tw - 16f, UiSkin.SmallLine), FishCard.Fit(note, skin.SmallBold, w - tw - 16f), skin.SmallBold, UiSkin.Gold);
            }
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>A fill in the ARC skin: a pixel shape in place of the old rounded rect (UiSkin.ArcFill, A-188).</summary>
        private static void Fill(UiSkin skin, Rect rect, Color color, float radius)
        {
            skin.ArcFill(rect, color, radius);
        }

        /// <summary>A footer button's width: the icon, the label in the button font and the padding (IconButton's layout).</summary>
        private static float ButtonWidth(UiSkin skin, string label)
        {
            return Mathf.Ceil(16f + 22f + 8f + Width(skin.Button, label) - skin.Button.padding.horizontal + 24f);
        }

        private static float Width(GUIStyle style, string text)
        {
            Measure.text = text;
            return style.CalcSize(Measure).x;
        }
    }
}
