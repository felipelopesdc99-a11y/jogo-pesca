using FishingIdle.GameService.Aquarium;
using FishingIdle.GameService.Arena;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Market;
using FishingIdle.GameService.Persistence;
using FishingIdle.Texts;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>
/// The genes (M24-T52, A-176, TD-056): four per fish (Vida, Ataque, Defesa, Velocidade), 0–31, rolled uniformly and
/// independently at capture with the service's Rng, kept for good; each scales its attribute from ×0,90 to ×1,10; the
/// quality (0–100%) and the stars summarize them; saves before version 23 get 16/16/16/16; the NPC price never changes.
/// </summary>
public sealed class GenesTests
{
    private static GameConfig Config => TestSupport.RealConfig();

    // ------------------------------------------------------------------ the roll

    [Fact]
    public void Each_gene_is_uniform_from_0_to_31()
    {
        var config = Config;
        var rng = new Rng(0x6E4E_5001UL);
        const int sets = 64_000;
        var counts = new int[GeneRules.Count, 32];
        for (var i = 0; i < sets; i++)
        {
            var genes = GeneRules.Roll(config, rng);
            Assert.Equal(GeneRules.Count, genes.Length);
            for (var g = 0; g < GeneRules.Count; g++)
            {
                Assert.InRange(genes[g], 0, 31);
                counts[g, genes[g]]++;
            }
        }

        // 2.000 expected per value; ±12% is far outside the noise (σ ≈ 44) yet catches any bias.
        for (var g = 0; g < GeneRules.Count; g++)
        {
            for (var v = 0; v <= 31; v++)
            {
                Assert.InRange(counts[g, v], 1760, 2240);
            }
        }
    }

    [Fact]
    public void The_four_genes_are_independent()
    {
        var config = Config;
        var rng = new Rng(0x6E4E_5002UL);
        const int sets = 64_000;
        var rolls = new int[sets][];
        for (var i = 0; i < sets; i++)
        {
            rolls[i] = GeneRules.Roll(config, rng);
        }

        for (var a = 0; a < GeneRules.Count; a++)
        {
            for (var b = a + 1; b < GeneRules.Count; b++)
            {
                var correlation = Correlation(rolls.Select(r => (double)r[a]).ToArray(), rolls.Select(r => (double)r[b]).ToArray());
                Assert.InRange(correlation, -0.02, 0.02);
            }
        }

        // Every pair of values shows up (1.024 pairs, ~62 each): a high gene does not make the others high or low.
        var pairs = new HashSet<(int, int)>(rolls.Select(r => (r[GeneRules.Attack], r[GeneRules.Defense])));
        Assert.Equal(32 * 32, pairs.Count);
    }

    [Fact]
    public void Catches_get_genes_from_their_own_stream_and_keep_them()
    {
        var (game, clock, dir) = TestSupport.NewGame(seed: TestSupport.FixedSeed);
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 600, stepSeconds: 10);
        var save = game.Session.Save;
        Assert.NotEmpty(save.FishingBox);
        Assert.All(save.FishingBox, c => Assert.True(GeneRules.IsValid(game.Session.Config, c.Genes)));
        // Not all the same: the genes are rolled, not a constant.
        Assert.True(save.FishingBox.Select(c => string.Join(",", c.Genes)).Distinct().Count() > save.FishingBox.Count / 2);
        // The same catch id always gives the same genes (a repeated cycle cannot reroll them).
        var first = save.FishingBox[0];
        Assert.Equal(first.Genes, GeneRules.RollForCatch(game.Session.Config, save.RngSeed, first.Id));

        // The view shows them; keeping the fish carries them to the Aquarium.
        var view = game.Fishing.GetFishingBox().Single(c => c.CatchId == first.Id);
        Assert.Equal(first.Genes[GeneRules.Attack], view.Genes.Attack);
        var genes = (int[])first.Genes.Clone();
        var kept = game.Aquarium.KeepCatches(new[] { first.Id }).Value.Kept.Single();
        var instance = save.Aquarium.Single(f => f.Id == kept.FishId);
        Assert.Equal(genes, instance.Genes);
        Assert.Equal(genes[GeneRules.Speed], kept.Genes.Speed);

