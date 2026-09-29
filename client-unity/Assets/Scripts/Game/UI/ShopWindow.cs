using FishingIdle.Game.Bootstrap;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Shop;
using FishingIdle.Game.Visual;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>The Shop (GDD section 7). V0.1 sells rods; prices and requirements come from rods.json.</summary>
    public sealed class ShopWindow
    {
        private readonly GameRoot _root;
        private ShopView _shop;
        private bool _dirty = true;

        public ShopWindow(GameRoot root)
        {
            _root = root;
            _root.AquariumChanged += () => _dirty = true;
            _root.BoxChanged += () => _dirty = true;
        }

        public bool IsOpen { get; private set; }

        public void Open()
        {
            IsOpen = true;
            _dirty = true;
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

            if (_shop == null)
            {
                return;
            }

            var area = WindowFrame.Draw(skin, screenWidth, screenHeight, GameTexts.Shop.Title, GameTexts.Shop.Note, out var closed, 1100f, 700f, Icons.Shop);
            if (closed)
            {
                Close();
                return;
            }

            skin.DrawIcon(new Rect(area.x, area.y + 3, 22, 22), Icons.Rod, UiSkin.Accent);
            GUI.Label(new Rect(area.x + 30, area.y, 300, 26), GameTexts.Shop.Rods, skin.Heading);
            var x = area.x;
            foreach (var rod in _shop.Rods)
            {
                DrawRod(skin, new Rect(x, area.y + 40, 460, area.height - 50), rod);
                x += 480;
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
                GUI.DrawTexture(new Rect(x, y, w, 116), picture, ScaleMode.ScaleToFit, true);
                y += 124;
            }

            GUI.Label(new Rect(x, y, w, 30), rod.Name, skin.Heading);
            y += 32;
            GUI.Label(new Rect(x, y, w, 22), GameTexts.Profile.Tier(rod.Tier) + " · " + GameTexts.Shop.MaxLevelOf(rod.MaxLevel), skin.SmallMuted);
            y += 30;
            skin.CoinIcon(new Rect(x, y + 2, 22, 22));
            GUI.Label(new Rect(x + 30, y, w - 30, 28), rod.IsFree ? GameTexts.Shop.Free : GameTexts.Shop.Price(Format.Number(rod.PriceCoins)), skin.Number);
            y += 40;

            GUI.Label(new Rect(x, y, w, 20), GameTexts.Shop.AtLevel1 + " → " + GameTexts.Shop.AtMax, skin.SmallMuted);
            y += 24;
            Row(skin, x, ref y, w, GameTexts.Profile.RarityBonus, rod.RarityBonus, rod.RarityBonusAtMax);
            Row(skin, x, ref y, w, GameTexts.Profile.SizeBonus, rod.SizeBonus, rod.SizeBonusAtMax);
            Row(skin, x, ref y, w, GameTexts.Profile.ShellBonus, rod.ShellBonus, rod.ShellBonusAtMax);
            y += 6;
            GUI.Label(new Rect(x, y, w, 20), rod.CanCatchRare ? GameTexts.Profile.CatchesRare : GameTexts.Profile.NoRare, skin.Small);
            y += 22;
            GUI.Label(new Rect(x, y, w, 20), GameTexts.Shop.Requires(rod.UnlockFisherLevel), skin.Small);

            var button = new Rect(x, rect.yMax - 58, w, 42);
            if (rod.Owned)
            {
                var label = GameTexts.Shop.Owned.ToUpperInvariant();
                skin.AccentPill(new Rect(x, button.y + 9, skin.PillWidth(label, true) + 6, 26), label, UiSkin.Accent, Icons.Check);
            }
            else if (rod.BuyBlocker != ServiceError.None)
            {
                skin.DrawIcon(new Rect(x, button.y + 11, 20, 20), Icons.Lock, UiSkin.Gold);
                GUI.Label(new Rect(x + 28, button.y + 2, w - 28, 42), GameTexts.ServiceErrorMessage(rod.BuyBlocker.ToString()), skin.SmallGold);
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
    }
}
