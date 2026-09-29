using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Scene;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Expeditions;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>The Expedition menu (GDD section 32) and its completion dialog.</summary>
    public sealed class ExpeditionWindow
    {
        private readonly GameRoot _root;
        private ExpeditionsView _view;
        private float _nextRefresh;

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

        public void Close() => IsOpen = false;

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

            var area = WindowFrame.Draw(skin, screenWidth, screenHeight, GameTexts.Expedition.Title, GameTexts.Expedition.Note, out var closed, 1320f, 760f);
            if (closed)
            {
                Close();
                return;
            }

            GUI.Label(new Rect(area.x, area.y, 400, 22), GameTexts.Expedition.YourStrength, skin.SmallMuted);
            GUI.Label(new Rect(area.x, area.y + 20, 400, 30), Format.Number(_view.CardumeStrength) + "  ·  " + GameTexts.Cardume.Filled(_view.CardumeFilled, _view.CardumeSize), skin.Number);

            var top = area.y + 64;
            var active = _view.Active;
            if (active != null)
            {
                var box = new Rect(area.x, top, area.width, 110);
                GUI.Box(box, GUIContent.none, skin.CardSelected);
                GUI.Label(new Rect(box.x + 20, box.y + 14, box.width - 40, 26), GameTexts.Expedition.Away + ": " + active.Name, skin.Heading);
                skin.Bar(new Rect(box.x + 20, box.y + 48, box.width - 40, 12), (float)active.Progress);
                GUI.Label(new Rect(box.x + 20, box.y + 66, 300, 22), GameTexts.Expedition.ReturnsIn(Format.Countdown(active.SecondsLeft)), skin.Small);
                GUI.Label(new Rect(box.x + 320, box.y + 66, box.width - 340, 40), GameTexts.Expedition.LockNote, skin.SmallMuted);
                top += 126;
            }
            else if (_view.CardumeFilled == 0)
            {
                GUI.Label(new Rect(area.x, top, area.width, 22), GameTexts.Expedition.NoCardume, skin.SmallGold);
                top += 32;
            }

            var count = _view.Expeditions.Count;
            var cardW = (area.width - (count - 1) * 16f) / Mathf.Max(1, count);
            for (var i = 0; i < count; i++)
            {
                DrawOffer(skin, new Rect(area.x + i * (cardW + 16f), top, cardW, area.yMax - top), _view.Expeditions[i]);
            }
        }

        private void DrawOffer(UiSkin skin, Rect rect, ExpeditionOfferView e)
        {
            GUI.Box(rect, GUIContent.none, skin.Card);
            var x = rect.x + 18;
            var w = rect.width - 36;
            var y = rect.y + 16;

            GUI.Label(new Rect(x, y, w, 28), e.Name, skin.Heading);
            y += 30;
            GUI.Label(new Rect(x, y, w, 20), GameTexts.Expedition.Duration(Format.Duration(e.DurationMinutes * 60)), skin.SmallMuted);
            y += 34;

            Row(skin, x, ref y, w, GameTexts.Expedition.Recommended, Format.Number(e.RecommendedStrength));
            Row(skin, x, ref y, w, GameTexts.Expedition.Efficiency, Format.Percent(e.Efficiency, 0));
            Row(skin, x, ref y, w, GameTexts.Expedition.Reward, Format.Number(e.ExpectedCoins));
            Row(skin, x, ref y, w, GameTexts.Expedition.FishChance, Format.Percent(e.FishFindChance, 1));
            skin.Bar(new Rect(x, y + 6, w, 8), (float)Mathf.Clamp01((float)e.Efficiency / 1.5f), e.Efficiency >= 1.0);

            var button = new Rect(x, rect.yMax - 58, w, 42);
            if (e.StartBlocker == ServiceError.None)
            {
                if (GUI.Button(button, GameTexts.Expedition.Send, skin.ButtonPrimary))
                {
                    _root.StartExpedition(e.ExpeditionId);
                    _nextRefresh = 0f;
                }
            }
            else if (e.StartBlocker != ServiceError.ExpeditionActive)
            {
                GUI.Label(button, GameTexts.ServiceErrorMessage(e.StartBlocker.ToString()), skin.SmallGold);
            }
        }

        private static void Row(UiSkin skin, float x, ref float y, float w, string label, string value)
        {
            GUI.Label(new Rect(x, y, w * 0.6f, 20), label, skin.SmallMuted);
            GUI.Label(new Rect(x + w * 0.4f, y, w * 0.6f, 20), value, skin.SmallRight);
            y += 24;
        }

        /// <summary>The completion popup; found fish shown prominently (GDD section 32).</summary>
        public static void DrawResult(UiSkin skin, GameRoot root, float screenWidth, float screenHeight)
        {
            var result = root.ExpeditionResult;
            if (result == null || root.WelcomeBack != null)
            {
                return;
            }

            var fish = result.FoundFish;
            var rect = WindowFrame.Dialog(skin, screenWidth, screenHeight, fish != null ? 400f : 260f, 620f);
            var x = rect.x + 30;
            var w = rect.width - 60;
            GUI.Label(new Rect(x, rect.y + 24, w, 32), GameTexts.Expedition.ResultTitle, skin.Title);
            GUI.Label(new Rect(x, rect.y + 62, w, 22), result.Name + " · " + GameTexts.Expedition.Efficiency + " " + Format.Percent(result.Efficiency, 0), skin.SmallMuted);
            GUI.Label(new Rect(x, rect.y + 94, w, 30), GameTexts.Expedition.Coins(Format.Number(result.Coins)), skin.Number);

            if (fish != null)
            {
                GUI.Label(new Rect(x, rect.y + 136, w, 24), GameTexts.Expedition.FoundFish, skin.Heading);
                GUI.DrawTexture(new Rect(x + w / 2f - 110, rect.y + 168, 220, 110), Art.FishTexture(fish.SpeciesId), ScaleMode.ScaleToFit, true);
                GUI.Label(new Rect(x, rect.y + 284, w, 22), GameTexts.Expedition.InBox(fish.SpeciesName, Format.SizeCm(fish.SizeCm)), skin.Small);
            }
            else
            {
                GUI.Label(new Rect(x, rect.y + 136, w, 24), GameTexts.Expedition.NoFish, skin.SmallMuted);
            }

            if (GUI.Button(new Rect(rect.xMax - 230, rect.yMax - 62, 200, 42), GameTexts.Expedition.Collect, skin.ButtonPrimary))
            {
                root.AcknowledgeExpedition();
            }
        }
    }
}
