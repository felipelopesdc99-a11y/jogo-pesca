using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Aquarium;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Persistence;
using FishingIdle.GameService.Shop;
using FishingIdle.GameService.WorldBoss;

namespace FishingIdle.GameService.Clans
{
    /// <summary>A member line of the clan window.</summary>
    public sealed class ClanMemberView
    {
        public string Name { get; internal set; }
        public int FisherLevel { get; internal set; }
        public bool IsOwner { get; internal set; }
        public bool IsPlayer { get; internal set; }
        public long JoinedAtMs { get; internal set; }

        /// <summary>The fish this member put in the clan's Cardume (null species = none yet).</summary>
        public string FishSpeciesId { get; internal set; }
        public string FishSpeciesName { get; internal set; }
        public int FishLevel { get; internal set; }
        public long FishStrength { get; internal set; }
        public bool HasFish => !string.IsNullOrEmpty(FishSpeciesId);
    }

    /// <summary>An Aquarium fish as the clan's Cardume shows it (the player's own, or a candidate to put there).</summary>
    public sealed class ClanFishView
    {
        public long FishId { get; internal set; }
        public string SpeciesId { get; internal set; }
        public string SpeciesName { get; internal set; }
        public string RarityId { get; internal set; }
        public string SizeCategoryName { get; internal set; }
        public int Level { get; internal set; }

        /// <summary>The Cardume's Strength rule on this fish alone (no complete-Cardume bonus).</summary>
        public long Strength { get; internal set; }

        /// <summary>Its position in the player's own Cardume (0 = not there): it stays there too.</summary>
        public int CardumePosition { get; internal set; }

        /// <summary>It is the fish the player has in the clan's Cardume now.</summary>
        public bool InClanCardume { get; internal set; }
    }

    /// <summary>The player's clan.</summary>
    public sealed class ClanInfoView
    {
        public string Id { get; internal set; }
        public string Name { get; internal set; }
        public string OwnerName { get; internal set; }
        public bool IsOwner { get; internal set; }
        public long CreatedAtMs { get; internal set; }
        public int MemberCount { get; internal set; }
        public int Capacity { get; internal set; }
        public int BaseMembers { get; internal set; }
        public int MaxMembers { get; internal set; }
        public int ExtraSlots { get; internal set; }

        /// <summary>Dólares of the next place (0 when the clan is at max_members).</summary>
        public long NextSlotPriceDollars { get; internal set; }

        /// <summary>Why "Comprar vaga" is off (NotClanOwner, ClanSlotsMaxed, NotEnoughDollars); None when it is on.</summary>
        public ServiceError BuySlotBlocker { get; internal set; }

        /// <summary>The clan's Strength: the sum of the members' fish.</summary>
        public long Strength { get; internal set; }
        public List<ClanMemberView> Members { get; } = new List<ClanMemberView>();

        /// <summary>The player's fish in the clan's Cardume, or null.</summary>
        public ClanFishView MyFish { get; internal set; }
    }

    /// <summary>A reward of the top 3, as this player would receive it (Moedas and Conchas by the player's own level).</summary>
    public sealed class ClanRewardView
    {
        public int Rank { get; internal set; }
        public long Dollars { get; internal set; }
        public long Coins { get; internal set; }
        public long Shells { get; internal set; }
        public long ClothingFragments { get; internal set; }
        public long BoatFragments { get; internal set; }
        public string BoatName { get; internal set; }
    }

    /// <summary>A line of the clans' ranking (only with the online rewards on).</summary>
    public sealed class ClanRankLineView
    {
        public int Rank { get; internal set; }
        public string Name { get; internal set; }
        public long Strength { get; internal set; }
        public int Members { get; internal set; }
        public bool IsMine { get; internal set; }
    }

    /// <summary>One closed ranking day of the player's clan and what it paid.</summary>
    public sealed class ClanResultView
    {
        public DateTime Date { get; internal set; }
        public string ClanName { get; internal set; }
        public int Rank { get; internal set; }
        public long Strength { get; internal set; }
        public long Dollars { get; internal set; }
        public long Coins { get; internal set; }
        public long Shells { get; internal set; }
        public long ClothingFragments { get; internal set; }
        public long BoatFragments { get; internal set; }
        public string BoatName { get; internal set; }
        public bool BoatAssembled { get; internal set; }

        public bool IsEmpty => Dollars <= 0 && Coins <= 0 && Shells <= 0 && ClothingFragments <= 0 && BoatFragments <= 0;
    }

