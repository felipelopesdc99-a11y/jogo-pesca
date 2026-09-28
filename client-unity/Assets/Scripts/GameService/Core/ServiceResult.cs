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
