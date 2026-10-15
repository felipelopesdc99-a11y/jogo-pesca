using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>
/// The Dev Panel edits /config by loading each file as a JSON tree and writing it back. This
/// guarantees that a save with no edits reproduces the file byte for byte, so a real edit only
/// changes the values the owner touched (clean diffs, no reformatting noise in git).
/// </summary>
public sealed class DevPanelRoundTripTests
{
    public static IEnumerable<object[]> ConfigFiles() =>
        Directory.GetFiles(TestSupport.ConfigDirectory, "*.json").Select(f => new object[] { Path.GetFileName(f) });

    [Theory]
    [MemberData(nameof(ConfigFiles))]
    public void Loading_and_saving_a_config_file_changes_nothing(string file)
    {
        var original = File.ReadAllText(Path.Combine(TestSupport.ConfigDirectory, file)).Replace("\r\n", "\n");

        // Same serialization as BalanceEditor.Serialize.
        var roundTrip = JObject.Parse(original).ToString(Formatting.Indented).Replace("\r\n", "\n") + "\n";

        Assert.Equal(original, roundTrip);
    }
}