        // Saving and loading keeps them, in the Box and in the Aquarium.
        game.Fishing.StopFishing();
        var boxGenes = save.FishingBox.ToDictionary(c => c.Id, c => string.Join(",", c.Genes));
        game.Session.Persist();
        var reopened = LocalGame.Start(TestSupport.CertainCatchConfig(), new JsonFilePlayerRepository(dir), clock, _ => { }).Game;
        Assert.Equal(genes, reopened.Session.Save.Aquarium.Single(f => f.Id == kept.FishId).Genes);
        foreach (var c in reopened.Session.Save.FishingBox.Where(c => boxGenes.ContainsKey(c.Id)))
        {
            Assert.Equal(boxGenes[c.Id], string.Join(",", c.Genes));
        }
    }

    // ------------------------------------------------------------------ the effect on the attributes

    [Fact]
    public void A_gene_scales_its_own_attribute_from_090_to_110()
    {
        var config = Config;
        Assert.Equal(0.90, GeneRules.Multiplier(config, 0), 9);
        Assert.Equal(1.10, GeneRules.Multiplier(config, 31), 9);
        Assert.Equal(0.90 + 0.20 * 16 / 31.0, GeneRules.Multiplier(config, 16), 9);
        Assert.InRange(GeneRules.Multiplier(config, 16), 1.0, 1.004);   // the neutral gene of the migration: ×1,003

        config.TryGetSpecies("tilapia", out var tilapia);
        var plain = FishRules.Stats(config, tilapia, 420, 5, "adult");
        var mixed = FishRules.Stats(config, tilapia, 420, 5, "adult", new[] { 31, 0, 16, 31 });
        Assert.Equal(plain.Hp * 1.10, mixed.Hp, 6);
        Assert.Equal(plain.Attack * 0.90, mixed.Attack, 6);
        Assert.Equal(plain.Defense * GeneRules.Multiplier(config, 16), mixed.Defense, 6);
        Assert.Equal(plain.Speed * 1.10, mixed.Speed, 6);

        // A broken set (wrong length, out of range) counts as neutral, never as a crash or a free bonus.
        var broken = FishRules.Stats(config, tilapia, 420, 5, "adult", new[] { 99, 99 });
        var neutral = FishRules.Stats(config, tilapia, 420, 5, "adult", GeneRules.Neutral(config));
        Assert.Equal(neutral.Attack, broken.Attack, 9);
    }

    [Fact]
    public void On_average_the_genes_leave_the_balance_where_it_was()
    {
        var config = Config;
        config.TryGetSpecies("tilapia", out var tilapia);
        var plain = FishRules.Stats(config, tilapia, 420, 10, "adult");
        var rng = new Rng(0x6E4E_5003UL);
        const int fish = 100_000;
        double hp = 0, attack = 0, defense = 0, speed = 0;
        for (var i = 0; i < fish; i++)
        {
            var s = FishRules.Stats(config, tilapia, 420, 10, "adult", GeneRules.Roll(config, rng));
            hp += s.Hp / plain.Hp;
            attack += s.Attack / plain.Attack;
            defense += s.Defense / plain.Defense;
            speed += s.Speed / plain.Speed;
        }

        Assert.InRange(hp / fish, 0.997, 1.003);
        Assert.InRange(attack / fish, 0.997, 1.003);
        Assert.InRange(defense / fish, 0.997, 1.003);
        Assert.InRange(speed / fish, 0.997, 1.003);
    }

    [Fact]
    public void The_aquarium_view_and_the_clan_strength_use_the_fish_genes()
    {
        var (game, clock, _) = TestSupport.NewGame(seed: TestSupport.FixedSeed);
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 120, stepSeconds: 10);
        var id = game.Aquarium.KeepCatches(game.Fishing.GetFishingBox().Take(1).Select(c => c.CatchId).ToList()).Value.Kept[0].FishId;
        var save = game.Session.Save;
        var instance = save.Aquarium.Single(f => f.Id == id);
        instance.Genes = new[] { 31, 31, 0, 0 };
        var config = game.Session.Config;
        config.TryGetSpecies(instance.SpeciesId, out var species);
        var expected = FishRules.Stats(config, species, instance.SizeMm, instance.Level, instance.SizeCategoryId, instance.Genes);

        var view = game.Aquarium.GetFish(id);
        Assert.Equal(expected.Attack, view.Stats.Attack, 9);
        Assert.Equal(expected.Defense, view.Stats.Defense, 9);
        Assert.Equal(31, view.Genes.Hp);
        Assert.True(view.Genes.HpPerfect && view.Genes.AttackPerfect && !view.Genes.DefensePerfect);
        Assert.Equal(2, view.Genes.PerfectCount);

        var strength = Clans.ClanRules.FishStrength(config, instance);
        Assert.Equal(Profile.CardumeRules.Display(config, Profile.CardumeRules.RawStrength(config, expected)), strength);
    }

    // ------------------------------------------------------------------ quality and stars

    [Theory]
    [InlineData(0, 0, 0, 0, 1)]
    [InlineData(9, 9, 9, 9, 1)]       // 36/124 = 29%
    [InlineData(10, 9, 9, 10, 2)]     // 38/124 = 30,6%
    [InlineData(16, 16, 16, 16, 2)]   // the neutral set: 51,6%
    [InlineData(17, 17, 17, 18, 3)]   // 69/124 = 55,6%
    [InlineData(24, 23, 23, 23, 4)]   // 93/124 = 75%
    [InlineData(27, 28, 28, 28, 4)]   // 111/124 = 89,5%
    [InlineData(28, 28, 28, 28, 5)]   // 112/124 = 90,3%
    [InlineData(31, 31, 31, 31, 5)]
    public void Stars_follow_the_configured_bands(int hp, int attack, int defense, int speed, int stars)
    {
        var view = GeneRules.View(Config, new[] { hp, attack, defense, speed });
        Assert.Equal(stars, view.Stars);
        Assert.Equal(hp + attack + defense + speed, view.Total);
        Assert.Equal(view.Total / 124.0, view.Quality, 9);
        Assert.Equal(5, view.MaxStars);
        Assert.Equal(stars >= 5, view.IsCelebrated);
    }

    [Fact]
    public void Four_perfect_genes_make_the_perfect_specimen()
    {
        var perfect = GeneRules.View(Config, new[] { 31, 31, 31, 31 });
        Assert.True(perfect.IsPerfectSpecimen);
        Assert.Equal(4, perfect.PerfectCount);
        Assert.Equal(1.0, perfect.Quality, 9);

        var almost = GeneRules.View(Config, new[] { 31, 31, 31, 30 });
        Assert.False(almost.IsPerfectSpecimen);
        Assert.Equal(3, almost.PerfectCount);
        Assert.False(almost.SpeedPerfect);
    }

    [Fact]
    public void Five_star_fish_are_rare_but_exist()
    {
        var config = Config;
        var rng = new Rng(0x6E4E_5004UL);
        const int fish = 200_000;
        var counts = new int[6];
        for (var i = 0; i < fish; i++)
        {
            counts[GeneRules.View(config, GeneRules.Roll(config, rng)).Stars]++;
        }

        // Exact odds: 1★ 9,6%, 2★ 53,7%, 3★ 31,7%, 4★ 4,8%, 5★ 0,17%.
        Assert.InRange(counts[1] / (double)fish, 0.088, 0.104);
        Assert.InRange(counts[2] / (double)fish, 0.525, 0.549);
        Assert.InRange(counts[3] / (double)fish, 0.307, 0.327);
        Assert.InRange(counts[4] / (double)fish, 0.044, 0.052);
        Assert.InRange(counts[5] / (double)fish, 0.0012, 0.0023);
    }

    // ------------------------------------------------------------------ economy and protection

    [Fact]
    public void The_npc_price_never_depends_on_the_genes()
    {
        var (game, clock, _) = TestSupport.NewGame(seed: TestSupport.FixedSeed);
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 300, stepSeconds: 10);
        game.Fishing.StopFishing();
        var save = game.Session.Save;
        var entry = save.FishingBox[0];
        var before = game.Fishing.GetFishingBox().Single(c => c.CatchId == entry.Id).SalePriceCoins;
        entry.Genes = new[] { 31, 31, 31, 31 };
        Assert.Equal(before, game.Fishing.GetFishingBox().Single(c => c.CatchId == entry.Id).SalePriceCoins);
        entry.Genes = new[] { 0, 0, 0, 0 };
        Assert.Equal(before, game.Fishing.GetFishingBox().Single(c => c.CatchId == entry.Id).SalePriceCoins);

        var id = game.Aquarium.KeepCatches(new[] { entry.Id }).Value.Kept[0].FishId;
        var low = game.Aquarium.GetFish(id).SalePriceCoins;
        save.Aquarium.Single(f => f.Id == id).Genes = new[] { 31, 31, 31, 31 };
        Assert.Equal(low, game.Aquarium.GetFish(id).SalePriceCoins);
        Assert.Equal(before, low);
    }

    [Fact]
    public void A_five_star_fish_asks_before_a_bulk_sale_or_being_eaten()
    {
        var (game, clock, _) = TestSupport.NewGame(seed: TestSupport.FixedSeed);
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 300, stepSeconds: 10);
        game.Fishing.StopFishing();
        var save = game.Session.Save;
        var common = game.Fishing.GetFishingBox().Where(c => !c.IsProtected).Select(c => c.CatchId).ToList();
        Assert.True(common.Count >= 3);

        // In the Fishing Box: the 5★ catch is protected, a 4★ one is not.
        save.FishingBox.Single(c => c.Id == common[0]).Genes = new[] { 28, 28, 28, 28 };
        save.FishingBox.Single(c => c.Id == common[1]).Genes = new[] { 27, 28, 28, 28 };
        var box = game.Fishing.GetFishingBox();
        Assert.True(box.Single(c => c.CatchId == common[0]).IsProtected);
        Assert.False(box.Single(c => c.CatchId == common[1]).IsProtected);
        var sale = game.Fishing.PreviewSale(new[] { common[0], common[1] });
        Assert.True(sale.NeedsConfirmation);
        Assert.Single(sale.ProtectedCatches);

        // In the Aquarium: the 5★ fish asks before a bulk sale and before being food.
        Assert.True(game.Aquarium.KeepCatches(new[] { common[0] }).Succeeded);
        var five = game.Aquarium.GetFish(save.Aquarium.Single(f => f.SourceCatchId == common[0]).Id);
        Assert.Equal(5, five.Genes.Stars);
        Assert.True(five.IsValuableFood);
        Assert.True(game.Aquarium.PreviewSale(new[] { five.FishId }).NeedsConfirmation);
    }

    [Fact]
    public void The_aquarium_sorts_by_quality()
    {
        var (game, clock, _) = TestSupport.NewGame(seed: TestSupport.FixedSeed);
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 300, stepSeconds: 10);
        game.Aquarium.KeepCatches(game.Fishing.GetFishingBox().Take(8).Select(c => c.CatchId).ToList());
        var totals = game.Aquarium.GetAquarium(AquariumSort.Quality).Fish.Select(f => f.Genes.Total).ToList();
        Assert.Equal(8, totals.Count);
        Assert.Equal(totals.OrderByDescending(t => t), totals);
    }

    // ------------------------------------------------------------------ the simulated players' fish

    [Fact]
    public void Market_and_auction_bots_sell_fish_with_rolled_genes()
    {
        var (game, clock, _) = TestSupport.NewGame(seed: TestSupport.FixedSeed);
        game.Market.Search(new MarketQuery());
        game.Auctions.GetAuctions();
        clock.AdvanceSeconds(3 * 3600);
        game.Market.Update();
        var config = game.Session.Config;
        var state = game.Session.Save.Market;
        var fish = state.BotListings.Select(l => l.Goods.Fish).Concat(state.Auctions.Select(a => a.Goods.Fish)).Where(f => f != null).ToList();
        Assert.NotEmpty(fish);
        Assert.All(fish, f => Assert.True(GeneRules.IsValid(config, f.Genes)));
        Assert.True(fish.Select(f => string.Join(",", f.Genes)).Distinct().Count() > 1);

        // The buyer sees them on the listing.
        var listing = game.Market.Search(new MarketQuery { Kind = MarketKindFilter.Fish, Sort = MarketSort.QualityDescending });
        Assert.NotEmpty(listing);
        Assert.All(listing, l => Assert.NotNull(l.Goods.Fish.Genes));
        var totals = listing.Select(l => l.Goods.Fish.Genes.Total).ToList();
        Assert.Equal(totals.OrderByDescending(t => t), totals);
    }

    [Fact]
    public void Arena_bots_fish_have_genes_too()
    {
        // Each attribute has its own gene, so a bot fish's Ataque / Vida ratio moves away from its species' ratio
        // (the complete-Cardume bonus multiplies all four alike and keeps the ratio).
        var config = Config;
        var moved = 0;
        for (var index = 0; index < config.ArenaBots.BotCount; index++)
        {
            var bot = ArenaBots.Build(config, index);
            Assert.Equal(bot.Cardume.Select(f => f?.Stats.Attack), ArenaBots.Build(config, index).Cardume.Select(f => f?.Stats.Attack));
            foreach (var fighter in bot.Cardume.Where(f => f != null))
            {
                config.TryGetSpecies(fighter.SpeciesId, out var species);
                if (species.BaseStats.Attack <= 0)
                {
                    continue;
                }

                var ratio = fighter.Stats.Attack / fighter.Stats.Hp / (species.BaseStats.Attack / species.BaseStats.Hp);
                Assert.InRange(ratio, 0.9 / 1.1 - 1e-9, 1.1 / 0.9 + 1e-9);
                if (Math.Abs(ratio - 1) > 1e-6)
                {
                    moved++;
                }
            }
        }

        Assert.True(moved > 0);
    }

    // ------------------------------------------------------------------ saves before the genes

    [Fact]
    public void A_version_22_save_gives_every_fish_the_neutral_genes()
    {
        var (game, clock, dir) = TestSupport.NewGame(seed: TestSupport.FixedSeed);
        game.Session.Save.Coins = 1_000_000;
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 600, stepSeconds: 30);
        game.Fishing.StopFishing();
        var keep = game.Fishing.GetFishingBox().Take(3).Select(c => c.CatchId).ToList();
        var kept = game.Aquarium.KeepCatches(keep).Value.Kept;
        game.Cardume.SetSlot(1, kept[0].FishId);
        Assert.True(game.Market.ListFish(kept[2].FishId, 5_000).Succeeded);
        game.Market.Search(new MarketQuery());
        game.Fishing.MarkSeen();
        game.Session.Persist();

        var path = Path.Combine(dir, JsonFilePlayerRepository.SaveFileName);
        var json = JObject.Parse(File.ReadAllText(path));
        json["save_version"] = 22;
        var removed = StripGenes(json);
        Assert.True(removed > 3);
        File.WriteAllText(path, json.ToString());
        File.Delete(Path.Combine(dir, JsonFilePlayerRepository.BackupFileName));

        var reopened = LocalGame.Start(TestSupport.CertainCatchConfig(), new JsonFilePlayerRepository(dir), clock, _ => { });
        Assert.True(reopened.Succeeded, string.Join("\n", reopened.Errors));
        var opened = reopened.Game;
        Assert.Equal(SaveLoadStatus.Loaded, opened.Session.LoadStatus);
        var save = opened.Session.Save;
        Assert.Equal(PlayerSave.CurrentVersion, save.SaveVersion);
        Assert.Equal(23, PlayerSave.CurrentVersion);
        var neutral = new[] { 16, 16, 16, 16 };
        Assert.All(save.FishingBox, c => Assert.Equal(neutral, c.Genes));
        Assert.All(save.Aquarium, f => Assert.Equal(neutral, f.Genes));
        Assert.All(save.Market.MyListings.Select(l => l.Goods.Fish).Where(f => f != null), f => Assert.Equal(neutral, f.Genes));
        Assert.All(save.Market.BotListings.Select(l => l.Goods.Fish).Where(f => f != null), f => Assert.Equal(neutral, f.Genes));

        // Neither stronger nor weaker in any relevant way: ×1,003 on each attribute, 2 stars, no celebration.
        var config = opened.Session.Config;
        var fish = save.Aquarium.Single(f => f.Id == kept[0].FishId);
        config.TryGetSpecies(fish.SpeciesId, out var species);
        var before = FishRules.Stats(config, species, fish.SizeMm, fish.Level, fish.SizeCategoryId);
        var view = opened.Aquarium.GetFish(fish.Id);
        Assert.InRange(view.Stats.Attack / before.Attack, 1.0, 1.004);
        Assert.Equal(2, view.Genes.Stars);
        Assert.False(view.Genes.IsCelebrated);

        // Running the migration again changes nothing; a new catch still gets rolled genes.
        Assert.Equal(0, GeneMigration.GiveMissingGenes(config, save));
        opened.Fishing.StartFishing();
        TestSupport.PlayFor(opened, clock, 120, stepSeconds: 10);
        var fresh = save.FishingBox.Where(c => !keep.Contains(c.Id)).OrderByDescending(c => c.Id).Take(10).ToList();
        Assert.Contains(fresh, c => !c.Genes.SequenceEqual(neutral));
    }

    // ------------------------------------------------------------------ the config

    [Fact]
    public void The_real_config_has_valid_genes()
    {
        var genes = Config.Progression.Genes;
        Assert.Equal(31, genes.MaxValue);
        Assert.Equal(0.90, genes.StatMultiplierMin, 9);
        Assert.Equal(1.10, genes.StatMultiplierMax, 9);
        Assert.Equal(16, genes.MigrationNeutralValue);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, genes.QualityStars.Select(b => b.Stars));
        Assert.Equal(new[] { 0.0, 30, 55, 75, 90 }, genes.QualityStars.Select(b => b.MinPercent));
        Assert.Equal(5, Config.Economy.FishingBox.BulkSaleProtection.GeneStarsAtOrAbove);
        Assert.Equal(5, Config.Progression.Feeding.ValuableFeedRules.GeneStarsAtOrAbove);
    }

    [Theory]
    [InlineData("max_value", 0)]
    [InlineData("max_value", 300)]
    [InlineData("stat_multiplier_min", 0)]
    [InlineData("stat_multiplier_max", 0.5)]
    [InlineData("migration_neutral_value", 40)]
    [InlineData("celebrate_min_stars", 6)]
    public void A_broken_genes_section_is_refused(string key, object value)
    {
        var result = TestSupport.ConfigWith(GameConfigLoader.ProgressionFile, text =>
        {
            var root = JObject.Parse(text);
            root["genes"]![key] = JToken.FromObject(value);
            return root.ToString();
        });
        Assert.False(result.Succeeded);
        Assert.Contains(GameTexts.Validation.GenesInvalid, result.Errors);
    }

    [Fact]
    public void Star_bands_out_of_order_or_a_missing_section_are_refused()
    {
        var unordered = TestSupport.ConfigWith(GameConfigLoader.ProgressionFile, text =>
        {
            var root = JObject.Parse(text);
            root["genes"]!["quality_stars"]![2]!["min_percent"] = 20;
            return root.ToString();
        });
        Assert.Contains(GameTexts.Validation.GenesInvalid, unordered.Errors);

        var missing = TestSupport.ConfigWith(GameConfigLoader.ProgressionFile, text =>
        {
            var root = JObject.Parse(text);
            root.Remove("genes");
            return root.ToString();
        });
        Assert.False(missing.Succeeded);

        var protection = TestSupport.ConfigWith(GameConfigLoader.EconomyFile, text =>
        {
            var root = JObject.Parse(text);
            root["fishing_box"]!["bulk_sale_protection"]!["gene_stars_at_or_above"] = 9;
            return root.ToString();
        });
        Assert.Contains(GameTexts.Validation.GeneProtectionInvalid, protection.Errors);
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>Removes every gene from a save file the way a version 22 game wrote it: "g" on box catches, "genes" on fish.</summary>
    internal static int StripGenes(JToken token)
    {
        var removed = 0;
        if (token is JObject obj)
        {
            foreach (var name in new[] { "genes", "g" })
            {
                if (obj.Remove(name))
                {
                    removed++;
                }
            }

            foreach (var property in obj.Properties().ToList())
            {
                removed += StripGenes(property.Value);
            }
        }
        else if (token is JArray array)
        {
            foreach (var item in array)
            {
                removed += StripGenes(item);
            }
        }

        return removed;
    }

    private static double Correlation(double[] a, double[] b)
    {
        var ma = a.Average();
        var mb = b.Average();
        double cov = 0, va = 0, vb = 0;
        for (var i = 0; i < a.Length; i++)
        {
            cov += (a[i] - ma) * (b[i] - mb);
            va += (a[i] - ma) * (a[i] - ma);
            vb += (b[i] - mb) * (b[i] - mb);
        }

        return cov / Math.Sqrt(va * vb);
    }
}
