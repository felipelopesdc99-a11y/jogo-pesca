using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Scene;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Expeditions;
using FishingIdle.Game.Visual;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The Expedition menu (GDD section 32). When the Cardume is back, the report of what it brought
    /// opens on top of this window the next time the player opens it, and goes away once read
    /// (addendum A-082).
    /// </summary>
    public sealed class ExpeditionWindow
    {
        private readonly GameRoot _root;
        private ExpeditionsView _view;
        private float _nextRefresh;
        private bool _confirmCancel;

        public ExpeditionWindow(GameRoot root)
        {
            _root = root;
        }

        public bool IsOpen { get; private set; }

        public void Open()
        {
            IsOpen = true;
            _nextRefresh = 0f;
        }

        public void Close()
        {
            if (_confirmCancel)
            {
                _confirmCancel = false;
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

            if (Time.unscaledTime >= _nextRefresh)
            {
                _view = _root.GetExpeditions();
                _nextRefresh = Time.unscaledTime + 0.5f;
            }

            if (_view == null)
            {
                return;
            }

            // With a report waiting, the menu underneath is shown but inert until it is read.
            var report = _root.ExpeditionResult;
            var wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && report == null && !_confirmCancel;
            var area = WindowFrame.Draw(skin, screenWidth, screenHeight, GameTexts.Expedition.Title, GameTexts.Expedition.Note, out var closed, 1320f, 760f, Icons.Expedition);
            if (closed)
            {
                GUI.enabled = wasEnabled;
                Close();
                return;
            }

            skin.IconBadge(new Rect(area.x, area.y, 50, 50), Icons.Fish, UiSkin.Gold);
            GUI.Label(new Rect(area.x + 62, area.y + 2, 400, 22), GameTexts.Expedition.YourStrength, skin.SmallMuted);
            GUI.Label(new Rect(area.x + 62, area.y + 20, 500, 30), Format.Number(_view.CardumeStrength) + "  ·  " + GameTexts.Cardume.Filled(_view.CardumeFilled, _view.CardumeSize), skin.Number);

            var top = area.y + 64;
            var active = _view.Active;
            if (active != null)
            {
                var box = new Rect(area.x, top, area.width, 110);
                GUI.Box(box, GUIContent.none, skin.CardSelected);
                GUI.Label(new Rect(box.x + 20, box.y + 14, box.width - 260, 26), FishCard.Fit(GameTexts.Expedition.Away + ": " + active.Name, skin.Heading, box.width - 260), skin.Heading);
                if (GUI.Button(new Rect(box.xMax - 220, box.y + 10, 200, 34), GameTexts.Expedition.Cancel, skin.ButtonDanger))
                {
                    _confirmCancel = true;
                }
                skin.Bar(new Rect(box.x + 20, box.y + 48, box.width - 40, 12), (float)active.Progress);
                GUI.Label(new Rect(box.x + 20, box.y + 66, 300, 22), GameTexts.Expedition.ReturnsIn(Format.Countdown(active.SecondsLeft)), skin.Small);
                GUI.Label(new Rect(box.x + 320, box.y + 66, box.width - 340, 40), GameTexts.Expedition.LockNote, skin.SmallMuted);
                top += 126;
            }
            else if (_view.CardumeFilled == 0)
            {
                skin.DrawIcon(new Rect(area.x, top + 1, 20, 20), Icons.Info, UiSkin.Gold);
                GUI.Label(new Rect(area.x + 28, top, area.width - 28, 22), GameTexts.Expedition.NoCardume, skin.SmallGold);
                top += 32;
            }

            var count = _view.Expeditions.Count;
            var cardW = (area.width - (count - 1) * 16f) / Mathf.Max(1, count);
            for (var i = 0; i < count; i++)
            {
                DrawOffer(skin, new Rect(area.x + i * (cardW + 16f), top, cardW, area.yMax - top), _view.Expeditions[i]);
            }

            GUI.enabled = wasEnabled;
            if (report != null)
            {
                DrawReport(skin, area, report);
            }
            else if (_confirmCancel)
            {
                if (_view.Active == null)
                {
                    _confirmCancel = false;
                    return;
                }

                var dialog = WindowFrame.Dialog(skin, screenWidth, screenHeight, 220f);
                GUI.Label(new Rect(dialog.x + 28, dialog.y + 24, dialog.width - 56, 30), GameTexts.Expedition.CancelTitle, skin.Heading);
                GUI.Label(new Rect(dialog.x + 28, dialog.y + 64, dialog.width - 56, 70), GameTexts.Expedition.CancelBody, skin.Body);
                if (GUI.Button(new Rect(dialog.x + 28, dialog.yMax - 64, 190, 42), GameTexts.Expedition.KeepGoing, skin.Button))
                {
                    _confirmCancel = false;
                }

                if (GUI.Button(new Rect(dialog.xMax - 238, dialog.yMax - 64, 210, 42), GameTexts.Expedition.Cancel, skin.ButtonDanger))
                {
                    _confirmCancel = false;
                    _root.CancelExpedition();
                    _nextRefresh = 0f;
                }
            }
        }

        private void DrawOffer(UiSkin skin, Rect rect, ExpeditionOfferView e)
        {
            GUI.Box(rect, GUIContent.none, skin.Card);
            var x = rect.x + 18;
            var w = rect.width - 36;
            var y = rect.y + 16;

            // Each expedition has its own landscape (Art Bible, section 23).
            var picture = ArtAssets.Texture("Expedicoes/" + e.ExpeditionId);
            if (picture != null)
            {
                var pr = new Rect(rect.x + 10, rect.y + 10, rect.width - 20, Mathf.Min(180f, (rect.width - 20) / 1.54f));
                GUI.DrawTexture(pr, picture, ScaleMode.ScaleAndCrop, true, 0, Color.white, 0, 10);
                y = pr.yMax + 12;
            }

            GUI.Label(new Rect(x, y, w, 28), e.Name, skin.Heading);
            y += 30;
            skin.DrawIcon(new Rect(x, y + 1, 18, 18), Icons.Clock, UiSkin.Muted);
            GUI.Label(new Rect(x + 24, y, w - 24, 20), GameTexts.Expedition.Duration(Format.Duration(e.DurationMinutes * 60)), skin.SmallMuted);
            y += 30;

            Row(skin, x, ref y, w, GameTexts.Expedition.Recommended, Format.Number(e.RecommendedStrength));
            Row(skin, x, ref y, w, GameTexts.Expedition.Efficiency, Format.Percent(e.Efficiency, 0));
            Row(skin, x, ref y, w, GameTexts.Expedition.Reward, Format.Number(e.ExpectedCoins));
            Row(skin, x, ref y, w, GameTexts.Expedition.FishChance, Format.Percent(e.FishFindChance, 1));
            skin.Bar(new Rect(x, y + 6, w, 8), (float)Mathf.Clamp01((float)e.Efficiency / 1.5f), e.Efficiency >= 1.0);

            var button = new Rect(x, rect.yMax - 58, w, 42);
            if (e.StartBlocker == ServiceError.None)
            {
                if (skin.IconButton(button, Icons.Arrow, GameTexts.Expedition.Send, skin.ButtonPrimary))
                {
                    _root.StartExpedition(e.ExpeditionId);
                    _nextRefresh = 0f;
                }
            }
            else if (e.StartBlocker != ServiceError.ExpeditionActive)
            {
                skin.DrawIcon(new Rect(x, button.y + 11, 20, 20), Icons.Lock, UiSkin.Gold);
                GUI.Label(new Rect(x + 28, button.y + 2, w - 28, 42), GameTexts.ServiceErrorMessage(e.StartBlocker.ToString()), skin.SmallGold);
            }
        }

        private static void Row(UiSkin skin, float x, ref float y, float w, string label, string value)
        {
            GUI.Label(new Rect(x, y, w * 0.6f, 20), label, skin.SmallMuted);
            GUI.Label(new Rect(x + w * 0.4f, y, w * 0.6f, 20), value, skin.SmallRight);
            y += 24;
        }

        /// <summary>
        /// The report of the Expedition that came back: where it went and when it returned, the coins
        /// and the fish it found, shown prominently (GDD section 32). "Ótimo!" marks it as read.
        /// </summary>
        private void DrawReport(UiSkin skin, Rect area, ExpeditionResultView report)
        {
            var fish = report.FoundFish;
            var height = fish != null ? 590f : 400f;
            var width = 660f;
            GUI.DrawTexture(new Rect(area.x - 20, area.y - 20, area.width + 40, area.height + 40), skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.6f), 0, 14);
            var rect = new Rect(area.center.x - width / 2f, area.center.y - height / 2f, width, height);
            skin.DrawShadow(rect);
            GUI.Box(rect, GUIContent.none, skin.PanelSolid);

            // The landscape of the Expedition across the top.
            var y = rect.y + 14;
            var picture = !string.IsNullOrEmpty(report.ExpeditionId) ? ArtAssets.Texture("Expedicoes/" + report.ExpeditionId) : null;
            if (picture != null)
            {
                GUI.DrawTexture(new Rect(rect.x + 14, y, rect.width - 28, 96), picture, ScaleMode.ScaleAndCrop, true, 0, Color.white, 0, 10);
                y += 108;
            }

            var x = rect.x + 30;
            var w = rect.width - 60;
            GUI.Label(new Rect(x, y, w, 18), GameTexts.Expedition.ReportLabel, skin.SmallGold);
            GUI.Label(new Rect(x, y + 18, w, 34), GameTexts.Expedition.ResultTitle, skin.Title);
            GUI.Label(new Rect(x, y + 54, w, 20), GameTexts.Expedition.ReturnedAt(report.Name, Format.DateTimeFromUnixMs(report.CompletedAtMs)), skin.SmallMuted);
            y += 86;

            GUI.Label(new Rect(x, y, w, 22), GameTexts.Expedition.Brought, skin.BodyBold);
            y += 28;
            skin.CoinIcon(new Rect(x, y + 2, 26, 26));
            GUI.Label(new Rect(x + 34, y, w * 0.6f, 30), GameTexts.Expedition.Coins(Format.Number(report.Coins)), skin.Number);
            GUI.Label(new Rect(x + w * 0.5f, y + 6, w * 0.5f, 20), GameTexts.Expedition.Efficiency + " " + Format.Percent(report.Efficiency, 0), skin.SmallMutedRight);
            y += 40;

            if (fish != null)
            {
                GUI.Label(new Rect(x, y, w, 22), GameTexts.Expedition.FoundFish, skin.Heading);
                y += 30;
                var model = new FishCardModel
                {
                    SpeciesId = fish.SpeciesId,
                    Name = fish.SpeciesName,
                    Line = VisualTheme.IsSpecialSize(fish.SizeCategoryId) ? Format.SizeCm(fish.SizeCm) : Format.SizeCm(fish.SizeCm) + " · " + fish.SizeCategoryName,
                    RarityId = fish.RarityId,
                    RarityName = fish.RarityName,
                    SizeCategoryId = fish.SizeCategoryId,
                    SizeCategoryName = fish.SizeCategoryName,
                    Bar = (float)fish.SizePercentile,
                    Coins = Format.Number(fish.SalePriceCoins),
                };
                FishCard.StatusBadge(model, fish.IsNewSpecies, fish.IsPersonalRecord);
                var card = new Rect(rect.center.x - 106, y, 212, 196);
                skin.DrawGlow(card, UiSkin.RarityColor(fish.RarityId), 0.3f);
                FishCard.Draw(skin, card, model);
                y += 204;
            }
            else
            {
                GUI.Label(new Rect(x, y, w, 22), report.FoundAFish ? GameTexts.Expedition.FoundFish : GameTexts.Expedition.NoFish, skin.SmallMuted);
                y += 30;
            }

            GUI.Label(new Rect(x, rect.yMax - 58, w - 220, 40), GameTexts.Expedition.ReportNote, skin.SmallMuted);
            if (skin.IconButton(new Rect(rect.xMax - 230, rect.yMax - 62, 200, 42), Icons.Check, GameTexts.Expedition.Collect, skin.ButtonReward))
            {
                _root.AcknowledgeExpedition();
            }
        }
    }
}
