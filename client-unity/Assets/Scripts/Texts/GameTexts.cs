namespace FishingIdle.Texts
{
    /// <summary>
    /// Every sentence a person reads in the game or in the Dev Panel, in PT-BR, in one place.
    /// </summary>
    /// <remarks>
    /// Project rule (CLAUDE.md, section 1): no player- or owner-facing text is written inline in
    /// code. Add new text here and reference it. Technical keys (status ids, species ids) stay in
    /// English in data and are translated by the helpers below at display time. Names of species,
    /// maps, rods, rarities and size categories come from /config, which is already PT-BR.
    /// </remarks>
    public static class GameTexts
    {
        public const string GameTitle = "Fishing Idle";

        // ------------------------------------------------------------------ player

        public static class Player
        {
            public const string DefaultName = "Pescador";
            public const string Level = "Nível";
            public const string LevelShort = "Nv.";
            public const string MaxLevel = "Nível máximo";
            public const string Coins = "Moedas";
            public const string Shells = "Conchas";
            public const string Map = "Mapa";
            public const string Rod = "Vara";
            public const string TotalCatches = "Capturas";
            public const string SpeciesDiscovered = "Espécies descobertas";
            public const string CollapseCard = "Recolher";
            public const string ExpandCard = "Perfil";

            public static string Xp(string current, string needed) => "XP " + current + " / " + needed;
        }

        // ------------------------------------------------------------------ navigation (GDD section 7)

        public static class Navigation
        {
            // Only menus that already exist in the game are listed; the rest arrive with their milestones.
            public const string Fishing = "Pesca";
            public const string Map = "Mapa";
            public const string Aquarium = "Aquário";
            public const string Shop = "Loja";
            public const string Profile = "Perfil";
        }

        // ------------------------------------------------------------------ fishing HUD

        public static class Fishing
        {
            public const string Start = "Iniciar pesca";
            public const string Stop = "Parar pesca";
            public const string Idle = "Pesca parada. Aperte \"Iniciar pesca\" para começar.";
            public const string Casting = "Arremessando…";
            public const string Waiting = "Esperando o peixe morder…";
            public const string Bite = "Mordeu!";
            public const string Reeling = "Puxando a linha…";
            public const string Caught = "Pegou!";

            public static string NextCatchIn(string countdown) => "Próxima captura em " + countdown;
            public static string CycleInfo(string duration) => "1 captura a cada " + duration;
        }

        // ------------------------------------------------------------------ Fishing Box

        public static class Box
        {
            public const string Title = "Caixa de Pesca";
            public const string Open = "Caixa de Pesca";
            public const string Close = "Fechar";
            public const string Empty = "A Caixa de Pesca está vazia. Os peixes que você pescar aparecem aqui.";
            public const string EmptyFilter = "Nenhum peixe neste filtro.";
            public const string SelectAll = "Selecionar todos";
            public const string ClearSelection = "Limpar seleção";
            public const string SellSelected = "Vender selecionados";
            public const string NothingSelected = "Clique nos peixes para selecionar.";
            public const string NewSpeciesBadge = "NOVA ESPÉCIE";
            public const string RecordBadge = "RECORDE";
            public const string Value = "Valor";

            // Filters (GDD section 11: one primary filter at a time).
            public const string FilterAll = "Todos";

            public static string Count(int count) => count == 1 ? "1 peixe" : count + " peixes";
            public static string Selected(int count, string coins) => (count == 1 ? "1 selecionado" : count + " selecionados") + " · " + coins + " moedas";
            public static string Sold(int count, string coins) => (count == 1 ? "1 peixe vendido" : count + " peixes vendidos") + " por " + coins + " moedas.";
        }

        // ------------------------------------------------------------------ Aquarium (GDD sections 12, 13, 22)

        public static class Aquarium
        {
            public const string Title = "Aquário";
            public const string Empty = "O Aquário está vazio. Na Caixa de Pesca, selecione peixes e clique em \"Guardar no Aquário\".";
            public const string KeepSelected = "Guardar no Aquário";
            public const string SortLabel = "Ordenar:";
            public const string SortSize = "Tamanho";
            public const string SortLevel = "Nível";
            public const string SortSpecies = "Espécie";
            public const string SortNewest = "Mais recentes";
            public const string PickFish = "Clique num peixe para ver a ficha dele.";
            public const string Size = "Tamanho";
            public const string Level = "Nível";
            public const string Stats = "Atributos";
            public const string Hp = "Vida";
            public const string Attack = "Ataque";
            public const string Defense = "Defesa";
            public const string Speed = "Velocidade";
            public const string CaughtAt = "Pescado em";
            public const string SaleValue = "Valor de venda";
            public const string FeedValue = "Vale como alimento";
            public const string Feed = "Alimentar";
            public const string Sell = "Vender";
            public const string MaxLevelReached = "Nível máximo";
            public const string StatsNote = "Atributos vêm da espécie, da raridade, do tamanho e do nível. Dois peixes iguais nesses quatro pontos são sempre iguais.";

            public const string FeedTitle = "Alimentar";
            public const string FeedFromBox = "Da Caixa de Pesca";
            public const string FeedFromAquarium = "Do Aquário";
            public const string FeedPickHint = "Escolha os peixes que serão consumidos. Eles deixam de existir.";
            public const string FeedNothingSelected = "Nenhum alimento selecionado.";
            public const string FeedConfirm = "Alimentar agora";
            public const string Back = "Voltar";
            public const string FeedValuableTitle = "Confirmar alimentação";
            public const string FeedValuableBody = "Estes peixes valiosos serão consumidos:";
            public const string FeedCardumeBody = "Estes peixes estão no Cardume e sairão dele:";

            public static string Count(int count, int capacity) => count + " / " + capacity + " peixes";
            public static string Slots(int count, int capacity) => "Aquário: " + count + " / " + capacity;
            public static string Kept(int kept, int count, int capacity) => (kept == 1 ? "1 peixe guardado" : kept + " peixes guardados") + " no Aquário (" + count + " / " + capacity + ").";
            public static string Fed(string xp, int level) => "Alimentado: +" + xp + " XP. Agora no Nível " + level + ".";
            public static string LevelOf(int level, int max) => "Nível " + level + " de " + max;
            public static string Xp(string current, string needed) => "XP " + current + " / " + needed;
            public static string FeedXp(string xp) => "+" + xp + " XP";
            public static string FeedSummary(int count, string xp) => (count == 1 ? "1 alimento" : count + " alimentos") + " · +" + xp + " XP";
            public static string FeedResult(int levelBefore, int levelAfter) => levelAfter > levelBefore ? "Nível " + levelBefore + " → Nível " + levelAfter : "Continua no Nível " + levelBefore;
            public static string Wasted(string xp) => xp + " XP passam do nível máximo e serão perdidos.";
            public static string SellTitle(string species) => "Vender " + species + "?";
            public static string SellBody(string coins) => "Você recebe " + coins + " moedas. O XP investido neste peixe não volta. Esta ação não pode ser desfeita.";
        }

        // ------------------------------------------------------------------ Map and travel (GDD section 18)

        public static class Map
        {
            public const string Title = "Mapa";
            public const string Travel = "Viajar";
            public const string YouAreHere = "Você está aqui";
            public const string TravelNote = "A viagem é sempre manual. Durante a viagem a pesca fica pausada e volta sozinha na chegada, se estava ligada.";
            public const string Species = "Espécies";
            public const string NeedsLevel = "Nível necessário";
            public const string NeedsRod = "Vara necessária";
            public const string AnyRod = "Qualquer vara";

            public static string Departing(string map) => "Partindo para " + map + ".";
            public static string Arrived(string map) => "Você chegou a " + map + ".";
            public static string Traveling(string map) => "Viajando para " + map + "…";
            public static string ArrivesIn(string countdown) => "Chegada em " + countdown;
            public static string TravelTime(string duration) => "Viagem: " + duration;
            public static string Discovered(int found, int total) => found + " de " + total + " descobertas";
            public static string LevelRequirement(int level) => "Nível " + level;
        }

        // ------------------------------------------------------------------ Shop (GDD sections 7, 19)

        public static class Shop
        {
            public const string Title = "Loja";
            public const string Rods = "Varas";
            public const string Buy = "Comprar";
            public const string Owned = "Já é sua";
            public const string AtLevel1 = "No nível 1";
            public const string AtMax = "No nível máximo";
            public const string Note = "Varas compradas ficam no Inventário (Perfil) e são equipadas na hora. Lá você também melhora, vende ou destrói varas.";
            public const string Upgrade = "Melhorar";
            public const string SellRod = "Vender";
            public const string DestroyRod = "Destruir";
            public const string MaxLevel = "Nível máximo";

            public static string Price(string coins) => coins + " moedas";
            public static string Requires(int level) => "Disponível no Nível " + level;
            public static string Bought(string rod) => rod + " comprada e equipada!";
            public static string Upgraded(string rod, int level) => rod + " agora está no Nível " + level + ".";
            public static string RodSold(string coins) => "Vara vendida por " + coins + " moedas.";
            public static string RodDestroyed(string rod) => rod + " foi destruída.";
            public static string UpgradeFor(int level, string coins) => "Melhorar p/ Nv. " + level + " (" + coins + ")";
            public static string SellFor(string coins) => "Vender (" + coins + ")";
            public static string MaxLevelOf(int max) => "Até o Nível " + max;
            public static string SellTitle(string rod) => "Vender " + rod + "?";
            public static string SellBody(string coins) => "O jogo paga " + coins + " moedas por ela. A vara sai do seu Inventário.";
            public static string DestroyTitle(string rod) => "Destruir " + rod + "?";
            public const string DestroyBody = "A vara some do Inventário e você não recebe nada. Esta ação não pode ser desfeita.";
        }

        // ------------------------------------------------------------------ Cardume (GDD sections 23, 26, 31)

        public static class Cardume
        {
            public const string Title = "Cardume";
            public const string Front = "Frente";
            public const string Back = "Trás";
            public const string Empty = "Vazio";
            public const string Strength = "Força do Cardume";
            public const string StrengthPrivate = "Só você vê a Força do Cardume. Ela não decide batalhas; serve para Expedições e para você comparar formações.";
            public const string PickSlot = "Clique numa posição e depois num peixe do Aquário para colocá-lo ali.";
            public const string Remove = "Tirar da posição";
            public const string AquariumList = "Peixes do Aquário";
            public const string NoFish = "Guarde peixes no Aquário para montar o Cardume.";
            public const string OrderNote = "Ordem de ataque dos adversários: 1 → 2 → 3 → 4 → 5 → 6. Peixes resistentes na frente protegem os de trás.";

            public static string Position(int position) => "Posição " + position;
            public static string InPosition(string species, int position) => species + " (Cardume, posição " + position + ")";
            public static string Filled(int filled, int size) => filled + " / " + size + " peixes";
            public static string BonusActive(string percent) => "Cardume completo: +" + percent + " em todos os atributos.";
            public static string BonusMissing(int missing, string percent) => "Faltam " + missing + " para o Cardume completo (+" + percent + " em todos os atributos).";
            public static string Badge(int position) => "C" + position;
        }

        // ------------------------------------------------------------------ Profile (GDD sections 20, 37, 38)

        public static class Profile
        {
            public const string Title = "Perfil";
            public const string TabEquipment = "Equipamentos";
            public const string TabInventory = "Inventário";
            public const string TabCardume = "Cardume";
            public const string TabEncyclopedia = "Enciclopédia";
            public const string TabRecords = "Destaques";
            public const string RodSlot = "Vara de pesca";
            public const string Equip = "Equipar";
            public const string Equipped = "Equipada";
            public const string NotAllowedHere = "Não serve neste mapa";
            public const string RarityBonus = "Chance de raridade";
            public const string SizeBonus = "Qualidade de tamanho";
            public const string ShellBonus = "Conchas";
            public const string CatchesRare = "Pesca peixes Raros";
            public const string NoRare = "Não pesca peixes Raros";
            public const string NoShells = "Não gera Conchas";
            public const string InventoryNote = "Suas varas ficam aqui. Novas varas chegam com a Loja (Milestone 4).";
            public const string Undiscovered = "???";
            public const string Largest = "Maior";
            public const string TimesCaught = "Pescados";
            public const string FirstCaught = "Descoberto em";
            public const string TotalCatches = "Capturas";
            public const string Discovered = "Espécies descobertas";
            public const string Biggest = "Maior peixe já pescado";
            public const string HighestLevel = "Peixe de nível mais alto";
            public const string Exceptional = "Capturas Excepcionais";
            public const string Rare = "Capturas Raras";
            public const string Sold = "Peixes vendidos";
            public const string CoinsFromSales = "Moedas com vendas";
            public const string None = "—";

            public static string Tier(int tier) => tier == 0 ? "Vara inicial" : "Tier " + tier;
            public static string Discovery(int found, int total) => found + " de " + total + " espécies";
        }

        // ------------------------------------------------------------------ confirmation (GDD section 11)

        public static class Dialogs
        {
            public const string ValuableTitle = "Peixes valiosos na venda";
            public const string Review = "Revisar peixes";
            public const string ConfirmButton = "Confirmar";
            public const string Cancel = "Cancelar";

            public static string ValuableBody(int count) =>
                (count == 1 ? "1 peixe valioso" : count + " peixes valiosos") +
                " (raros ou de tamanho Excepcional) estão nesta venda. Deseja vender mesmo assim?";

            public const string ResetTitle = "Apagar o save?";
            public const string ResetBody = "Isso começa um jogo novo do zero. Uma cópia do save atual é guardada na pasta do save, então nada se perde de verdade.";
            public const string ResetConfirm = "Apagar e começar de novo";
        }

        // ------------------------------------------------------------------ toasts

        public static class Toasts
        {
            public static string Catch(string species, string size, string category) => species + " · " + size + " (" + category + ")";
            public static string NewSpecies(string species) => "Nova espécie descoberta: " + species + "!";
            public static string PersonalRecord(string species, string size) => "Novo recorde de " + species + ": " + size + "!";
            public static string Exceptional(string species) => "Captura Excepcional: " + species + "!";
            public static string LevelUp(int level) => "Você subiu para o Nível " + level + "!";
            public static string Shells(string amount) => "+" + amount + " Conchas";
            public const string FishingStarted = "Pesca iniciada.";
            public const string FishingStopped = "Pesca parada.";
            public const string ConfigReloaded = "Balanceamento recarregado.";
            public const string SaveReset = "Save apagado. Um jogo novo começou.";
        }

        // ------------------------------------------------------------------ startup / fatal screens

        public static class Startup
        {
            public const string ConfigErrorTitle = "O jogo não pôde começar: o balanceamento tem problemas";
            public const string ConfigErrorHint = "Corrija os itens abaixo nos arquivos da pasta /config (ou pelo Painel de Desenvolvimento) e aperte Play de novo.";
            public const string UnexpectedErrorTitle = "O jogo encontrou um erro inesperado";
            public const string UnexpectedErrorHint = "Os detalhes técnicos estão no Console do Unity.";
            public const string SaveTooNew = "Este save foi criado por uma versão mais nova do jogo. Você está jogando com um save temporário, que não será gravado, para não estragar o original.";
            public const string SaveRecovered = "O save principal estava danificado e foi restaurado a partir do backup.";
            public const string SaveUnrecoverable = "O save estava danificado e não pôde ser recuperado. Um jogo novo começou; os arquivos danificados foram guardados na pasta do save.";
        }

        // ------------------------------------------------------------------ service errors

        /// <summary>Translates a ServiceError key into a sentence the player understands.</summary>
        public static string ServiceErrorMessage(string errorKey)
        {
            switch (errorKey)
            {
                case "AlreadyFishing": return "Você já está pescando.";
                case "NotFishing": return "A pesca já está parada.";
                case "EmptySelection": return "Selecione pelo menos um peixe.";
                case "CatchNotFound": return "Um dos peixes selecionados não está mais na Caixa de Pesca.";
                case "DuplicateCatchInRequest": return "O mesmo peixe foi selecionado duas vezes.";
                case "AquariumFull": return "O Aquário não tem vagas suficientes. Venda ou use peixes como alimento para liberar espaço.";
                case "FishNotFound": return "Um dos peixes selecionados não está mais no Aquário.";
                case "CannotFeedItself": return "Um peixe não pode ser alimento dele mesmo.";
                case "FishAtMaxLevel": return "Este peixe já está no nível máximo.";
                case "InvalidCardumePosition": return "Essa posição do Cardume não existe.";
                case "ItemNotFound": return "Esse item não está mais no seu Inventário.";
                case "RodNotAllowedOnMap": return "Essa vara não pode ser usada neste mapa.";
                case "Traveling": return "Você está viajando. Espere chegar ao destino.";
                case "AlreadyOnMap": return "Você já está neste mapa.";
                case "MapNotFound": return "Esse mapa não existe.";
                case "MapLocked": return "Você ainda não tem nível para este mapa.";
                case "RodTooWeakForMap": return "Sua vara não serve para este mapa. Compre uma vara melhor na Loja.";
                case "NotEnoughCoins": return "Moedas insuficientes.";
                case "RodNotForSale": return "Essa vara não está à venda.";
                case "RodLocked": return "Você ainda não tem nível para comprar esta vara.";
                case "RodAlreadyOwned": return "Você já tem esta vara no Inventário.";
                case "RodAtMaxLevel": return "Esta vara já está no nível máximo.";
                case "RodHasNoLevels": return "Esta vara não tem níveis para melhorar.";
                case "RodEquipped": return "Equipe outra vara antes de vender ou destruir esta.";
                case "RodNotSellable": return "A Vara Inicial não pode ser vendida nem destruída.";
                case "SpeciesMissingFromConfig": return "Um dos peixes é de uma espécie que não existe mais no balanceamento, então não pode ser vendido agora.";
                default: return "Não foi possível fazer isso agora.";
            }
        }

        // ------------------------------------------------------------------ save

        public static class Save
        {
            public static string Status(string statusKey)
            {
                switch (statusKey)
                {
                    case "NotFound": return "Nenhum save encontrado — um jogo novo foi criado.";
                    case "Loaded": return "Save carregado normalmente.";
                    case "RecoveredFromBackup": return "Save principal danificado; restaurado a partir do backup.";
                    case "Unrecoverable": return "Save danificado e sem backup válido; um jogo novo foi criado.";
                    case "TooNew": return "Save de uma versão mais nova do jogo; não será sobrescrito.";
                    default: return statusKey;
                }
            }
        }

        // ------------------------------------------------------------------ config validation

        /// <summary>Messages for invalid balance files. Each one names the file and the entry to fix.</summary>
        public static class Validation
        {
            public static string ConfigFolderMissing(string path) => "A pasta de balanceamento não foi encontrada: " + path;
            public static string ConfigFileMissing(string file) => "O arquivo " + file + " não foi encontrado.";
            public static string ConfigFileUnreadable(string file, string detail) => "O arquivo " + file + " não pôde ser lido: " + detail;
            public static string ConfigFileEmpty(string file) => "O arquivo " + file + " está vazio.";
            public static string ConfigFileInvalidJson(string file, string detail) => "O arquivo " + file + " não é um JSON válido (" + detail + ").";
            public static string Missing(string file, string path) => file + ": o item \"" + path + "\" está faltando.";
            public static string DuplicateOrEmptyId(string file, string section, string id) => file + ": em \"" + section + "\" existe um id vazio ou repetido (\"" + (id ?? "") + "\").";
            public static string NegativeValue(string file, string what) => file + ": \"" + what + "\" não pode ter valor negativo.";
            public static string AtLeast(string file, string what, int min) => file + ": \"" + what + "\" precisa ser pelo menos " + min + ".";
            public static string BadPercentileBand(string category) => "progression.json: a categoria de tamanho \"" + category + "\" precisa de percentis entre 0 e 1, com o mínimo menor que o máximo.";
            public static string WeightsSumToZero(string file, string what) => file + ": os pesos de \"" + what + "\" somam zero — nada poderia ser sorteado.";
            public static string RodUpgradeCostMissing(string rod, int level) => "rods.json: a vara \"" + rod + "\" não tem custo de melhoria para o nível " + level + " em upgrade_costs.";
            public static string FishXpTableGap(int level) => "progression.json: a tabela de XP do peixe (fish_level.xp_table) não tem o nível " + level + ".";
            public static string BadBaseStats(string species) => "fish_catalog.json: a espécie \"" + species + "\" precisa de base_stats com Vida e Velocidade maiores que zero e Ataque e Defesa não negativos.";
            public static string XpTableGap(int level) => "progression.json: a tabela de XP do Pescador não tem o nível " + level + ".";
            public static string UnknownRarity(string species, string rarity) => "fish_catalog.json: a espécie \"" + species + "\" usa a raridade \"" + (rarity ?? "") + "\", que não existe em progression.json.";
            public static string BadSizeRange(string species) => "fish_catalog.json: a espécie \"" + species + "\" precisa de size_cm com mínimo maior que zero e máximo maior que o mínimo.";
            public static string PoolUnknownSpecies(string map, string species) => "maps.json: o mapa \"" + map + "\" lista a espécie \"" + (species ?? "") + "\", que não existe em fish_catalog.json.";
            public static string PoolRarityNotAvailable(string map, string species, string rarity) => "maps.json: o mapa \"" + map + "\" tem \"" + species + "\" (raridade \"" + rarity + "\"), mas essa raridade não está em available_rarities do mapa.";
            public static string ChanceOutOfRange(string file, string what) => file + ": \"" + what + "\" é uma chance e precisa estar entre 0 e 1.";
            public static string BadIntRange(string file, string what) => file + ": \"" + what + "\" precisa ter mínimo ≥ 0 e máximo ≥ mínimo.";
        }

        // ------------------------------------------------------------------ Dev Panel (Unity Editor window)

        public static class DevPanel
        {
            public const string MenuPath = "Fishing Idle/Painel de Desenvolvimento";
            public const string WindowTitle = "Painel Dev";

            public const string TabOverview = "Visão geral";
            public const string TabRoadmap = "Roadmap";
            public const string TabBalance = "Balanceamento";
            public const string TabSave = "Save";

            public const string Reload = "Recarregar";
            public const string CurrentMilestone = "Milestone atual";
            public const string GameVersion = "Versão do jogo";
            public const string ConfigVersion = "Versão do balanceamento";
            public const string RoadmapUpdatedAt = "Última atualização do roadmap";
            public const string Completion = "Tarefas concluídas";
            public const string Pending = "Tarefas pendentes";
            public const string NextStep = "Próximo passo";
            public const string Blockers = "Bloqueios e decisões suas";
            public const string NoBlockers = "Nada bloqueado e nenhuma decisão pendente.";
            public const string InProgress = "Em andamento agora";
            public const string NothingInProgress = "Nenhuma tarefa em andamento.";
            public const string RecentlyDone = "Concluídas recentemente";
            public const string RoadmapUnreadable = "Não foi possível ler docs/roadmap.json";
            public const string CompletionCriteria = "Critérios de conclusão";
            public const string Dependencies = "Depende de";
            public const string Notes = "Notas";
            public const string FilterAll = "Todos os status";

            public const string BalanceIntro = "Os valores abaixo vêm dos arquivos da pasta /config. Nada é gravado até você apertar \"Salvar\", e nada inválido é gravado: o painel valida tudo antes.";
            public const string Save = "Salvar";
            public const string SaveAndApply = "Salvar e aplicar no jogo";
            public const string Discard = "Descartar alterações";
            public const string Unsaved = "Há alterações não salvas.";
            public const string Saved = "Balanceamento salvo.";
            public const string SavedAndApplied = "Balanceamento salvo e aplicado no jogo em execução.";
            public const string InvalidNotSaved = "Nada foi salvo. Corrija estes problemas:";
            public const string SectionFishing = "Pesca";
            public const string SectionSpecies = "Espécies";
            public const string SectionMaps = "Mapas e chances";
            public const string SectionSizes = "Distribuição de tamanho";
            public const string SectionXp = "XP do Pescador";
            public const string SectionEconomy = "Economia";
            public const string SectionOthers = "Outros arquivos";
            public const string OthersNote = "Arena, Expedições, Mercado e Leilão ainda não existem no jogo. Os valores deles ficam editáveis aqui quando o milestone correspondente for implementado; até lá, estão em /config para consulta.";

            public const string OnlineCycle = "Tempo de pesca online (segundos por captura)";
            public const string OfflineCycle = "Tempo de pesca offline (segundos por captura)";
            public const string OfflineCap = "Limite de acúmulo offline (horas)";
            public const string OfflineNote = "A pesca offline chega no Milestone 5; os dois valores offline já ficam aqui para quando ela existir.";
            public const string SpeciesName = "Nome";
            public const string SpeciesRarity = "Raridade";
            public const string SpeciesSizeMin = "Tam. mín. (cm)";
            public const string SpeciesSizeMax = "Tam. máx. (cm)";
            public const string SpeciesSale = "Venda (moedas)";
            public const string SpeciesFisherXp = "XP Pescador";
            public const string SpeciesFeedXp = "XP alimento";
            public const string CatchWeight = "Peso";
            public const string CatchChance = "Chance";
            public const string SizeCategory = "Categoria";
            public const string SizeWeight = "Peso do sorteio";
            public const string SizePercentileMin = "Percentil mín.";
            public const string SizePercentileMax = "Percentil máx.";
            public const string SizeXpMultiplier = "Mult. XP";
            public const string SaleInfluence = "Influência do tamanho no preço";
            public const string XpLevel = "Nível";
            public const string XpToNext = "XP para o próximo";
            public const string ShellChance = "Chance de Concha por captura (0 a 1)";
            public const string MinimumPrice = "Preço mínimo de venda (moedas)";

            public const string SaveLocation = "Pasta do save";
            public const string OpenFolder = "Abrir pasta";
            public const string SaveMissing = "Ainda não existe save. Ele é criado na primeira vez que você aperta Play.";
            public const string ResetSave = "Apagar save (começar do zero)";
            public const string ResetDone = "Save apagado. Uma cópia foi guardada em:";
            public const string ResetNothing = "Não havia save para apagar.";
            public const string SaveSummary = "Resumo do save";
            public const string PlayingNote = "O jogo está rodando: o reset é aplicado na hora.";
            public const string SaveUnreadable = "O arquivo de save não pôde ser lido.";

            public const string ConfigInvalid = "inválido — veja os erros abaixo";
            public const string SaveVersionLabel = "Versão do save";
            public const string SaveUpdatedAt = "Gravado em";
            public const string SectionRods = "Varas";
            public const string StatHp = "Vida";
            public const string StatAttack = "Ataque";
            public const string StatDefense = "Defesa";
            public const string StatSpeed = "Velocidade";
            public const string StatsNote = "Atributos-base valem para nível 1 e tamanho mediano. Eles só entram em jogo com o Aquário e a Arena (Milestones 2 e 7), mas já podem ser ajustados aqui.";
            public const string TravelSeconds = "Duração da viagem entre mapas (segundos)";
            public const string UnlockLevel = "Nível do Pescador para liberar";
            public const string StatInfluence = "Influência do tamanho nos atributos";
            public const string FeedInfluence = "Influência do tamanho no XP de alimento";
            public const string XpTotalTo10 = "XP total do nível 1 ao 10";
            public const string FishXpTable = "XP do peixe por nível";
            public const string FisherXpTable = "XP do Pescador por nível";
            public const string AquariumCapacity = "Capacidade do Aquário";
            public const string FishLevelBonus = "Bônus de atributo por nível do peixe (%)";
            public const string FeedRecovery = "Recuperação do XP investido ao alimentar (0 a 1)";
            public const string RodPrice = "Preço de compra (moedas)";
            public const string RodRarityBonus = "Raridade";
            public const string RodSizeBonus = "Tamanho";
            public const string RodShellBonus = "Conchas";
            public const string RodUpgradeCost = "Custo p/ este nível";
            public const string RodsNote = "Bônus são o total naquele nível (0,10 = +10%). A compra e a melhoria de varas chegam no Milestone 4; a Vara Inicial já está em uso.";
            public const string ShellMin = "Conchas por drop (mínimo)";
            public const string ShellMax = "Conchas por drop (máximo)";
            public const string ExpeditionsTable = "Expedições";
            public const string ExpDuration = "Minutos";
            public const string ExpStrength = "Força rec.";
            public const string ExpCoins = "Moedas";
            public const string ExpFishChance = "Chance peixe";

            /// <summary>Balance values of systems that arrive in later milestones, with PT-BR labels.</summary>
            public static readonly BalanceFieldGroup[] OtherFields =
            {
                new BalanceFieldGroup("Arena — Energia", new[]
                {
                    new BalanceField("arena.json", "energy.max", "Energia máxima"),
                    new BalanceField("arena.json", "energy.regeneration_seconds_per_point", "Segundos para regenerar 1 Energia"),
                    new BalanceField("arena.json", "energy.cost_per_initiated_attack", "Energia gasta por ataque"),
                }),
                new BalanceFieldGroup("Arena — Adversários", new[]
                {
                    new BalanceField("arena.json", "opponent_selection.opponents_per_set", "Adversários oferecidos"),
                    new BalanceField("arena.json", "opponent_selection.rank_window_percent_above", "Janela acima do seu rank (%)"),
                    new BalanceField("arena.json", "opponent_selection.rerolls_per_set", "Trocas de adversários por ciclo"),
                }),
                new BalanceFieldGroup("Arena — Honra", new[]
                {
                    new BalanceField("arena.json", "honor.attacker_victory_gain", "Honra por vitória atacando"),
                    new BalanceField("arena.json", "honor.successful_defense_gain", "Honra por defesa bem-sucedida"),
                    new BalanceField("arena.json", "honor.defeat_loss", "Honra perdida na derrota"),
                }),
                new BalanceFieldGroup("Arena — Combate", new[]
                {
                    new BalanceField("arena.json", "combat.damage_roll.min", "Variação de dano (mínimo)"),
                    new BalanceField("arena.json", "combat.damage_roll.max", "Variação de dano (máximo)"),
                    new BalanceField("arena.json", "combat.defense_mitigation.constant", "Constante da defesa"),
                    new BalanceField("arena.json", "combat.defense_mitigation.minimum_damage_ratio_of_attack", "Dano mínimo (fração do ataque)"),
                    new BalanceField("arena.json", "combat.speed.base_interval_seconds", "Intervalo base entre ataques (s)"),
                    new BalanceField("arena.json", "combat.speed.reference_speed", "Velocidade de referência"),
                    new BalanceField("arena.json", "combat.target_battle_duration_seconds", "Duração-alvo da batalha (s)"),
                }),
                new BalanceFieldGroup("Cardume", new[]
                {
                    new BalanceField("arena.json", "cardume.complete_bonus.hp_percent", "Bônus 6/6 em Vida (%)"),
                    new BalanceField("arena.json", "cardume.complete_bonus.attack_percent", "Bônus 6/6 em Ataque (%)"),
                    new BalanceField("arena.json", "cardume.complete_bonus.defense_percent", "Bônus 6/6 em Defesa (%)"),
                    new BalanceField("arena.json", "cardume.complete_bonus.speed_percent", "Bônus 6/6 em Velocidade (%)"),
                }),
                new BalanceFieldGroup("Expedições — Eficiência", new[]
                {
                    new BalanceField("expeditions.json", "efficiency.below_recommended.exponent", "Abaixo da recomendada: expoente"),
                    new BalanceField("expeditions.json", "efficiency.below_recommended.floor", "Abaixo da recomendada: eficiência mínima"),
                    new BalanceField("expeditions.json", "efficiency.above_recommended.slope", "Acima da recomendada: inclinação do bônus"),
                    new BalanceField("expeditions.json", "efficiency.above_recommended.cap", "Acima da recomendada: bônus máximo"),
                }),
            };

            /// <summary>Market and Auction values (economy.json).</summary>
            public static readonly BalanceFieldGroup[] EconomyFields =
            {
                new BalanceFieldGroup("Mercado", new[]
                {
                    new BalanceField("economy.json", "market_fixed_price.max_active_listings_per_player", "Anúncios ativos por jogador"),
                    new BalanceField("economy.json", "market_fixed_price.max_listing_duration_days", "Duração máxima do anúncio (dias)"),
                    new BalanceField("economy.json", "market_fixed_price.completed_sale_fee_ratio", "Taxa sobre a venda (0,03 = 3%)"),
                }),
                new BalanceFieldGroup("Leilão", new[]
                {
                    new BalanceField("economy.json", "auction.duration_hours", "Duração do leilão (horas)"),
                    new BalanceField("economy.json", "auction.min_bid_increment_ratio", "Aumento mínimo do lance (0,03 = 3%)"),
                    new BalanceField("economy.json", "auction.bid_fee_ratio", "Taxa por lance (0,01 = 1%)"),
                    new BalanceField("economy.json", "auction.anti_snipe_window_seconds", "Janela anti-lance-de-última-hora (s)"),
                    new BalanceField("economy.json", "auction.anti_snipe_reset_to_seconds", "Cronômetro volta para (s)"),
                    new BalanceField("economy.json", "auction.seller_early_close_fee_ratio", "Taxa de encerramento antecipado (0,03 = 3%)"),
                }),
            };

            public static string CompletionValue(int done, int total, string percent) => done + " de " + total + " (" + percent + ")";
            public static string MilestoneProgress(int done, int total) => done + "/" + total;
        }

        /// <summary>One labelled balance value: which file, which JSON path, what the owner reads.</summary>
        public sealed class BalanceField
        {
            public BalanceField(string file, string path, string label)
            {
                File = file;
                Path = path;
                Label = label;
            }

            public string File { get; }
            public string Path { get; }
            public string Label { get; }
        }

        public sealed class BalanceFieldGroup
        {
            public BalanceFieldGroup(string title, BalanceField[] fields)
            {
                Title = title;
                Fields = fields;
            }

            public string Title { get; }
            public BalanceField[] Fields { get; }
        }

        // ------------------------------------------------------------------ editor setup and build

        public static class ProjectSetup
        {
            public const string OpenSceneMenu = "Fishing Idle/Abrir cena principal";
            public const string OpenSaveFolderMenu = "Fishing Idle/Abrir pasta do save";
            public const string CompanyName = "FishingIdle";
            public const string SceneCreated = "Cena principal criada em";
            public const string PlayerSettingsApplied = "Configurações iniciais do projeto aplicadas (nome do produto, janela, rodar em segundo plano).";
            public const string BuildConfigInvalid = "O build foi cancelado: os arquivos de balanceamento em /config têm problemas.";
        }

        /// <summary>Roadmap status keys (stored in English) as the owner reads them.</summary>
        public static string RoadmapStatus(string statusKey)
        {
            switch (statusKey)
            {
                case "TODO": return "A FAZER";
                case "IN_PROGRESS": return "EM ANDAMENTO";
                case "DONE": return "CONCLUÍDO";
                case "BLOCKED": return "BLOQUEADO";
                case "NEEDS_OWNER_DECISION": return "PRECISA DE DECISÃO DO PROPRIETÁRIO";
                default: return statusKey;
            }
        }

        /// <summary>Roadmap subsystem keys as the owner reads them.</summary>
        public static string RoadmapSubsystem(string subsystemKey)
        {
            switch (subsystemKey)
            {
                case "repo": return "repositório";
                case "docs": return "documentação";
                case "config": return "balanceamento";
                case "ops": return "operação";
                case "server": return "regras do jogo";
                case "client": return "jogo (Unity)";
                case "web": return "web";
                default: return subsystemKey;
            }
        }
    }
}
