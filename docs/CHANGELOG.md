# Histórico de mudanças

As versões seguem `docs/VERSIONAMENTO.md`. As versões de cada componente ficam em `version.json` na
raiz do repositório, que o servidor serve em `GET /api/dev/version`.

## [0.2.0-m16.28] — 01/10/2026

### Adicionado

- O barco, a vara e a isca equipados aparecem na cena de pesca (GDD_ADENDO A-096): o pescador senta
  dentro do Barco 1 a 5 com a pintura da Loja, segura a pintura da vara em uso e, com isca, ela fica
  pendurada abaixo da boia. Trocar na Loja muda na hora.
- `tools/Arte/equipamento_na_cena.py`: recorta a lateral da frente de cada barco, mede as varas e
  monta a prévia.

### Pendente

- Pintura das iscas (`M16-T12`, `ASSET_PENDENTE`); por enquanto, uma forma colorida.

### Não verificado

- Não aberto no Editor do Unity ainda.

## [0.2.0-m16.27] — 01/10/2026

### Mudado

- Bônus de chance de puxar um terço menores (GDD_ADENDO A-095): barcos +2% a +10%, iscas +3% / +7%
  / +10%, Vara 1 +1,5% a +8%, Vara 2 +5,5% a +13,5%. Simulação: Nível 20→30 em 4,0 h (antes 3,7 h),
  30→40 em 4,5 h (antes 4,0 h).

## [0.2.0-m16.26] — 01/10/2026

### Corrigido

- Preço cortado nos cards (Caixa de Pesca, Aquário, Mercado): o último dígito sumia ("10.44" em vez
  de "10.440"). O rótulo das moedas nunca fica mais estreito que o número.
- Mesmo problema evitado no saldo do Mercado e nas tentativas de isca do painel de pesca.
- Números grandes com ponto de milhar também em contagens: peixes na Caixa, selecionados, vendidos,
  pescados e escapados offline, tentativas de isca e Honra.
- Textos que repetiam números da configuração agora leem o valor real: a nota da pesca offline
  (tempo por tentativa e limite de horas), as notas do Leilão (duração, aumento mínimo, taxa e tempo
  do último lance) e a nota de chance da Loja (mínimo e máximo).