    /// <summary>The daily ranking block of the window.</summary>
    public sealed class ClanRankingView
    {
        /// <summary>The top 3 rewards are paid (only with a server; off in the local game).</summary>
        public bool OnlineRewards { get; internal set; }

        /// <summary>Seconds to the next 00:00 (Brasília).</summary>
        public double SecondsToReset { get; internal set; }
        public List<ClanRewardView> Rewards { get; } = new List<ClanRewardView>();

        /// <summary>The clans in order (filled only with the online rewards on).</summary>
        public List<ClanRankLineView> Lines { get; } = new List<ClanRankLineView>();

        /// <summary>The last closed day that paid the player's clan (null = none).</summary>
        public ClanResultView LastResult { get; internal set; }
    }

    /// <summary>Everything the "Clã" window shows.</summary>
    public sealed class ClanView
    {
        public int MinFisherLevel { get; internal set; }
        public int FisherLevel { get; internal set; }

        /// <summary>The Fisher level allows creating or joining a clan (Nv.100+).</summary>
        public bool LevelAllows { get; internal set; }
        public long Dollars { get; internal set; }

        public long CreateCostDollars { get; internal set; }

        /// <summary>Why "Criar clã" is off (ClanLevelTooLow, AlreadyInClan, NotEnoughDollars); None when it is on.</summary>
        public ServiceError CreateBlocker { get; internal set; }
        public int NameMinLength { get; internal set; }
        public int NameMaxLength { get; internal set; }
        public int BaseMembers { get; internal set; }

        /// <summary>The members' extra Fisher XP (0.01 = +1%) and whether the player gets it now.</summary>
        public double XpBonus { get; internal set; }
        public bool XpBonusActive { get; internal set; }

        /// <summary>The player's clan, or null.</summary>
        public ClanInfoView Clan { get; internal set; }
        public ClanRankingView Ranking { get; internal set; }

        public bool InClan => Clan != null;
    }

    /// <summary>What one <see cref="IClanService.Update"/> brought, for the notices.</summary>
    public sealed class ClanUpdate
    {
        /// <summary>A closed ranking day paid the player now (null = nothing paid).</summary>
        public ClanResultView Paid { get; internal set; }

        /// <summary>The player's fish left the Aquarium and so the clan's Cardume.</summary>
        public bool FishRemoved { get; internal set; }
    }

    /// <summary>
    /// The Clãs (M24-T49, A-174, TD-055). Local today; with a server, the same interface over HTTP, the server being the
    /// authority on the clan list, the members, their fish and the daily ranking.
    /// </summary>
    public interface IClanService
    {
        ClanView GetClan();

        /// <summary>The Aquarium fish the player can put in the clan's Cardume, strongest first.</summary>
        List<ClanFishView> GetFishCandidates();

        /// <summary>Creates a clan with this name, paying clans.json → costs.create_dollars. The player becomes its owner.</summary>
        ServiceResult<ClanView> Create(string name);

        /// <summary>Joins a clan by its id (the invite). Free; needs Nv.100 and a free place.</summary>
        ServiceResult<ClanView> Join(string clanId);

        /// <summary>Leaves the clan; an empty clan closes, an owner who leaves passes it to the oldest member.</summary>
        ServiceResult<ClanView> Leave();

        /// <summary>The owner buys one more place with Dólares (each one dearer).</summary>
        ServiceResult<ClanView> BuySlot();

        /// <summary>Puts one of the player's Aquarium fish in the clan's Cardume (it stays in the Aquarium and the Cardume).</summary>
        ServiceResult<ClanView> SetFish(long fishId);

        ServiceResult<ClanView> ClearFish();

        /// <summary>Called by the running game: closes a ranking day at 00:00 (Brasília) and pays it with the online rewards on.</summary>
        ClanUpdate Update();
    }

    public sealed class LocalClanService : IClanService
    {
        private readonly GameSession _session;
        private readonly LocalAquariumService _aquarium;
        private readonly IFishingService _fishing;

        public LocalClanService(GameSession session, LocalAquariumService aquarium, IFishingService fishing)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _aquarium = aquarium ?? throw new ArgumentNullException(nameof(aquarium));
            _fishing = fishing ?? throw new ArgumentNullException(nameof(fishing));
        }

        private PlayerSave Save => _session.Save;
        private GameConfig Config => _session.Config;
        private ClansConfig Clans => Config.Clans;

