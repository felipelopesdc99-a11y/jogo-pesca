using FishingIdle.Game.Bootstrap;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Shop;
using FishingIdle.Game.Visual;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The Shop (GDD section 7): rods, and since V0.2 boats and baits (docs/SISTEMA_SUCESSO_PESCA.md).
    /// Prices, bonuses and requirements come from rods.json and equipment.json; on the left, the gear
    /// in use and the chance of pulling out each rarity, so the player sees what a purchase improves.
    /// </summary>
    public sealed class ShopWindow
    {
        public enum Tab
        {
            Rods,
            Boats,
            Baits,
            Vip,
        }

        private const float SummaryWidth = 330f;

        private readonly GameRoot _root;
        private ShopView _shop;
        private bool _dirty = true;
        private Tab _tab;
        private Vector2 _rodScroll;

        public ShopWindow(GameRoot root)
        {
            _root = root;
            _root.AquariumChanged += () => _dirty = true;
            _root.BoxChanged += () => _dirty = true;
        }

        public bool IsOpen { get; private set; }

        public void Open(Tab tab = Tab.Rods)
        {
            IsOpen = true;
            _dirty = true;
            _tab = tab;
        }

        public void Close() => IsOpen = false;

        /// <summary>The Shop has no confirmation dialog of its own.</summary>
        public bool HasDialog => false;

        public void Draw(UiSkin skin, float screenWidth, float screenHeight)
        {
            if (!IsOpen)
            {
                return;
            }

            if (_dirty)
            {
                _dirty = false;
                _shop = _root.GetShop();
            }

            var gear = _root.Gear;
            if (_shop == null || gear == null)
            {
                return;
            }

            var area = WindowFrame.Draw(skin, screenWidth, screenHeight, GameTexts.Shop.Title, GameTexts.Shop.Note, out var closed, 1200f, 730f, Icons.Shop);
            if (closed)
            {
                Close();
                return;
            }

            DrawSummary(skin, new Rect(area.x, area.y, SummaryWidth, area.height), gear);

            var right = new Rect(area.x + SummaryWidth + 24, area.y, area.width - SummaryWidth - 24, area.height);
            var tabs = new[] { (Tab.Rods, GameTexts.Gear.TabRods, Icons.Rod), (Tab.Boats, GameTexts.Gear.TabBoats, Icons.Boat), (Tab.Baits, GameTexts.Gear.TabBaits, Icons.Bait), (Tab.Vip, GameTexts.Vip.Tab, Icons.Dollar) };
            var tx = right.x;
            foreach (var (tab, label, icon) in tabs)
            {
                var r = new Rect(tx, right.y, 160, 36);
                if (GUI.Button(r, GUIContent.none, _tab == tab ? skin.ChipActive : skin.Chip))
                {
                    _tab = tab;
                }

                skin.DrawIcon(new Rect(r.x + 16, r.y + 8, 20, 20), icon, _tab == tab ? Color.white : UiSkin.Muted);
                GUI.Label(new Rect(r.x + 44, r.y + 6, r.width - 50, 24), label, skin.BodyBold);
                tx += 170;
            }

            var content = new Rect(right.x, right.y + 52, right.width, right.height - 52);
            switch (_tab)
            {
                case Tab.Boats: DrawBoats(skin, content, gear); break;
                case Tab.Baits: DrawBaits(skin, content, gear); break;
                case Tab.Vip: DrawVip(skin, content); break;
                default: DrawRods(skin, content); break;
            }
        }

        // ------------------------------------------------------------------ summary

        private static void DrawSummary(UiSkin skin, Rect rect, GearView gear)
        {
            GUI.Box(rect, GUIContent.none, skin.Card);
            var x = rect.x + 20;
            var w = rect.width - 40;
            var y = rect.y + 16;
            GUI.Label(new Rect(x, y, w, 28), GameTexts.Gear.Yours, skin.Heading);
            y += 38;

            GearRow(skin, x, ref y, w, Icons.Rod, GameTexts.Gear.Rod, gear.RodName, gear.RodBonus);
            GearRow(skin, x, ref y, w, Icons.Boat, GameTexts.Gear.Boat, gear.BoatName, gear.BoatBonus);
            GearRow(skin, x, ref y, w, Icons.Bait, GameTexts.Gear.Bait, gear.BaitName ?? GameTexts.Gear.NoBait, gear.BaitBonus);
            if (gear.BaitName != null)
            {
                GUI.Label(new Rect(x + 30, y - 8, w - 30, 20), GameTexts.Gear.ChargesLeft(gear.BaitChargesLeft), skin.SmallMuted);
                y += 16;
            }

            skin.Divider(new Rect(x, y, w, 1));
            y += 10;
            GUI.Label(new Rect(x, y, w * 0.6f, 22), GameTexts.Gear.Total, skin.SmallBold);
            GUI.Label(new Rect(x + w * 0.4f, y, w * 0.6f, 22), "+" + Format.Percent(gear.TotalBonus, 0), skin.SmallGoldRight);
            y += 36;

            GUI.Label(new Rect(x, y, w, 24), GameTexts.Gear.ChanceTitle, skin.BodyBold);
            y += 30;
            foreach (var chance in gear.Chances)
            {
                var color = chance.BitesHere ? UiSkin.RarityColor(chance.RarityId) : UiSkin.Muted;
                GUI.contentColor = color;
                GUI.Label(new Rect(x, y, w * 0.6f, 26), chance.RarityName, skin.BodyBold);
                GUI.contentColor = Color.white;
                GUI.Label(new Rect(x + w * 0.4f, y, w * 0.6f, 26), Format.Percent(chance.Chance, 0), chance.BitesHere ? skin.NumberRight : skin.SmallMutedRight);
                y += 26;
                skin.Bar(new Rect(x, y, w, 6), (float)chance.Chance, color);
                y += 10;
                if (!chance.BitesHere)
                {
                    GUI.Label(new Rect(x, y, w, 18), GameTexts.Gear.NotHere, skin.SmallMuted);
                    y += 18;
                }

                y += 8;
            }

            // The note goes under the list; on a short window it is left out instead of overlapping the bars.
            var noteY = Mathf.Max(y + 4, rect.yMax - 104);
            if (rect.yMax - 12 - noteY >= 56)
            {
                GUI.Label(new Rect(x, noteY, w, rect.yMax - 12 - noteY), GameTexts.Gear.ChanceNote(Format.Percent(gear.ChanceMin, 0), Format.Percent(gear.ChanceMax, 0)), skin.SmallMuted);
            }
        }

        private static void GearRow(UiSkin skin, float x, ref float y, float w, string icon, string label, string name, double bonus)
        {
            skin.DrawIcon(new Rect(x, y + 2, 20, 20), icon, UiSkin.Accent);
            GUI.Label(new Rect(x + 30, y, w - 110, 22), label + ": " + name, skin.Small);
            GUI.Label(new Rect(x + w - 90, y, 90, 22), "+" + Format.Percent(bonus, 0), skin.SmallRight);
            y += 34;
        }

        // ------------------------------------------------------------------ rods

        private void DrawRods(UiSkin skin, Rect content)
        {
            // A list, like boats and baits: with 6 rods, side-by-side cards got too narrow to read the names.
            // On a wide window the bonuses sit beside the name; on a narrow one, under it.
            var width = content.width - 18f;
            var wide = width >= 900f;
            var rowHeight = wide ? 132f : 240f;
            var view = new Rect(0, 0, width, _shop.Rods.Count * (rowHeight + 8f));
            _rodScroll = GUI.BeginScrollView(content, _rodScroll, view);
            var y = 0f;
            foreach (var rod in _shop.Rods)
            {
                DrawRod(skin, new Rect(0, y, width, rowHeight), rod, wide);
                y += rowHeight + 8f;
            }

            GUI.EndScrollView();
        }

        private void DrawRod(UiSkin skin, Rect rect, RodOfferView rod, bool wide)
        {
            GUI.Box(rect, GUIContent.none, rod.Owned ? skin.CardSelected : skin.Card);

            // The rod itself first (Art Bible, section 27): an important item, without fantasy excess.
            var pictureWidth = wide ? 110f : 90f;
            var picture = ArtAssets.Texture("Varas/" + rod.RodId);
            if (picture != null)
            {
                GUI.DrawTexture(new Rect(rect.x + 10, rect.y + 10, pictureWidth, Mathf.Min(rect.height - 20, 110)), picture, ScaleMode.ScaleToFit, true);
            }
            else
            {
                skin.IconBadge(new Rect(rect.x + 10 + (pictureWidth - 56) / 2f, rect.y + 30, 56, 56), Icons.Rod, rod.Owned ? UiSkin.Accent : UiSkin.Muted);
            }

            var buttonWidth = wide ? 220f : 180f;
            var x = rect.x + pictureWidth + 24;
            var bonusWidth = wide ? Mathf.Clamp(rect.width * 0.3f, 220f, 300f) : 0f;
            var infoWidth = rect.width - (x - rect.x) - buttonWidth - 32 - (wide ? bonusWidth + 20 : 0);

            // Name, tier and what it catches.
            var y = rect.y + 12;
            GUI.Label(new Rect(x, y, infoWidth, 28), rod.Name, skin.Heading);
            y += 30;
            GUI.Label(new Rect(x, y, infoWidth, 22), GameTexts.Profile.Tier(rod.Tier) + " · " + GameTexts.Shop.MaxLevelOf(rod.MaxLevel), skin.SmallMuted);
            y += 22;
            GUI.Label(new Rect(x, y, infoWidth, 22), rod.CanCatchMythic ? GameTexts.Profile.CatchesUpToMythic : rod.CanCatchLegendary ? GameTexts.Profile.CatchesUpToLegendary : rod.CanCatchEpic ? GameTexts.Profile.CatchesRareAndEpic : rod.CanCatchRare ? GameTexts.Profile.CatchesRare : GameTexts.Profile.NoRare, skin.Small);
            y += 22;
            GUI.Label(new Rect(x, y, infoWidth, 22), GameTexts.Shop.Requires(rod.UnlockFisherLevel), skin.Small);
            y += 28;

            // Bonuses at level 1 → at the maximum level: beside the name, or under it.
            var bx = wide ? x + infoWidth + 20 : x;
            var bw = wide ? bonusWidth : rect.xMax - 16 - x;
            var by = wide ? rect.y + 10 : y;
            GUI.Label(new Rect(bx, by, bw, 20), GameTexts.Shop.AtLevel1 + " → " + GameTexts.Shop.AtMax, skin.SmallMuted);
            by += 22;
            Row(skin, bx, ref by, bw, GameTexts.Shop.CatchBonus, rod.CatchBonus, rod.CatchBonusAtMax);
            Row(skin, bx, ref by, bw, GameTexts.Profile.RarityBonus, rod.RarityBonus, rod.RarityBonusAtMax);
            Row(skin, bx, ref by, bw, GameTexts.Profile.SizeBonus, rod.SizeBonus, rod.SizeBonusAtMax);
            Row(skin, bx, ref by, bw, GameTexts.Profile.ShellBonus, rod.ShellBonus, rod.ShellBonusAtMax);

            // Price and button on the right.
            var rx = rect.xMax - buttonWidth - 16;
            if (!rod.Owned)
            {
                if (rod.IsFree)
                {
                    GUI.Label(new Rect(rx, rect.y + 16, buttonWidth, 24), GameTexts.Shop.Free, skin.BodyBold);
                }
                else
                {
                    Cost(skin, new Rect(rx, rect.y + 18, buttonWidth, 22), rod.PriceCoins, rod.PriceShells);
                }
            }

            var button = new Rect(rx, rect.y + 52, buttonWidth, 42);
            if (rod.Owned)
            {
                OwnedPill(skin, button.x, button.y, GameTexts.Shop.Owned);
            }
            else if (rod.BuyBlocker != ServiceError.None)
            {
                Blocked(skin, new Rect(button.x, button.y, button.width, 42), rod.BuyBlocker, rod.BuyBlocker == ServiceError.RodLocked ? GameTexts.Shop.Requires(rod.UnlockFisherLevel) : null);
            }
            else if (skin.IconButton(button, Icons.Buy, rod.IsFree ? GameTexts.Shop.ClaimFree : GameTexts.Shop.Buy, skin.ButtonPrimary))
            {
                _root.BuyRod(rod.RodId);
                _dirty = true;
            }
        }

        private static void Row(UiSkin skin, float x, ref float y, float w, string label, double at1, double atMax)
        {
            GUI.Label(new Rect(x, y, w - 100, 22), label, skin.SmallMuted);
            GUI.Label(new Rect(x + w - 110, y, 110, 22), "+" + Format.Percent(at1, 0) + " → +" + Format.Percent(atMax, 0), skin.SmallRight);
            y += 24;
        }

        // ------------------------------------------------------------------ boats

        private void DrawBoats(UiSkin skin, Rect content, GearView gear)
        {
            GUI.Label(new Rect(content.x, content.y, content.width, 22), GameTexts.Gear.BoatsNote, skin.SmallMuted);
            var y = content.y + 32;
            var rowHeight = Mathf.Min(96f, (content.height - 32f) / Mathf.Max(1, gear.Boats.Count) - 8f);
            foreach (var boat in gear.Boats)
            {
                var rect = new Rect(content.x, y, content.width, rowHeight);
                GUI.Box(rect, GUIContent.none, boat.InUse ? skin.CardSelected : skin.Card);
                var cy = rect.y + (rect.height - 56) / 2f;

                // ASSET_PENDENTE: a picture of each boat (docs/ASSETS_PENDENTES.md); a clean icon meanwhile.
                var picture = ArtAssets.Texture("Barcos/" + boat.BoatId);
                if (picture != null)
                {
                    GUI.DrawTexture(new Rect(rect.x + 14, rect.y + 6, 110, rect.height - 12), picture, ScaleMode.ScaleToFit, true);
                }
                else
                {
                    skin.IconBadge(new Rect(rect.x + 40, cy, 56, 56), Icons.Boat, boat.InUse ? UiSkin.Accent : UiSkin.Muted);
                }

                var x = rect.x + 140;
                GUI.Label(new Rect(x, cy, 220, 28), boat.Name, skin.Heading);
                GUI.Label(new Rect(x, cy + 28, 220, 36), string.IsNullOrEmpty(boat.Description) ? GameTexts.Profile.Tier(boat.Tier) : boat.Description, skin.SmallMuted);
                GUI.Label(new Rect(x + 230, cy + 4, 180, 24), GameTexts.Gear.Bonus(Format.Percent(boat.Bonus, 0)), skin.BodyBold);
                if (!boat.Owned)
                {
                    Cost(skin, new Rect(x + 230, cy + 30, 220, 22), boat.CostCoins, boat.CostShells);
                }

                var button = new Rect(rect.xMax - 230, rect.y + (rect.height - 42) / 2f, 210, 42);
                if (boat.InUse)
                {
                    OwnedPill(skin, button.x, button.y, GameTexts.Gear.InUse);
                }
                else if (boat.Owned)
                {
                    if (skin.IconButton(button, Icons.Swap, GameTexts.Gear.Use, skin.Button))
                    {
                        _root.UseBoat(boat.BoatId);
                    }
                }
                else if (boat.BuyBlocker != ServiceError.None)
                {
                    // Inside the button's place, like the rods: never over the Conchas price beside it.
                    Blocked(skin, new Rect(button.x, button.y, button.width, 42), boat.BuyBlocker, boat.BuyBlocker == ServiceError.BoatLocked ? GameTexts.Shop.Requires(boat.UnlockFisherLevel) : null);
                }
                else if (skin.IconButton(button, Icons.Buy, GameTexts.Shop.Buy, skin.ButtonPrimary))
                {
                    _root.BuyBoat(boat.BoatId);
                }

                y += rowHeight + 8;
            }
        }

        // ------------------------------------------------------------------ baits

        private void DrawBaits(UiSkin skin, Rect content, GearView gear)
        {
            GUI.Label(new Rect(content.x, content.y, content.width, 40), GameTexts.Gear.BaitsNote, skin.SmallMuted);
            var y = content.y + 50;
            foreach (var bait in gear.Baits)
            {
                var rect = new Rect(content.x, y, content.width, 118);
                GUI.Box(rect, GUIContent.none, bait.InUse ? skin.CardSelected : skin.Card);
                skin.IconBadge(new Rect(rect.x + 40, rect.y + 31, 56, 56), Icons.Bait, bait.InUse ? UiSkin.Accent : UiSkin.Muted);

                var x = rect.x + 140;
                GUI.Label(new Rect(x, rect.y + 16, 240, 28), bait.Name, skin.Heading);
                GUI.Label(new Rect(x, rect.y + 46, 240, 22), GameTexts.Gear.Bonus(Format.Percent(bait.Bonus, 0)), skin.BodyBold);
                GUI.Label(new Rect(x, rect.y + 72, 240, 22), GameTexts.Gear.Charges(bait.ChargesPerPurchase), skin.SmallMuted);
                if (!string.IsNullOrEmpty(bait.Description))
                {
                    GUI.Label(new Rect(x, rect.y + 94, rect.width - 140 - 250, 20), bait.Description, skin.SmallMuted);
                }
                Cost(skin, new Rect(x + 250, rect.y + 20, 240, 22), bait.CostCoins, bait.CostShells);
                GUI.Label(new Rect(x + 250, rect.y + 50, 240, 22), bait.ChargesLeft > 0 ? GameTexts.Gear.ChargesLeft(bait.ChargesLeft) : string.Empty, bait.InUse ? skin.SmallGold : skin.Small);

                var buy = new Rect(rect.xMax - 230, rect.y + 14, 210, 42);
                if (bait.BuyBlocker != ServiceError.None)
                {
                    // Inside the button's place, like the rods: never over the Conchas price beside it.
                    Blocked(skin, new Rect(buy.x, buy.y, buy.width, 42), bait.BuyBlocker, bait.BuyBlocker == ServiceError.BaitLocked ? GameTexts.Shop.Requires(bait.UnlockFisherLevel) : null);
                }
                else if (skin.IconButton(buy, Icons.Buy, GameTexts.Shop.Buy, skin.ButtonPrimary))
                {
                    _root.BuyBait(bait.BaitId);
                }

                var use = new Rect(rect.xMax - 230, rect.y + 64, 210, 40);
                if (bait.InUse)
                {
                    if (skin.IconButton(use, Icons.Pause, GameTexts.Gear.PutAway, skin.Button))
                    {
                        _root.UseBait(null);
                    }
                }
                else if (bait.ChargesLeft > 0 && skin.IconButton(use, Icons.Swap, GameTexts.Gear.Use, skin.Button))
                {
                    _root.UseBait(bait.BaitId);
                }

                y += 130;
            }
        }

        // ------------------------------------------------------------------ VIP (A-110)

        private void DrawVip(UiSkin skin, Rect content)
        {
            var vip = _root.Vip;
            if (vip == null)
            {
                return;
            }

            GUI.Label(new Rect(content.x, content.y, content.width, 40), GameTexts.Vip.Note, skin.SmallMuted);
            var rect = new Rect(content.x, content.y + 50, content.width, 190);
            GUI.Box(rect, GUIContent.none, vip.Active ? skin.CardSelected : skin.Card);
            skin.IconBadge(new Rect(rect.x + 40, rect.y + 30, 56, 56), Icons.Dollar, vip.Active ? UiSkin.Gold : UiSkin.Muted);

            var x = rect.x + 140;
            var w = rect.width - 140 - 260;
            GUI.Label(new Rect(x, rect.y + 18, w, 28), GameTexts.Vip.Title, skin.Heading);
            GUI.Label(new Rect(x, rect.y + 50, w, 22), GameTexts.Vip.Benefit(Format.Percent(vip.OfflineFisherXpBonus, 0)), skin.BodyBold);
            GUI.Label(new Rect(x, rect.y + 76, w, 22), GameTexts.Vip.Duration(Format.Number((long)System.Math.Round(vip.DurationDays))), skin.Small);
            GUI.Label(new Rect(x, rect.y + 102, w, 22), GameTexts.Vip.Price(Format.Number(vip.PriceDollars)), skin.SmallGold);
            GUI.Label(new Rect(x, rect.y + 128, w, 22), GameTexts.Vip.YourDollars(Format.Number(vip.Dollars)), skin.SmallMuted);
            GUI.Label(new Rect(x, rect.y + 154, w, 22), vip.Active
                ? GameTexts.Vip.Until(Format.DateTimeFromUnixMs(vip.UntilMs), Format.Duration(vip.RemainingMs / 1000.0))
                : GameTexts.Vip.Inactive, vip.Active ? skin.SmallGold : skin.SmallMuted);

            var button = new Rect(rect.xMax - 240, rect.y + 20, 220, 42);
            if (vip.BuyBlocker != ServiceError.None)
            {
                Blocked(skin, new Rect(button.x - 20, button.y, button.width + 20, 42), vip.BuyBlocker);
            }
            else if (skin.IconButton(button, Icons.Buy, vip.Active ? GameTexts.Vip.Extend : GameTexts.Vip.Buy, skin.ButtonPrimary))
            {
                _root.BuyVip();
            }

            if (vip.Active)
            {
                OwnedPill(skin, button.x, button.y + 52, GameTexts.Vip.Active);
            }
        }

        // ------------------------------------------------------------------ helpers

        private static void Cost(UiSkin skin, Rect rect, long coins, long shells)
        {
            // The Conchas price sits right after the coins (not at a fixed 120 px), so it stays clear
            // of the button column on the right.
            var coinsText = Format.Short(coins);
            var coinsWidth = skin.CoinAmountWidth(coinsText, rect.height);
            skin.CoinAmount(new Rect(rect.x, rect.y, coinsWidth, rect.height), coinsText);
            if (shells > 0)
            {
                var sx = rect.x + coinsWidth + 10f;
                var shellsText = Format.Number(shells);
                skin.DrawIcon(new Rect(sx, rect.y, rect.height, rect.height), Icons.Shell, Color.white);
                GUI.Label(new Rect(sx + 4 + rect.height, rect.y, skin.SmallGoldLine.CalcSize(new GUIContent(shellsText)).x + 4f, rect.height), shellsText, skin.SmallGoldLine);
            }
        }

        private static void OwnedPill(UiSkin skin, float x, float y, string text)
        {
            var label = text.ToUpperInvariant();
            skin.AccentPill(new Rect(x, y + 9, skin.PillWidth(label, true) + 6, 26), label, UiSkin.Accent, Icons.Check);
        }

        private static void Blocked(UiSkin skin, Rect rect, ServiceError blocker, string text = null)
        {
            skin.DrawIcon(new Rect(rect.x, rect.y + 11, 20, 20), Icons.Lock, UiSkin.Gold);
            GUI.Label(new Rect(rect.x + 28, rect.y + 2, rect.width - 28, 42), text ?? GameTexts.ServiceErrorMessage(blocker.ToString()), skin.SmallGold);
        }
    }
}
