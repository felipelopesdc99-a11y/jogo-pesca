using System.Linq;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>A-121: the own Profile counts species and catches per rarity.</summary>
public sealed class ProfileSummaryTests
{
    [Fact]
    public void Every_rarity_is_tallied_and_the_totals_match()
    {
        var (game, clock, _) = TestSupport.NewGame();
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 600, stepSeconds: 30);

        var records = game.Profile.GetProfile().Records;
        Assert.Equal(5, records.ByRarity.Count);
        Assert.Equal("common", records.ByRarity[0].RarityId);
        Assert.Equal(records.SpeciesTotal, records.ByRarity.Sum(t => t.SpeciesTotal));
        Assert.Equal(records.SpeciesDiscovered, records.ByRarity.Sum(t => t.SpeciesFound));
        Assert.Equal(records.TotalCatches, records.ByRarity.Sum(t => t.Caught));
    }
}

/// <summary>M22-T11: the Encyclopedia knows every species' map and rarity, and the bite share once found.</summary>
public sealed class EncyclopediaByMapTests
{
    [Fact]
    public void Map_and_rarity_are_known_before_discovery_and_the_name_is_not()
    {
        var (game, clock, _) = TestSupport.NewGame();
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 600, stepSeconds: 30);

        var entries = game.Profile.GetProfile().Encyclopedia;
        Assert.All(entries, e => Assert.NotNull(e.MapId));
        Assert.All(entries, e => Assert.NotNull(e.RarityId));
        Assert.All(entries.Where(e => !e.Discovered), e => Assert.Null(e.Name));
        Assert.All(entries.Where(e => e.Discovered), e => Assert.InRange(e.BiteShare, 0.0001, 1.0));
    }
}

/// <summary>A-124: big amounts in short form.</summary>
public sealed class ShortNumberTests
{
    [Theory]
    [InlineData(845_320L, "845.320")]
    [InlineData(1_000_000L, "1 mi")]
    [InlineData(12_400_000L, "12,4 mi")]
    [InlineData(999_999_999L, "999,9 mi")]
    [InlineData(3_200_000_000L, "3,2 bi")]
    [InlineData(1_500_000_000_000L, "1,5 tri")]
    public void Amounts_are_shortened(long value, string expected)
    {
        Assert.Equal(expected, FishingIdle.Texts.Format.Short(value));
    }
}