        private ClanState State
        {
            get
            {
                if (Save.Clans == null)
                {
                    Save.Clans = new ClanState();
                }

                return Save.Clans;
            }
        }

        // ------------------------------------------------------------------ reads

        public ClanView GetClan()
        {
            var state = State;
            var view = new ClanView
            {
                MinFisherLevel = Clans.Membership.MinFisherLevel,
                FisherLevel = Save.FisherLevel,
                LevelAllows = ClanRules.LevelAllows(Config, Save),
                Dollars = Save.Dollars,
                CreateCostDollars = Clans.Costs.CreateDollars,
                CreateBlocker = CreateBlocker(),
                NameMinLength = Clans.Name.MinLength,
                NameMaxLength = Clans.Name.MaxLength,
                BaseMembers = Clans.Membership.BaseMembers,
                XpBonus = Clans.XpBonus,
                XpBonusActive = ClanRules.BonusActive(Config, Save),
            };

            var clan = ClanRules.MyClan(Save);
            if (clan != null)
            {
                view.Clan = Info(clan);
            }

            var now = _session.Clock.UtcNowMs;
            var ranking = new ClanRankingView
            {
                OnlineRewards = Clans.Ranking.OnlineRewardsEnabled,
                SecondsToReset = Math.Max(0, (ClanRules.NextResetMs(Config, now) - now) / 1000.0),
            };
            var boat = WorldBossRules.BoatFor(Config, Save.FisherLevel);
            for (var rank = 1; rank <= Clans.Ranking.RewardsByRank.Count; rank++)
            {
                var reward = Clans.Ranking.RewardsByRank[rank - 1];
                ranking.Rewards.Add(new ClanRewardView
                {
                    Rank = rank,
                    Dollars = reward.Dollars,
                    Coins = Coins(reward),
                    Shells = Shells(reward),
                    ClothingFragments = Config.Clothing != null ? reward.ClothingFragments : 0,
                    BoatFragments = boat != null ? reward.BoatFragments : 0,
                    BoatName = boat?.DisplayName,
                });
            }

            if (ranking.OnlineRewards)
            {
                var ranked = ClanRules.Ranked(Config, Save);
                for (var i = 0; i < ranked.Count; i++)
                {
                    ranking.Lines.Add(new ClanRankLineView
                    {
                        Rank = i + 1,
                        Name = ranked[i].Clan.Name,
                        Strength = ranked[i].Strength,
                        Members = ranked[i].Clan.Members.Count,
                        IsMine = clan != null && ranked[i].Clan.Id == clan.Id,
                    });
                }
            }

            if (state.LastResult != null)
            {
                ranking.LastResult = Result(state.LastResult);
            }

            view.Ranking = ranking;
            return view;
        }

        public List<ClanFishView> GetFishCandidates()
        {
            var me = ClanRules.MyMember(Save, ClanRules.MyClan(Save));
            return Save.Aquarium
                .Select(f => ToFishView(f, me))
                .OrderByDescending(f => f.Strength)
                .ThenByDescending(f => f.Level)
                .ThenBy(f => f.FishId)
                .ToList();
        }

        // ------------------------------------------------------------------ intents

        public ServiceResult<ClanView> Create(string name)
        {
            var blocker = CreateBlocker();
            if (blocker != ServiceError.None && blocker != ServiceError.NotEnoughDollars)
            {
                return ServiceResult<ClanView>.Fail(blocker);
            }

            var clean = ClanRules.CleanName(name);
            if (!ClanRules.ValidName(Config, clean))
            {
                return ServiceResult<ClanView>.Fail(ServiceError.InvalidClanName);
            }

            if (ClanRules.NameTaken(Save, clean))
            {
                return ServiceResult<ClanView>.Fail(ServiceError.ClanNameTaken);
            }

            if (blocker == ServiceError.NotEnoughDollars)
            {
                return ServiceResult<ClanView>.Fail(blocker);
            }

            var state = State;
            var now = _session.Clock.UtcNowMs;
            var price = Clans.Costs.CreateDollars;
            Save.Dollars -= price;
            var clan = new ClanRecord
            {
                Id = "clan_" + state.NextClanNumber++,
                Name = clean,
                OwnerId = Save.PlayerId,
                CreatedAtMs = now,
            };
            clan.Members.Add(NewMember(now));
            state.KnownClans.Add(clan);
            state.MyClanId = clan.Id;
            StartRankingDay(now);
            _session.Persist();
            _session.Log("Clan " + clan.Id + " created for " + price + " dollars.");
            return ServiceResult<ClanView>.Ok(GetClan());
        }

