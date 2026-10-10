using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Visual;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Crew;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The Crew window (M24-T08, addendum A-154): the Crew's income, the fleet milestone, the hire mode (×1, ×10, ×100,
    /// Máx) and one row per member with its units, income, next milestone, price and Contratar. A second tab holds the
    /// Upgrades bought with Moedas (M24-T11, A-155, <see cref="UpgradesPanel"/>).
    /// </summary>
    /// <remarks>
    /// Shows what the Crew service returns (prices, income, milestones, locks); nothing here computes a price or an
    /// income. Only the unlocked members and the next locked one are listed, so the window does not fill up with
    /// members the player cannot reach yet. Portraits are placeholders until the art arrives (ASSET_PENDENTE:
    /// Resources/Arte/Tripulacao/trip_crew_01 … trip_crew_10, docs/ASSETS_PENDENTES.md).
    /// </remarks>
    public sealed class CrewWindow
    {
        private const float SummaryHeight = 104f, ModeHeight = 44f, RowHeight = 100f, Gap = 12f, TabHeight = 40f, TabWidth = 170f;
        private const float RefreshSeconds = 0.25f;

        /// <summary>The members' portraits (Resources/Arte/Tripulacao), by member id. Missing = the drawn placeholder.</summary>
        private const string PortraitFolder = "Tripulacao/trip_";

        private static readonly CrewBuyMode[] Modes = { CrewBuyMode.One, CrewBuyMode.Ten, CrewBuyMode.Hundred, CrewBuyMode.Max };

        private enum Tab
        {
            Crew,
            Upgrades,
        }

        private readonly GameRoot _root;
        private readonly UpgradesPanel _upgrades;
        private Tab _tab = Tab.Crew;
        private CrewBuyMode _mode = CrewBuyMode.One;
        private CrewView _view;
        private float _nextRefresh;
        private Vector2 _scroll;

        public CrewWindow(GameRoot root)
        {
            _root = root;
            _upgrades = new UpgradesPanel(root);
        }

        public bool IsOpen { get; private set; }

        public void Open()
        {
            IsOpen = true;
            _nextRefresh = 0f;
            _upgrades.Invalidate();
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
            var info = _tab == Tab.Upgrades ? _upgrades.Info
                : view == null ? null : GameTexts.Crew.Info(view.UnlockPreviousCount, string.Join(", ", view.MilestoneCounts),
                Format.Factor(view.MilestoneMultiplier), Format.Factor(view.FleetMilestoneMultiplier),
                GameTexts.Offline.Hours(view.OfflineFullRateHours), Format.Percent(view.OfflineReducedRate, 0), GameTexts.Offline.Hours(view.OfflineMaxHours));
            var counter = view == null ? null : GameTexts.Crew.HudRate(Format.PerSecond(view.CoinsPerSecond));
            var area = WindowFrame.Draw(skin, screenWidth, screenHeight, GameTexts.Crew.Title, info, out var closed, 1200f, 840f, NavIcon, counter);
            if (closed)
            {
                Close();
                return;
            }

            if (view == null)
            {
                return;
            }

            DrawTabs(skin, new Rect(area.x, area.y, area.width, TabHeight));
            area = new Rect(area.x, area.y + TabHeight + Gap, area.width, area.height - TabHeight - Gap);
            if (_tab == Tab.Upgrades)
            {
                _upgrades.Draw(skin, area);
                return;
            }

            var summary = new Rect(area.x, area.y, area.width, SummaryHeight);
            DrawSummary(skin, summary, view);
            var modes = new Rect(area.x, summary.yMax + Gap, area.width, ModeHeight);
            DrawModes(skin, modes);
            var list = new Rect(area.x, modes.yMax + Gap, area.width, area.yMax - modes.yMax - Gap);
            DrawList(skin, list, view);
        }

        /// <summary>The Crew's menu icon (ASSET_PENDENTE ico_tripulacao; the boat until it arrives).</summary>
        public static string NavIcon => ArtAssets.Icon(Icons.Crew) != null ? Icons.Crew : Icons.Boat;

        private void Refresh()
        {
            _view = _root.GetCrew(_mode);
            _nextRefresh = Time.unscaledTime + RefreshSeconds;
        }

        /// <summary>"Tripulação | Melhorias" as chips, like the Market's tabs.</summary>
        private void DrawTabs(UiSkin skin, Rect rect)
        {
            var x = rect.x;
            foreach (var (tab, label) in new[] { (Tab.Crew, GameTexts.Crew.TabCrew), (Tab.Upgrades, GameTexts.Crew.TabUpgrades) })
            {
                var active = _tab == tab;
                if (GUI.Button(new Rect(x, rect.y, TabWidth, rect.height), label, active ? skin.ChipActive : skin.Chip) && !active)
                {
                    _tab = tab;
                    _nextRefresh = 0f;
                    _upgrades.Invalidate();
                }

                x += TabWidth + 10f;
            }
        }

        // ------------------------------------------------------------------ summary and modes

        /// <summary>Left: the Crew's Moedas and XP per second. Right: the fleet milestone with its progress.</summary>
        private static void DrawSummary(UiSkin skin, Rect rect, CrewView view)
        {
            skin.Inset(rect);
            var half = rect.width * 0.46f;

            // Income.
            var x = rect.x + 20f;
            GUI.Label(new Rect(x, rect.y + 12f, half - 30f, UiSkin.SmallLine), GameTexts.Crew.TotalIncome, skin.SmallMuted);
            skin.CoinIcon(new Rect(x, rect.y + 38f, 30f, 30f));
            GUI.Label(new Rect(x + 40f, rect.y + 38f, half - 70f, 30f), GameTexts.Crew.HudRate(Format.PerSecond(view.CoinsPerSecond)), skin.Number);
            var second = view.CoinsPerSecond > 0
                ? GameTexts.Crew.XpPerSecond(Format.Rate(view.XpPerSecond)) + " · " + GameTexts.Crew.Earned(Format.Short(view.CoinsEarned))
                : GameTexts.Crew.NoneYet;
            GUI.Label(new Rect(x, rect.y + 74f, half - 30f, UiSkin.SmallLine), FishCard.Fit(second, skin.SmallMuted, half - 30f), skin.SmallMuted);

            // Fleet milestone.
            var divider = rect.x + half;
            GUI.DrawTexture(new Rect(divider, rect.y + 14f, 1f, rect.height - 28f), skin.White, ScaleMode.StretchToFill, true, 0, new Color(1f, 1f, 1f, 0.12f), 0, 0);
            var fx = divider + 24f;
            var fw = rect.xMax - fx - 20f;
            GUI.Label(new Rect(fx, rect.y + 12f, fw - 120f, UiSkin.SmallLine), GameTexts.Crew.Fleet, skin.SmallMuted);
            if (view.FleetMilestonesReached > 0)
            {
                skin.AccentPill(new Rect(rect.xMax - 140f, rect.y + 10f, 120f, 26f), GameTexts.Crew.FleetActive(Format.Factor(view.FleetMultiplier)), UiSkin.Gold);
            }

            var next = view.FleetNextMilestone;
            string line;
            float fraction;
            if (next > 0)
            {
                var from = view.FleetPreviousMilestone;
                fraction = Mathf.Clamp01((view.FleetFewestUnits - from) / (float)Mathf.Max(1, next - from));
                line = GameTexts.Crew.FleetNext(next, view.FleetMembersReady, view.MemberCount, Format.Factor(view.FleetMilestoneMultiplier));
            }
            else
            {
                fraction = 1f;
                line = GameTexts.Crew.FleetDone(Format.Factor(view.FleetMultiplier));
            }

            GUI.Label(new Rect(fx, rect.y + 44f, fw, UiSkin.SmallLine), FishCard.Fit(line, skin.SmallBold, fw), skin.SmallBold);
            skin.Bar(new Rect(fx, rect.y + 74f, fw, 12f), fraction, true);
        }

        private void DrawModes(UiSkin skin, Rect rect)
        {
            var labelWidth = skin.SmallMuted.CalcSize(new GUIContent(GameTexts.Crew.BuyModeLabel)).x + 4f;
            GUI.Label(new Rect(rect.x, rect.center.y - 10f, labelWidth, UiSkin.SmallLine), GameTexts.Crew.BuyModeLabel, skin.SmallMuted);
            var x = rect.x + labelWidth + 14f;
            const float width = 96f;
            foreach (var mode in Modes)
            {
                var active = mode == _mode;
                var chip = new Rect(x, rect.y + 2f, width, rect.height - 4f);
                if (GUI.Button(chip, ModeLabel(mode), active ? skin.ChipActive : skin.Chip) && !active)
                {
                    _mode = mode;
                    Refresh();
                }

                x += width + 10f;
            }
        }

        private static string ModeLabel(CrewBuyMode mode)
        {
            switch (mode)
            {
                case CrewBuyMode.Ten: return GameTexts.Crew.ModeTen;
                case CrewBuyMode.Hundred: return GameTexts.Crew.ModeHundred;
                case CrewBuyMode.Max: return GameTexts.Crew.ModeMax;
                default: return GameTexts.Crew.ModeOne;
            }
        }

        // ------------------------------------------------------------------ members

        private void DrawList(UiSkin skin, Rect list, CrewView view)
        {
            // The unlocked members and the first locked one; the rest stay hidden until they are close.
            var shown = 0;
            foreach (var member in view.Members)
            {
                shown++;
                if (!member.Unlocked)
                {
                    break;
                }
            }

            var scrolls = shown * RowHeight > list.height;
            var content = new Rect(0, 0, list.width - (scrolls ? 20f : 0f), shown * RowHeight);
            _scroll = GUI.BeginScrollView(list, _scroll, content);
            for (var i = 0; i < shown; i++)
            {
                var row = new Rect(0, i * RowHeight, content.width, RowHeight - 8f);
                var member = view.Members[i];
                if (member.Unlocked)
                {
                    DrawMember(skin, row, member, view);
                }
                else
                {
                    DrawLocked(skin, row, member);
                }
            }

            GUI.EndScrollView();
        }

        private void DrawMember(UiSkin skin, Rect row, CrewMemberView member, CrewView view)
        {
            GUI.Box(row, GUIContent.none, skin.Card);
            var portrait = new Rect(row.x + 14f, row.y + (row.height - 64f) / 2f, 64f, 64f);
            DrawPortrait(skin, portrait, member, false);

            // Name with the units beside it; then the income and its multiplier.
            const float buttonWidth = 230f, barWidth = 230f;
            var x = portrait.xMax + 16f;
            var right = row.xMax - buttonWidth - 28f - barWidth - 24f;
            var units = Format.Number(member.Units);
            var unitsWidth = skin.Number.CalcSize(new GUIContent(units)).x + 4f;
            var nameWidth = Mathf.Max(60f, right - x - unitsWidth - 12f);
            var name = FishCard.Fit(member.Name, skin.Heading, nameWidth);
            var shownName = Mathf.Min(skin.Heading.CalcSize(new GUIContent(name)).x + 2f, nameWidth);
            GUI.Label(new Rect(x, row.y + 14f, shownName, 28f), name, skin.Heading);
            GUI.Label(new Rect(x + shownName + 12f, row.y + 12f, unitsWidth, 30f), units, skin.Number);

            var income = GameTexts.Crew.HudRate(Format.PerSecond(member.CoinsPerSecond));
            if (member.Multiplier > 1.0)
            {
                income += " · " + GameTexts.Crew.MemberMultiplier(Format.Factor(member.Multiplier));
            }

            skin.CoinIcon(new Rect(x, row.y + 52f, 20f, 20f));
            GUI.Label(new Rect(x + 26f, row.y + 52f, right - x - 26f, UiSkin.SmallLine), FishCard.Fit(income, skin.SmallGold, right - x - 26f), skin.SmallGold);

            // Next milestone: "×2 em 50" and a bar from the previous milestone to it.
            var bar = new Rect(row.xMax - buttonWidth - 28f - barWidth, row.y + 16f, barWidth, 0f);
            if (member.NextMilestone > 0)
            {
                var from = member.PreviousMilestone;
                var fraction = Mathf.Clamp01((member.Units - from) / (float)Mathf.Max(1, member.NextMilestone - from));
                GUI.Label(new Rect(bar.x, bar.y, barWidth - 70f, UiSkin.SmallLine), GameTexts.Crew.NextMilestone(Format.Factor(view.MilestoneMultiplier), member.NextMilestone), skin.SmallBold);
                GUI.Label(new Rect(bar.xMax - 90f, bar.y, 90f, UiSkin.SmallLine), Format.Number(member.Units) + "/" + Format.Number(member.NextMilestone), skin.SmallMutedRightLine);
                skin.Bar(new Rect(bar.x, bar.y + 30f, barWidth, 10f), fraction, true);
            }
            else
            {
                GUI.Label(new Rect(bar.x, bar.y, barWidth, UiSkin.SmallLine), GameTexts.Crew.AllMilestones, skin.SmallGoldLine);
                skin.Bar(new Rect(bar.x, bar.y + 30f, barWidth, 10f), 1f, true);
            }

            // Contratar ×N with the price under it.
            var button = new Rect(row.xMax - buttonWidth - 16f, row.y + 12f, buttonWidth, 42f);
            var affordable = member.BuyBlocker == ServiceError.None;
            var enabled = GUI.enabled;
            GUI.enabled = enabled && affordable;
            if (skin.IconButton(button, Icons.Add, GameTexts.Crew.HireAmount(Format.Number(member.BuyAmount)), affordable ? skin.ButtonPrimary : skin.Button))
            {
                _root.HireCrew(member.MemberId, _mode);
                Refresh();
            }

            GUI.enabled = enabled;
            var price = member.BuyCost == CrewRules.Unaffordable ? GameTexts.Crew.TooExpensive : Format.Short(member.BuyCost);
            var priceStyle = affordable ? skin.SmallGold : skin.SmallMuted;
            var priceWidth = Mathf.Min(priceStyle.CalcSize(new GUIContent(price)).x + 4f, buttonWidth - 30f);
            var px = button.center.x - (priceWidth + 26f) / 2f;
            skin.CoinIcon(new Rect(px, button.yMax + 8f, 20f, 20f));
            GUI.Label(new Rect(px + 26f, button.yMax + 8f, priceWidth, UiSkin.SmallLine), FishCard.Fit(price, priceStyle, priceWidth), priceStyle);
        }

        private static void DrawLocked(UiSkin skin, Rect row, CrewMemberView member)
        {
            GUI.Box(row, GUIContent.none, skin.Card);
            GUI.DrawTexture(row, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.45f), 0, 10);
            var portrait = new Rect(row.x + 14f, row.y + (row.height - 64f) / 2f, 64f, 64f);
            DrawPortrait(skin, portrait, member, true);
            var x = portrait.xMax + 16f;
            var w = row.xMax - x - 20f;
            GUI.Label(new Rect(x, row.y + 14f, w, 28f), FishCard.Fit(member.Name, skin.Heading, w), skin.Heading);
            skin.DrawIcon(new Rect(x, row.y + 52f, 18f, 18f), Icons.Lock, UiSkin.Gold);
            var line = GameTexts.Crew.LockedLine(member.UnlockAfterCount, member.UnlockAfterNamePlural, Format.Number(member.UnlockAfterHave));
            GUI.Label(new Rect(x + 26f, row.y + 52f, w - 26f, UiSkin.SmallLine), FishCard.Fit(line, skin.SmallMuted, w - 26f), skin.SmallMuted);
        }

        /// <summary>The member's portrait, or a placeholder: a tile with a person (the first three) or a boat and its number.</summary>
        private static void DrawPortrait(UiSkin skin, Rect rect, CrewMemberView member, bool locked)
        {
            var art = ArtAssets.Texture(PortraitFolder + member.MemberId);
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
                // ASSET_PENDENTE: trip_crew_NN. Fishers on foot first, then the boats.
                skin.IconBadge(rect, member.Position <= 3 ? Icons.Profile : Icons.Boat, locked ? UiSkin.Muted : UiSkin.Accent);
                var badge = new Rect(rect.xMax - 22f, rect.yMax - 22f, 24f, 24f);
                GUI.DrawTexture(badge, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Night, 0, 12f);
                GUI.Label(badge, member.Position.ToString(), skin.PillText);
            }

            GUI.color = previous;
        }
    }
}
