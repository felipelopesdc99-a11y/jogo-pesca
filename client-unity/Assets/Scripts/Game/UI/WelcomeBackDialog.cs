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
    /// (A-175): a scene strip with the time away, big number cards (fish, sale value, XP with the levels, Conchas), the
    /// catches by rarity, the highlights, the fragments with their progress and the useful warnings, on the ARC
    /// panel. Every number comes from the service's <see cref="OfflineReport"/>; the screen only lays them out. What it
    /// shows is built once per report; only the short count-up at the opening formats numbers frame by frame.
    /// </summary>
    public static class WelcomeBackDialog
    {
        // Layout on the HUD's 1080-tall canvas (A-149): the frame is 920 wide (content 864) on every screen, ~1200×600 and
        // ~1320×840 alike; only its height follows the report, up to the canvas less 120.
        private const float MaxWidth = 920f, Gap = 12f, SectionGap = 18f, HeadingHeight = 28f;
        private const float StatHeight = 100f, BestHeight = 184f, GearHeight = 68f, WarningHeight = 34f, FooterHeight = 52f;
        private const float SceneHeight = 124f, RarityBar = 14f, LegendRow = 24f;

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
            public bool Strong, OpensBox;
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

            var footer = WindowFrame.Footer(skin, content, FooterHeight);
            var buttonY = footer.y + (footer.height - 44f) / 2f;
            if (skin.IconButton(new Rect(footer.xMax - 200f, buttonY, 200f, 44f), Icons.Play, GameTexts.Offline.Continue, skin.ButtonPrimary))
            {
                closed = true;
            }

            if (report.FishingBoxCount > 0 && skin.IconButton(new Rect(footer.xMax - 200f - Gap - 280f, buttonY, 280f, 44f), Icons.Box, GameTexts.Offline.OpenBox, skin.Button))
            {
                openBox = true;
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
                         .ThenByDescending(c => c.SalePriceCoins).Take(4))
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
                    Bar = (float)c.SizePercentile,
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
                    OpensBox = true,
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
            var h = sceneHeight + 12f;
            if (Stats.Count > 0)
            {
                h += SectionGap + StatHeight;
            }

            if (Rarities.Count > 0)
            {
                h += SectionGap + HeadingHeight + RarityBar + 10f + LegendRows(width) * LegendRow;
            }

            if (Best.Count > 0)
            {
                h += SectionGap + HeadingHeight + BestHeight;
            }

            if (Gears.Count > 0 || _clothingLevels != null)
            {
                var rows = (Gears.Count + GearColumns(width) - 1) / GearColumns(width);
                h += SectionGap + HeadingHeight + rows * (GearHeight + Gap) + (_clothingLevels != null ? 24f : 0f);
            }

            if (Warnings.Count > 0)
            {
                h += SectionGap + HeadingHeight + Warnings.Count * WarningHeight;
            }

            Measure.text = _note;
            return h + SectionGap + skin.SmallMuted.CalcHeight(Measure, width) + 4f;
        }

        private static int GearColumns(float width) => width >= 600f ? 2 : 1;

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

                x += w + 18f;
            }

            return rows;
        }

        private static float LegendWidth(RarityEntry r) => 16f + r.NameWidth + 6f + r.CountWidth;

        // ------------------------------------------------------------------ drawing

        private static bool DrawBody(UiSkin skin, OfflineReport report, float x, float y, float w, float sceneHeight, float ease)
        {
            var openBox = false;
            DrawScene(skin, new Rect(x + 6f, y + 6f, w - 12f, sceneHeight - 6f));
            y += sceneHeight + 12f;

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
                var cw = (w - Gap * 3f) / 4f;
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
                var columns = GearColumns(w);
                var gw = (w - Gap * (columns - 1)) / columns;
                for (var i = 0; i < Gears.Count; i++)
                {
                    DrawGear(skin, new Rect(x + (i % columns) * (gw + Gap), y + (i / columns) * (GearHeight + Gap), gw, GearHeight), Gears[i], ease);
                }

                y += (Gears.Count + columns - 1) / columns * (GearHeight + Gap);
                if (_clothingLevels != null)
                {
                    UiSkin.ColoredLabel(new Rect(x, y, w, 22f), FishCard.Fit(_clothingLevels, skin.SmallBold, w), skin.SmallBold, UiSkin.Gold);
                    y += 24f;
                }
            }

            if (Warnings.Count > 0)
            {
                y += SectionGap;
                Heading(skin, x, y, w, GameTexts.Offline.Warnings, null);
                y += HeadingHeight;
                foreach (var warning in Warnings)
                {
                    openBox |= DrawWarning(skin, new Rect(x, y, w, WarningHeight - 4f), warning);
                    y += WarningHeight;
                }
            }

            y += SectionGap;
            Measure.text = _note;
            GUI.Label(new Rect(x, y, w, skin.SmallMuted.CalcHeight(Measure, w) + 4f), _note, skin.SmallMuted);
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
                const float titleHeight = 40f;
                var top = r.y + (r.height - titleHeight - 24f) / 2f;
                UiSkin.ShadowLabel(new Rect(r.x + 22f, top, r.width - 44f, titleHeight), FishCard.Fit(_sceneTitle, skin.Title, r.width - 44f), skin.Title);
                UiSkin.ShadowLabel(new Rect(r.x + 22f, top + titleHeight, r.width - 44f, 24f), FishCard.Fit(_sceneLine, skin.Small, r.width - 44f), skin.Small);
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

        /// <summary>A big number card: icon and label, the number (counting up at the opening) and a short line under it.</summary>
        private static void DrawStat(UiSkin skin, Rect r, Stat stat, float ease)
        {
            if (stat.Highlight)
            {
                skin.DrawGlow(r, UiSkin.Gold, 0.35f + 0.08f * Mathf.Sin(Time.unscaledTime * 2.2f));
            }

            Card(skin, r, stat.Highlight ? UiSkin.Gold : UiSkin.FrameDark, stat.Highlight ? 2f : 1f);
            var x = r.x + 14f;
            var w = r.width - 28f;
            var icon = new Rect(x, r.y + 12f, 24f, 24f);
            if (stat.Currency)
            {
                skin.CurrencyIcon(icon, stat.Icon);
            }
            else
            {
                skin.DrawIcon(icon, stat.Icon, UiSkin.Accent);
            }

            GUI.Label(new Rect(x + 30f, r.y + 12f, w - 30f, 24f), FishCard.Fit(stat.Label, skin.SmallMuted, w - 30f), skin.SmallMuted);

            var text = ease >= 1f ? stat.Final : Format.Number((long)(stat.Value * (double)ease));
            if (Width(skin.NumberBig, stat.Final) > w)
            {
                text = ease >= 1f ? stat.FinalShort : Format.Short((long)(stat.Value * (double)ease));
            }

            GUI.Label(new Rect(x, r.y + 36f, w, 38f), text, skin.NumberBig);
            if (!string.IsNullOrEmpty(stat.Sub))
            {
                var style = stat.Highlight ? skin.SmallBold : skin.SmallMuted;
                var line = FishCard.Fit(stat.Sub, style, w);
                if (stat.Highlight)
                {
                    UiSkin.ColoredLabel(new Rect(x, r.y + 74f, w, 22f), line, style, UiSkin.Gold);
                }
                else
                {
                    GUI.Label(new Rect(x, r.y + 74f, w, 22f), line, style);
                }
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

            y += RarityBar + 10f;
            var lx = x;
            foreach (var r in Rarities)
            {
                var lw = LegendWidth(r);
                if (lx > x && lx + lw > x + w)
                {
                    lx = x;
                    y += LegendRow;
                }

                GUI.DrawTexture(new Rect(lx, y + 6f, 10f, 10f), skin.White, ScaleMode.StretchToFill, true, 0, r.Color, 0, 0f);
                GUI.Label(new Rect(lx + 16f, y, r.NameWidth + 2f, 22f), r.Name, skin.SmallMuted);
                GUI.Label(new Rect(lx + 16f + r.NameWidth + 6f, y, r.CountWidth + 2f, 22f), r.Count, skin.SmallBold);
                lx += lw + 18f;
            }

            return y + LegendRow;
        }

        /// <summary>A fragment card: the fragment's icon, what it is, how many came and the progress until it is assembled.</summary>
        private static void DrawGear(UiSkin skin, Rect r, Gear gear, float ease)
        {
            Card(skin, r, gear.Tag != null ? UiSkin.Gold : UiSkin.FrameDark, gear.Tag != null ? 2f : 1f);
            var icon = new Rect(r.x + 10f, r.y + (r.height - 46f) / 2f, 46f, 46f);
            skin.LineBox(icon, UiSkin.FieldColor, UiSkin.FrameDark, 1);
            var art = ArtAssets.Texture(gear.Art);
            if (art != null)
            {
                PixelDraw.Fit(new Rect(icon.x + 3f, icon.y + 3f, icon.width - 6f, icon.height - 6f), art);
            }
            else
            {
                skin.DrawIcon(new Rect(icon.x + 10f, icon.y + 10f, 26f, 26f), gear.Icon, UiSkin.Accent);
            }

            var x = icon.xMax + 12f;
            var w = r.xMax - 12f - x;

            // Right column: what came (+N), or the "montado" tag.
            var gainedWidth = Width(skin.Heading, gear.Gained) + 4f;
            UiSkin.ColoredLabel(new Rect(r.xMax - 12f - gainedWidth, r.y + 6f, gainedWidth, 26f), gear.Gained, skin.Heading, UiSkin.Gold);
            GUI.Label(new Rect(x, r.y + 8f, w - gainedWidth - 8f, 22f), FishCard.Fit(gear.Name, skin.BodyBold, w - gainedWidth - 8f), skin.BodyBold);

            var countWidth = 0f;
            if (gear.Tag != null)
            {
                countWidth = Width(skin.Badge, gear.Tag) + 10f;
                skin.Tag(new Rect(r.xMax - 12f - countWidth, r.y + 32f, countWidth, 20f), gear.Tag, UiSkin.Gold);
            }
            else if (gear.Count != null)
            {
                countWidth = Width(skin.SmallMuted, gear.Count) + 4f;
                GUI.Label(new Rect(r.xMax - 12f - countWidth, r.y + 30f, countWidth, 22f), gear.Count, skin.SmallMuted);
            }

            var kindWidth = w - countWidth - 8f;
            var kind = FishCard.Fit(gear.Kind, skin.SmallMuted, kindWidth);
            if (gear.KindGold)
            {
                UiSkin.ColoredLabel(new Rect(x, r.y + 30f, kindWidth, 22f), kind, skin.SmallBold, UiSkin.Gold);
            }
            else
            {
                GUI.Label(new Rect(x, r.y + 30f, kindWidth, 22f), kind, skin.SmallMuted);
            }

            if (gear.Fill >= 0f)
            {
                skin.Bar(new Rect(x, r.y + 54f, w, 7f), gear.Fill * ease, gear.Tag != null || gear.KindGold ? UiSkin.Gold : UiSkin.Accent);
            }
        }

        /// <summary>A warning line: an icon and the text; the box's one has its fill bar and a button to open it.</summary>
        private static bool DrawWarning(UiSkin skin, Rect r, Warning warning)
        {
            if (warning.Strong)
            {
                skin.LineBox(r, UiSkin.FieldColor, Color.clear, 1);
            }

            skin.DrawIcon(new Rect(r.x + 8f, r.y + (r.height - 20f) / 2f, 20f, 20f), warning.Icon, warning.Strong ? UiSkin.Gold : UiSkin.Muted);
            var right = r.xMax - 8f;
            var clicked = false;
            if (warning.OpensBox)
            {
                var bw = Mathf.Max(120f, Width(skin.SmallBold, GameTexts.Offline.OpenBoxShort) + 28f);
                clicked = GUI.Button(new Rect(right - bw, r.y + 2f, bw, r.height - 4f), GameTexts.Offline.OpenBoxShort, skin.Chip);
                right -= bw + 12f;
                if (warning.Fill >= 0f)
                {
                    skin.Bar(new Rect(right - 120f, r.y + (r.height - 8f) / 2f, 120f, 8f), warning.Fill, warning.Fill >= 1f ? UiSkin.Danger : UiSkin.Gold);
                    right -= 132f;
                }
            }

            var x = r.x + 36f;
            var style = warning.Strong ? skin.SmallBold : skin.SmallMuted;
            GUI.Label(new Rect(x, r.y + (r.height - 22f) / 2f, right - x, 22f), FishCard.Fit(warning.Text, style, right - x), style);
            return clicked;
        }

        private static void Heading(UiSkin skin, float x, float y, float w, string title, string note)
        {
            var tw = Width(skin.Heading, title) + 4f;
            GUI.Label(new Rect(x, y, tw, 24f), title, skin.Heading);
            if (!string.IsNullOrEmpty(note))
            {
                UiSkin.ColoredLabel(new Rect(x + tw + 12f, y + 2f, w - tw - 12f, 22f), FishCard.Fit(note, skin.SmallBold, w - tw - 12f), skin.SmallBold, UiSkin.Gold);
            }
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>
        /// An ARC card (A-184): the card colour with a one-pixel line in <paramref name="border"/> and the card's inner dark
        /// line; a <paramref name="borderWidth"/> of 2 or more (a highlight) adds a second line in the same colour.
        /// </summary>
        private static void Card(UiSkin skin, Rect r, Color border, float borderWidth)
        {
            var inner = borderWidth >= 2f ? border : VisualTheme.Current.CardLine;
            skin.Shape(r, 1, false, UiSkin.CardColor, 2, border, inner, Color.clear);
        }

        /// <summary>A fill in the ARC skin: a pixel shape in place of the old rounded rect (UiSkin.ArcFill, A-188).</summary>
        private static void Fill(UiSkin skin, Rect rect, Color color, float radius)
        {
            skin.ArcFill(rect, color, radius);
        }

        private static float Width(GUIStyle style, string text)
        {
            Measure.text = text;
            return style.CalcSize(Measure).x;
        }
    }
}
