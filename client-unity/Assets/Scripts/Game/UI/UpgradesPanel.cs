using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Visual;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Upgrades;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The "Melhorias" tab of the Crew window (M24-T11, addendum A-155): the 3 cheapest Crew members' upgrades, every
    /// general upgrade with its next level, and a collapsible list of what was bought.
    /// </summary>
    /// <remarks>
    /// Shows what the Upgrades service returns (prices, locks, levels, effects); nothing here computes a price or an
    /// effect. Art is provisional until it arrives (ASSET_PENDENTE: Resources/Arte/Melhorias/melhoria_&lt;id&gt; for the
    /// general upgrades, docs/ASSETS_PENDENTES.md); the Crew members' upgrades use the member's portrait.
    /// </remarks>
    public sealed class UpgradesPanel
    {
        private const float HeaderHeight = 32f, CrewRowHeight = 96f, GeneralRowHeight = 100f, BoughtLineHeight = 30f, Gap = 12f;
        private const float ButtonWidth = 190f, RefreshSeconds = 0.25f;

        /// <summary>The general upgrades' art (Resources/Arte/Melhorias), by upgrade id. Missing = an icon placeholder.</summary>
        private const string ArtFolder = "Melhorias/melhoria_";

        /// <summary>The members' portraits, shared with the Crew tab.</summary>
        private const string PortraitFolder = "Tripulacao/trip_";

        private readonly GameRoot _root;
        private UpgradesView _view;
        private float _nextRefresh;
        private bool _boughtOpen;
        private Vector2 _boughtScroll;
        private Vector2 _generalScroll;

        public UpgradesPanel(GameRoot root)
        {
            _root = root;
        }

        /// <summary>The text behind the window's "i" while this tab is open.</summary>
        public string Info
        {
            get
            {
                var view = View();
                return view == null ? null : GameTexts.Upgrades.Info(Format.Factor(view.CrewMultiplier), string.Join(", ", view.CrewUnlockCounts));
            }
        }

        /// <summary>Reads the service again on the next draw (on opening and after switching tabs).</summary>
        public void Invalidate() => _nextRefresh = 0f;

        public void Draw(UiSkin skin, Rect area)
        {
            var view = View();
            if (view == null)
            {
                return;
            }

            // Left: the Crew members' next upgrades, then what was bought. Right: the general upgrades.
            var leftWidth = Mathf.Floor(area.width * 0.52f);
            var left = new Rect(area.x, area.y, leftWidth, area.height);
            var right = new Rect(left.xMax + 20f, area.y, area.xMax - left.xMax - 20f, area.height);

            var crewHeight = HeaderHeight + Mathf.Max(1, view.NextCrew.Count) * CrewRowHeight + (view.CrewAvailableCount > 0 ? 26f : 0f);
            DrawCrew(skin, new Rect(left.x, left.y, left.width, crewHeight), view);
            DrawBought(skin, new Rect(left.x, left.y + crewHeight + Gap, left.width, left.yMax - left.y - crewHeight - Gap), view);
            DrawGeneral(skin, right, view);
        }

        private UpgradesView View()
        {
            if (_view == null || Time.unscaledTime >= _nextRefresh)
            {
                _view = _root.GetUpgrades();
                _nextRefresh = Time.unscaledTime + RefreshSeconds;
            }

            return _view;
        }

        // ------------------------------------------------------------------ Crew members' upgrades

        private void DrawCrew(UiSkin skin, Rect rect, UpgradesView view)
        {
            DrawHeader(skin, new Rect(rect.x, rect.y, rect.width, HeaderHeight), GameTexts.Upgrades.NextCrew,
                GameTexts.Upgrades.CrewProgress(view.CrewBoughtCount, view.CrewTotalCount));
            var y = rect.y + HeaderHeight;
            if (view.NextCrew.Count == 0)
            {
                var empty = view.CrewBoughtCount >= view.CrewTotalCount && view.CrewTotalCount > 0 ? GameTexts.Upgrades.AllCrewBought : GameTexts.Upgrades.NoCrewOffers;
                GUI.Box(new Rect(rect.x, y, rect.width, CrewRowHeight - 8f), GUIContent.none, skin.Card);
                GUI.Label(new Rect(rect.x + 20f, y + (CrewRowHeight - 8f) / 2f - 11f, rect.width - 40f, UiSkin.SmallLine), FishCard.Fit(empty, skin.SmallMuted, rect.width - 40f), skin.SmallMuted);
                return;
            }

            foreach (var offer in view.NextCrew)
            {
                DrawOffer(skin, new Rect(rect.x, y, rect.width, CrewRowHeight - 8f), offer);
                y += CrewRowHeight;
            }

            if (view.CrewAvailableCount > 0)
            {
                GUI.Label(new Rect(rect.x + 4f, y, rect.width - 8f, UiSkin.SmallLine), GameTexts.Upgrades.MoreAvailable(view.CrewAvailableCount), skin.SmallMuted);
            }
        }

        // ------------------------------------------------------------------ general upgrades

        private void DrawGeneral(UiSkin skin, Rect rect, UpgradesView view)
        {
            DrawHeader(skin, new Rect(rect.x, rect.y, rect.width, HeaderHeight), GameTexts.Upgrades.General, null);
            var list = new Rect(rect.x, rect.y + HeaderHeight, rect.width, rect.height - HeaderHeight);
            var scrolls = view.General.Count * GeneralRowHeight > list.height;
            var content = new Rect(0, 0, list.width - (scrolls ? 20f : 0f), view.General.Count * GeneralRowHeight);
            _generalScroll = GUI.BeginScrollView(list, _generalScroll, content);
            for (var i = 0; i < view.General.Count; i++)
            {
                DrawOffer(skin, new Rect(0, i * GeneralRowHeight, content.width, GeneralRowHeight - 8f), view.General[i]);
            }

            GUI.EndScrollView();
        }

        // ------------------------------------------------------------------ one upgrade

        /// <summary>Art, name, effect (and level), then Comprar with the price, the lock line or "Nível máximo".</summary>
        private void DrawOffer(UiSkin skin, Rect row, UpgradeOfferView offer)
        {
            var locked = offer.BuyBlocker == ServiceError.UpgradeLocked;
            GUI.Box(row, GUIContent.none, skin.Card);
            if (locked)
            {
                GUI.DrawTexture(row, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.45f), 0, 10);
            }

            var art = new Rect(row.x + 12f, row.y + (row.height - 56f) / 2f, 56f, 56f);
            DrawArt(skin, art, offer, locked);

            var x = art.xMax + 14f;
            var textWidth = row.xMax - ButtonWidth - 28f - x;
            GUI.Label(new Rect(x, row.y + 10f, textWidth, 28f), FishCard.Fit(offer.Name, skin.Heading, textWidth), skin.Heading);

            // Effect of the next level; general upgrades also show the level and what they give now.
            var effect = GameTexts.Upgrades.Effect(offer.EffectKey, offer.EffectValue, offer.MemberName);
            if (!offer.IsCrewUpgrade)
            {
                effect = GameTexts.Upgrades.Level(offer.Level, offer.MaxLevel) + " · " + effect;
            }

            GUI.Label(new Rect(x, row.y + 40f, textWidth, UiSkin.SmallLine), FishCard.Fit(effect, skin.SmallGold, textWidth), skin.SmallGold);
            string third = null;
            if (locked)
            {
                third = GameTexts.Crew.LockedLine(offer.UnlockCount, offer.MemberNamePlural, Format.Number(offer.UnlockHave));
            }
            else if (!offer.IsCrewUpgrade && offer.Level > 0)
            {
                third = GameTexts.Upgrades.Now(GameTexts.Upgrades.Effect(offer.EffectKey, offer.EffectTotal, offer.MemberName));
            }

            if (third != null)
            {
                if (locked)
                {
                    skin.DrawIcon(new Rect(x, row.y + 64f, 16f, 16f), Icons.Lock, UiSkin.Gold);
                }

                var tx = locked ? x + 22f : x;
                GUI.Label(new Rect(tx, row.y + 62f, textWidth - (tx - x), UiSkin.SmallLine), FishCard.Fit(third, skin.SmallMuted, textWidth - (tx - x)), skin.SmallMuted);
            }

            // Comprar with the price under it, or "Nível máximo".
            var button = new Rect(row.xMax - ButtonWidth - 14f, row.y + 10f, ButtonWidth, 40f);
            if (offer.BuyBlocker == ServiceError.UpgradeMaxLevel)
            {
                skin.AccentPill(new Rect(button.x, button.y + 6f, button.width, 28f), GameTexts.Upgrades.MaxLevel, UiSkin.Gold);
                return;
            }

            var affordable = offer.BuyBlocker == ServiceError.None;
            var enabled = GUI.enabled;
            GUI.enabled = enabled && affordable;
            if (skin.IconButton(button, Icons.Buy, GameTexts.Upgrades.Buy, affordable ? skin.ButtonPrimary : skin.Button))
            {
                _root.BuyUpgrade(offer.UpgradeId);
                Invalidate();
            }

            GUI.enabled = enabled;
            var price = offer.Cost == UpgradeRules.Unaffordable ? GameTexts.Crew.TooExpensive : Format.Short(offer.Cost);
            var priceStyle = affordable ? skin.SmallGold : skin.SmallMuted;
            var priceWidth = Mathf.Min(priceStyle.CalcSize(new GUIContent(price)).x + 4f, ButtonWidth - 30f);
            var px = button.center.x - (priceWidth + 26f) / 2f;
            skin.CoinIcon(new Rect(px, button.yMax + 8f, 20f, 20f));
            GUI.Label(new Rect(px + 26f, button.yMax + 8f, priceWidth, UiSkin.SmallLine), FishCard.Fit(price, priceStyle, priceWidth), priceStyle);
        }

        /// <summary>
        /// A Crew member's upgrade shows the member's portrait (or its placeholder); a general one its own art (or an
        /// icon placeholder by effect). ASSET_PENDENTE: melhoria_&lt;id&gt;.
        /// </summary>
        private static void DrawArt(UiSkin skin, Rect rect, UpgradeOfferView offer, bool locked)
        {
            var art = ArtAssets.Texture(offer.IsCrewUpgrade ? PortraitFolder + offer.MemberId : ArtFolder + offer.UpgradeId);
            var previous = GUI.color;
            if (locked)
            {
                GUI.color = previous * new Color(0.35f, 0.38f, 0.45f, 1f);
            }

            if (art != null)
            {
                GUI.DrawTexture(rect, art, ScaleMode.ScaleToFit, true);
            }
            else
            {
                skin.IconBadge(rect, PlaceholderIcon(offer), locked ? UiSkin.Muted : offer.IsCrewUpgrade ? UiSkin.Accent : UiSkin.Gold);
            }

            GUI.color = previous;
        }

        private static string PlaceholderIcon(UpgradeOfferView offer)
        {
            switch (offer.EffectKey)
            {
                case "crew_coins": return Icons.Bell;
                case "fish_sale": return Icons.Sell;
                case "crew_offline_hours": return Icons.Box;
                case "crew_xp": return Icons.Search;
                case "fishing_coins": return Icons.Waves;
                default: return Icons.Star;
            }
        }

        // ------------------------------------------------------------------ bought

        /// <summary>"Compradas (N)" with Mostrar / Esconder; open, one line per upgrade with what it gives now.</summary>
        private void DrawBought(UiSkin skin, Rect rect, UpgradesView view)
        {
            if (rect.height < HeaderHeight)
            {
                return;
            }

            var header = new Rect(rect.x, rect.y, rect.width, HeaderHeight);
            GUI.Label(new Rect(header.x, header.y + 4f, header.width - 140f, 24f), GameTexts.Upgrades.BoughtHeader(view.Bought.Count), skin.BodyBold);
            if (view.Bought.Count > 0 && GUI.Button(new Rect(header.xMax - 130f, header.y, 130f, 30f), _boughtOpen ? GameTexts.Upgrades.Hide : GameTexts.Upgrades.Show, skin.Chip))
            {
                _boughtOpen = !_boughtOpen;
            }

            var body = new Rect(rect.x, header.yMax + 6f, rect.width, rect.yMax - header.yMax - 6f);
            if (view.Bought.Count == 0)
            {
                GUI.Label(new Rect(body.x + 4f, body.y, body.width - 8f, UiSkin.SmallLine), GameTexts.Upgrades.NothingBought, skin.SmallMuted);
                return;
            }

            if (!_boughtOpen || body.height < BoughtLineHeight)
            {
                return;
            }

            skin.Inset(body);
            var list = new Rect(body.x + 12f, body.y + 8f, body.width - 24f, body.height - 16f);
            var scrolls = view.Bought.Count * BoughtLineHeight > list.height;
            var content = new Rect(0, 0, list.width - (scrolls ? 20f : 0f), view.Bought.Count * BoughtLineHeight);
            _boughtScroll = GUI.BeginScrollView(list, _boughtScroll, content);
            for (var i = 0; i < view.Bought.Count; i++)
            {
                var item = view.Bought[i];
                var line = new Rect(0, i * BoughtLineHeight, content.width, BoughtLineHeight);
                var effect = item.IsCrewUpgrade
                    ? GameTexts.Upgrades.Effect(item.EffectKey, item.EffectValue, item.MemberName)
                    : GameTexts.Upgrades.Effect(item.EffectKey, item.EffectTotal, item.MemberName) + " · " + GameTexts.Upgrades.Level(item.Level, item.MaxLevel);
                var effectWidth = Mathf.Min(skin.SmallGold.CalcSize(new GUIContent(effect)).x + 4f, line.width * 0.5f);
                skin.DrawIcon(new Rect(line.x, line.y + 6f, 16f, 16f), Icons.Check, UiSkin.Accent);
                var nameWidth = line.width - effectWidth - 34f;
                GUI.Label(new Rect(line.x + 24f, line.y + 3f, nameWidth, UiSkin.SmallLine), FishCard.Fit(item.Name, skin.SmallBold, nameWidth), skin.SmallBold);
                GUI.Label(new Rect(line.xMax - effectWidth, line.y + 3f, effectWidth, UiSkin.SmallLine), FishCard.Fit(effect, skin.SmallGold, effectWidth), skin.SmallGold);
            }

            GUI.EndScrollView();
        }

        private static void DrawHeader(UiSkin skin, Rect rect, string title, string right)
        {
            var rightWidth = right == null ? 0f : skin.SmallMuted.CalcSize(new GUIContent(right)).x + 4f;
            GUI.Label(new Rect(rect.x, rect.y + 4f, rect.width - rightWidth - 8f, 24f), FishCard.Fit(title, skin.BodyBold, rect.width - rightWidth - 8f), skin.BodyBold);
            if (right != null)
            {
                GUI.Label(new Rect(rect.xMax - rightWidth, rect.y + 6f, rightWidth, UiSkin.SmallLine), right, skin.SmallMuted);
            }
        }
    }
}
