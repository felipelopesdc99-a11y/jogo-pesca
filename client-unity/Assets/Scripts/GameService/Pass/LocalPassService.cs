using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Persistence;
using FishingIdle.GameService.Shop;

namespace FishingIdle.GameService.Pass
{
    /// <summary>The two rows of the Passe do Pescador.</summary>
    public enum PassTrack
    {
        Free,
        Premium,
    }

    /// <summary>One reward of a milestone, as the pass screen shows it.</summary>
    public sealed class PassRewardView
    {
        public long Coins { get; internal set; }
        public long Shells { get; internal set; }
        public long Dollars { get; internal set; }

        /// <summary>
        /// The Dólares of this milestone were already paid when the level was reached, before the pass (A-111): claiming
        /// gives everything else, not them again.
        /// </summary>
        public bool DollarsAlreadyPaid { get; internal set; }

        public string BoatId { get; internal set; }
        public string BoatName { get; internal set; }
        public int Fragments { get; internal set; }

        /// <summary>Fragments of one rod (M24-T32) and Fragmentos de Roupa (M24-T33).</summary>
        public string RodId { get; internal set; }
        public string RodName { get; internal set; }
        public int RodFragments { get; internal set; }
        public int ClothingFragments { get; internal set; }

        public bool Claimed { get; internal set; }

        /// <summary>Reached, not claimed and (premium) bought: the "Resgatar" button works.</summary>
        public bool Claimable { get; internal set; }

        /// <summary>A premium reward while the premium is not bought (the padlock).</summary>
        public bool Locked { get; internal set; }

        public bool IsEmpty => Coins <= 0 && Shells <= 0 && Dollars <= 0 && Fragments <= 0 && RodFragments <= 0 && ClothingFragments <= 0;
    }

    public sealed class PassMilestoneView
    {
        public int Level { get; internal set; }

        /// <summary>The Fisher has reached this level.</summary>
        public bool Reached { get; internal set; }
        public PassRewardView Free { get; internal set; }
        public PassRewardView Premium { get; internal set; }
    }

    /// <summary>Everything the "Passe do Pescador" window shows (M24-T17, A-158).</summary>
    public sealed class PassView
    {
        public string PassId { get; internal set; }
        public int FisherLevel { get; internal set; }
        public bool Premium { get; internal set; }
        public long PremiumPriceDollars { get; internal set; }
        public long Dollars { get; internal set; }

        /// <summary>Why the premium cannot be bought now; None when it can.</summary>
        public ServiceError BuyPremiumBlocker { get; internal set; }

        /// <summary>Rewards ready to claim, free and premium.</summary>
        public int ClaimableCount { get; internal set; }

        /// <summary>The last milestone reached (0 before the first) and the next one (0 after the last).</summary>
        public int PreviousMilestoneLevel { get; internal set; }
        public int NextMilestoneLevel { get; internal set; }

        public List<PassMilestoneView> Milestones { get; } = new List<PassMilestoneView>();
    }

    /// <summary>What one claim (or "Resgatar tudo") gave.</summary>
    public sealed class PassClaimResult
    {
        /// <summary>Rewards claimed (a milestone's free and premium count as two).</summary>
        public int Claimed { get; internal set; }
        public long Coins { get; internal set; }
        public long Shells { get; internal set; }
        public long Dollars { get; internal set; }

        /// <summary>Fragments received, one entry per boat (summed), with the state after the claim.</summary>
        public List<FragmentGainView> Fragments { get; } = new List<FragmentGainView>();

        /// <summary>Names of the boats whose fragments this claim completed (the player assembles them, M24-T58, A-180).</summary>
        public List<string> BoatsReady { get; } = new List<string>();

        /// <summary>Rod fragments received, one entry per rod (summed), and the rods whose fragments they completed (M24-T32, M24-T58).</summary>
        public List<RodFragmentGainView> RodFragments { get; } = new List<RodFragmentGainView>();
        public List<string> RodsReady { get; } = new List<string>();

        /// <summary>Fragmentos de Roupa received (M24-T33).</summary>
        public long ClothingFragments { get; internal set; }
        public PassView Pass { get; internal set; }
    }

    /// <summary>
    /// The Passe do Pescador (M24-T17, A-158): one permanent pass that goes up with the Fisher level. Every milestone
    /// (pass.json) has a free reward and a premium one; the premium is bought once with Dólares and also opens the premium
    /// rewards of the milestones already passed. Rewards: Moedas, Conchas, Dólares and boat fragments (M24-T16).
    /// </summary>
    public interface IPassService
    {
        PassView GetPass();

        /// <summary>Whether reaching this Fisher level reaches a pass milestone (for the level-up notice).</summary>
        bool IsMilestone(int level);

        /// <summary>Claims one reward of a reached milestone.</summary>
        ServiceResult<PassClaimResult> Claim(int level, PassTrack track);

        /// <summary>Claims every reward that can be claimed now ("Resgatar tudo").</summary>
        ServiceResult<PassClaimResult> ClaimAll();

