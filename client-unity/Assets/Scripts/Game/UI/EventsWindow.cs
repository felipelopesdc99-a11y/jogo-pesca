using System;
using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Visual;
using FishingIdle.GameService.WorldBoss;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The "Eventos" window (M24-T39, addendum A-167), on the Placa do Píer with the "i": the game's events as cards in a
    /// scrolling list. Today there is one, the Boss Mundial: its boss, the daily time, the state (countdown, happening now
    /// with the time left, or the last result), the band reached today and "Abrir", which opens the Boss window.
    /// </summary>
    /// <remarks>
    /// Shows what the service returned in the last sync (GameRoot.WorldBoss); decides nothing. A new event adds its kind to
    /// <see cref="EventKind"/> and one card drawer. Icon provisional (ASSET_PENDENTE: ico_eventos; request in
    /// docs/ASSETS_PENDENTES.md).
    /// </remarks>
    public sealed class EventsWindow
    {
        private const float CardHeight = 196f, CardPad = 6f, CardGap = 12f, ScrollBar = 18f, ArtSize = 112f, OpenWidth = 168f;

        private enum EventKind
        {
            WorldBoss,
        }

        private static readonly EventKind[] Kinds = { EventKind.WorldBoss };

        private readonly GameRoot _root;
        private Vector2 _scroll;

        public EventsWindow(GameRoot root)
        {
            _root = root;
        }

        public bool IsOpen { get; private set; }

        /// <summary>"Abrir" on the Boss Mundial card: the Hud closes this window and opens the Boss window.</summary>
        public Action OpenBoss { get; set; }

        /// <summary>The menu and sign icon: ico_eventos when it exists, otherwise the clock.</summary>
        public static string NavIcon => ArtAssets.Icon(Icons.Events) != null ? Icons.Events : Icons.Clock;

        public void Open()
        {
            IsOpen = true;
            _scroll = Vector2.zero;
        }

        public void Close() => IsOpen = false;

        public void Draw(UiSkin skin, float screenWidth, float screenHeight)
        {
            if (!IsOpen)
            {
                return;
            }

            var area = WindowFrame.Draw(skin, screenWidth, screenHeight, GameTexts.Events.Title, GameTexts.Events.Info, out var closed, 1040f, 720f, NavIcon, GameTexts.Events.Counter(Happening()));
            if (closed)
            {
                Close();
                return;
            }

            var contentHeight = Kinds.Length * (CardHeight + CardGap) - CardGap + CardPad * 2f;
            var scrolls = contentHeight > area.height;
            var inner = area.width - (scrolls ? ScrollBar : 0f);
            _scroll = GUI.BeginScrollView(area, _scroll, new Rect(0f, 0f, inner, Mathf.Max(contentHeight, area.height - 1f)));
            for (var i = 0; i < Kinds.Length; i++)
            {
                var card = new Rect(CardPad, CardPad + i * (CardHeight + CardGap), inner - CardPad * 2f, CardHeight);
                switch (Kinds[i])
                {
                    case EventKind.WorldBoss:
                        DrawBossCard(skin, card, _root.WorldBoss);
                        break;
                }
            }

            GUI.EndScrollView();
        }

        /// <summary>How many events are happening now (the header counter and the gold dot of the menu button).</summary>
        private int Happening() => _root.WorldBoss != null && _root.WorldBoss.Open ? 1 : 0;

        private void DrawBossCard(UiSkin skin, Rect rect, WorldBossView view)
        {
            var open = view != null && view.Open;
            if (open)
            {
                skin.DrawGlow(rect, UiSkin.Gold, 0.25f + 0.1f * Mathf.Sin(Time.unscaledTime * 3f));
            }

            GUI.Box(rect, GUIContent.none, open ? skin.CardImportant : skin.Card);

            // The boss icon on a deep disc (ico_boss, or the attack icon while it is missing).
            var disc = new Rect(rect.x + 20f, rect.center.y - ArtSize / 2f, ArtSize, ArtSize);
            GUI.DrawTexture(disc, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.95f), 0, ArtSize / 2f);
            GUI.DrawTexture(disc, skin.White, ScaleMode.StretchToFill, true, 0, open ? UiSkin.Gold : UiSkin.Border, 2f, ArtSize / 2f);
            var icon = WorldBossWindow.NavIcon;
            skin.DrawIcon(new Rect(disc.center.x - 30f, disc.center.y - 30f, 60f, 60f), icon, icon == Icons.Attack ? UiSkin.Danger : Color.white);

            var button = new Rect(rect.xMax - 20f - OpenWidth, rect.center.y - 22f, OpenWidth, 44f);
            var x = disc.xMax + 20f;
            var w = button.x - 20f - x;
            var y = rect.y + 16f;

            // Title and, during the event, the "AGORA" pill.
            var titleWidth = Mathf.Min(w - (open ? 96f : 0f), skin.Heading.CalcSize(new GUIContent(GameTexts.WorldBoss.Title)).x + 4f);
            GUI.Label(new Rect(x, y, titleWidth, 30f), GameTexts.WorldBoss.Title, skin.Heading);
            if (open)
            {
                skin.AccentPill(new Rect(x + titleWidth + 12f, y + 4f, 84f, 24f), GameTexts.Events.Now, UiSkin.Gold);
            }

            y += 32f;
            if (view == null)
            {
                return;
            }

            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), FishCard.Fit(view.BossName, skin.BodyBold, w), skin.BodyBold);
            y += 24f;
            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), FishCard.Fit(GameTexts.Events.Schedule(WorldBossWindow.StartTime(view), view.DurationMinutes), skin.SmallMuted, w), skin.SmallMuted);
            y += 24f;

            // State: happening now, or the countdown to the next one.
            var state = open ? GameTexts.Events.Happening(Format.Countdown(view.SecondsLeft)) : GameTexts.Events.StartsIn(Format.Countdown(view.SecondsToStart));
            var stateStyle = open ? skin.SmallGold : skin.SmallBold;
            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), FishCard.Fit(state, stateStyle, w), stateStyle);
            y += 24f;

            // Today's band, then (outside the event) the last result.
            var bandStyle = view.Band > 0 ? skin.SmallGold : skin.Small;
            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), FishCard.Fit(GameTexts.Events.BandToday(view.Band), bandStyle, w), bandStyle);
            y += 24f;
            if (!open)
            {
                var r = view.LastResult;
                var last = r == null
                    ? GameTexts.Events.NoLastResult
                    : GameTexts.Events.LastResult(Format.Date(r.Date), GameTexts.WorldBoss.LastResultBand(r.Band),
                        WorldBossWindow.RewardItems(r.Coins, r.Shells, r.ClothingFragments, r.BoatFragments, r.BoatName, r.Dollars));
                GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), FishCard.Fit(last, skin.SmallMuted, w), skin.SmallMuted);
            }

            if (GUI.Button(button, GameTexts.Events.Open, skin.ButtonPrimary))
            {
                OpenBoss?.Invoke();
            }
        }
    }
}
