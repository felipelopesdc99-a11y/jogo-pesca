using System;
using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Visual;
using FishingIdle.GameService.WorldBoss;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The "Eventos" window (M24-T39, addendum A-167), on a "Diário de bordo" page (A-170) with the "i": the game's events as cards in a
    /// scrolling list. Today there is one, the Boss Mundial: its boss, the daily time, the state (countdown, happening now
    /// with the time left, or the last result), the band reached today and "Abrir", which opens the Boss window.
    /// </summary>
    /// <remarks>
    /// Shows what the service returned in the last sync (GameRoot.WorldBoss); decides nothing. A new event adds its kind to
    /// <see cref="EventKind"/> and one card drawer. Icon provisional (ASSET_PENDENTE: ico_eventos; request in
    /// docs/ASSETS_PENDENTES.md). From A-179 to A-190 the cards sat on the cork of a notice board; since A-190 they sit
    /// on the plain panel.
    /// </remarks>
    public sealed class EventsWindow
    {
        private const float CardHeight = 196f, CardPad = 6f, CardGap = 12f, ScrollBar = 18f, ArtSize = 112f, OpenWidth = 168f;

        // A-190: the notice board of A-179 (eventos_quadro) left; the cards sit on the plain panel.

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
        // One icon per idea (A-190): Eventos is the calendar everywhere; the bell (ico_avisos, and the ribbon bell ico_eventos)
        // stays only with the Avisos.
        public static string NavIcon => ArtAssets.Icon(CalendarIcon) != null ? CalendarIcon : ArtAssets.Icon(Icons.Events) != null ? Icons.Events : Icons.Clock;

        private const string CalendarIcon = "atalho_eventos";

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

            // A plain list on the panel since A-190 (the cork board, eventos_quadro, is no longer drawn: too much decoration
            // for one event).
            var list = area;
            var cardHeight = CardHeight;
            var contentHeight = Kinds.Length * (cardHeight + CardGap) - CardGap + CardPad * 2f;
            var scrolls = contentHeight > list.height;
            var inner = list.width - (scrolls ? ScrollBar : 0f);
            _scroll = GUI.BeginScrollView(list, _scroll, new Rect(0f, 0f, inner, Mathf.Max(contentHeight, list.height - 1f)));
            // The list starts at the top of the panel.
            const float top = 0f;
            for (var i = 0; i < Kinds.Length; i++)
            {
                var card = new Rect(CardPad, top + CardPad + i * (cardHeight + CardGap), inner - CardPad * 2f, cardHeight);
                switch (Kinds[i])
                {
                    case EventKind.WorldBoss:
                        DrawBossCard(skin, card, _root.WorldBoss, false);
                        break;
                }
            }

            GUI.EndScrollView();
        }

        /// <summary>How many events are happening now (the header counter and the gold dot of the menu button).</summary>
        private int Happening() => _root.WorldBoss != null && _root.WorldBoss.Open ? 1 : 0;

        private void DrawBossCard(UiSkin skin, Rect rect, WorldBossView view, bool tall)
        {
            var open = view != null && view.Open;
            GUI.Box(rect, GUIContent.none, open ? skin.CardImportant : skin.Card);

            // The boss icon on a deep disc (ico_boss, or the attack icon while it is missing).
            var disc = new Rect(rect.x + 20f, rect.center.y - ArtSize / 2f, ArtSize, ArtSize);
            // The ARC disc (gold, dark gap, dark gold); its outer line turns reward gold while the event happens.
            skin.FramedDisc(disc, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.95f), open ? UiSkin.Gold : UiSkin.FrameGold);
            var icon = WorldBossWindow.NavIcon;
            skin.DrawIconExact(new Rect(disc.center.x - 32f, disc.center.y - 32f, 64f, 64f), icon, icon == Icons.Attack ? UiSkin.Danger : Color.white);

            // On the board the card is narrower and taller: "Abrir" goes to the bottom right, under the text.
            var button = tall
                ? new Rect(rect.xMax - 20f - OpenWidth, rect.yMax - 16f - 44f, OpenWidth, 44f)
                : new Rect(rect.xMax - 20f - OpenWidth, rect.center.y - 22f, OpenWidth, 44f);
            var x = disc.xMax + 20f;
            var w = (tall ? rect.xMax : button.x) - 20f - x;
            var y = rect.y + 16f;

            // One title (A-190): the boss's name and, during the event, the "AGORA" pill; "Boss Mundial" is the small line
            // under it (it was a third title level).
            var title = view != null ? view.BossName : GameTexts.WorldBoss.Title;
            var titleWidth = Mathf.Min(w - (open ? 96f : 0f), skin.Heading.CalcSize(new GUIContent(title)).x + 4f);
            GUI.Label(new Rect(x, y, titleWidth, 30f), FishCard.Fit(title, skin.Heading, titleWidth), skin.Heading);
            if (open)
            {
                skin.AccentPill(new Rect(x + titleWidth + 12f, y + 4f, 84f, 24f), GameTexts.Events.Now, UiSkin.Gold);
            }

            y += 32f;
            if (view == null)
            {
                return;
            }

            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), GameTexts.WorldBoss.Title, skin.SmallMuted);
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