        /// <summary>Spends the premium price in Dólares; the premium rewards of every milestone become claimable as they are reached.</summary>
        ServiceResult<PassView> BuyPremium();
    }

    public sealed class LocalPassService : IPassService
    {
        private readonly GameSession _session;
        private readonly IFishingService _fishing;

        public LocalPassService(GameSession session, IFishingService fishing)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _fishing = fishing ?? throw new ArgumentNullException(nameof(fishing));
        }

        private PlayerSave Save => _session.Save;
        private GameConfig Config => _session.Config;
        private PassState State => Save.Pass;

        public bool IsMilestone(int level) => Config.PassMilestoneAt(level) != null;

        public PassView GetPass()
        {
            var config = Config;
            var state = State;
            var view = new PassView
            {
                PassId = config.Pass.PassId,
                FisherLevel = Save.FisherLevel,
                Premium = state.Premium,
                PremiumPriceDollars = config.Pass.PremiumPriceDollars,
                Dollars = Save.Dollars,
                BuyPremiumBlocker = PremiumBlocker(),
            };

            foreach (var milestone in config.Pass.Milestones)
            {
                var reached = Save.FisherLevel >= milestone.Level;
                var row = new PassMilestoneView
                {
                    Level = milestone.Level,
                    Reached = reached,
                    Free = Reward(milestone.Free, milestone.Level, PassTrack.Free, reached),
                    Premium = Reward(milestone.Premium, milestone.Level, PassTrack.Premium, reached),
                };
                view.Milestones.Add(row);
                if (row.Free.Claimable) view.ClaimableCount++;
                if (row.Premium.Claimable) view.ClaimableCount++;
                if (reached) view.PreviousMilestoneLevel = milestone.Level;
                else if (view.NextMilestoneLevel == 0) view.NextMilestoneLevel = milestone.Level;
            }

            return view;
        }

        public ServiceResult<PassClaimResult> Claim(int level, PassTrack track)
        {
            var milestone = Config.PassMilestoneAt(level);
            if (milestone == null)
            {
                return ServiceResult<PassClaimResult>.Fail(ServiceError.PassMilestoneNotFound);
            }

            var blocker = ClaimBlocker(milestone, track);
            if (blocker != ServiceError.None)
            {
                return ServiceResult<PassClaimResult>.Fail(blocker);
            }

            // Fragments may assemble a boat or a rod and change what is in use: cycles completed before go with the old one.
            _fishing.Sync();
            var result = new PassClaimResult();
            Give(milestone, track, result);
            return Done(result);
        }

        public ServiceResult<PassClaimResult> ClaimAll()
        {
            var ready = new List<(PassMilestoneConfig Milestone, PassTrack Track)>();
            foreach (var milestone in Config.Pass.Milestones)
            {
                foreach (var track in new[] { PassTrack.Free, PassTrack.Premium })
                {
                    if (ClaimBlocker(milestone, track) == ServiceError.None)
                    {
                        ready.Add((milestone, track));
                    }
                }
            }

            if (ready.Count == 0)
            {
                return ServiceResult<PassClaimResult>.Fail(ServiceError.PassNothingToClaim);
            }

            _fishing.Sync();
            var result = new PassClaimResult();
            foreach (var (milestone, track) in ready)
            {
                Give(milestone, track, result);
            }

            return Done(result);
        }

        public ServiceResult<PassView> BuyPremium()
        {
            var blocker = PremiumBlocker();
            if (blocker != ServiceError.None)
            {
                return ServiceResult<PassView>.Fail(blocker);
            }

            var price = Config.Pass.PremiumPriceDollars;
            Save.Dollars -= price;
            Missions.MissionTracker.OnDollarsSpent(Save, price);
            State.Premium = true;
            State.PremiumBoughtAtMs = _session.Clock.UtcNowMs;
            _session.Persist();
            _session.Log("Bought the pass premium for " + price + " dollars.");
            return ServiceResult<PassView>.Ok(GetPass());
        }

        // ------------------------------------------------------------------ internals

        private PassRewardView Reward(PassRewardConfig reward, int level, PassTrack track, bool reached)
        {
            var claimed = Claimed(track).Contains(level);
            var locked = track == PassTrack.Premium && !State.Premium;
            Config.TryGetBoat(reward?.BoatId, out var boat);
            var rod = reward != null && Config.TryGetRod(reward.RodId, out var r) && GameConfig.IsFragmentRod(r) ? r : null;
            return new PassRewardView
            {
                Coins = reward?.Coins ?? 0,
                Shells = reward?.Shells ?? 0,
                Dollars = reward?.Dollars ?? 0,
                DollarsAlreadyPaid = (reward?.Dollars ?? 0) > 0 && LegacyDollarsPaid(level, track),
                BoatId = boat?.Id,
                BoatName = boat?.DisplayName,
                Fragments = boat != null ? reward.Fragments : 0,
                RodId = rod?.Id,
                RodName = rod?.DisplayName,
                RodFragments = rod != null ? reward.RodFragments : 0,
                ClothingFragments = Config.Clothing != null ? Math.Max(0, reward?.ClothingFragments ?? 0) : 0,
                Claimed = claimed,
                Locked = locked,
                Claimable = reached && !claimed && !locked,
            };
        }

