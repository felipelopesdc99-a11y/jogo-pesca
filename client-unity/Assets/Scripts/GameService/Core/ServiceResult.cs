using FishingIdle.Texts;

namespace FishingIdle.GameService.Core
{
    /// <summary>Why the game service refused a request. Technical keys; the text lives in Texts.</summary>
    public enum ServiceError
    {
        None,
        AlreadyFishing,
        NotFishing,
        EmptySelection,
        CatchNotFound,
        DuplicateCatchInRequest,
        SpeciesMissingFromConfig,
        AquariumFull,
        FishNotFound,
        CannotFeedItself,
        FishAtMaxLevel,
        InvalidCardumePosition,
        ItemNotFound,
        RodNotAllowedOnMap,
        Traveling,
        AlreadyOnMap,
        MapNotFound,
        MapLocked,
        RodTooWeakForMap,
        NotEnoughCoins,
        RodNotForSale,
        RodAlreadyOwned,
        RodAtMaxLevel,
        RodHasNoLevels,
        RodEquipped,
        RodNotSellable,
        ExpeditionNotFound,
        ExpeditionActive,
        ExpeditionNotActive,
        DevToolsDisabled,
        InvalidName,
        AvatarNotFound,
        CardumeEmpty,
        CardumeLocked,
        NotEnoughEnergy,
        OpponentNotFound,
        NoRerollsLeft,
        ListingNotFound,
        ListingLimitReached,
        InvalidPrice,
        FishInCardume,
        RodNotTradable,
        OwnListing,
        WithdrawalNotFound,
        AuctionNotFound,
        AuctionLimitReached,
        BidTooLow,
        AlreadyHighestBidder,
        AuctionHasNoBids,
        AuctionEnded,
        NoRod,
        TutorialStepMismatch,
        BoatNotFound,
        BoatAlreadyOwned,
        BoatNotOwned,
        BaitNotFound,
        BaitNoCharges,
        NotEnoughShells,
        NotEnoughDollars,
        VipUnavailable,
        ArenaItemNotFound,
        NotEnoughHonor,
        ArenaWeeklyLimit,
        InvalidAmount,
        CrewMemberNotFound,
        CrewMemberLocked,
    }

    /// <summary>
    /// The answer to a player intent: either the authoritative result, or a refusal with a reason.
    /// </summary>
    public sealed class ServiceResult<T>
    {
        private ServiceResult(bool succeeded, T value, ServiceError error)
        {
            Succeeded = succeeded;
            Value = value;
            Error = error;
        }

        public bool Succeeded { get; }

        public T Value { get; }

        public ServiceError Error { get; }

        /// <summary>The refusal explained in PT-BR, ready to show to the player.</summary>
        public string ErrorMessage => Succeeded ? string.Empty : GameTexts.ServiceErrorMessage(Error.ToString());

        public static ServiceResult<T> Ok(T value) => new ServiceResult<T>(true, value, ServiceError.None);

        public static ServiceResult<T> Fail(ServiceError error) => new ServiceResult<T>(false, default, error);
    }
}
