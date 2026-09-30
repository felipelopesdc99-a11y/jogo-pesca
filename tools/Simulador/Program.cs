using System.Diagnostics;
using System.Text;
using FishingIdle.GameService;
using FishingIdle.GameService.Aquarium;
using FishingIdle.GameService.Arena;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Expeditions;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Market;
using FishingIdle.GameService.Persistence;
using FishingIdle.GameService.Profile;
using FishingIdle.Texts;

namespace FishingIdle.Simulador;

/// <summary>
/// Plays the real rules (the same LocalGame the Unity client runs) with a manual clock and writes
/// docs/relatorios/SIMULACAO_BALANCEAMENTO.md. It changes no balance value: it only measures.
/// </summary>
public static class Program
{
    private const long StartMs = 1_790_000_000_000;
    private const int Players = 5;

    /// <summary>Observations worth the owner's attention, collected while measuring; listed at the top.</summary>
    private static readonly List<string> Attention = new();

    public static int Main(string[] args)
    {
        var root = FindRepositoryRoot();
        var output = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]) ? Path.GetFullPath(args[0]) : Path.Combine(root, "docs", "relatorios", "SIMULACAO_BALANCEAMENTO.md");
        var load = GameConfigLoader.LoadFromDirectory(Path.Combine(root, "config"));
        if (!load.Succeeded)
        {
            Console.WriteLine("O balanceamento em /config tem problemas; corrija antes de simular:");
            foreach (var e in load.Errors)
            {
                Console.WriteLine("  • " + e);
            }

            return 1;
        }

        var config = load.Config;
        var watch = Stopwatch.StartNew();
        var report = new StringBuilder();
        report.AppendLine("# Relatório de simulação do balanceamento");
        report.AppendLine();
        report.AppendLine("Gerado por `./ops/scripts/simular.sh` (ferramenta em `tools/Simulador`). Ele joga as **regras reais**");
        report.AppendLine("do jogo com um relógio simulado e mede os números atuais de `/config`. Não muda nenhum valor: serve");
        report.AppendLine("para decidir o balanceamento com dados. Rode de novo depois de editar o balanceamento.");
        report.AppendLine();
        report.AppendLine("- Versão do balanceamento: `" + config.Version + "`");
        report.AppendLine("- Jogadores simulados por medição: " + Players + " (a tabela mostra a média)");
        report.AppendLine();

        var body = new StringBuilder();
        Console.WriteLine("Simulando progressão do Pescador...");
        var progression = Progression(config);
        WriteProgression(body, config, progression);

        Console.WriteLine("Sorteando capturas...");
        WriteCatchTables(body, config);

        Console.WriteLine("Calculando economia...");
        WriteEconomy(body, config, progression);

        Console.WriteLine("Simulando batalhas...");
        WriteCombat(body, config);

        Console.WriteLine("Calculando Expedições...");
        WriteExpeditions(body, config, progression);

        Console.WriteLine("Simulando Mercado e Leilão...");
        WriteMarket(body, config);

        Console.WriteLine("Medindo o Sucesso da Captura...");
        WriteCatchSuccess(body, config, progression, CertainCatch(root));

        report.AppendLine("## Pontos de atenção");
        report.AppendLine();
        report.AppendLine("Observações automáticas sobre os números atuais. São só fatos medidos: decidir se algo muda é do");
        report.AppendLine("proprietário (o balanceamento ainda não foi feito de propósito).");
        report.AppendLine();
        foreach (var line in Attention)
        {
            report.AppendLine("- " + line);
        }

        report.AppendLine();
        report.Append(body);

        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        File.WriteAllText(output, report.ToString(), new UTF8Encoding(false));
        Console.WriteLine("Relatório gravado em " + Path.GetRelativePath(root, output) + " (" + Format.Decimal(watch.Elapsed.TotalSeconds, 1) + " s).");
        return 0;
    }

    // ------------------------------------------------------------------ progression

    private sealed class LevelMark
    {
        public double Hours;
        public long CoinsEarned;
    }

    private sealed class MapStay
    {
        public double ArrivedAtHours;
        public double Hours;
        public double Coins;
        public double Shells;
        public double Xp;
        public int Players;
    }

    private sealed class ProgressionResult
    {
        /// <summary>Per level (2..max): average online hours and coins earned (all sold) to reach it.</summary>
        public SortedDictionary<int, LevelMark> Levels = new();

        /// <summary>Per map id: when the players got there and what an hour there gave, on average.</summary>
        public Dictionary<string, MapStay> Maps = new();

        /// <summary>Per rod name: when it was bought, on average.</summary>
        public List<string> RodsBought = new();
        public double RodBoughtAtHours;
        public double Map2At => At("map_02");
        public double CoinsPerHourMap1 => CoinsPerHour("map_01");
        public double CoinsPerHourMap2 => CoinsPerHour("map_02");
        public double ShellsPerHourMap2 => Maps.TryGetValue("map_02", out var m) && m.Hours > 0 ? m.Shells / m.Hours : 0;
        public double CatchesPerHour;
        public double FirstRareAtHours;
        public double EscapesPerHour;
        public List<string> BoatsBought = new();

        public double At(string mapId) => Maps.TryGetValue(mapId, out var m) && m.Players > 0 ? m.ArrivedAtHours / m.Players : 0;
        public double CoinsPerHour(string mapId) => Maps.TryGetValue(mapId, out var m) && m.Hours > 0 ? m.Coins / m.Hours : 0;
        public double XpPerHour(string mapId) => Maps.TryGetValue(mapId, out var m) && m.Hours > 0 ? m.Xp / m.Hours : 0;
    }

    /// <summary>
    /// A simple, sensible player fishing online non-stop: sells everything every 10 minutes, buys each
    /// rod as soon as allowed and affordable, buys boats with what is left (saving up when the next rod
    /// is 3 levels away), and travels to the best map it can as soon as it opens.
    /// </summary>
    private static ProgressionResult Progression(GameConfig config)
    {
        var result = new ProgressionResult();
        var sums = new Dictionary<int, (double hours, long coins, int n)>();
        double catches = 0, totalHours = 0, firstRare = 0, escapes = 0;
        var boatTimes = new Dictionary<string, (double hours, int n)>();
        var rodTimes = new Dictionary<string, (double hours, int n)>();
        var buyable = config.Rods.Rods.Where(config.IsPurchasable).OrderBy(r => r.Tier).ToList();
        var mapOrder = config.Maps.Maps.OrderBy(m => m.UnlockFisherLevel).Select(m => m.Id).ToList();
        const double capHours = 150;

        MapStay Stay(string mapId)
        {
            if (!result.Maps.TryGetValue(mapId, out var stay))
            {
                stay = new MapStay();
                result.Maps[mapId] = stay;
            }

            return stay;
        }

        for (var p = 0; p < Players; p++)
        {
            var (game, clock) = NewPlayer(config, 1000 + (ulong)p);
            game.Fishing.StartFishing();
            long earned = 0;
            var lastLevel = 1;
            var bought = new HashSet<string>();
            var steps = 0;
            double hours = 0;
            double rareAt = -1;
            var mapId = game.Player.GetPlayer().MapId;
            Stay(mapId).Players++;

            while (hours < capHours && game.Player.GetPlayer().FisherLevel < config.Progression.Fisher.MaxLevel)
            {
                clock.AdvanceSeconds(30);
                steps++;
                hours = steps * 30 / 3600.0;
                game.Maps.Update();
                var sync = game.Fishing.Sync();
                var stay = Stay(mapId);
                stay.Hours += 30 / 3600.0;
                stay.Xp += sync.XpGained;
                stay.Shells += sync.ShellsGained;
                if (rareAt < 0 && sync.NewCatches.Any(c => c.RarityId != "common"))
                {
                    rareAt = hours;
                }

                if (steps % 20 == 0)
                {
                    var box = game.Fishing.GetFishingBox().Select(c => c.CatchId).ToList();
                    if (box.Count > 0)
                    {
                        var sale = game.Fishing.SellCatches(box);
                        if (sale.Succeeded)
                        {
                            earned += sale.Value.CoinsGained;
                            stay.Coins += sale.Value.CoinsGained;
                        }
                    }

                    foreach (var rod in buyable.Where(r => !bought.Contains(r.Id)))
                    {
                        if (game.Shop.BuyRod(rod.Id).Succeeded)
                        {
                            bought.Add(rod.Id);
                            var t = rodTimes.TryGetValue(rod.DisplayName, out var rt) ? rt : (0, 0);
                            rodTimes[rod.DisplayName] = (t.hours + hours, t.n + 1);
                        }
                    }

                    // Boats with what is left: never while a rod the player could already use is not
                    // bought, and not in the 3 levels before the next rod (saving up for it).
                    var level = game.Player.GetPlayer().FisherLevel;
                    var nextRod = buyable.FirstOrDefault(r => !bought.Contains(r.Id));
                    if (nextRod == null || level < nextRod.Acquisition.UnlockFisherLevel - 3)
                    {
                        foreach (var boat in game.Gear.GetGear().Boats.Where(x => !x.Owned && x.BuyBlocker == ServiceError.None))
                        {
                            if (game.Gear.BuyBoat(boat.BoatId).Succeeded)
                            {
                                var t = boatTimes.TryGetValue(boat.Name, out var bt) ? bt : (0, 0);
                                boatTimes[boat.Name] = (t.hours + hours, t.n + 1);
                            }
                        }
                    }

                    var best = game.Maps.GetMaps().Maps
                        .Where(m => m.MapId != mapId && m.TravelBlocker == ServiceError.None && mapOrder.IndexOf(m.MapId) > mapOrder.IndexOf(mapId))
                        .OrderByDescending(m => mapOrder.IndexOf(m.MapId))
                        .FirstOrDefault();
                    if (best != null && game.Maps.TravelTo(best.MapId).Succeeded)
                    {
                        mapId = best.MapId;
                        Stay(mapId).ArrivedAtHours += hours;
                        Stay(mapId).Players++;
                    }
                }

                var reached = game.Player.GetPlayer().FisherLevel;
                while (lastLevel < reached)
                {
                    lastLevel++;
                    var s = sums.TryGetValue(lastLevel, out var v) ? v : (0, 0, 0);
                    sums[lastLevel] = (s.hours + hours, s.coins + earned, s.n + 1);
                }
            }

            var end = game.Player.GetPlayer();
            catches += end.TotalCatches;
            escapes += game.Session.Save.Stats.Escapes;
            firstRare += rareAt < 0 ? hours : rareAt;
            totalHours += hours;
        }

        foreach (var (level, v) in sums)
        {
            result.Levels[level] = new LevelMark { Hours = v.hours / v.n, CoinsEarned = v.coins / v.n };
        }

        result.RodBoughtAtHours = buyable.Count > 0 && rodTimes.TryGetValue(buyable[0].DisplayName, out var first) ? first.hours / first.n : 0;
        foreach (var (name, t) in rodTimes)
        {
            result.RodsBought.Add(name + " com " + Format.Decimal(t.hours / t.n, 1) + " h" + (t.n < Players ? " (" + t.n + " de " + Players + " jogadores)" : ""));
        }

        result.CatchesPerHour = totalHours > 0 ? catches / totalHours : 0;
        result.EscapesPerHour = totalHours > 0 ? escapes / totalHours : 0;
        result.FirstRareAtHours = firstRare / Players;
        foreach (var (name, t) in boatTimes)
        {
            result.BoatsBought.Add(name + " com " + Format.Decimal(t.hours / t.n, 1) + " h" + (t.n < Players ? " (" + t.n + " de " + Players + " jogadores)" : ""));
        }

        return result;
    }

    private static void WriteProgression(StringBuilder r, GameConfig config, ProgressionResult p)
    {
        r.AppendLine("## 1. Progressão do Pescador (pesca online, sem parar)");
        r.AppendLine();
        r.AppendLine("Estratégia simulada: pesca o tempo todo, vende tudo a cada 10 minutos, compra a próxima vara assim");
        r.AppendLine("que pode, depois os barcos, e viaja para o próximo mapa assim que ele libera. Não usa isca. Offline, cada captura leva " +
                     Format.Duration(config.Progression.Fishing.OfflineCycleSeconds) + " em vez de " + Format.Duration(config.Progression.Fishing.OnlineCycleSeconds) + ".");
        r.AppendLine();
        r.AppendLine("| Nível | Horas de pesca online | Moedas ganhas até ali |");
        r.AppendLine("|---:|---:|---:|");
        foreach (var (level, mark) in p.Levels.Where(x => x.Key <= 20 || x.Key % 10 == 0))
        {
            r.AppendLine("| " + level + " | " + Format.Decimal(mark.Hours, 1) + " h | " + Format.Number(mark.CoinsEarned) + " |");
        }

        r.AppendLine();
        r.AppendLine("Até o Nível 20 aparecem todos os níveis; depois, de 10 em 10. A simulação para no nível máximo ou com 150 h.");
        r.AppendLine();
        r.AppendLine("- Capturas por hora online: " + Format.Decimal(p.CatchesPerHour, 0));
        if (p.Levels.TryGetValue(10, out var l10)) Attention.Add("Nível 10 (libera o segundo mapa) chega com " + Format.Decimal(l10.Hours, 1) + " h de pesca online (" + Format.Decimal(l10.Hours * config.Progression.Fishing.OfflineCycleSeconds / config.Progression.Fishing.OnlineCycleSeconds, 1) + " h se fosse só offline).");
        if (p.Levels.TryGetValue(20, out var l20)) Attention.Add("Nível 20 chega com " + Format.Decimal(l20.Hours, 1) + " h de pesca online.");
        if (p.Levels.TryGetValue(20, out var a20) && p.Levels.TryGetValue(30, out var a30)) Attention.Add("Do Nível 20 ao 30 (Pantanal Dourado): " + Format.Decimal(a30.Hours - a20.Hours, 1) + " h online (meta de docs/PROGRESSAO_MAPAS_3_4.md: ~3,8 h, com 100% de captura).");
        if (p.Levels.TryGetValue(30, out var b30) && p.Levels.TryGetValue(40, out var b40)) Attention.Add("Do Nível 30 ao 40 (Estuário das Marés): " + Format.Decimal(b40.Hours - b30.Hours, 1) + " h online (meta: ~4,0 h, com 100% de captura).");
        foreach (var rod in p.RodsBought) r.AppendLine("- " + rod + " de pesca");
        r.AppendLine();
        r.AppendLine("| Mapa | Chegada | XP por hora | Moedas por hora (vendendo tudo) | Conchas por hora |");
        r.AppendLine("|---|---:|---:|---:|---:|");
        foreach (var map in config.Maps.Maps.OrderBy(m => m.UnlockFisherLevel))
        {
            if (!p.Maps.TryGetValue(map.Id, out var stay) || stay.Hours <= 0) continue;
            r.AppendLine("| " + map.DisplayName + " | " + Format.Decimal(p.At(map.Id), 1) + " h | " + Format.Number((long)p.XpPerHour(map.Id)) + " | " + Format.Number((long)p.CoinsPerHour(map.Id)) + " | " + Format.Decimal(stay.Shells / stay.Hours, 1) + " |");
        }

        r.AppendLine();
        foreach (var boat in p.BoatsBought) r.AppendLine("- " + boat + " de pesca");
        r.AppendLine();
    }

    // ------------------------------------------------------------------ catches

    private static void WriteCatchTables(StringBuilder r, GameConfig config)
    {
        r.AppendLine("## 2. Frequência de raridade e de tamanho");
        r.AppendLine();
        r.AppendLine("200.000 sorteios reais (`CatchRules.Roll`) por combinação de mapa e vara: o que morde, antes da Chance de Sucesso (seção 7).");
        r.AppendLine();
        var combos = new List<(string label, MapConfig map, RodConfig rod, int level)>();
        foreach (var map in config.Maps.Maps.OrderBy(m => m.UnlockFisherLevel))
        {
            foreach (var rod in config.Rods.Rods.Where(x => x.Tier >= map.MinimumRodTier).OrderBy(x => x.Tier))
            {
                combos.Add((map.DisplayName + " · " + rod.DisplayName + (rod.HasInternalLevels ? " Nv.1" : ""), map, rod, 1));
                if (rod.HasInternalLevels)
                {
                    combos.Add((map.DisplayName + " · " + rod.DisplayName + " Nv." + config.RodMaxLevel(rod), map, rod, config.RodMaxLevel(rod)));
                }
            }
        }

        var rarities = config.Progression.Rarity.Tiers;
        var sizes = config.SizeCategories;
        r.AppendLine("| Mapa · vara | " + string.Join(" | ", rarities.Select(x => x.DisplayName)) + " | " + string.Join(" | ", sizes.Select(x => x.DisplayName)) + " | Conchas por 100 capturas |");
        r.AppendLine("|---|" + string.Concat(Enumerable.Repeat("---:|", rarities.Count + sizes.Count + 1)));
        foreach (var (label, map, rod, level) in combos)
        {
            const int n = 200_000;
            var rng = new Rng(42);
            var byRarity = rarities.ToDictionary(x => x.Id, _ => 0);
            var bySize = sizes.ToDictionary(x => x.Id, _ => 0);
            long shells = 0;
            for (var i = 0; i < n; i++)
            {
                var c = CatchRules.Roll(config, map, rod, level, rng);
                byRarity[c.Species.Rarity]++;
                bySize[c.SizeCategory.Id]++;
                shells += c.Shells;
            }

            var rare = rarities.Skip(1).Sum(x => byRarity[x.Id]) / (double)n;
            if (rare > 0 && level == 1 && rod.HasInternalLevels)
            {
                var pulled = rare * CatchRules.SuccessChance(config, rarities[1].Id, config.RodBonusesAt(rod, level).CatchSuccess + config.StarterBoat.CatchSuccessBonus);
                Attention.Add("Peixes acima de comum: " + Format.Percent(rare, 2) + " das mordidas em " + label + "; com a Chance de Sucesso, um puxado a cada ~" + Format.Number((long)Math.Round(1 / pulled)) + " tentativas (~" + Format.Decimal(1 / pulled / 120.0, 1) + " h online, sem barco nem isca).");
            }

            r.AppendLine("| " + label + " | " + string.Join(" | ", rarities.Select(x => Format.Percent(byRarity[x.Id] / (double)n, 2)))
                         + " | " + string.Join(" | ", sizes.Select(x => Format.Percent(bySize[x.Id] / (double)n, 2)))
                         + " | " + Format.Decimal(shells * 100.0 / n, 1) + " |");
        }

        r.AppendLine();
    }

    // ------------------------------------------------------------------ economy

    private static void WriteEconomy(StringBuilder r, GameConfig config, ProgressionResult p)
    {
        r.AppendLine("## 3. Economia");
        r.AppendLine();
        r.AppendLine("- Moedas por hora vendendo tudo, primeiro mapa: " + Format.Number((long)p.CoinsPerHourMap1));
        if (p.CoinsPerHourMap1 > 0 && p.CoinsPerHourMap2 > p.CoinsPerHourMap1 * 3)
        {
            Attention.Add("Ao chegar ao segundo mapa, as Moedas por hora sobem " + Format.Decimal(p.CoinsPerHourMap2 / p.CoinsPerHourMap1, 1) + "× (" + Format.Number((long)p.CoinsPerHourMap1) + " → " + Format.Number((long)p.CoinsPerHourMap2) + ").");
        }
        if (p.CoinsPerHourMap2 > 0)
        {
            r.AppendLine("- Moedas por hora vendendo tudo, segundo mapa (com a vara comprada, Nv.1): " + Format.Number((long)p.CoinsPerHourMap2));
            r.AppendLine("- Conchas por hora no segundo mapa: " + Format.Decimal(p.ShellsPerHourMap2, 1));
        }

        r.AppendLine();
        r.AppendLine("| Vara | Preço | Todas as melhorias | Horas de pesca para pagar a vara | Horas para pagar as melhorias |");
        r.AppendLine("|---|---:|---:|---:|---:|");
        foreach (var rod in config.Rods.Rods.Where(config.IsPurchasable).OrderBy(x => x.Tier))
        {
            var price = rod.Acquisition.PurchaseCostCoins;
            var upgrades = MarketRules.RodUpgradeSpend(config, rod, config.RodMaxLevel(rod));
            var before = p.CoinsPerHourMap1 > 0 ? price / p.CoinsPerHourMap1 : 0;
            var after = p.CoinsPerHourMap2 > 0 ? upgrades / p.CoinsPerHourMap2 : 0;
            r.AppendLine("| " + rod.DisplayName + " | " + Format.Number(price) + " | " + Format.Number(upgrades) + " | " + Format.Decimal(before, 1) + " h | " + Format.Decimal(after, 1) + " h |");
        }

        r.AppendLine();
        r.AppendLine("Preço de venda ao NPC por espécie (tamanho mínimo, médio e máximo da espécie):");
        r.AppendLine();
        r.AppendLine("| Espécie | Raridade | Mínimo | Médio | Máximo |");
        r.AppendLine("|---|---|---:|---:|---:|");
        foreach (var s in config.FishCatalog.Species.OrderBy(x => config.RarityRank(x.Rarity)).ThenBy(x => x.BaseSaleValueCoins))
        {
            config.TryGetRarity(s.Rarity, out var rarity);
            long At(double cm) => CatchRules.SalePrice(config, s, (int)Math.Round(cm * 10));
            r.AppendLine("| " + s.DisplayName + " | " + (rarity?.DisplayName ?? s.Rarity) + " | " + Format.Number(At(s.SizeCm.Min)) + " | "
                         + Format.Number(At((s.SizeCm.Min + s.SizeCm.Max) / 2)) + " | " + Format.Number(At(s.SizeCm.Max)) + " |");
        }

        r.AppendLine();
    }

    // ------------------------------------------------------------------ combat

    private static void WriteCombat(StringBuilder r, GameConfig config)
    {
        r.AppendLine("## 4. Combate (adversários simulados da Arena)");
        r.AppendLine();
        r.AppendLine("Cada linha: 500 batalhas do adversário de uma posição contra um ~10% acima dele (como um ataque");
        r.AppendLine("normal). Meta do GDD: a maioria das lutas em até ~" + Format.Decimal(config.Arena.Combat.TargetBattleDurationSeconds, 0) + " s.");
        r.AppendLine();
        var allDurations = new List<double>();
        r.AppendLine("| Posição do atacante | Alvo | Força do atacante | Força do alvo | Duração média | 90% das lutas até | Acima da meta | Atacante vence |");
        r.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|");
        var count = config.ArenaBots.BotCount;
        foreach (var rank in new[] { 2, 5, 10, 25, 50, 100, 150, count }.Where(x => x <= count).Distinct())
        {
            var attacker = ArenaBots.Build(config, rank - 1);
            var targetRank = Math.Max(1, rank - (int)Math.Ceiling(rank * config.Arena.OpponentSelection.RankWindowPercentAbove / 100.0));
            var target = ArenaBots.Build(config, targetRank - 1);
            var durations = new List<double>();
            var wins = 0;
            for (var i = 0; i < 500; i++)
            {
                var outcome = BattleEngine.Resolve(config, attacker.Cardume, target.Cardume, new Rng((ulong)(rank * 10_000 + i)));
                durations.Add(outcome.DurationSeconds);
                if (outcome.Winner == 0) wins++;
            }

            durations.Sort();
            allDurations.Add(durations.Average());
            var target60 = config.Arena.Combat.TargetBattleDurationSeconds;
            r.AppendLine("| #" + rank + " | #" + targetRank + " | " + Format.Number(Strength(config, attacker.Cardume)) + " | " + Format.Number(Strength(config, target.Cardume)) + " | "
                         + Format.Decimal(durations.Average(), 1) + " s | " + Format.Decimal(durations[(int)(durations.Count * 0.9)], 1) + " s | "
                         + Format.Percent(durations.Count(d => d > target60) / (double)durations.Count, 0) + " | " + Format.Percent(wins / 500.0, 0) + " |");
        }

        r.AppendLine();
        if (allDurations.Count > 0 && allDurations.Max() < config.Arena.Combat.TargetBattleDurationSeconds * 0.5)
        {
            Attention.Add("As batalhas duram em média " + Format.Decimal(allDurations.Min(), 0) + " a " + Format.Decimal(allDurations.Max(), 0) + " s, bem abaixo da meta de ~" + Format.Decimal(config.Arena.Combat.TargetBattleDurationSeconds, 0) + " s.");
        }
    }

    private static long Strength(GameConfig config, IEnumerable<Fighter> team)
    {
        return CardumeRules.Display(config, team.Where(f => f != null).Sum(f => CardumeRules.RawStrength(config, f.Stats)));
    }

    // ------------------------------------------------------------------ expeditions

    private static void WriteExpeditions(StringBuilder r, GameConfig config, ProgressionResult p)
    {
        r.AppendLine("## 5. Expedições");
        r.AppendLine();
        r.AppendLine("Força de referência: Cardumes dos adversários simulados em algumas posições da Arena.");
        r.AppendLine();
        var count = config.ArenaBots.BotCount;
        var marks = new[] { count, count * 3 / 4, count / 2, count / 4, 1 }.Where(x => x >= 1).Distinct().ToList();
        double bestExpeditionPerHour = 0;
        r.AppendLine("| Expedição | Duração | Força recomendada | Moedas na recomendada | Moedas por hora | " + string.Join(" | ", marks.Select(m => "Moedas c/ Cardume do #" + m)) + " |");
        r.AppendLine("|---|---:|---:|---:|---:|" + string.Concat(Enumerable.Repeat("---:|", marks.Count)));
        foreach (var e in config.Expeditions.Expeditions)
        {
            var atRecommended = ExpeditionRules.Coins(e, ExpeditionRules.Efficiency(config, e.RecommendedStrength, e.RecommendedStrength));
            var perHour = atRecommended / (e.DurationMinutes / 60.0);
            bestExpeditionPerHour = Math.Max(bestExpeditionPerHour, perHour);
            var cells = marks.Select(m =>
            {
                var s = Strength(config, ArenaBots.Build(config, m - 1).Cardume);
                return Format.Number(ExpeditionRules.Coins(e, ExpeditionRules.Efficiency(config, s, e.RecommendedStrength)));
            });
            r.AppendLine("| " + e.DisplayName + " | " + Format.Duration(e.DurationMinutes * 60) + " | " + Format.Number((long)e.RecommendedStrength) + " | " + Format.Number(atRecommended) + " | "
                         + Format.Number((long)perHour) + " | " + string.Join(" | ", cells) + " |");
        }

        r.AppendLine();
        if (p.CoinsPerHourMap1 > 0)
        {
            Attention.Add("A Expedição que mais rende por hora, na Força recomendada, dá " + Format.Number((long)bestExpeditionPerHour) + " Moedas/h — " + Format.Percent(bestExpeditionPerHour / p.CoinsPerHourMap1, 0) + " do que a pesca rende no primeiro mapa (ela roda junto com a pesca).");
        }

        r.AppendLine("Para comparar: pescar rende " + Format.Number((long)p.CoinsPerHourMap1) + " Moedas por hora no primeiro mapa (e a Expedição roda junto com a pesca).");
        r.AppendLine();
    }

    // ------------------------------------------------------------------ market and auction

    private static void WriteMarket(StringBuilder r, GameConfig config)
    {
        var demand = config.MarketBots.Demand;
        r.AppendLine("## 6. Mercado e Leilão (jogadores simulados)");
        r.AppendLine();
        r.AppendLine("Tempo médio até um comprador simulado levar um anúncio, pelo preço em relação à referência (taxa de " + Format.Percent(config.Market.CompletedSaleFeeRatio, 0) + " na venda):");
        r.AppendLine();
        r.AppendLine("| Preço | Chance por verificação | Tempo médio até vender |");
        r.AppendLine("|---:|---:|---:|");
        foreach (var ratio in new[] { 0.5, 1.0, 1.5, 2.0, 3.0, 3.9 })
        {
            var chance = MarketRules.PurchaseChance(config, (long)(1000 * ratio), 1000);
            var minutes = chance > 0 ? demand.CheckIntervalMinutes / chance : double.PositiveInfinity;
            r.AppendLine("| " + Format.Decimal(ratio, 1) + "× | " + Format.Percent(chance, 0) + " | " + (double.IsInfinity(minutes) ? "nunca" : Format.Duration(minutes * 60)) + " |");
        }

        r.AppendLine();
        r.AppendLine("Leilões do jogador (30 leilões reais de 6 h, lance inicial = preço de venda ao NPC):");
        r.AppendLine();
        var ratios = new List<double>();
        var unsold = 0;
        for (var i = 0; i < 30; i++)
        {
            var (game, clock) = NewPlayer(config, 5000 + (ulong)i);
            game.Fishing.StartFishing();
            for (var s = 0; s < 40; s++)
            {
                clock.AdvanceSeconds(30);
                game.Fishing.Sync();
            }

            game.Fishing.StopFishing();
            var box = game.Fishing.GetFishingBox();
            if (box.Count == 0 || !game.Aquarium.KeepCatches(new[] { box[0].CatchId }).Succeeded)
            {
                continue;
            }

            var fish = game.Aquarium.GetAquarium(AquariumSort.Newest).Fish[0];
            var candidate = game.Market.GetSellCandidates().First(c => c.IsFish && c.SourceId == fish.FishId);
            game.Auctions.StartAuction(true, fish.FishId, Math.Max(1, fish.SalePriceCoins));
            clock.AdvanceSeconds(config.Auction.DurationHours * 3600 + 120);
            var news = game.Market.Update();
            var sold = news.FirstOrDefault(n => n.Kind == MarketEvent.KindAuctionSold);
            if (sold == null)
            {
                unsold++;
            }
            else
            {
                ratios.Add(sold.PriceCoins / (double)Math.Max(1, candidate.Goods.ReferenceCoins));
            }
        }

        r.AppendLine("- Terminaram sem lance: " + unsold + " de 30");
        if (ratios.Count > 0)
        {
            r.AppendLine("- Preço final médio: " + Format.Decimal(ratios.Average(), 2) + "× a referência (mín. " + Format.Decimal(ratios.Min(), 2) + "×, máx. " + Format.Decimal(ratios.Max(), 2) + "×)");
        }

        r.AppendLine();
    }

    // ------------------------------------------------------------------ catch success

    /// <summary>The same config with every bite pulled out: the game as it was before Catch Success.</summary>
    private static GameConfig CertainCatch(string root)
    {
        var dir = Path.Combine(root, "config");
        var texts = GameConfigLoader.RequiredFiles.ToDictionary(f => f, f => File.ReadAllText(Path.Combine(dir, f)));
        var progression = Newtonsoft.Json.Linq.JObject.Parse(texts[GameConfigLoader.ProgressionFile]);
        foreach (var tier in progression["rarity"]!["tiers"]!)
        {
            tier["catch_success_base"] = 1.0;
        }

        progression["fishing"]!["catch_success_max"] = 1.0;
        texts[GameConfigLoader.ProgressionFile] = progression.ToString();
        return GameConfigLoader.LoadFromTexts(texts).Config;
    }

    private static void WriteCatchSuccess(StringBuilder r, GameConfig config, ProgressionResult after, GameConfig beforeConfig)
    {
        var before = Progression(beforeConfig);
        r.AppendLine("## 7. Sucesso da Captura: quanto ela muda o jogo");
        r.AppendLine();
        r.AppendLine("A primeira coluna é o jogo de hoje se toda mordida virasse captura, com os mesmos números de `/config`;");
        r.AppendLine("a segunda é o jogo de verdade, com a Chance de Sucesso (docs/SISTEMA_SUCESSO_PESCA.md). Mesma estratégia");
        r.AppendLine("da seção 1. O intervalo da pesca não muda. A comparação com a versão de antes do rebalanceamento está");
        r.AppendLine("no CHANGELOG (0.2.0-m14.19).");
        r.AppendLine();
        r.AppendLine("| Medida | Se toda mordida virasse captura | Com a Chance de Sucesso |");
        r.AppendLine("|---|---:|---:|");
        string Hours(ProgressionResult p, int level) => p.Levels.TryGetValue(level, out var m) ? Format.Decimal(m.Hours, 1) + " h" : "—";
        foreach (var level in new[] { 5, 10, 15, 20, 30, 40 })
        {
            r.AppendLine("| Nível " + level + " | " + Hours(before, level) + " | " + Hours(after, level) + " |");
        }

        r.AppendLine("| Capturas por hora | " + Format.Decimal(before.CatchesPerHour, 0) + " | " + Format.Decimal(after.CatchesPerHour, 0) + " |");
        r.AppendLine("| Escapes por hora | 0 | " + Format.Decimal(after.EscapesPerHour, 0) + " |");
        r.AppendLine("| Moedas por hora, primeiro mapa | " + Format.Number((long)before.CoinsPerHourMap1) + " | " + Format.Number((long)after.CoinsPerHourMap1) + " |");
        r.AppendLine("| Moedas por hora, segundo mapa | " + Format.Number((long)before.CoinsPerHourMap2) + " | " + Format.Number((long)after.CoinsPerHourMap2) + " |");
        r.AppendLine("| Conchas por hora, segundo mapa | " + Format.Decimal(before.ShellsPerHourMap2, 1) + " | " + Format.Decimal(after.ShellsPerHourMap2, 1) + " |");
        r.AppendLine("| Primeiro peixe Raro | " + Format.Decimal(before.FirstRareAtHours, 1) + " h | " + Format.Decimal(after.FirstRareAtHours, 1) + " h |");
        r.AppendLine("| Vara 1 comprada | " + Format.Decimal(before.RodBoughtAtHours, 1) + " h | " + Format.Decimal(after.RodBoughtAtHours, 1) + " h |");
        r.AppendLine();
        if (before.Levels.TryGetValue(10, out var b10) && after.Levels.TryGetValue(10, out var a10))
        {
            Attention.Add("Sucesso da Captura: " + Format.Decimal(after.EscapesPerHour, 0) + " peixes escapam por hora. Se toda mordida virasse captura, o Nível 10 chegaria com " + Format.Decimal(b10.Hours, 1) + " h em vez de " + Format.Decimal(a10.Hours, 1) + " h.");
        }

        r.AppendLine("Combinações de equipamento, 10.000 tentativas cada (`CatchSimulator`, a mesma regra do jogo). A isca fica");
        r.AppendLine("sempre ligada; o custo dela por hora já está descontado em \"Moedas/h líquidas\".");
        r.AppendLine();
        var tiers = config.Progression.Rarity.Tiers;
        r.AppendLine("| Mapa · vara · barco · isca | Taxa real | " + string.Join(" | ", tiers.Select(t => t.DisplayName + " puxados (chance)")) + " | Capturas/h | Escapes/h | XP/h | Moedas/h | Moedas/h líquidas | Conchas/h |");
        r.AppendLine("|---|---:|" + string.Concat(Enumerable.Repeat("---:|", tiers.Count)) + "---:|---:|---:|---:|---:|---:|");
        var boats = config.Equipment.Boats.OrderBy(b => b.Tier).ToList();
        var baits = config.Equipment.Baits.OrderBy(b => b.Tier).ToList();
        var combos = new List<(MapConfig map, RodConfig rod, int level, BoatConfig boat, BaitConfig bait)>();
        foreach (var map in config.Maps.Maps.OrderBy(m => m.UnlockFisherLevel))
        {
            foreach (var rod in config.Rods.Rods.Where(x => x.Tier >= map.MinimumRodTier).OrderBy(x => x.Tier))
            {
                var levels = rod.HasInternalLevels ? new[] { 1, config.RodMaxLevel(rod) } : new[] { 1 };
                foreach (var level in levels)
                {
                    combos.Add((map, rod, level, boats[0], null));
                    if (level > 1 || !rod.HasInternalLevels)
                    {
                        combos.Add((map, rod, level, boats[Math.Min(1, boats.Count - 1)], baits.FirstOrDefault()));
                    }

                    if (level > 1)
                    {
                        combos.Add((map, rod, level, boats[boats.Count - 1], baits.LastOrDefault()));
                    }
                }
            }
        }

        foreach (var (map, rod, level, boat, bait) in combos)
        {
            var sim = CatchSimulator.Run(config, map, rod, level, boat, bait, 10_000, 42);
            string Cell(string rarity)
            {
                var row = sim.ByRarity.FirstOrDefault(x => x.RarityId == rarity);
                return row == null ? "—" : Format.Number(row.Caught) + " (" + Format.Percent(row.Chance, 0) + ")";
            }

            var label = map.DisplayName + " · " + rod.DisplayName + (rod.HasInternalLevels ? " Nv." + level : "") + " · " + boat.DisplayName + " · " + (bait?.DisplayName ?? "sem isca");
            r.AppendLine("| " + label + " | " + Format.Percent(sim.SuccessRate, 1) + " | " + string.Join(" | ", tiers.Select(t => Cell(t.Id))) + " | "
                         + Format.Decimal(sim.CatchesPerHour, 0) + " | " + Format.Decimal(sim.EscapesPerHour, 0) + " | " + Format.Number((long)sim.XpPerHour) + " | "
                         + Format.Number((long)sim.CoinsPerHour) + " | " + Format.Number((long)(sim.CoinsPerHour - sim.BaitCoinsPerHour)) + " | "
                         + Format.Decimal(sim.ShellsPerHour - sim.BaitShellsPerHour, 1) + " |");
        }

        r.AppendLine();
        r.AppendLine("Preço dos barcos em horas de pesca no segundo mapa (Moedas e Conchas por hora da seção 1):");
        r.AppendLine();
        r.AppendLine("| Barco | Bônus | Custo | Nível | Horas de pesca | Conchas |");
        r.AppendLine("|---|---:|---:|---:|---:|---:|");
        foreach (var boat in boats.Skip(1))
        {
            var hours = after.CoinsPerHourMap2 > 0 ? boat.CostCoins / after.CoinsPerHourMap2 : 0;
            var shellHours = after.ShellsPerHourMap2 > 0 ? boat.CostShells / after.ShellsPerHourMap2 : 0;
            r.AppendLine("| " + boat.DisplayName + " | +" + Format.Percent(boat.CatchSuccessBonus, 0) + " | " + Format.Number(boat.CostCoins) + " Moedas + " + Format.Number(boat.CostShells) + " Conchas | "
                         + boat.UnlockFisherLevel + " | " + Format.Decimal(hours, 1) + " h | " + (boat.CostShells > 0 ? Format.Decimal(shellHours, 1) + " h" : "—") + " |");
        }

        r.AppendLine();
    }

    // ------------------------------------------------------------------ helpers

    private static (LocalGame Game, ManualClock Clock) NewPlayer(GameConfig config, ulong seed)
    {
        var clock = new ManualClock(StartMs);
        var game = LocalGame.Start(config, new MemoryRepository(), clock, _ => { }).Game;
        game.Session.Save.RngSeed = seed;
        game.Tutorial.Skip();
        return (game, clock);
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "version.json")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? Directory.GetCurrentDirectory();
    }
}

/// <summary>A save that lives only in memory: the simulation writes thousands of times and needs no file.</summary>
internal sealed class MemoryRepository : IPlayerRepository
{
    private PlayerSave _save;

    public string Location => "(memória)";

    public bool Exists => _save != null;

    public SaveLoadResult Load() => new SaveLoadResult(_save == null ? SaveLoadStatus.NotFound : SaveLoadStatus.Loaded, _save, "memory");

    public void Save(PlayerSave save) => _save = save;

    public string Reset()
    {
        _save = null;
        return null;
    }
}

