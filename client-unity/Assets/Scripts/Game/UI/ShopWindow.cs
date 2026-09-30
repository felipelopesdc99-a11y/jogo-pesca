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
        }

        private const float SummaryWidth = 330f;

        private readonly GameRoot _root;
        private ShopView _shop;
        private bool _dirty = true;
        private Tab _tab;

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
            var tabs = new[] { (Tab.Rods, GameTexts.Gear.TabRods, Icons.Rod), (Tab.Boats, GameTexts.Gear.TabBoats, Icons.Boat), (Tab.Baits, GameTexts.Gear.TabBaits, Icons.Bait) };
            var tx = right.x;
            foreach (var (tab, label, icon) in tabs)
            {
                var r = new Rect(tx, right.y, 170, 36);
                if (GUI.Button(r, GUIContent.none, _tab == tab ? skin.ChipActive : skin.Chip))
                {
                    _tab = tab;
                }

                skin.DrawIcon(new Rect(r.x + 16, r.y + 8, 20, 20), icon, _tab == tab ? UiSkin.Accent : UiSkin.Muted);
                GUI.Label(new Rect(r.x + 44, r.y + 6, r.width - 50, 24), label, skin.BodyBold);
                tx += 182;
            }

            var content = new Rect(right.x, right.y + 52, right.width, right.height - 52);
            switch (_tab)
            {
                case Tab.Boats: DrawBoats(skin, content, gear); break;
                case Tab.Baits: DrawBaits(skin, content, gear); break;
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

            GUI.Label(new Rect(x, rect.yMax - 104, w, 92), GameTexts.Gear.ChanceNote, skin.SmallMuted);
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
            var x = content.x;
            var count = Mathf.Max(2, _shop.Rods.Count);
            var w = (content.width - 20f * (count - 1)) / count;
            foreach (var rod in _shop.Rods)
            {
                DrawRod(skin, new Rect(x, content.y, w, content.height), rod);
                x += w + 20f;
            }
        }

        private void DrawRod(UiSkin skin, Rect rect, RodOfferView rod)
        {
            GUI.Box(rect, GUIContent.none, rod.Owned ? skin.CardSelected : skin.Card);
            var x = rect.x + 22;
            var w = rect.width - 44;
            var y = rect.y + 14;

            // The rod itself first (Art Bible, section 27): an important item, without fantasy excess.
            var picture = ArtAssets.Texture("Varas/" + rod.RodId);
            if (picture != null)
            {
                GUI.DrawTexture(new Rect(x, y, w, 100), picture, ScaleMode.ScaleToFit, true);
                y += 106;
            }

            GUI.Label(new Rect(x, y, w, 30), rod.Name, skin.Heading);
            y += 32;
            GUI.Label(new Rect(x, y, w, 22), GameTexts.Profile.Tier(rod.Tier) + " · " + GameTexts.Shop.MaxLevelOf(rod.MaxLevel), skin.SmallMuted);
            y += 28;
            skin.CoinIcon(new Rect(x, y + 2, 22, 22));
            GUI.Label(new Rect(x + 30, y, w - 30, 28), rod.IsFree ? GameTexts.Shop.Free : GameTexts.Shop.Price(Format.Number(rod.PriceCoins)), skin.Number);
            y += 36;

            GUI.Label(new Rect(x, y, w, 20), GameTexts.Shop.AtLevel1 + " → " + GameTexts.Shop.AtMax, skin.SmallMuted);
            y += 24;
            Row(skin, x, ref y, w, GameTexts.Shop.CatchBonus, rod.CatchBonus, rod.CatchBonusAtMax);
            Row(skin, x, ref y, w, GameTexts.Profile.RarityBonus, rod.RarityBonus, rod.RarityBonusAtMax);
            Row(skin, x, ref y, w, GameTexts.Profile.SizeBonus, rod.SizeBonus, rod.SizeBonusAtMax);
            Row(skin, x, ref y, w, GameTexts.Profile.ShellBonus, rod.ShellBonus, rod.ShellBonusAtMax);
            y += 6;
            GUI.Label(new Rect(x, y, w, 20), rod.CanCatchEpic ? GameTexts.Profile.CatchesRareAndEpic : rod.CanCatchRare ? GameTexts.Profile.CatchesRare : GameTexts.Profile.NoRare, skin.Small);
            y += 22;
            GUI.Label(new Rect(x, y, w, 20), GameTexts.Shop.Requires(rod.UnlockFisherLevel), skin.Small);

            var button = new Rect(x, rect.yMax - 58, w, 42);
            if (rod.Owned)
            {
                OwnedPill(skin, x, button.y, GameTexts.Shop.Owned);
            }
            else if (rod.BuyBlocker != ServiceError.None)
            {
                Blocked(skin, new Rect(x, button.y, w, 42), rod.BuyBlocker);
            }
            else if (skin.IconButton(button, Icons.Buy, rod.IsFree ? GameTexts.Shop.ClaimFree : GameTexts.Shop.Buy, skin.ButtonPrimary))
            {
                _root.BuyRod(rod.RodId);
                _dirty = true;
            }
        }

        private static void Row(UiSkin skin, float x, ref float y, float w, string label, double at1, double atMax)
        {
            GUI.Label(new Rect(x, y, w * 0.55f, 22), label, skin.SmallMuted);
            GUI.Label(new Rect(x + w * 0.45f, y, w * 0.55f, 22), "+" + Format.Percent(at1, 0) + " → +" + Format.Percent(atMax, 0), skin.SmallRight);
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
                GUI.Label(new Rect(x, cy + 30, 220, 22), GameTexts.Profile.Tier(boat.Tier), skin.SmallMuted);
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
                    Blocked(skin, new Rect(button.x - 40, button.y, button.width + 40, 42), boat.BuyBlocker, boat.BuyBlocker == ServiceError.BoatLocked ? GameTexts.Shop.Requires(boat.UnlockFisherLevel) : null);
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
                Cost(skin, new Rect(x + 250, rect.y + 20, 240, 22), bait.CostCoins, bait.CostShells);
                GUI.Label(new Rect(x + 250, rect.y + 50, 240, 22), bait.ChargesLeft > 0 ? GameTexts.Gear.ChargesLeft(bait.ChargesLeft) : string.Empty, bait.InUse ? skin.SmallGold : skin.Small);

                var buy = new Rect(rect.xMax - 230, rect.y + 14, 210, 42);
                if (bait.BuyBlocker != ServiceError.None)
                {
                    Blocked(skin, new Rect(buy.x - 40, buy.y, buy.width + 40, 42), bait.BuyBlocker, bait.BuyBlocker == ServiceError.BaitLocked ? GameTexts.Shop.Requires(bait.UnlockFisherLevel) : null);
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

        // ------------------------------------------------------------------ helpers

        private static void Cost(UiSkin skin, Rect rect, long coins, long shells)
        {
            skin.CoinAmount(new Rect(rect.x, rect.y, 120, rect.height), Format.Number(coins));
            if (shells > 0)
            {
                skin.DrawIcon(new Rect(rect.x + 124, rect.y, rect.height, rect.height), Icons.Shell, Color.white);
                GUI.Label(new Rect(rect.x + 128 + rect.height, rect.y, 90, rect.height), Format.Number(shells), skin.SmallGold);
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
