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

    [Fact]
    public void Discovered_species_bring_size_range_record_category_and_base_stats()
    {
        var (game, clock, _) = TestSupport.NewGame();
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 600, stepSeconds: 30);

        var profile = game.Profile.GetProfile();
        var max = profile.EncyclopediaStatsMax;
        Assert.NotNull(max);
        Assert.True(max.Hp > 0 && max.Attack > 0 && max.Speed > 0);

        var found = profile.Encyclopedia.Where(e => e.Discovered).ToList();
        Assert.NotEmpty(found);
        Assert.All(found, e =>
        {
            Assert.True(e.SpeciesMinCm > 0 && e.SpeciesMinCm < e.SpeciesMaxCm);
            Assert.InRange(e.LargestCm, e.SpeciesMinCm - 0.1, e.SpeciesMaxCm + 0.1);
            Assert.NotNull(e.LargestSizeCategoryId);
            Assert.NotNull(e.LargestSizeCategoryName);
            Assert.NotNull(e.BaseStats);
            Assert.InRange(e.BaseStats.Hp, 0.0001, max.Hp);
            Assert.InRange(e.BaseStats.Speed, 0.0001, max.Speed);
        });
        Assert.All(profile.Encyclopedia.Where(e => !e.Discovered), e =>
        {
            Assert.Null(e.BaseStats);
            Assert.Null(e.LargestSizeCategoryId);
            Assert.False(e.LargestIsSpecial);
        });
    }

    [Fact]
    public void The_record_category_is_read_back_from_the_size()
    {
        var (game, _, _) = TestSupport.NewGame();
        var records = game.Session.Save.SpeciesRecords;

        // Tambaqui: 40–110 cm, 70 cm is in the Adulto band. Pacu: 30–80 cm, 80 cm only the Perfeição band reaches.
        records["tambaqui"] = new FishingIdle.GameService.Persistence.SpeciesRecord { LargestMm = 700, TimesCaught = 1, FirstCaughtAtMs = TestSupport.StartMs };
        records["pacu"] = new FishingIdle.GameService.Persistence.SpeciesRecord { LargestMm = 800, TimesCaught = 1, FirstCaughtAtMs = TestSupport.StartMs };
        var entries = game.Profile.GetProfile().Encyclopedia;

        var tambaqui = entries.Single(e => e.SpeciesId == "tambaqui");
        var pacu = entries.Single(e => e.SpeciesId == "pacu");
        Assert.Equal("adult", tambaqui.LargestSizeCategoryId);
        Assert.False(tambaqui.LargestIsSpecial);
        Assert.Equal("perfect", pacu.LargestSizeCategoryId);
        Assert.True(pacu.LargestIsSpecial);
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
