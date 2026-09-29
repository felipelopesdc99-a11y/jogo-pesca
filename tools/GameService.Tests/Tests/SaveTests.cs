using FishingIdle.GameService.Core;
using FishingIdle.GameService.Persistence;
using Xunit;

namespace FishingIdle.GameService.Tests;

public sealed class SaveTests
{
    [Fact]
    public void Progress_survives_closing_and_reopening_the_game()
    {
        var saveDir = TestSupport.NewTempDirectory();
        var clock = new ManualClock(TestSupport.StartMs);
        var (game, _, _) = TestSupport.NewGame(saveDir: saveDir, clock: clock);
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 300);
        game.Fishing.SellCatches(game.Fishing.GetFishingBox().Take(4).Select(c => c.CatchId).ToList());
        var before = game.Player.GetPlayer();
        var boxBefore = game.Fishing.GetFishingBox().Select(c => (c.CatchId, c.SpeciesId, c.SizeCm)).ToList();

        var (reopened, _, _) = TestSupport.NewGame(saveDir: saveDir, clock: clock);
        var after = reopened.Player.GetPlayer();

        Assert.Equal(before.Coins, after.Coins);
        Assert.Equal(before.FisherLevel, after.FisherLevel);
        Assert.Equal(before.FisherXp, after.FisherXp);
        Assert.Equal(boxBefore, reopened.Fishing.GetFishingBox().Select(c => (c.CatchId, c.SpeciesId, c.SizeCm)).ToList());
        Assert.Equal(SaveLoadStatus.Loaded, reopened.Session.LoadStatus);
    }

    [Fact]
    public void The_save_file_carries_a_version_and_a_backup_is_kept()
    {
        var (game, clock, saveDir) = TestSupport.NewGame();
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 60);

        var json = File.ReadAllText(Path.Combine(saveDir, JsonFilePlayerRepository.SaveFileName));

        Assert.Contains("\"save_version\": " + FishingIdle.GameService.Persistence.PlayerSave.CurrentVersion, json);
        Assert.True(File.Exists(Path.Combine(saveDir, JsonFilePlayerRepository.BackupFileName)));
    }

    [Fact]
    public void A_damaged_save_is_restored_from_the_backup_and_kept_aside()
    {
        var (game, clock, saveDir) = TestSupport.NewGame();
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 120);
        File.WriteAllText(Path.Combine(saveDir, JsonFilePlayerRepository.SaveFileName), "{ isto não é json");

        var (reopened, _, _) = TestSupport.NewGame(saveDir: saveDir, clock: clock);

        Assert.Equal(SaveLoadStatus.RecoveredFromBackup, reopened.Session.LoadStatus);
        Assert.NotEmpty(reopened.Fishing.GetFishingBox());
        Assert.Single(Directory.GetFiles(saveDir, "*.corrupt-*.json"));
    }

    [Fact]
    public void A_save_with_impossible_values_is_not_trusted()
    {
        var (game, clock, saveDir) = TestSupport.NewGame();
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 60);
        foreach (var file in new[] { JsonFilePlayerRepository.SaveFileName, JsonFilePlayerRepository.BackupFileName })
        {
            var path = Path.Combine(saveDir, file);
            File.WriteAllText(path, File.ReadAllText(path).Replace("\"coins\": 0", "\"coins\": -500"));
        }

        var (reopened, _, _) = TestSupport.NewGame(saveDir: saveDir, clock: clock);

        Assert.Equal(SaveLoadStatus.Unrecoverable, reopened.Session.LoadStatus);
        Assert.Equal(0, reopened.Player.GetPlayer().Coins);
        Assert.Equal(2, Directory.GetFiles(saveDir, "*.corrupt-*.json").Length);
    }

    [Fact]
    public void A_save_from_a_newer_game_version_is_never_overwritten()
    {
        var (game, _, saveDir) = TestSupport.NewGame();
        var path = Path.Combine(saveDir, JsonFilePlayerRepository.SaveFileName);
        var future = File.ReadAllText(path).Replace("\"save_version\": " + FishingIdle.GameService.Persistence.PlayerSave.CurrentVersion, "\"save_version\": 99");
        File.WriteAllText(path, future);

        var (reopened, clock, _) = TestSupport.NewGame(saveDir: saveDir);
        reopened.Fishing.StartFishing();
        TestSupport.PlayFor(reopened, clock, 120);

        Assert.Equal(SaveLoadStatus.TooNew, reopened.Session.LoadStatus);
        Assert.Equal(future, File.ReadAllText(path));
    }

    [Fact]
    public void Reset_starts_a_new_player_and_keeps_a_copy_of_the_old_save()
    {
        var (game, clock, saveDir) = TestSupport.NewGame();
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 300);

        var kept = game.Session.ResetSave();

        Assert.NotNull(kept);
        Assert.True(File.Exists(kept));
        Assert.Empty(game.Fishing.GetFishingBox());
        Assert.False(game.Fishing.GetStatus().IsFishing);
        Assert.Equal(1, game.Player.GetPlayer().FisherLevel);
    }
}
