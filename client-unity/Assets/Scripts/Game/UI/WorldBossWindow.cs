using System;
using System.Collections.Generic;
using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Visual;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.WorldBoss;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The "Boss Mundial" window (M24-T29, addendum A-165), on the Placa do Píer with the "i": the Cardume on the left
    /// attacking the boss on the right (damage numbers floating up, as in the Arena replay), the day's damage with the band
    /// bar, the six bands and their rewards, "Ranking de hoje" and the Dólares of the top 10. Outside the event: the countdown
    /// and the last event's result.
    /// </summary>
    /// <remarks>
    /// Shows what the service returned in the last sync (GameRoot.WorldBoss); the attacks happen in the service, also with
    /// this window closed. Art is provisional (ASSET_PENDENTE: the boss art Resources/Arte/Boss/boss_mundial and the icon
    /// ico_boss; requests in docs/ASSETS_PENDENTES.md).
    /// </remarks>
    public sealed class WorldBossWindow
    {
        private const float StageHeight = 300f, Gap = 12f, SideWidth = 360f;
        private const string BossArt = "Boss/boss_mundial";
        private const string BossIcon = "boss";
        private const float HitSeconds = 0.9f;

        private readonly GameRoot _root;
        private readonly List<(float at, int position, long damage)> _hits = new List<(float at, int position, long damage)>();
        private int _attacksSeen = -1;

        public WorldBossWindow(GameRoot root)
        {
            _root = root;
        }

        public bool IsOpen { get; private set; }

        /// <summary>"‹ Eventos" in the header: the Hud closes this window and opens the Eventos list (M24-T39, A-167).</summary>
        public Action BackToEvents { get; set; }

        /// <summary>The boss icon (window sign, Eventos card): ico_boss when it exists, otherwise the attack icon.</summary>
        public static string NavIcon => ArtAssets.Icon(BossIcon) != null ? BossIcon : Icons.Attack;

        public void Open()
        {
            IsOpen = true;
            _attacksSeen = -1;
            _hits.Clear();
        }

        public void Close() => IsOpen = false;

        public void Draw(UiSkin skin, float screenWidth, float screenHeight)
        {
            if (!IsOpen)
            {
                return;
            }

            var view = _root.WorldBoss;
            var info = view == null ? null : GameTexts.WorldBoss.Info(StartTime(view), view.DurationMinutes, Format.Factor(view.AttackIntervalSeconds));
            var counter = view == null ? null : view.Open ? GameTexts.WorldBoss.CounterOpen(Format.Countdown(view.SecondsLeft)) : GameTexts.WorldBoss.Counter(Format.Countdown(view.SecondsToStart));
            var area = WindowFrame.Draw(skin, screenWidth, screenHeight, GameTexts.WorldBoss.Title, info, out var closed, 1320f, 840f, NavIcon, counter);
            if (closed)
            {
                Close();
                return;
            }

            // Back to the Eventos list, in the free strip of the header (after the counter, left of Fechar).
            if (BackToEvents != null && WindowFrame.Side.width >= 120f)
            {
                var back = WindowFrame.TakeSide(Mathf.Min(170f, WindowFrame.Side.width));
                if (GUI.Button(new Rect(back.x, back.y + 2f, back.width, back.height - 4f), GameTexts.Events.Back, skin.Button))
                {
                    BackToEvents();
                    return;
                }
            }

            if (view == null)
            {
                return;
            }

            TrackHits(view);
            var main = new Rect(area.x, area.y, area.width - SideWidth - Gap, area.height);
            var side = new Rect(main.xMax + Gap, area.y, SideWidth, area.height);
            DrawStage(skin, new Rect(main.x, main.y, main.width, StageHeight), view);
            var below = new Rect(main.x, main.y + StageHeight + Gap, main.width, main.height - StageHeight - Gap);
            if (view.Open || view.LastResult == null)
            {
                DrawProgress(skin, below, view);
            }
            else
            {
                DrawLastResult(skin, below, view);
            }

            DrawRanking(skin, side, view);
        }

        internal static string StartTime(WorldBossView view) => Format.Time(new DateTime(2000, 1, 1, view.StartHour, view.StartMinute, 0));

        /// <summary>A new attack (the count went up) makes every fish's share float up from the boss.</summary>
        private void TrackHits(WorldBossView view)
        {
            if (_attacksSeen >= 0 && view.Open && view.Attacks > _attacksSeen)
            {
                foreach (var fish in view.Team)
                {
                    if (fish != null && fish.LastDamage > 0)
                    {
                        _hits.Add((Time.unscaledTime, fish.Position, fish.LastDamage));
                    }
                }
            }

            _attacksSeen = view.Attacks;
            _hits.RemoveAll(h => Time.unscaledTime - h.at > HitSeconds);
        }

        // ------------------------------------------------------------------ stage: Cardume vs Boss

        private void DrawStage(UiSkin skin, Rect rect, WorldBossView view)
        {
            // Cardume against the boss is a dark scene on the diary sheet, in night colours (A-180).
            WindowFrame.Photo(skin, rect);
            using var night = UiSkin.NightScope();
            GUI.Box(rect, GUIContent.none, skin.PanelSolid);
            skin.Inset(rect);
            var half = rect.width / 2f;
            var left = new Rect(rect.x + 16f, rect.y + 12f, half - 70f, rect.height - 24f);
            var right = new Rect(rect.x + half + 54f, rect.y + 12f, half - 70f, rect.height - 24f);
            GUI.Label(new Rect(left.x, left.y, left.width, 26f), GameTexts.WorldBoss.YourCardume, skin.BodyBold);
            DrawCardume(skin, new Rect(left.x, left.y + 30f, left.width, left.height - 30f), view);

            // The middle: the VS disc during the event; the countdown outside it.
            var cx = rect.center.x;
            if (view.Open)
            {
                var d = 76f;
                var disc = new Rect(cx - d / 2f, rect.center.y - d / 2f, d, d);
                skin.DrawGlow(disc, UiSkin.Gold, 0.35f);
                GUI.DrawTexture(disc, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.96f), 0, d / 2f);
                GUI.DrawTexture(disc, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Gold, 2f, d / 2f);
                GUI.Label(disc, GameTexts.Arena.VsMark, skin.VsMark);
            }

            DrawBoss(skin, right, view);
            if (!view.Open)
            {
                var band = new Rect(rect.x + 20f, rect.yMax - 64f, rect.width - 40f, 52f);
                GUI.DrawTexture(band, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.88f), 0, 10f);
                GUI.Label(new Rect(band.x, band.y + 4f, band.width, 26f), GameTexts.WorldBoss.StartsIn(Format.Countdown(view.SecondsToStart)), skin.TitleCenter);
                GUI.Label(new Rect(band.x, band.y + 30f, band.width, UiSkin.SmallLine), GameTexts.WorldBoss.Daily(StartTime(view), view.DurationMinutes) + " · " + GameTexts.WorldBoss.ClosedHint, skin.SmallMutedCenter);
            }
        }

        /// <summary>The six positions, two columns (front row nearer the boss), fish facing right; a small lunge on each attack.</summary>
        private void DrawCardume(UiSkin skin, Rect rect, WorldBossView view)
        {
            if (view.AttackBlocker != ServiceError.None)
            {
                var text = view.AttackBlocker == ServiceError.CardumeLocked ? GameTexts.WorldBoss.CardumeLocked : GameTexts.WorldBoss.CardumeEmpty;
                skin.DrawIcon(new Rect(rect.x, rect.y + 4f, 22f, 22f), Icons.Warning, UiSkin.Danger);
                GUI.Label(new Rect(rect.x + 30f, rect.y, rect.width - 30f, 48f), text, skin.SmallGold);
                rect = new Rect(rect.x, rect.y + 52f, rect.width, rect.height - 52f);
            }

            var colW = rect.width / 2f;
            var rowH = rect.height / 3f;
            var lunge = view.Open && _hits.Count > 0 ? Mathf.Sin(Mathf.Clamp01((Time.unscaledTime - _hits[_hits.Count - 1].at) / 0.18f) * Mathf.PI) : 0f;
            for (var i = 0; i < view.Team.Count && i < 6; i++)
            {
                var fish = view.Team[i];
                var front = i < 3;
                var x = rect.x + (front ? colW : 0f) + (front ? lunge * 16f : 0f);
                var y = rect.y + (i % 3) * rowH;
                var cell = new Rect(x + 6f, y + 2f, colW - 12f, rowH - 4f);
                if (fish == null)
                {
                    GUI.DrawTexture(new Rect(cell.x + 10f, cell.y + 6f, cell.width - 20f, cell.height - 12f), skin.White, ScaleMode.StretchToFill, true, 0, new Color(1f, 1f, 1f, 0.04f), 0, 8f);
                    continue;
                }

                var art = new Rect(cell.x, cell.y, cell.width, cell.height - UiSkin.SmallLine);
                var previous = GUI.color;
                GUI.color = previous * (view.AttackBlocker == ServiceError.None ? Color.white : new Color(1f, 1f, 1f, 0.4f));
                ArenaWindow.DrawFish(art, fish.SpeciesId, false);
                GUI.color = previous;
                var label = fish.SpeciesName + " · " + GameTexts.Player.LevelShort + " " + fish.Level;
                GUI.Label(new Rect(cell.x, cell.yMax - UiSkin.SmallLine, cell.width, UiSkin.SmallLine), FishCard.Fit(label, skin.SmallMutedCenter, cell.width), skin.SmallMutedCenter);
            }
        }

        /// <summary>The boss (provisional art until boss_mundial exists), its name, "Vida infinita", and the damage floating up.</summary>
        private void DrawBoss(UiSkin skin, Rect rect, WorldBossView view)
        {
            GUI.Label(new Rect(rect.x, rect.y, rect.width, 26f), FishCard.Fit(view.BossName, skin.BodyBold, rect.width), skin.BodyBold);
            var body = new Rect(rect.x, rect.y + 30f, rect.width, rect.height - 30f - 34f);
            var tex = ArtAssets.Texture(BossArt);
            var shake = view.Open && _hits.Count > 0 && Time.unscaledTime - _hits[_hits.Count - 1].at < 0.15f ? 4f : 0f;
            var drawn = new Rect(body.x + shake, body.y, body.width, body.height);
            if (tex != null)
            {
                GUI.DrawTexture(drawn, tex, ScaleMode.ScaleToFit, true);
            }
            else
            {
                // Clean placeholder: a deep disc with the attack icon (ASSET_PENDENTE).
                var d = Mathf.Min(drawn.width, drawn.height) - 8f;
                var disc = new Rect(drawn.center.x - d / 2f, drawn.center.y - d / 2f, d, d);
                skin.DrawGlow(disc, UiSkin.Danger, view.Open ? 0.45f : 0.2f);
                GUI.DrawTexture(disc, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.95f), 0, d / 2f);
                GUI.DrawTexture(disc, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Danger, 2f, d / 2f);
                skin.DrawIcon(new Rect(disc.center.x - d * 0.25f, disc.center.y - d * 0.25f, d * 0.5f, d * 0.5f), Icons.Attack, UiSkin.Danger);
            }

            skin.AccentPill(new Rect(rect.center.x - 90f, rect.yMax - 30f, 180f, 28f), GameTexts.WorldBoss.InfiniteHp, UiSkin.Danger);

            // Damage numbers float up and fade (as in the Arena replay), spread by position.
            foreach (var hit in _hits)
            {
                var t = Time.unscaledTime - hit.at;
                var x = body.center.x - 120f + ((hit.position - 1) % 3) * 80f + ((hit.position - 1) / 3) * 30f;
                var y = body.y + 30f + ((hit.position - 1) / 3) * 40f - t * 50f;
                var c = GUI.color;
                GUI.color = c * new Color(1f, 1f, 1f, 1f - t / HitSeconds);
                UiSkin.ShadowLabel(new Rect(x - 50f, y, 100f, 24f), GameTexts.WorldBoss.DamagePop(Format.Short(hit.damage)), skin.SmallGoldCenter);
                GUI.color = c;
            }
        }

        // ------------------------------------------------------------------ today: damage, bands, reward

        private void DrawProgress(UiSkin skin, Rect rect, WorldBossView view)
        {
            skin.Inset(rect);
            var x = rect.x + 20f;
            var w = rect.width - 40f;
            var y = rect.y + 14f;
            GUI.Label(new Rect(x, y, w * 0.5f, 24f), GameTexts.WorldBoss.DamageToday, skin.SmallMuted);
            var right = view.Open ? GameTexts.WorldBoss.TimeLeft(Format.Countdown(view.SecondsLeft)) : GameTexts.WorldBoss.Waiting;
            GUI.Label(new Rect(x + w * 0.5f, y, w * 0.5f, 24f), right, view.Open ? skin.SmallGoldRight : skin.SmallMutedRight);
            y += 24f;
            GUI.Label(new Rect(x, y, w * 0.6f, 38f), Format.Number(view.Damage), skin.NumberBig);
            GUI.Label(new Rect(x + w * 0.5f, y + 8f, w * 0.5f, 24f), GameTexts.WorldBoss.Attacks(Format.Number(view.Attacks), Format.Number(view.MaxAttacks)), skin.SmallMutedRight);
            y += 44f;

            // The band bar: 0 to the day's maximum, a tick at each band.
            var bar = new Rect(x, y, w, 14f);
            var fraction = view.ReferenceDamage > 0 ? Mathf.Clamp01((float)(view.Damage / (double)view.ReferenceDamage)) : 0f;
            skin.Bar(bar, fraction, true);
            foreach (var band in view.Bands)
            {
                var tx = bar.x + bar.width * Mathf.Clamp01((float)(band.DamagePercent / 100.0));
                GUI.DrawTexture(new Rect(tx - 1f, bar.y - 4f, 2f, bar.height + 8f), skin.White, ScaleMode.StretchToFill, true, 0, band.Reached ? UiSkin.Gold : UiSkin.Border, 0, 0);
            }

            y += 22f;
            string next;
            if (view.NextBand == 0)
            {
                next = GameTexts.WorldBoss.AllBands;
            }
            else
            {
                var nb = view.Bands[view.NextBand - 1];
                next = nb.ThresholdDamage > 0
                    ? GameTexts.WorldBoss.NextBand(nb.Number, Format.Number(nb.ThresholdDamage))
                    : GameTexts.WorldBoss.NextBandPercent(nb.Number, Format.PercentShort(nb.DamagePercent / 100.0));
            }

            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), next, view.NextBand == 0 ? skin.SmallGoldLine : skin.SmallMuted);
            y += 30f;

            GUI.Label(new Rect(x, y, w, 24f), GameTexts.WorldBoss.Bands, skin.BodyBold);
            y += 28f;
            var count = Mathf.Max(1, view.Bands.Count);
            var cardGap = 8f;
            var cardW = (w - cardGap * (count - 1)) / count;
            var cardH = Mathf.Max(120f, rect.yMax - y - 64f);
            for (var i = 0; i < view.Bands.Count; i++)
            {
                DrawBandCard(skin, new Rect(x + i * (cardW + cardGap), y, cardW, cardH), view.Bands[i], view.Band);
            }

            y += cardH + 10f;
            var reward = view.Band > 0 ? view.Bands[view.Band - 1] : null;
            var line = reward == null ? GameTexts.WorldBoss.NoReward : GameTexts.WorldBoss.YourReward + ": " + RewardItems(reward.Coins, reward.Shells, reward.ClothingFragments, reward.BoatFragments, reward.BoatName, 0);
            GUI.Label(new Rect(x, y, w, 40f), line, reward == null ? skin.SmallMuted : skin.SmallGold);
        }

        private static void DrawBandCard(UiSkin skin, Rect rect, WorldBossBandView band, int reached)
        {
            var current = band.Number == reached;
            GUI.Box(rect, GUIContent.none, current ? skin.CardSelected : band.Reached ? skin.CardImportant : skin.Card);
            var x = rect.x + 10f;
            var w = rect.width - 20f;
            var y = rect.y + 8f;
            GUI.Label(new Rect(x, y, w, 22f), GameTexts.WorldBoss.Band(band.Number), band.Reached ? skin.SmallGold : skin.SmallBold);
            if (band.Reached)
            {
                skin.DrawIcon(new Rect(rect.xMax - 28f, y + 2f, 18f, 18f), Icons.Check, UiSkin.Gold);
            }

            y += 22f;
            var need = band.ThresholdDamage > 0 ? GameTexts.WorldBoss.BandThreshold(Format.Short(band.ThresholdDamage)) : GameTexts.WorldBoss.BandPercent(Format.PercentShort(band.DamagePercent / 100.0));
            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine * 2f), need, skin.SmallMuted);
            y += UiSkin.SmallLine * 2f + 4f;
            y = RewardLine(skin, x, y, w, Icons.Coin, band.Coins > 0 ? Format.Short(band.Coins) : null);
            y = RewardLine(skin, x, y, w, Icons.Shell, band.Shells > 0 ? Format.Number(band.Shells) : null);
            y = RewardLine(skin, x, y, w, Icons.Star, band.ClothingFragments > 0 ? GameTexts.Pass.ClothingFragments(Format.Number(band.ClothingFragments)) : null);
            RewardLine(skin, x, y, w, Icons.Boat, band.BoatFragments > 0 ? GameTexts.Pass.Fragments(Format.Number(band.BoatFragments), band.BoatName) : null);
        }

        private static float RewardLine(UiSkin skin, float x, float y, float w, string icon, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return y;
            }

            skin.DrawIcon(new Rect(x, y + 2f, 16f, 16f), icon, UiSkin.Gold);
            GUI.Label(new Rect(x + 22f, y, w - 22f, UiSkin.SmallLine), FishCard.Fit(text, skin.Small, w - 22f), skin.Small);
            return y + UiSkin.SmallLine + 2f;
        }

        internal static string RewardItems(long coins, long shells, long clothing, long boatFragments, string boat, long dollars)
        {
            var items = new List<string>();
            if (coins > 0) items.Add(GameTexts.WorldBoss.ItemCoins(Format.Short(coins)));
            if (shells > 0) items.Add(GameTexts.WorldBoss.ItemShells(Format.Number(shells)));
            if (clothing > 0) items.Add(GameTexts.WorldBoss.ItemClothing(Format.Number(clothing)));
            if (boatFragments > 0) items.Add(GameTexts.WorldBoss.ItemBoat(Format.Number(boatFragments), boat));
            if (dollars > 0) items.Add(GameTexts.WorldBoss.ItemDollars(Format.Number(dollars)));
            return items.Count > 0 ? string.Join(", ", items) : GameTexts.WorldBoss.LastResultNothing;
        }

        // ------------------------------------------------------------------ outside the event: the last result

        private void DrawLastResult(UiSkin skin, Rect rect, WorldBossView view)
        {
            skin.Inset(rect);
            var r = view.LastResult;
            var x = rect.x + 20f;
            var w = rect.width - 40f;
            var y = rect.y + 16f;
            GUI.Label(new Rect(x, y, w, 30f), GameTexts.WorldBoss.LastResultTitle(Format.Date(r.Date)), skin.Heading);
            y += 40f;
            GUI.Label(new Rect(x, y, w, 38f), Format.Number(r.Damage), skin.NumberBig);
            y += 40f;
            GUI.Label(new Rect(x, y, w, 24f), GameTexts.WorldBoss.LastResultDamage(Format.Number(r.Damage), Format.Number(r.Attacks)), skin.SmallMuted);
            y += 30f;
            var status = GameTexts.WorldBoss.LastResultBand(r.Band) + (r.Rank > 0 ? " · " + GameTexts.WorldBoss.LastResultRank(r.Rank) : string.Empty);
            GUI.Label(new Rect(x, y, w, 26f), status, skin.BodyBold);
            y += 34f;
            GUI.Label(new Rect(x, y, w, 44f), RewardItems(r.Coins, r.Shells, r.ClothingFragments, r.BoatFragments, r.BoatName, r.Dollars), r.IsEmpty ? skin.SmallMuted : skin.SmallGold);
            y += 56f;
            skin.Divider(new Rect(x, y, w, 1f));
            y += 14f;

            // What the next event pays, band by band.
            GUI.Label(new Rect(x, y, w, 24f), GameTexts.WorldBoss.Bands, skin.BodyBold);
            y += 28f;
            var count = Mathf.Max(1, view.Bands.Count);
            var cardGap = 8f;
            var cardW = (w - cardGap * (count - 1)) / count;
            var cardH = Mathf.Max(110f, rect.yMax - y - 14f);
            for (var i = 0; i < view.Bands.Count; i++)
            {
                DrawBandCard(skin, new Rect(x + i * (cardW + cardGap), y, cardW, cardH), view.Bands[i], 0);
            }
        }

        // ------------------------------------------------------------------ ranking and the Dólares of the top 10

        private static void DrawRanking(UiSkin skin, Rect rect, WorldBossView view)
        {
            skin.Inset(rect);
            var x = rect.x + 16f;
            var w = rect.width - 32f;
            var y = rect.y + 14f;
            skin.DrawIcon(new Rect(x, y + 3f, 20f, 20f), Icons.Ranking, UiSkin.Gold);
            GUI.Label(new Rect(x + 28f, y, w - 28f, 26f), GameTexts.WorldBoss.RankingToday, skin.BodyBold);
            y += 34f;
            if (view.Ranking.Count == 0)
            {
                GUI.Label(new Rect(x, y, w, UiSkin.SmallLine * 2f), GameTexts.WorldBoss.RankingEmpty, skin.SmallMuted);
                y += UiSkin.SmallLine * 2f + 6f;
            }

            foreach (var row in view.Ranking)
            {
                var line = new Rect(x, y, w, 30f);
                GUI.DrawTexture(line, skin.White, ScaleMode.StretchToFill, true, 0, row.IsPlayer ? new Color(UiSkin.Gold.r, UiSkin.Gold.g, UiSkin.Gold.b, 0.14f) : new Color(1f, 1f, 1f, 0.04f), 0, 6f);
                GUI.Label(new Rect(line.x + 8f, line.y + 5f, 34f, 20f), GameTexts.WorldBoss.DollarsRank(row.Rank), skin.SmallBold);
                var name = row.Name + (row.IsPlayer ? " " + GameTexts.WorldBoss.You : string.Empty);
                GUI.Label(new Rect(line.x + 44f, line.y + 5f, w - 44f - 110f, 20f), FishCard.Fit(name, skin.Small, w - 44f - 110f), skin.Small);
                GUI.Label(new Rect(line.xMax - 110f, line.y + 5f, 102f, 20f), Format.Short(row.Damage), skin.SmallGoldRight);
                y += 34f;
            }

            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine * 2f), GameTexts.WorldBoss.RankingLocalNote, skin.SmallMuted);
            y += UiSkin.SmallLine * 2f + 10f;
            skin.Divider(new Rect(x, y, w, 1f));
            y += 12f;

            skin.DrawIcon(new Rect(x, y + 3f, 20f, 20f), Icons.Dollar, UiSkin.Gold);
            GUI.Label(new Rect(x + 28f, y, w - 28f, 26f), GameTexts.WorldBoss.DollarsTitle, skin.BodyBold);
            y += 32f;
            var half = w / 2f;
            var rows = (view.DollarsByRank.Count + 1) / 2;
            for (var i = 0; i < view.DollarsByRank.Count; i++)
            {
                var col = i / Mathf.Max(1, rows);
                var row = i % Mathf.Max(1, rows);
                var cell = new Rect(x + col * half, y + row * 26f, half - 8f, 24f);
                GUI.Label(new Rect(cell.x, cell.y, 40f, 22f), GameTexts.WorldBoss.DollarsRank(i + 1), skin.SmallBold);
                GUI.Label(new Rect(cell.x + 40f, cell.y, cell.width - 40f, 22f), GameTexts.WorldBoss.DollarsAmount(Format.Number(view.DollarsByRank[i])), view.OnlineRewards ? skin.SmallGold : skin.SmallMuted);
            }

            y += rows * 26f + 6f;
            if (!view.OnlineRewards)
            {
                GUI.Label(new Rect(x, y, w, UiSkin.SmallLine * 2f), GameTexts.WorldBoss.DollarsOffline, skin.SmallMuted);
            }
        }
    }
}
