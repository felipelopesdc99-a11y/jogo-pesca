using FishingIdle.GameService.Config;
using Xunit;

namespace FishingIdle.GameService.Tests;

public sealed class ConfigTests
{
    [Fact]
    public void The_repository_balance_files_are_valid()
    {
        var result = GameConfigLoader.LoadFromDirectory(TestSupport.ConfigDirectory);

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        Assert.Equal(40, result.Config.FishCatalog.Species.Count);
        Assert.Equal("map_01", result.Config.StartingMap.Id);
        Assert.Equal(0, result.Config.StarterRod.Tier);
    }

    [Fact]
    public void Identical_files_have_the_same_version_and_an_edit_changes_it()
    {
        var a = GameConfigLoader.LoadFromTexts(TestSupport.ConfigTexts());
        var b = GameConfigLoader.LoadFromTexts(TestSupport.ConfigTexts());
        var edited = TestSupport.ConfigWith(GameConfigLoader.ProgressionFile,
            json => json.Replace("\"online_cycle_seconds\": 30", "\"online_cycle_seconds\": 25"));

        Assert.True(edited.Succeeded, string.Join("\n", edited.Errors));
        Assert.Equal(a.Config.Version, b.Config.Version);
        Assert.NotEqual(a.Config.Version, edited.Config.Version);
        Assert.Equal(25, edited.Config.Fishing.OnlineCycleSeconds);
    }

    [Fact]
    public void A_negative_catch_weight_is_rejected_with_a_portuguese_message()
    {
        var result = TestSupport.ConfigWith(GameConfigLoader.MapsFile,
            json => json.Replace("\"catch_weight\": 220", "\"catch_weight\": -5"));

        Assert.False(result.Succeeded);
        Assert.Null(result.Config);
        Assert.Contains(result.Errors, e => e.Contains("maps.json") && e.Contains("negativo"));
    }

    [Fact]
    public void Broken_json_is_rejected_and_names_the_file()
    {
        var result = TestSupport.ConfigWith(GameConfigLoader.RodsFile, json => json + "}}");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("rods.json") && e.Contains("JSON válido"));
    }

    [Fact]
    public void A_pool_species_missing_from_the_catalog_is_rejected()
    {
        var result = TestSupport.ConfigWith(GameConfigLoader.MapsFile,
            json => json.Replace("\"species_id\": \"lambari\"", "\"species_id\": \"peixe_inexistente\""));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("peixe_inexistente"));
    }

    [Fact]
    public void A_cycle_shorter_than_one_second_is_rejected()
    {
        var result = TestSupport.ConfigWith(GameConfigLoader.ProgressionFile,
            json => json.Replace("\"online_cycle_seconds\": 30", "\"online_cycle_seconds\": 0"));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("online_cycle_seconds"));
    }
}
