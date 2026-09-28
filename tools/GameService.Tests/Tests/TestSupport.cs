using FishingIdle.GameService;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Persistence;

namespace FishingIdle.GameService.Tests;

/// <summary>Shared fixtures: the real /config files, a temp save folder and a hand-driven clock.</summary>
internal static class TestSupport
{
    public const long StartMs = 1_790_000_000_000;

    public static string RepositoryRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "version.json")))
            {
                dir = dir.Parent;
            }

            return dir?.FullName ?? throw new InvalidOperationException("Repository root not found.");
        }
    }

    public static string ConfigDirectory => Path.Combine(RepositoryRoot, "config");

    public static Dictionary<string, string> ConfigTexts()
    {
        return GameConfigLoader.RequiredFiles.ToDictionary(f => f, f => File.ReadAllText(Path.Combine(ConfigDirectory, f)));
    }

    public static GameConfig RealConfig()
    {
        var result = GameConfigLoader.LoadFromDirectory(ConfigDirectory);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("\n", result.Errors));
        }

        return result.Config;
    }

    /// <summary>The real config with a JSON edit applied to one file.</summary>
    public static ConfigLoadResult ConfigWith(string file, Func<string, string> edit)
    {
        var texts = ConfigTexts();
        texts[file] = edit(texts[file]);
        return GameConfigLoader.LoadFromTexts(texts);
    }

    public static string NewTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "fishing-idle-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    public static (LocalGame Game, ManualClock Clock, string SaveDir) NewGame(GameConfig config = null, string saveDir = null, ManualClock clock = null)
    {
        saveDir ??= NewTempDirectory();
        clock ??= new ManualClock(StartMs);
        var result = LocalGame.Start(config ?? RealConfig(), new JsonFilePlayerRepository(saveDir), clock, _ => { });
        return (result.Game, clock, saveDir);
    }

    /// <summary>Advances time in small steps, syncing like the running client does.</summary>
    public static List<FishingIdle.GameService.Fishing.CatchView> PlayFor(LocalGame game, ManualClock clock, double seconds, double stepSeconds = 1)
    {
        var catches = new List<FishingIdle.GameService.Fishing.CatchView>();
        var elapsed = 0.0;
        while (elapsed < seconds)
        {
            clock.AdvanceSeconds(stepSeconds);
            elapsed += stepSeconds;
            catches.AddRange(game.Fishing.Sync().NewCatches);
        }

        return catches;
    }
}