        /// <summary>The free track's Dólares of a milestone the old rule (A-111) already paid (save version 16 migration).</summary>
        private bool LegacyDollarsPaid(int level, PassTrack track)
        {
            return track == PassTrack.Free && level <= State.LegacyDollarsUpToLevel;
        }

        private List<int> Claimed(PassTrack track)
        {
            return track == PassTrack.Premium ? State.PremiumClaimed : State.FreeClaimed;
        }

        private ServiceError ClaimBlocker(PassMilestoneConfig milestone, PassTrack track)
        {
            if (Save.FisherLevel < milestone.Level) return ServiceError.PassMilestoneNotReached;
            if (track == PassTrack.Premium && !State.Premium) return ServiceError.PassPremiumLocked;
            if (Claimed(track).Contains(milestone.Level)) return ServiceError.PassAlreadyClaimed;
            return ServiceError.None;
        }

        private ServiceError PremiumBlocker()
        {
            if (State.Premium) return ServiceError.PassPremiumOwned;
            if (Save.Dollars < Config.Pass.PremiumPriceDollars) return ServiceError.NotEnoughDollars;
            return ServiceError.None;
        }

        /// <summary>Pays one reward and marks it claimed.</summary>
        private void Give(PassMilestoneConfig milestone, PassTrack track, PassClaimResult result)
        {
            var reward = track == PassTrack.Premium ? milestone.Premium : milestone.Free;
            Claimed(track).Add(milestone.Level);
            result.Claimed++;
            if (reward == null)
            {
                return;
            }

            if (reward.Coins > 0)
            {
                Save.Coins = SafeAdd(Save.Coins, reward.Coins);
                result.Coins = SafeAdd(result.Coins, reward.Coins);
            }

            if (reward.Shells > 0)
            {
                Save.Shells = SafeAdd(Save.Shells, reward.Shells);
                result.Shells = SafeAdd(result.Shells, reward.Shells);
            }

            if (reward.Dollars > 0 && !LegacyDollarsPaid(milestone.Level, track))
            {
                Save.Dollars = SafeAdd(Save.Dollars, reward.Dollars);
                result.Dollars = SafeAdd(result.Dollars, reward.Dollars);
            }

            if (reward.Fragments > 0 && Config.TryGetBoat(reward.BoatId, out var boat))
            {
                var gain = BoatFragmentRules.Add(Config, Save, boat, reward.Fragments);
                if (gain != null)
                {
                    var same = result.Fragments.FirstOrDefault(f => f.BoatId == gain.BoatId);
                    if (same != null)
                    {
                        same.Amount += gain.Amount;
                        same.Have = gain.Have;
                        same.Owned = gain.Owned;
                        same.Ready |= gain.Ready;
                    }
                    else
                    {
                        result.Fragments.Add(gain);
                    }

                    if (gain.Ready)
                    {
                        result.BoatsReady.Add(gain.BoatName);
                    }
                }
            }

            if (reward.RodFragments > 0 && Config.TryGetRod(reward.RodId, out var rod))
            {
                var gain = RodFragmentRules.Add(Config, Save, rod, reward.RodFragments, _session.Clock.UtcNowMs);
                if (gain != null)
                {
                    var same = result.RodFragments.FirstOrDefault(f => f.RodId == gain.RodId);
                    if (same != null)
                    {
                        same.Amount += gain.Amount;
                        same.Have = gain.Have;
                        same.Owned = gain.Owned;
                        same.Ready |= gain.Ready;
                    }
                    else
                    {
                        result.RodFragments.Add(gain);
                    }

                    if (gain.Ready)
                    {
                        result.RodsReady.Add(gain.RodName);
                    }
                }
            }

            if (reward.ClothingFragments > 0 && Config.Clothing != null)
            {
                ClothingRules.AddFragments(Save, reward.ClothingFragments);
                result.ClothingFragments = SafeAdd(result.ClothingFragments, reward.ClothingFragments);
            }
        }

        private ServiceResult<PassClaimResult> Done(PassClaimResult result)
        {
            State.FreeClaimed.Sort();
            State.PremiumClaimed.Sort();
            _session.Persist();
            _session.Log("Pass: claimed " + result.Claimed + " rewards (" + result.Coins + " coins, " + result.Shells + " shells, "
                         + result.Dollars + " dollars, " + result.Fragments.Sum(f => f.Amount) + " boat fragments, "
                         + result.RodFragments.Sum(f => f.Amount) + " rod fragments, " + result.ClothingFragments + " clothing fragments).");
            result.Pass = GetPass();
            return ServiceResult<PassClaimResult>.Ok(result);
        }

        private static long SafeAdd(long a, long b) => b > long.MaxValue - a ? long.MaxValue : a + b;
    }
}
