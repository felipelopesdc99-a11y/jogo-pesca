using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Visual;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Pass;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The "Passe do Pescador" window (M24-T17, addendum A-158): the player's level and the next milestone, the premium
    /// (buy with Dólares, or "Premium ativo"), "Resgatar tudo", and a horizontal track of milestones with two rows, Grátis
    /// and Premium. Each cell shows its reward and Resgatar / Resgatado / the padlock / the level it needs.
    /// </summary>
    /// <remarks>
    /// Shows what the pass service returns; nothing here decides a reward or whether it can be claimed. Only the columns
    /// in view are drawn (the track has 100). Art (Resources/Arte): ico_passe, the fragment icons in Fragmentos/, and since
    /// A-179 the strip passe_faixa behind the summary and the seal passe_premium_selo of the Premium row; each falls back to
    /// the drawn look when its file is missing.
    /// </remarks>
    public sealed class PassWindow
    {
        private const float SummaryHeight = 104f, Gap = 12f, LabelColumn = 118f, Column = 168f, ColumnGap = 10f, HeaderHeight = 44f;
        private const float RefreshSeconds = 0.5f, WheelStep = 48f;

        /// <summary>The fragment icon of each boat (Resources/Arte/Fragmentos/frag_&lt;boat id&gt;). Missing = the boat icon.</summary>
        private const string FragmentFolder = "Fragmentos/frag_";

        /// <summary>The Fragmento de Roupa icon (M24-T33). Missing = the star icon.</summary>
        private const string ClothingFragmentArt = "Fragmentos/frag_roupa";

        /// <summary>The pass icon (ico_passe.png; the star if the file is missing).</summary>
        private const string PassIcon = "passe";

        // A-179: the sunset strip behind the summary (taller, with a navy veil under the light text) and the seal of
        // the Premium row. Missing files = the plain summary and the star / padlock icon.
        private const string BannerArt = "UI/passe_faixa", PremiumSealArt = "UI/passe_premium_selo";
        private const float SummaryArtHeight = 150f, SealSize = 72f;

        private readonly GameRoot _root;
        private PassView _view;
        private float _nextRefresh;
        private Vector2 _scroll;
        private bool _scrollPending;

        public PassWindow(GameRoot root)
        {
            _root = root;
        }

        public bool IsOpen { get; private set; }

        /// <summary>The pass button's icon: ico_passe when it exists, otherwise the star.</summary>
        public static string NavIcon => ArtAssets.Icon(PassIcon) != null ? PassIcon : Icons.Star;

        public void Open()
        {
            IsOpen = true;
            _nextRefresh = 0f;
            _scrollPending = true;
        }

        public void Close() => IsOpen = false;

        public void Draw(UiSkin skin, float screenWidth, float screenHeight)
        {
            if (!IsOpen)
            {
                return;
            }

            if (_view == null || Time.unscaledTime >= _nextRefresh)
            {
                Refresh();
            }

            var view = _view;
            var info = view == null ? null : GameTexts.Pass.Info(view.Milestones.Count > 0 ? view.Milestones[0].Level : 10, Format.Number(view.PremiumPriceDollars));
            var counter = view == null ? null : GameTexts.Player.LevelShort + view.FisherLevel;
            var area = WindowFrame.Draw(skin, screenWidth, screenHeight, GameTexts.Pass.Title, info, out var closed, 1320f, 840f, NavIcon, counter);
            if (closed)
            {
                Close();
                return;
            }

            if (view == null)
            {
                return;
            }

            var banner = ArtAssets.Texture(BannerArt);
            var summaryHeight = banner != null ? SummaryArtHeight : SummaryHeight;
            var gap = banner != null ? Gap + 6f : Gap;
            DrawSummary(skin, new Rect(area.x, area.y, area.width, summaryHeight), view, banner != null);
            DrawTrack(skin, new Rect(area.x, area.y + summaryHeight + gap, area.width, area.height - summaryHeight - gap), view);
        }

        private void Refresh()
        {
            _view = _root.IsRunning ? _root.Game.Pass.GetPass() : null;
            _nextRefresh = Time.unscaledTime + RefreshSeconds;
        }

        // ------------------------------------------------------------------ summary

        /// <summary>
        /// Left: the level and the next milestone with its bar. Middle: the premium. Right: "Resgatar tudo". With the
        /// strip art, it is drawn on the photo in night colours, over a navy veil (A-179).
        /// </summary>
        private void DrawSummary(UiSkin skin, Rect rect, PassView view, bool onArt)
        {
            if (onArt && WindowFrame.PhotoArt(skin, rect, BannerArt) != null)
            {
                // Strong where the text is (level, next milestone, the Dólares line), lighter on the right, where only
                // the "Resgatar tudo" button sits.
                var split = rect.width * 0.72f;
                WindowFrame.Veil(skin, new Rect(rect.x, rect.y, split, rect.height), 0.84f, 0.72f);
                WindowFrame.Veil(skin, new Rect(rect.x + split, rect.y, rect.width - split, rect.height), 0.72f, 0.3f);
                using (UiSkin.NightScope())
                {
                    DrawSummaryBody(skin, new Rect(rect.x, rect.y + (rect.height - SummaryHeight) / 2f, rect.width, SummaryHeight), view);
                }

                return;
            }

            skin.Inset(rect);
            DrawSummaryBody(skin, rect, view);
        }

        private void DrawSummaryBody(UiSkin skin, Rect rect, PassView view)
        {
            var x = rect.x + 20f;
            var leftWidth = rect.width * 0.38f;
            GUI.Label(new Rect(x, rect.y + 12f, leftWidth, 30f), GameTexts.Pass.YourLevel(view.FisherLevel), skin.Heading);
            if (view.NextMilestoneLevel > 0)
            {
                var from = view.PreviousMilestoneLevel;
                var fraction = Mathf.Clamp01((view.FisherLevel - from) / (float)Mathf.Max(1, view.NextMilestoneLevel - from));
                GUI.Label(new Rect(x, rect.y + 46f, leftWidth, UiSkin.SmallLine), GameTexts.Pass.NextMilestone(view.NextMilestoneLevel), skin.SmallMuted);
                skin.Bar(new Rect(x, rect.y + 74f, leftWidth - 30f, 12f), fraction, true);
            }
            else
            {
                GUI.Label(new Rect(x, rect.y + 46f, leftWidth, UiSkin.SmallLine), GameTexts.Pass.AllReached, skin.SmallGoldLine);
            }

            // The premium: buy it, or the gold pill once bought.
            var premium = new Rect(rect.x + leftWidth + 20f, rect.y + 14f, rect.width * 0.32f, 44f);
            if (view.Premium)
            {
                skin.AccentPill(new Rect(premium.x, premium.y + 6f, premium.width, 32f), GameTexts.Pass.PremiumActive, UiSkin.Gold, Icons.Star);
            }
            else
            {
                var affordable = view.BuyPremiumBlocker == ServiceError.None;
                var enabled = GUI.enabled;
                GUI.enabled = enabled && affordable;
                if (skin.IconButton(premium, Icons.Dollar, GameTexts.Pass.BuyPremium(Format.Number(view.PremiumPriceDollars)), affordable ? skin.ButtonPrimary : skin.Button))
                {
                    _root.BuyPassPremium();
                    Refresh();
                }

                GUI.enabled = enabled;
                var style = affordable ? skin.SmallGold : skin.SmallMuted;
                GUI.Label(new Rect(premium.x, premium.yMax + 8f, premium.width, UiSkin.SmallLine), FishCard.Fit(GameTexts.Pass.YourDollars(Format.Number(view.Dollars)), style, premium.width), style);
            }

            // Claim everything that is ready.
            var all = new Rect(rect.xMax - 20f - 250f, rect.y + 14f, 250f, 44f);
            var ready = view.ClaimableCount > 0;
            var was = GUI.enabled;
            GUI.enabled = was && ready;
            if (skin.IconButton(all, Icons.Check, ready ? GameTexts.Pass.ClaimAllCount(view.ClaimableCount) : GameTexts.Pass.NothingToClaim, ready ? skin.ButtonPrimary : skin.Button))
            {
                _root.ClaimAllPass();
                Refresh();
            }

            GUI.enabled = was;
        }

        // ------------------------------------------------------------------ the track

        /// <summary>The fixed row labels on the left and the scrolling milestones on the right.</summary>
        private void DrawTrack(UiSkin skin, Rect rect, PassView view)
        {
            var count = view.Milestones.Count;
            var rowsTop = rect.y + HeaderHeight + 8f;
            var rowHeight = (rect.yMax - 20f - rowsTop - Gap) / 2f;

            // Row labels: Grátis, Premium (with the padlock until it is bought).
            var freeLabel = new Rect(rect.x, rowsTop, LabelColumn - 10f, rowHeight);
            var premiumLabel = new Rect(rect.x, rowsTop + rowHeight + Gap, LabelColumn - 10f, rowHeight);
            RowLabel(skin, freeLabel, GameTexts.Pass.Free, null, UiSkin.Accent);
            if (!PremiumSeal(skin, premiumLabel, view.Premium))
            {
                RowLabel(skin, premiumLabel, GameTexts.Pass.PremiumRow, view.Premium ? Icons.Star : Icons.Lock, UiSkin.Gold);
            }

            var track = new Rect(rect.x + LabelColumn, rect.y, rect.width - LabelColumn, rect.height);
            var contentWidth = count * (Column + ColumnGap);
            if (_scrollPending)
            {
                // Open on the first reward ready to claim, otherwise on the next milestone.
                _scrollPending = false;
                var target = 0;
                for (var i = 0; i < count; i++)
                {
                    var m = view.Milestones[i];
                    if (m.Free.Claimable || m.Premium.Claimable || !m.Reached)
                    {
                        target = i;
                        break;
                    }
                }

                _scroll.x = Mathf.Clamp(target * (Column + ColumnGap) - Column, 0f, Mathf.Max(0f, contentWidth - track.width));
            }

            // The mouse wheel scrolls the track sideways.
            if (Event.current.type == EventType.ScrollWheel && track.Contains(Event.current.mousePosition))
            {
                _scroll.x = Mathf.Clamp(_scroll.x + Event.current.delta.y * WheelStep, 0f, Mathf.Max(0f, contentWidth - track.width));
                Event.current.Use();
            }

            _scroll = GUI.BeginScrollView(track, _scroll, new Rect(0f, 0f, contentWidth, track.height - 20f), true, false);
            var first = Mathf.Max(0, Mathf.FloorToInt(_scroll.x / (Column + ColumnGap)) - 1);
            var last = Mathf.Min(count - 1, Mathf.CeilToInt((_scroll.x + track.width) / (Column + ColumnGap)) + 1);
            for (var i = first; i <= last; i++)
            {
                var m = view.Milestones[i];
                var cx = i * (Column + ColumnGap);
                var isNext = m.Level == view.NextMilestoneLevel;
                Header(skin, new Rect(cx, 0f, Column, HeaderHeight), m, isNext);
                Cell(skin, new Rect(cx, rowsTop - rect.y, Column, rowHeight), m, m.Free, PassTrack.Free);
                Cell(skin, new Rect(cx, rowsTop - rect.y + rowHeight + Gap, Column, rowHeight), m, m.Premium, PassTrack.Premium);
            }

            GUI.EndScrollView();
        }

        private static void RowLabel(UiSkin skin, Rect rect, string text, string icon, Color color)
        {
            GUI.Box(rect, GUIContent.none, skin.Card);
            GUI.DrawTexture(new Rect(rect.x + 6f, rect.y + 10f, 4f, rect.height - 20f), skin.White, ScaleMode.StretchToFill, true, 0, color, 0, 2f);
            var y = rect.center.y - 14f;
            if (icon != null)
            {
                skin.DrawIcon(new Rect(rect.center.x - 11f, y - 26f, 22f, 22f), icon, color);
            }

            GUI.Label(new Rect(rect.x + 12f, y, rect.width - 16f, 28f), text, skin.CenterBold);
        }

        /// <summary>
        /// The Premium row label with its seal (passe_premium_selo, A-179): the seal over the word, dimmed with a padlock
        /// until the premium is bought. Returns false when the file is missing (the caller draws the plain label).
        /// </summary>
        private static bool PremiumSeal(UiSkin skin, Rect rect, bool premium)
        {
            var seal = ArtAssets.Texture(PremiumSealArt);
            if (seal == null)
            {
                return false;
            }

            GUI.Box(rect, GUIContent.none, skin.Card);
            GUI.DrawTexture(new Rect(rect.x + 6f, rect.y + 10f, 4f, rect.height - 20f), skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Gold, 0, 2f);
            var size = Mathf.Min(SealSize, rect.width - 24f, rect.height - 56f);
            var top = rect.center.y - (size + 6f + 28f) / 2f;
            var sealRect = new Rect(rect.x + 4f + (rect.width - 4f - size) / 2f, top, size, size);
            GUI.DrawTexture(sealRect, seal, ScaleMode.ScaleToFit, true, 0, premium ? Color.white : new Color(1f, 1f, 1f, 0.5f), 0, 0);
            if (!premium)
            {
                var lockSize = Mathf.Round(size * 0.36f);
                var lockRect = new Rect(sealRect.xMax - lockSize + 2f, sealRect.yMax - lockSize + 2f, lockSize, lockSize);
                GUI.DrawTexture(lockRect, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.OnSheet(new Color(0.06f, 0.11f, 0.2f, 0.92f)), 0, lockSize / 2f);
                skin.DrawIconExact(new Rect(lockRect.x + lockSize * 0.2f, lockRect.y + lockSize * 0.2f, lockSize * 0.6f, lockSize * 0.6f), Icons.Lock, Color.white);
            }

            GUI.Label(new Rect(rect.x + 12f, sealRect.yMax + 6f, rect.width - 16f, 28f), GameTexts.Pass.PremiumRow, skin.CenterBold);
            return true;
        }

        /// <summary>"Nv.40" over the column: gold when reached, outlined in turquoise when it is the next one.</summary>
        private static void Header(UiSkin skin, Rect rect, PassMilestoneView m, bool isNext)
        {
            var pill = new Rect(rect.x + 20f, rect.y + 6f, rect.width - 40f, rect.height - 12f);
            if (isNext)
            {
                skin.DrawGlow(pill, UiSkin.Accent, 0.35f);
            }

            var color = m.Reached ? UiSkin.Gold : isNext ? UiSkin.Accent : UiSkin.Muted;
            skin.AccentPill(pill, GameTexts.Pass.MilestoneLevel(m.Level), color);
        }

        /// <summary>One reward: its lines, then Resgatar / Resgatado / the padlock / the level it needs.</summary>
        private void Cell(UiSkin skin, Rect rect, PassMilestoneView m, PassRewardView reward, PassTrack track)
        {
            GUI.Box(rect, GUIContent.none, skin.Card);
            if (reward.Claimable)
            {
                GUI.DrawTexture(rect, skin.White, ScaleMode.StretchToFill, true, 0, track == PassTrack.Premium ? UiSkin.Gold : UiSkin.Accent, 2f, 10f);
            }

            var dim = reward.Claimed || reward.Locked || !m.Reached;
            var previous = GUI.color;
            if (dim)
            {
                GUI.color = previous * new Color(1f, 1f, 1f, reward.Claimed ? 0.55f : 0.75f);
            }

            var x = rect.x + 12f;
            var w = rect.width - 24f;
            var y = rect.y + 12f;

            // Up to six lines (M24-T32/T33 added rod and clothing fragments): they get closer when the cell is short.
            var lines = (reward.Coins > 0 ? 1 : 0) + (reward.Shells > 0 ? 1 : 0) + (reward.Dollars > 0 ? 1 : 0) + (reward.Fragments > 0 ? 1 : 0)
                + (reward.RodFragments > 0 ? 1 : 0) + (reward.ClothingFragments > 0 ? 1 : 0);
            var step = Mathf.Clamp((rect.height - 70f) / Mathf.Max(1, lines), 22f, 26f);
            if (reward.IsEmpty)
            {
                GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), GameTexts.Pass.Empty, skin.SmallMuted);
            }

            if (reward.Coins > 0)
            {
                skin.CoinIcon(new Rect(x, y, 20f, 20f));
                GUI.Label(new Rect(x + 26f, y, w - 26f, UiSkin.SmallLine), FishCard.Fit(Format.Short(reward.Coins), skin.SmallGoldLine, w - 26f), skin.SmallGoldLine);
                y += step;
            }

            if (reward.Shells > 0)
            {
                skin.CurrencyIcon(new Rect(x, y, 20f, 20f), Icons.Shell);
                GUI.Label(new Rect(x + 26f, y, w - 26f, UiSkin.SmallLine), Format.Number(reward.Shells), skin.Small);
                y += step;
            }

            if (reward.Dollars > 0)
            {
                skin.CurrencyIcon(new Rect(x, y, 20f, 20f), Icons.Dollar);
                var text = GameTexts.Pass.DollarsLine(Format.Number(reward.Dollars)) + (reward.DollarsAlreadyPaid ? " · " + GameTexts.Pass.DollarsPaid : string.Empty);
                var style = reward.DollarsAlreadyPaid ? skin.SmallMuted : skin.Small;
                GUI.Label(new Rect(x + 26f, y, w - 26f, UiSkin.SmallLine), FishCard.Fit(text, style, w - 26f), style);
                y += step;
            }

            if (reward.Fragments > 0)
            {
                // frag_boat_NN (art delivered 13/10/2026); the boat icon if the file is missing.
                var art = ArtAssets.Texture(FragmentFolder + reward.BoatId);
                if (art != null)
                {
                    GUI.DrawTexture(new Rect(x, y, 20f, 20f), art, ScaleMode.ScaleToFit, true);
                }
                else
                {
                    skin.DrawIcon(new Rect(x, y, 20f, 20f), Icons.Boat, UiSkin.Accent);
                }

                var text = GameTexts.Pass.Fragments(Format.Number(reward.Fragments), reward.BoatName);
                GUI.Label(new Rect(x + 26f, y, w - 26f, UiSkin.SmallLine), FishCard.Fit(text, skin.Small, w - 26f), skin.Small);
                y += step;
            }

            if (reward.RodFragments > 0)
            {
                // frag_rod_NN (art delivered 13/10/2026); the rod icon if the file is missing.
                var art = ArtAssets.Texture(FragmentFolder + reward.RodId);
                if (art != null)
                {
                    GUI.DrawTexture(new Rect(x, y, 20f, 20f), art, ScaleMode.ScaleToFit, true);
                }
                else
                {
                    skin.DrawIcon(new Rect(x, y, 20f, 20f), Icons.Rod, UiSkin.Accent);
                }

                var text = GameTexts.Pass.RodFragments(Format.Number(reward.RodFragments), reward.RodName);
                GUI.Label(new Rect(x + 26f, y, w - 26f, UiSkin.SmallLine), FishCard.Fit(text, skin.Small, w - 26f), skin.Small);
                y += step;
            }

            if (reward.ClothingFragments > 0)
            {
                // frag_roupa (art delivered 13/10/2026); the star icon if the file is missing.
                var art = ArtAssets.Texture(ClothingFragmentArt);
                if (art != null)
                {
                    GUI.DrawTexture(new Rect(x, y, 20f, 20f), art, ScaleMode.ScaleToFit, true);
                }
                else
                {
                    skin.DrawIcon(new Rect(x, y, 20f, 20f), Icons.Star, UiSkin.Accent);
                }

                var text = GameTexts.Pass.ClothingFragments(Format.Number(reward.ClothingFragments));
                GUI.Label(new Rect(x + 26f, y, w - 26f, UiSkin.SmallLine), FishCard.Fit(text, skin.Small, w - 26f), skin.Small);
            }

            GUI.color = previous;

            // The bottom of the cell.
            var bottom = new Rect(rect.x + 10f, rect.yMax - 46f, rect.width - 20f, 36f);
            if (reward.Claimed)
            {
                skin.AccentPill(bottom, GameTexts.Pass.Claimed, UiSkin.Muted, Icons.Check);
            }
            else if (reward.Locked)
            {
                skin.AccentPill(bottom, GameTexts.Pass.Locked, UiSkin.Gold, Icons.Lock);
            }
            else if (!m.Reached)
            {
                GUI.Label(bottom, GameTexts.Pass.MilestoneLevel(m.Level), skin.SmallMutedCenter);
            }
            else if (skin.IconButton(bottom, Icons.Check, GameTexts.Pass.Claim, track == PassTrack.Premium ? skin.ButtonReward : skin.ButtonPrimary))
            {
                _root.ClaimPass(m.Level, track);
                Refresh();
            }
        }
    }
}
