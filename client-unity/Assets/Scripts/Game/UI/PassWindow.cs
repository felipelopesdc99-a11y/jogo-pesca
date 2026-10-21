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
    /// and Premium. Each cell shows its reward as icon + number and one state: the Resgatar button, a faint check once
    /// claimed, a small padlock while the Premium is not bought (M25-T24, A-191).
    /// </summary>
    /// <remarks>
    /// Shows what the pass service returns; nothing here decides a reward or whether it can be claimed. Only the columns
    /// in view are drawn (the track has 100). Art (Resources/ArtePixel): ico_passe, the fragment icons in Fragmentos/, and since
    /// A-179 the strip passe_faixa behind the summary and the seal passe_premium_selo of the Premium row; each falls back to
    /// the drawn look when its file is missing.
    /// </remarks>
    public sealed class PassWindow
    {
        // Grid of 8 (A-190): 16 between the columns and the rows, the cells 168 wide.
        private const float SummaryHeight = 104f, Gap = 16f, LabelColumn = 128f, Column = 168f, ColumnGap = 16f, HeaderHeight = 40f;
        private const float RefreshSeconds = 0.5f, WheelStep = 48f;

        /// <summary>The fragment icon of each boat (Resources/ArtePixel/Fragmentos/frag_&lt;boat id&gt;). Missing = the boat icon.</summary>
        private const string FragmentFolder = "Fragmentos/frag_";

        /// <summary>The Fragmento de Roupa icon (M24-T33). Missing = the star icon.</summary>
        private const string ClothingFragmentArt = "Fragmentos/frag_roupa";

        /// <summary>The pass icon (ico_passe.png; the star if the file is missing).</summary>
        private const string PassIcon = "passe";

        // A-179: the sunset strip behind the summary (taller, with a navy veil under the light text) and the seal of
        // the Premium row. Missing files = the plain summary and the star / padlock icon.
        private const string BannerArt = "UI/passe_faixa", PremiumSealArt = "UI/passe_premium_selo";
        private const float SummaryArtHeight = 150f, SealSize = 72f;

        /// <summary>The height of passe_faixa (0–1 from the top) kept in the middle of the strip: the sun and the buoys.</summary>
        private const float BannerFocusY = 0.52f;

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
            var gap = banner != null ? Gap + 8f : Gap;
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
            // The pixel strip at a whole scale, cut at the edges, centred on the sun and the line of buoys (M25-T15).
            if (onArt && WindowFrame.PhotoArt(skin, rect, BannerArt, 0.5f, BannerFocusY) != null)
            {
                // Strong where the text is (level, next milestone, the Dólares line), lighter on the right, where only
                // the "Resgatar tudo" button sits.
                var split = rect.width * 0.72f;
                WindowFrame.Veil(skin, new Rect(rect.x, rect.y, split, rect.height), 0.84f, 0.72f);
                WindowFrame.Veil(skin, new Rect(rect.x + split, rect.y, rect.width - split, rect.height), 0.72f, 0.3f);
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

            // The premium: buy it, or the gold pill once bought. The button is as wide as its label (A-191, measured).
            var buyLabel = GameTexts.Pass.BuyPremium(Format.Number(view.PremiumPriceDollars));
            var buyWidth = Mathf.Min(rect.width * 0.36f, Mathf.Ceil(70f + skin.Button.CalcSize(new GUIContent(buyLabel)).x - skin.Button.padding.horizontal));
            var premium = new Rect(rect.x + leftWidth + 32f, rect.y + 16f, buyWidth, 48f);
            if (view.Premium)
            {
                var activeWidth = skin.PillWidth(GameTexts.Pass.PremiumActive, true) + 8f;
                skin.AccentPill(new Rect(premium.x, premium.y + 8f, activeWidth, 32f), GameTexts.Pass.PremiumActive, UiSkin.Gold, Icons.Star);
            }
            else
            {
                var affordable = view.BuyPremiumBlocker == ServiceError.None;
                var enabled = GUI.enabled;
                GUI.enabled = enabled && affordable;
                if (skin.IconButton(premium, Icons.Dollar, buyLabel, affordable ? skin.ButtonPrimary : skin.Button))
                {
                    _root.BuyPassPremium();
                    Refresh();
                }

                GUI.enabled = enabled;
                var style = affordable ? skin.SmallGold : skin.SmallMuted;
                GUI.Label(new Rect(premium.x, premium.yMax + 8f, premium.width, UiSkin.SmallLine), FishCard.Fit(GameTexts.Pass.YourDollars(Format.Number(view.Dollars)), style, premium.width), style);
            }

            // Claim everything that is ready (measured, the longer of its two labels).
            var ready = view.ClaimableCount > 0;
            var allLabel = ready ? GameTexts.Pass.ClaimAllCount(view.ClaimableCount) : GameTexts.Pass.NothingToClaim;
            var allWidth = Mathf.Ceil(70f + skin.Button.CalcSize(new GUIContent(allLabel)).x - skin.Button.padding.horizontal);
            var all = new Rect(rect.xMax - 24f - allWidth, rect.y + 16f, allWidth, 48f);
            var was = GUI.enabled;
            GUI.enabled = was && ready;
            if (skin.IconButton(all, Icons.Check, allLabel, ready ? skin.ButtonReward : skin.Button))
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
            var rowHeight = (rect.yMax - 24f - rowsTop - Gap) / 2f;

            // Row labels: Grátis, Premium (with the padlock until it is bought).
            var freeLabel = new Rect(rect.x, rowsTop, LabelColumn - Gap, rowHeight);
            var premiumLabel = new Rect(rect.x, rowsTop + rowHeight + Gap, LabelColumn - Gap, rowHeight);
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

            _scroll = GUI.BeginScrollView(track, _scroll, new Rect(0f, 0f, contentWidth, track.height - 24f), true, false);
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
            skin.ArcFill(new Rect(rect.x + 6f, rect.y + 10f, UiSkin.ArcPixel * 2f, rect.height - 20f), color, 0f);
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
            skin.ArcFill(new Rect(rect.x + 6f, rect.y + 10f, UiSkin.ArcPixel * 2f, rect.height - 20f), UiSkin.Gold, 0f);
            var size = Mathf.Min(SealSize, rect.width - 24f, rect.height - 56f);
            var top = rect.center.y - (size + 6f + 28f) / 2f;
            var sealRect = new Rect(rect.x + 4f + (rect.width - 4f - size) / 2f, top, size, size);
            PixelDraw.Fit(sealRect, seal, premium ? Color.white : new Color(1f, 1f, 1f, 0.5f));
            if (!premium)
            {
                var lockSize = Mathf.Round(size * 0.36f);
                var lockRect = new Rect(sealRect.xMax - lockSize + 2f, sealRect.yMax - lockSize + 2f, lockSize, lockSize);
                skin.PixelPill(lockRect, new Color(0.06f, 0.11f, 0.2f, 0.92f), UiSkin.FrameDark);
                skin.DrawIconExact(new Rect(lockRect.x + lockSize * 0.2f, lockRect.y + lockSize * 0.2f, lockSize * 0.6f, lockSize * 0.6f), Icons.Lock, Color.white);
            }

            GUI.Label(new Rect(rect.x + 12f, sealRect.yMax + 6f, rect.width - 16f, 28f), GameTexts.Pass.PremiumRow, skin.CenterBold);
            return true;
        }

        /// <summary>"Nv.40" over the column: gold when reached, outlined in turquoise when it is the next one.</summary>
        private static void Header(UiSkin skin, Rect rect, PassMilestoneView m, bool isNext)
        {
            // No glow on the next one (A-190 rule 12: light is for rewards): its turquoise pill is enough.
            var pill = new Rect(rect.x + 24f, rect.y + 4f, rect.width - 48f, rect.height - 8f);
            var color = m.Reached ? UiSkin.Gold : isNext ? UiSkin.Accent : UiSkin.Muted;
            skin.AccentPill(pill, GameTexts.Pass.MilestoneLevel(m.Level), color);
        }

        /// <summary>
        /// One reward: its lines as icon + number (M25-T24), then one state at the bottom: the Resgatar button, a faint check
        /// once claimed, a small padlock while the Premium is not bought; nothing while the milestone is ahead (the
        /// column's "Nv.40" says it).
        /// </summary>
        private void Cell(UiSkin skin, Rect rect, PassMilestoneView m, PassRewardView reward, PassTrack track)
        {
            GUI.Box(rect, GUIContent.none, skin.Card);
            if (reward.Claimable)
            {
                skin.ArcLine(rect, track == PassTrack.Premium ? UiSkin.Gold : UiSkin.Accent, 10f);
            }

            var dim = reward.Claimed || reward.Locked || !m.Reached;
            var previous = GUI.color;
            if (dim)
            {
                GUI.color = previous * new Color(1f, 1f, 1f, reward.Claimed ? 0.55f : 0.75f);
            }

            var x = rect.x + 16f;
            var w = rect.width - 32f;
            var y = rect.y + 12f;

            // Up to six lines (M24-T32/T33 added rod and clothing fragments): they get closer when the cell is short.
            var lines = (reward.Coins > 0 ? 1 : 0) + (reward.Shells > 0 ? 1 : 0) + (reward.Dollars > 0 ? 1 : 0) + (reward.Fragments > 0 ? 1 : 0)
                + (reward.RodFragments > 0 ? 1 : 0) + (reward.ClothingFragments > 0 ? 1 : 0);
            var step = Mathf.Clamp((rect.height - 76f) / Mathf.Max(1, lines), 24f, 32f);
            if (reward.IsEmpty)
            {
                GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), GameTexts.Pass.Empty, skin.SmallMuted);
            }

            if (reward.Coins > 0)
            {
                skin.CoinIcon(new Rect(x, y + 2f, 20f, 20f));
                Amount(skin, new Rect(x + 28f, y, w - 28f, UiSkin.SmallLine), Format.Short(reward.Coins), null, skin.SmallGoldLine);
                y += step;
            }

            if (reward.Shells > 0)
            {
                skin.CurrencyIcon(new Rect(x, y + 2f, 20f, 20f), Icons.Shell);
                Amount(skin, new Rect(x + 28f, y, w - 28f, UiSkin.SmallLine), Format.Number(reward.Shells), null, skin.SmallBold);
                y += step;
            }

            if (reward.Dollars > 0)
            {
                skin.CurrencyIcon(new Rect(x, y + 2f, 20f, 20f), Icons.Dollar);
                Amount(skin, new Rect(x + 28f, y, w - 28f, UiSkin.SmallLine), Format.Number(reward.Dollars), reward.DollarsAlreadyPaid ? GameTexts.Pass.DollarsPaid : null,
                    reward.DollarsAlreadyPaid ? skin.SmallMuted : skin.SmallBold);
                y += step;
            }

            if (reward.Fragments > 0)
            {
                // frag_boat_NN (art delivered 13/10/2026); the boat icon if the file is missing.
                FragmentIcon(skin, new Rect(x, y + 2f, 20f, 20f), FragmentFolder + reward.BoatId, Icons.Boat);
                Amount(skin, new Rect(x + 28f, y, w - 28f, UiSkin.SmallLine), Format.Number(reward.Fragments), reward.BoatName, skin.SmallBold);
                y += step;
            }

            if (reward.RodFragments > 0)
            {
                // frag_rod_NN (art delivered 13/10/2026); the rod icon if the file is missing.
                FragmentIcon(skin, new Rect(x, y + 2f, 20f, 20f), FragmentFolder + reward.RodId, Icons.Rod);
                Amount(skin, new Rect(x + 28f, y, w - 28f, UiSkin.SmallLine), Format.Number(reward.RodFragments), reward.RodName, skin.SmallBold);
                y += step;
            }

            if (reward.ClothingFragments > 0)
            {
                // frag_roupa (art delivered 13/10/2026); the star icon if the file is missing.
                FragmentIcon(skin, new Rect(x, y + 2f, 20f, 20f), ClothingFragmentArt, Icons.Star);
                Amount(skin, new Rect(x + 28f, y, w - 28f, UiSkin.SmallLine), Format.Number(reward.ClothingFragments), GameTexts.Pass.ClothingShort, skin.SmallBold);
            }

            GUI.color = previous;

            // The bottom of the cell: one state.
            var bottom = new Rect(rect.x + 8f, rect.yMax - 56f, rect.width - 16f, 48f);
            if (reward.Claimed)
            {
                skin.DrawIcon(new Rect(bottom.center.x - 12f, bottom.center.y - 12f, 24f, 24f), Icons.Check, new Color(UiSkin.Muted.r, UiSkin.Muted.g, UiSkin.Muted.b, 0.7f));
            }
            else if (reward.Locked)
            {
                skin.DrawIcon(new Rect(bottom.center.x - 10f, bottom.center.y - 10f, 20f, 20f), Icons.Lock, UiSkin.Gold);
            }
            else if (m.Reached && skin.IconButton(bottom, Icons.Check, GameTexts.Pass.Claim, track == PassTrack.Premium ? skin.ButtonReward : skin.ButtonPrimary))
            {
                _root.ClaimPass(m.Level, track);
                Refresh();
            }
        }

        /// <summary>A number in <paramref name="style"/>, then an optional name in the soft small text, cut with "…" if needed.</summary>
        private static void Amount(UiSkin skin, Rect rect, string number, string name, GUIStyle style)
        {
            var nw = Mathf.Min(rect.width, style.CalcSize(new GUIContent(number)).x + 2f);
            GUI.Label(new Rect(rect.x, rect.y, nw, rect.height), number, style);
            if (!string.IsNullOrEmpty(name) && rect.width - nw - 8f > 24f)
            {
                var rest = rect.width - nw - 8f;
                GUI.Label(new Rect(rect.x + nw + 8f, rect.y, rest, rect.height), FishCard.Fit(name, skin.SmallMuted, rest), skin.SmallMuted);
            }
        }

        private static void FragmentIcon(UiSkin skin, Rect rect, string art, string fallback)
        {
            var texture = ArtAssets.Texture(art);
            if (texture != null)
            {
                PixelDraw.Fit(rect, texture);
            }
            else
            {
                skin.DrawIcon(rect, fallback, UiSkin.Accent);
            }
        }
    }
}
