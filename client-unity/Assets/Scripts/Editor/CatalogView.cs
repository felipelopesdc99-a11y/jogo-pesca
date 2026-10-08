using System.Collections.Generic;
using System.Linq;
using FishingIdle.Game.Scene;
using FishingIdle.Game.Visual;
using FishingIdle.GameService.Config;
using FishingIdle.Texts;
using UnityEditor;
using UnityEngine;
using T = FishingIdle.Texts.GameTexts.DevPanel;

namespace FishingIdle.Editor
{
    /// <summary>
    /// The Development Panel's content browser (M18-T09): cards with the art and the key numbers of every
    /// fish, map, rod, boat, bait and Expedition, read from /config. Read-only: numbers are edited in the
    /// Balance tab.
    /// </summary>
    internal sealed class CatalogView
    {
        private static readonly string[] Sections = { T.CatalogFish, T.CatalogMaps, T.CatalogRods, T.CatalogBoats, T.CatalogBaits, T.CatalogExpeditions };
        private const float CardWidth = 220f;
        private int _section;
        private string _search = string.Empty;
        private Vector2 _scroll;

        public void Draw(GameConfig config)
        {
            if (config == null)
            {
                EditorGUILayout.HelpBox(T.ConfigInvalid, MessageType.Error);
                return;
            }

            _section = GUILayout.Toolbar(_section, Sections);
            if (_section == 0)
            {
                _search = EditorGUILayout.TextField(T.CatalogSearch, _search);
            }

            EditorGUILayout.Space(4);
            var cards = Cards(config).ToList();
            var columns = Mathf.Max(1, Mathf.FloorToInt((EditorGUIUtility.currentViewWidth - 30f) / (CardWidth + 10f)));
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (var i = 0; i < cards.Count; i += columns)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (var c = i; c < Mathf.Min(cards.Count, i + columns); c++)
                    {
                        DrawCard(cards[c]);
                    }

                    GUILayout.FlexibleSpace();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private sealed class Card
        {
            public string Title;
            public string Subtitle;
            public Texture2D Art;
            public Color Accent = new Color(0.15f, 0.77f, 0.76f);
            public readonly List<string> Lines = new List<string>();
        }

        private IEnumerable<Card> Cards(GameConfig config)
        {
            switch (_section)
            {
                case 1:
                    foreach (var map in config.Maps.Maps.OrderBy(m => m.UnlockFisherLevel))
                    {
                        var card = new Card { Title = map.DisplayName, Subtitle = map.Id, Art = ArtAssets.Texture(SceneTheme.For(map.Id).ArtPath("thumb")) };
                        card.Lines.Add(T.CatalogUnlock(map.UnlockFisherLevel));
                        card.Lines.Add(T.CatalogSpecies(map.FishPool.Count));
                        card.Lines.Add(T.CatalogRarities(string.Join(", ", map.AvailableRarities.Select(r => config.TryGetRarity(r, out var t) ? t.DisplayName : r))));
                        card.Lines.Add(T.CatalogExpedition(Format.Decimal(map.ExpeditionRewardMultiplier > 0 ? map.ExpeditionRewardMultiplier : 1, 1)));
                        yield return card;
                    }

                    break;
                case 2:
                    foreach (var rod in config.Rods.Rods.OrderBy(r => r.Tier))
                    {
                        var card = new Card { Title = rod.DisplayName, Subtitle = rod.Id + " · " + GameTexts.Profile.Tier(rod.Tier), Art = ArtAssets.Texture("Varas/" + rod.Id) };
                        card.Lines.Add(T.CatalogPrice(Format.Short(rod.Acquisition?.PurchaseCostCoins ?? 0), Format.Number(rod.Acquisition?.PurchaseCostShells ?? 0)));
                        card.Lines.Add(T.CatalogUnlock(rod.Acquisition?.UnlockFisherLevel ?? 1));
                        card.Lines.Add(T.CatalogMaxLevel(config.RodMaxLevel(rod)));
                        card.Lines.Add(T.CatalogCatches(string.Join(", ", (rod.CanCatchRarities ?? new List<string>()).Select(r => config.TryGetRarity(r, out var t) ? t.DisplayName : r))));
                        yield return card;
                    }

                    break;
                case 3:
                    foreach (var boat in config.Equipment.Boats.OrderBy(b => b.Tier))
                    {
                        var card = new Card { Title = boat.DisplayName, Subtitle = boat.Id, Art = ArtAssets.Texture("Barcos/" + boat.Id) };
                        card.Lines.Add(T.CatalogBonus(Format.Percent(boat.CatchSuccessBonus, 0)));
                        card.Lines.Add(T.CatalogPrice(Format.Short(boat.CostCoins), Format.Number(boat.CostShells)));
                        yield return card;
                    }

                    break;
                case 4:
                    foreach (var bait in config.Equipment.Baits.OrderBy(b => b.Tier))
                    {
                        var card = new Card { Title = bait.DisplayName, Subtitle = bait.Id, Art = ArtAssets.Texture("Iscas/" + bait.Id) };
                        card.Lines.Add(T.CatalogBonus(Format.Percent(bait.CatchSuccessBonus, 0)));
                        card.Lines.Add(T.CatalogCharges(bait.Charges));
                        card.Lines.Add(T.CatalogPrice(Format.Short(bait.CostCoins), Format.Number(bait.CostShells)));
                        yield return card;
                    }

                    break;
                case 5:
                    foreach (var e in config.Expeditions.Expeditions)
                    {
                        var card = new Card { Title = e.DisplayName, Subtitle = e.Id, Art = ArtAssets.Texture("Expedicoes/" + e.Id) };
                        card.Lines.Add(T.CatalogDuration(Format.Duration(e.DurationMinutes * 60.0)));
                        card.Lines.Add(T.CatalogStrength(Format.Number((long)e.RecommendedStrength)));
                        card.Lines.Add(T.CatalogReward(Format.Number(e.RewardCoins)));
                        card.Lines.Add(T.CatalogFindChance(Format.Percent(e.FishFindChance, 0)));
                        yield return card;
                    }

                    break;
                default:
                    foreach (var s in config.FishCatalog.Species)
                    {
                        if (!string.IsNullOrWhiteSpace(_search) && s.DisplayName.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            continue;
                        }

                        config.TryGetRarity(s.Rarity, out var rarity);
                        config.TryGetMap(s.PrimaryMapId, out var map);
                        var card = new Card
                        {
                            Title = s.DisplayName,
                            Subtitle = (rarity?.DisplayName ?? s.Rarity) + " · " + (map?.DisplayName ?? s.PrimaryMapId),
                            Art = Art.FishTexture(s.Id),
                            Accent = Game.UI.UiSkin.RarityColor(s.Rarity),
                        };
                        card.Lines.Add(T.CatalogSize(Format.SizeCm(s.SizeCm.Min), Format.SizeCm(s.SizeCm.Max)));
                        card.Lines.Add(T.CatalogValue(Format.Number((long)s.BaseSaleValueCoins)));
                        card.Lines.Add(T.CatalogXp(Format.Number((long)s.BaseFisherXp), Format.Number((long)s.BaseFeedXp)));
                        yield return card;
                    }

                    break;
            }
        }

        private static void DrawCard(Card card)
        {
            var rect = GUILayoutUtility.GetRect(CardWidth, CardWidth * 0.95f, GUILayout.Width(CardWidth), GUILayout.Height(CardWidth * 0.95f));
            EditorGUI.DrawRect(rect, new Color(0.13f, 0.18f, 0.24f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 3), card.Accent);
            if (card.Art != null)
            {
                GUI.DrawTexture(new Rect(rect.x + 8, rect.y + 10, rect.width - 16, 84), card.Art, ScaleMode.ScaleToFit, true);
            }

            var y = rect.y + 100;
            GUI.Label(new Rect(rect.x + 8, y, rect.width - 16, 18), card.Title, EditorStyles.boldLabel);
            y += 18;
            GUI.Label(new Rect(rect.x + 8, y, rect.width - 16, 16), card.Subtitle, EditorStyles.miniLabel);
            y += 18;
            foreach (var line in card.Lines)
            {
                GUI.Label(new Rect(rect.x + 8, y, rect.width - 16, 16), line, EditorStyles.miniLabel);
                y += 15;
            }

            GUILayout.Space(10);
        }
    }
}
