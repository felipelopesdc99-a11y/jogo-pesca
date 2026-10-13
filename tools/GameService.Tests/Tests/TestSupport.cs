using FishingIdle.GameService;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Persistence;

namespace FishingIdle.GameService.Tests;

/// <summary>Shared fixtures: the real /config files, a temp save folder and a hand-driven clock.</summary>
internal static class TestSupport
{
    public const long StartMs = 1_790_000_000_000;

    /// <summary>The fixed seed the Market and Auction tests use (M24-T26: no test depends on a lucky draw).</summary>
    public const ulong FixedSeed = 0x5EED_2026_1012UL;

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

    private static GameConfig _certain;

    /// <summary>
    /// The real config with every bite pulled out (Catch Success 100%) and no rod fragments (M24-T32). Tests about timing,
    /// the box, sales and the other systems use it so they keep counting one fish per cycle with the rod they were given;
    /// the Catch Success tests use <see cref="RealConfig"/>.
    /// </summary>
    public static GameConfig CertainCatchConfig()
    {
        if (_certain != null)
        {
            return _certain;
        }

        var result = ConfigWith(GameConfigLoader.ProgressionFile, json => json, certainCatch: true);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("\n", result.Errors));
        }

        return _certain = result.Config;
    }

    /// <summary>
    /// A rod chance that never drops in a test but keeps the config valid: since M24-T47 (A-173) the validator wants every
    /// map's rod to have a chance above 0 (every rod drops on every map; a rod at 0 would lock the progression).
    /// </summary>
    public const double NeverDrops = 1e-12;

    /// <summary>The real config with a JSON edit applied to one file (and, if asked, every bite pulled out).</summary>
    public static ConfigLoadResult ConfigWith(string file, Func<string, string> edit, bool certainCatch = false)
    {
        var texts = ConfigTexts();
        if (certainCatch)
        {
            var root = Newtonsoft.Json.Linq.JObject.Parse(texts[GameConfigLoader.ProgressionFile]);
            foreach (var tier in root["rarity"]!["tiers"]!)
            {
                tier["catch_success_base"] = 1.0;
            }

            root["fishing"]!["catch_success_max"] = 1.0;
            texts[GameConfigLoader.ProgressionFile] = root.ToString();

            // Rods by fragments (M24-T32): with every bite pulled out, a long test on a map would assemble its rod by
            // itself and change the rod in use. These tests give rods with TestSupport.GiveRod; the rod drops are
            // tested in RodsAndClothingByFragmentsTests with their own config.
            var rods = Newtonsoft.Json.Linq.JObject.Parse(texts[GameConfigLoader.RodsFile]);
            foreach (var rod in rods["rods"]!)
            {
                if (rod["fragment_drop_chance"] != null)
                {
                    rod["fragment_drop_chance"] = NeverDrops;
                }
            }

            texts[GameConfigLoader.RodsFile] = rods.ToString();
        }

        texts[file] = edit(texts[file]);
        return GameConfigLoader.LoadFromTexts(texts);
    }

    public static string NewTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "fishing-idle-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// A running game. By default the tutorial is skipped, so the player starts with the Starter Rod
    /// equipped, and every bite is pulled out (<see cref="CertainCatchConfig"/>); pass
    /// <see cref="RealConfig"/> to play with the real Catch Success.
    /// </summary>
    /// <param name="seed">A fixed random seed for a new player, so a test that depends on the draws (Market, Auctions)
    /// plays the same game every run; null draws one at random like the real game.</param>
    public static (LocalGame Game, ManualClock Clock, string SaveDir) NewGame(GameConfig config = null, string saveDir = null, ManualClock clock = null, bool skipTutorial = true, ulong? seed = null)
    {
        saveDir ??= NewTempDirectory();
        clock ??= new ManualClock(StartMs);
        var result = LocalGame.Start(config ?? CertainCatchConfig(), new JsonFilePlayerRepository(saveDir), clock, _ => { }, seed);
        if (skipTutorial)
        {
            result.Game.Tutorial.Skip();
        }

        return (result.Game, clock, saveDir);
    }

    /// <summary>
    /// Gives a boat the way the game does since M24-T16 (A-157): all the fragments it needs, which assemble it (and put it
    /// in use when it is better). Boats are no longer sold.
    /// </summary>
    public static void GiveBoat(LocalGame game, string boatId)
    {
        var config = game.Session.Config;
        config.TryGetBoat(boatId, out var boat);
        var gain = FishingIdle.GameService.Shop.BoatFragmentRules.Add(config, game.Session.Save, boat, boat.FragmentsRequired);
        if (gain == null || !gain.Assembled)
        {
            throw new InvalidOperationException("Boat " + boatId + " was not assembled.");
        }

        game.Session.Persist();
    }

    /// <summary>
    /// Gives a rod the way the game does since M24-T32 (A-163): all the fragments it needs, which assemble it (and put it in
    /// use when its tier is higher than the rod in use). Rods are no longer sold.
    /// </summary>
    public static FishingIdle.GameService.Shop.RodFragmentGainView GiveRod(LocalGame game, string rodId)
    {
        var config = game.Session.Config;
        config.TryGetRod(rodId, out var rod);
        var gain = FishingIdle.GameService.Shop.RodFragmentRules.Add(config, game.Session.Save, rod, rod.FragmentsRequired, game.Session.Clock.UtcNowMs);
        if (gain == null || !gain.Assembled)
        {
            throw new InvalidOperationException("Rod " + rodId + " was not assembled.");
        }

        game.Session.Persist();
        return gain;
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
