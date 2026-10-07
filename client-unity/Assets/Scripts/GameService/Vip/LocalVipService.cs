using System;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Persistence;

namespace FishingIdle.GameService.Vip
{
    /// <summary>What the VIP screen shows (A-110).</summary>
    public sealed class VipView
    {
        public bool Active { get; internal set; }

        /// <summary>When the VIP ends, in Unix ms; 0 when it is not active.</summary>
        public long UntilMs { get; internal set; }
        public long RemainingMs { get; internal set; }
        public long PriceDollars { get; internal set; }
        public double DurationDays { get; internal set; }
        public double OfflineFisherXpBonus { get; internal set; }
        public long Dollars { get; internal set; }

        /// <summary>Why buying is refused right now; None when it is possible.</summary>
        public ServiceError BuyBlocker { get; internal set; }
    }

    /// <summary>The VIP rules, shared by the fishing service and the VIP service.</summary>
    public static class VipRules
    {
        public static bool IsActive(PlayerSave save, long atMs) => save != null && save.VipUntilMs > atMs;

        /// <summary>The extra Fisher XP an offline catch made at <paramref name="atMs"/> earns from the VIP.</summary>
        public static long OfflineBonusXp(GameConfig config, PlayerSave save, long fisherXp, long atMs)
        {
            var vip = config.Economy.Vip;
            if (vip == null || fisherXp <= 0 || !IsActive(save, atMs))
            {
                return 0;
            }

            return (long)Math.Floor(fisherXp * vip.OfflineFisherXpBonus);
        }
    }

    /// <summary>The VIP (A-110): 100 Dólares buy 30 days of +50% Fisher XP on offline fishing (values in economy.json).</summary>
    public interface IVipService
    {
        VipView GetVip();

        /// <summary>Spends the Dólares and adds the VIP days; buying while active extends from the current end.</summary>
        ServiceResult<VipView> BuyVip();
    }

    public sealed class LocalVipService : IVipService
    {
        private readonly GameSession _session;

        public LocalVipService(GameSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        private PlayerSave Save => _session.Save;
        private GameConfig Config => _session.Config;

        public VipView GetVip()
        {
            var now = _session.Clock.UtcNowMs;
            var vip = Config.Economy.Vip;
            var active = VipRules.IsActive(Save, now);
            return new VipView
            {
                Active = active,
                UntilMs = active ? Save.VipUntilMs : 0,
                RemainingMs = active ? Save.VipUntilMs - now : 0,
                PriceDollars = vip?.PriceDollars ?? 0,
                DurationDays = vip?.DurationDays ?? 0,
                OfflineFisherXpBonus = vip?.OfflineFisherXpBonus ?? 0,
                Dollars = Save.Dollars,
                BuyBlocker = Blocker(),
            };
        }

        public ServiceResult<VipView> BuyVip()
        {
            var blocker = Blocker();
            if (blocker != ServiceError.None)
            {
                return ServiceResult<VipView>.Fail(blocker);
            }

            var vip = Config.Economy.Vip;
            var now = _session.Clock.UtcNowMs;
            var start = Math.Max(now, Save.VipUntilMs);
            Save.Dollars -= vip.PriceDollars;
            Save.VipUntilMs = start + (long)Math.Round(vip.DurationDays * 86400000.0);
            _session.Persist();
            _session.Log("Bought VIP for " + vip.PriceDollars + " dollars; active until " + Save.VipUntilMs + ".");
            return ServiceResult<VipView>.Ok(GetVip());
        }

        private ServiceError Blocker()
        {
            var vip = Config.Economy.Vip;
            if (vip == null || vip.PriceDollars < 1 || vip.DurationDays <= 0) return ServiceError.VipUnavailable;
            if (Save.Dollars < vip.PriceDollars) return ServiceError.NotEnoughDollars;
            return ServiceError.None;
        }
    }
}