        public ServiceResult<ClanView> Join(string clanId)
        {
            if (ClanRules.MyClan(Save) != null)
            {
                return ServiceResult<ClanView>.Fail(ServiceError.AlreadyInClan);
            }

            if (!ClanRules.LevelAllows(Config, Save))
            {
                return ServiceResult<ClanView>.Fail(ServiceError.ClanLevelTooLow);
            }

            var clan = State.KnownClans.FirstOrDefault(c => c.Id == clanId);
            if (clan == null)
            {
                return ServiceResult<ClanView>.Fail(ServiceError.ClanNotFound);
            }

            if (clan.Members.Count >= ClanRules.Capacity(Config, clan))
            {
                return ServiceResult<ClanView>.Fail(ServiceError.ClanFull);
            }

            var now = _session.Clock.UtcNowMs;
            clan.Members.RemoveAll(m => m.PlayerId == Save.PlayerId);
            clan.Members.Add(NewMember(now));
            State.MyClanId = clan.Id;
            StartRankingDay(now);
            _session.Persist();
            _session.Log("Joined clan " + clan.Id + ".");
            return ServiceResult<ClanView>.Ok(GetClan());
        }

        public ServiceResult<ClanView> Leave()
        {
            var clan = ClanRules.MyClan(Save);
            if (clan == null)
            {
                return ServiceResult<ClanView>.Fail(ServiceError.NotInClan);
            }

            clan.Members.RemoveAll(m => m.PlayerId == Save.PlayerId);
            State.MyClanId = null;
            if (clan.Members.Count == 0)
            {
                // Nobody left: the clan closes (its name is free again).
                State.KnownClans.Remove(clan);
                _session.Log("Left clan " + clan.Id + "; it was empty and closed.");
            }
            else if (clan.OwnerId == Save.PlayerId)
            {
                // The owner left: the member who joined first becomes the owner.
                clan.OwnerId = clan.Members.OrderBy(m => m.JoinedAtMs).ThenBy(m => m.PlayerId, StringComparer.Ordinal).First().PlayerId;
                _session.Log("Left clan " + clan.Id + "; ownership passed on.");
            }
            else
            {
                _session.Log("Left clan " + clan.Id + ".");
            }

            _session.Persist();
            return ServiceResult<ClanView>.Ok(GetClan());
        }

        public ServiceResult<ClanView> BuySlot()
        {
            var clan = ClanRules.MyClan(Save);
            if (clan == null)
            {
                return ServiceResult<ClanView>.Fail(ServiceError.NotInClan);
            }

            var blocker = BuySlotBlocker(clan);
            if (blocker != ServiceError.None)
            {
                return ServiceResult<ClanView>.Fail(blocker);
            }

            var price = ClanRules.SlotPrice(Config, clan.ExtraSlots);
            Save.Dollars -= price;
            clan.ExtraSlots++;
            _session.Persist();
            _session.Log("Clan " + clan.Id + ": place " + ClanRules.Capacity(Config, clan) + " bought for " + price + " dollars.");
            return ServiceResult<ClanView>.Ok(GetClan());
        }

        public ServiceResult<ClanView> SetFish(long fishId)
        {
            var me = ClanRules.MyMember(Save, ClanRules.MyClan(Save));
            if (me == null)
            {
                return ServiceResult<ClanView>.Fail(ServiceError.NotInClan);
            }

            if (fishId <= 0 || Save.Aquarium.All(f => f.Id != fishId))
            {
                return ServiceResult<ClanView>.Fail(ServiceError.FishNotFound);
            }

            // The fish is not moved: it stays in the Aquarium and, if it is there, in the player's own Cardume.
            me.FishId = fishId;
            _session.Persist();
            return ServiceResult<ClanView>.Ok(GetClan());
        }

        public ServiceResult<ClanView> ClearFish()
        {
            var me = ClanRules.MyMember(Save, ClanRules.MyClan(Save));
            if (me == null)
            {
                return ServiceResult<ClanView>.Fail(ServiceError.NotInClan);
            }

            me.FishId = 0;
            _session.Persist();
            return ServiceResult<ClanView>.Ok(GetClan());
        }

