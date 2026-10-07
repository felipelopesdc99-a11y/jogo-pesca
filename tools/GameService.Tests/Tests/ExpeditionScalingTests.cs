using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>A-114: an Expedition pays more on later maps (maps.json → expedition_reward_multiplier).</summary>
public sealed class ExpeditionScalingTests
{
    [Fact]
    public void The_expected_reward_grows_with_the_map()
    {
        var (game, _, _) = TestSupport.NewGame();
        long OnMap(string mapId)
        {
            game.Session.Save.CurrentMapId = mapId;
            return game.Expeditions.GetExpeditions().Expeditions.Find(e => e.ExpeditionId == "exp_6h").ExpectedCoins;
        }

        var first = OnMap("map_01");
        var second = OnMap("map_02");
        var last = OnMap("map_10");

        Assert.True(first > 0);
        Assert.InRange(second / (double)first, 8.4, 8.6);
        Assert.InRange(last / (double)first, 1990, 2010);
    }
}
