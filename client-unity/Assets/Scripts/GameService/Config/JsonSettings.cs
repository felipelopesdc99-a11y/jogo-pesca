using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace FishingIdle.GameService.Config
{
    /// <summary>
    /// One JSON convention for every file the game reads or writes: snake_case keys, as in /config
    /// and the rest of the repository (docs/DECISOES.md, TD-003).
    /// </summary>
    public static class JsonSettings
    {
        public static readonly JsonSerializerSettings Default = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() },
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Include,
            Formatting = Formatting.Indented,
        };
    }
}