        public ClanUpdate Update()
        {
            var update = new ClanUpdate();
            var state = State;
            var now = _session.Clock.UtcNowMs;
            var changed = false;
            if (ClanRules.DropMissingFish(Save))
            {
                update.FishRemoved = true;
                changed = true;
            }

            var today = ClanRules.DayOf(Config, now);
            if (state.RankingDay == 0)
            {
                state.RankingDay = today;
                changed = true;
            }
            else if (today > state.RankingDay)
            {
                // 00:00 passed: the day state.RankingDay is closed. Locally it is ranked now (the first update after
                // midnight); the future server ranks at 00:00 itself (TD-055). A game closed for several days pays one day.
                Settle(state.RankingDay, update);
                state.RankingDay = today;
                changed = true;
            }

            // The player's own line carries the name and level the server would show to the other members.
            var me = ClanRules.MyMember(Save, ClanRules.MyClan(Save));
            if (me != null && (me.Name != Save.PlayerName || me.FisherLevel != Save.FisherLevel))
            {
                me.Name = Save.PlayerName;
                me.FisherLevel = Save.FisherLevel;
                changed = true;
            }

            if (changed)
            {
                _session.Persist();
            }

            return update;
        }

        // ------------------------------------------------------------------ internals

        private ServiceError CreateBlocker()
        {
            if (ClanRules.MyClan(Save) != null) return ServiceError.AlreadyInClan;
            if (!ClanRules.LevelAllows(Config, Save)) return ServiceError.ClanLevelTooLow;
            if (Save.Dollars < Clans.Costs.CreateDollars) return ServiceError.NotEnoughDollars;
            return ServiceError.None;
        }

        private ServiceError BuySlotBlocker(ClanRecord clan)
        {
            if (clan.OwnerId != Save.PlayerId) return ServiceError.NotClanOwner;
            if (ClanRules.Capacity(Config, clan) >= Clans.Membership.MaxMembers) return ServiceError.ClanSlotsMaxed;
            if (Save.Dollars < ClanRules.SlotPrice(Config, clan.ExtraSlots)) return ServiceError.NotEnoughDollars;
            return ServiceError.None;
        }

        private ClanMemberRecord NewMember(long now) => new ClanMemberRecord
        {
            PlayerId = Save.PlayerId,
            Name = Save.PlayerName,
            FisherLevel = Save.FisherLevel,
            JoinedAtMs = now,
        };

        /// <summary>A player who enters a clan starts the ranking clock from today (no day before counts).</summary>
        private void StartRankingDay(long now)
        {
            if (State.RankingDay == 0)
            {
                State.RankingDay = ClanRules.DayOf(Config, now);
            }
        }

        private ClanInfoView Info(ClanRecord clan)
        {
            var me = ClanRules.MyMember(Save, clan);
            var full = ClanRules.Capacity(Config, clan) >= Clans.Membership.MaxMembers;
            var info = new ClanInfoView
            {
                Id = clan.Id,
                Name = clan.Name,
                IsOwner = clan.OwnerId == Save.PlayerId,
                CreatedAtMs = clan.CreatedAtMs,
                MemberCount = clan.Members.Count,
                Capacity = ClanRules.Capacity(Config, clan),
                BaseMembers = Clans.Membership.BaseMembers,
                MaxMembers = Clans.Membership.MaxMembers,
                ExtraSlots = clan.ExtraSlots,
                NextSlotPriceDollars = full ? 0 : ClanRules.SlotPrice(Config, clan.ExtraSlots),
                BuySlotBlocker = BuySlotBlocker(clan),
                Strength = ClanRules.ClanStrength(Config, Save, clan),
            };

            var myFish = ClanRules.MyFish(Save, me);
            info.MyFish = myFish != null ? ToFishView(myFish, me) : null;
            foreach (var member in clan.Members.OrderByDescending(m => m.PlayerId == clan.OwnerId).ThenBy(m => m.JoinedAtMs))
            {
                var isPlayer = member.PlayerId == Save.PlayerId;
                var line = new ClanMemberView
                {
                    Name = isPlayer ? Save.PlayerName : member.Name,
                    FisherLevel = isPlayer ? Save.FisherLevel : member.FisherLevel,
                    IsOwner = member.PlayerId == clan.OwnerId,
                    IsPlayer = isPlayer,
                    JoinedAtMs = member.JoinedAtMs,
                    FishStrength = ClanRules.MemberStrength(Config, Save, member),
                };

                var speciesId = isPlayer ? myFish?.SpeciesId : member.FishSpeciesId;
                if (!string.IsNullOrEmpty(speciesId))
                {
                    line.FishSpeciesId = speciesId;
                    line.FishSpeciesName = Config.TryGetSpecies(speciesId, out var species) ? species.DisplayName : speciesId;
                    line.FishLevel = isPlayer ? myFish.Level : member.FishLevel;
                }

                if (line.IsOwner)
                {
                    info.OwnerName = line.Name;
                }

                info.Members.Add(line);
            }

            return info;
        }

