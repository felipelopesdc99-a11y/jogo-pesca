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

            public static string NextCatchIn(string countdown) => "Próxima fisgada em " + countdown;
            public static string CycleInfo(string duration) => "1 tentativa a cada " + duration;
            public static string ChanceShort(string rarity, string percent) => rarity + " " + percent;
            public static string RareEscaped(string rarity, string percent) => "Um peixe " + rarity + " escapou! Sua chance de puxar era " + percent + ".";
            public static string BaitRanOut(string bait) => "Sua " + bait + " acabou. Compre mais na Loja → Iscas.";
            public static string BaitLeft(int charges) => charges == 1 ? "1 tentativa" : Format.Number(charges) + " tentativas";
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

            // Filters: rarity and size are separate groups that combine (addendum A-079).
            public const string RarityLabel = "Raridade";
            public const string SizeLabel = "Tamanho";
            public const string AllRarities = "Todas";
            public const string AllSizes = "Todos";
            public const string SortLabel = "Ordenar:";
            public const string SortNewest = "Mais recentes";
            public const string SortPrice = "Mais caros";

            public static string Count(int count) => count == 1 ? "1 peixe" : Format.Number(count) + " peixes";
            public static string Selected(int count, string coins) => (count == 1 ? "1 selecionado" : Format.Number(count) + " selecionados") + " · " + coins + " moedas";
            public static string Sold(int count, string coins) => (count == 1 ? "1 peixe vendido" : Format.Number(count) + " peixes vendidos") + " por " + coins + " moedas.";
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
            public const string ColumnPosition = "Posição";
            public const string ColumnPlayer = "Jogador";
            public const string You = "Você";
            public const string LocalNote = "Por enquanto o jogo roda só neste computador, então o ranking mostra só você. Quando o jogo for online, aqui aparecem todos os jogadores reais.";
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
            public static string LeavesCardume(int position) => "Este peixe está no Cardume (posição " + position + ") e sairá dele.";
        }

        // ------------------------------------------------------------------ offline return (GDD section 10)

        public static class Offline
        {
            public const string Title = "Bem-vindo de volta!";
            public const string Continue = "Continuar";
            public const string OpenBox = "Abrir a Caixa de Pesca";
            public const string Best = "Destaques";
            public const string NothingCaught = "Nenhum peixe desta vez.";
            public static string Note(string cycle, string cap) => "Enquanto o jogo fica fechado, o pescador continua: 1 tentativa a cada " + cycle + ", por até " + cap + ", com a mesma chance de puxar. Os peixes esperam na Caixa de Pesca.";

            public static string Away(string duration) => "Você ficou fora por " + duration + ".";
            public static string CappedAt(string duration) => "Só as primeiras " + duration + " contam para a pesca offline.";
            public static string Caught(int count) => count == 1 ? "1 peixe pescado" : Format.Number(count) + " peixes pescados";
            public static string Xp(string xp) => "+" + xp + " XP de Pescador";
            public static string Levels(int level) => "Subiu para o Nível " + level + "!";
            public static string NewSpecies(int count) => count == 1 ? "1 espécie nova" : count + " espécies novas";
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
            public const string ShopEmpty = "A Loja da Arena ainda não tem itens. Eles são cadastrados em arena.json → shop.items quando o proprietário decidir o que vender por Honra.";
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
            public static string RankOfTotal(int rank, int total) => "#" + rank + " de " + total;
            public static string EnergyOf(int energy, int max) => energy + " / " + max;
            public static string NextEnergy(string countdown) => "+1 em " + countdown;
            public static string RerollsLeft(int left) => Reroll + " (" + left + ")";
            public static string RankChange(int before, int after) => before == after ? "Posição mantida: #" + after : "Posição: #" + before + " → #" + after;
            public static string HonorChange(long change) => (change >= 0 ? "+" : "−") + Format.Number(System.Math.Abs(change)) + " de Honra";
            public static string DefenseWon(string attacker, string honor) => attacker + " atacou você e perdeu. +" + honor + " de Honra.";
            public static string DefenseLost(string attacker, int rank) => attacker + " venceu você na Arena. Sua posição agora é #" + rank + ".";
            public static string Versus(string opponent) => "Você × " + opponent;
            public static string Clock(string time) => "Tempo: " + time;
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
                    case "claim_rod": return "Abra a " + Navigation.Shop + " (menu de cima) e pegue o Caniço Manso, a vara inicial. Ela é grátis.";
                    case "start_fishing": return "Clique em \"" + Fishing.Start + "\", embaixo, no centro.";
                    case "first_catch": return "A cada ciclo o pescador tira um peixe da água. Quando ele aparecer, veja no aviso a espécie, o tamanho e a categoria.";
                    case "open_box": return "Todo peixe pescado vai para a " + Box.Open + " (canto de baixo, à direita). Abra para ver os peixes.";
                    case "sell_fish": return "Na Caixa, selecione um peixe e clique em \"" + Box.SellSelected + "\". As Moedas compram varas melhores.";
                    case "keep_fish": return "Selecione outro peixe e clique em \"" + Aquarium.KeepSelected + "\". Peixes guardados sobem de nível e lutam por você.";
                    case "cardume": return "Abra o " + Navigation.Profile + " → " + Profile.TabCardume + " e coloque um peixe numa posição. O Cardume luta na Arena e vai em Expedições.";
                    case "expedition": return "No menu " + Navigation.Expedition + ", você envia o Cardume por 30 min a 6 h para trazer Moedas e, às vezes, um peixe. Enquanto isso, ele fica ocupado.";
                    default: return string.Empty;
                }
            }
        }

        // ------------------------------------------------------------------ Notifications, settings and compact mode

        public static class Hud
        {
            public const string Notifications = "Notificações";
            public const string NoNotifications = "Nada por aqui ainda.";
            public const string ClearNotifications = "Limpar";
            public const string Settings = "Configurações";
            public const string Sound = "Som";
            public const string Ambient = "Som ambiente";
            public const string Volume = "Volume";
            public const string Zoom = "Aproximar";
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
            public const string Note = "Compre e venda peixes e varas. Tudo o que sai do Mercado — compras, anúncios cancelados ou vencidos — espera em Itens a Retirar. No jogo local, os outros jogadores são simulados.";
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
            public static string CurrencyListed(string amount, string currency, string price) => amount + " " + currency + " anunciadas por " + price + " Moedas.";
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
            public const string SelectHint = "Clique num card para ver os detalhes.";

            public const string Seller = "Vendedor";
            public const string EndsLabel = "Termina em";
            public const string Price = "Preço";
            public const string Buy = "Comprar";
            public const string BuyNote = "A compra vai para Itens a Retirar. Mesmo com o Aquário cheio você pode comprar; só a retirada espera uma vaga.";

            public const string SellHint = "Escolha um peixe do Aquário ou uma vara do Inventário para anunciar.";
            public const string NothingToSell = "Você não tem peixes no Aquário nem varas para anunciar.";
            public const string NpcValue = "O jogo (NPC) paga";
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

            public static string Bought(string name) => name + " comprado. Está em Itens a Retirar.";
            public static string Listed(string name, string price) => name + " anunciado por " + price + " Moedas.";
            public static string Cancelled(string name) => "Anúncio cancelado. " + name + " está em Itens a Retirar.";
            public static string Sold(string buyer, string name, string net) => buyer + " comprou " + name + ". +" + net + " Moedas (já sem a taxa).";
            public static string Expired(string name) => "O anúncio de " + name + " venceu. Ele está em Itens a Retirar.";
            public static string Withdrawn(string name) => name + " retirado.";
            public static string WithdrewAll(int count, int left) => left > 0
                ? count + " retirado(s). " + left + " peixe(s) ficaram porque o Aquário está cheio."
                : count + " item(ns) retirado(s).";
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
            public const string ReportNote = "Tudo já está com você: as moedas no saldo e o peixe na Caixa de Pesca.";
            public const string NoCardume = "Monte seu Cardume no Perfil para poder enviar Expedições.";

            public static string Departed(string name) => "Cardume enviado: " + name + ".";
            public static string Duration(string duration) => "Duração: " + duration;
            public static string ReturnsIn(string countdown) => "Volta em " + countdown;
            public static string Coins(string coins) => "+" + coins + " moedas";
            public static string ReturnedAt(string name, string when) => name + " · voltou em " + when;
            public static string InBox(string species, string size) => species + " · " + size + " — já está na Caixa de Pesca.";
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
            public static string Traveling(string map) => "Viajando para " + map + "…";
            public static string ArrivesIn(string countdown) => "Chegada em " + countdown;
            public static string TravelTime(string duration) => "Viagem: " + duration;
            public static string Discovered(int found, int total) => found + " de " + total + " descobertas";
            public static string LevelRequirement(int level) => "Nível " + level;
            public static string Chapter(int number) => "CAPÍTULO " + number;

            /// <summary>The one-line feeling of each map on arrival (docs/IDENTIDADE_POR_MAPA.md).</summary>
            public static string Feeling(string mapId)
            {
                switch (mapId)
                {
                    case "map_01": return "Um começo tranquilo e bonito.";
                    case "map_02": return "A aventura sai do conforto do lago.";
                    case "map_03": return "Agora o mundo se abriu de verdade.";
                    case "map_04": return "Cheguei em uma nova fronteira do jogo.";
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
            public static string UpgradeForWithShells(int level, string coins, string shells) => "Nv. " + level + ": " + coins + " + " + shells + " Conchas";
            public static string SellFor(string coins) => "Vender (" + coins + ")";
            public static string MaxLevelOf(int max) => "Até o Nível " + max;
            public static string SellTitle(string rod) => "Vender " + rod + "?";
            public static string SellBody(string coins) => "O jogo paga " + coins + " moedas por ela. A vara sai do seu Inventário.";
            public static string DestroyTitle(string rod) => "Destruir " + rod + "?";
            public const string DestroyBody = "A vara some do Inventário e você não recebe nada. Esta ação não pode ser desfeita.";
            public const string CatchBonus = "Sucesso da captura";
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
            public const string ChanceTitle = "Chance de puxar o peixe";
            public static string ChanceNote(string min, string max) => "Depois que o peixe morde, esta é a chance de puxá-lo. Se ele escapa, nada entra na Caixa. Nunca passa de " + max + " nem fica abaixo de " + min + ".";
            public const string NotHere = "não morde aqui";
            public const string InUse = "Em uso";
            public const string Use = "Usar";
            public const string PutAway = "Guardar";
            public const string BoatsNote = "O barco fica com você para sempre e aumenta a chance de puxar todo peixe. Troque quando quiser.";
            public const string BaitsNote = "A isca dura um número de tentativas: gasta 1 por tentativa, puxando o peixe ou não, também offline. Comprar de novo soma tentativas. Só uma isca fica em uso.";
            public const string OpenDetails = "Ver equipamento e chances";

            public static string ChanceLine(string rarity, string percent) => "Chance de puxar peixe " + rarity + ": " + percent;
            public static string Bonus(string percent) => "+" + percent + " de chance";
            public static string Charges(int charges) => charges == 1 ? "1 tentativa por compra" : Format.Number(charges) + " tentativas por compra";
            public static string ChargesLeft(int charges) => charges == 1 ? "Resta 1 tentativa" : "Restam " + Format.Number(charges) + " tentativas";
            public static string CostCoinsAndShells(string coins, string shells) => coins + " moedas + " + shells + " conchas";
            public static string BoughtBoat(string boat) => boat + " comprado! Ele já está em uso.";
            public static string UsingBoat(string boat) => "Agora você pesca com o " + boat + ".";
            public static string BoughtBait(string bait, int charges) => bait + ": +" + charges + " tentativas.";
            public static string UsingBait(string bait) => "Isca em uso: " + bait + ".";
            public const string BaitPutAway = "Isca guardada. As tentativas que sobraram ficam para depois.";
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
            public const string CatchesRareAndEpic = "Pesca peixes Raros e Épicos";
            public const string CatchesUpToLegendary = "Pesca peixes Raros, Épicos e Lendários";
            public const string CatchesUpToMythic = "Pesca peixes Raros, Épicos, Lendários e Míticos";
            public const string NoRare = "Não pesca peixes Raros";
            public const string NoShells = "Não gera Conchas";
            public const string InventoryNote = "Suas varas ficam aqui. Varas novas se compram na Loja.";
            public const string Undiscovered = "???";
            public const string Largest = "Maior";
            public const string TimesCaught = "Pescados";
            public const string FirstCaught = "Descoberto em";
            public const string TotalCatches = "Capturas";
            public const string Discovered = "Espécies descobertas";
            public const string Biggest = "Maior peixe já pescado";
            public const string HighestLevel = "Peixe de nível mais alto";
            public const string Exceptional = "Capturas Excepcionais";
            public const string Perfect = "Capturas Perfeição";
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
            public static string Perfect(string species) => "Perfeição: " + species + "! O maior possível, com +5% em todos os atributos.";
            public static string LevelUp(int level) => "Você subiu para o Nível " + level + "!";
            public static string Shells(string amount) => "+" + amount + " Conchas";
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
                case "BoatNotFound": return "Esse barco não existe.";
                case "BoatLocked": return "Você ainda não tem nível para comprar este barco.";
                case "BoatAlreadyOwned": return "Você já tem este barco.";
                case "BoatNotOwned": return "Compre este barco antes de usá-lo.";
                case "BaitNotFound": return "Essa isca não existe.";
                case "BaitLocked": return "Você ainda não tem nível para comprar esta isca.";
                case "BaitNoCharges": return "Essa isca acabou. Compre mais na Loja.";
                case "NotEnoughShells": return "Conchas insuficientes.";
                case "NotEnoughDollars": return "Dólares insuficientes.";
                case "InvalidAmount": return "Escolha uma quantidade de pelo menos 1.";
                case "RodAtMaxLevel": return "Esta vara já está no nível máximo.";
                case "RodHasNoLevels": return "Esta vara não tem níveis para melhorar.";
                case "RodEquipped": return "Equipe outra vara antes de vender ou destruir esta.";
                case "RodNotSellable": return "O Caniço Manso, a vara inicial, não pode ser vendido nem destruído.";
                case "ExpeditionNotFound": return "Essa Expedição não existe.";
                case "ExpeditionActive": return "Seu Cardume já está numa Expedição.";
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
            public static string BadPercentileBand(string category) => "progression.json: a categoria de tamanho \"" + category + "\" precisa de percentis entre 0 e 1, com o mínimo menor que o máximo.";
            public static string WeightsSumToZero(string file, string what) => file + ": os pesos de \"" + what + "\" somam zero — nada poderia ser sorteado.";
            public static string RodUpgradeCostMissing(string rod, int level) => "rods.json: a vara \"" + rod + "\" não tem custo de melhoria para o nível " + level + " em upgrade_costs.";
            public static string FishXpTableGap(int level) => "progression.json: a tabela de XP do peixe (fish_level.xp_table) não tem o nível " + level + ".";
            public static string BadBaseStats(string species) => "fish_catalog.json: a espécie \"" + species + "\" precisa de base_stats com Vida e Velocidade maiores que zero e Ataque e Defesa não negativos.";
            public static string XpTableGap(int level) => "progression.json: a tabela de XP do Pescador não tem o nível " + level + ".";
            public static string StarterBoatNotFree(string boat) => "equipment.json: o barco inicial \"" + boat + "\" (o de menor tier) precisa custar 0 moedas e 0 conchas.";
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

            // Raridades (addendum A-083)
            public const string RarityValuesTitle = "Quanto cada raridade vale a mais";
            public const string RarityValuesNote = "Multiplicadores sobre os valores de cada espécie: 2 = o dobro, 1 = igual ao básico. \"Venda\" é o preço para o comerciante; \"Atributos\", Vida, Ataque, Defesa e Velocidade.";
            public const string RarityName = "Raridade";
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
            public const string MinimumPrice = "Preço mínimo de venda (moedas)";
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
            public const string FishLevelBonus = "Bônus de atributo por nível do peixe (%)";
            public const string FeedRecovery = "Recuperação do XP investido ao alimentar (0 a 1)";
            public const string RodPrice = "Preço de compra (moedas)";
            public const string RodRarityBonus = "Raridade";
            public const string RodSizeBonus = "Tamanho";
            public const string RodShellBonus = "Conchas";
            public const string RodCatchBonus = "Puxar";
            public const string RodUpgradeCost = "Custo p/ este nível";
            public const string RodsNote = "Bônus são o total naquele nível (0,10 = +10%). \"Puxar\" soma pontos na Chance de Sucesso da Captura (0,05 = +5%); ele também aparece na aba Sucesso da pesca.";
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
            public const string GearLevel = "Nível";
            public const string GearCharges = "Tentativas";
            public const string GearNote = "Bônus em pontos (0,05 = +5%). O barco de menor tier é o inicial: todo jogador já tem e ele precisa custar 0. A isca gasta 1 tentativa por pescaria, puxando o peixe ou não, também offline.";
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
            public const string SimRodCannotFish = "Esta vara não pesca neste mapa (tier mínimo do mapa ou raridades).";
            public const string SimResultAttempts = "Tentativas";
            public const string SimResultCatches = "Capturas";
            public const string SimResultEscapes = "Escapes";
            public const string SimResultRate = "Taxa real de sucesso";
            public const string SimPerHour = "Por hora de pesca online";
            public const string SimCatchesHour = "Capturas por hora";
            public const string SimEscapesHour = "Escapes por hora";
            public const string SimXpHour = "XP por hora";
            public const string SimCoinsHour = "Moedas por hora (venda ao NPC)";
            public const string SimBaitHour = "Custo da isca por hora";
            public const string SimNetCoinsHour = "Moedas por hora, descontada a isca";
            public const string SimShellsHour = "Conchas por hora, descontada a isca";
            public const string SimRarity = "Raridade";
            public const string SimBites = "Mordidas";
            public const string SimCaught = "Puxadas";
            public const string SimChance = "Chance";
            public static string SimBaitCost(string coins, string shells) => coins + " moedas + " + shells + " conchas";
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
                    new BalanceField("market_bots.json", "valuation.fish_reference_ratio", "Referência do peixe (× venda ao NPC)"),
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
