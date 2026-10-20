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
            public const string Dollars = "Dólares";
            public const string Map = "Mapa";
            public const string Rod = "Vara";
            public const string TotalCatches = "Capturas";
            public const string SpeciesDiscovered = "Espécies descobertas";
            public const string NoRod = "Nenhuma — pegue na Loja";
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
            public const string Arena = "Arena";
            public const string Ranking = "Ranking";
            public const string Expedition = "Expedição";
            public const string Shop = "Loja";
            public const string Market = "Mercado";
            public const string Profile = "Perfil";
            public const string Events = "Eventos";

            // The Clãs (M24-T49, A-174): a sub-tab of Perfil.
            public const string Clan = "Clã";

            // The top menu (M24-T43, A-169): four big tabs grouping the menus (the Tripulação tab left with the Crew,
            // M24-T46, A-172), and the round shortcuts on the right.
            public const string Adventure = "Aventura";
            public const string Trade = "Comércio";
            public const string PassShort = "Passe";
            public const string Missions = "Missões";
            public const string OpenProfile = "Abrir o Perfil";
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
            public const string Escaped = "Escapou…";

            // Catch Success (docs/SISTEMA_SUCESSO_PESCA.md): the owner's approved failure message.
            public const string EscapeMessage = "Você ainda não é bom o suficiente.";
            public const string EscapeHint = "Melhore sua vara, barco ou isca para aumentar suas chances.";
            public static string EscapeRare(string rarity) => "Um peixe " + rarity + " escapou! Melhore sua vara, barco ou isca para aumentar suas chances.";

            /// <summary>The one status line of the compact fishing panel (A-190): "Esperando o peixe morder… · 0:07".</summary>
            public static string StatusLine(string phase, string countdown) => phase + " · " + countdown;

            public const string IdleShort = "Pesca parada";
            public static string CycleInfo(string duration) => "1 tentativa a cada " + duration;
            public static string ChanceShort(string rarity, string percent) => rarity + " " + percent;
            public static string RareEscaped(string rarity, string percent) => "Um peixe " + rarity + " escapou! Sua chance de puxar era " + percent + ".";
            public static string BaitRanOut(string bait) => "A isca " + bait + " acabou. Compre mais na Loja → Iscas.";
            public static string BaitLeft(int charges) => charges == 1 ? "1 tentativa" : Format.Number(charges) + " tentativas";
        }

        // ------------------------------------------------------------------ Fishing Box

        public static class Box
        {
            public const string Title = "Caixa de Pesca";
            public const string Info = "Os peixes que você pesca esperam aqui até você vender ou guardar no Aquário. Na venda em lote, os peixes valiosos (raros, de tamanho especial ou de genes de elite) pedem uma confirmação antes.";
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

            // Filters: rarity and size are separate groups that combine (addendum A-079).
            public const string RarityLabel = "Raridade";
            public const string SizeLabel = "Tamanho";
            public const string AllRarities = "Todas";
            public const string AllSizes = "Todos";
            public const string SortLabel = "Ordenar:";
            public const string SortNewest = "Mais recentes";
            public const string SortPrice = "Mais caros";
            public const string SortValuable = "Mais valiosos";
            public const string SortLargest = "Maiores";
            public const string SortRarest = "Mais raros";
            public const string SortSpecies = "Por espécie";
            public const string SortNewestHint = "O último peixe pescado primeiro";
            public const string SortValuableHint = "Quem vale mais na venda primeiro";
            public const string SortLargestHint = "Pelo tamanho em relação à espécie";
            public const string SortRarestHint = "Mítico → Comum, e o maior de cada";
            public const string SortSpeciesHint = "Mesmos peixes juntos, de A a Z";
            public const string SortQuality = "Melhor qualidade";
            public const string SortQualityHint = "Genes mais altos primeiro";
            public static string SizeMenu(string size) => "Tamanho: " + size;
            public static string Showing(int count) => count == 1 ? "1 peixe neste filtro" : Format.Number(count) + " peixes neste filtro";

            public static string Count(int count) => count == 1 ? "1 peixe" : Format.Number(count) + " peixes";
            public static string CountOf(int count, int capacity) => Format.Number(count) + " de " + Format.Number(capacity) + " peixes";

            /// <summary>The Caixa button of the HUD (A-190): icon + "128/300".</summary>
            public static string Ratio(int count, int capacity) => Format.Number(count) + "/" + Format.Number(capacity);
            public const string Full = "Caixa cheia! A pesca está parada até você vender ou guardar peixes.";
            public const string AlmostFull = "Caixa quase cheia: quando encher, a pesca para até você vender.";
            public const string FullShort = "Caixa cheia: venda peixes";

            /// <summary>The Caixa button of the HUD when full (A-190; the name and the reason show on hover).</summary>
            public const string FullTiny = "Cheia";
            public static string LimitNote(int capacity) => "A Caixa guarda até " + Format.Number(capacity) + " peixes. Cheia, a pesca para até você vender ou guardar algum.";
            public static string SkippedFull(long attempts) => "A Caixa encheu: " + Format.Number(attempts) + (attempts == 1 ? " tentativa ficou" : " tentativas ficaram") + " sem pescar.";
            public static string Selected(int count, string coins) => (count == 1 ? "1 selecionado" : Format.Number(count) + " selecionados") + " · " + coins + " Moedas";
            public static string Sold(int count, string coins) => (count == 1 ? "1 peixe vendido" : Format.Number(count) + " peixes vendidos") + " por " + coins + " Moedas.";
        }

        // ------------------------------------------------------------------ Genes (M24-T52, addendum A-176)

        public static class Genes
        {
            public const string Title = "Genes";
            public const string Quality = "Qualidade";
            public const string PerfectGene = "Gene perfeito";
            public const string PerfectSpecimen = "EXEMPLAR PERFEITO";

            /// <summary>One gene against the maximum, e.g. "27/31".</summary>
            public static string Value(int gene, int max) => gene + "/" + max;

            /// <summary>The quality next to the stars, e.g. "Qualidade 72%".</summary>
            public static string QualityLine(string percent) => "Qualidade " + percent;
            public static string StarsName(int stars) => stars == 1 ? "1 estrela" : stars + " estrelas";

            // The quality filter of the Fishing Box and the Aquarium.
            public const string FilterAll = "Todas";
            public static string FilterMenu(string value) => "Qualidade: " + value;
            public static string FilterAtLeast(int stars, int top) => stars >= top ? StarsName(stars) : stars + " estrelas ou mais";
            public static string FilterChip(string value) => "Qualidade: " + value;

            /// <summary>The genes paragraph of the "Como funciona" notes (Caixa de Pesca, Aquário).</summary>
            public static string Info(int max, string down, string up, int topStars) =>
                "Genes: cada peixe nasce com 4 genes, um para cada atributo (Vida, Ataque, Defesa e Velocidade), de 0 a " + max +
                ". Cada gene muda o seu atributo de " + down + " a " + up + "; " + max + " é o gene perfeito, em dourado. As estrelas (1 a " + topStars +
                ") resumem a qualidade dos 4 genes: um peixe de " + topStars + " estrelas é raro, e caçar exemplares melhores é o que deixa o Cardume mais forte. Os genes nunca mudam e não mexem no preço de venda.";
        }

        // ------------------------------------------------------------------ Ranking (addendum A-100)

        public static class Ranking
        {
            public const string Title = "Ranking";
            public const string Subtitle = "Só jogadores reais, sem jogadores simulados";
            public const string TabLevel = "Nível";
            public const string TabCoins = "Moedas";
            public const string TabShells = "Conchas";
            public const string TabFish = "Peixes pescados";
            public const string You = "Você";
            public const string LocalNote = "Por enquanto o jogo roda só neste computador, então o ranking mostra só você. Quando o jogo for online, aqui aparecem todos os jogadores reais.";

            // Stage and scoreboard (addendum A-146).
            public const string Vacant = "Vago";
            public const string VacantMark = "—";
            public const string NotListed = "Você fora da lista";
            public const string YourPositionNotListed = "Sua posição: fora da lista";
            public static string YouAt(int position) => "Você #" + Format.Number(position);
            public static string YourPosition(int position) => "Sua posição #" + Format.Number(position);
            public static string PlaceShort(int position) => position + "º";
        }

        // ------------------------------------------------------------------ Search by fish name (addendum A-084)

        public static class Search
        {
            public const string Placeholder = "Buscar peixe pelo nome";
            public const string NoMatch = "Nenhum peixe com esse nome.";
        }

        // ------------------------------------------------------------------ Aquarium (GDD sections 12, 13, 22)

        public static class Aquarium
        {
            public const string Title = "Aquário";
            public const string Info = "Os peixes que você guardou ficam aqui: alimente-os para subir de nível e escolha os melhores para o Cardume. Atributos vêm da espécie, da raridade, do tamanho, do nível e dos genes.";
            public const string Empty = "O Aquário está vazio. Na Caixa de Pesca, selecione peixes e clique em \"Guardar no Aquário\".";
            public const string KeepSelected = "Guardar no Aquário";
            public const string SortLabel = "Ordenar:";
            public const string SortSize = "Tamanho";
            public const string SortLevel = "Nível";
            public const string SortSpecies = "Espécie";
            public const string SortNewest = "Mais recentes";
            public const string SortQuality = "Qualidade";
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
            public const string StatsNote = "Atributos vêm da espécie, da raridade, do tamanho, do nível e dos genes. Dois peixes iguais nesses cinco pontos são sempre iguais.";

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

            public static string Count(int count, int capacity) => Format.Number(count) + " / " + Format.Number(capacity) + " peixes";
            public static string Slots(int count, int capacity) => "Aquário: " + Format.Number(count) + " / " + Format.Number(capacity);
            public static string Kept(int kept, int count, int capacity) => (kept == 1 ? "1 peixe guardado" : Format.Number(kept) + " peixes guardados") + " no Aquário (" + Format.Number(count) + " / " + Format.Number(capacity) + ").";
            public static string Fed(string xp, int level) => "Alimentado: +" + xp + " XP. Agora no Nível " + level + ".";
            public static string LevelOf(int level, int max) => "Nível " + level + " de " + max;
            public static string Xp(string current, string needed) => "XP " + current + " / " + needed;
            public static string FeedXp(string xp) => "+" + xp + " XP";
            public static string FeedSummary(int count, string xp) => (count == 1 ? "1 alimento" : Format.Number(count) + " alimentos") + " · +" + xp + " XP";
            public static string FeedResult(int levelBefore, int levelAfter) => levelAfter > levelBefore ? "Nível " + levelBefore + " → Nível " + levelAfter : "Continua no Nível " + levelBefore;
            public static string Wasted(string xp) => xp + " XP passam do nível máximo e serão perdidos.";
            public static string SellTitle(string species) => "Vender " + species + "?";
            public static string SellBody(string coins) => "Você recebe " + coins + " Moedas. O XP investido neste peixe não volta. Esta ação não pode ser desfeita.";
            public static string LeavesCardume(int position) => "Este peixe está no Cardume (posição " + position + ") e sairá dele.";

            // Selling several fish at once (addendum A-130).
            public const string MultiSelect = "Selecionar vários";
            public const string MultiSelectTitle = "Venda em lote";
            public const string MultiSelectHint = "Clique nos peixes para marcar ou desmarcar. Ctrl + clique também começa uma seleção.";
            public const string MultiSelectNone = "Nenhum peixe selecionado.";
            public const string MultiSelectExit = "Sair da seleção";
            public const string SellManyProtected = "Valiosos ou no Cardume (sairão dele):";
            public static string SelectedCount(int count) => count == 1 ? "1 peixe selecionado" : count + " peixes selecionados";
            public static string SelectedTotal(string coins) => "Você recebe " + coins + " Moedas";
            public static string SellMany(int count) => "Vender " + count;
            public static string SellManyTitle(int count) => count == 1 ? "Vender 1 peixe?" : "Vender " + count + " peixes?";
            public static string SellManyBody(string coins) => "Você recebe " + coins + " Moedas. O XP investido nesses peixes não volta. Esta ação não pode ser desfeita.";
            public static string AndMore(int count) => "e mais " + count;

            // The living tank and its drawer (addendum A-144).
            public const string DrawerHide = "Recolher";
            public const string DrawerShow = "Mostrar todos";
            public static string DrawerTitle(int count) => "Todos os peixes (" + Format.Number(count) + ")";
            public static string SwimmingNote(int swimming, int total) => "Nadando no tanque: " + Format.Number(swimming) + " de " + Format.Number(total);

            // Card mode, the hero sheet (addendum A-145).
            public const string ViewTank = "Tanque";
            public const string ViewCards = "Cartas";
            public const string CardsStatsNote = "comparado aos seus peixes";
            public const string CardumeLabel = "Cardume";
            public const string CardumeOut = "Fora do Cardume";
            public static string CardumeSlot(int position) => "Posição " + position;
            public static string CardsPosition(int index, int total) => Format.Number(index) + " de " + Format.Number(total);
            public static string CardsLine(int level, string size) => Player.LevelShort + " " + level + " · " + size;
        }

        // ------------------------------------------------------------------ offline return (GDD section 10)

        public static class Offline
        {
            public const string Title = "Bem-vindo de volta!";
            public const string Continue = "Continuar";
            public const string OpenBox = "Abrir a Caixa de Pesca";
            public const string OpenBoxShort = "Abrir a Caixa";
            public static string Note(string cycle, string cap) => "Enquanto o jogo fica fechado, o pescador continua: 1 tentativa a cada " + cycle + ", por até " + cap + ", com a mesma chance de puxar. Os peixes esperam na Caixa de Pesca.";
            public static string NoteWithVip(string vipCycle, string cycle, string cap) => "Enquanto o jogo fica fechado, o pescador continua: 1 tentativa a cada " + vipCycle + " com o VIP (" + cycle + " sem ele), por até " + cap + ", com a mesma chance de puxar. Os peixes esperam na Caixa de Pesca.";
            public static string Hours(double hours) => hours == 1 ? "1 hora" : Format.Decimal(hours, 0) + " horas";

            // The report (M24-T51, A-175): the scene strip on top.
            public static string SceneAway(string duration) => "Você ficou fora por " + duration;
            public static string SceneCapped(string duration) => "Contaram as primeiras " + duration + ", o limite da pesca offline.";
            public const string SceneKept = "O pescador continuou na água enquanto você esteve fora.";
            public const string SceneNothing = "O pescador ficou na água, mas nenhum peixe veio desta vez.";

            // The big number cards.
            public const string StatFish = "Peixes pescados";
            public const string StatValue = "Valor da pesca";
            public const string StatValueSub = "se vender tudo";
            public const string StatXp = "XP de Pescador";
            public static string StatXpVip(string vipXp) => "+" + vipXp + " do VIP";
            public static string StatLevel(int from, int to) => "Nv. " + from + " → " + to + "!";
            public const string StatShells = "Conchas";

            // Rarities, highlights, fragments and warnings.
            public const string Rarities = "Peixes por raridade";
            public const string Best = "Destaques";
            public static string NewSpecies(int count) => count == 1 ? "1 espécie nova" : count + " espécies novas";
            public static string Records(int count) => count == 1 ? "1 recorde de tamanho" : count + " recordes de tamanho";
            public static string TopGenes(int count, int stars) => count == 1 ? "1 exemplar de " + stars + " estrelas" : count + " exemplares de " + stars + " estrelas";
            public const string Separator = " · ";
            public const string Equipment = "Fragmentos e equipamento";
            public static string Gained(string amount) => "+" + amount;
            public static string Progress(string have, string need) => have + "/" + need;
            public const string BoatFragments = "Fragmentos de barco";
            public const string RodFragments = "Fragmentos de vara";
            public const string ClothingFragmentsName = "Fragmentos de Roupa";
            public const string ClothingFragmentsKind = "Para montar uma roupa";
            public const string ClothingReady = "Já dá para montar uma roupa!";
            public const string ClothingAllAssembled = "Todas as roupas montadas";
            public static string Extras(string amount) => "Já é seu · " + amount + " de sobra";
            // Fragments completed while away (M24-T58, A-180): nothing assembles by itself, the player presses "Montar".
            public const string BoatReadyTag = "COMPLETO!";
            public const string RodReadyTag = "COMPLETA!";
            public const string BoatReadyKind = "Pronto para montar na Loja";
            public const string RodReadyKind = "Pronta para montar na Loja";
            public static string ClothingLevels(string pieces) => "Roupas subiram de nível: " + pieces;
            public static string PieceLevel(string piece, int level) => piece + " Nv." + level;
            public const string Warnings = "Avisos";
            public static string BoxFull(string count, string capacity) => "Caixa de Pesca cheia (" + count + "/" + capacity + "): a pesca para até você vender.";
            public static string BoxAlmostFull(string count, string capacity) => "Caixa de Pesca quase cheia (" + count + "/" + capacity + ").";
            public static string Escaped(int count) => count == 1 ? "1 peixe escapou" : Format.Number(count) + " peixes escaparam";
        }

        // ------------------------------------------------------------------ Arena (GDD sections 27–30)

        public static class Arena
        {
            public const string Title = "Arena";
            public const string TabOpponents = "Adversários";
            public const string TabRanking = "Ranking";
            public const string TabHistory = "Histórico";
            public const string TabShop = "Loja da Arena";
            public const string Rank = "Posição";
            public const string Energy = "Energia";
            public const string Honor = "Honra";
            public const string Attack = "Atacar";
            public const string Reroll = "Trocar adversários";
            public const string RerollUsed = "Troca já usada: ataque para receber novos adversários.";
            public const string Note = "Ataque um dos três adversários. Vencer troca a sua posição com a dele e dá Honra; perder custa um pouco de Honra. Cada ataque gasta 1 de Energia, vencendo ou perdendo. Outros jogadores também podem atacar você, inclusive com o jogo fechado.";
            public const string NoForceNote = "A Força dos adversários não é mostrada. Olhe o Cardume, os níveis e as raridades para decidir.";
            public const string EmptyHistory = "Nenhuma batalha ainda.";
            public const string ShopEmpty = "A Loja da Arena ainda não tem itens.";
            public const string ViewProfile = "Ver perfil";
            public const string Formation = "Cardume em formação";
            public static string PositionOf(int position) => "Posição " + position + (position <= 3 ? " · frente" : " · trás");
            public const string ShopNote = "Troque a Honra ganha nas batalhas por Conchas e Dólares. Alguns itens têm limite por semana.";
            public static string ShopReward(string amount, bool dollars) => "+" + amount + (dollars ? (amount == "1" ? " Dólar" : " Dólares") : (amount == "1" ? " Concha" : " Conchas"));
            public static string ShopPrice(string honor) => honor + " de Honra";
            public static string ShopWeekly(int bought, int limit) => "Esta semana: " + bought + " de " + limit;
            public static string ShopBought(string item) => "Compra feita: " + item + "!";
            public const string Attacked = "Ataque";
            public const string Defended = "Defesa";
            public const string Win = "Vitória";
            public const string Loss = "Derrota";
            public const string You = "Você";
            public const string Speed1 = "1x";
            public const string Speed2 = "2x";
            public const string Skip = "Pular";
            public const string BackToArena = "Voltar à Arena";
            public const string Victory = "Vitória!";
            public const string Defeat = "Derrota";

            public static string RankOf(int rank) => "#" + rank;
            public const string Previous = "Anterior";
            public const string Next = "Próxima";
            public const string MyPosition = "Minha posição";
            public const string RankingEmpty = "Ninguém no ranking ainda.";
            public static string PageOf(int page, int pages) => "Página " + page + " de " + pages;
            public static string Place(int rank) => rank + "º lugar";
            public static string PodiumLead(string species, int level) => species + " · Nv. " + level;
            public static string OutsideTop(int rank, int top) => "Sua posição: #" + rank + " (fora dos " + top + " primeiros)";
            public static string EnergyOf(int energy, int max) => energy + " / " + max;
            public static string NextEnergy(string countdown) => "+1 em " + countdown;
            public static string RerollsLeft(int left) => Reroll + " (" + left + ")";
            public static string RankChange(int before, int after) => before == after ? "Posição mantida: #" + after : "Posição: #" + before + " → #" + after;
            public static string HonorChange(long change) => (change >= 0 ? "+" : "−") + Format.Number(System.Math.Abs(change)) + " de Honra";
            public static string DefenseWon(string attacker, string honor) => attacker + " atacou você e perdeu. +" + honor + " de Honra.";
            public static string DefenseLost(string attacker, int rank) => attacker + " venceu você na Arena. Sua posição agora é #" + rank + ".";
            public static string DefenseLostHonor(string attacker, int rank, string honor) => attacker + " venceu você na Arena. Sua posição agora é #" + rank + ". −" + honor + " de Honra.";
            public static string Versus(string opponent) => "Você × " + opponent;
            public static string Clock(string time) => "Tempo: " + time;

            // Duel layout of the Adversários tab and the VS intro before the replay.
            public static string OfTotal(int total) => "de " + total;
            public static string AttackCost(int cost) => Attack + " · " + cost + " " + Energy;
            public const string VsMark = "VS";
            public const string IntroSkip = "Clique ou aperte Espaço para pular";
        }

        // ------------------------------------------------------------------ Tutorial (GDD section 40)

        public static class Tutorial
        {
            public const string Begin = "Começar";
            public const string GotIt = "Entendi";
            public const string Skip = "Pular tutorial";
            public const string Completed = "Tutorial concluído! Agora é com você: pesque, evolua seus peixes e explore os menus.";

            public static string StepOf(int step, int total) => "Tutorial · passo " + step + " de " + total;

            public static string Title(string step)
            {
                switch (step)
                {
                    case "welcome": return "Bem-vindo ao Fishing Idle!";
                    case "claim_rod": return "Pegue sua primeira vara";
                    case "start_fishing": return "Comece a pescar";
                    case "first_catch": return "Espere a primeira captura";
                    case "open_box": return "Abra a Caixa de Pesca";
                    case "sell_fish": return "Venda um peixe";
                    case "keep_fish": return "Guarde um peixe no Aquário";
                    case "cardume": return "Monte o seu Cardume";
                    case "expedition": return "Expedições";
                    default: return string.Empty;
                }
            }

            public static string Body(string step)
            {
                switch (step)
                {
                    case "welcome": return "Você é um pescador no Lago Sereno. O barco pesca sozinho, até com o jogo fechado. Vamos dar os primeiros passos — leva uns 2 minutos.";
                    case "claim_rod": return "Abra a aba " + Navigation.Trade + " (em cima) → " + Navigation.Shop + " e pegue o Caniço Manso, a vara inicial. Ela é grátis.";
                    case "start_fishing": return "Clique em \"" + Fishing.Start + "\", embaixo, no centro.";
                    case "first_catch": return "A cada ciclo o pescador tira um peixe da água. Quando ele aparecer, veja no aviso a espécie, o tamanho e a categoria.";
                    case "open_box": return "Todo peixe pescado vai para a " + Box.Open + " (canto de baixo, à direita). Abra para ver os peixes. Ela tem limite: quando enche, a pesca para até você vender.";
                    case "sell_fish": return "Na Caixa, selecione um peixe e clique em \"" + Box.SellSelected + "\". As Moedas melhoram sua vara, seu barco e compram iscas.";
                    case "keep_fish": return "Selecione outro peixe e clique em \"" + Aquarium.KeepSelected + "\". Peixes guardados sobem de nível e lutam por você.";
                    case "cardume": return "Abra a aba " + Navigation.Profile + " → " + Profile.TabCardume + " e coloque um peixe numa posição. O Cardume luta na Arena e vai em Expedições.";
                    case "expedition": return "Na aba " + Navigation.Adventure + " → " + Navigation.Expedition + ", você envia o Cardume por 30 min a 6 h para trazer Moedas e, às vezes, um peixe. Enquanto isso, ele fica ocupado.";
                    default: return string.Empty;
                }
            }
        }

        // ------------------------------------------------------------------ Notifications, settings and compact mode

        public static class Hud
        {
            /// <summary>The PC clock is behind the saved game time: the game waits until it catches up (TD-030, TD-052).</summary>
            public const string ClockFrozenTitle = "Relógio do computador atrasado";
            public static string ClockFrozenBody(string left) =>
                "O relógio do computador está atrás do horário salvo; o jogo fica parado até ele alcançar esse horário (faltam " + left + "). Confira a data e a hora do computador.";
            public const string Notifications = "Notificações";
            public const string NoNotifications = "Nada por aqui ainda.";
            public const string ClearNotifications = "Limpar";
            public const string Settings = "Configurações";
            public const string Sound = "Som";
            public const string Ambient = "Som ambiente";
            public const string Volume = "Volume";
            public const string On = "Ligado";
            public const string Off = "Desligado";
            public const string CompactMode = "Modo compacto";
            public const string CompactNote = "Uma janela pequena só com a água, o barco e o pescador, para deixar aberta enquanto trabalha. A pesca continua igual.";
            public const string Expand = "Expandir";
            public const string Fishing = "Pescando";
            public const string Stopped = "Parado";

            public static string BoxCount(int count) => "Caixa: " + count;
        }

        // ------------------------------------------------------------------ Market (GDD sections 33–34)

        public static class Market
        {
            public const string Title = "Mercado";
            public const string Note = "Compre e venda peixes e varas. Tudo o que sai do Mercado — compras, anúncios cancelados ou vencidos — espera em Itens a Retirar. No jogo local, os outros jogadores são simulados.\n\nCada peixe tem genes: as estrelas no cartão mostram a qualidade, e a ficha mostra o gene de cada atributo (de 0 a 31). Compare antes de comprar; o preço de venda ao jogo não muda com os genes.";
            public const string SortQuality = "Melhor qualidade";
            public const string TabBuy = "Comprar";
            public const string TabSell = "Vender";
            public const string TabMine = "Meus Anúncios";
            public const string TabWithdraw = "Itens a Retirar";
            public const string TabCurrency = "Conchas e Dólares";
            public const string CurrencySellTitle = "Anunciar Conchas ou Dólares";
            public const string CurrencyWhat = "O que vender";
            public const string CurrencyYouHave = "Você tem";
            public const string CurrencyAmount = "Quantidade";
            public const string CurrencyTotalPrice = "Preço total (Moedas)";
            public const string CurrencyListButton = "Anunciar";
            public const string CurrencyHoldNote = "Enquanto estiver anunciada, a quantidade sai da sua carteira. Cancelar devolve na hora.";
            public const string CurrencyOffersTitle = "Ofertas de outros jogadores";
            public const string CurrencyNoOffers = "Ninguém está vendendo ainda. Por enquanto o jogo roda só neste computador; quando ele for online, os anúncios dos outros jogadores aparecem aqui.";
            public const string CurrencyMineTitle = "Seus anúncios";
            public const string CurrencyNoListings = "Você não tem anúncios de Conchas ou Dólares.";
            public const string CurrencyCancel = "Cancelar";
            public const string CurrencyCancelled = "Anúncio cancelado. A quantidade voltou para a sua carteira.";
            public static string CurrencyPerUnit(string coins) => "Por unidade: " + coins + " Moedas";
            public static string CurrencyLine(string amount, string currency, string price) => amount + " " + currency + " por " + price + " Moedas";
            public static string CurrencyListed(string amount, string currency, string price) => "Anúncio criado: " + amount + " " + currency + " por " + price + " Moedas.";
            public static string CurrencyMineCount(int count, int max) => CurrencyMineTitle + " (" + count + "/" + max + ")";
            public const string Coins = "Suas Moedas";

            public const string FilterKind = "Tipo";
            public const string KindAll = "Tudo";
            public const string KindFish = "Peixes";
            public const string KindRods = "Varas";
            public const string FilterSpecies = "Espécie";
            public const string FilterRarity = "Raridade";
            public const string FilterSize = "Categoria de tamanho";
            public const string AnyFemale = "Todas";
            public const string AnyMale = "Todos";
            public const string SizeRange = "Tamanho (cm): mín. / máx.";
            public const string LevelRange = "Nível: mín. / máx.";
            public const string PriceRange = "Preço: mín. / máx.";
            public const string SortLabel = "Ordenar por";
            public const string SortPriceAsc = "Menor preço";
            public const string SortPriceDesc = "Maior preço";
            public const string SortSizeDesc = "Maior tamanho";
            public const string SortSizeAsc = "Menor tamanho";
            public const string SortNewest = "Mais recentes";
            public const string ClearFilters = "Limpar filtros";
            public const string NoResults = "Nenhum anúncio com esses filtros.";
            public const string SelectHint = "Clique num anúncio para ver os detalhes.";

            public const string Seller = "Vendedor";
            public const string EndsLabel = "Termina em";
            public const string Price = "Preço";
            public const string Buy = "Comprar";
            public const string BuyNote = "A compra vai para Itens a Retirar. Mesmo com o Aquário cheio você pode comprar; só a retirada espera uma vaga.";

            public const string SellHint = "Escolha um peixe do Aquário ou uma vara do Inventário para anunciar.";
            public const string NothingToSell = "Você não tem peixes no Aquário nem varas para anunciar.";
            public const string NpcValue = "O jogo paga";
            public const string Reference = "Referência do mercado simulado";
            public const string YourPrice = "Seu preço (Moedas)";
            public const string YouReceive = "Você recebe se vender";
            public const string ListingNote = "Sem taxa para anunciar. O preço é livre. Enquanto estiver anunciado, o item não pode ser usado.";

            public const string NoListings = "Você não tem anúncios ativos.";
            public const string Cancel = "Cancelar anúncio";
            public const string RecentSales = "Últimas vendas";
            public const string NoSales = "Nenhuma venda ainda.";

            public const string WithdrawNote = "Estes itens são seus: não ocupam vaga, não vencem e não podem ser usados até você retirar.";
            public const string Withdraw = "Retirar";
            public const string WithdrawAll = "Retirar tudo";
            public const string NothingToWithdraw = "Nada para retirar.";
            public const string Rod = "Vara";

            public const string TabAuction = "Leilão";
            /// <summary>How long an auction lasts, for the notes ("1 hora", "6 horas").</summary>
            public static string AuctionDuration(double hours) => hours == 1 ? "1 hora" : Format.Decimal(hours, 0) + " horas";
            public static string AuctionNote(string duration, string increment, string fee, string reset) => "Leilões duram " + duration + ". Cada lance precisa ser pelo menos " + increment + " maior que o atual e custa " + fee + " dele em taxa, que não volta. O valor do lance fica reservado enquanto você estiver ganhando; se alguém passar, ele volta na hora. Lance nos últimos instantes faz o tempo voltar para " + reset + ".";
            public const string MyAuction = "Meu leilão";
            public const string NoMyAuction = "Você não tem leilão ativo.";
            public const string CreateAuction = "Criar leilão";
            public const string BackToAuctions = "Voltar aos leilões";
            public const string StartingBid = "Lance inicial (Moedas)";
            public static string StartAuctionNote(string duration) => "Depois de criado, o leilão não pode ser cancelado. Sem lances, ele dura " + duration + " e o item volta por Itens a Retirar.";
            public const string CurrentBid = "Lance atual";
            public const string NoBids = "Sem lances";
            public const string YourBid = "Seu lance (Moedas)";
            public const string PlaceBid = "Dar lance";
            public const string YouAreWinning = "Você está ganhando este leilão.";
            public const string Winning = "GANHANDO";
            public const string NoAuctions = "Nenhum leilão aberto agora.";

            public static string StartAuctionFor(double hours) => "Leiloar por " + Format.Decimal(hours, 0) + " horas";
            public static string BidCount(int count) => count == 1 ? "1 lance" : count + " lances";
            public static string MinBid(string coins) => "Lance mínimo: " + coins;
            public static string BidFee(double ratio, string fee) => "Taxa do lance (" + Format.Percent(ratio, 0) + "): " + fee;
            public static string Reserved(string coins) => "Reservado em lances: " + coins;
            public static string StartingAt(string coins) => "Inicial " + coins;
            public static string BidAt(string coins) => "Lance " + coins;
            public static string EndNow(string net) => "Encerrar agora (receber " + net + ")";
            public static string AuctionStarted(string name) => "Leilão de " + name + " criado. Ele termina em 6 horas.";
            public static string BidPlaced(string name, string bid, string fee) => "Lance de " + bid + " em " + name + ". Taxa paga: " + fee + ".";
            public static string Outbid(string name, string bidder) => bidder + " passou o seu lance em " + name + ". O valor do seu lance voltou.";
            public static string AuctionWon(string name) => "Você ganhou o leilão de " + name + ". Ele está em Itens a Retirar.";
            public static string AuctionLost(string name) => "O leilão de " + name + " terminou com outro vencedor.";
            public static string AuctionSold(string name, string net) => "Seu leilão de " + name + " terminou. +" + net + " Moedas.";
            public static string AuctionUnsold(string name) => "Seu leilão de " + name + " terminou sem lances. O item está em Itens a Retirar.";

            public static string TabMineCount(int count, int max) => TabMine + " (" + count + "/" + max + ")";
            public static string TabWithdrawCount(int count) => count > 0 ? TabWithdraw + " (" + count + ")" : TabWithdraw;
            public static string EndsIn(string time) => "Termina em " + time;
            public static string BuyFor(string price) => "Comprar por " + price;
            public static string ListFor(double days) => "Anunciar por " + Format.Decimal(days, 0) + " dias";
            public static string Fee(double ratio) => "Taxa na venda (" + Format.Percent(ratio, 0) + ")";
            public static string AquariumSlots(int count, int capacity) => "Aquário: " + count + "/" + capacity;
            public static string FishLine(string size, string category) => size + " · " + category;
            public static string RodLine(string tier, int level, bool hasLevels) => hasLevels ? tier + " · Nível " + level : tier;

            public static string Bought(string name) => "Compra feita: " + name + ". Está em Itens a Retirar.";
            public static string Listed(string name, string price) => "Anúncio criado: " + name + " por " + price + " Moedas.";
            public static string Cancelled(string name) => "Anúncio cancelado. " + name + " está em Itens a Retirar.";
            public static string Sold(string buyer, string name, string net) => buyer + " comprou " + name + ". +" + net + " Moedas (já sem a taxa).";
            public static string Expired(string name) => "O anúncio de " + name + " venceu. Ele está em Itens a Retirar.";
            public static string Withdrawn(string name) => "Item retirado: " + name + ".";
            public static string WithdrewAll(int count, int left) => left > 0
                ? (count == 1 ? "1 item retirado. " : Format.Number(count) + " itens retirados. ")
                  + (left == 1 ? "1 peixe ficou porque o Aquário está cheio." : Format.Number(left) + " peixes ficaram porque o Aquário está cheio.")
                : (count == 1 ? "1 item retirado." : Format.Number(count) + " itens retirados.");
            public static string SaleLine(string name, string buyer, string price, string fee) => name + " → " + buyer + " · " + price + " Moedas (taxa " + fee + ")";

            public static string Reason(string reasonKey)
            {
                switch (reasonKey)
                {
                    case "bought": return "Comprado";
                    case "cancelled": return "Anúncio cancelado";
                    case "expired": return "Anúncio vencido";
                    case "auction_won": return "Ganho no leilão";
                    case "auction_unsold": return "Leilão sem lances";
                    default: return reasonKey;
                }
            }
        }

        // ------------------------------------------------------------------ Expeditions (GDD section 32)

        public static class Expedition
        {
            public const string Title = "Expedição";
            public const string Send = "Enviar o Cardume";
            public const string Recommended = "Força recomendada";
            public const string YourStrength = "Força do seu Cardume";
            public const string Efficiency = "Aproveitamento";
            public const string Reward = "Moedas previstas";
            public const string FishChance = "Chance de achar um peixe";
            public const string FishChanceShort = "Chance de peixe";
            public const string Away = "Seu Cardume está em Expedição";
            public const string LockNote = "Enquanto ele estiver fora, você não pode mudar a formação, alimentar nem vender os peixes do Cardume. A pesca continua normalmente.";
            public const string Note = "Expedições dão Moedas e, às vezes, um peixe (que vai para a Caixa de Pesca). Não dão XP nem Conchas, e nenhum peixe se perde. Funcionam com o jogo fechado.";
            public const string CompletedToast = "Seu Cardume voltou da Expedição! Abra a Expedição para ver o que ele trouxe.";
            public const string ResultTitle = "Seu Cardume voltou!";
            public const string ReportLabel = "RELATÓRIO DA EXPEDIÇÃO";
            public const string Brought = "O que ele trouxe";
            public const string FoundFish = "Seu Cardume encontrou um peixe!";
            public const string NoFish = "Nenhum peixe desta vez.";
            public const string Collect = "Ótimo!";
            public const string ReportNote = "Tudo já está com você: as Moedas no saldo e o peixe na Caixa de Pesca.";
            public const string NoCardume = "Monte seu Cardume no Perfil para poder enviar Expedições.";

            public const string Cancel = "Cancelar Expedição";
            public const string CancelTitle = "Cancelar a Expedição?";
            public const string CancelBody = "O Cardume volta agora e não traz nada: nem Moedas, nem peixe. Depois você pode enviá-lo de novo.";
            public const string KeepGoing = "Deixar continuar";
            public const string Cancelled = "Expedição cancelada. O Cardume voltou sem recompensa.";

            public static string Departed(string name) => "Cardume enviado: " + name + ".";
            public static string Duration(string duration) => "Duração: " + duration;
            public const string ReturnsInLabel = "Volta em";
            public static string ReturnsIn(string countdown) => ReturnsInLabel + " " + countdown;
            public static string Coins(string coins) => "+" + coins + " Moedas";
            public static string ReturnedAt(string name, string when) => name + " · voltou em " + when;
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
            // Information, not a lock (M24-T58, A-180): a map opens only by the Fisher level.
            public const string RodHere = "Sua vara aqui";
            public const string RodCatchesAll = "Pesca todas as raridades";
            public static string RodMisses(string rarities) => "Não pesca " + rarities;
            public const string Locked = "Bloqueado";

            public static string Departing(string map) => "Partindo para " + map + ".";
            public static string Traveling(string map) => "Viajando para " + map + "…";
            public static string ArrivesIn(string countdown) => "Chegada em " + countdown;
            public static string TravelTime(string duration) => "Viagem: " + duration;
            public static string Discovered(int found, int total) => found + " de " + total + " descobertas";
            public static string LevelRequirement(int level) => "Nível " + level;
            public static string Chapter(int number) => "CAPÍTULO " + number;
            public static string LevelShort(int level) => "Nv. " + level;
            public static string TravelWithTime(string duration) => Travel + " · " + duration;

            /// <summary>The one-line feeling of each map on arrival (docs/IDENTIDADE_POR_MAPA.md).</summary>
            public static string Feeling(string mapId)
            {
                switch (mapId)
                {
                    case "map_01": return "Um começo tranquilo e bonito.";
                    case "map_02": return "A aventura sai do conforto do lago.";
                    case "map_03": return "Agora o mundo se abriu de verdade.";
                    case "map_04": return "Cheguei a uma nova fronteira do jogo.";
                    case "map_05": return "Finalmente cheguei ao mar aberto da costa.";
                    case "map_06": return "Longe da costa, os peixes viram troféus.";
                    case "map_07": return "Só o horizonte e o mar azul em volta.";
                    case "map_08": return "Aqui o oceano mostra o seu tamanho.";
                    case "map_09": return "A noite revela o que vive no fundo.";
                    case "map_10": return "O destino final de todo pescador.";
                    default: return null;
                }
            }
        }

        // ------------------------------------------------------------------ Shop (GDD sections 7, 19)

        public static class Shop
        {
            public const string Free = "Grátis";
            public const string ClaimFree = "Pegar grátis";
            public const string Title = "Loja";
            public const string Rods = "Varas";
            public const string Buy = "Comprar";
            public const string Owned = "Já é sua";
            public const string AtLevel1 = "No Nível 1";
            public const string AtMax = "No nível máximo";
            public const string Note = "Varas não são vendidas: cada vara é montada com fragmentos que às vezes caem quando você puxa um peixe em qualquer mapa (também na pesca offline) ou que vêm do Passe do Pescador. Com todos os fragmentos, aperte Montar: para montar uma vara você precisa ter a vara anterior no Inventário (a primeira pede o Caniço Manso). A vara montada vai para o Inventário (Perfil) no Nível 1; equipe quando quiser. Melhore o nível da sua vara aqui ou no Inventário (cada nível dá um pouco mais, até o Nível 100); lá você também vende ou destrói varas. Os mapas abrem só pelo seu nível; uma vara mais fraca só pesca menos raridades.";
            public const string Upgrade = "Melhorar";
            public const string SellRod = "Vender";
            public const string DestroyRod = "Destruir";
            public const string MaxLevel = "Nível máximo";

            public static string Price(string coins) => coins + " Moedas";
            public static string Bought(string rod) => "Vara " + rod + " equipada!";
            public static string Upgraded(string rod, int level) => rod + " agora está no Nível " + level + ".";
            public static string RodSold(string coins) => "Vara vendida por " + coins + " Moedas.";
            public static string RodDestroyed(string rod) => "A vara " + rod + " foi destruída.";
            public static string UpgradeFor(int level, string coins) => "Melhorar → Nv. " + level + " (" + coins + ")";
            public static string UpgradeForWithShells(int level, string coins, string shells) => "Nv. " + level + ": " + coins + " + " + shells + " Conchas";
            public static string SellFor(string coins) => "Vender (" + coins + ")";
            public static string MaxLevelOf(int max) => "Até o Nível " + max;

            // Levels up to Nv.100 (M24-T13, A-156).
            public static string LevelOf(int level, int max) => "Nível " + level + "/" + max;
            public static string AtLevelNow(int level) => "No Nível " + level + " (agora)";
            public static string UpgradeTo(int level) => "Melhorar → Nv. " + level;
            public static string SellTitle(string rod) => "Vender " + rod + "?";
            public static string SellBody(string coins) => "O jogo paga " + coins + " Moedas por ela. A vara sai do seu Inventário.";
            public static string DestroyTitle(string rod) => "Destruir " + rod + "?";
            public const string DestroyBody = "A vara some do Inventário e você não recebe nada. Esta ação não pode ser desfeita.";
            public const string CatchBonus = "Sucesso da captura";

            // "Balcão do Píer" (A-150).
            public const string OnCounter = "no balcão";
            public const string OwnedBoat = "Já é seu";
            public const string StarterRod = "Já é sua desde o começo";
            public static string Position(int index, int total) => Format.Number(index) + " de " + Format.Number(total);
            public static string Info(string tabNote, string chanceNote) => tabNote + "\n\n" + chanceNote;
        }

        // ------------------------------------------------------------------ owner's test tools (A-123)

        public static class Dev
        {
            public const string Title = "Ferramentas de teste";
            public const string Subtitle = "Só no Editor do Unity · F2 abre e fecha";
            public const string Button = "Testes (F2)";
            public const string Currencies = "Moedas e recursos";
            public const string Time = "Avançar o tempo";
            public const string Open = "Com o jogo aberto";
            public const string Closed = "Com o jogo fechado";
            public const string TimeNote = "Aberto: pesca online (1 tentativa a cada ciclo de pesca). Fechado: pesca offline e o Bem-vindo de volta. Expedições, Energia e o resto andam junto.";
            public const string Level = "Nível do Pescador";
            public const string Fish = "Dar peixes (vão para a Caixa, sem XP)";
            public const string Search = "Buscar espécie";
            public const string RandomSize = "Aleatório";
            public const string Arena = "Arena";
            public const string FillEnergy = "Encher Energia";
            public const string Close = "Fechar";
            public static string Hours(double hours) => "+" + (hours == 1 ? "1 hora" : Format.Number((long)hours) + " horas");
            public static string Give(int count) => "Dar " + count;
            public static string Gave(string what) => "Teste: " + what + ".";
            public static string GaveFish(int count, string species) => "Teste: " + count + " × " + species + " na Caixa de Pesca.";
            public static string LevelSet(int level) => "Teste: Pescador no Nível " + level + ".";
            public const string EnergyFilled = "Teste: Energia da Arena cheia.";
            public static string Advanced(double hours, bool online) => "Teste: +" + (hours == 1 ? "1 hora" : Format.Number((long)hours) + " horas") + (online ? " com o jogo aberto." : " com o jogo fechado.");
        }

        // ------------------------------------------------------------------ VIP (A-110)

        public static class Vip
        {
            public const string Tab = "VIP";
            public const string Title = "VIP do Pescador";
            public const string Active = "VIP ativo";
            public const string Inactive = "Sem VIP";
            public const string Buy = "Comprar VIP";
            public const string Extend = "Renovar VIP";
            public const string Note = "Comprar com o VIP ativo soma os dias ao fim do atual. O VIP não muda a pesca com o jogo aberto, as chances nem as vendas.";
            public static string NoteWithCycle(string vipCycle, string normalCycle) => "Com o VIP, a pesca com o jogo fechado faz 1 tentativa a cada " + vipCycle + " (sem VIP, a cada " + normalCycle + "). Comprar com o VIP ativo soma os dias ao fim do atual. O VIP não muda a pesca com o jogo aberto, as chances nem as vendas.";
            public static string Benefit(string percent) => "+" + percent + " de XP de Pescador na pesca offline";
            public static string Duration(string days) => "Dura " + days + " dias";
            public static string Price(string dollars) => dollars + " Dólares";
            public static string YourDollars(string dollars) => "Você tem " + dollars + " Dólares";
            public static string Until(string date, string remaining) => "Até " + date + " (faltam " + remaining + ")";
            public static string Bought(string date) => "VIP ativo até " + date + "!";
        }

        // ------------------------------------------------------------------ Passe do Pescador (M24-T17, A-158)

        public static class Pass
        {
            public const string Title = "Passe do Pescador";
            public const string ButtonTip = "Passe do Pescador";
            public const string Free = "Grátis";
            public const string PremiumRow = "Premium";
            public const string PremiumActive = "Premium ativo";
            public const string Claim = "Resgatar";
            public const string Claimed = "Resgatado";
            public const string ClaimAll = "Resgatar tudo";
            public const string NothingToClaim = "Nada para resgatar";
            public const string AllReached = "Todos os marcos alcançados";
            public const string Locked = "Premium";
            public const string Empty = "—";
            public const string DollarsPaid = "já recebidos";
            public static string Info(int every, string price) =>
                "O Passe sobe com o nível do Pescador: a cada " + every + " níveis você chega a um marco, do Nv." + every + " ao Nv.1000. " +
                "Cada marco tem uma recompensa grátis e uma Premium: Moedas, Conchas, Dólares e fragmentos de barco, de vara (nos marcos que liberam um mapa novo) e de roupa. " +
                "Os marcos alcançados ficam esperando: aperte Resgatar em cada um ou Resgatar tudo. " +
                "O Premium custa " + price + " Dólares, é comprado uma vez e vale para sempre; ele libera também as recompensas Premium dos marcos que você já passou. " +
                "Os Dólares que antes vinham a cada 10 níveis agora estão na fileira Grátis do Passe.";
            public static string YourLevel(int level) => "Seu nível: " + level;
            public static string NextMilestone(int level) => "Próximo marco: Nv." + level;
            public static string MilestoneLevel(int level) => "Nv." + level;
            public static string BuyPremium(string price) => "Comprar Premium (" + price + " Dólares)";
            public static string YourDollars(string dollars) => "Você tem " + dollars + " Dólares";
            public static string ClaimAllCount(int count) => "Resgatar tudo (" + count + ")";
            public static string Fragments(string amount, string boat) => amount + " frag. · " + boat;
            public static string RodFragments(string amount, string rod) => amount + " frag. · " + rod;
            public static string ClothingFragments(string amount) => amount + " frag. de Roupa";
            public static string DollarsLine(string amount) => amount + " Dólares";
            public const string BoughtPremium = "Premium do Passe comprado! As recompensas Premium dos marcos que você já passou estão prontas para resgatar.";
            public static string ClaimedToast(int count) => count == 1 ? "Passe: 1 recompensa resgatada." : "Passe: " + count + " recompensas resgatadas.";
            public static string ClaimedItems(string items) => "Passe: " + items;
            public static string ItemCoins(string amount) => "+" + amount + " Moedas";
            public static string ItemShells(string amount) => "+" + amount + " Conchas";
            public static string ItemDollars(string amount) => "+" + amount + " Dólares";
        }

        // ------------------------------------------------------------------ Missões (M24-T54, A-177)

        public static class Missions
        {
            public const string Title = "Missões";
            public const string Info =
                "São 100 missões que acompanham o jogo inteiro, do primeiro peixe ao Nv.1000, todas à vista desde o começo. " +
                "Cada uma mostra o que fazer, o quanto falta (\"X / Y\") e o prêmio: Moedas e Conchas, que crescem ao longo da lista, e Dólares nas mais difíceis. " +
                "Quando uma missão é concluída ela fica pronta: aperte Resgatar nela ou Resgatar todas. Cada prêmio é pago uma vez só. " +
                "Uma missão concluída continua concluída, mesmo que depois você gaste as Moedas ou venda o peixe. " +
                "As missões marcadas \"Liga quando o jogo estiver online\" precisam de outros jogadores de verdade (o seu Clã) e só contam com o jogo online.";
            public const string TabActive = "Em andamento";
            public const string TabReady = "Prontas";
            public const string TabDone = "Concluídas";
            public const string Claim = "Resgatar";
            public const string Done = "Concluída";
            public const string Ready = "Pronta!";
            public const string OnlineOnly = "Liga quando o jogo estiver online";
            public const string EmptyActive = "Nenhuma missão em andamento: todas foram concluídas!";
            public const string EmptyReady = "Nenhuma missão pronta para resgatar agora. Continue jogando!";
            public const string NothingToClaim = "Nada para resgatar";
            public const string EmptyDone = "Nenhuma missão concluída ainda. Resgate as prontas para vê-las aqui.";
            public static string ClaimAll(int count) => "Resgatar todas (" + count + ")";
            public static string Counter(int claimed, int total) => claimed + " / " + total + " concluídas";
            /// <summary>"3 / 10", "523.000 / 1 mi", "431 mi / 1 bi": big numbers in short form so the bar's label fits.</summary>
            public static string Progress(long current, long target) => Format.Short(current) + " / " + Format.Short(target);
            public static string BestRank(long rank) => rank > 0 ? "Sua melhor posição: " + Format.Number(rank) + "º" : "Você ainda não está no Ranking";
            public static string ReadyToast(string text) => "Missão concluída: " + text + ". Resgate em Missões!";
            public static string ReadyManyToast(int count) => count + " missões concluídas. Resgate em Missões!";
            public static string ClaimedItems(int count, string items) => (count == 1 ? "Missão resgatada: " : count + " missões resgatadas: ") + items;
            public static string ItemCoins(string amount) => "+" + amount + " Moedas";
            public static string ItemShells(string amount) => "+" + amount + " Conchas";
            public static string ItemDollars(string amount) => "+" + amount + (amount == "1" ? " Dólar" : " Dólares");

            /// <summary>A species above Comum with its rarity: "Tubarão-boca-grande (Mítico)".</summary>
            public static string WithRarity(string name, string rarity) => name + " (" + rarity + ")";

            /// <summary>
            /// The goal of a mission in PT-BR, from its type (missions.json), its target and the name it points at (species,
            /// rarity, size, map, rod, boat or bait, from /config). <paramref name="number"/> is the stars, the fish level or the
            /// top gene value; <paramref name="all"/> = "todas" / "a faixa máxima".
            /// </summary>
            public static string Objective(string type, long target, string name, int number, bool all, bool fishOnly, bool contested)
            {
                var one = target == 1;
                var n = Format.Number(target);
                switch (type)
                {
                    case "catch_count": return one ? "Pesque 1 peixe" : "Pesque " + Amount(target, "peixes");
                    case "catch_species":
                        return one ? "Pesque " + Article(name) + " " + name : "Pesque " + n + " " + Plural(name);
                    case "catch_rarity":
                        return one ? "Pesque o primeiro peixe " + name : "Pesque " + n + " peixes " + Plural(name);
                    case "catch_size_category":
                        return one ? "Pesque um peixe de tamanho " + name : "Pesque " + n + " peixes de tamanho " + name;
                    case "catch_gene_stars":
                        return one ? "Pesque um peixe " + number + " estrelas" : "Pesque " + n + " peixes " + number + " estrelas";
                    case "catch_perfect_gene":
                        return one ? "Pesque um peixe com um gene perfeito (" + number + ")" : "Pesque " + n + " peixes com um gene perfeito (" + number + ")";
                    case "catch_perfect_specimen":
                        return one ? "Pesque um Exemplar perfeito (os 4 genes em " + number + ")" : "Pesque " + n + " Exemplares perfeitos (os 4 genes em " + number + ")";
                    case "fisher_level": return "Chegue ao Nv." + target + " de Pescador";
                    case "coins_held": return "Junte " + Amount(target, "Moedas");
                    case "shells_held": return "Junte " + Amount(target, "Conchas");
                    case "dollars_spent": return "Gaste " + Amount(target, "Dólares");
                    case "box_sell": return one ? "Venda 1 peixe da Caixa de Pesca" : "Venda " + n + " peixes da Caixa de Pesca";
                    case "aquarium_keep": return one ? "Guarde 1 peixe no Aquário" : "Guarde " + n + " peixes no Aquário";
                    case "fish_feed": return one ? "Alimente um peixe do Aquário pela primeira vez" : "Alimente peixes do Aquário " + n + " vezes";
                    case "fish_level": return "Suba um peixe do Aquário ao Nv." + target;
                    case "cardume_size": return "Monte seu Cardume com " + n + " peixes";
                    case "cardume_strength": return "Alcance " + Amount(target, "de força no Cardume");
                    case "cardume_all_rarity_min": return "Monte um Cardume completo só com peixes " + Plural(name) + " ou melhores";
                    case "cardume_gene_stars": return "Tenha " + n + " peixes " + number + " estrelas no Cardume ao mesmo tempo";
                    case "cardume_all_level": return "Tenha um Cardume completo com todos os peixes no Nv." + number;
                    case "map_unlocked": return "Libere " + Article(name) + " " + name;
                    case "encyclopedia_species":
                        return all ? "Registre todas as espécies na Enciclopédia" : "Registre " + n + " espécies na Enciclopédia";
                    case "map_all_species": return "Pesque todas as " + n + " espécies " + Of(name) + " " + name;
                    case "rarity_all_species": return "Pesque todas as espécies " + FemininePlural(name);
                    case "size_records": return one ? "Bata o recorde de tamanho de uma espécie" : "Bata o recorde de tamanho de " + n + " espécies diferentes";
                    case "rod_assembled": return "Monte a vara " + name;
                    case "boat_assembled": return "Monte o barco " + name;
                    case "boats_all": return "Monte todos os " + n + " barcos";
                    case "clothing_assembled":
                        return all ? "Monte todas as peças de roupa" : one ? "Monte a primeira peça de roupa" : "Monte as " + n + " peças de roupa";
                    case "clothing_max_level":
                        return all ? "Suba todas as roupas ao nível máximo" : one ? "Suba uma peça de roupa ao nível máximo" : "Suba " + n + " peças de roupa ao nível máximo";
                    case "bait_used": return one ? "Use uma isca " + name : "Use a isca " + name + " " + n + " vezes";
                    case "pass_rewards_claimed": return one ? "Pegue 1 recompensa do Passe do Pescador" : "Pegue " + n + " recompensas do Passe do Pescador";
                    case "expedition_started": return one ? "Mande o Cardume na primeira Expedição" : "Mande o Cardume em " + n + " Expedições";
                    case "expeditions_completed": return one ? "Complete uma Expedição" : "Complete " + n + " Expedições";
                    case "arena_duels": return one ? "Faça seu primeiro duelo na Arena" : "Faça " + n + " duelos na Arena";
                    case "arena_wins": return one ? "Ganhe 1 duelo na Arena" : "Ganhe " + n + " duelos na Arena";
                    case "arena_win_streak": return "Ganhe " + n + " duelos seguidos na Arena";
                    case "arena_distinct_opponents": return "Ataque " + n + " jogadores diferentes na Arena";
                    case "arena_win_stronger":
                        return one ? "Vença um jogador mais forte que você na Arena" : "Vença " + n + " jogadores mais fortes que você na Arena";
                    case "arena_rank_top": return "Chegue ao top " + n + " do Ranking da Arena";
                    case "market_listing": return one ? "Coloque o primeiro anúncio no Mercado" : "Coloque " + n + " anúncios no Mercado";
                    case "market_buy":
                        if (fishOnly)
                        {
                            return one ? "Compre um peixe anunciado por outro jogador no Mercado" : "Compre " + n + " peixes de outros jogadores no Mercado";
                        }

                        return one ? "Compre um item no Mercado" : "Compre " + n + " itens no Mercado";
                    case "market_sell":
                        if (name != null)
                        {
                            return one ? "Venda 1 peixe " + name + " no Mercado" : "Venda " + n + " peixes " + Plural(name) + " no Mercado";
                        }

                        return one ? "Venda um peixe para outro jogador no Mercado" : "Venda " + n + " peixes no Mercado";
                    case "auction_bid": return one ? "Dê um lance num Leilão" : "Dê " + n + " lances em Leilões";
                    case "auction_won":
                        if (contested)
                        {
                            return one ? "Ganhe um Leilão contra outros jogadores" : "Ganhe " + n + " Leilões contra outros jogadores";
                        }

                        return one ? "Ganhe um Leilão" : "Ganhe " + n + " Leilões";
                    case "boss_attack": return one ? "Ataque o Boss Mundial pela primeira vez" : "Ataque o Boss Mundial " + n + " vezes";
                    case "boss_band": return all ? "Chegue à faixa máxima de dano do Boss Mundial" : "Chegue à faixa " + n + " de dano do Boss Mundial";
                    case "boss_days": return "Ataque o Boss Mundial em " + n + " dias diferentes";
                    case "clan_join": return "Entre em um Clã ou crie o seu";
                    case "clan_fish": return "Coloque seu melhor peixe no Cardume do Clã";
                    case "clan_same_day_boss": return "Ataque o Boss Mundial no mesmo dia que outro membro do seu Clã";
                    case "clan_rank_top": return "Fique no top " + n + " de Clãs no ranking diário";
                    case "profile_visit": return one ? "Visite o perfil de outro jogador" : "Visite o perfil de outros jogadores " + n + " vezes";
                    default: return type;
                }
            }

            /// <summary>"10.000 Moedas", "1 milhão de Moedas", "2 bilhões de peixes": round millions and billions in words.</summary>
            public static string Amount(long value, string noun)
            {
                if (value >= 1_000_000_000L && value % 1_000_000_000L == 0)
                {
                    var b = value / 1_000_000_000L;
                    return Format.Number(b) + (b == 1 ? " bilhão de " : " bilhões de ") + StripDe(noun);
                }

                if (value >= 1_000_000L && value % 1_000_000L == 0)
                {
                    var m = value / 1_000_000L;
                    return Format.Number(m) + (m == 1 ? " milhão de " : " milhões de ") + StripDe(noun);
                }

                return Format.Number(value) + " " + noun;
            }

            private static string StripDe(string noun) => noun.StartsWith("de ", System.StringComparison.Ordinal) ? noun.Substring(3) : noun;

            /// <summary>The article of a name by its first word: "a" for one ending in "a" (Costa, Piranha), otherwise "o".</summary>
            public static string Article(string name)
            {
                return FirstWord(name).EndsWith("a", System.StringComparison.Ordinal) ? "a" : "o";
            }

            /// <summary>"do" or "da" (de + the article).</summary>
            public static string Of(string name) => Article(name) == "a" ? "da" : "do";

            /// <summary>
            /// PT-BR plural of a name, on its first word when it has a hyphen or a space ("Lambari" → "Lambaris", "Raro" →
            /// "Raros", "Tubarão-boca-grande" → "Tubarões-boca-grande", "Comum" → "Comuns").
            /// </summary>
            public static string Plural(string name)
            {
                if (string.IsNullOrEmpty(name))
                {
                    return name ?? string.Empty;
                }

                var word = FirstWord(name);
                var rest = name.Substring(word.Length);
                string plural;
                if (word.EndsWith("ão", System.StringComparison.Ordinal)) plural = word.Substring(0, word.Length - 2) + "ões";
                else if (word.EndsWith("m", System.StringComparison.Ordinal)) plural = word.Substring(0, word.Length - 1) + "ns";
                else if (word.EndsWith("l", System.StringComparison.Ordinal)) plural = word.Substring(0, word.Length - 1) + "is";
                else if (word.EndsWith("r", System.StringComparison.Ordinal) || word.EndsWith("z", System.StringComparison.Ordinal)) plural = word + "es";
                else if (word.EndsWith("s", System.StringComparison.Ordinal)) plural = word;
                else plural = word + "s";
                return plural + rest;
            }

            /// <summary>Feminine plural of a rarity ("Lendário" → "Lendárias", "Comum" → "Comuns").</summary>
            public static string FemininePlural(string name)
            {
                if (string.IsNullOrEmpty(name))
                {
                    return name ?? string.Empty;
                }

                return name.EndsWith("o", System.StringComparison.Ordinal) ? name.Substring(0, name.Length - 1) + "as" : Plural(name);
            }

            private static string FirstWord(string name)
            {
                if (string.IsNullOrEmpty(name))
                {
                    return string.Empty;
                }

                var cut = name.IndexOfAny(Separators);
                return cut > 0 ? name.Substring(0, cut) : name;
            }

            private static readonly char[] Separators = { '-', ' ' };
        }

        // ------------------------------------------------------------------ Eventos (M24-T39, A-167)

        public static class Events
        {
            public const string Title = "Eventos";
            public const string Info =
                "Os eventos do jogo ficam aqui. Cada cartão mostra quando o evento acontece, se ele já começou e o que " +
                "você conseguiu. Clique em \"Abrir\" para entrar no evento. Quando um evento está acontecendo, o botão " +
                "Eventos, na coluna de atalhos à direita, fica dourado com o tempo que falta.";
            public const string Back = "‹ Eventos";
            public const string Open = "Abrir";
            public const string Now = "AGORA";
            public static string Schedule(string time, int minutes) => "Todo dia às " + time + " (Brasília), " + minutes + " minutos";
            public static string StartsIn(string countdown) => "Começa em " + countdown;
            public static string Happening(string countdown) => "Acontecendo agora · faltam " + countdown;
            public static string BandToday(int band) => band > 0 ? "Sua faixa de hoje: Faixa " + band : "Sua faixa de hoje: nenhuma ainda";
            public static string LastResult(string date, string band, string items) => "Último (" + date + "): " + band + " · " + items;
            public const string NoLastResult = "Você ainda não participou.";
            public static string Counter(int happening) => happening > 0 ? (happening == 1 ? "1 acontecendo agora" : happening + " acontecendo agora") : null;
        }

        // ------------------------------------------------------------------ Boss Mundial (M24-T29, A-165)

        public static class WorldBoss
        {
            public const string Title = "Boss Mundial";
            public const string NowOpen = "AGORA";
            public static string Info(string time, int minutes, string interval) =>
                "Todo dia às " + time + " (horário de Brasília) o Boss Mundial aparece por " + minutes + " minutos. Ele tem vida infinita: " +
                "o objetivo é causar o maior dano possível. Com o jogo aberto, seu Cardume ataca sozinho a cada " + interval + " segundos, " +
                "com a mesma força da Arena; com o jogo fechado ele não ataca. O dano do dia leva você às faixas de recompensa " +
                "(Moedas, Conchas, Fragmentos de Roupa e fragmentos de barco); ficar os " + minutes + " minutos inteiros leva à última. " +
                "Você recebe a recompensa da maior faixa alcançada quando o evento acaba, ou na próxima vez que abrir o jogo. " +
                "Os 10 maiores danos do dia também ganham Dólares, quando o jogo estiver online. Cardume vazio ou em Expedição não ataca.";
            public static string Counter(string countdown) => "Começa em " + countdown;
            public static string CounterOpen(string countdown) => "Termina em " + countdown;
            public static string StartsIn(string countdown) => "O Boss aparece em " + countdown;
            public static string Daily(string time, int minutes) => "Todo dia às " + time + " (horário de Brasília), por " + minutes + " minutos";
            public static string TimeLeft(string countdown) => "Tempo restante: " + countdown;
            public const string InfiniteHp = "Vida infinita";
            public const string YourCardume = "Seu Cardume";
            public const string DamageToday = "Dano de hoje";
            public static string Attacks(string count, string max) => count + " de " + max + " ataques";
            public const string Bands = "Faixas de recompensa";
            public static string Band(int number) => "Faixa " + number;
            public static string BandPercent(string percent) => percent + " do dano máximo";
            public static string BandThreshold(string damage) => damage + " de dano";
            public static string NextBand(int number, string damage) => "Próxima: Faixa " + number + " com " + damage + " de dano";
            public static string NextBandPercent(int number, string percent) => "Próxima: Faixa " + number + " (" + percent + " do dano máximo)";
            public const string AllBands = "Todas as faixas alcançadas!";
            public const string NoBand = "Nenhuma faixa ainda";
            public const string YourReward = "Sua recompensa (paga quando o evento acabar)";
            public const string NoReward = "Cause dano para chegar à Faixa 1.";
            public const string RankingToday = "Ranking de hoje";
            public const string RankingEmpty = "Ninguém atacou ainda hoje.";
            public const string RankingLocalNote = "Ranking com outros jogadores quando o jogo estiver online.";
            public const string DollarsTitle = "Dólares do top 10";
            public static string DollarsRank(int rank) => rank + "º";
            public static string DollarsAmount(string amount) => amount + " Dólares";
            public const string DollarsOffline = "Pagos quando o jogo estiver online.";
            public const string You = "(você)";
            public const string CardumeEmpty = "Seu Cardume está vazio: coloque peixes no Cardume (Perfil → Cardume) para atacar o Boss.";
            public const string CardumeLocked = "Seu Cardume está em uma Expedição e não pode atacar o Boss agora.";
            public const string Waiting = "O Boss ainda não apareceu.";
            public const string ClosedHint = "Deixe o jogo aberto no horário: o Cardume ataca sozinho.";
            public static string LastResultTitle(string date) => "Último evento (" + date + ")";
            public static string LastResultDamage(string damage, string attacks) => damage + " de dano em " + attacks + " ataques";
            public static string LastResultBand(int band) => band > 0 ? "Faixa " + band : "Nenhuma faixa";
            public static string LastResultRank(int rank) => rank + "º no ranking do dia";
            public const string LastResultNone = "Você ainda não participou de um Boss Mundial.";
            public const string LastResultNothing = "Nenhuma recompensa.";
            public static string ItemCoins(string amount) => "+" + amount + " Moedas";
            public static string ItemShells(string amount) => "+" + amount + " Conchas";
            public static string ItemClothing(long count) => count == 1 ? "+1 Fragmento de Roupa" : "+" + Format.Number(count) + " Fragmentos de Roupa";
            public static string ItemBoat(string amount, string boat) => "+" + amount + " frag. · " + boat;
            public static string ItemDollars(string amount) => "+" + amount + " Dólares";
            public static string DamagePop(string amount) => "-" + amount;

            // Notices
            public static string StartedToast(string boss) => "O Boss Mundial apareceu: " + boss + "! Seu Cardume já está atacando.";
            public static string BandToast(int band) => "Boss Mundial: você chegou à Faixa " + band + "!";
            public static string PaidToast(string items) => "Boss Mundial: " + items;
        }

        // ------------------------------------------------------------------ Clãs (M24-T49, A-174)

        public static class Clan
        {
            public const string Title = "Clã";
            public static string Info(int minLevel, string bonus, int baseMembers) =>
                "Pescadores do Nv." + minLevel + " para cima podem criar um clã (com Dólares) ou entrar num. Todo clã começa com " +
                baseMembers + " vagas; o dono pode comprar mais vagas com Dólares, cada uma mais cara. Quem está num clã ganha +" + bonus +
                " de XP de Pescador em cada peixe pescado. Cada membro coloca 1 peixe do Aquário no Cardume do Clã: o peixe continua " +
                "no seu Aquário e no seu Cardume. A Força do clã é a soma da Força desses peixes, com a mesma regra do Cardume. Todo dia " +
                "às 00:00 (horário de Brasília) os 3 clãs mais fortes ganham prêmios: isso liga quando o jogo estiver online.";
            public static string Counter(int members, int capacity) => members + "/" + capacity + " membros";

            // Below the level / without a clan
            public static string LevelTitle(int level) => "Clãs a partir do Nv." + level;
            public static string LevelRequired(int level, int current) => "Só Pescadores do Nv." + level + " para cima podem criar ou entrar num clã. Você está no Nv." + current + ".";
            public static string LevelProgress(int current, int level) => "Nv." + current + " de " + level;
            public const string NoClanTitle = "Você ainda não tem clã";
            public const string CreateTitle = "Criar um clã";
            public const string NameLabel = "Nome do clã";
            public static string NameRule(int min, int max) => "De " + min + " a " + max + " caracteres: letras, números, espaço, ponto, hífen ou sublinhado. Não pode repetir o nome de outro clã.";
            public static string CreateButton(string cost) => "Criar clã · " + cost + " Dólares";
            public const string CreateButtonFree = "Criar clã · Grátis";
            public const string CreateFreeNote = "O primeiro clã de cada jogador é grátis; os próximos custam Dólares.";
            public static string YourDollars(string amount) => "Você tem " + amount + " Dólares";
            public const string JoinTitle = "Entrar num clã";
            public const string JoinOnline = "A lista de clãs para entrar aparece quando o jogo estiver online. Por enquanto, crie o seu.";
            public static string BonusLine(string bonus) => "Quem está num clã ganha +" + bonus + " de XP de Pescador em cada peixe.";

            // The clan
            public static string OwnerLine(string owner) => "Dono: " + owner;
            public static string BonusActive(string bonus) => "Bônus do clã: +" + bonus + " de XP de Pescador";
            public const string Members = "Membros";
            public const string Owner = "Dono";
            public const string You = "(você)";
            public const string NoFish = "Sem peixe no Cardume do Clã";
            public static string MemberFish(string species, int level, string strength) => species + " · Nv." + level + " · Força " + strength;
            public static string MemberLevel(int level) => "Pescador Nv." + level;
            public static string SlotsLine(int baseMembers, int extra, int max) => baseMembers + " vagas + " + extra + " compradas (máximo " + max + ")";
            public static string BuySlot(string price) => "Comprar vaga · " + price + " Dólares";
            /// <summary>The same button when the long one does not fit (the "+" icon already says "comprar").</summary>
            public static string BuySlotShort(string price) => "Vaga · " + price + " Dólares";
            public const string SlotsMaxed = "O clã já tem o máximo de vagas.";
            public const string OnlyOwnerBuys = "Só o dono compra vagas.";
            public const string Leave = "Sair do clã";
            public const string Strength = "Força do clã";
            public static string StrengthValue(string value) => "Força " + value;

            // The clan's Cardume
            public const string ClanCardume = "Cardume do Clã";
            public const string ClanCardumeHint = "Cada membro coloca 1 peixe. Ele continua no seu Aquário e no seu Cardume.";
            public const string YourFish = "Seu peixe";
            public const string NoFishYet = "Você ainda não colocou um peixe no Cardume do Clã.";
            public const string ChooseFish = "Escolher peixe";
            public const string ChangeFish = "Trocar";
            public const string RemoveFish = "Tirar";
            public static string InYourCardume(int position) => "Também no seu Cardume (posição " + position + ")";
            public const string PickTitle = "Escolha o peixe do Cardume do Clã";
            public const string PickHint = "Os mais fortes primeiro. O peixe não sai do Aquário nem do seu Cardume.";
            public const string PickEmpty = "Seu Aquário está vazio. Guarde peixes da Caixa de Pesca no Aquário.";
            public const string Pick = "Escolher";
            public const string Picked = "No clã";
            public static string PickLine(string size, int level) => size + " · Nv." + level;

            // Ranking and the top 3
            public const string Ranking = "Ranking diário dos clãs";
            public static string RankingReset(string countdown) => "Fecha todo dia às 00:00 (Brasília) · faltam " + countdown;
            public const string RewardsTitle = "Prêmios do top 3 (cada membro recebe)";
            public static string Rank(int rank) => rank + "º";
            public const string RewardsOffline = "O ranking entre clãs e os prêmios ligam quando o jogo estiver online.";
            public const string RankingEmpty = "Nenhum clã com peixe no Cardume ainda.";
            public static string RankLine(int rank, string name, string strength, int members) => rank + "º  " + name + " · Força " + strength + " · " + members + " membros";
            public static string LastResult(string date, int rank, string items) => "Último prêmio (" + date + "): " + rank + "º lugar · " + items;
            public static string LastResultNoPrize(string date, int rank) => rank > 0 ? "Último dia (" + date + "): " + rank + "º lugar, sem prêmio" : "Último dia (" + date + "): sem colocação";

            // Leave confirmation
            public const string LeaveTitle = "Sair do clã?";
            public const string LeaveAlone = "Você é o único membro: o clã fecha e o nome fica livre. Os Dólares gastos não voltam.";
            public const string LeaveOwner = "Você é o dono: o membro mais antigo passa a ser o dono.";
            public const string LeaveMember = "Você sai do clã e deixa de ganhar o bônus de XP.";
            public const string LeaveConfirm = "Sair";

            // Notices
            public static string CreatedToast(string name) => "Clã criado: " + name + "!";
            public const string LeftToast = "Você saiu do clã.";
            public static string SlotBoughtToast(int capacity) => "Vaga comprada: o clã agora tem " + capacity + " vagas.";
            public const string FishSetToast = "Peixe colocado no Cardume do Clã.";
            public const string FishClearedToast = "Peixe tirado do Cardume do Clã.";
            public const string FishLeftToast = "Seu peixe saiu do Aquário e deixou o Cardume do Clã.";
            public static string PaidToast(int rank, string items) => "Clã em " + rank + "º lugar no ranking de ontem: " + items;
        }

        // ------------------------------------------------------------------ gear: boats, baits, Catch Success (docs/SISTEMA_SUCESSO_PESCA.md)

        public static class Gear
        {
            public const string TabRods = "Varas";
            public const string TabBoats = "Barcos";
            public const string TabBaits = "Iscas";
            public const string Yours = "Seu equipamento";
            public const string Rod = "Vara";
            public const string Boat = "Barco";
            public const string Bait = "Isca";
            public const string NoBait = "Nenhuma";
            public const string Total = "Bônus total";
            public const string TotalWithGloves = "Total com Luvas";
            public const string ChanceTitle = "Chance de puxar o peixe";
            public static string ChanceNote(string min, string max) => "Depois que o peixe morde, esta é a chance de puxá-lo. Se ele escapa, nada entra na Caixa. Nunca passa de " + max + " nem fica abaixo de " + min + ".";
            public const string NotHere = "não morde aqui";
            /// <summary>One line under the bobbers when a rarity does not bite on this map (the columns are too narrow for it).</summary>
            public const string NotHereLegend = "Boia deitada: não morde aqui";
            public const string InUse = "Em uso";
            public const string Use = "Usar";
            public const string PutAway = "Guardar";
            public const string BoatsNote = "Barcos não são vendidos: cada barco é montado com fragmentos que às vezes caem quando você puxa um peixe em qualquer mapa (também na pesca offline) ou que vêm do Passe do Pescador. Com todos os fragmentos, aperte Montar: para montar um barco você precisa ter o barco anterior (o primeiro pede o Água Mansa, que é seu desde o começo). O barco montado fica com você no Nível 1; aperte Usar para passar a usá-lo. Ele aumenta a chance de puxar todo peixe, e cada nível que você melhora soma um pouco mais, até o Nível 100. Troque quando quiser.";
            public const string BaitsNote = "A isca dura um número de tentativas: gasta 1 por tentativa, puxando o peixe ou não, também offline. Comprar de novo soma tentativas. Só uma isca fica em uso.";
            public const string OpenDetails = "Ver equipamento e chances";

            public static string Bonus(string percent) => "+" + percent + " de chance";
            public static string Charges(int charges) => charges == 1 ? "1 tentativa por compra" : Format.Number(charges) + " tentativas por compra";
            public static string ChargesLeft(int charges) => charges == 1 ? "Resta 1 tentativa" : "Restam " + Format.Number(charges) + " tentativas";
            // Boats by fragments (M24-T16, A-157).
            public static string FragmentsTag(string have, string required) => have + "/" + required;
            public static string FragmentsLine(string have, string required) => "Fragmentos " + have + "/" + required;

            /// <summary>Where the fragments of a boat, a rod or the Fragmento de Roupa drop: every map (M24-T47, A-173).</summary>
            public const string FragmentsEverywhere = "Caem em todos os mapas";

            /// <summary>The short chance line of the Loja sheet (A-190): "0,8% por peixe".</summary>
            public static string FragmentsChanceShort(string percent) => percent + " por peixe";
            public static string FragmentsExtra(string amount) => "Fragmentos extras: " + amount;
            public const string AssembleHint = "Monte com fragmentos";

            // Assembling by hand (M24-T58, A-180).
            public const string Assemble = "Montar";
            public static string NeedsBoat(string boat) => "Precisa do barco " + boat;
            public static string NeedsRod(string rod) => "Precisa da vara " + rod;
            public static string BoatReadyToast(string boat) => "Fragmentos completos: " + boat + "! Monte na Loja → Barcos.";
            public static string BoatReadyNeedsToast(string boat, string previous) => "Fragmentos completos: " + boat + ". Para montar, precisa do barco " + previous + ".";
            public static string RodReadyToast(string rod) => "Fragmentos completos: " + rod + "! Monte na Loja → Varas.";
            public static string RodReadyNeedsToast(string rod, string previous) => "Fragmentos completos: " + rod + ". Para montar, precisa da vara " + previous + " no Inventário.";
            public const string StarterBoat = "Já é seu desde o começo";
            public static string FragmentToast(string boat, string have, string required) => "+1 fragmento de barco: " + boat + " (" + have + "/" + required + ")";
            public static string FragmentExtraToast(string boat) => "+1 fragmento de barco: " + boat + " (extra)";
            public static string FragmentsToast(string amount, string boat) => "+" + amount + " fragmentos de barco: " + boat;
            public static string BoatAssembledToast(string boat) => "Barco montado: " + boat + "! Aperte Usar para passar a usá-lo.";
            public const string BoatAssembledTitle = "Barco montado!";
            public static string UsingBoat(string boat) => "Barco em uso: " + boat + ".";
            public static string UpgradedBoat(string boat, int level) => boat + " agora está no Nível " + level + ".";
            public static string NextLevelBonus(string percent) => "Próximo nível: +" + percent + " de chance";
            public static string MaxLevelBonus(int max, string percent) => "No Nível " + max + ": +" + percent + " de chance";
            public static string BoughtBait(string bait, int charges) => bait + ": +" + (charges == 1 ? "1 tentativa." : Format.Number(charges) + " tentativas.");
            public static string UsingBait(string bait) => "Isca em uso: " + bait + ".";
            public const string BaitPutAway = "Isca guardada. As tentativas que sobraram ficam para depois.";

            // Rods by fragments (M24-T32, A-163).
            public static string RodFragmentToast(string rod, string have, string required) => "+1 fragmento de vara: " + rod + " (" + have + "/" + required + ")";
            public static string RodFragmentsToast(string amount, string rod, string have, string required) => "+" + amount + " fragmentos de vara: " + rod + " (" + have + "/" + required + ")";
            public static string RodFragmentExtraToast(string amount, string rod) => "+" + amount + (amount == "1" ? " fragmento de vara: " : " fragmentos de vara: ") + rod + " (extra)";
            public static string RodFragmentsPassToast(string amount, string rod) => "+" + amount + " fragmentos de vara: " + rod;
            public static string RodAssembledToast(string rod) => "Vara montada: " + rod + "! Ela está no Inventário; equipe quando quiser.";
            public const string RodAssembledTitle = "Vara montada!";
            public static string BoatFragmentsGroupToast(string amount, string boat, string have, string required) => "+" + amount + " fragmentos de barco: " + boat + " (" + have + "/" + required + ")";
            public static string BoatFragmentsExtraGroupToast(string amount, string boat) => "+" + amount + " fragmentos de barco: " + boat + " (extra)";

            // Clothing (M24-T33, A-164).
            public const string TabClothing = "Roupas";
            public const string ClothingNote = "Roupas não são vendidas: um tipo só de fragmento, o Fragmento de Roupa, às vezes cai quando você puxa um peixe em qualquer mapa (também na pesca offline) ou vem do Passe do Pescador. Você escolhe qual peça montar; cada peça montada custa mais fragmentos que a anterior. A peça montada fica vestida para sempre (as cinco juntas) e sobe de nível sozinha com o uso: cada peixe que você puxa conta, até o Nível 100, sem gastar nada.";
            public const string ClothingPiece = "Roupa";
            public const string Wearing = "Vestida";
            public static string ClothingFragmentsHave(string have) => "Fragmentos de Roupa: " + have;
            public static string ClothingAssemble(string cost) => "Montar (" + cost + " fragmentos de roupa)";
            public const string ClothingAllAssembled = "As 5 peças estão montadas. Os fragmentos que sobram ficam guardados.";
            public static string ClothingUseXp(string xp, string need) => "Uso: " + xp + "/" + need + " peixes para o próximo nível";
            public const string ClothingMaxLevel = "Nível máximo: não sobe mais";
            public static string ClothingBonusAtMax(int max, string bonus) => "No Nível " + max + ": " + bonus;
            public static string ClothingAxis(string axis, string percent)
            {
                switch (axis)
                {
                    case "catch_success": return "+" + percent + " de chance de puxar";
                    case "rarity_efficiency": return "+" + percent + " de raridade";
                    case "size_quality": return "+" + percent + " de tamanho";
                    case "shell_yield": return "+" + percent + " de Conchas";
                    case "fisher_xp": return "+" + percent + " de XP de Pescador";
                    default: return "+" + percent;
                }
            }

            public static string ClothingAxisName(string axis)
            {
                switch (axis)
                {
                    case "catch_success": return "Chance de puxar o peixe";
                    case "rarity_efficiency": return "Peixes raros";
                    case "size_quality": return "Peixes grandes";
                    case "shell_yield": return "Conchas";
                    case "fisher_xp": return "XP de Pescador";
                    default: return axis;
                }
            }

            public static string ClothingFragmentToast(string have, string cost) => "+1 Fragmento de Roupa (" + have + "/" + cost + ")";
            public static string ClothingFragmentsToast(string amount, string have, string cost) => "+" + amount + " Fragmentos de Roupa (" + have + "/" + cost + ")";
            public static string ClothingFragmentsExtraToast(string amount) => "+" + amount + (amount == "1" ? " Fragmento de Roupa (extra)" : " Fragmentos de Roupa (extra)");
            public static string ClothingFragmentsPassToast(string amount) => "+" + amount + " Fragmentos de Roupa";
            public static string ClothingReadyToast(string cost) => "Você já pode montar uma roupa (" + cost + " Fragmentos de Roupa). Veja na Loja → Roupas.";
            public static string ClothingAssembledToast(string piece) => "Roupa montada e vestida: " + piece + "!";
            public const string ClothingAssembledTitle = "Roupa montada!";
            public static string ClothingLevelToast(string piece, int level) => piece + " subiu para o Nível " + level + ".";
        }

        // ------------------------------------------------------------------ Cardume (GDD sections 23, 26, 31)

        public static class Cardume
        {
            public const string Title = "Cardume";
            public const string Front = "Frente";
            public const string Back = "Trás";
            public const string FrontTag = "FRENTE";
            public const string BackTag = "TRÁS";
            public const string OrderShort = "Os adversários atacam na ordem 1 → 6: peixes resistentes na frente protegem os de trás.";
            public static string BonusShort(string percent) => "Cardume completo · +" + percent;
            public static string BonusMissingShort(int missing, string percent) => "Faltam " + missing + " para +" + percent;
            public static string StrengthShort(string strength) => "Força " + strength;
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
            public const string RarityBonus = "Chance de raridade";
            public const string SizeBonus = "Qualidade de tamanho";
            public const string ShellBonus = "Conchas";
            public const string CatchesRare = "Pesca peixes Raros";
            public const string CatchesRareAndEpic = "Pesca peixes Raros e Épicos";
            public const string CatchesUpToLegendary = "Pesca peixes Raros, Épicos e Lendários";
            public const string CatchesUpToMythic = "Pesca peixes Raros, Épicos, Lendários e Míticos";
            public const string NoRare = "Não pesca peixes Raros";
            public const string NoShells = "Não gera Conchas";
            public const string InventoryNote = "Suas varas ficam aqui. Varas novas são montadas com fragmentos que caem pescando (veja na Loja → Varas).";
            public const string Undiscovered = "???";
            public const string Largest = "Maior";
            public const string TimesCaught = "Pescados";
            public const string FirstCaught = "Descoberto em";
            public const string TotalCatches = "Capturas";
            public const string Discovered = "Espécies descobertas";
            public const string Biggest = "Maior peixe já pescado";
            public const string HighestLevel = "Peixe de nível mais alto";
            public const string Exceptional = "Capturas Excepcionais";
            public const string Perfect = "Capturas Perfeitas";
            public const string Rare = "Capturas Raras";
            public const string Sold = "Peixes vendidos";
            public const string CoinsFromSales = "Moedas com vendas";
            public const string None = "—";

            public const string TabSummary = "Resumo";
            public const string Edit = "Editar";
            public const string EditTitle = "Seu nome e seu avatar";
            public const string NameLabel = "Nome";
            public const string NameRule = "De 3 a 16 caracteres: letras, números, espaço, ponto, hífen ou sublinhado.";
            public static string NameRuleFor(int min, int max) => "De " + min + " a " + max + " caracteres: letras, números, espaço, ponto, hífen ou sublinhado.";
            public const string AvatarLabel = "Avatar";
            public const string Save = "Salvar";
            public const string Saved = "Nome e avatar salvos.";
            public static string BiteShare(string percent) => "morde " + percent + " das vezes";

            // The Encyclopedia as a card album (A-143): binder tabs, pages of cards and the species sheet.
            public const string EncAllTab = "Todas";
            public const string EncAllTitle = "Todas as espécies";
            public static string EncTabProgress(int found, int total) => found + "/" + total;
            public static string EncPage(int page, int pages) => "Página " + page + " de " + pages;
            public static string EncNumber(int number) => "Nº " + number;

            /// <summary>The catches on an album card (A-190): "37×" (the record and the map are on the sheet).</summary>
            public static string EncTimes(string count) => count + "×";
            public const string EncNotYet = "Não pescada";
            public const string EncBack = "Voltar";
            public const string EncPrevGlyph = "‹";
            public const string EncNextGlyph = "›";
            public const string EncSheet = "Ficha da espécie";
            public const string EncCaught = "Quantas pescou";
            public const string EncLargest = "Maior exemplar";
            public const string EncSizeRange = "Tamanho da espécie";
            public const string EncBite = "Chance de mordida";
            public const string EncStats = "Atributos base";
            public const string EncStatsNote = "Nv. 1 · tamanho médio";
            public const string EncUnknownValue = "?";
            public const string EncEmpty = "Nenhuma espécie nesta aba.";
            public static string EncHint(string map) => "Pesque em " + map + " para descobrir esta espécie.";
            public static string EncLivesIn(string map) => "Vive em " + map;
            public static string EncPosition(int index, int total) => index + " de " + total;
            public static string EncRange(string min, string max) => min + " a " + max;
            public static string EncLargestLine(string size, string category) => size + " · " + category;
            public const string LastBattle = "Última batalha";
            public const string Space = "Espaço";
            public const string Expedition = "Expedição";
            public const string NoExpedition = "Nenhuma em andamento";
            public const string Collection = "Coleção";
            public const string ColumnSpecies = "Espécies";
            public const string ColumnCaught = "Pescados";
            public const string StatCatches = "peixes pescados";
            public const string StatExceptional = "excepcionais";
            public const string StatPerfect = "perfeições";
            public const string StatSales = "Moedas em vendas";
            public static string StatBiggest(string species) => "maior: " + species;
            public static string OfPlayers(int total) => "de " + Format.Number(total) + " jogadores";
            public static string BattleLine(bool won, int climbed) => won ? (climbed > 0 ? "Vitória · subiu " + climbed : "Vitória") : (climbed < 0 ? "Derrota · caiu " + (-climbed) : "Derrota");
            public static string StrengthShort(string strength) => "Força " + strength;
            public static string CardumeFull(int filled, int size, string bonus) => filled + "/" + size + " · bônus de Cardume completo ativo (+" + bonus + ")";
            public static string CardumeCount(int filled, int size) => filled + "/" + size + " peixes no Cardume";
            public static string ChanceBonus(string percent) => "chance +" + percent;
            public static string RodLevel(int level, int max) => "Vara · Nv. " + level + " de " + max;
            public static string BaitLeft(int charges) => charges == 1 ? "Isca · resta 1 tentativa" : "Isca · restam " + Format.Number(charges) + " tentativas";
            public static string VipUntil(string date) => "VIP até " + date;

            public static string Tier(int tier) => tier == 0 ? "Vara inicial" : "Categoria " + tier;
            public static string Discovery(int found, int total) => found + " de " + total + " espécies";

            // The backpack inventory: equipped gear on the left, the rods in a grid of slots.
            public const string EquippedHeader = "EQUIPADO";
            public const string BonusTotals = "Bônus somados";
            public const string NoBaitSlot = "Nenhuma isca";
            public const string NoBaitLine = "Isca · compre na Loja";
            public const string ClickForActions = "Clique para ver as ações";
            public const string GearInShop = "Barcos e iscas se trocam na Loja";
            public static string RodsCount(int count) => "Varas · " + count;
            public static string RodLine(int tier) => tier == 0 ? "Vara inicial" : "Vara · Categoria " + tier;
            public static string BoatLine(string percent) => "Barco · +" + percent + " de sucesso";
            public static string BaitLine(int charges) => charges == 1 ? "Isca · 1 tentativa" : "Isca · " + Format.Number(charges) + " tentativas";
            public static string BoatTier(int tier) => tier == 0 ? "Barco inicial" : "Barco · Categoria " + tier;
            public static string BaitTier(int tier) => "Isca · Categoria " + tier;
            public static string ChargesBadge(int charges) => "×" + Format.Number(charges);
            public static string SuccessSplit(string rod, string boat, string bait) => "Vara +" + rod + " · Barco +" + boat + " · Isca +" + bait;
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

        // ------------------------------------------------------------------ window header: the label of the "Diário de bordo" (A-148, A-170)

        public static class WindowHeader
        {
            /// <summary>The round button on the corner of the sign that opens the explanation of the menu.</summary>
            public const string InfoButton = "i";
            public const string InfoTitle = "Como funciona";
        }

        // ------------------------------------------------------------------ toasts

        public static class Toasts
        {
            public static string Catch(string species, string size, string category) => species + " · " + size + " (" + category + ")";
            public static string NewSpecies(string species) => "Nova espécie descoberta: " + species + "!";
            public static string PersonalRecord(string species, string size) => "Novo recorde de " + species + ": " + size + "!";
            public static string Exceptional(string species) => "Captura Excepcional: " + species + "!";
            public static string Perfect(string species) => "Perfeição: " + species + "! O maior possível, com +5% em todos os atributos.";
            public static string PerfectWithSize(string species, string size) => "Perfeição: " + species + ", " + size + "! O maior possível, com +5% em todos os atributos.";
            public static string LevelUp(int level) => "Você subiu para o Nível " + level + "!";
            public static string TopGenes(string species, int stars) => "Exemplar de " + stars + " estrelas: " + species + "! Genes de elite.";
            public static string PerfectSpecimen(string species) => "Exemplar perfeito: " + species + "! Os 4 genes no máximo.";
            /// <summary>Added to a catch toast when the fish has perfect genes (M24-T52), e.g. " · gene perfeito: Ataque".</summary>
            public static string PerfectGenes(string attributes) => " · gene perfeito: " + attributes;
            public static string Shells(string amount) => "+" + amount + (amount == "1" ? " Concha" : " Conchas");
            public static string Dollars(string amount) => "+" + amount + (amount == "1" ? " Dólar" : " Dólares");
            public static string PassMilestone(int level) => "Passe do Pescador: marco do Nv." + level + " alcançado. Resgate no Passe!";
            public const string FishingStarted = "Pesca iniciada.";
            public const string FishingStopped = "Pesca parada.";
            public const string ConfigReloaded = "Balanceamento recarregado.";
            public const string SaveReset = "Save apagado. Um jogo novo começou.";
        }

        // ------------------------------------------------------------------ celebrations (Art Bible, sections 15–16)

        public static class Celebration
        {
            public const string RareCatch = "Captura rara!";
            public const string LegendaryCatch = "Captura lendária!";
            public const string MythicCatch = "Captura mítica!";

            /// <summary>Title of the rarity celebration: Lendário and Mítico have their own (docs/PROGRESSAO_MAPAS_5_A_10_ADAPTADA.md).</summary>
            public static string RarityCatch(string rarityId)
            {
                switch (rarityId)
                {
                    case "legendary": return LegendaryCatch;
                    case "mythic": return MythicCatch;
                    default: return RareCatch;
                }
            }

            public const string Exceptional = "Tamanho Excepcional!";
            public const string Perfect = "Perfeição!";
            public const string Record = "Novo recorde!";
            public const string NewSpecies = "Nova espécie!";
            public static string TopGenes(int stars) => "Exemplar de " + stars + " estrelas!";
            public const string PerfectSpecimen = "Exemplar perfeito!";
            public static string GenesLine(string species, string quality) => species + " · genes " + quality;
            public static string LevelUp(int level) => "Nível " + level + "!";
            public const string AuctionWon = "Você venceu o leilão!";
            public static string CatchLine(string species, string size) => species + " · " + size;
            public static string RareLine(string species, string rarity, string size) => species + " · " + rarity + " · " + size;
            public static string RecordLine(string species, string before, string after) => species + " · " + before + " → " + after;
            public static string NewSpeciesLine(string species) => species + " entrou na sua Enciclopédia";
            public const string LevelUpLine = "Pescador subiu de nível";
            public static string AuctionLine(string item) => item + " espera em Itens a Retirar";
            public static string Coins(string amount) => "+" + amount;
        }

        // ------------------------------------------------------------------ startup / fatal screens

        public static class Startup
        {
            public const string ConfigErrorTitle = "O jogo não pôde começar: o balanceamento tem problemas";
            public const string ConfigErrorHint = "Corrija os itens abaixo nos arquivos da pasta /config (ou pelo Painel de Desenvolvimento) e aperte Play de novo.";
            public const string UnexpectedErrorTitle = "O jogo encontrou um erro inesperado";
            public const string UnexpectedErrorHint = "Tire um print desta tela e mande junto com o pedido de correção. Os detalhes completos também ficam no Console do Unity.";

            /// <summary>The error line of the startup screen: what failed, in the exception's own words.</summary>
            public static string ErrorLine(string kind, string message) => "Erro: " + message + " (" + kind + ")";

            /// <summary>Where in the code it failed (the first lines of the stack trace).</summary>
            public static string WhereLine(string where) => "Onde: " + where;

            /// <summary>What was running when it failed (opening the game, a frame of the screen, a sync).</summary>
            public static string StageLine(string stage) => "Momento: " + stage;

            public const string StageOpening = "abertura do jogo";
            public const string StageScene = "montagem da cena";
            public const string StageScreen = "desenho da tela";
            public const string StageRunning = "jogo em andamento";
            public static string SaveLine(string path) => "Save: " + path;
            public const string SaveTooNew = "Este save foi criado por uma versão mais nova do jogo. Você está jogando com um save temporário, que não será gravado, para não estragar o original.";
            public const string SaveRecovered = "O save principal estava danificado e foi restaurado a partir do backup.";
            public const string SaveUnrecoverable = "O save estava danificado e não pôde ser recuperado. Um jogo novo começou; os arquivos danificados foram guardados na pasta do save.";

            // The Crew and the Upgrades left the game (M24-T46, A-172): the notice of a save from before, shown once.
            public static string CrewRemovedRefund(string coins) => "A Tripulação e as Melhorias saíram do jogo. Você recebeu de volta " + coins + " Moedas, tudo o que gastou nelas.";
            public const string CrewRemoved = "A Tripulação e as Melhorias saíram do jogo. Você não tinha gastado nada nelas.";
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
                case "Traveling": return "Você está viajando. Espere chegar ao destino.";
                case "AlreadyOnMap": return "Você já está neste mapa.";
                case "MapNotFound": return "Esse mapa não existe.";
                case "MapLocked": return "Você ainda não tem nível para este mapa.";
                case "NotEnoughCoins": return "Moedas insuficientes.";
                case "RodNotForSale": return "As varas não são vendidas: monte com fragmentos.";
                case "RodAlreadyOwned": return "Você já tem esta vara no Inventário.";
                case "BoatNotFound": return "Esse barco não existe.";
                case "BoatNotOwned": return "Monte este barco com fragmentos antes de usá-lo.";
                case "BoatAtMaxLevel": return "Este barco já está no nível máximo.";
                case "BoatHasNoLevels": return "Este barco não tem níveis para melhorar.";
                case "BaitNotFound": return "Essa isca não existe.";
                case "BaitNoCharges": return "Essa isca acabou. Compre mais na Loja.";
                case "NotEnoughShells": return "Conchas insuficientes.";
                case "NotEnoughDollars": return "Dólares insuficientes.";
                case "VipUnavailable": return "O VIP não está disponível agora.";
                case "ArenaItemNotFound": return "Este item não está à venda.";
                case "NotEnoughHonor": return "Honra insuficiente.";
                case "ArenaWeeklyLimit": return "Limite desta semana atingido. Volte em alguns dias.";
                case "InvalidAmount": return "Escolha uma quantidade de pelo menos 1.";
                case "PassMilestoneNotFound": return "Esse marco do Passe não existe.";
                case "PassMilestoneNotReached": return "Você ainda não chegou ao nível deste marco do Passe.";
                case "PassAlreadyClaimed": return "Você já resgatou esta recompensa.";
                case "PassPremiumLocked": return "Esta recompensa é do Premium. Compre o Premium para resgatar.";
                case "PassPremiumOwned": return "Você já tem o Premium do Passe.";
                case "PassNothingToClaim": return "Nada para resgatar agora. Suba de nível para chegar ao próximo marco.";
                case "ClothingNotFound": return "Essa peça de roupa não existe.";
                case "ClothingAlreadyOwned": return "Você já montou esta peça.";
                case "BoatAlreadyOwned": return "Você já tem este barco.";
                case "NotEnoughBoatFragments": return "Faltam fragmentos para montar este barco.";
                case "BoatNeedsPrevious": return "Precisa do barco anterior para montar este (veja qual na Loja → Barcos).";
                case "NotEnoughRodFragments": return "Faltam fragmentos para montar esta vara.";
                case "RodNeedsPrevious": return "Precisa da vara anterior no Inventário para montar esta (veja qual na Loja → Varas).";
                case "NotFromFragments": return "Este item não é montado com fragmentos.";
                case "NotEnoughClothingFragments": return "Faltam Fragmentos de Roupa para montar esta peça.";
                case "ClanLevelTooLow": return "Só Pescadores do Nv.100 para cima podem criar ou entrar num clã.";
                case "AlreadyInClan": return "Você já está num clã. Saia dele antes.";
                case "NotInClan": return "Você não está num clã.";
                case "InvalidClanName": return "Nome de clã inválido: use de 3 a 20 caracteres (letras, números, espaço, ponto, hífen ou sublinhado).";
                case "ClanNameTaken": return "Já existe um clã com esse nome. Escolha outro.";
                case "ClanNotFound": return "Esse clã não existe mais.";
                case "ClanFull": return "Esse clã está cheio: não há vaga livre.";
                case "NotClanOwner": return "Só o dono do clã pode comprar vagas.";
                case "ClanSlotsMaxed": return "O clã já tem o máximo de vagas.";
                case "MissionNotFound": return "Essa missão não existe.";
                case "MissionNotReady": return "Essa missão ainda não foi concluída.";
                case "MissionAlreadyClaimed": return "O prêmio dessa missão já foi resgatado.";
                case "MissionNothingToClaim": return "Nenhuma missão pronta para resgatar.";
                case "RodAtMaxLevel": return "Esta vara já está no nível máximo.";
                case "RodHasNoLevels": return "Esta vara não tem níveis para melhorar.";
                case "RodEquipped": return "Equipe outra vara antes de vender ou destruir esta.";
                case "RodNotSellable": return "O Caniço Manso, a vara inicial, não pode ser vendido nem destruído.";
                case "ExpeditionNotFound": return "Essa Expedição não existe.";
                case "ExpeditionActive": return "Seu Cardume já está numa Expedição.";
                case "ExpeditionNotActive": return "Não há Expedição em andamento para cancelar.";
                case "DevToolsDisabled": return "As ferramentas de teste só funcionam no Editor do Unity.";
                case "InvalidName": return "Use de 3 a 16 caracteres: letras, números, espaço, ponto, hífen ou sublinhado.";
                case "AvatarNotFound": return "Este avatar não existe.";
                case "CardumeEmpty": return "Coloque pelo menos um peixe no Cardume (Perfil → Cardume).";
                case "CardumeLocked": return "Seu Cardume está numa Expedição. Espere ele voltar.";
                case "NotEnoughEnergy": return "Sem Energia. Ela volta 1 ponto por hora.";
                case "OpponentNotFound": return "Esse adversário não está mais na sua lista.";
                case "NoRerollsLeft": return "Você já trocou os adversários. Ataque para receber uma lista nova.";
                case "ListingNotFound": return "Esse anúncio não está mais no Mercado.";
                case "ListingLimitReached": return "Você já tem o máximo de anúncios ativos. Espere vender ou cancele um.";
                case "InvalidPrice": return "Digite um preço válido, em Moedas.";
                case "FishInCardume": return "Tire o peixe do Cardume antes de anunciar.";
                case "RodNotTradable": return "Esta vara não pode ser vendida no Mercado.";
                case "OwnListing": return "Esse anúncio é seu.";
                case "WithdrawalNotFound": return "Esse item não está mais em Itens a Retirar.";
                case "AuctionNotFound": return "Esse leilão não existe mais.";
                case "AuctionLimitReached": return "Você já tem um leilão ativo. Espere ele terminar.";
                case "BidTooLow": return "O lance precisa ser pelo menos o mínimo mostrado.";
                case "AlreadyHighestBidder": return "Você já tem o maior lance neste leilão.";
                case "AuctionHasNoBids": return "Sem lances, o leilão não pode ser encerrado antes: espere as 6 horas.";
                case "AuctionEnded": return "Esse leilão já terminou.";
                case "NoRod": return "Você ainda não tem vara. Pegue o Caniço Manso grátis na Loja.";
                case "TutorialStepMismatch": return "O tutorial já passou desse ponto.";
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
            public static string BoatMigrationInvalid(string what) => "equipment.json: \"boat_migration." + what + "\" está inválido (versão de pelo menos 1; cada barco removido com id que nenhum barco de hoje usa, fragments_go_to de um barco de hoje que não seja o inicial e upgrade_cost válido).";
            public static string BaitMigrationInvalid(string what) => "equipment.json: \"bait_migration." + what + "\" está inválido (versão de pelo menos 1; cada isca antiga com id, bônus entre 0 e 1, pelo menos 1 tentativa e custo não negativo).";
            public static string BadPercentileBand(string category) => "progression.json: a categoria de tamanho \"" + category + "\" precisa de percentis entre 0 e 1, com o mínimo menor que o máximo.";
            public static string WeightsSumToZero(string file, string what) => file + ": os pesos de \"" + what + "\" somam zero — nada poderia ser sorteado.";
            public static string LevelCostInvalid(string file, string item) => file + ": \"" + item + ".upgrade_cost\" precisa de coins_first maior que zero, shells_first de pelo menos 1 (todo nível custa Moedas e Conchas) e crescimentos entre 1 e 10, e o último nível precisa custar menos de 1 quintilhão.";
            public static string LevelBonusInvalid(string file, string item) => file + ": os bônus de \"" + item + "\" não podem ser negativos, o do nível máximo precisa ser maior ou igual ao do Nv.1, e a chance de sucesso fica entre 0 e 1.";
            public static string LegacyLevelsInvalid(string file, string item) => file + ": \"" + item + ".legacy_levels_v1\" precisa ter 10 níveis (os níveis 1 a 10 antigos), em ordem crescente, entre 1 e o nível máximo.";
            public static string LevelCurveInvalid(string file, string what) => file + ": \"" + what + "\" precisa de nível máximo entre 1 e 1.000 e expoente da curva entre 0,1 e 10.";
            public static string FishXpTableGap(int level) => "progression.json: a tabela de XP do peixe (fish_level.xp_table) não tem o nível " + level + ".";
            public static string BadBaseStats(string species) => "fish_catalog.json: a espécie \"" + species + "\" precisa de base_stats com Vida e Velocidade maiores que zero e Ataque e Defesa não negativos.";
            public static string XpTableGap(int level) => "progression.json: a tabela de XP do Pescador não tem o nível " + level + ".";
            public static string StarterBoatHasMap(string boat) => "equipment.json: o barco inicial \"" + boat + "\" (o de menor categoria) é de todos e não pode ter map_id.";
            public static string BoatMapUnknown(string boat, string map) => "equipment.json: o barco \"" + boat + "\" precisa de um map_id que exista em maps.json (está \"" + (map ?? "") + "\").";
            public static string BoatMapRepeated(string map) => "equipment.json: o mapa \"" + map + "\" tem mais de um barco (em map_id ou extra_fragment_maps). Cada mapa é de um barco só (o barco de que o Boss Mundial dá fragmentos).";
            public static string PassLevels(int max) => "pass.json: os marcos (milestones) precisam de níveis diferentes, em ordem crescente, entre 1 e " + max + ".";
            public static string RodMethodUnknown(string rod, string method) => "rods.json: a vara \"" + rod + "\" tem acquisition.method \"" + (method ?? "") + "\". Use \"fragments\" (vara montada com fragmentos) ou \"free_claim_in_shop\" (a Vara Inicial).";
            public static string ClothingInvalid(string what) => "equipment.json: \"clothing." + what + "\" está inválido.";
            public static string ClothingAxisUnknown(string piece, string axis) => "equipment.json: a roupa \"" + piece + "\" tem bonus_axis \"" + (axis ?? "") + "\". Use catch_success, rarity_efficiency, size_quality, shell_yield ou fisher_xp.";
            public static string PassRodUnknown(int level, string rod) => "pass.json: o marco do Nv." + level + " dá fragmentos da vara \"" + (rod ?? "") + "\", que não existe em rods.json (ou é a Vara Inicial).";
            public const string WorldBossSchedule = "world_boss.json: \"schedule\" precisa de utc_offset_minutes entre −720 e 840, start_minute_of_day de 0 a 1439 e duration_minutes de 1 a 240, e o evento precisa terminar no mesmo dia (start_minute_of_day + duration_minutes até 1440).";
            public const string WorldBossAttack = "world_boss.json: \"attack\" precisa de interval_seconds de 1 a 60 (e não maior que a duração do evento), max_catch_up_attacks de 1 a 100 e boss_defense de 0 ou mais.";
            public const string WorldBossBands = "world_boss.json: \"bands\" precisa ter de 1 a 20 faixas, com damage_percent maior que 0 e até 100, em ordem crescente, e nenhum valor negativo.";
            public const string WorldBossRanking = "world_boss.json: \"ranking.dollars_by_rank\" precisa existir, com no máximo 100 valores e nenhum negativo.";
            public const string WorldBossIncome = "world_boss.json: \"reference_income\" precisa começar no level 1, com os levels em ordem crescente e nenhum valor negativo.";
            public const string ClansMembership = "clans.json: \"membership\" precisa de min_fisher_level entre 1 e o nível máximo do Pescador, base_members de 1 a 1.000 e max_members entre base_members e 1.000.";
            public const string ClansCosts = "clans.json: \"costs\" precisa de create_dollars de 0 ou mais, extra_slot_first_dollars de pelo menos 1 e extra_slot_growth entre 1 e 10.";
            public const string ClansName = "clans.json: \"name\" precisa de min_length de pelo menos 1 e max_length entre min_length e 40.";
            public const string ClansXpBonus = "clans.json: \"xp_bonus\" precisa ficar entre 0 e 1 (0,01 = +1%).";
            public static string MissionUnknownType(string id, string type) => "missions.json: a missão \"" + id + "\" tem um tipo que o jogo não conhece (\"" + (type ?? "") + "\").";
            public static string MissionReward(string id) => "missions.json: a missão \"" + id + "\" precisa de um prêmio (reward) com Moedas, Conchas ou Dólares, sem valores negativos.";
            public static string MissionTarget(string id) => "missions.json: a missão \"" + id + "\" precisa de um target de pelo menos 1 (0 só nos tipos que aceitam \"todas\").";
            public static string MissionParameter(string id, string what) => "missions.json: a missão \"" + id + "\" tem \"" + what + "\" faltando ou inválido.";
            public const string ClansRanking = "clans.json: \"ranking\" precisa de utc_offset_minutes entre −720 e 840 e de 1 a 10 recompensas em rewards_by_rank, sem valores negativos.";
            public static string PassBoatUnknown(int level, string boat) => "pass.json: o marco do Nv." + level + " dá fragmentos do barco \"" + (boat ?? "") + "\", que não existe em equipment.json (ou é o barco inicial).";
            public static string UnknownSizeInRarity(string rarity, string size) => "progression.json: a raridade \"" + rarity + "\" cita o tamanho \"" + (size ?? "") + "\" em size_weight_multipliers, que não existe em size.categories.";
            public static string UnknownRarity(string species, string rarity) => "fish_catalog.json: a espécie \"" + species + "\" usa a raridade \"" + (rarity ?? "") + "\", que não existe em progression.json.";
            public static string BadSizeRange(string species) => "fish_catalog.json: a espécie \"" + species + "\" precisa de size_cm com mínimo maior que zero e máximo maior que o mínimo.";
            public static string PoolUnknownSpecies(string map, string species) => "maps.json: o mapa \"" + map + "\" lista a espécie \"" + (species ?? "") + "\", que não existe em fish_catalog.json.";
            public static string PoolRarityNotAvailable(string map, string species, string rarity) => "maps.json: o mapa \"" + map + "\" tem \"" + species + "\" (raridade \"" + rarity + "\"), mas essa raridade não está em available_rarities do mapa.";
            public static string ChanceOutOfRange(string file, string what) => file + ": \"" + what + "\" é uma chance e precisa estar entre 0 e 1.";
            public static string TargetPriority(int size) => "arena.json: \"formation.target_priority\" precisa ter cada posição do Cardume (1 a " + size + ") exatamente uma vez.";
            public static string MapRodCatchesNothing(string map, string rod) => "maps.json / rods.json: com a vara \"" + rod + "\" o mapa \"" + map + "\" não teria nenhum peixe para pescar (nenhuma raridade em comum). Ajuste o pool do mapa ou as raridades da vara.";
            public static string UnknownRodRarity(string rod, string rarity) => "rods.json: a vara \"" + rod + "\" cita a raridade \"" + rarity + "\", que não existe em progression.json.";
            public static string BadIntRange(string file, string what) => file + ": \"" + what + "\" precisa ter mínimo ≥ 0 e máximo ≥ mínimo.";
            public const string GenesInvalid = "progression.json: \"genes\" precisa de max_value de 1 a 255, stat_multiplier_min entre 0 e 1, stat_multiplier_max entre 1 e 10, stat_multiplier_neutral_gene entre 1 e max_value − 1, migration_neutral_value entre 0 e max_value, quality_stars com estrelas 1, 2, 3… em ordem (a primeira em min_percent 0, as seguintes subindo até 100) e celebrate_min_stars entre 0 e a maior estrela.";
            public const string GeneProtectionInvalid = "progression.json / economy.json: \"gene_stars_at_or_above\" (em feeding.valuable_feed_rules e em fishing_box.bulk_sale_protection) precisa ficar entre 0 (desligado) e a maior estrela de genes.quality_stars.";
            public static string CrewRefundInvalid(string what) => "economy.json: \"crew_removal_refund." + what + "\" está inválido (cada linha precisa de id único, base_cost ≥ 0 e cost_growth acima de 1 nos tripulantes; unlock_count e cost_factor ≥ 0 nas Melhorias de tripulante; cost_growth de 1 a 1.000 e max_level de 0 a 1.000 nas Melhorias gerais).";
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
            public const string TabCatalog = "Catálogo";
            public const string CatalogFish = "Peixes";
            public const string CatalogMaps = "Mapas";
            public const string CatalogRods = "Varas";
            public const string CatalogBoats = "Barcos";
            public const string CatalogBaits = "Iscas";
            public const string CatalogExpeditions = "Expedições";
            public const string CatalogSearch = "Buscar peixe";
            public static string CatalogUnlock(int level) => "Libera no Nv. " + level;
            public static string CatalogSpecies(int count) => count + " espécies";
            public static string CatalogRarities(string list) => "Raridades: " + list;
            public static string CatalogCatches(string list) => "Pesca: " + list;
            public static string CatalogExpedition(string multiplier) => "Expedição ×" + multiplier;
            public static string CatalogPrice(string coins, string shells) => shells == "0" ? coins + " Moedas" : coins + " Moedas + " + shells + " Conchas";
            public static string CatalogMaxLevel(int level) => "Até o Nv. " + level;
            public static string CatalogBonus(string percent) => "+" + percent + " de chance de puxar";
            public static string CatalogCharges(int charges) => charges + " tentativas por compra";
            public static string CatalogFragments(int required, string percent) => required + " fragmentos · " + percent + " por peixe · todos os mapas";
            public static string CatalogDuration(string duration) => "Duração: " + duration;
            public static string CatalogStrength(string strength) => "Força recomendada: " + strength;
            public static string CatalogReward(string coins) => "Moedas no Mapa 1: " + coins;
            public static string CatalogFindChance(string percent) => "Acha peixe: " + percent;
            public static string CatalogSize(string min, string max) => "Tamanho: " + min + " a " + max;
            public static string CatalogValue(string coins) => "Valor base: " + coins + " Moedas";
            public static string CatalogXp(string fisher, string feed) => "XP: " + fisher + " · alimento: " + feed;

            public const string Reload = "Recarregar";
            public const string CurrentMilestone = "Marco atual";
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
            public const string SectionRarities = "Raridades";
            public const string SectionSuccess = "Sucesso da pesca";
            public const string SectionXp = "XP do Pescador";
            public const string SectionEconomy = "Economia";
            public const string SectionOthers = "Outros arquivos";
            public const string OthersNote = "Valores da Arena, dos adversários simulados, do Cardume e das Expedições. O jogo usa todos eles; salvar com o jogo rodando aplica na hora.";

            public const string OnlineCycle = "Tempo de pesca online (segundos por tentativa)";
            public const string OfflineCycle = "Tempo de pesca offline (segundos por tentativa)";
            public const string OfflineCap = "Limite de acúmulo offline (horas)";
            public const string OfflineNote = "Com o jogo fechado, o pescador faz 1 tentativa a cada \"tempo de pesca offline\", até o limite de horas.";
            public const string SpeciesName = "Nome";
            public const string SpeciesRarity = "Raridade";
            public const string SpeciesSizeMin = "Tam. mín. (cm)";
            public const string SpeciesSizeMax = "Tam. máx. (cm)";
            public const string SpeciesSale = "Venda (Moedas)";
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

            // Raridades (addendum A-083)
            public const string RarityValuesTitle = "Quanto cada raridade vale a mais";
            public const string RarityValuesNote = "Multiplicadores sobre os valores de cada espécie: 2 = o dobro, 1 = igual ao básico. \"Venda\" é o preço para o comerciante; \"Atributos\", Vida, Ataque, Defesa e Velocidade. \"Na pesca\" multiplica a chance de um peixe daquela raridade morder na pesca (0,35 = 35% da chance do mapa e da vara); os Comuns completam o resto. Não vale para Expedições nem para o Mercado.";
            public const string RarityName = "Raridade";
            public const string RarityFishingBite = "Na pesca";
            public const string RarityStat = "Atributos";
            public const string RarityFisherXp = "XP Pescador";
            public const string RarityFeedXp = "XP alimento";
            public const string RaritySale = "Venda";
            public const string RaritySizeTitle = "Chance de cada tamanho, por raridade";
            public const string RaritySizeNote = "Multiplica o peso de cada tamanho só para os peixes daquela raridade (1 = sem mudança; 0,85 = 15% menos). Ao lado, a chance final de cada tamanho. O bônus de tamanho da vara vem por cima destes números.";
            public static string TimesSize(string size) => "× " + size;
            public const string XpLevel = "Nível";
            public const string XpToNext = "XP para o próximo";
            public const string ShellChance = "Chance de Concha por captura (0 a 1)";
            public const string MinimumPrice = "Preço mínimo de venda (Moedas)";
            public const string SalePriceMultiplier = "Multiplicador do preço de venda dos peixes (1 = valores do catálogo; 0,5 = metade)";

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
            public const string StatsNote = "Atributos-base valem para nível 1 e tamanho mediano. São os números do peixe no Aquário e na Arena.";
            public const string TravelSeconds = "Duração da viagem entre mapas (segundos)";
            public const string UnlockLevel = "Nível do Pescador para liberar";
            public const string StatInfluence = "Influência do tamanho nos atributos";
            public const string FeedInfluence = "Influência do tamanho no XP de alimento";
            public const string XpTotalTo10 = "XP total do nível 1 ao 10";
            public const string FishXpTable = "XP do peixe por nível";
            public const string FisherXpTable = "XP do Pescador por nível";
            public const string AquariumCapacity = "Capacidade do Aquário";
            public const string FishLevelBonus = "Bônus de atributo do peixe no nível máximo (%)";
            public const string LevelCurveMax = "Nível máximo";
            public const string LevelCurveExponent = "Expoente da curva (acima de 1: cada nível soma um pouco mais)";
            public const string RodAtLevel1 = "No Nível 1";
            public const string RodAtMaxLevel = "No nível máximo";
            public const string LevelCostFirst = "1º nível";
            public const string LevelCostGrowth = "× por nível";
            public const string LevelShellsFirst = "Conchas 1º";
            public const string LevelShellsGrowth = "Conchas ×";
            public const string GearBonusAtMax = "Bônus máx.";
            public const string FeedRecovery = "Recuperação do XP investido ao alimentar (0 a 1)";
            public const string RodPrice = "Valor de referência no Mercado (Moedas)";
            public const string RodFragmentsRequired = "Fragmentos para montar";
            public const string RodFragmentChance = "Chance de fragmento por peixe, em qualquer mapa (0,001 = 0,1%)";
            public const string ClothingTitle = "Roupas";
            public const string ClothingChance = "Chance do Fragmento de Roupa por peixe (0,001 = 0,1%)";
            public const string ClothingXpFirst = "XP de uso do Nv.1 para o Nv.2";
            public const string ClothingXpGrowth = "XP de uso: × por nível";
            public const string ClothingNote = "Um tipo só de fragmento, que cai em todos os mapas. O jogador escolhe a peça; a 1ª, a 2ª … custam os valores de clothing.fragments_to_assemble. Cada peixe puxado dá XP de uso a todas as peças montadas. Bônus em pontos (0,05 = +5%): Luvas na chance de puxar, Chapéu na raridade, Botas no tamanho, Camiseta nas Conchas, Calça no XP de Pescador de cada peixe.";
            public const string RodRarityBonus = "Raridade";
            public const string RodSizeBonus = "Tamanho";
            public const string RodShellBonus = "Conchas";
            public const string RodCatchBonus = "Puxar";
            public const string RodUpgradeCost = "Custo de cada nível";
            public const string RodsNote = "Bônus são o total naquele nível (0,10 = +10%); entre o Nível 1 e o máximo o jogo segue a curva (expoente abaixo). Um nível custa \"1º nível\" × \"× por nível\" elevado a (nível − 1), em Moedas e em Conchas. \"Puxar\" soma pontos na Chance de Sucesso da Captura (0,05 = +5%); ele também aparece na aba Sucesso da pesca.";
            public const string RodStarterCatchBonus = "Bônus de puxar da vara inicial, o Caniço Manso (0,05 = +5%)";

            // ---- Sucesso da pesca (docs/SISTEMA_SUCESSO_PESCA.md)
            public const string SuccessIntro = "Depois que o peixe morde, o jogo sorteia se o pescador consegue puxá-lo. Chance = chance-base da raridade + vara + barco + isca, entre o mínimo e o máximo abaixo. Se o peixe escapa, nada entra na Caixa: sem XP, sem Conchas, sem descoberta, sem recorde.";
            public const string SuccessBaseTitle = "Chance-base por raridade";
            public const string SuccessBase = "Chance-base";
            public const string SuccessMin = "Chance mínima (0,05 = 5%)";
            public const string SuccessMax = "Chance máxima (0,95 = 95%)";
            public const string SuccessRodsNote = "O bônus de cada vara por nível fica na aba Varas, na coluna \"Puxar\".";
            public const string BoatsTitle = "Barcos";
            public const string BaitsTitle = "Iscas";
            public const string GearBonus = "Bônus";
            public const string GearCoins = "Moedas";
            public const string GearShells = "Conchas";
            public const string BoatFragments = "Fragmentos";
            public const string BoatFragmentChance = "Chance/peixe";
            public const string GearCharges = "Tentativas";
            public const string GearNote = "Bônus em pontos (0,05 = +5%). O barco de menor categoria é o inicial: todo jogador já tem, não é de nenhum mapa e não tem nível. Os outros barcos não são vendidos: são montados com \"Fragmentos\"; cada peixe puxado em qualquer mapa tem a \"Chance/peixe\" de cada barco de dar 1 fragmento dele (0,001 = 0,1%). O map_id de cada barco só diz de qual barco o Boss Mundial dá fragmentos. Eles sobem de nível como as varas: \"Bônus\" no Nível 1, \"Bônus máx.\" no nível máximo. A isca gasta 1 tentativa por pescaria, puxando o peixe ou não, também offline.";
            public const string SimulatorTitle = "Simulador de sucesso";
            public const string SimulatorNote = "Roda as regras do jogo com os números desta tela, mesmo os que ainda não foram salvos. A isca fica sempre ligada; o custo dela por hora é descontado das Moedas.";
            public const string SimMap = "Mapa";
            public const string SimRod = "Vara";
            public const string SimRodLevel = "Nível da vara";
            public const string SimBoat = "Barco";
            public const string SimBait = "Isca";
            public const string SimNoBait = "Sem isca";
            public const string SimAttempts = "Tentativas";
            public const string SimRun = "Simular";
            public const string SimInvalid = "O balanceamento desta tela tem problemas; corrija antes de simular:";
            public const string SimRodCannotFish = "Esta vara não pesca nenhuma raridade deste mapa.";
            public const string SimResultAttempts = "Tentativas";
            public const string SimResultCatches = "Capturas";
            public const string SimResultEscapes = "Fugas";
            public const string SimResultRate = "Taxa real de sucesso";
            public const string SimPerHour = "Por hora de pesca online";
            public const string SimCatchesHour = "Capturas por hora";
            public const string SimEscapesHour = "Fugas por hora";
            public const string SimXpHour = "XP por hora";
            public const string SimCoinsHour = "Moedas por hora (venda ao jogo)";
            public const string SimBaitHour = "Custo da isca por hora";
            public const string SimNetCoinsHour = "Moedas por hora, descontada a isca";
            public const string SimShellsHour = "Conchas por hora, descontada a isca";
            public const string SimRarity = "Raridade";
            public const string SimBites = "Mordidas";
            public const string SimCaught = "Puxadas";
            public const string SimChance = "Chance";
            public static string SimBaitCost(string coins, string shells) => coins + " Moedas + " + shells + " Conchas";
            public const string ShellMin = "Conchas por captura (mínimo)";
            public const string ShellMax = "Conchas por captura (máximo)";
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
                    new BalanceField("arena.json", "opponent_selection.rank_window_percent_above", "Janela acima da sua posição (%)"),
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
                new BalanceFieldGroup("Arena — Adversários simulados (MVP local)", new[]
                {
                    new BalanceField("arena_bots.json", "bot_count", "Quantidade de adversários"),
                    new BalanceField("arena_bots.json", "strength_by_rank.top_fish_level", "Nível dos peixes do 1º colocado"),
                    new BalanceField("arena_bots.json", "strength_by_rank.bottom_fish_level", "Nível dos peixes do último colocado"),
                    new BalanceField("arena_bots.json", "incoming_attacks.check_interval_minutes", "Minutos entre ataques recebidos (verificação)"),
                    new BalanceField("arena_bots.json", "incoming_attacks.chance_per_check", "Chance de ser atacado por verificação (0 a 1)"),
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
                new BalanceFieldGroup("Mercado — Jogadores simulados (MVP local)", new[]
                {
                    new BalanceField("market_bots.json", "supply.target_listing_count", "Anúncios de outros jogadores à venda"),
                    new BalanceField("market_bots.json", "supply.refresh_interval_minutes", "Minutos entre novos anúncios"),
                    new BalanceField("market_bots.json", "supply.listing_duration_hours", "Duração dos anúncios deles (horas)"),
                    new BalanceField("market_bots.json", "demand.check_interval_minutes", "Minutos entre compradores (verificação)"),
                    new BalanceField("market_bots.json", "demand.chance_at_reference_price", "Chance de vender no preço de referência (0 a 1)"),
                    new BalanceField("market_bots.json", "valuation.fish_reference_ratio", "Referência do peixe (× venda ao jogo)"),
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
                new BalanceFieldGroup("Leilão — Jogadores simulados (MVP local)", new[]
                {
                    new BalanceField("market_bots.json", "auctions.target_auction_count", "Leilões de outros jogadores abertos"),
                    new BalanceField("market_bots.json", "auctions.bid_check_interval_minutes", "Minutos entre lances simulados (verificação)"),
                    new BalanceField("market_bots.json", "auctions.bid_chance_per_check", "Chance de um lance por verificação (0 a 1)"),
                    new BalanceField("market_bots.json", "auctions.max_bid_ratio", "Lance máximo deles (× referência)"),
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

        public static class FriendsBuild
        {
            public const string Menu = "Fishing Idle/Gerar versão para amigos (Windows)";
            public const string Title = "Versão para amigos";
            public const string Ok = "OK";
            public const string NoWindowsSupport = "Falta o módulo \"Windows Build Support\" no Unity. Abra o Unity Hub → Instalações → engrenagem da versão 6000.3 → Adicionar módulos → marque \"Windows Build Support (Mono)\" e instale. Depois tente de novo.";
            public const string Failed = "A versão não foi gerada. Veja o Console do Unity para saber o motivo e me mande um print.";
            public static string Done(string zip, string sizeMb) => "Pronto! O arquivo " + zip + " (" + sizeMb + " MB) está na pasta dist, que acabou de abrir. Mande esse zip para os seus amigos (Google Drive, WeTransfer ou itch.io).";
            public static string Readme(string version) =>
                "FISHING IDLE — versão de teste " + version + "\r\n" +
                "\r\n" +
                "Como jogar\r\n" +
                "1. Extraia este zip numa pasta (botão direito → Extrair tudo).\r\n" +
                "2. Abra FishingIdle.exe.\r\n" +
                "3. Se o Windows mostrar \"O Windows protegeu o computador\", clique em \"Mais informações\" e depois em \"Executar assim mesmo\". Isso aparece porque o jogo ainda não tem assinatura digital; ele não instala nada.\r\n" +
                "\r\n" +
                "Bom saber\r\n" +
                "- O jogo pesca sozinho, até com ele fechado (por até 24 horas).\r\n" +
                "- O progresso fica salvo só neste computador.\r\n" +
                "- Esta é uma versão de teste: Ranking e comércio de Conchas e Dólares ainda não são online, e algumas artes são provisórias.\r\n" +
                "- Para jogar uma versão nova, extraia por cima ou numa pasta nova: o progresso continua.\r\n" +
                "\r\n" +
                "Achou um problema ou tem uma ideia? Mande um print e conte o que aconteceu para quem te enviou o jogo.\r\n";
        }

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