- Textos antigos que falavam de milestones já entregues: inventário do Perfil ("Novas varas chegam
  com a Loja") e duas notas do Painel de Balanceamento.
- Nota do Leilão ganhou altura para não cortar a última linha ao lado da busca.

### Não verificado

- Não aberto no Editor do Unity ainda.

## [0.2.0-m16.25] — 01/10/2026

### Corrigido

- Painel de pesca: o botão "Parar pesca" cobria a barra que conta o tempo até a próxima fisgada.
  O painel ficou um pouco mais alto (para cima), e o botão continua no mesmo lugar. Visto pelo
  proprietário no Play.

## [0.2.0-m16.24] — 01/10/2026

### Mudado

- Mercado: o campo "Buscar peixe pelo nome" fica no alto, à direita, em Comprar, Vender e Leilão (e
  na escolha do peixe para criar um leilão). Antes, em Comprar, ele ficava escondido na coluna de
  filtros, e o Leilão não tinha busca. Trocar de aba ou reabrir o Mercado limpa a busca.
- O campo de busca (Caixa de Pesca, Aquário e Mercado) ficou mais visível: fundo claro, borda
  turquesa e lupa colorida.

### Não verificado

- Não aberto no Editor do Unity ainda.

## [0.2.0-m16.23] — 01/10/2026

### Corrigido

- Janela do Mapa: com quatro mapas, os cards de baixo saíam da janela. Agora a lista rola dentro dela
  (roda do mouse ou barra) e abre já mostrando o mapa onde você está. Visto pelo proprietário no Play.

## [0.2.0-m16.22] — 01/10/2026

### Mudado

- O fundo do Pantanal e do Estuário alterna duas faixas pintadas novas, mais baixas e variadas, em vez
  de repetir a mesma faixa espelhada.
- Margem direita do Pantanal com a arte refeita (antes era a esquerda espelhada).
- Ícones de barco e isca finais, no estilo do menu.

### Adicionado

- Borboletas amarelas no Pantanal.

### Não verificado

- Não aberto no Editor do Unity ainda (`M16-T06`).

## [0.2.0-m16.21] — 01/10/2026

### Adicionado

- **Arte final dos mapas 3 e 4**: cenários do Pantanal Dourado e do Estuário das Marés, os 20 peixes,
  a Vara 2 e os seis barcos da Loja.
- **Paisagem viva nova**: araras-azuis, tuiuiú (voando e pousando na beira), colhereiros, guarás,
  trinta-réis, caranguejo chama-maré e boto-cinza; carandaás, aguapés e plantas de mangue.
- `tools/Arte/processar_mapas_3_4.py` para colocar as imagens no jogo.

### Pendente

- Refazer Pantanal 06 (veio sem fundo chapado) e Pantanal 16 (arquivo vazio); Novo 03 (ícones) não
  veio (`M16-T07`).

### Não verificado

- Não aberto no Editor do Unity ainda (`M16-T06`).

## [0.2.0-m16.20] — 30/09/2026

### Adicionado

- **Pantanal Dourado** (nível 20, Vara 1) e **Estuário das Marés** (nível 30, Vara 2), com 10 espécies
  cada (`docs/PROGRESSAO_MAPAS_3_4.md`). Enciclopédia com 40 espécies.
- **Raridade Épico** (roxo): Barbado no Pantanal; Camurupim e Mero no Estuário. A Vara 1 passa a pescar
  Épico onde o mapa tem Épico.
- **Vara 2**: 90.000 moedas no nível 30, Nv.1 a 10.
- Cenários, peixes, Vara 2 e miniaturas provisórios, pintados por script; paisagem viva com os
  animais que já existem.
- Pedidos de arte dos mapas 3 e 4 atualizados com as espécies novas, os seis barcos e os ícones de barco
  e isca.

### Balanceamento

- Nível 20→30 em 3,7 h online (meta 3,8 h) e 30→40 em 4,0 h (meta 4,0 h), com a Chance de Sucesso.
  Os mapas 3 e 4 têm valores ×1,75 e ×1,55 sobre o documento (A-093).

### Não verificado

- Não aberto no Editor do Unity ainda (`M16-T06`).

## [0.2.0-m15.19] — 30/09/2026

### Adicionado

- **Sucesso da Captura** (`docs/SISTEMA_SUCESSO_PESCA.md`): depois que o peixe morde, o jogo sorteia
  se o pescador consegue puxá-lo. Chance = chance-base da raridade (Comum 50%, Raro 38%) + vara +
  barco + isca, entre 5% e 95%. Peixe que escapa não dá nada. Offline com a mesma regra.
- **Escape na tela:** a vara luta, a boia afunda, uma sombra aparece na água, a linha afrouxa com um
  respingo e sobe "Você ainda não é bom o suficiente." por cima da boia. Peixe Raro luta mais e deixa
  um aviso na lista.
- **Loja → Barcos** (Inicial e 1 a 5, de +3% a +15%, com Moedas e, nos maiores, Conchas) e
  **Loja → Iscas** (Simples, Melhorada e Premium, de +5% a +15%, 100 tentativas por compra).
- **Seu equipamento** na Loja, com a chance de puxar cada raridade; a mesma chance no painel de pesca.
- **Painel de Desenvolvimento → Balanceamento → Sucesso da pesca:** chance-base, mínimo e máximo,
  barcos, iscas e um simulador (mapa + vara + barco + isca + 10.000 tentativas). Varas ganhou a
  coluna "Puxar".
- `config/equipment.json` (barcos e iscas). Save versão 10, compatível com os anteriores.

### Mudado

- **Rebalanceamento:** XP, valor de venda e XP como alimento das espécies dobrados; Conchas por peixe
  puxado de 5% para 10%; Aruanã com peso 10 (era 5). Antes e depois, medidos no simulador:

  | Medida | Antes | Agora |
  |---|---:|---:|
  | Nível 10 | 2,1 h | 2,1 h |
  | Nível 20 | 4,6 h | 4,4 h |
  | Nível 30 | 10,1 h | 8,8 h |
  | Moedas por hora, Lago Sereno | 3.375 | 3.298 |
  | Moedas por hora, Rio Selvagem | 27.182 | 37.481 |
  | Conchas por hora, Rio Selvagem | 5,6 | 7,6 |
  | Primeiro peixe Raro | 3,8 h | 3,9 h |

- Os textos da pesca falam em "tentativa" e "fisgada" ("1 tentativa a cada 30 s").
- Conchas agora têm onde ser gastas (barcos e Isca Premium).

### Pendente

- Arte dos barcos (`M15-T07`, `ASSET_PENDENTE`); som ao escapar (`OD-020`); chance do Épico (24%)
  quando a raridade existir (`OD-019`).

### Não verificado

- Não aberto no Editor do Unity ainda (`M15-T09`).

## [0.2.0-m14.18] — 30/09/2026

### Adicionado

- **Caixa de Pesca → Ordenar:** Mais recentes ou Mais caros.

## [0.2.0-m14.17] — 30/09/2026

### Adicionado

- **Tamanho Perfeição**, acima do Excepcional: 1 a cada 100 excepcionais, o maior tamanho possível
  e +5% em todos os atributos do peixe. Selo azul-diamante, aviso, celebração e filtro próprio.

### Não verificado

- Não aberto no Editor do Unity ainda (`M14-T15`).

## [0.2.0-m14.16] — 30/09/2026

### Adicionado

- **Cada mapa é um capítulo:** ao chegar, o nome do mapa aparece por cima da cena como título de
  capítulo ("CAPÍTULO 2 · Rio Selvagem" e uma frase curta) e some sozinho. O menu do Mapa mostra o
  capítulo e a frase em cada card.
- `docs/IDENTIDADE_POR_MAPA.md`: o documento de identidade por mapa do proprietário.

### Mudado

- O aviso "Você chegou a…" saiu; o título de capítulo faz esse papel.

### Pendente

- Mapas 3 e 4 (Pantanal Dourado e Estuário das Marés): decisão `OD-018`.

## [0.2.0-m14.15] — 30/09/2026

### Adicionado

- **Painel de Balanceamento → Raridades:** quanto cada raridade vale a mais (venda, atributos, XP) e
  a chance de cada tamanho por raridade, com as porcentagens finais na hora.

### Não verificado

- Não aberto no Editor do Unity ainda (`M14-T15`).

## [0.2.0-m14.14] — 30/09/2026

### Adicionado

- **Busca pelo nome do peixe** na Caixa de Pesca, no Aquário e no Mercado (ignora maiúsculas e
  acentos).

### Não verificado

- Não aberto no Editor do Unity ainda (`M14-T15`).

## [0.2.0-m14.13] — 30/09/2026

### Mudado

- **Tamanho por raridade:** peixes Raros vêm Grandes ou Excepcionais um pouco menos que os Comuns
  (Grande 16,7% em vez de 19%; Excepcional 0,72% em vez de 1%). Ajustável em `config/progression.json`.

## [0.2.0-m14.12] — 30/09/2026

### Corrigido

- As cores de tamanho (Pequeno verde, Adulto lilás, Grande laranja, Excepcional dourado) agora
  aparecem também no Aquário, no Mercado e no Perfil.

## [0.2.0-m14.11] — 30/09/2026

### Mudado

- **Relatório da Expedição:** quando o Cardume volta, aparece um aviso e um ponto vermelho no botão
  Expedição; ao abrir a aba, um relatório mostra o que ele trouxe (moedas, peixe, quando voltou) e
  some depois de lido. Antes, o resultado aparecia por cima da tela, onde o jogador estivesse.

### Não verificado

- Não aberto no Editor do Unity ainda (`M14-T15`).

## [0.2.0-m14.10] — 30/09/2026

### Adicionado

- **Ambiente em lista de gravações longas:** cada mapa toca gravações de 2 a 3 minutos em ordem
  sorteada, com passagem suave entre elas, para o som só se repetir depois de uns 10 minutos. As
  gravações ainda não chegaram (`M13-T07`); até lá, fica o som antigo.
- `tools/Audio/processar_ambiente.py` prepara as gravações para o jogo.

### Não verificado

- Não ouvido no Editor do Unity ainda.

## [0.2.0-m14.9] — 30/09/2026

### Adicionado

- **Paisagem viva** (imagens Vivo 01 a 18 do proprietário): árvores, palmeiras, juncos e folhagens
  que vergam com rajadas de vento; vitórias-régias boiando; e animais que aparecem de vez em quando:
  patos, garças, andorinhas, martim-pescador, capivaras, tartaruga e sapo no Lago Sereno; araras,
  tucano, jacaré e macacos no Rio Selvagem.
- **Diretor de cenário:** um animal por vez, com descanso sorteado entre eles, nunca o mesmo duas
  vezes seguidas e nada novo com uma janela aberta ou durante uma celebração. Libélulas, sombras e
  saltos de peixe seguem o mesmo ritmo.
- `Resources/Visual/paisagem_viva.json` (onde fica cada um, com que frequência aparece, vento) e
  `tools/Arte/processar_vivos.py` (recorta as imagens do ChatGPT).

### Não verificado

- Não aberto no Editor do Unity ainda (`M14-T15`).

## [0.2.0-m14.8] — 29/09/2026

### Mudado

- **Menos som:** os avisos só fazem som ao subir de nível, numa captura Excepcional e numa espécie
  nova. Os outros avisos continuam na tela, em silêncio. O som ambiente não mudou.

### Não verificado

- Não ouvido no Editor do Unity ainda (`M13-T04`).

## [0.2.0-m14.7] — 29/09/2026

### Mudado

- O selo **EXCEPCIONAL** do card não cobre mais o peixe: fica no canto direito da linha do tamanho,
  onde os outros tamanhos mostram o nome colorido.

### Não verificado

- Não aberto no Editor do Unity ainda (`M14-T15`).

## [0.2.0-m14.6] — 29/09/2026

### Mudado

- **Caixa de Pesca com dois filtros:** Raridade (Todas, Comum, Raro) e Tamanho (Todos, Pequeno,
  Adulto, Grande, Excepcional), que se combinam.
- **Cores:** Comum branco, Raro azul; Pequeno verde, Adulto lilás, Grande laranja, Excepcional
  dourado. Aparecem nos filtros e nos cards (o nome do tamanho vem na cor dele).

### Não verificado

- Não aberto no Editor do Unity ainda (`M14-T15`).

## [0.2.0-m14.5] — 29/09/2026

### Adicionado

- **Água viva:** ondinhas em perspectiva deslizando na superfície (no Rio, com a correnteza) e
  reflexos que balançam em faixas, como água de verdade.
- Documento "Pedidos de arte: paisagem viva" com os pedidos de plantas soltas e animais para o
  ChatGPT.

### Não verificado

- Não aberto no Editor do Unity ainda (`M14-T15`).

## [0.2.0-m14.4] — 29/09/2026

### Corrigido

- O pescador aparecia sentado atrás do barco. O barco agora é desenhado em duas camadas (a borda de
  trás e o interior atrás dele, a lateral da frente na frente), e ele fica sentado no banco, dentro do
  barco, balançando junto.

## [0.2.0-m14.3] — 29/09/2026

### Adicionado

- **Arte final feita pelo proprietário no ChatGPT** (32 pedidos): cenários do Lago Sereno e do Rio
  Selvagem, os 20 peixes, barco, pescador, retrato, caixa de pesca, ícones principais, moeda, concha,
  varas, Expedições e fotos dos mapas.
- Reflexos das margens e montanhas na água, ondulando devagar.
- `tools/Arte/processar_pedidos.py` recorta e padroniza as imagens do ChatGPT; os geradores de arte
  provisória não sobrescrevem mais a arte final (`tools/Arte/finais.txt`).

### Mudado

- O pescador segura a vara apoiada no joelho; o retrato dele aparece no cartão e no Perfil.
- Margens presas às bordas da tela em qualquer formato de monitor.

### Não verificado

- Não aberto no Editor do Unity ainda (`M14-T15`).

## [0.2.0-m14.2] — 29/09/2026

### Adicionado

- **Cena mais viva, sem poluir:** poeira dourada no ar, libélulas perto dos juncos, sombras de peixe
  passando embaixo d'água, raios de sol bem fracos, névoa no horizonte, brilhos no reflexo do sol,
  anéis na água em volta do barco e da boia, respingos e gotas na captura.
- Brilho suave pulsando atrás de "Iniciar pesca" quando a pesca está parada.
- `ambient_life` no `tema_visual.json` para reduzir ou desligar tudo isso.
- Documento com os 32 pedidos de arte para o ChatGPT (link em `docs/ASSETS_PENDENTES.md`).

### Não verificado

- Não aberto no Editor do Unity ainda (`M14-T15`).

## [0.2.0-m14.1] — 29/09/2026

Visual **"Lago Dourado — Clean Premium"**, pela Bíblia de Arte (`docs/ART_BIBLE_V0_1.md`) e pelo
documento de 185 referências.

### Adicionado

- **Tema visual em dados** (`client-unity/Assets/Resources/Visual/tema_visual.json`): cores, cores de
  raridade, brilho e tempos de avisos e celebrações num arquivo só.
- **Fontes** Fredoka (títulos e números) e Nunito (texto), gratuitas (OFL).
- **44 ícones** num traço só, nos menus, botões, cartões e avisos.
- **Card oficial de peixe** na Caixa de Pesca, no Aquário e no Mercado, com selo dourado do
  Excepcional.
- **20 peixes pintados** de lado, todos no mesmo estilo.
- **Cenários novos em camadas:** Lago Sereno ao entardecer (sol baixo, reflexo dourado, pinheiros,
  juncos) e Rio Selvagem (paredões, cachoeira, correnteza). Barco e pescador novos.
- **Celebrações:** faixa grande para captura rara, Excepcional, recorde (antigo → novo), espécie nova
  (a silhueta se colore), subir de nível e leilão vencido; vitória na Arena com raios de luz.
- **Contador de moedas** que sobe com "+N".
- Fotos nos cards do Mapa e das Expedições; varas desenhadas na Loja e no Mercado.
- Lista de artes a produzir: `docs/ASSETS_PENDENTES.md`. Geradores em `tools/Arte`.

### Mudado

- Todas as janelas com o mesmo cabeçalho (ícone, título, subtítulo, ✕ Fechar), painéis com sombra e
  botões com ícone.
- O menu "Aquário 2/10" não é mais cortado (os botões do topo têm a largura do texto).
- No Aquário, a ficha do peixe não fica mais embaixo do botão Fechar.

### Não verificado

- Nada disso foi aberto no Editor do Unity ainda (`M14-T15`).

## [0.2.0-m13.1] — 29/09/2026

Primeira entrega da **V0.2** — Áudio e ambiente. Os testes da V0.1 pelo proprietário ficaram em
espera, a pedido dele.

### Adicionado

- **Ambiente calmo:** ondas suaves de mar e uma brisa leve (loops de 40 s, sem emenda), entrando
  com fade.
- **Sons novos:** peixe pescado, captura rara/Excepcional/espécie nova, recorde pessoal, subir de
  nível, moedas, aviso, momento importante e um clique discreto.
- Os sons agora são **arquivos** em `client-unity/Assets/Resources/Sons` (fáceis de trocar por sons
  gravados) e são gerados por `tools/Audio/gerar_sons.py`.

### Mudado

- O som ambiente antigo (ruído que parecia vento forte) foi substituído.
- Quando dois avisos chegam ao mesmo tempo, toca só o som mais importante.

### Não verificado

- Os sons ainda não foram ouvidos dentro do Unity (`M13-T04`).

## [0.1.0-m11.2] — 29/09/2026

Revisão geral do projeto, a pedido do proprietário. Nenhum número do balanceamento mudou.

### Corrigido

- **Pesca offline perdida depois de uma viagem:** se o jogo fosse fechado durante a viagem, o tempo
  entre a chegada do barco e a volta do jogador não virava pesca offline. Agora vira.
- **Vara fraca demais no destino:** durante a viagem dava para equipar uma vara que não serve para o
  mapa de destino. Agora isso é recusado.
- **Tela de alimentar:** a janela de confirmação podia travar quando um peixe novo chegava enquanto
  ela estava aberta.
- **Caixa de Pesca:** a lista voltava para o topo a cada peixe novo, atrapalhando quem estava
  rolando e selecionando.
- **Perfil:** apertar Esc com a confirmação de vender/destruir vara aberta fechava a janela e deixava
  a confirmação "presa" para a próxima vez.
- **Avisos e Opções:** o painel continuava aberto por cima de uma janela e roubava os cliques dela.
- **Mercado e Leilão:** o que os jogadores simulados fazem enquanto a tela está aberta agora é gravado
  na hora.
- **Combate:** uma lista de prioridade de alvos incompleta em `arena.json` poderia fazer um peixe
  atacar uma vaga vazia; o combate agora se protege disso e o balanceamento recusa essa lista.
- **Balanceamento:** agora é recusado um mapa onde uma vara permitida não teria nenhum peixe para
  pescar (antes isso parava o jogo com erro), e uma vara que cita uma raridade inexistente.
- Se o serviço de jogo parasse com um erro, o `GameRoot` ainda tentava ler o estado no mesmo quadro.
- Sons e imagens gerados pelo jogo continuam válidos ao apertar Play de novo no Editor.
- Hora das notificações no formato 24 h pela função padrão; a janela de Expedição não fixa mais "6"
  como tamanho do Cardume.

### Mudado

- A confirmação de venda de um peixe do Aquário avisa quando ele está no Cardume.
- `README.md` e `client-unity/README.md` atualizados: situação atual e o começo com o tutorial.

## [0.1.0-m11.1] — 29/09/2026

Milestone 11 — Preparação do balanceamento, testes de abuso e correções. Com ele, todos os
sistemas do MVP local estão no jogo.

### Adicionado

- **Simulador de balanceamento** (`./ops/scripts/simular.sh`): joga as regras reais e gera
  `docs/relatorios/SIMULACAO_BALANCEAMENTO.md` com os números atuais e pontos de atenção.
- **Testes de abuso local e de save/load** (9 novos, 144 no total): relógio, pedidos repetidos,
  item em dois lugares, save editado, save completo reabrindo igual.

### Corrigido

- Relógio do PC voltando no tempo não faz mais o Mercado e o Leilão refazerem verificações.
- O comprador simulado de um leilão nunca é o próprio vendedor.

### Não mudou

- Nenhum número do balanceamento (a pedido do proprietário). As revisões ficaram como tarefas em
  aberto no Milestone 11.

## [0.1.0-m10.1] — 29/09/2026

Milestone 10 — Tutorial, UX e polimento.

### Adicionado

- **Tutorial curto** (9 passos) que ensina fazendo: pegar a Vara Inicial grátis na Loja, pescar,
  abrir a Caixa, vender, guardar no Aquário, montar o Cardume e conhecer a Expedição. Dá para pular.
- **Sino de Avisos**: os últimos 50 avisos relevantes, com contador de não lidos.
- **Opções**: som, som ambiente, volume e **modo compacto** (janela pequena só com a cena).
- **Áudio provisório** gerado por código e som ambiente de água.
- Faíscas nas capturas importantes e fade ao abrir os menus.
- 4 testes novos do tutorial (135 no total).

### Mudado

- **Jogador novo começa sem vara** e pega a Vara Inicial na Loja (GDD seção 40). Saves antigos não
  mudam: entram com o tutorial concluído.
- **Formato do save: versão 9.**
- As Conchas saíram do menu de cima e foram para o cartão do jogador.

### Não verificado

- Nada desta versão foi aberto no Editor do Unity ainda (`M10-T08`).

## [0.1.0-m9.1] — 29/09/2026

Milestone 9 — Leilão local.

### Adicionado

- **Aba Leilão no Mercado**: um leilão seu por vez, de 6 horas, com lance inicial livre e sem
  cancelamento; encerrar antes só com lance, pagando 3%.
- **Lances**: mínimo de +3% sobre o atual, taxa de 1% por lance (não volta), valor reservado
  enquanto você lidera e devolvido na hora quando alguém passa. Lance no último minuto volta o
  tempo para 1 minuto.
- **Leilões e lances simulados** no seu PC, inclusive nos seus leilões e com o jogo fechado.
- Avisos de lance superado, leilão ganho, perdido, vendido e sem lances.
- 11 testes novos do Leilão (131 no total).

### Mudado

- **Formato do save: versão 8.** Saves antigos são convertidos sozinhos.

### Não verificado

- A aba Leilão ainda não foi aberta no Editor do Unity (`M9-T07`).

## [0.1.0-m8.1] — 29/09/2026

Milestone 8 — Mercado local.

### Adicionado

- **Menu Mercado** com as abas Comprar, Vender, Meus Anúncios e Itens a Retirar.
- **Comprar:** cards com filtros combinados (tipo, espécie, raridade, categoria de tamanho,
  tamanho, nível e preço), ordenação e painel de detalhes.
- **Vender:** peixes do Aquário e varas do Inventário, preço livre, sem taxa para anunciar,
  7 dias, até 5 anúncios. Taxa de 3% só quando vende.
- **Itens a Retirar:** tudo o que sai do Mercado passa por aqui. Comprar com o Aquário cheio é
  permitido; só a retirada de peixe espera uma vaga.
- **Outros jogadores simulados:** vendedores e compradores gerados no seu PC, também com o jogo
  fechado. `config/market_bots.json`, editável no Painel.
- Testes novos do Mercado (120 testes no total).

### Mudado

- **Formato do save: versão 7.** Saves antigos são convertidos sozinhos.
- Menu superior ganhou o item Mercado.

### Não verificado

- As telas novas ainda não foram abertas no Editor do Unity (`M8-T06`).

## [0.1.0-m7.1] — 29/09/2026

Milestone 7 — Arena local.

### Adicionado

- **Menu Arena** com 200 adversários simulados no seu PC: posição, Energia (24, +1 por hora),
  Honra, três adversários por vez (com uma troca), ranking e histórico.
- **Combate automático** resolvido pelas regras: ordem de alvo 1 → 6, vaga derrotada fica vazia,
  só uma pequena variação de dano (±3%), dano mínimo garantido, bônus do Cardume completo.
- **Replay** da batalha com 1x, 2x e Pular, e o resultado com a mudança de posição e de Honra.
- **Ataques recebidos**: adversários atacam você de hora em hora, inclusive com o jogo fechado.
  Defender dá Honra; perder troca a posição.
- `config/arena_bots.json`: como os adversários simulados são montados. Editável no Painel.
- 11 testes novos (105 no total).

### Mudado

- **Formato do save: versão 6.** Saves antigos são convertidos sozinhos.
- A aba Balanceamento do Painel passa a usar sempre a mesma lista de arquivos que o jogo carrega.

### Pendente com o proprietário

- `OD-009`: o que a Loja da Arena vende por Honra (a loja existe, mas está vazia).

### Não verificado

- As telas novas ainda não foram abertas no Editor do Unity (`M7-T10`).

## [0.1.0-m6.1] — 29/09/2026

Milestone 6 — Expedições.

### Adicionado

- **Menu Expedição**: envie o Cardume por 30 min, 1 h, 3 h ou 6 h. Mostra a força recomendada, o
  aproveitamento previsto com o seu Cardume, as moedas previstas e a chance de achar um peixe.
- **Recompensas**: moedas conforme a força do Cardume e, às vezes, um peixe, que vai para a Caixa de
  Pesca. Sem XP, sem Conchas, sem perder peixes.
- **Funciona com o jogo fechado**: a Expedição que terminou fora é paga ao abrir.
- **Travas**: com o Cardume fora, a formação e os peixes dele ficam travados; a pesca continua.
- Janela de conclusão com o peixe encontrado em destaque.
- 6 testes novos (93 no total).

### Mudado

- **Formato do save: versão 5.** Saves antigos são convertidos sozinhos.
- `expeditions.json` passa a ser carregado e validado pelo jogo.

### Não verificado

- As telas novas ainda não foram abertas no Editor do Unity (`M6-T05`).

## [0.1.0-m5.1] — 28/09/2026

Milestone 5 — Pesca offline.

### Adicionado

- **Pesca offline**: com a pesca ligada, fechar o jogo não para o pescador. Ao voltar, sai 1 peixe a
  cada 60 segundos do tempo fora, por até 24 horas. Os peixes vão para a Caixa de Pesca.
- **Bem-vindo de volta**: tela com o tempo fora, os peixes, o XP, espécies novas, Conchas e as
  melhores capturas.
- O PC em suspensão com o jogo aberto também conta como tempo offline.
- 5 testes novos (87 no total).

### Não verificado

- A tela de retorno ainda não foi aberta no Editor do Unity (`M5-T04`).

## [0.1.0-m4.1] — 28/09/2026

Milestone 4 — Mapas e Varas.

### Adicionado

- **Menu Mapa**: Lago Sereno e Rio Selvagem, com os requisitos de cada um. O Rio Selvagem abre no
  Nível 10 e exige a Vara 1.
- **Viagem de 30 segundos**: a pesca pausa e volta sozinha na chegada; o barco sai de cena, a tela
  escurece e o barco chega ao novo mapa.
- **Cena do Rio Selvagem**: correnteza, pedras, mata densa, cachoeira com névoa, água mais escura.
- **Menu Loja**: compra da Vara 1 (Nível 10, 2.500 moedas), que já vem equipada.
- **Melhoria de vara** de 1 a 10 com moedas, sem falha; **revenda** ao jogo (40% do preço + 25% das
  melhorias) e **destruir**, no Perfil → Inventário.
- Com a Vara 1 saem peixes Raros no Rio Selvagem e Conchas.
- 11 testes novos (84 no total).

### Mudado

- **Formato do save: versão 4** (viagem e gastos com a vara). Saves antigos são convertidos sozinhos.
- A barra superior agora tem Pesca, Mapa, Aquário, Loja e Perfil.

### Não verificado

- As telas e a cena nova ainda não foram abertas no Editor do Unity (`M4-T09`).

## [0.1.0-m3.1] — 28/09/2026

Milestone 3 — Perfil, Cardume, Inventário e Vara.

### Adicionado

- **Perfil** (menu na barra superior) com as abas Equipamentos, Inventário, Cardume, Enciclopédia e
  Destaques, e a **Força do Cardume** no topo, visível só para você.
- **Cardume**: seis posições (1–3 na frente, 4–6 atrás), montado com peixes do Aquário; bônus de
  +3% em todos os atributos com as 6 posições preenchidas.
- **Inventário e slot de Vara**: a vara equipada agora é um item do Inventário; a tela já permite
  equipar outra vara (as novas chegam no Milestone 4).
- **Enciclopédia**: as 20 espécies, com silhueta escura até a primeira captura; maior exemplar e
  quantidade pescada de cada uma.
- **Destaques**: capturas, espécies descobertas, maior peixe, peixe de nível mais alto,
  Excepcionais, Raros e vendas.
- No Aquário, peixes do Cardume têm a marca C1–C6; alimentar ou vender um deles pede confirmação e
  o tira do Cardume.
- 8 testes novos (73 no total).

### Mudado

- **Formato do save: versão 3** (vara no Inventário, Cardume). Saves antigos são convertidos sozinhos.
- O balanceamento do Cardume e da Força passa a ser lido de `arena.json`.

### Não verificado

- As telas novas ainda não foram abertas no Editor do Unity (`M3-T09`).

## [0.1.0-m2.1] — 28/09/2026

Milestone 2 — Aquário e peixe persistente.

### Adicionado

- **Aquário** (menu na barra superior): até 100 peixes, cards visuais e quatro ordenações.
- **Guardar no Aquário**, na Caixa de Pesca: a captura vira um peixe completo, com identidade
  própria, só nesse momento.
- **Ficha do peixe**: tamanho dentro da faixa da espécie, raridade, nível 1 a 10 com XP, e os quatro
  atributos (Vida, Ataque, Defesa, Velocidade), calculados sem nenhum sorteio escondido.
- **Alimentação**: peixes da Caixa ou do Aquário viram XP; um peixe já evoluído devolve 50% do XP
  investido. Prévia com o nível resultante e aviso de XP perdido acima do nível 10.
- **Venda de peixe do Aquário** para o jogo, com confirmação.
- **Painel de Desenvolvimento**: tabela de XP do peixe e capacidade do Aquário editáveis; aba Save
  mostra quantos peixes há no Aquário.
- 13 testes novos (65 no total).

### Mudado

- **Formato do save: versão 2.** Saves antigos são atualizados sozinhos ao abrir o jogo.

### Não verificado

- A tela do Aquário ainda não foi aberta no Editor do Unity (`M2-T09`).

## [0.1.0-m1.2] — 28/09/2026

O projeto deixa de usar a Vercel, a pedido do proprietário.

### Removido

- `docs/PAINEL_ONLINE.md`, o guia de publicação do painel na Vercel.
- A tarefa de publicar o painel web (`M12-T12`). A decisão `OD-002` foi encerrada como descartada.

### Mudado

- A infraestrutura online (servidor, painel web, Docker) fica arquivada: continua no repositório,
  mas fora das buscas (`.ignore`), das regras de trabalho (`CLAUDE.md`, seção 8) e do `verify.sh`
  padrão, que agora só a inclui com `--completo` (TD-022).

### Adicionado

- `vercel.json` na raiz e em `web/dev-console`, desligando as publicações automáticas da Vercel
  enquanto o projeto da Vercel ainda estiver conectado ao repositório (TD-021). Isso para os e-mails
  de "preview" a cada envio. Os arquivos podem ser apagados depois que a Vercel for desconectada.

## [0.1.0-m1.1] — 28/09/2026

**Mudança de prioridade: MVP local jogável no PC** (`docs/CLAUDE_START_HERE_V0_1_1.md`). O jogo
agora roda inteiro dentro do Unity, sem servidor, Docker, internet ou serviço pago. Entregues o
novo Milestone 0 (jogo abre e é editável) e o Milestone 1 (loop de pesca jogável).

### Adicionado — o jogo

- **Serviço de jogo local** (`Assets/Scripts/GameService`): as regras, separadas da parte visual e
  sem depender do Unity. Ciclo online de 30s por carimbo de tempo e cursor gravado no save
  (repetir pedidos não gera peixe a mais), sorteio determinístico de espécie e tamanho
  (20/60/19/1), XP do Pescador, Conchas, preço contínuo por tamanho e venda tudo-ou-nada.
- **Cena 2.5D do Lago Sereno** gerada por código: céu, sol, nuvens, morros em camadas com
  paralaxe, mata, água com brilhos, juncos, vitórias-régias, pássaros, peixes saltando ao longe,
  barco balançando e pescador de chapéu de palha.
- **Animação da pesca sincronizada ao serviço**: arremesso, espera, mordida, puxada, e o peixe
  pescado saindo da água, com aura para capturas importantes, antes de ir para a Caixa.
- **Caixa de Pesca**: cards visuais com o peixe, tamanho, barra de tamanho, preço e marcas de
  espécie nova e recorde; filtros; seleção múltipla; venda com confirmação para peixes valiosos.
- **HUD**: barra superior com Moedas, card retrátil do jogador com nível e XP, painel de pesca com
  contagem regressiva, botão da Caixa e avisos na tela.
- **Save local** com versão, backup a cada gravação, recuperação de arquivo danificado, proteção
  contra save de versão mais nova e reset que guarda uma cópia.
- **Balanceamento validado ao iniciar**: se `/config` tiver um problema, o jogo mostra a lista em
  PT-BR em vez de rodar com valores quebrados.

### Adicionado — ferramentas

- **Painel de Desenvolvimento dentro do Unity** (menu Fishing Idle → Painel de Desenvolvimento):
  visão geral, roadmap com filtro, edição de balanceamento (inclusive com o jogo rodando) e save.
- **Primeira abertura automática**: cria e abre a cena `Principal`, registra no build e ajusta nome
  do produto, janela e "rodar em segundo plano".
- **Checagens sem o Unity** (`tools/`): 52 testes das regras e compilação do código do jogo e do
  painel contra as bibliotecas de referência do Unity. Tudo em `./ops/scripts/verify.sh`.

### Adicionado — documentação

- `docs/BASE_TECNICA.md` — como o código é organizado, para qualquer desenvolvedor.
- `docs/GDD_ADENDO.md` — detalhes de design decididos na implementação, sem editar o GDD travado.
- `docs/CLAUDE_START_HERE_V0_1_1.md` — a nova prioridade do projeto.
- `docs/INFRA_ONLINE.md` — instruções da infraestrutura online, agora adiada.
- Decisões TD-015 a TD-020 em `docs/DECISOES.md`.

### Mudado

- **Roadmap reorganizado** nos milestones do MVP local (M0 a M11). O que foi construído para a
  versão online foi preservado no **M12 — Infraestrutura online (adiada)**, com o histórico.
- **README** reescrito com o passo a passo para jogar.
- `config/economy.json` ganhou `fishing_box.bulk_sale_protection`.
- O diagnóstico de conexão com o servidor no Unity ficou dormente (o jogo não precisa de servidor).
- `verify.sh` roda as checagens do jogo e só roda servidor e painel web se houver .NET 10 e Node.

### Não verificado

- **O projeto ainda não foi aberto no Editor do Unity**: o ambiente de trabalho não tem o Unity.
  As regras passaram nos testes e o código compilou contra as bibliotecas de referência, mas o
  visual e a jogabilidade só se confirmam apertando Play (`M0-T13`, `M1-T12`).

## [0.1.0-m0.4] — 25/09/2026

O painel passa a poder rodar publicado na internet e a se atualizar sozinho.

### Adicionado

- **O painel lê o repositório direto do GitHub** (`M0-T15`). A camada de dados tenta três fontes em
  ordem e sempre diz na tela qual respondeu: a API do servidor (única com dados de execução), o
  arquivo do clone local, e o GitHub. Rodando de um clone, a ordem é a de antes; publicado, só o
  GitHub responde e a API nem chega a ser tentada, para a página não esperar um tempo limite inútil.
- **Atualização automática** (`M0-T16`). O painel relê a página a cada 60 segundos, mostra o horário
  da última leitura e oferece um botão de atualizar agora. Respeita a preferência de movimento
  reduzido do sistema.
- **O commit atual na Visão geral** — identificador curto, assunto e data — para deixar explícito o
  que a tela está refletindo.
- **`docs/PAINEL_ONLINE.md`**, o passo a passo para publicar o painel na Vercel sem instalar nenhum
  programa.

### Corrigido antes de existir

- A listagem de `/config` usava a API do GitHub, limitada a 60 consultas por hora por endereço de
  IP — um limite que um host compartilhado consome sozinho. Agora, quando esse limite é atingido, o
  painel lê os arquivos por um caminho que não tem esse limite. Verificado com o limite realmente
  estourado: as sete linhas continuaram aparecendo com suas descrições.

### Pendente com o proprietário

- `M0-T17` e `OD-002`: criar a conta na Vercel e publicar. O código está pronto e testado — uma
  simulação do ambiente publicado, sem clone e sem backend, renderizou as quatro páginas lendo
  apenas o GitHub — mas a conta só o proprietário pode criar.

## [0.1.0-m0.3] — 25/09/2026

A regra de idioma passa a ser obrigatória e permanente, por definição do proprietário.

### Adicionado

- **`CLAUDE.md` na raiz do repositório**, lido automaticamente no início de toda sessão de trabalho.
  Ele carrega a regra obrigatória de idioma na íntegra, mais os outros princípios permanentes do
  projeto: o servidor é a autoridade, balanceamento mora em dados, o escopo da V0.1 é pequeno de
  propósito, o painel reflete o repositório, e `verify.sh` precisa passar antes de versionar.

### Corrigido — texto visível que ainda estava em inglês

- **As mensagens dos scripts de `ops/scripts`**, que são a primeira coisa que o proprietário lê ao
  subir o projeto: `dev-up.sh`, `dev-down.sh`, `migrate.sh`, `new-migration.sh` e `verify.sh`.
- **Os comentários de `.env.example`** na raiz e em `web/dev-console`, arquivos que o proprietário
  abre para editar.
- **Os valores técnicos que chegam à tela vindos da API**: o overlay do Unity agora traduz
  `database` para "banco de dados" e `healthy` para "saudável", em vez de exibir a chave crua.

### Corrigido — consistência das chaves técnicas

- `economy.json` tinha `shells.source` traduzido para `apenas_pesca_na_v0_1` enquanto todas as
  outras chaves técnicas seguiam em inglês. Revertido para `fishing_only_in_v0_1`. Chaves técnicas
  são armazenadas em inglês e traduzidas apenas na exibição; traduzir uma delas quebraria dados já
  gravados.

## [0.1.0-m0.2] — 25/09/2026

Tradução completa para PT-BR. Nenhum comportamento mudou.

### Alterado

- **Painel inteiro em PT-BR.** Todo o texto de interface está concentrado em
  `web/dev-console/src/lib/strings.ts`, de modo que um eventual segundo idioma seja uma mudança de
  um arquivo. Datas, horas e números passaram a ser formatados no padrão brasileiro
  (25/09/2026, 10,3%).
- **Roadmap em PT-BR** — os 13 milestones, suas metas e critérios, e os títulos, descrições e notas
  de conclusão das 126 tarefas.
- **Nomes que o jogador lê em PT-BR e com acentuação correta**: as 20 espécies (Tilápia, Traíra,
  Curimbatá, Matrinxã, Tucunaré, Jaú, Aruanã…), as varas (Vara Inicial, Vara 1), as expedições
  (Saída Rápida, Volta na Margem, Águas Profundas, Viagem Longa), as raridades (Comum, Raro), as
  categorias de tamanho (Pequeno, Adulto, Grande, Excepcional) e as moedas (Moedas, Conchas, Honra).
- **Mensagens do servidor que aparecem na tela em PT-BR**: falhas de validação do roadmap, causa da
  indisponibilidade do banco, erros dos endpoints e a mensagem de partida que orienta a configurar a
  string de conexão.
- **Overlay do Unity em PT-BR**: conectado, degradado, inacessível, e as mensagens de falha de
  conexão.
- **Documentação em PT-BR**, com os arquivos renomeados: `DECISIONS.md` → `DECISOES.md`,
  `SECURITY.md` → `SEGURANCA.md`, `VERSIONING.md` → `VERSIONAMENTO.md`.
- **Regra de idioma registrada** como decisão TD-014 em `docs/DECISOES.md`.

### Mantido em inglês, de propósito

- Comentários de código, nomes de variáveis, classes e arquivos, e logs técnicos do servidor.
- As chaves técnicas gravadas em dados: os status (`TODO`, `DONE`…), os subsistemas (`server`,
  `client`…) e os identificadores (`lambari`, `rod_01`, `map_02`). Uma chave técnica não deve mudar
  quando o idioma muda; o painel a traduz na hora de exibir.
- `docs/GDD_V0_1.md` e `docs/CLAUDE_START_HERE_V0_1.md`, os documentos de design travados. Traduzir
  a fonte de verdade correria o risco de deslocar o sentido de uma decisão já fechada.

## [0.1.0-m0.1] — 25/09/2026

Milestone 0 — fundação do repositório e do fluxo de trabalho. Ainda sem jogabilidade, de propósito.

### Adicionado

- **Estrutura do monorepo** conforme a seção 4 do GDD: `client-unity`, `server`, `web`,
  `shared-contracts`, `config`, `docs`, `ops`.
- **API do backend** (`server/src/FishingIdle.Api`, ASP.NET Core em .NET 10 LTS) com log estruturado
  em console, leitura de opções, erros em problem-details e CORS de origem exata para o painel.
  - `GET /health` — estado com detalhamento por dependência, sempre HTTP 200 para o corpo ser legível.
  - `GET /health/live` — sinal de vida sem dependências.
  - `GET /api/dev/roadmap` — roadmap validado mais o resumo calculado.
  - `GET /api/dev/version` — versões dos componentes mais dados de execução.
  - `GET /api/dev/config` e `GET /api/dev/config/{arquivo}` — arquivos de balanceamento, só leitura.
- **Persistência em PostgreSQL** via EF Core 10 e Npgsql, com a migration inicial criando
  `config_versions` e `admin_audit_log`. As migrations são aplicadas deliberadamente, nunca ao subir.
- **36 testes automatizados do servidor**, incluindo testes de validação que rodam contra o
  `docs/roadmap.json` real, para que um erro de digitação em um status quebre a suíte.
- **Painel de Desenvolvimento** (`web/dev-console`, Next.js 15 + TypeScript) com as páginas Visão
  geral, Roadmap, Configuração do jogo e Build / Versão. Lê o backend e, se ele cair, lê o
  `docs/roadmap.json` direto do repositório, marcando qual fonte respondeu.
- **Esqueleto do cliente Unity** (`client-unity`) com inicialização por código, cliente de API
  guiado por configuração, verificação de `/health` e um overlay de diagnóstico (F1) que distingue
  conectado, degradado e inacessível.
- **Ambiente local** — `ops/docker-compose.yml` (PostgreSQL com healthcheck e volume nomeado, API,
  painel) mais os scripts `dev-up.sh`, `dev-down.sh`, `migrate.sh`, `new-migration.sh` e `verify.sh`.
- **Configuração de balanceamento** — os sete arquivos de `/config` preenchidos com dados
  provisórios: 20 espécies, 2 mapas com pesos de captura, Vara Inicial e Vara 1 com custos de
  melhoria, as curvas de XP, a distribuição de tamanho e os valores de economia, arena e expedições.
- **Roadmap** — `docs/roadmap.json` com 13 milestones e 126 tarefas, mais um esquema JSON.
- **Documentação** — `GDD_V0_1.md` (fonte de verdade), `DECISOES.md`, `ROADMAP.md`, `API.md`,
  `SEGURANCA.md`, `VERSIONAMENTO.md`, este histórico, e um `README.md` na raiz com as instruções de
  execução local.

### Limitações conhecidas

- `/api/dev/*` não tem autenticação. O ambiente sobe em localhost e não pode ser exposto
  publicamente até a tarefa `M1-T01`. Veja `docs/SEGURANCA.md`.
- A edição de configuração é somente leitura até as tarefas `M1-T06` a `M1-T08` entregarem
  validação, versionamento e auditoria.
- O ambiente Docker Compose e a compilação do Unity não foram executados no ambiente de autoria
  (sem daemon do Docker, sem Editor do Unity). Registrado como a tarefa `M0-T14`.
