using System.Collections.Generic;
using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Scene;
using FishingIdle.Game.Visual;
using FishingIdle.GameService.Clans;
using FishingIdle.GameService.Core;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The "Clã" window (M24-T49, addendum A-174), a sub-tab of Perfil, on a "Diário de bordo" page (A-170) with the "i".
    /// Below the Fisher Nv.100 it shows the requirement; without a clan, "Criar um clã" (name and the price in Dólares) and the
    /// note that the list of clans to join comes with the online game; in a clan, the members and places ("Comprar vaga" for
    /// the owner), the clan's Cardume with the player's fish (choose, change, remove) and the clan's Strength. Always the daily
    /// ranking block with the top 3 rewards and the notice that they turn on with the online game.
    /// </summary>
    /// <remarks>
    /// Shows what the clan service returned (GameRoot.Clan); decides nothing. Icon ico_cla (received 13/10/2026; the
    /// Honra icon only if the file is missing). Since A-179 the clan column opens with the night harbour (cla_fundo) and the
    /// crest (cla_brasao) as a photo on the sheet; without the files, the plain sunken panels. M25-T24 (A-191): the harbour
    /// only as the header strip, the sections separated by space (no sunken panel around them: window, then the row cards),
    /// and the online notice as one line of soft text.
    /// </remarks>
    public sealed class ClanWindow
    {
        // Grid of 8 (A-190): 16 between elements, 32 between the sections and the two columns.
        private const float Gap = 16f, SectionGap = 32f, Pad = 16f, MemberRow = 64f, ScrollBar = 18f, PickRow = 64f;

        /// <summary>The clan icon (ico_cla.png, received 13/10/2026; the Honra icon if the file is missing).</summary>
        private const string ClanIcon = "cla";

        // A-179: the harbour at night (cla_fundo) as a photo on top of the clan column, with the crest (cla_brasao) on
        // its left; in a clan, the name goes on the crest's ribbon too. Missing files = the plain sunken panels.
        private const string SceneArt = "UI/cla_fundo", CrestArt = "UI/cla_brasao";
        private const float SceneHeight = 160f, SceneGap = 32f;

        /// <summary>
        /// The blank ribbon of the crest, in the picture's pixels: the pixel crest (ArtePixel, 51×48, M25-T15), or the old
        /// painted one (512×512) when the pixel file is missing.
        /// </summary>
        private static readonly Rect Ribbon = new Rect(10f, 34f, 30f, 6f), RibbonPainted = new Rect(116f, 390f, 280f, 44f);

        private static GUIStyle _ribbon;

        private readonly GameRoot _root;
        private string _nameDraft = string.Empty;
        private Vector2 _membersScroll;
        private Vector2 _pickScroll;
        private bool _picking;
        private bool _confirmLeave;
        private List<ClanFishView> _candidates;

        public ClanWindow(GameRoot root)
        {
            _root = root;
        }

        public bool IsOpen { get; private set; }

        /// <summary>True while the fish picker or the "Sair do clã?" confirmation is open.</summary>
        public bool HasDialog => _picking || _confirmLeave;

        /// <summary>The clan icon: ico_cla when it exists, otherwise the Honra icon.</summary>
        public static string NavIcon => ArtAssets.Icon(ClanIcon) != null ? ClanIcon : Icons.Honor;

        public void Open()
        {
            IsOpen = true;
            _picking = false;
            _confirmLeave = false;
            _membersScroll = Vector2.zero;
        }

        /// <summary>Closes a dialog first, then the window.</summary>
        public void Close()
        {
            if (_picking || _confirmLeave)
            {
                _picking = false;
                _confirmLeave = false;
                return;
            }

            IsOpen = false;
        }

        public void Draw(UiSkin skin, float screenWidth, float screenHeight)
        {
            if (!IsOpen)
            {
                return;
            }

            var view = _root.Clan;
            var info = view == null ? null : GameTexts.Clan.Info(view.MinFisherLevel, Format.PercentShort(view.XpBonus), view.BaseMembers);
            var counter = view != null && view.Clan != null ? GameTexts.Clan.Counter(view.Clan.MemberCount, view.Clan.Capacity) : null;
            GUI.enabled = !HasDialog;
            var area = WindowFrame.Draw(skin, screenWidth, screenHeight, GameTexts.Clan.Title, info, out var closed, 1320f, 840f, NavIcon, counter);
            if (closed)
            {
                GUI.enabled = true;
                IsOpen = false;
                return;
            }

            if (view != null)
            {
                // Left: the clan (or how to have one); right: the daily ranking and the top 3.
                var leftWidth = Mathf.Floor(area.width * 0.58f);
                var left = new Rect(area.x, area.y, leftWidth, area.height);
                var right = new Rect(area.x + leftWidth + SectionGap, area.y, area.width - leftWidth - SectionGap, area.height);
                if (view.Clan != null)
                {
                    DrawClan(skin, left, view);
                }
                else if (!view.LevelAllows)
                {
                    DrawLevelRequired(skin, left, view);
                }
                else
                {
                    DrawNoClan(skin, left, view);
                }

                DrawRanking(skin, right, view);
            }

            GUI.enabled = true;
            if (_picking)
            {
                DrawPicker(skin, screenWidth, screenHeight, view);
            }
            else if (_confirmLeave)
            {
                DrawLeaveDialog(skin, screenWidth, screenHeight, view);
            }
        }

        // ------------------------------------------------------------------ without a clan

        private static void DrawLevelRequired(UiSkin skin, Rect rect, ClanView view)
        {
            rect = SceneOnTop(skin, rect, null);
            var x = rect.x;
            var w = rect.width;
            var y = rect.y;
            skin.DrawIconExact(new Rect(x, y, 40f, 40f), Icons.Lock, UiSkin.Muted);
            GUI.Label(new Rect(x + 52f, y + 4f, w - 52f, 32f), GameTexts.Clan.LevelTitle(view.MinFisherLevel), skin.Heading);
            y += 56f;
            GUI.Label(new Rect(x, y, w, 48f), GameTexts.Clan.LevelRequired(view.MinFisherLevel, view.FisherLevel), skin.Body);
            y += 56f;
            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), GameTexts.Clan.LevelProgress(view.FisherLevel, view.MinFisherLevel), skin.SmallMuted);
            y += 26f;
            skin.Bar(new Rect(x, y, w, 12f), Mathf.Clamp01(view.FisherLevel / (float)Mathf.Max(1, view.MinFisherLevel)), true);
            y += 32f;
            GUI.Label(new Rect(x, y, w, 44f), GameTexts.Clan.BonusLine(Format.PercentShort(view.XpBonus)), skin.Small);
        }

        private void DrawNoClan(UiSkin skin, Rect rect, ClanView view)
        {
            rect = SceneOnTop(skin, rect, null);
            var x = rect.x;
            var w = rect.width;
            var y = rect.y;
            GUI.Label(new Rect(x, y, w, 32f), GameTexts.Clan.NoClanTitle, skin.Heading);
            y += 38f;
            GUI.Label(new Rect(x, y, w, 44f), GameTexts.Clan.BonusLine(Format.PercentShort(view.XpBonus)), skin.Small);
            y += 52f;

            // Create a clan: the name, its rule and the price in Dólares.
            GUI.Label(new Rect(x, y, w, 28f), GameTexts.Clan.CreateTitle, skin.BodyBold);
            y += 32f;
            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), GameTexts.Clan.NameLabel, skin.SmallMuted);
            y += 24f;
            _nameDraft = GUI.TextField(new Rect(x, y, Mathf.Min(w, 480f), 40f), _nameDraft ?? string.Empty, Mathf.Max(1, view.NameMaxLength), skin.SearchField);
            y += 48f;
            GUI.Label(new Rect(x, y, w, 48f), GameTexts.Clan.NameRule(view.NameMinLength, view.NameMaxLength), skin.SmallMuted);
            y += 56f;

            var canCreate = view.CreateBlocker == ServiceError.None;
            var createLabel = view.CreateCostDollars == 0 ? GameTexts.Clan.CreateButtonFree : GameTexts.Clan.CreateButton(Format.Number(view.CreateCostDollars));
            var button = new Rect(x, y, Mathf.Min(w, Mathf.Ceil(70f + skin.Button.CalcSize(new GUIContent(createLabel)).x - skin.Button.padding.horizontal)), 48f);
            var enabled = GUI.enabled;
            GUI.enabled = enabled && canCreate;
            if (skin.IconButton(button, Icons.Dollar, createLabel, canCreate ? skin.ButtonPrimary : skin.Button))
            {
                if (_root.CreateClan(_nameDraft))
                {
                    _nameDraft = string.Empty;
                }
            }

            GUI.enabled = enabled;
            var style = canCreate ? skin.SmallGold : skin.SmallMuted;
            var below = view.CreateCostDollars == 0 ? GameTexts.Clan.CreateFreeNote : GameTexts.Clan.YourDollars(Format.Number(view.Dollars));
            GUI.Label(new Rect(x, button.yMax + 8f, w, UiSkin.SmallLine), FishCard.Fit(below, style, w), style);
            y = button.yMax + 8f + UiSkin.SmallLine + SectionGap;

            // Join a clan: the list comes from the server (TD-055). One heading and one soft line, no divider.
            GUI.Label(new Rect(x, y, w, 28f), GameTexts.Clan.JoinTitle, skin.Heading);
            y += 32f;
            GUI.Label(new Rect(x, y, w, 48f), GameTexts.Clan.JoinOnline, skin.SmallMuted);
        }

        // ------------------------------------------------------------------ the clan

        private void DrawClan(UiSkin skin, Rect rect, ClanView view)
        {
            var clan = view.Clan;

            // Summary: name, owner, Strength, the XP bonus, and the actions. With the art, it is the night harbour with
            // the crest on the left and the text in night colours over a veil (A-179).
            var onArt = ArtAssets.Texture(SceneArt) != null;
            var summaryHeight = onArt ? SceneHeight : 132f;
            var summary = new Rect(rect.x, rect.y, rect.width, summaryHeight);
            var crestWidth = 0f;
            if (onArt)
            {
                crestWidth = Scene(skin, summary, clan.Name);
                WindowFrame.Veil(skin, new Rect(summary.x + crestWidth, summary.y, summary.width - crestWidth, summary.height), 0.68f, 0.68f);
            }
            else
            {
                skin.Inset(summary);
            }

            // The summary's text and actions on the photo.
            {
                var x = summary.x + Pad + crestWidth;
                var w = summary.width - Pad * 2f - crestWidth;
                var top = onArt ? summary.y + (summaryHeight - 132f) / 2f : summary.y;
                var actionsWidth = Mathf.Min(300f, w * 0.46f);
                var textWidth = w - actionsWidth - Gap;
                GUI.Label(new Rect(x, top + 12f, textWidth, 32f), FishCard.Fit(clan.Name, skin.Heading, textWidth), skin.Heading);
                GUI.Label(new Rect(x, top + 46f, textWidth, UiSkin.SmallLine), FishCard.Fit(GameTexts.Clan.OwnerLine(clan.OwnerName ?? string.Empty), skin.SmallMuted, textWidth), skin.SmallMuted);
                GUI.Label(new Rect(x, top + 70f, textWidth, UiSkin.SmallLine), GameTexts.Clan.Strength + ": " + Format.Number(clan.Strength), skin.SmallBold);
                if (view.XpBonusActive)
                {
                    GUI.Label(new Rect(x, top + 96f, textWidth, UiSkin.SmallLine), FishCard.Fit(GameTexts.Clan.BonusActive(Format.PercentShort(view.XpBonus)), skin.SmallGoldLine, textWidth), skin.SmallGoldLine);
                }

                var ax = summary.xMax - Pad - actionsWidth;
                var buy = new Rect(ax, top + 16f, actionsWidth, 48f);
                var canBuy = clan.BuySlotBlocker == ServiceError.None;
                var enabled = GUI.enabled;
                if (clan.NextSlotPriceDollars > 0 && clan.IsOwner)
                {
                    GUI.enabled = enabled && canBuy;
                    // The short label when the long one does not fit the button with its icon (measured, A-191).
                    var buyStyle = canBuy ? skin.ButtonPrimary : skin.Button;
                    var buyLabel = GameTexts.Clan.BuySlot(Format.Number(clan.NextSlotPriceDollars));
                    var buyIcon = Icons.Add;
                    if (buyStyle.CalcSize(new GUIContent(buyLabel)).x - buyStyle.padding.horizontal + 46f > buy.width - 16f)
                    {
                        // The short one: the Dólar icon and "Vaga · 120" (icon + number, A-190 rule 7).
                        buyLabel = GameTexts.Clan.BuySlotShort(Format.Number(clan.NextSlotPriceDollars));
                        buyIcon = Icons.Dollar;
                    }

                    if (skin.IconButton(buy, buyIcon, buyLabel, buyStyle))
                    {
                        _root.BuyClanSlot();
                    }

                    GUI.enabled = enabled;
                }
                else
                {
                    var note = clan.IsOwner ? GameTexts.Clan.SlotsMaxed : GameTexts.Clan.OnlyOwnerBuys;
                    GUI.Label(new Rect(ax, top + 16f, actionsWidth, 48f), note, skin.SmallMuted);
                }

                if (GUI.Button(new Rect(ax, top + 72f, actionsWidth, 40f), GameTexts.Clan.Leave, skin.ButtonDanger))
                {
                    _confirmLeave = true;
                }
            }

            // The clan's Cardume: the player's fish.
            const float fishHeight = 120f;
            var fishRect = new Rect(rect.x, summary.yMax + SectionGap, rect.width, fishHeight);
            DrawMyFish(skin, fishRect, clan);

            // Members and places.
            var membersRect = new Rect(rect.x, fishRect.yMax + SectionGap, rect.width, rect.yMax - fishRect.yMax - SectionGap);
            DrawMembers(skin, membersRect, clan);
        }

        /// <summary>
        /// Without a clan: the night harbour with the crest as a photo on top of <paramref name="rect"/> (A-179); returns
        /// the room left under it, or <paramref name="rect"/> itself when the art is missing.
        /// </summary>
        private static Rect SceneOnTop(UiSkin skin, Rect rect, string name)
        {
            var scene = new Rect(rect.x, rect.y, rect.width, SceneHeight);
            if (ArtAssets.Texture(SceneArt) == null)
            {
                return rect;
            }

            Scene(skin, scene, name);
            return new Rect(rect.x, scene.yMax + SceneGap, rect.width, rect.height - SceneHeight - SceneGap);
        }

        /// <summary>
        /// The night harbour as a photo filling <paramref name="rect"/> and the crest on its left, with <paramref name="name"/>
        /// (if any) on the ribbon when it fits. Returns the width the crest takes from the left edge (0 without the crest).
        /// </summary>
        private static float Scene(UiSkin skin, Rect rect, string name)
        {
            // The pixel harbour at a whole scale, cut at the edges, centred on the moonlit water (M25-T15).
            if (WindowFrame.PhotoArt(skin, rect, SceneArt, 0.5f, 0.55f) == null)
            {
                return 0f;
            }

            var crest = ArtAssets.Texture(CrestArt);
            if (crest == null)
            {
                return 0f;
            }

            var size = rect.height - 12f;
            var crestRect = new Rect(rect.x + 14f, rect.y + 6f, size, size);

            // The crest at its largest whole scale (PixelDraw); the ribbon is placed on the rect it really takes.
            Texture drawnTex = crest;
            var drawn = PixelDraw.FitRect(crestRect, ref drawnTex);
            PixelDraw.Fit(crestRect, crest);
            if (!string.IsNullOrEmpty(name))
            {
                if (_ribbon == null || _ribbon.font != skin.SmallBold.font)
                {
                    // Fixed dark ink (not tracked: the ribbon is light in both modes).
                    _ribbon = new GUIStyle(skin.SmallBold) { fontSize = UiSkin.ChipSize, alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Clip };
                    _ribbon.normal.textColor = UiSkin.DarkInk;
                }

                // The ribbon is light: dark ink on it, only when the whole name fits (the full name is beside the crest).
                var k = drawn.width / drawnTex.width;
                var band = ArtAssets.IsPixel(crest) ? Ribbon : RibbonPainted;
                var ribbon = new Rect(drawn.x + band.x * k, drawn.y + band.y * k, band.width * k, band.height * k);
                if (_ribbon.CalcSize(new GUIContent(name)).x <= ribbon.width)
                {
                    GUI.Label(new Rect(ribbon.x, ribbon.y - 3f, ribbon.width, ribbon.height + 6f), name, _ribbon);
                }
            }

            return size + 20f;
        }

        private void DrawMyFish(UiSkin skin, Rect rect, ClanInfoView clan)
        {
            var x = rect.x;
            var w = rect.width;
            GUI.Label(new Rect(x, rect.y, w, 28f), GameTexts.Clan.ClanCardume, skin.Heading);
            GUI.Label(new Rect(x, rect.y + 28f, w, UiSkin.SmallLine), FishCard.Fit(GameTexts.Clan.ClanCardumeHint, skin.SmallMuted, w), skin.SmallMuted);

            var fish = clan.MyFish;
            var buttonsWidth = fish != null ? 272f : Mathf.Ceil(70f + skin.Button.CalcSize(new GUIContent(GameTexts.Clan.ChooseFish)).x - skin.Button.padding.horizontal);
            var bx = rect.xMax - buttonsWidth;
            var textWidth = bx - Gap - x;
            var y = rect.y + 64f;
            if (fish == null)
            {
                GUI.Label(new Rect(x, y + 8f, textWidth, UiSkin.SmallLine), FishCard.Fit(GameTexts.Clan.NoFishYet, skin.Small, textWidth), skin.Small);
                if (skin.IconButton(new Rect(bx, y, buttonsWidth, 44f), Icons.Fish, GameTexts.Clan.ChooseFish, skin.ButtonPrimary))
                {
                    OpenPicker();
                }

                return;
            }

            var art = Art.FishTexture(fish.SpeciesId);
            if (art != null)
            {
                PixelDraw.Fit(new Rect(x, y - 4f, 72f, 52f), art);
            }

            var tx = x + 84f;
            var tw = textWidth - 84f;
            GUI.Label(new Rect(tx, y - 2f, tw, 24f), FishCard.Fit(GameTexts.Clan.MemberFish(fish.SpeciesName, fish.Level, Format.Number(fish.Strength)), skin.BodyBold, tw), skin.BodyBold);
            var where = fish.CardumePosition > 0 ? GameTexts.Clan.InYourCardume(fish.CardumePosition) : fish.SizeCategoryName;
            GUI.Label(new Rect(tx, y + 24f, tw, UiSkin.SmallLine), FishCard.Fit(where ?? string.Empty, skin.SmallMuted, tw), skin.SmallMuted);

            var half = (buttonsWidth - 8f) / 2f;
            if (skin.IconButton(new Rect(bx, y, half, 44f), Icons.Swap, GameTexts.Clan.ChangeFish, skin.Button))
            {
                OpenPicker();
            }

            if (GUI.Button(new Rect(bx + half + 8f, y, half, 44f), GameTexts.Clan.RemoveFish, skin.Button))
            {
                _root.ClearClanFish();
            }
        }

        private void DrawMembers(UiSkin skin, Rect rect, ClanInfoView clan)
        {
            var x = rect.x;
            var w = rect.width;
            // The count is already in the window's counter (A-190: no duplicate): "Membros" and the places on the right.
            GUI.Label(new Rect(x, rect.y, w * 0.4f, 28f), GameTexts.Clan.Members, skin.Heading);
            var slots = GameTexts.Clan.SlotsLine(clan.BaseMembers, clan.ExtraSlots, clan.MaxMembers);
            GUI.Label(new Rect(x + w * 0.4f, rect.y + 2f, w * 0.6f, UiSkin.SmallLine), FishCard.Fit(slots, skin.SmallMutedRightLine, w * 0.6f), skin.SmallMutedRightLine);

            var list = new Rect(rect.x, rect.y + 40f, rect.width, rect.height - 40f);
            var contentHeight = clan.Members.Count * MemberRow;
            var scrolls = contentHeight > list.height;
            var inner = list.width - (scrolls ? ScrollBar : 0f);
            _membersScroll = GUI.BeginScrollView(list, _membersScroll, new Rect(0f, 0f, inner, Mathf.Max(contentHeight, list.height - 1f)));
            for (var i = 0; i < clan.Members.Count; i++)
            {
                var m = clan.Members[i];
                var row = new Rect(0f, i * MemberRow, inner, MemberRow - 8f);
                GUI.Box(row, GUIContent.none, m.IsPlayer ? skin.CardSelected : skin.Card);
                var nameWidth = row.width * 0.42f;
                var name = m.Name + (m.IsPlayer ? " " + GameTexts.Clan.You : string.Empty);
                GUI.Label(new Rect(row.x + 16f, row.y + 2f, nameWidth, 28f), FishCard.Fit(name, skin.BodyBold, nameWidth), skin.BodyBold);
                var level = GameTexts.Clan.MemberLevel(m.FisherLevel) + (m.IsOwner ? " · " + GameTexts.Clan.Owner : string.Empty);
                GUI.Label(new Rect(row.x + 16f, row.y + 28f, nameWidth, UiSkin.SmallLine), FishCard.Fit(level, m.IsOwner ? skin.SmallGoldLine : skin.SmallMuted, nameWidth), m.IsOwner ? skin.SmallGoldLine : skin.SmallMuted);
                var fx = row.x + 16f + nameWidth + 16f;
                var fw = row.xMax - fx - 16f;
                var fishText = m.HasFish ? GameTexts.Clan.MemberFish(m.FishSpeciesName, m.FishLevel, Format.Number(m.FishStrength)) : GameTexts.Clan.NoFish;
                GUI.Label(new Rect(fx, row.y + 16f, fw, UiSkin.SmallLine), FishCard.Fit(fishText, m.HasFish ? skin.Small : skin.SmallMuted, fw), m.HasFish ? skin.Small : skin.SmallMuted);
            }

            GUI.EndScrollView();
        }

        // ------------------------------------------------------------------ the ranking and the top 3

        private static void DrawRanking(UiSkin skin, Rect rect, ClanView view)
        {
            var ranking = view.Ranking;
            var x = rect.x;
            var w = rect.width;
            var y = rect.y;
            GUI.Label(new Rect(x, y, w, 28f), FishCard.Fit(GameTexts.Clan.Ranking, skin.Heading, w), skin.Heading);
            y += 32f;
            if (ranking == null)
            {
                return;
            }

            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), FishCard.Fit(GameTexts.Clan.RankingReset(Format.Countdown(ranking.SecondsToReset)), skin.SmallMuted, w), skin.SmallMuted);
            y += 24f;

            // The online notice right under the heading, one line of soft text (M25-T24), not a gold box.
            if (!ranking.OnlineRewards)
            {
                skin.DrawIcon(new Rect(x, y + 2f, 20f, 20f), Icons.Lock, UiSkin.Muted);
                GUI.Label(new Rect(x + 28f, y, w - 28f, UiSkin.SmallLine), FishCard.Fit(GameTexts.Clan.RewardsOffline, skin.SmallMuted, w - 28f), skin.SmallMuted);
                y += 24f;
            }

            y += SectionGap;
            GUI.Label(new Rect(x, y, w, 28f), FishCard.Fit(GameTexts.Clan.RewardsTitle, skin.Heading, w), skin.Heading);
            y += 32f;
            foreach (var reward in ranking.Rewards)
            {
                var row = new Rect(x, y, w, 64f);
                GUI.Box(row, GUIContent.none, reward.Rank == 1 ? skin.CardImportant : skin.Card);
                GUI.Label(new Rect(row.x + 16f, row.y + 20f, 48f, UiSkin.SmallLine), GameTexts.Clan.Rank(reward.Rank), reward.Rank == 1 ? skin.SmallGold : skin.SmallBold);
                var items = WorldBossWindow.RewardItems(reward.Coins, reward.Shells, reward.ClothingFragments, reward.BoatFragments, reward.BoatName, reward.Dollars);
                // The prize list centred in the row, on one or two lines.
                var itemsHeight = Mathf.Min(48f, skin.Small.CalcHeight(new GUIContent(items), row.width - 88f));
                GUI.Label(new Rect(row.x + 72f, row.y + (row.height - itemsHeight) / 2f, row.width - 88f, itemsHeight + 4f), items, skin.Small);
                y += 72f;
            }

            y += Gap;
            if (!ranking.OnlineRewards)
            {
                // Off in the local game (TD-055): only the player's clan exists here (the notice is under the heading).
                return;
            }

            if (ranking.LastResult != null)
            {
                var r = ranking.LastResult;
                var last = r.IsEmpty
                    ? GameTexts.Clan.LastResultNoPrize(Format.Date(r.Date), r.Rank)
                    : GameTexts.Clan.LastResult(Format.Date(r.Date), r.Rank, WorldBossWindow.RewardItems(r.Coins, r.Shells, r.ClothingFragments, r.BoatFragments, r.BoatName, r.Dollars));
                GUI.Label(new Rect(x, y, w, 40f), last, skin.SmallMuted);
                y += 44f;
            }

            if (ranking.Lines.Count == 0)
            {
                GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), GameTexts.Clan.RankingEmpty, skin.SmallMuted);
                return;
            }

            foreach (var line in ranking.Lines)
            {
                if (y + UiSkin.SmallLine > rect.yMax - 8f)
                {
                    break;
                }

                var style = line.IsMine ? skin.SmallGoldLine : skin.Small;
                GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), FishCard.Fit(GameTexts.Clan.RankLine(line.Rank, line.Name, Format.Number(line.Strength), line.Members), style, w), style);
                y += 24f;
            }
        }

        // ------------------------------------------------------------------ dialogs

        private void OpenPicker()
        {
            _candidates = _root.ClanFishCandidates();
            _pickScroll = Vector2.zero;
            _picking = true;
        }

        private void DrawPicker(UiSkin skin, float screenWidth, float screenHeight, ClanView view)
        {
            var rect = WindowFrame.Dialog(skin, screenWidth, screenHeight, Mathf.Min(640f, screenHeight - 160f), 720f);
            var x = rect.x + 28f;
            var w = rect.width - 56f;
            GUI.Label(new Rect(x, rect.y + 20f, w, 30f), GameTexts.Clan.PickTitle, skin.Heading);
            GUI.Label(new Rect(x, rect.y + 54f, w, UiSkin.SmallLine), FishCard.Fit(GameTexts.Clan.PickHint, skin.SmallMuted, w), skin.SmallMuted);

            var list = new Rect(x, rect.y + 86f, w, rect.height - 86f - 76f);
            var fish = _candidates ?? new List<ClanFishView>();
            if (fish.Count == 0)
            {
                GUI.Label(new Rect(list.x, list.y + 8f, list.width, 44f), GameTexts.Clan.PickEmpty, skin.Small);
            }
            else
            {
                var contentHeight = fish.Count * PickRow;
                var scrolls = contentHeight > list.height;
                var inner = list.width - (scrolls ? ScrollBar : 0f);
                _pickScroll = GUI.BeginScrollView(list, _pickScroll, new Rect(0f, 0f, inner, Mathf.Max(contentHeight, list.height - 1f)));
                var current = view?.Clan?.MyFish?.FishId ?? 0;
                for (var i = 0; i < fish.Count; i++)
                {
                    var f = fish[i];
                    var row = new Rect(0f, i * PickRow, inner, PickRow - 6f);
                    GUI.Box(row, GUIContent.none, f.FishId == current ? skin.CardSelected : skin.Card);
                    var art = Art.FishTexture(f.SpeciesId);
                    if (art != null)
                    {
                        PixelDraw.Fit(new Rect(row.x + 10f, row.y + 6f, 64f, row.height - 12f), art);
                    }

                    var tw = row.width - 84f - 150f;
                    GUI.Label(new Rect(row.x + 84f, row.y + 6f, tw, 24f), FishCard.Fit(f.SpeciesName + " · " + GameTexts.Clan.StrengthValue(Format.Number(f.Strength)), skin.BodyBold, tw), skin.BodyBold);
                    var detail = GameTexts.Clan.PickLine(f.SizeCategoryName ?? string.Empty, f.Level) + (f.CardumePosition > 0 ? " · " + GameTexts.Clan.InYourCardume(f.CardumePosition) : string.Empty);
                    GUI.Label(new Rect(row.x + 84f, row.y + 30f, tw, UiSkin.SmallLine), FishCard.Fit(detail, skin.SmallMuted, tw), skin.SmallMuted);
                    var pick = new Rect(row.xMax - 140f, row.y + 9f, 128f, 40f);
                    if (f.FishId == current)
                    {
                        skin.AccentPill(new Rect(pick.x, pick.y + 6f, pick.width, 28f), GameTexts.Clan.Picked, UiSkin.Gold, Icons.Check);
                    }
                    else if (GUI.Button(pick, GameTexts.Clan.Pick, skin.ButtonPrimary))
                    {
                        _root.SetClanFish(f.FishId);
                        _picking = false;
                    }
                }

                GUI.EndScrollView();
            }

            if (GUI.Button(new Rect(rect.xMax - 28f - 160f, rect.yMax - 62f, 160f, 42f), GameTexts.Dialogs.Cancel, skin.Button))
            {
                _picking = false;
            }
        }

        private void DrawLeaveDialog(UiSkin skin, float screenWidth, float screenHeight, ClanView view)
        {
            var rect = WindowFrame.Dialog(skin, screenWidth, screenHeight, 250f, 620f);
            var x = rect.x + 28f;
            var w = rect.width - 56f;
            GUI.Label(new Rect(x, rect.y + 22f, w, 30f), GameTexts.Clan.LeaveTitle, skin.Heading);
            var clan = view?.Clan;
            var text = clan == null ? GameTexts.Clan.LeaveMember
                : clan.MemberCount <= 1 ? GameTexts.Clan.LeaveAlone
                : clan.IsOwner ? GameTexts.Clan.LeaveOwner + " " + GameTexts.Clan.LeaveMember
                : GameTexts.Clan.LeaveMember;
            GUI.Label(new Rect(x, rect.y + 64f, w, 90f), text, skin.Body);
            if (GUI.Button(new Rect(x, rect.yMax - 64f, 150f, 42f), GameTexts.Dialogs.Cancel, skin.Button))
            {
                _confirmLeave = false;
            }

            if (GUI.Button(new Rect(rect.xMax - 28f - 190f, rect.yMax - 64f, 190f, 42f), GameTexts.Clan.LeaveConfirm, skin.ButtonDanger))
            {
                _confirmLeave = false;
                _root.LeaveClan();
            }
        }
    }
}