        private ClanFishView ToFishView(FishInstance fish, ClanMemberRecord me)
        {
            var view = _aquarium.ToView(fish);
            return new ClanFishView
            {
                FishId = fish.Id,
                SpeciesId = fish.SpeciesId,
                SpeciesName = view.SpeciesName,
                RarityId = view.RarityId,
                SizeCategoryName = view.SizeCategoryName,
                Level = fish.Level,
                Strength = ClanRules.FishStrength(Config, fish),
                CardumePosition = view.CardumePosition,
                InClanCardume = me != null && me.FishId == fish.Id,
            };
        }

        private long Coins(ClanRewardConfig reward) => WorldBossRules.Minutes(WorldBossRules.IncomeAt(Config.WorldBoss, Save.FisherLevel).CoinsPerHour, reward.IncomeMinutes);

        private long Shells(ClanRewardConfig reward) => WorldBossRules.Minutes(WorldBossRules.IncomeAt(Config.WorldBoss, Save.FisherLevel).ShellsPerHour, reward.ShellMinutes);

        /// <summary>Closes a ranking day: with the online rewards on, a clan in the top 3 pays its reward to the player once.</summary>
        private void Settle(long day, ClanUpdate update)
        {
            var clan = ClanRules.MyClan(Save);
            if (!Clans.Ranking.OnlineRewardsEnabled || clan == null)
            {
                // Off in the local game: the day closes without a ranking or a payment.
                return;
            }

            var ranked = ClanRules.Ranked(Config, Save);
            var index = ranked.FindIndex(x => x.Clan.Id == clan.Id);
            var rank = index >= 0 ? index + 1 : 0;
            var result = new ClanRankingResult
            {
                Day = day,
                ClanName = clan.Name,
                Rank = rank,
                Strength = index >= 0 ? ranked[index].Strength : 0,
                RewardsOn = true,
            };

            var assembled = false;
            var reward = ClanRules.RewardFor(Config, rank);
            if (reward != null)
            {
                result.Dollars = reward.Dollars;
                result.Coins = Coins(reward);
                result.Shells = Shells(reward);
                Save.Dollars = SafeMath.Add(Save.Dollars, result.Dollars);
                Save.Coins = SafeMath.Add(Save.Coins, result.Coins);
                Save.Shells = SafeMath.Add(Save.Shells, result.Shells);
                if (reward.ClothingFragments > 0 && Config.Clothing != null)
                {
                    ClothingRules.AddFragments(Save, reward.ClothingFragments);
                    result.ClothingFragments = reward.ClothingFragments;
                }

                var boat = WorldBossRules.BoatFor(Config, Save.FisherLevel);
                if (reward.BoatFragments > 0 && boat != null)
                {
                    // A boat assembled now changes the catch chance: the cycles completed before go with the old one.
                    _fishing.Sync();
                    var gain = BoatFragmentRules.Add(Config, Save, boat, reward.BoatFragments);
                    if (gain != null)
                    {
                        result.BoatFragments = gain.Amount;
                        result.BoatId = boat.Id;
                        assembled = gain.Assembled;
                    }
                }
            }

            State.LastResult = result;
            _session.Log("Clan ranking: day " + day + " closed, clan " + clan.Id + " rank " + rank + " (strength " + result.Strength + "); paid "
                         + result.Dollars + " dollars, " + result.Coins + " coins, " + result.Shells + " shells, " + result.ClothingFragments
                         + " clothing fragments, " + result.BoatFragments + " boat fragments.");
            if (reward != null)
            {
                var paid = Result(result);
                paid.BoatAssembled = assembled;
                update.Paid = paid;
            }
        }

        private ClanResultView Result(ClanRankingResult r)
        {
            Config.TryGetBoat(r.BoatId, out var boat);
            return new ClanResultView
            {
                Date = ClanRules.DateOf(r.Day),
                ClanName = r.ClanName,
                Rank = r.Rank,
                Strength = r.Strength,
                Dollars = r.Dollars,
                Coins = r.Coins,
                Shells = r.Shells,
                ClothingFragments = r.ClothingFragments,
                BoatFragments = r.BoatFragments,
                BoatName = boat?.DisplayName,
            };
        }
    }
}
