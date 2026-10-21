# Histórico de mudanças

As versões seguem `docs/VERSIONAMENTO.md`. As versões de cada componente ficam em `version.json` na
raiz do repositório, que o servidor serve em `GET /api/dev/version`.

## [0.2.0-m25.8] — 20/10/2026

### Corrigido

- O pescador se mexia mesmo sem pescar: agora, com a pesca parada, ele fica imóvel no primeiro quadro, com a vara e a
  boia em repouso (A-192). Visual só fica verificado quando o proprietário apertar Play.

## [0.2.0-m25.7] — 20/10/2026

Fonte nova e as últimas telas da revisão visual (A-191). Nenhuma regra, número ou save mudou. Visual só fica verificado
quando o proprietário apertar Play.

### Alterado

- **Nunito em todo o texto** (escolha do proprietário): títulos, números, botões, corpo e HUD numa letra lisa e
  arredondada, em quatro pesos; a Press Start 2P e a VT323 saíram. Ícones, personagens e peixes continuam em pixel art.
  O texto fica nítido em qualquer tamanho de janela. Selos sem maiúsculas forçadas ("Nova espécie", "Recorde").
- **Caixa de Pesca:** um botão "Filtros" com ordem, tamanho e qualidade; "Selecionar todos" e "Limpar seleção" num
  botão só; o valor dos selecionados em moeda + número.
- **Mercado:** "Meus anúncios" e "Itens a Retirar" viraram uma aba só, com os dois blocos.
- **Arena:** posição em texto, sem medalha e sem brilho; "Frente" e "Trás" escritos acima das filas.
- **Passe:** cada recompensa em ícone + número e um estado só (botão, visto ou cadeado).
- **Clã:** o porto só no topo, seções separadas por espaço e o aviso do jogo online numa linha.
- **Bem-vindo de volta:** números grandes com ícone, fragmentos e avisos em linhas e um botão principal.
- **Título de chegada** com uma faixa escura que se lê sobre o céu claro.

### Pendente

- Decisão do proprietário: tirar a cor de Pequeno, Adulto e Grande.

## [0.2.0-m25.6] — 20/10/2026

Revisão visual pedida pelo proprietário (A-190), 2ª parte: menus mais limpos e nada sobreposto. Nenhuma regra, número ou
save mudou. Visual só fica verificado quando o proprietário apertar Play.

### Alterado

- **HUD:** as Moedas aparecem uma vez só (no cartão do jogador); painel de pesca de uma linha com a barra e o botão de
  parar (ciclo e chances ao passar o mouse); atalhos menores e sem rótulo (o nome aparece ao passar o mouse); no máximo
  2 avisos na tela; botão da Caixa com o ícone e "128/300".
- **Loja:** lista, item escolhido e ficha, sem a parede de tábuas, os ganchos, o pedestal e o voo; nada gira; as chances
  em pílulas da cor da raridade ("Épico 10%").
- **Missões:** linhas mais baixas, recompensa em linha e barras alinhadas; sem o "Nº" e sem repetir o nome da aba.
- **Ranking:** sem o nível repetido, sem o troféu no título e sem a moldura do pódio; retratos em pixel.
- **Enciclopédia:** nomes dos mapas inteiros; o cartão mostra só o nome, o mapa (em "Todas") e quantas vezes pescou.
- **Aquário:** os peixes não giram mais. **Eventos:** sem o quadro de cortiça, um título só.

### Pendente

- Revisão das outras telas (M25-T24) e as decisões do proprietário: fontes (M25-T22) e a cor de Pequeno/Adulto/Grande.

## [0.2.0-m25.5] — 20/10/2026

Revisão visual pedida pelo proprietário depois de jogar a versão pixel (A-190), 1ª parte. Nenhuma regra, número ou save
mudou. Visual só fica verificado quando o proprietário apertar Play.

### Alterado

- **Paleta A "Ardósia"** (escolha do proprietário): painéis azul-ardósia sólidos no lugar do azul-noite quase preto,
  cartões um tom mais escuros, títulos em creme (o amarelo fica só para recompensa) e raridades um tom mais claras. O
  tema passou de 38 para 14 cores de interface e 5 de raridade; as outras saem delas pelo código (TD-066).
- **Pescador menor** (figura de 35 px, era 48) e **barcos menores** (~100 px, eram 124), recortados de novo das folhas
  originais; a Caixa de Pesca no barco acompanha (40 × 28) e a vara tem 48 px.
- **Moldura das janelas mais limpa:** o título e o contador ficam como texto sobre o painel (sem caixa), o "✕" sem
  círculo (aparece ao passar o mouse) e as abas laterais mais largas, para "Enciclopédia" caber inteira.
- **Letra menos borrada:** a escala da interface usa um passo limpo (0,5 · 1 · 1,5 · 2) quando cabe; nas outras alturas
  fica como antes.
- Itens escuros (varas 3 a 5, fragmentos delas, calça, luvas e botas) com um encaixe claro atrás; fundos escuros do
  Mapa, da Expedição, do Clã e do Boss um pouco mais claros. Eventos usa o calendário; o sino é só dos Avisos. Cada aba
  do Perfil tem o seu ícone.
- Fontes e tamanhos de letra num lugar só (`UiSkin`), para a troca de fontes ser só de arquivo e números.

### Pendente

- Trocar as fontes (M25-T22, o proprietário escolhe) e refazer 10 ícones (M25-T23, pedido pronto em `ASSETS_PIXEL.md`).

## [0.2.0-m25.4] — 20/10/2026

Acabamento da arte pixel. Nenhuma regra, número ou save mudou. Visual só fica verificado quando o proprietário apertar
Play.

### Corrigido

- Laterais e cantos (`near_*`, `fg_*`) dos outros 7 mapas: a margem transparente de 2 px saiu do lado que encosta na
  beira da tela e da base, como já era nos 3 mapas da 3ª entrega. Evita uma fresta na borda da tela (TD-065).
- A nota dos avatares em `config/progression.json` apontava para `Resources/Arte/Avatares`, que não existe mais; agora
  aponta para `Resources/ArtePixel/Avatares`.

## [0.2.0-m25.3] — 20/10/2026

As 11 imagens que faltavam (3ª entrega, um desenho por imagem): 339 de 339 em pixel. Nenhuma regra, número ou save
mudou. Visual só fica verificado quando o proprietário apertar Play.

### Adicionado

- **Mandi-amarelo em pixel** (pedido L4-03a): 116 × 56 e `_p` 58 × 28. Os 100 peixes estão em pixel.
- **Laterais e cantos em pixel** do Lago Sereno (`near_left`, `near_right`, L1-06b), Banco das Baleias e Talude
  Noturno (`near_*` e `fg_*`, L6-08b e L6-09b), no lugar dos provisórios convertidos. As duas peças de cada par usam a
  mesma escala e os lados retos encostam na borda da imagem (sem fresta na beira da tela). Miniaturas dos 3 mapas
  refeitas.
- `tools/Arte/pixel_folhas.py`: opções `--escala` (escala fixa, para peças do mesmo mapa vindas uma por imagem) e
  `--encostar` (lados sem margem transparente).

### Alterado

- Cantos do Lago Sereno (`fg_left`, `fg_right`): só a margem transparente do lado da borda e de baixo saiu; o desenho
  é o mesmo.

### Removido

- `Resources/Arte` (a última imagem antiga), o fallback `ArtAssets.LegacyRoot` e a regra de importação
  `ArtImportSettings.ApplyOldFish` (TD-065).
- O comando `provisorios` do `tools/Arte/cena_pixel.py`, `tools/Arte/pixel_provisorios.txt` e
  `tools/Arte/fontes_antigas/Mapas` (10 PNGs). `fontes_antigas/Vivos` fica (comando `vivos`).

## [0.2.0-m25.2] — 20/10/2026

5 peixes do Pantanal Dourado em pixel (pedido L4-03a, 2ª entrega). Nenhuma regra, número ou save mudou. Vem depois da
limpeza da arte antiga (0.2.0-m25.1). Visual só fica verificado quando o proprietário apertar Play.

### Adicionado

- **Muçum, jundiá, piavuçu, jurupensém e pacu-peva em pixel** (`Resources/ArtePixel/Peixes`, com a versão pequena
  `_p`), recortados com `tools/Arte/pixel_folhas.py` a partir de imagens de um peixe só (1536 × 1024, grade de 8 px),
  com os parâmetros do pedido L4-03a (`--grade 1x1 --celula 192x128`).

### Removido

- As 5 imagens antigas desses peixes em `Resources/Arte/Peixes`. Fica só a do mandi-amarelo, que não veio e vai ser
  pedido de novo; o fallback da arte antiga (TD-065) sai quando ele chegar.

### Alterado

- `docs/ASSETS_PIXEL.md`: os pedidos que faltam foram reescritos como um desenho por imagem, sem imagem de referência
  anexada (20/10/2026).

## [0.2.0-m25.1] — 20/10/2026

Limpeza da arte antiga e checkup do projeto (M25-T17, TD-065). Nada muda na tela: o que saiu ou não era mais lido ou
perdia para o arquivo em pixel. Nenhuma regra, número ou save mudou. O sufixo passa a `m25` porque o milestone atual do
roadmap agora é o M25 (antes estava em M24 por engano). Vem depois das telas de aventura em pixel (0.2.0-m24.25).

### Removido

- **Arte antiga (`Resources/Arte`):** de 659 imagens (166,4 MB) para 6 (2,2 MB). Saíram 421 imagens (141,3 MB) que já
  têm versão em pixel ou que o jogo não lia mais (céus, nuvens e água pintados dos mapas, barco e mãos do pescador
  pintados, varas da cena, moedas e peças da barra antiga, o "Diário de bordo", emblemas do Ranking e as peças que as
  telas passaram a desenhar). A pasta `client-unity/Assets` caiu de 189,8 MB para 25,0 MB.
- **Fontes Fredoka e Nunito** e suas licenças: a interface usa só Press Start 2P e VT323.
- **Código do estilo antigo:** cenário pintado, pescador e vara pintados, o modo papel das janelas, efeitos que só o
  cenário pintado usava e 12 textos sem uso.
- **20 ferramentas antigas de arte** em `tools/Arte` (geradores e processadores do estilo anterior).

### Alterado

- **Arte de origem fora do jogo:** as 10 laterais e cantos que viram os provisórios dos mapas (pedidos L1-06b, L6-08b
  e L6-09b) e os 222 desenhos da paisagem viva foram para `tools/Arte/fontes_antigas/` (22,9 MB), que não vai no build.
- **Ficam na arte antiga só os 6 peixes do Pantanal** (mandi-amarelo, pacu-peva, jurupensém, piavuçu, jundiá e muçum)
  até o pedido L4-03a chegar em pixel.

### Corrigido

- O tempo do leilão ("1 hora" / "6 horas") agora vem dos textos do jogo, como todo texto da tela.
- O Cardume dos adversários da Arena segue o tamanho do Cardume do balanceamento (`arena.json`, `cardume.max_fish`),
  em vez de 6 escrito no código.
- A variação de tamanho dos peixes dos adversários foi para `arena_bots.json` (`size_percentile_spread`, mesmo valor).
- A viagem entre mapas não tem mais um tempo de reserva escrito no código: `maps.json` precisa ter
  `travel.duration_seconds`, e a checagem do balanceamento avisa se faltar.

## [0.2.0-m24.25] — 20/10/2026

Aquário, Enciclopédia, Mapa, Expedição, Arena, Boss Mundial e retratos em pixel e na pele ARC (M25-T13, T14 e T16,
A-189, TD-064). Só apresentação: nenhuma regra, número, recompensa ou save mudou. Vem depois das telas de comércio,
social e perfil (0.2.0-m24.24).

### Alterado

- **Aquário:** água e areia pixel em escala inteira cortando as bordas; vidro e bolhas desenhados pelo código; peixes
  nadando sem girar (espelho e 1 pixel de balanço); "C1" num selinho nas miniaturas da gaveta.
- **Ficha do peixe e Enciclopédia:** água em faixas, aura da raridade em pixel, pedestal pixel, setas em disco pixel;
  abas, página e cartas do álbum na pele ARC, com o verso da carta desenhado pelo código.
- **Mapa:** mar e ilhas pixel em escala inteira, anéis de 1 pixel no mapa atual e no escolhido, barco sem girar.
- **Expedição:** carta náutica, porto, medalhão, selo e bússola em pixel; cartão da missão no painel ARC.
- **Arena:** fundo em escala inteira, "VS" escrito pelo código, divisão da abertura em degraus, pódios em escala inteira,
  anel dourado na vitória no lugar dos raios girando.
- **Boss Mundial:** abismo e Boss em escala inteira, poses alternadas, golpe em anel vermelho, impacto e bolhas pelo
  código.
- **Retratos e avatares:** em escala inteira, versão de 32 px nas listas e versão em círculo de pixels nas molduras
  redondas (`tools/Arte/retratos_pixel.py`).

## [0.2.0-m24.24] — 20/10/2026

Telas de comércio, social e perfil em pixel e na pele ARC (M25-T15, A-188, TD-063). Só apresentação: nenhuma regra,
número, recompensa ou save mudou. Vem depois da pele ARC (0.2.0-m24.23).

### Alterado

- **Fundos e faixas em escala inteira, cortando as bordas:** Mercado, Caixa de Pesca, Passe, Clã, Eventos e Bem-vindo
  de volta mostram a arte pixel sem esticar, centrada na parte que importa; o cartão do Boss fica na cortiça do quadro
  de avisos em pixel.
- **Cantos em pixel nas dez telas:** os retângulos arredondados viraram pílulas, chanfros e quadrados em pixel, com
  contorno de um pixel de arte e sombra dura.
- **Loja:** parede, viga, ganchos, plaquinhas, etiquetas, balcão, carta e caixa de equipamento desenhados em pixel pelo
  código; boias pixel; "Boia deitada: não morde aqui" numa linha só embaixo das boias.
- **Ranking:** palco pixel em escala inteira, escudos, troféu e coroa a 1×, ícones pixel no lugar dos emblemas.
- **Perfil:** a carteira do jogador virou painel ARC desenhado pelo código; carteira de moedas com ícones pixel.
- **Clã:** brasão pixel com o nome na faixa; "Vaga · N Dólares" quando o texto longo não cabe no botão.
- **Missões:** ícones de Moedas, Conchas e Dólares a 1×.

## [0.2.0-m24.23] — 20/10/2026

A interface inteira na **pele ARC** e nas **fontes pixel** (M25-T03, A-184). Só apresentação: nenhuma regra, número,
recompensa ou save mudou, e as telas continuam organizadas como antes. Vem depois da pixel art ligada no jogo (0.2.0-m24.22).

### Alterado

- **Fontes:** Press Start 2P nos títulos e números e VT323 no resto, ambas OFL (`Resources/Fontes`, com
  `OFL-PressStart2P.txt` e `OFL-VT323.txt`), nos tamanhos da Bíblia (títulos 24, corpo 28, pequeno 24, chips 22).
  Títulos com maiúscula acentuada usam a VT323, porque a Press Start 2P desenha "É" como "é".
- **Janelas:** painel azul-noite com moldura dourada fina em pixel no lugar do "Diário de bordo" (madeira e papel); título
  em etiqueta ARC, "✕" em pixel, sub-abas laterais ARC, dicas e "Como funciona" em caixa escura.
- **Botões e chips em pílula:** secundário com linha dourada, ação com linha turquesa, recompensa com face dourada;
  filtros com linha azul-escura e o escolhido com linha dourada.
- **Barras quadradas em blocos** (XP em listras verde-água, atributos e genes em azul-claro), cartões com linha interna e
  selecionado com contorno ouro, contadores vermelhos com linha dourada.
- **Menu de cima e atalhos:** faixa escura fechada pela moldura, aba aberta em pílula com linha dourada, atalhos redondos
  com moldura dourada e rótulo em pílula; a plaquinha de Moedas virou pílula (sem cordas).
- Sem brilho difuso nem sombra borrada: o brilho virou anel de 1 pixel e a sombra, um deslocamento duro.
- `tema_visual.json` com a paleta ARC e o bloco novo `arc` (moldura, campos, cartões, barras, alerta, véu).
- **Ícones da interface pelo `PixelDraw`** (fecha a M25-T11): escala inteira e os coloridos sem tinta; Moedas, Conchas e
  Dólares pelos ícones pixel.

## [0.2.0-m24.22] — 20/10/2026

A pixel art ligada no jogo (M25-T04 a T12): a cena de pesca dos 10 mapas, o pescador animado, os peixes, os itens e os
ícones. **Nenhuma regra, número, sorteio, recompensa ou save mudou.** Compila; o visual só fica verificado quando o
proprietário apertar Play.

### Adicionado

- **Carregamento com fallback:** o jogo procura cada imagem em `Resources/ArtePixel/` e usa a arte antiga do mesmo nome
  enquanto a nova não existir. Nas telas, a pixel art aparece na maior escala inteira que cabe (versão pequena `_p`
  quando nem 1× cabe), sem borrar e sem girar (A-187).
- **Câmera pixel** própria: a cena é desenhada numa grade de 216 px de altura e ampliada sem suavizar (5× em 1080p;
  "nítido-suave" quando a janela não é múltipla de 216); tudo que se mexe para em pixel inteiro (TD-062).
- **Cena em pixel nos 10 mapas:** céu e água pelo código nas paletas da Bíblia, estrelas e lua de noite, nuvens comuns
  tingidas, faixas do fundo, laterais, cantos, reflexos e brilhos que mudam de lugar em passos de pixel (A-185).
- **Pescador animado por fase** (parado, arremesso, fisgada, recolher, mostrando, escapou) com a vara e a linha
  desenhadas pelo código nas cores da vara; barco em pixel com a frente cortada para ele sentar dentro; Caixa de Pesca
  e boia em pixel (A-186).
- `tools/Arte/cena_pixel.py`: grava `Resources/Visual/cena_pixel.json` (os números da cena), corta a frente dos
  barcos, desenha os 12 símbolos simples, converte a paisagem viva (222 quadros) e as peças de mapa que faltam, gera as
  miniaturas dos mapas e monta prévias da cena como o jogo desenha, com um GIF do pescador.

### Alterado

- Plantas e vitórias-régias não giram mais na cena pixel (encostam 1 px na rajada); o barco balança 1 px sem inclinar.
- Na cena pixel saem os brilhos suaves (sol, raios, névoa, poeira, libélulas e a aura da captura); a recompensa
  continua com as faíscas.

### Provisório

- Laterais do Lago Sereno e laterais e cantos do Banco das Baleias e do Talude Noturno: convertidos da arte antiga até
  os pedidos L1-06b, L6-08b e L6-09b chegarem. Os 6 peixes do Pantanal (L4-03a) seguem com a arte antiga.
- Ícones coloridos ainda são tingidos pela cor pedida em algumas telas até o `UiSkin` usar o `PixelDraw.Icon`
  (M25-T11, junto com a pele da M25-T03).

## [0.2.0-m24.21] — 20/10/2026

A pixel art que o proprietário mandou (77 folhas, lotes 1 a 7) recortada e gravada em `Resources/ArtePixel/`
(M25). **O jogo ainda não usa essa pasta** (a ligação é o M25-T04 em diante): nenhuma tela, regra, número ou save mudou.

### Adicionado

- **323 das 339 imagens** em `client-unity/Assets/Resources/ArtePixel/` (mais as 94 versões `_p` dos peixes): 94
  peixes, os 17 quadros do pescador, retrato e avatares, 52 ícones, objetos, ilhas, pódios, itens, expedições,
  cenários dos 10 mapas, fundos das telas e as 4 poses do Boss. Faltam 16, para pedir de novo (Pantanal 1 de 2,
  laterais do Lago Sereno, laterais e cantos do Banco das Baleias e do Talude Noturno), com o motivo em
  `docs/ASSETS_PIXEL.md`.
- Importação de `ArtePixel/` com Point, sem compressão e sem mipmaps (`ArtImportSettings`, TD-059).

### Alterado

- **`tools/Arte/pixel_folhas.py` refeito** (TD-060): acha cada desenho pela forma em vez de cortar por grade igual
  (nada mais sai cortado), tira a escala do tamanho do desenho, paleta por desenho, limpa o halo rosa e os pixels
  soltos; animações com o mesmo quadro e os pés no mesmo lugar. Manifesto com os campos novos e teste atualizado.
- A-183: tamanhos e âncoras fixados no recorte (pescador de 48 px com os pés na coluna 40, peixes até 112 px).

## [0.2.0-m24.20] — 19/10/2026

Plano da troca para o novo estilo visual escolhido pelo proprietário: pixel art "Estilo ARC" (A-182, M25). **Só
documentação e ferramenta:** nada mudou no jogo (nenhuma tela, arte, regra, número ou save).

### Adicionado

- **`docs/ART_BIBLE_PIXEL.md`**, a nova fonte de verdade visual: grade da cena (216 px de altura, 20 pixels por
  unidade, 5× no 1080p), escala inteira na interface, tamanho nativo de cada tipo de arte, contorno, luz, cores,
  animações do pescador por fase da pesca, paleta da interface ARC e dos 10 mapas, fontes Press Start 2P e VT323 com
  os tamanhos mínimos, importação e câmera. A `docs/ART_BIBLE_V0_1.md` fica guardada, travada, como histórico.
- **`docs/ASSETS_PIXEL.md`**, a lista viva das artes do novo estilo: as 659 imagens de hoje, uma por uma (316 refeitas
  por pedido, 240 convertidas da arte atual, 60 desenhadas pelo código, 43 saem) e os 77 pedidos ao ChatGPT (339
  imagens) em folhas, em 7 lotes, com os nomes de arquivo e o status.
- **`tools/Arte/pixel_folhas.py`**, que recorta as folhas do ChatGPT: tira o fundo magenta, detecta a ampliação
  (inteira ou não), volta cada desenho à grade, reduz a paleta, limpa pixels soltos, põe o contorno onde falta e
  grava com os nomes do pedido (manifesto `tools/Arte/pixel_pedidos.json`). Testado com folhas sintéticas
  (`tools/Arte/pixel_folhas_teste.py`).
- Roadmap: **M25 "Visual em pixel art (Estilo ARC)"**, com a ordem do trabalho (pele e fontes, câmera, cena piloto,
  pescador, peixes, itens, ícones, mapas, telas, retratos, limpeza). TD-059 registra o pipeline (pasta `ArtePixel`,
  importação Point, câmera pixel própria, fontes OFL).

## [0.2.0-m24.19] — 15/10/2026

As 31 artes de prioridade C, mandadas pelo proprietário, nas telas que ainda usavam desenho provisório (M24-T60,
A-181). Nenhuma regra, número ou save mudou. Compilado no CI, não aberto no Unity.

### Mudado

- **Mapa:** o mar pintado no lugar do degradê; uma ilha pintada por mapa nos medalhões e no painel (a moldura, o
  número, o halo e o cadeado continuam do jogo); o barquinho pintado, que vira para o lado da rota durante a viagem.
- **Enciclopédia:** página do álbum azul-marinho (com o texto claro), abas do fichário, moldura das cartas na cor da
  raridade e verso das cartas não descobertas.
- **Ficha do peixe** (Enciclopédia e cartas do Aquário): brilho na cor da raridade e pedestal de pedra e coral, sem
  achatar.
- **Aquário:** água com raios de luz, areia presa embaixo (sem esticar), bolhas e painéis de vidro.
- **Arena:** pódio pintado (ouro, prata e bronze) na proporção da arte, com o lugar numa pílula sobre a faixa de metal.
- **Avatares:** os 6 retratos do jogador, sobre um disco azul-mar em todas as molduras.
- Sem um arquivo, cada tela volta ao desenho provisório.

## [0.2.0-m24.18] — 13/10/2026

Montar barco e vara pelo botão, um depois do outro, e mapas só pelo nível, pedido do proprietário (M24-T58, A-180).
Save continua v24. Verificado no CI, não aberto no Unity.

### Mudado

- **Barcos e varas não montam mais sozinhos.** Com os fragmentos completos, o jogador aperta **Montar** na Loja
  (Barcos ou Varas). Para montar é preciso ter o **anterior**: o Barco 1 pede o Água Mansa e o Barco N pede o Barco
  N − 1; a Vara 1 pede o Caniço Manso e a Vara N pede a Vara N − 1 no Inventário. Sem o anterior, a Loja mostra
  "Precisa do barco X" / "Precisa da vara X". Montar gasta só os fragmentos pedidos (o resto fica guardado) e **não**
  troca o barco em uso nem a vara equipada.
- Os avisos da pesca, do Passe, do Boss Mundial e do Clã dizem "Fragmentos completos: … Monte na Loja"; o Bem-vindo de
  volta marca "COMPLETO!".
- **Mapas abrem só pelo nível do Pescador.** Nenhum mapa pede vara; qualquer vara pode ser equipada em qualquer mapa. Na
  tela do Mapa, "Sua vara aqui" mostra, só como informação, as raridades que a vara em uso não pesca naquele mapa.
- Nada mudou nos números de balanceamento. O ritmo re-simulado: os mapas abrem bem antes (Mapa 10 em ~8 h, ~1,3 dia e
  ~2,8 dias, contra 1,8 / 3,8 / 5,8 dias) e o Nv.1000 chega ~1 dia antes (54,2 / 101,3 / 108,8 dias).

### Pendente

- **OD-074:** vender fragmentos de barco e de vara (e barcos)? Hoje só as varas montadas se vendem.

## [0.2.0-m24.17] — 13/10/2026

Artes dos menus que ainda não tinham visual próprio (M24-T57, A-179). Verificado no CI, não aberto no Unity.

### Mudado

- **Mercado**: a feira do porto numa faixa no topo; o painel lateral ganhou a altura inteira em todas as abas. Ícone
  novo do **Leilão** no marcador e nos botões de lance e de leiloar.
- **Caixa de Pesca**: o convés com a caixa de gelo numa faixa no topo.
- **Passe do Pescador**: o resumo sobre o pôr do sol; selo dourado na fileira Premium (com cadeado até comprar).
- **Arena**: a arena submersa atrás da batalha e da tela VS; o emblema dos dois peixes no "VS".
- **Eventos**: os cartões pregados no quadro de cortiça.
- **Clã**: o porto à noite com o brasão no topo; o nome do clã também vai na fita do brasão.
- **Perfil**: o cartão de couro azul com a moldura do avatar no cabeçalho.

## [0.2.0-m24.16] — 13/10/2026

Boss Mundial animado, pedido do proprietário (M24-T56, A-178), e artes que chegaram. Verificado no CI, não aberto no Unity.

### Adicionado

- **Boss Mundial animado**: o fundo do abismo atrás da cena; o Boss respira (passa devagar da pose parada para a pose de
  boca entreaberta e volta, a cada 2,4 s), balança na água e solta bolhas. A cada golpe do Cardume ele dá um bote para
  a esquerda com a boca escancarada, pisca em vermelho e um estouro de água aparece no corpo. Fora do horário do evento
  ele só respira, mais escuro.

### Mudado

- Chegaram e já estão no jogo: o ícone do atalho **Missões** (`ico_atalho_missoes`), o ícone do **Clã** (`ico_cla`) e a
  cena do **Bem-vindo de volta** (`bemvindo_cena`), além das artes do Boss animado.
- O tremor do Boss ao levar um golpe ficou mais suave.

## [0.2.0-m24.15] — 13/10/2026

Missões, pedido do proprietário (M24-T54, A-177). Save v24. Verificado no CI, não aberto no Unity.

### Adicionado

- **100 missões** que acompanham o jogo inteiro, do primeiro peixe ao Nv.1000, na ordem da lista aprovada, **todas à
  vista desde o começo**: pescar, vender, Aquário, Cardume, genes, mapas, Enciclopédia, varas, barcos, roupas, iscas,
  Passe, Expedições, Arena, Mercado, Leilão, Boss Mundial e Clã, mais as 10 sociais.
- Atalho redondo **"Missões"** na coluna da direita (depois do Passe), com a bolinha do número de missões prontas.
- Janela **"Missões"** no padrão Diário de bordo: marcadores **Em andamento / Prontas / Concluídas**, **Resgatar
  todas**, e um cartão por missão com o que fazer, a barra **"X / Y"**, o prêmio (Moedas, Conchas e, nas difíceis,
  Dólares) e **Resgatar** ou o selo **Concluída**. Aviso quando uma missão fica pronta.
- Prêmios **provisórios** (OD-073): Moedas e Conchas crescendo ao longo da lista (de 1.700 Moedas e 9 Conchas a 19
  milhões de Moedas e 1.000 Conchas) e **Dólares em 18 missões difíceis, 170 no total**.
- Duas missões de Clã com outros jogadores (Boss no mesmo dia que outro membro, top 3 de Clãs) mostram **"Liga quando o
  jogo estiver online"**; as outras sociais já contam com os jogadores simulados da Arena, do Mercado e do Leilão.
- "Ver perfil" de um adversário da Arena conta para a missão de visitar o perfil de outro jogador.
- Arte pendente: `ico_atalho_missoes` (pedido pronto em `docs/ASSETS_PENDENTES.md`); até chegar, o atalho usa o ✓.

### Mudado

- Save **v24**. Quem já jogava abre com as missões que o save já prova **prontas para resgatar** (nível, Moedas,
  Enciclopédia, mapas, equipamentos, Aquário, Cardume, Passe, clã, vendas, Expedições, duelos recentes, Boss).
- Proposta para decidir: os prêmios das missões (OD-073).

## [0.2.0-m24.14] — 13/10/2026

Genes mais fortes, decisão do proprietário. Verificado no CI, não aberto no Unity.

### Mudado

- Cada gene agora mexe de **−20% (gene 0)** a **+30% (gene 31)** no seu atributo; o gene 16 é neutro. Antes era de
  −10% a +10%. Um peixe com genes ruins fica realmente fraco e um 5 estrelas, realmente forte. Os peixes que vieram de
  saves antigos (genes 16) ficam exatamente como estavam. Na média, os atributos sobem ~2%.

## [0.2.0-m24.13] — 13/10/2026

Genes: cada peixe é único, pedido do proprietário (M24-T52, A-176). Save v23. Verificado no CI, não aberto no Unity.

### Adicionado

- Cada peixe nasce com **4 genes**, um para cada atributo (**Vida, Ataque, Defesa e Velocidade**), de **0 a 31**,
  sorteados na captura e permanentes. Cada gene muda o seu atributo de **−10% (gene 0) a +10% (gene 31)**; na média, a
  força dos peixes fica a mesma de antes.
- **Estrelas de qualidade (1 a 5)** pela soma dos genes: 5 estrelas é raro (cerca de 1 a cada 575 peixes). Gene 31 é o
  **gene perfeito**, em dourado; os 4 em 31 são o **Exemplar perfeito**.
- As estrelas aparecem no **cartão de todo peixe** (Caixa de Pesca, Aquário, Mercado, destaques, Expedição); a **ficha**
  do Aquário e do Mercado mostra "Qualidade 72%" e cada atributo com o seu gene ("Ataque 27/31").
- **Caixa de Pesca:** ordem "Melhor qualidade" e filtro "Qualidade" (5 estrelas, 4 ou mais…). **Aquário:** ordem
  "Qualidade" e botão de filtro por estrelas. **Mercado:** ordem "Melhor qualidade" e os genes no anúncio.
- Pescar um peixe de **5 estrelas** dá aviso e celebração ("Exemplar de 5 estrelas!"); um gene perfeito aparece na linha
  da captura. No "Bem-vindo de volta", os 5 estrelas entram primeiro nos destaques.
- Peixes de 5 estrelas pedem confirmação antes de uma venda em lote e antes de virar alimento.
- Os peixes do Mercado, do Leilão e da Arena também têm genes.
- O "i" (Como funciona) da Caixa de Pesca, do Aquário e do Mercado explica os genes.

### Mudado

- Save **v23**. Os peixes que você já tinha recebem genes neutros **16/16/16/16** (2 estrelas, quase a mesma força de
  antes). O preço de venda ao jogo não muda com os genes.
- Proposta para decidir: o **Temperamento** dos peixes (OD-072).

## [0.2.0-m24.12] — 13/10/2026

Relatório "Bem-vindo de volta", pedido do proprietário (M24-T51, A-175). Verificado no CI, não aberto no Unity.

### Mudado

- A tela de quando você volta virou um **relatório** no padrão "Diário de bordo": uma faixa de cena com o tempo fora
  (e o limite de 24 h, quando passou), cartões com números grandes que sobem ao abrir (**peixes pescados**, **valor da
  pesca** em Moedas se vender tudo, **XP** com "Nv. X → Y!" quando subiu de nível, **Conchas**), uma barra com os peixes
  **por raridade**, os **destaques** em cartas de peixe (com nova espécie e recorde), os **fragmentos** de cada barco,
  vara e roupa com o ícone e a barrinha até montar ("Montado!"), e os **avisos** (Caixa de Pesca cheia ou quase, com
  botão para abrir; isca que acabou; peixes que escaparam). Rola quando não cabe na tela. Esc fecha.
- Arte pendente: `bemvindo_cena` (1600×700); até chegar, o jogo desenha um lago ao entardecer.

## [0.2.0-m24.11] — 13/10/2026

Primeiro clã grátis, decisão do proprietário. Verificado no CI, não aberto no Unity.

### Mudado

- O **primeiro clã de cada jogador é grátis**. Quem apagar o clã (saindo dele vazio) e criar outro paga 50 Dólares.
  Saves que já criaram um clã contam como tendo usado o grátis.

## [0.2.0-m24.10] — 13/10/2026

Vagas do clã, decisão do proprietário. Verificado no CI, não aberto no Unity.

### Mudado

- O clã começa com 10 vagas e o dono pode comprar **até mais 5**, uma por compra, cada uma mais cara (20, 30, 45, 68 e
  101 Dólares). O máximo passou de 50 para 15 vagas.

## [0.2.0-m24.9] — 13/10/2026

Artes das roupas, dos fragmentos e do Boss Mundial, feitas pelo proprietário no ChatGPT. Verificado no CI, não aberto no Unity.

### Mudado

- Loja → Roupas mostra as 5 peças (luvas, chapéu, botas, camiseta e calça) no lugar da estrela.
- Os fragmentos dos 5 barcos, das 5 varas e o Fragmento de Roupa ganharam ícone próprio no Passe e na Loja (ao lado da
  contagem de fragmentos).
- A janela do Boss Mundial mostra o Leviatã do Abismo no lugar do disco provisório.

## [0.2.0-m24.8] — 13/10/2026

Clãs, pedido do proprietário (M24-T49, A-174, TD-055). Save versão 22. Não compilado localmente; verificado no CI. Não
aberto no Unity.

### Adicionado

- **Clãs** (sub-aba "Clã" do Perfil): Pescadores do Nv.100 para cima criam um clã (50 Dólares, nome de 3 a 20
  caracteres sem repetir) ou entram num (por id/convite; a lista de clãs aparece quando o jogo estiver online). Todo clã
  tem 10 vagas; o dono compra vagas extras com Dólares (20, 30, 45 … até 50 vagas). Quem está num clã ganha **+1% de XP
  de Pescador** em cada peixe.
- **Cardume do Clã:** cada membro coloca 1 peixe do Aquário (ele continua no Aquário e no Cardume pessoal); a Força do
  clã é a soma da Força desses peixes, com a regra do Cardume. Peixe vendido, usado como alimento ou anunciado sai do
  Cardume do Clã.
- **Ranking diário dos clãs** às 00:00 (Brasília) com prêmios para o top 3 (Dólares, Moedas, Conchas, Fragmentos de
  Roupa e fragmentos de barco, para cada membro). **Desligado no jogo local** (`clans.json →
  ranking.online_rewards_enabled`), como o top 10 do Boss Mundial; a janela avisa que liga quando o jogo estiver online.
- `config/clans.json` com os números provisórios (OD-071). Pedido de arte do ícone `ico_cla` em
  `docs/ASSETS_PENDENTES.md` (por enquanto, o ícone da Honra).

### Mudado

- Save versão 22: quem já jogava abre sem clã.

## [0.2.0-m24.7] — 13/10/2026

As varas pedem menos fragmentos, por decisão do proprietário (OD-070, opção c; M24-T48, A-173). Save continua na versão
21. Não compilado localmente; verificado no CI. Não aberto no Unity.

### Mudado

- **Varas por fragmentos:** as varas 1 a 5 pedem **5 / 8 / 12 / 15 / 20** fragmentos (eram 15 / 25 / 35 / 45 / 60). A
  chance continua 0,1% por peixe em qualquer mapa. O Passe continua dando os mesmos fragmentos de vara (3 / 4 / 6 / 7 / 9
  no grátis; o dobro no premium).
- **Quem já tinha fragmentos:** ao abrir o jogo, a vara cujos fragmentos guardados já bastam é montada (Nv.1); gasta só o
  número novo e o resto fica como extra.
- **Ritmo** (pesca + Passe grátis, sem Boss): Mapa 10 abre no dia ~1,8 / ~3,8 / ~5,8 (jogo aberto / 4 vezes por dia / 1
  vez por dia; eram ~7,7 / ~14,8 / ~16,8); Nv.1000 em ~54,8 / ~102,3 / ~109,8 dias (eram ~58,5 / ~108,8 / ~117,8); todos
  os 15 itens em ~19,0 / ~35,8 / ~39,8 dias (eram ~20,5 / ~38,3 / ~41,8).
- `tools/Progressao/fragmentos_qualquer_mapa.py` compara três regras (12/10, varas 15 a 60 e agora); os geradores
  guardam os números novos.

## [0.2.0-m24.6] — 13/10/2026

Todos os fragmentos caem em qualquer mapa, com 0,1% cada, por decisão do proprietário (M24-T47, A-173, TD-054). Save
continua na versão 21. Não compilado localmente; verificado no CI (408 testes, commit a739477). Não aberto no Unity.

### Mudado

- **Fragmentos em qualquer mapa:** cada peixe puxado (online e offline), em qualquer mapa, tem 0,1% de chance de dar 1
  fragmento de **cada** barco, de **cada** vara e do Fragmento de Roupa, em sorteios separados. Antes cada barco caía só
  em dois mapas (0,43% a 0,67%), cada vara num mapa só (1,63% a 7%) e a roupa em todos (0,17%).
- **Loja:** barcos, varas e roupas dizem "Caem em todos os mapas" e mostram a chance; as notas da Loja dizem "em qualquer
  mapa". O Boss Mundial e o Passe não mudaram.
- **Validação do config:** basta que a vara que cada mapa pede tenha chance maior que 0 (a progressão não trava).
  `rods.json → fragment_map_id` e a chance de `extra_fragment_maps` saíram.
- **Ritmo** (pesca + Passe grátis, sem Boss): todos os 15 itens em ~20,5 / ~38,3 / ~41,8 dias (jogo aberto / 4 vezes por
  dia / 1 vez por dia; antes ~11,9 / ~22,5 / ~25,8). Os mapas abrem mais tarde (Mapa 10 no dia ~7,7 / ~14,8 / ~16,8,
  antes ~0,8 / ~2,3 / ~5,8) e o Nv.1000 vem em ~58,5 / ~108,8 / ~117,8 dias (antes ~53,8 / ~100,3 / ~109,8). Decisão
  aberta: OD-070.
- Simulações: `tools/Progressao/fragmentos_qualquer_mapa.py` (novo, resultado em
  `docs/propostas/fragmentos_qualquer_mapa.json`); os geradores não escrevem mais as chances de fragmento.

## [0.2.0-m24.5] — 12/10/2026

A Tripulação e as Melhorias saíram do jogo, por decisão do proprietário (M24-T46, A-172). Save versão 21. Verificado
no CI (409 testes), não aberto no Unity.

### Removido

- **Tripulação** (os 10 tripulantes, a renda por segundo com o jogo aberto e fechado, os marcos) e **Melhorias** (as 50
  da Tripulação e as 5 gerais: Rádio do Porto, Freguesia na Feira, Caixa Térmica, Sonar de Cardume e Maré Boa), com a
  janela, a aba do menu de cima, a parte "Sua Tripulação trabalhou" do "Bem-vindo de volta", os avisos e os ícones
  `ico_tripulacao` e `ico_aba_tripulacao`. `config/crew.json` e `config/upgrades.json` saíram.

### Mudado

- **Menu de cima com 4 abas** (Pesca, Aventura, Comércio, Perfil), um pouco mais largas e centralizadas. A placa
  embaixo delas mostra só "MOEDAS" e o total.
- **Venda de peixe ao NPC** sem multiplicador (Caixa, Aquário e peixe da Expedição). O preço mínimo dos vendedores
  simulados do Mercado e do Leilão passa a ser a venda ao NPC + 1 Moeda (TD-053).
- **Curva do Pescador versão 4**: para o Nv.1000 continuar em ~60 dias com o jogo aberto (sem roupas; ~54 com roupas)
  sem a Tripulação, os níveis 250 em diante foram recalibrados (do Nv.1 ao Nv.1000: 217.677.170 XP, eram 229.596.770).
  Os níveis 1 a 249 não mudaram; ninguém perde nível.
- **Passe do Pescador e Boss Mundial**: as Moedas e Conchas dos marcos do Passe e a renda de referência do Boss foram
  regeradas pela renda só da pesca (no Nv.1000 do Passe: 3,24 mi de Moedas, eram 1,11 tri).
- Simulações: `tools/Progressao/jogador.py` é o novo jogador de referência; `simular_tripulacao.py` e
  `simular_melhorias.py` saíram.

### Migração

- **Save v20 → v21:** quem tinha tripulantes ou Melhorias recebe de volta todas as Moedas que gastou neles (pelas curvas
  de preço de antes, guardadas em `economy.json → crew_removal_refund`), com um aviso único. Saves sem Tripulação abrem
  normalmente, sem mudança.

## [0.2.0-m24.4] — 12/10/2026

Ícones das janelas no estilo novo. Verificado no CI, não aberto no Unity.

### Mudado

- As etiquetas das janelas Tripulação, Passe, Eventos e Boss Mundial usam os ícones novos do menu (mesmo desenho do
  atalho ou da aba), no lugar dos ícones provisórios.

## [0.2.0-m24.3] — 12/10/2026

Restante das artes do menu de cima, feitas pelo proprietário no ChatGPT (A-169). Verificado no CI, não aberto no Unity.

### Mudado

- Os 6 atalhos redondos da direita (Mapa, Aquário, Passe, Eventos, Boss Mundial, Ranking) ganharam os ícones novos e a
  moldura de madeira com aro dourado. Com isso, todas as 17 artes do novo visual estão no jogo.

## [0.2.0-m24.2] — 12/10/2026

Primeiras artes do novo visual, feitas pelo proprietário no ChatGPT (A-169, A-170). Verificado no CI, não aberto no Unity.

### Mudado

- As janelas "Diário de bordo" usam as artes de verdade: moldura de madeira, folha de papel, etiqueta azul do título,
  lacre de cera de fechar e marcador das abas (tingido com a cor de cada aba).
- As 5 abas do menu de cima (Pesca, Tripulação, Aventura, Comércio, Perfil) ganharam os ícones novos.
- Faltam os ícones dos atalhos redondos e a moldura deles; até chegarem, ficam os ícones atuais.

## [0.2.0-m24.1] — 12/10/2026

Revisão de integridade do jogo inteiro (M24-T45), pedida pelo proprietário. A versão passa a seguir o milestone atual
(M24), como registrado em `docs/VERSIONAMENTO.md`. Verificado no CI (460 testes), não aberto no Unity.

### Corrigido

- **Mercado:** comprar peixe de bot e vender ao NPC não dá mais lucro garantido. O preço mínimo dos bots é a venda ao
  NPC com a Freguesia na Feira no máximo + 1 Moeda (TD-052); os peixes de nível baixo dos bots ficam um pouco mais caros.
- **Somas grandes** (Conchas da pesca, Expedição, revenda de vara, Loja da Arena, anúncio de moeda cancelado, cargas de
  isca) não estouram mais.
- **Save:** arquivo sem `save_version` é lido como versão 1; anúncio de Conchas/Dólares com quantidade zero ou negativa é
  recusado; a cópia de segurança leva milissegundos no nome.
- **Teste do Leilão** que falhava às vezes (M24-T26) ficou estável.
- **Telas:** os atalhos redondos continuam clicáveis com uma janela aberta (o clique troca de janela); selos e rótulos
  coloridos ficam legíveis no papel; Mercado no tamanho das outras janelas; "Bem-vindo de volta" com rolagem; avisos e
  dica do tutorial não cobrem mais as janelas; "+1 Fragmento de Roupa" no singular; menos trabalho por quadro.
- **Documentação:** README, base técnica, regras de config, roadmap e segurança descrevem o jogo de hoje.

### Adicionado

- Nota discreta quando o relógio do computador está atrás do horário salvo (o jogo fica parado até ele alcançar).

## [0.2.0-m21.61] — 12/10/2026

Janelas no padrão "Diário de bordo", escolhido pelo proprietário (M24-T44, A-170). Verificado no CI, não aberto no Unity.

### Mudado

- Todas as janelas viram uma página de caderno: moldura de madeira com papel claro, título numa etiqueta azul com borda
  dourada, lacre vermelho "✕" para fechar e a cena escurecida atrás. O "i" de "Como funciona" fica na etiqueta.
- As sub-abas viram marcadores coloridos na lateral direita (Ranking, Aquário, Arena, Tripulação, Mercado, Loja e
  Perfil).
- Texto escuro no papel, com contraste de pelo menos 4,5:1 (tinta 10,9:1; secundário 6,4:1; turquesa 5,8:1; dourado
  5,7:1). Cartões, chips e botões secundários viram papel; o botão principal continua turquesa.
- Cenas próprias (tanque, arquipélago, carta náutica, parede da Loja, palco do Ranking, formações, batalha, Boss)
  ficam como uma foto presa na folha, com a arte e o texto claro de antes.
- Arte provisória da moldura, do papel, da etiqueta, do lacre e dos marcadores (`ASSET_PENDENTE`), com os pedidos para o
  ChatGPT em `docs/ASSETS_PENDENTES.md`.

## [0.2.0-m21.60] — 12/10/2026

Menu de cima: abas grandes com placa embaixo e atalhos redondos à direita (M24-T43, A-169). Verificado no CI, não aberto no Unity.

### Mudado

- A barra de cima virou uma faixa fina com **5 abas grandes coloridas**: Pesca, Tripulação, Aventura (Expedição, Arena e
  Eventos), Comércio (Mercado e Loja) e Perfil. Com uma janela de Aventura ou Comércio aberta, as irmãs aparecem como
  sub-abas logo abaixo da aba.
- O **cartão do jogador** (retrato, nome, nível, Moedas, Conchas e Dólares) fica à esquerda da faixa; um clique abre o Perfil.
- Uma **placa de madeira pendurada** no centro mostra a renda por segundo da Tripulação (dourada) e o total de Moedas.
- **Atalhos redondos** em coluna à direita: Mapa, Aquário, Passe do Pescador, Eventos (vira o Boss Mundial dourado,
  pulsando e com o tempo que falta, durante o evento) e Ranking, com os pontos e contagens de sempre.
- Sino e opções à direita da faixa; avisos à direita, embaixo da faixa; tutorial à esquerda, apontando para as abas novas.

### Removido

- O logo, o nome do mapa e o cartão grande do jogador à direita (os dados continuam no Perfil).

### Arte pendente

- Ícones das 5 abas e dos atalhos e a moldura redonda (`ASSET_PENDENTE`, pedidos em `docs/ASSETS_PENDENTES.md`).

## [0.2.0-m21.59] — 12/10/2026

Volta para 5 barcos; os mapas 6 a 10 dão fragmentos dos barcos 1 a 5 (M24-T42, A-168, TD-051). Verificado no CI, não aberto no Unity.

### Mudado

- Os barcos são de novo 5 (Remo Valente, Rastro Azul, Proa Selvagem, Costa Nobre, Horizonte Dourado), além da canoa
  inicial. Cada um cai em dois mapas: o de antes (mesma chance) e um dos mapas 6 a 10 (com a chance que o mapa já tinha).
- A Loja diz "Caem nos mapas X e Y" e mostra a chance do mapa em que você está.
- Passe do Pescador: fragmentos só dos 5 barcos, revezando (Nv.60 volta ao Remo Valente). Algumas recompensas em Moedas
  mudaram um pouco com o arquivo regerado.

### Removido

- Os barcos Vela do Sol, Quilha Azul, Canto da Baleia, Farol Noturno e Titã do Atlântico (e os pedidos de arte deles).

### Migração

- Save v20. Quem tinha fragmentos dos barcos 6 a 10 recebe fragmentos do barco 1 a 5 do mesmo mapa; um barco 6 a 10 já
  montado vira as Moedas e Conchas que os níveis dele custaram, mais os fragmentos que ele custou; se estava em uso, o
  jogo passa a usar o seu melhor barco.

## [0.2.0-m21.58] — 12/10/2026

Menu Eventos na barra de cima, com o Boss Mundial dentro (M24-T39, A-167). Verificado no CI, não aberto no Unity.

### Adicionado

- Botão **Eventos** (ícone + texto) no menu da barra de cima, com um ponto dourado e um brilho suave quando um evento
  está acontecendo (o Boss das 19:00 às 19:10).
- Janela **Eventos**: lista rolável de cartões. O cartão do Boss Mundial mostra o nome do Boss, o horário, a contagem
  regressiva ou "Acontecendo agora · faltam mm:ss", a sua faixa de hoje, o último resultado e o botão "Abrir".
- Na janela do Boss, o botão **‹ Eventos** volta à lista.

### Mudado

- O botão só de ícone do Boss (com a contagem) saiu da barra; a contagem fica dentro do menu Eventos.
- Com 11 menus, a barra primeiro aperta os botões (menos margem e espaço) e só depois passa a mostrar só ícones.

## [0.2.0-m21.57] — 12/10/2026

Correções na abertura do jogo (M24-T38, A-166, TD-050): o proprietário avisou que o jogo "não está abrindo" no Unity 6.3.
Compilado e testado no CI; não aberto no Unity.

### Corrigido

- Uma falha ao montar a cena interrompia a abertura antes da interface: a janela ficava vazia, sem explicação. Agora a
  interface é sempre criada e mostra a tela de erro.
- Uma falha ao desenhar a interface (qualquer janela) se repetia a cada quadro e deixava a tela pela metade. Agora o
  jogo para com a tela de erro.
- A leitura do estado da pesca e da viagem a cada quadro ficou protegida como as outras.

### Mudado

- A tela de erro mostra o erro e o tipo, o momento (abertura, cena, tela, jogo em andamento), onde no código, o erro
  interno e a pasta do save, e pede um print.

### Adicionado

- Testes `StartupWithOldSavesTests`: saves das versões 11 a 18 com dados reais (varas Nv.10 no Inventário, no Mercado e
  num leilão, iscas antigas, peixes Nv.10, barco comprado, Dólares antigos) abertos 3 dias depois, com todas as
  sincronizações do jogo (Pesca, Tripulação, Passe, Boss, Expedição, Mercado); e um save novo. Nada lança exceção, o save
  carrega e a abertura leva menos de 2 s.

## [0.2.0-m21.56] — 12/10/2026

Boss Mundial diário (M24-T29, A-165, TD-049): pedido do proprietário de 12/10/2026 ("quero criar um Boss mundial para
todos conseguirem atacar, todo dia às 19:00 …"). Verificado no CI, não aberto no Unity.

### Adicionado

- **Boss Mundial** (`config/world_boss.json`): todo dia das 19:00 às 19:10, horário de Brasília. Vida infinita; nome
  sugerido "Leviatã do Abismo" (OD-069). Com o jogo aberto o Cardume ataca sozinho a cada 3 s (até 200 ataques), com a
  regra de dano da Arena; com o jogo fechado nada conta. Cardume vazio ou em Expedição não ataca.
- 6 faixas de dano (5% a 95% do dano máximo do dia) com 5 a 60 minutos de renda em Moedas e Conchas, até 3 Fragmentos
  de Roupa e até 3 fragmentos do barco do mapa mais alto liberado; pagas uma vez, às 19:10 ou na próxima abertura.
- Ranking do dia por dano e os Dólares do top 1 a 10 (50, 40, 30, 25, 20, 15, 10, 10, 5, 5), desligados no jogo local
  (`online_rewards_enabled`) até existir servidor.
- HUD: botão do Boss à esquerda do Passe, com a contagem até as 19:00 e brilho durante o evento. Janela "Boss Mundial":
  Cardume x Boss com números de dano, dano de hoje, barra e cartões das faixas, sua recompensa, "Ranking de hoje",
  Dólares do top 10; fora do horário, a contagem e o último resultado. Avisos de começo, faixa e pagamento.
- Save versão 19 (`world_boss`). `tools/Progressao/boss_mundial.py` (renda de referência a partir do `pass.json`).
  Testes `WorldBossTests`. Arte provisória (pedidos em `docs/ASSETS_PENDENTES.md`).

### Mudado

- `BattleEngine`: o cálculo de um golpe saiu para `RollDamage` e `HitDamage` (mesma sequência de sorteios; as batalhas
  da Arena não mudam).
- OD-067 resolvida pelo proprietário ("sem problema na questão da calça, isso é pensado"): a curva do Pescador fica como
  está (M24-T35).

## [0.2.0-m21.55] — 12/10/2026

Varas e roupas por fragmentos (M24-T32 e M24-T33, A-163 e A-164, TD-048): decisões do proprietário de 12/10/2026 ("Vai
dropar fragmentos de equipamentos … coloca fragmento de roupa … Vai ter luva, calça, bota, chapéu, camiseta.").
Verificado no CI, não aberto no Unity.

### Adicionado

- **Varas por fragmentos**: cada vara cai no último mapa que a vara anterior alcança (Vara 1 no Lago Sereno, 2 no
  Pantanal Dourado, 3 na Costa de Coral, 4 na Corrente Azul, 5 no Talude Noturno), 15 a 60 fragmentos, 1,63% a 7% por
  peixe puxado, online e offline; com todos a vara é montada sozinha no Nv.1 e entra em uso se for de categoria maior.
  Calibrado para quem abre o jogo 4 vezes por dia montar cada vara no mesmo dia em que comprava antes. O jogo recusa um
  config em que alguma vara ou mapa ficasse impossível (a progressão nunca trava).
- **Roupas** (`equipment.json → clothing`): Luvas (chance de puxar, +0,5% → +15%), Chapéu (raridade, +2% → +80%), Botas
  (tamanho, +2% → +60%), Camiseta (Conchas, +5% → +175%) e Calça (XP de Pescador, +0,5% → +10%), do Nv.1 ao Nv.100. Um
  tipo só de Fragmento de Roupa (0,17% por peixe puxado, em todos os mapas); o jogador escolhe a peça; a 1ª custa 15, depois
  25, 40, 60 e 80. As 5 ficam vestidas juntas e sobem de nível com o uso (1 ponto por peixe puxado, sem custo). O teto de 95%
  não muda.
- Loja: aba **Roupas** (entre Barcos e Iscas) com as 5 peças, nível, barra de uso, bônus agora e no Nv.100 e "Montar (N
  fragmentos de roupa)"; "Seu equipamento" diz "Total com Luvas" quando elas contam.
- Avisos agrupados (no máximo um por tipo a cada 8 s) de fragmentos de barco, de vara e de roupa e dos níveis das roupas;
  aviso de "Você já pode montar uma roupa"; comemoração de vara e roupa montadas; linhas novas no "Bem-vindo de volta".
- Save versão 18 (`rod_fragments`, `clothing`): quem já joga mantém as varas com o nível; fragmentos começam em 0.
- Painel de Balanceamento: fragmentos e chance de cada vara (aba Varas) e a seção Roupas (Sucesso da pesca).
- `tools/Progressao/fragmentos_equipamento.py` (gerar e simular) e `docs/propostas/fragmentos_equipamento.json`.
- Testes `RodsAndClothingByFragmentsTests`.

### Mudado

- As varas **saíram da Loja**: a aba Varas mostra "Fragmentos 12/25 · Caem no mapa …", a barra, a chance e "MONTE COM
  FRAGMENTOS"; o Caniço Manso continua "Pegar grátis" no tutorial. O valor das varas no Mercado e na revenda é o preço de
  antes (`acquisition.market_reference_coins`); uma vara montada devolve na revenda só 25% do que foi gasto nos níveis.
- Passe do Pescador: fragmentos de vara nos marcos Nv.10, 30, 50, 70 e 90 (15% da vara; Premium 30%) e Fragmentos de Roupa
  em todo marco (2 grátis até o Nv.100 e 1 depois; Premium o dobro). `pass.json` regerado por `passe_fragmentos.py gerar`
  (Moedas dos marcos altos menores: Nv.1000 1,95 tri → 1,11 tri grátis).
- Com as roupas, o Pescador sobe mais rápido: Nv.1000 em ~55 dias com o jogo aberto (era ~60; OD-067).
- `simular_progressao.py`: varas por fragmentos, roupas e a pesca offline no mapa onde o jogo foi fechado.

## [0.2.0-m21.54] — 12/10/2026

Curva do Pescador: Nv.1000 em ~60 dias (M24-T31, A-162, TD-047): decisão do proprietário de 12/10/2026 ("60 dias pro
lvl 1000"). Verificado no CI, não aberto no Unity.

### Mudado

- `progression.json → fisher`: curva de XP versão 3 (`xp_curve_version` 2 → 3). Níveis 1 a 99 iguais; do Nv.100 em
  diante cada nível pede mais (Nv.500 178.000 → 224.000 XP; Nv.999 408.000 → 504.000); do Nv.1 ao Nv.1000, 188.953.530
  → 229.596.770 XP. Jogo aberto com a Tripulação e as Melhorias (sem Passe e sem VIP): Nv.10 em ~20 min e Nv.100 em
  ~0,5 dia (iguais), Nv.500 em ~14,6 dias (era ~12,2), Nv.1000 em ~60 dias (era ~50); 4 vezes por dia, Nv.1000 em ~108
  (era ~91); 1 vez por dia, ~124 (era ~105). `calibration_target` e as notas reescritos.
- Saves das curvas 1 e 2 convertidos pelo XP total ao abrir o jogo, sem nunca baixar nível (TD-038).
- `pass.json` regerado por `tools/Progressao/passe_fragmentos.py gerar`: Dólares e fragmentos iguais; Moedas e Conchas
  dos marcos altos maiores (Nv.1000: 838 bi → 1,95 tri Moedas grátis), porque o jogador de referência chega mais tarde.
- `tools/Progressao/calibrar_xp.py` mede com a economia atual (pesca a cada 10 s, Tripulação, Melhorias, níveis de vara
  e barco, fragmentos, iscas) e mantém os níveis do começo (`--manter-ate`).
- `docs/propostas/melhorias.json` e `docs/propostas/passe_fragmentos.json` regerados.
- OD-058 resolvido (M24-T15 concluída pela curva nova).

### Adicionado

- Testes `FisherCurveSixtyDaysTests` (versão 3, total da curva, começo igual, migração 1 → 3 e 2 → 3 sem baixar nível).

## [0.2.0-m21.53] — 12/10/2026

Três iscas (+5%, +10%, +15%) com 999 tentativas (M24-T30, A-161, TD-046): decisão do proprietário de 12/10/2026.
Verificado no CI, não aberto no Unity.

### Adicionado

- `equipment.json → bait_migration`: versão do modelo de iscas (2) e as iscas de antes, só para converter saves antigos.
- Save versão 17 (`bait_model_version`): ao abrir, cada isca antiga com tentativas vira a nova de bônus igual ou o mais
  próximo acima, com as tentativas pelo valor em Moedas (arredondado para cima); a isca em uso continua em uso.
- `tools/Progressao/simular_iscas.py` e `docs/propostas/iscas_retorno.md`: custo por hora e retorno de cada isca no
  começo, no meio e no fim.
- Testes `ThreeBaitsTests`.

### Mudado

- Iscas: Terra Viva **+5%** (era +3%), Maré Viva **+10%** (era +7%), Ouro de Maré **+15%** (era +10%); **999
  tentativas** por compra (eram 180); **23.000 Moedas + 13 Conchas**, **100.000 + 33** e **200.000 + 55** (eram 1.500 + 1,
  6.000 + 3 e 15.000 + 5): por tentativa, ~10% a 20% mais caro que antes pelo mesmo bônus.
- Validação: todo nível de vara e de barco precisa custar Conchas (`upgrade_cost.shells_first` ≥ 1; já era assim em
  todos).

## [0.2.0-m21.52] — 12/10/2026

Pesca a cada 10 s, VIP a cada 15 s fora do jogo e Caixa para 1 dia (M24-T27, A-160, TD-045): decisão do proprietário de
12/10/2026.

### Adicionado

- `economy.json → vip.offline_cycle_seconds` (15): com o VIP ativo, a pesca com o jogo fechado faz 1 tentativa a cada
  15 s em vez de 20 s; um VIP que acaba enquanto o jogador está fora conta 15 s só até o fim. O +50% de XP offline do
  VIP continua (decisão pendente OD-065). Validação (≥ 1 quando presente).
- A aba VIP da Loja e o "Bem-vindo de volta" mostram o ritmo da pesca offline com e sem VIP.
- Testes `TenSecondCycleTests`: ciclos de 10/20/15 s, VIP terminando no meio do tempo fora, um dia fora com VIP (5.760)
  cabendo na Caixa, config sem ciclo de VIP, validação, tela do VIP, raros por hora iguais aos de antes em 4 cenários
  (online e offline), simulação com semente fixa, Caixa de 8.640, iscas de 180 e fragmentos por hora.
- Decisão pendente OD-065 (M24-T28): o bônus de XP do VIP continua junto com o ciclo mais rápido?

### Mudado

- **1,5× peixes por hora**: uma tentativa a cada 10 s online (era 15 s) e a cada 20 s offline (era 30 s).
- **Raros por peixe a 2/3, os mesmos por hora**: `fishing_bite_multiplier` de 0,35 para 0,233333 (por exemplo, o Raro
  no fim do jogo morde 10,2% das vezes, era 15,2%, e continua 34,7 por hora).
- Caixa de Pesca de 3.000 para **8.640** peixes (1 dia de pesca online); iscas de 120 para **180** tentativas por
  compra (mesmo preço, mesmos 30 min online).
- Chance de fragmento de barco de 0,64%–1,03% para **0,43%–0,67%** por peixe puxado (mesmo ritmo de montagem).
- `config/pass.json` regerado pelo script: Nv.10 2.460 Moedas e 9 Conchas (era 1.950 e 6), Nv.1000 838 bi e 206
  Conchas (era 1,11 tri e 138).
- Ganhos por hora de pesca: XP ×1,49 / ×1,42 / ×1,13 e Moedas ×1,48 / ×1,40 / ×1,10 (começo / meio / fim).
- Tempos simulados com a Tripulação e as Melhorias (aberto / 4 vezes / 1 vez por dia): Nv.100 em 0,5 / 1,3 / 3,8 dias
  (era 0,7 / 1,8 / 3,8), Nv.500 em 12,5 / 23,5 / 29,8 (era 14,3 / 26,9 / 33,8), Nv.1000 em 50,5 / 91,3 / 105,8 dias
  (era 56,6 / 102,3 / 118,8).
- `tools/Progressao/simular_progressao.py --vip` simula também o ciclo offline do VIP; `docs/propostas/` (melhorias,
  tripulação, níveis e passe) regerados.

### Verificação

- Verificado no CI (GitHub) no commit dc5e45c: 383 testes passando e 0 erros no jogo, no Painel e no Simulador. Não
  aberto no Unity: a pesca a cada 10 s na
  cena e as telas da Loja e do "Bem-vindo de volta" precisam do Play do proprietário.

## [0.2.0-m21.51] — 11/10/2026

### Corrigido

- O Unity não abria o projeto: erro de compilação na Tripulação (CS1501). Testes desatualizados ajustados.

### Adicionado

- Verificação automática no GitHub a cada envio (TD-044). Resultado: 365 testes passando, jogo, Painel e Simulador
  compilando sem erros.

## [0.2.0-m21.50] — 11/10/2026

Mais peixes, menos raros (M24-T22, A-159, TD-043): decisão do proprietário de 11/10/2026.

### Adicionado

- `progression.json → rarity.tiers[].fishing_bite_multiplier` (0,35 no Raro, Épico, Lendário e Mítico): na pesca,
  a chance de cada raridade morder é multiplicada por ele depois dos pesos do mapa e do bônus de raridade da vara, e os
  Comuns completam o resto. `CatchRules.RollBite`, `FishingWeights` e `BiteShare`; validação (não pode ser negativo).
- Painel de Balanceamento → Raridades: coluna "Na pesca".
- Testes `MoreFishFewerRaresTests`: ciclo na metade, 70% dos raros por hora por raridade em 4 cenários (começo, meio,
  fim e fim sem nível), simulação com semente fixa, 240 tentativas por hora online e 2.880 num dia offline, fragmentos
  por hora iguais aos de antes, Expedições sem o multiplicador, Enciclopédia e validação.
- Decisões pendentes OD-063 (ganhos da pesca no fim do jogo caem com menos raros) e OD-064 (Excepcional e Perfeição
  vêm o dobro por hora).

### Mudado

- **O dobro de peixes por hora**: uma tentativa a cada 15 s online (era 30 s) e a cada 30 s offline (era 60 s); a
  chance de puxar e o limite de 24 h offline não mudaram.
- **Raros por peixe a 35%** e por hora a ~70% de antes em cada raridade (só na pesca; Expedições e vendedores do
  Mercado continuam iguais). A Enciclopédia mostra o "morde X%" da pesca, com duas casas abaixo de 0,1%.
- Caixa de Pesca de 1.500 para **3.000** peixes; iscas de 60 para **120** tentativas por compra (mesmo preço).
- Chance de fragmento de barco de 1,3%–2,4% para **0,64%–1,03%** por peixe puxado (mesmo ritmo de montagem); a Loja e o
  Catálogo mostram duas casas abaixo de 1%.
- `config/pass.json` regerado com a economia nova (agora é exatamente o que o script gera; o anterior não batia do
  Nv.300 em diante): Nv.10 1.950 Moedas e 6 Conchas (era 930 e 3), Nv.1000 1,11 tri e 138 Conchas (era 844 bi e 69).
- Tempos simulados com a Tripulação e as Melhorias (aberto / 4 vezes / 1 vez por dia): Nv.100 em 0,7 / 1,8 / 3,8 dias
  (era 1,1 / 2,4 / 4,8), Nv.1000 em 56,6 / 102,3 / 118,8 dias (era 50,5 / 91,5 / 105,8). Nv.10 em ~30 min online.
- `tools/Progressao`: `simular_progressao.py` aplica o multiplicador; `passe_fragmentos.py` grava a chance com 4 casas;
  `passe_fragmentos.py`, `simular_melhorias.py`, `simular_tripulacao.py` e `niveis_100.py` imprimem com vírgula
  decimal. `docs/propostas/` (melhorias, tripulação, níveis e passe) regerados. O simulador C# mostra a raridade da
  mordida na pesca.
- Texto do Painel de Testes: "Aberto: pesca online (1 tentativa a cada ciclo de pesca)" (antes citava 30 s).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity: os testes novos e ajustados não rodaram; a
  pesca a cada 15 s na cena, a Loja e a Enciclopédia precisam do Play do proprietário para serem declaradas verificadas.

## [0.2.0-m21.49] — 11/10/2026

Etapa 5 do novo escopo idle: barcos por fragmentos (M24-T16, A-157) e o Passe do Pescador (M24-T17, A-158).

### Adicionado

- **Barcos por fragmentos**: 10 barcos, um por mapa (os 5 de antes e 5 novos com nomes sugeridos: Vela do Sol, Quilha
  Azul, Canto da Baleia, Farol Noturno e Titã do Atlântico). Cada peixe puxado no mapa do barco tem 1,3% a 2,4% de
  chance de dar 1 fragmento (também na pesca offline; Expedição não dá); 20 a 200 fragmentos montam o barco sozinho,
  que entra no Nv.1 e passa a ser usado se for melhor. Fragmentos depois de montado ficam guardados como extras.
  Avisos de fragmento e de barco montado (com comemoração) e linha no "Bem-vindo de volta".
- **Passe do Pescador**: botão novo na barra de cima (à esquerda do sino, com um ponto dourado contando as recompensas
  prontas) e janela com o nível, o próximo marco, "Comprar Premium (100 Dólares)", "Resgatar tudo" e a trilha de 100
  marcos (Nv.10 ao Nv.1000) com as fileiras Grátis e Premium. Recompensas: Moedas, Conchas, Dólares e fragmentos de
  barco. O Premium libera também os marcos já passados. Aviso ao alcançar cada marco.
- `config/pass.json` (novo, validado); `config/equipment.json`: `boat_fragments` e, por barco, `map_id`,
  `fragments_required` e `fragment_drop_chance`; 5 barcos novos.
- `GameService/Shop/BoatFragmentRules.cs`, `GameService/Pass/LocalPassService.cs` (`IPassService`), `UI/PassWindow.cs`;
  erros do Passe ("Você ainda não chegou ao nível deste marco do Passe.", "Você já resgatou esta recompensa.", …).
- Save versão 16 (`BoatFragments`, `Pass`) (TD-042).
- `tools/Progressao/passe_fragmentos.py` (gera as chances e o passe e simula o ritmo; resultado em
  `docs/propostas/passe_fragmentos.json`).
- Testes `BoatFragmentsAndPassTests`: os 10 barcos, fragmento por captura com o Rng do serviço (mesmo seed, mesmo
  resultado), montar o barco, extras, pesca offline, barco fora da Loja, resgate, Premium com Dólares e retroativo,
  sem Dólares em dobro com o A-111, migração v15→v16 e validação do config.
- Pedidos de arte em `docs/ASSETS_PENDENTES.md`: barcos 6 a 10, ícones de fragmento e ícone do Passe.
- Decisões pendentes OD-059 (temporadas do Passe), OD-060 (fragmentos extras), OD-061 (nomes dos barcos novos) e
  OD-062 (ritmo dos barcos para quem não volta aos mapas e força do Premium).

### Mudado

- **Barcos saíram da Loja**: a aba Barcos mostra "12/20" na etiqueta e, na ficha, "Fragmentos 12/20 · Caem no mapa …",
  a barra, a chance por peixe e "MONTE COM FRAGMENTOS" no lugar do Comprar. Barco seu: nível, Melhorar e Usar como
  antes. `IGearService.BuyBoat` saiu; `cost_coins` e `cost_shells` saíram dos barcos.
- **Dólares por nível no Passe**: subir de nível não paga mais Dólares sozinho; os 10 a cada 10 níveis até o Nv.100
  (A-111) estão na faixa grátis do Passe; depois do Nv.100, 10 a cada 100 níveis (grátis) e 10 a cada 50 (Premium),
  como sugestão. `progression.json → fisher.dollars_per_levels` saiu. OD-055 resolvido.
- Quem já joga: os barcos comprados continuam seus, com o nível; fragmentos começam em 0; os marcos até o nível atual
  ficam para resgatar, sem os Dólares do A-111 que já foram pagos (até o Nv.100).
- Aviso ao trocar de barco: "Barco em uso: …" (antes "Agora você pesca com o …", que não servia para os nomes
  femininos dos barcos novos).
- Painel: aba Sucesso da pesca com fragmentos e chance por barco (no lugar do preço); Catálogo com os fragmentos.
- Simulador (`tools/Simulador`) e `tools/Progressao/simular_progressao.py` montam os barcos por fragmentos;
  `tools/Progressao/niveis_100.py gerar` foi bloqueado (gravaria os barcos de antes).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity: a Loja, a janela do Passe, o botão novo,
  os avisos e o "Bem-vindo de volta" precisam do Play do proprietário para serem declarados verificados.

## [0.2.0-m21.48] — 10/10/2026

Etapa 4 do novo escopo idle: varas, barcos e peixes até o Nv.100 (M24-T13, A-156).

### Adicionado

- **Barcos com nível** (Nv.1 ao Nv.100): cada barco comprado começa no Nv.1 com o bônus de antes e chega a 3× no
  Nv.100; cada nível custa Moedas e Conchas (o 1º, 5% do preço do barco). O barco inicial não tem nível. Botão
  "Melhorar → Nv. N" na ficha do barco na Loja, com o nível ("Nível 12/100"), o próximo nível e o preço.
- Na Loja, a ficha da vara que você tem mostra "Nível 37/100", os bônus de agora e "Melhorar → Nv. 38" com o preço.
- `config/rods.json`: `upgrade_rules.bonus_curve_exponent`, `level_curve_version`; por vara `bonuses_at_level_1`,
  `bonuses_at_max_level`, `upgrade_cost` e `legacy_levels_v1`. `config/equipment.json`: `boat_levels` e, por barco,
  `has_levels`, `catch_success_bonus_at_max_level`, `upgrade_cost`. `config/market_bots.json`: `supply.max_rod_level`.
- `GameService/Config/LevelCurve.cs` (curva e preço dos níveis), `GameService/Persistence/LevelCurveMigration.cs`,
  `IGearService.UpgradeBoat`, erros "Este barco já está no nível máximo." e "Este barco não tem níveis para melhorar."
- Save versão 15 (`BoatLevels`, `RodLevelCurveVersion`, `FishLevelCurveVersion`) (TD-041).
- `tools/Progressao/niveis_100.py` (gera os números e simula o ritmo; resultado em `docs/propostas/niveis_100.json`)
  e `tools/Progressao/curva_niveis.py` (as mesmas contas do jogo, para as simulações).
- Testes `ItemLevelsTests`: curva e arredondamento dos preços, varas até o Nv.100, teto de 95%, atributos do peixe,
  melhorar barco (custos, erros, bônus na pesca, save), ficha da Loja, migração v14→v15 sem perder força.
- Decisões pendentes OD-057 (M24-T14: subir o nível do peixe deve custar Moedas e Conchas?) e OD-058 (M24-T15: com o
  equipamento no Nv.100 o Pescador chega ao Nv.1000 mais cedo: ~50 dias em vez de ~70 com a Tripulação, ~54 em vez
  de ~75 só pescando, jogo aberto).

### Mudado

- **Varas até o Nv.100** (antes Nv.10): Nv.1 igual a antes, Nv.100 com 3× o bônus do Nv.10 de antes, passos pequenos
  no começo. O 1º nível custa um terço do de antes em Moedas e um quarto em Conchas, e cada nível 14% a mais em Moedas
  e 3% a mais em Conchas (Vara 1: 500 Moedas + 1 Concha no 1º nível, antes 1.500 + 2).
- **Peixes até o Nv.100** (antes Nv.10): bônus de atributo de 0 no Nv.1 a +108% no Nv.100 (antes +4% por nível, +36%
  no Nv.10); XP por nível pequeno no começo (5 XP); 2.465 XP até o Nv.41 (a força do Nv.10 de antes, que pedia
  3.885 XP) e 36.160 XP até o Nv.100. Continua sem custo em Moedas ou Conchas.
- Arena: os adversários mais fortes têm peixes no Nv.41 (antes 10). Mercado: peixes dos vendedores até o Nv.26 (antes
  6), prêmio de 3,37% por nível (antes 15%), varas até o Nv.41. Aviso de comida valiosa a partir do Nv.8 (antes 2).
- Quem já joga: cada vara e cada peixe vão para o nível novo com bônus igual ou maior (por exemplo Vara 1 Nv.10 →
  Nv.41, peixe Nv.10 → Nv.41); barcos começam no Nv.1.
- Perfil: bônus do barco e da vara com uma casa decimal quando não é inteiro; a dica do barco mostra o nível.
- Painel de Balanceamento: varas com Nível 1 / nível máximo / custo, barcos com bônus máximo e custo de nível, peixe
  com bônus no nível máximo e expoente.
- `tools/Progressao/simular_progressao.py` lê o formato novo (níveis de vara e barco até o Nv.100, o mais barato
  primeiro); nos simuladores da Tripulação e das Melhorias um nível é comprado quando custa até 10 minutos de renda.

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity: a Loja, o Perfil e o Painel precisam do
  Play do proprietário para serem declarados verificados.

## [0.2.0-m21.47] — 10/10/2026

Etapa 3 do novo escopo idle: Melhorias compradas (M24-T06 e M24-T11, A-155).

### Adicionado

- **Melhorias da Tripulação**: 5 por tripulante (Anzol Afiado, Linha Reforçada, Rede Nova, Motor Revisado e Mapa dos
  Cardumes), liberadas com 1, 10, 25, 50 e 100 unidades dele, compradas uma vez, cada uma ×2 na renda de Moedas
  daquele tripulante. Preço: 10× a 6.250× o preço da unidade daquele número.
- **Melhorias gerais com níveis**: Rádio do Porto (+25% na renda da Tripulação por nível, 10 níveis), Freguesia na
  Feira (+10% no preço de venda ao NPC de todo peixe, 10 níveis), Caixa Térmica (+2 h no teto offline da Tripulação,
  5 níveis), Sonar de Cardume (+5% no XP da Tripulação, 10 níveis) e Maré Boa (+10% nas Moedas da venda da Caixa de
  Pesca, 10 níveis). Nenhuma Melhoria pede nível do Pescador.
- `config/upgrades.json` (novo, validado) e `tools/Progressao/simular_melhorias.py` (resultado em
  `docs/propostas/melhorias.json`).
- Aba "Melhorias" na janela da Tripulação (abas "Tripulação | Melhorias"): as 3 Melhorias de tripulante mais baratas,
  as 5 gerais com o próximo nível, "Compradas" recolhível e avisos de compra. Ícones provisórios (pedidos em
  `docs/ASSETS_PENDENTES.md`).
- Save versão 14 (sem nenhuma Melhoria para quem já joga) (TD-040).
- Testes das Melhorias (`UpgradeTests`): liberação por quantidade, compra única, níveis das gerais, efeitos na renda
  e no XP da Tripulação, na venda de peixe da Caixa e do Aquário (Mercado sem mudança), no teto offline, crédito da
  renda antes da compra, sem nível mínimo, números enormes sem estouro, migração v13→v14.
- Decisão pendente OD-056 (M24-T12): a Maré Boa e a Freguesia na Feira fazem quase o mesmo.

### Mudado

- `config/crew.json`: renda por unidade recalibrada para as Melhorias (cada tripulante ~4,6× o anterior, antes
  ~6,5×; o Ajudante da Isca não mudou). Simulado, jogo aberto: 1 mi/s no dia 14,4 (era 12,7), 1 bi/s no dia 62,7
  (era 62), ~3,3 bi/s no dia 75 e Nv.1000 em 70,5 dias (era 71).
- O "renda ×N" de cada tripulante conta também as Melhorias dele; o teto offline mostrado inclui a Caixa Térmica.
- A venda da Caixa de Pesca soma com teto no maior número (não estoura com preços multiplicados).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.46] — 10/10/2026

Etapa 2 do novo escopo idle: a Tripulação automática (M24-T05 e M24-T08, A-154).

### Adicionado

- **Tripulação**: 10 tripulantes (do Ajudante da Isca ao Navio-Fábrica) contratados com Moedas, que rendem Moedas e
  XP de Pescador por segundo com o jogo aberto e fechado; nunca pescam peixes. Cada um libera com 10 do anterior.
  Comprar ×1, ×10, ×100 ou Máx. Marcos de 25 a 400 unidades dobram a renda do tripulante; o marco da frota (todos
  os 10 com o número) dobra a de todos. Fechado: 100% nas primeiras 2 h e 50% até 10 h.
- `config/crew.json` com os números de partida, simulados por `tools/Progressao/simular_tripulacao.py`
  (resultado em `docs/propostas/tripulacao.json`): jogo aberto — 1ª compra em 0,6 min, 2º tripulante em 7,1 min, a
  Tripulação passa a pesca em 1,5 h, ~2,9 mi/s perto do Nv.500 e ~1,1 bi/s no dia 75.
- Janela "Tripulação" (botão novo na barra de cima, logo depois de Pesca), "+X/s" no cartão do jogador e a parte
  "Sua Tripulação trabalhou" no "Bem-vindo de volta". Retratos e ícone provisórios (pedidos em
  `docs/ASSETS_PENDENTES.md`).
- Save versão 13 (Tripulação vazia para quem já joga; o tempo de antes não rende) (TD-039).
- Testes da Tripulação (`CrewTests`): preços ×1/×10/×100/Máx e números enormes, desbloqueio, marcos e marco da frota,
  renda aberta e fechada com 100%/50% e teto, relógio que volta, migração v12→v13, XP no total do Pescador, nenhum
  peixe.

### Mudado

- O XP do Pescador da pesca e da Tripulação passa pelo mesmo `FisherLevelRules.AddXp` (o comportamento da pesca não
  mudou).
- O contador de Moedas do cartão fica exato também em bilhões (antes arredondava por usar `float`).
- M24-T08 virou "Tela da Tripulação"; a tela das Melhorias ficou numa tarefa nova, M24-T11.

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.45] — 10/10/2026

Etapa 1 do novo escopo idle (M24, A-152 e A-153).

### Mudado

- Varas, barcos e iscas não têm mais nível mínimo para comprar: basta ter as Moedas e as Conchas (M24-T09). A Loja
  não mostra mais o cadeado "Nv. X", a linha "Disponível no Nível X" nem os itens escurecidos por nível. Os mapas
  continuam liberando nos mesmos níveis do Pescador.
- O Pescador vai até o Nv.1000 (antes Nv.100), com uma curva de XP nova (M24-T03): Nv.10 em ~1 h, Nv.100 em ~1 dia,
  Nv.500 em ~17,5 dias e Nv.1000 em ~75 dias com o jogo aberto o dia todo (4 vezes por dia: ~2,3, ~34 e ~144 dias;
  1 vez por dia: ~4,8, ~41 e ~168 dias).
- Os 10 Dólares a cada 10 níveis continuam só até o Nv.100; depois do Nv.100 é decisão do proprietário (OD-055).
- Saves na versão 12: quem já joga sobe para o nível que o XP total acumulado alcança na curva nova, nunca descendo,
  e recebe os Dólares dos marcos cruzados até o Nv.100 (TD-038).
- `tools/Progressao/calibrar_xp.py` calibra a curva para metas de tempo por nível (até o Nv.1000);
  `simular_progressao.py` e o simulador em C# compram itens sem nível.
- Painel de Testes: atalhos "Nv. 500" e "Nv. 1000". Painel de Balanceamento e Catálogo sem o nível dos itens.

### Corrigido

- `Format.Short` não quebra mais com o menor número possível (`long.MinValue`).
- O milestone M23 (ilha 2D, descartada) voltou ao `docs/roadmap.json` só com a tarefa entregue, para a numeração dos
  milestones seguir em ordem (o teste `Milestones_are_numbered_in_order_from_0` falhava sem ele).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.44] — 09/10/2026

### Removido

- Zoom da cena: a câmera mostra sempre a cena inteira; sem aproximar pela roda do mouse nem pelas Configurações (A-151).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.43] — 09/10/2026

### Mudado

- Loja com a arte do proprietário: parede de tábuas, ganchos de latão, tampo do balcão, caixa de pesca e boias.

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.42] — 09/10/2026

### Mudado

- Loja com o visual "Balcão do Píer": parede de ganchos azul-noite com as varas, barcos e iscas deitados e uma
  etiqueta pendurada em cada um (preço, "JÁ É SUA"/"JÁ É SEU", "EM USO" ou cadeado com "Nv. X"); abas como
  plaquinhas penduradas na viga; no centro, o balcão do píer com o item escolhido sobre um pedestal com aura; à
  direita, a ficha numa carta de moldura de corda com os bônus em barras (Nível 1 → máximo) e Comprar / Usar /
  Guardar. Ao escolher, o item sai do gancho e faz um arco curto até o pedestal (0,6 s, uma vez) e a ficha desliza da
  direita. Embaixo, "Seu equipamento" numa caixa de pesca aberta e a "Chance de puxar o peixe" como 5 boias na água,
  com as raridades que não mordem aqui deitadas e cinza. A frase de cada aba e a explicação da chance foram para o
  "i" da placa. Todas as informações e ações de antes continuam; nenhuma regra ou número muda (A-150).
- Arte opcional da Loja pedida em `docs/ASSETS_PENDENTES.md` (parede, gancho, balcão, caixa e boia), com o script
  `tools/Arte/processar_loja.py`; enquanto não chega, o jogo desenha tudo no código.

### Não verificado

- Não compilado (sem .NET) e não aberto no Editor.

## [0.2.0-m21.41] — 08/10/2026

### Mudado

- Texto mais legível no jogo todo: o texto suave ficou mais claro (`#B4C6DB`, contraste de pelo menos 5:1 em todos
  os painéis, cartões e botões) e o principal um branco suave (`#EEF3F8`); os rótulos pequenos passaram de 13 para
  15 px, chips e selos de 12–13 para 14 px e as etiquetas de 11 para 13 px, com a Nunito SemiBold no lugar da
  Regular. Sombra leve só no texto sobre a arte (chegada ao mapa e batalha da Arena). Alturas de linha e caixas
  ajustadas em todas as janelas para nada cortar nem se sobrepor (A-149).

### Não verificado

- Não compilado (sem .NET) e não aberto no Editor.

## [0.2.0-m21.40] — 08/10/2026

### Mudado

- Cabeçalho "Placa do Píer" em todos os menus com a moldura comum (Expedição, Ranking, Mercado, Loja, Arena,
  batalha da Arena, Mapa, Aquário e Caixa de Pesca): placa de tábuas azul-escuras com borda turquesa, pendurada por
  cordas, com o ícone e o título, que balança uma vez ao abrir. A frase que explicava o menu foi para o botão "i"
  ("Como funciona"); contadores e tempo ficam num fundo escuro ao lado da placa. Na Expedição, a Força do Cardume
  subiu para a faixa do cabeçalho e a carta náutica ganhou a altura liberada (A-148).

### Não verificado

- Não compilado (sem .NET) e não aberto no Editor.

## [0.2.0-m21.39] — 08/10/2026

### Mudado

- Expedição com a arte do proprietário: carta náutica com os destinos em medalhões (mais longe do píer quanto
  mais longa a viagem), painel em carta de missão com o selo "Chance de peixe", bússola com o tempo restante
  durante a viagem e o relatório de chegada sobre a cena do porto (A-147).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.38] — 08/10/2026

### Mudado

- Menu Ranking com a arte do proprietário: abas com emblemas, pódio no píer com coroa, placar com escudos e
  "Sua posição" fixa no rodapé (A-146).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.37] — 08/10/2026

### Adicionado

- Aquário: botão "Tanque | Cartas"; o modo cartas mostra cada peixe numa ficha de herói (pedestal, aura, nível,
  atributos em barras, Alimentar e Vender) com a lista de cartas ao lado (A-145).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.36] — 08/10/2026

### Mudado

- Aquário vivo: tanque com areia, plantas e bolhas, os peixes nadando, ficha num painel de vidro e gaveta com
  todos os peixes (A-144).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.35] — 08/10/2026

### Mudado

- Enciclopédia como álbum de cartas: abas por mapa, páginas de cartas e ficha da espécie com pedestal, recorde,
  faixa de tamanho, chance de mordida e atributos base (A-143).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.34] — 08/10/2026

### Mudado

- Arte definitiva da barra superior, dos botões do menu, do logo, da moldura do avatar e das moedas (A-142).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.33] — 08/10/2026

### Mudado

- Tela de Mapa estilo arquipélago: cada mapa é uma ilha-medalhão na rota do barco, do turquesa ao azul-noite (A-141).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.32] — 08/10/2026

### Mudado

- Viagem entre mapas em 10 segundos (antes 30), com o barco acompanhando (A-140).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.31] — 08/10/2026

### Mudado

- Ícones definitivos do menu, desenhados pelo proprietário (A-139).

### Não verificado

- Não aberto no Editor do Unity.

## [0.2.0-m21.30] — 08/10/2026

### Mudado

- Som ambiente com gravações reais de natureza em cada mapa; sai o loop sintetizado do mar (A-137, TD-036).
- Abismo Atlântico mais claro, sem a Via Láctea em "V" e sem as listras no topo do céu, com reflexo da lua (A-138).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity; o som não foi ouvido no jogo.

## [0.2.0-m21.29] — 07/10/2026

### Mudado

- Inventário estilo mochila, com ficha do item ao passar o mouse (A-135).
- Barra superior maior e com cara de jogo; cartão do jogador à direita com Moedas, Conchas e Dólares (A-136).

### Adicionado

- Agentes do projeto em `.claude/agents/`: código, visual e ideias.

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.28] — 07/10/2026

### Mudado

- Arena com cara de jogo: cabeçalho de jogador, Duelo com pedestais e os dois Cardumes frente a frente, tela VS
  antes da luta (A-134).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.27] — 07/10/2026

### Mudado

- Arena: adversários do mais fácil (esquerda) ao mais forte (direita) (A-133).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.26] — 07/10/2026

### Mudado

- Evoluir um peixe até o Nv. 10 custa 3.885 XP (antes 995) e toda espécie vale a mesma base como alimento:
  cerca de 100 Comuns de qualquer mapa, igual para todos os jogadores (A-132).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.25] — 07/10/2026

### Corrigido

- Textos: concordância, plurais, maiúsculas das moedas, números formatados e termos em inglês (A-131).
- Sobreposições: Arena e comemorações fora de 1080p, preço da Loja, HUD (chances, avisos, valor exato),
  Mercado, Perfil (Inventário, Cardume, Resumo), Aquário, Expedição, Caixa, Bem-vindo de volta e painel de Testes.
- Busca: o ✕ agora limpa o campo. Navegação travada com diálogo aberto.
- Avatar padrão, Ctrl + clique no Aquário, aviso de Caixa cheia no painel de Testes e validação do avatar ao salvar.

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.24] — 07/10/2026

### Adicionado

- Aquário: selecionar vários peixes e vender de uma vez, com total e confirmação (A-130).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.23] — 07/10/2026

### Adicionado

- Painel de Desenvolvimento: aba Catálogo com cards de peixes, mapas, varas, barcos, iscas e Expedições.
- Nome e avatar do jogador (arte dos avatares pedida) e "Ver perfil" do adversário na Arena (A-128, A-129).

### Mudado

- Aba Cardume com cara de jogo; o selo de raridade não sobrepõe mais o número da posição (A-129).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.22] — 07/10/2026

### Adicionado

- Enciclopédia por mapa, com progresso, mapa e raridade das espécies ainda não descobertas (A-126).
- Avisos importantes guardados entre sessões (A-127); versão do balanceamento nos resultados notáveis.

### Mudado

- Raridade sempre com o nome junto da cor (A-125).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.21] — 07/10/2026

### Mudado

- Números grandes em formato curto ("12,4 mi"), com o valor completo ao passar o mouse (A-124).
- Jogo menor: arte de cenário, paisagem viva, barcos e varas com crunch; peixes, ícones e varas mais nítidos
  quando pequenos; o mapa anterior sai da memória ao viajar (TD-035). Texturas do build: ~160 → ~75 MB.

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.20] — 07/10/2026

### Adicionado

- Ferramentas de teste do proprietário (F2, só no Editor): dar recursos e peixes, mudar o nível, encher a
  Energia e avançar o tempo com o jogo aberto ou fechado (A-123, TD-034).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.19] — 07/10/2026

### Adicionado

- Cancelar a Expedição em andamento: o Cardume volta na hora, sem recompensa (A-122).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.18] — 07/10/2026

### Adicionado

- Perfil como central do jogador: cabeçalho com carteira, nível e VIP; aba Resumo com Arena, Cardume, equipamento,
  espaço, Expedição e coleção por raridade (A-121).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.17] — 07/10/2026

### Corrigido

- Cards de peixe, Enciclopédia e Arena: nenhum texto passa por cima de outro; textos longos terminam em "…" (A-120).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.16] — 07/10/2026

### Adicionado

- Caixa de Pesca: ordens Maiores, Mais raros e Por espécie, num menu com ícone e explicação (A-119).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.15] — 07/10/2026

### Mudado

- Caixa de Pesca com abas de raridade, menus de Tamanho e Ordem e barra de ocupação (A-118).

### Corrigido

- Caixa de Pesca: o aviso do limite e as vagas do Aquário não se sobrepõem mais.

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.14] — 07/10/2026

### Mudado

- Caixa de Pesca com limite de 1.500 peixes; cheia, a pesca para até vender. Avisos na Caixa, no painel de pesca,
  no Bem-vindo de volta e no tutorial (A-117).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.13] — 07/10/2026

### Mudado

- Arena: perder uma defesa tira 2 de Honra (A-116).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.12] — 07/10/2026

### Adicionado

- Loja da Arena: Saco de Conchas e Bolsa de Dólares por Honra (A-115).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.11] — 07/10/2026

### Mudado

- Expedições crescem com o mapa: ~metade do que a pesca offline renderia no mesmo tempo (A-114).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.10] — 07/10/2026

### Adicionado

- Ranking da Arena com pódio dos 3 primeiros e páginas de 10 até o top 100 (A-112).

### Corrigido

- Selos de raridade: sem o risco no meio do nome; Lendário e Mítico com brilho suave (A-113).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.9] — 07/10/2026

### Mudado

- Loja: as varas aparecem em lista, como barcos e iscas, com o nome inteiro à vista (M22-T19).

### Corrigido

- Loja: o texto sobre a chance de puxar não se sobrepõe mais às barras em telas baixas.

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.8] — 07/10/2026

### Adicionado

- Dólares jogando: 10 Dólares a cada 10 níveis de Pescador (100 no Nv.100, o preço de um VIP) (A-111).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.7] — 07/10/2026

### Adicionado

- VIP: 100 Dólares do jogo por 30 dias de +50% de XP de Pescador na pesca offline, na Loja, aba VIP
  (A-110). Comprar Dólares com dinheiro real fica para quando houver loja (M22-T17).

### Não verificado

- Não compilado (sem .NET neste ambiente) e não aberto no Editor do Unity.

## [0.2.0-m21.6] — 07/10/2026

### Mudado

- Tabela de XP do Pescador recalibrada de novo, por decisão do proprietário: quem deixa o jogo aberto o dia
  todo chega ao Nv.100 em ~10 dias; abrindo 4 vezes por dia, ~19 dias; 1 vez por dia, ~24 dias. Níveis 1 a 9
  e XP dos peixes iguais (A-109).
- `tools/Progressao/calibrar_xp.py` ganhou `--perfil` (padrão: jogo aberto o dia todo).

### Não verificado

- Não aberto no Editor do Unity.

## [0.2.0-m21.5] — 07/10/2026

### Mudado

- Tabela de XP do Pescador recalibrada: do Nv.1 ao Nv.100 em cerca de 10 dias para quem abre o jogo
  4 vezes por dia (antes, 3 a 6 dias). Níveis 1 a 9 iguais; XP dos peixes igual (A-109).

### Adicionado

- `tools/Progressao/simular_progressao.py` e `calibrar_xp.py`, e o relatório
  `docs/relatorios/SIMULACAO_PROGRESSAO_NV100.md` (TD-033).

### Não verificado

- Não aberto no Editor do Unity.

## [0.2.0-m21.4] — 07/10/2026

### Adicionado

- Vulto gigante do Abismo Atlântico (Abismo 12): a arte dos Mapas 5 a 10 está completa.

### Não verificado

- Não aberto no Editor do Unity.

## [0.2.0-m21.3] — 06/10/2026

### Adicionado

- Arte final do Talude Noturno, do Abismo Atlântico e da Soberana Abissal, e o céu e os 4 peixes que
  faltavam na Corrente Azul: todos os 100 peixes e as 6 varas com arte final (A-108).
- Paisagem viva noturna: petréis e vultos de peixes grandes passando sob a água.
- `tools/Arte/processar_mapas_9_10.py` (fundo verde, céu noturno continuado com cópias espelhadas).

### Não verificado

- Não compilado nem aberto no Editor do Unity; conferido só em prévias montadas fora do Unity.

## [0.2.0-m21.2] — 06/10/2026

### Adicionado

- **Mapas 7 a 10** (Corrente Azul, Banco das Baleias, Talude Noturno e Abismo Atlântico), com 40
  espécies novas (100 no catálogo), a raridade **Mítico** e as varas **Atlântico Nobre** e **Soberana
  Abissal**. O jogo vai do Nv.1 ao Nv.100 (A-106).
- Arte final dos Mapas 7 e 8 e da Vara 4, e o céu e as nuvens que faltavam nos Mapas 5 e 6. Paisagem
  viva nova: peixes-voadores, pardelas, sargaço boiando e a baleia-jubarte respirando e saltando.
- Mapas 9 e 10 com cenário noturno provisório (céu estrelado, lua, plâncton brilhando) até a arte chegar.
- Pedido de arte dos Mapas 9 e 10 e da Vara 5 (31 imagens).
- `tools/Arte/processar_mapas_7_8.py` (separa os peixes pela forma, mesmo quando um encosta no outro),
  `tools/Arte/otimizar_png.py` e `gerar_cenarios.py` com cenário de mar aberto e noturno.

### Corrigido

- Voltar o relógio do PC não dá mais pesca offline, Energia ou Expedição de graça (TD-030).
- O Mercado simulado não paga por uma vara mais do que ela custa nova; vara anunciada conta como sua.
- Save editado não deixa peixe ou vara acima do nível máximo nem usar barco não comprado (A-107).

### Mudado

- Imagens importadas no tamanho que a tela mostra e PNG recomprimidos sem perdas: 20 MB a menos no
  projeto e build/memória de texturas estimados de ~209 para ~135–140 MB (TD-031).
- 15 quadros por segundo com a janela em segundo plano (TD-032).

### Não verificado

- Não compilado (sem .NET neste ambiente) nem aberto no Editor do Unity. Cenário conferido só em prévias
  montadas fora do Unity. Conferir no Play se alguma imagem ficou borrada com o teto novo.

## [0.2.0-m21.1] — 05/10/2026

### Adicionado

- **Mapa 5 · Costa de Coral** (Nv.40) e **Mapa 6 · Arquipélago do Sol** (Nv.50), com 20 espécies novas
  (60 no catálogo), a raridade **Lendário** (o Veleiro) e a **Vara 3 · Corrente Mestra** (A-105).
- Arte final dos dois mapas: cenário, peixes, Corrente Mestra (Loja e cena), gaivota, tartaruga-marinha,
  atobá, fragata, golfinho, coqueiros, restinga e vegetação das ilhas. Céu da Costa de Coral e nuvens dos
  dois mapas ainda provisórios.
- `tools/Progressao/aplicar_mapas.py` (coloca mapas da proposta em `/config`),
  `tools/Arte/processar_mapas_5_6.py` (recorta o pacote do ChatGPT) e `gerar_cenarios.py <Mapa>`
  (provisório só dos mapas pedidos).
- Pedido de arte dos Mapas 7 e 8 e da Vara 4 (com as 3 imagens que faltaram).

### Não verificado

- Não compilado (sem .NET neste ambiente) nem aberto no Editor do Unity. Cenário conferido só numa
  prévia montada fora do Unity.

## [0.2.0-m17.8] — 05/10/2026

### Adicionado

- Arte final dos pedidos 33 a 36: o pescador agora segura a vara com as duas mãos (os punhos são
  desenhados por cima da vara), as três varas da cena são finas e retas, as iscas Terra Viva, Maré Viva
  e Ouro de Maré aparecem penduradas na linha, e os Dólares ganharam o ícone final.
- `tools/Arte/equipamento_na_cena.py --pescador` e `--varas` para recortar essas imagens.
- Roadmap: tudo que o GDD pede e ainda não está no jogo entrou como proposta para o proprietário
  analisar (M11-T10 a T13, M12-T16 a T26, M18 a M20), marcado com `from_gdd_review`.

### Não verificado

- Não compilado (sem .NET neste ambiente) nem aberto no Editor do Unity. Conferido só numa prévia
  montada fora do Unity.

## [0.2.0-m17.7] — 02/10/2026

### Mudado

- Nomes próprios no equipamento (A-103): Caniço Manso, Ponta Selvagem, Maré Dourada; Água Mansa,
  Remo Valente, Rastro Azul, Proa Selvagem, Costa Nobre, Horizonte Dourado; Terra Viva, Maré Viva,
  Ouro de Maré. Cada um com uma frase curta na Loja.
- Barcos liberam nos níveis 10, 20, 30, 50 e 80, com preços novos (12.000 a 2.500.000 Moedas);
  iscas com 60 tentativas por 1.500, 6.000 e 15.000 Moedas.

### Não verificado

- Não aberto no Editor do Unity ainda.

## [0.2.0-m17.6] — 02/10/2026

### Adicionado

- Menu **Fishing Idle → Gerar versão para amigos (Windows)**: gera o jogo, um LEIA-ME para o
  jogador e o zip para enviar, na pasta `dist` (TD-029). Passo a passo em
  `docs/ENVIAR_PARA_AMIGOS.md`.

### Não verificado

- Não rodado no Editor do Unity ainda (o build depende do módulo Windows Build Support).

## [0.2.0-m17.5] — 02/10/2026

### Adicionado

- Zoom da cena (A-102): rodinha do mouse sobre a cena ou "Aproximar" em Configurações, até cerca de
  20% mais perto do barco. Fica guardado neste PC.

### Mudado

- Ranking na barra de menus do topo, ao lado da Arena (A-100). Avisos e Opções viraram só ícone para
  os nove menus caberem com nome.

### Não verificado

- Não aberto no Editor do Unity ainda.

## [0.2.0-m17.4] — 02/10/2026

### Adicionado

- Aba **Conchas e Dólares** no Mercado (A-101): anunciar Conchas ou Dólares por Moedas e cancelar. No
  jogo local ninguém compra ainda; as ofertas de outros jogadores chegam com o online.

### Não verificado

- Não aberto no Editor do Unity ainda.

## [0.2.0-m17.3] — 02/10/2026

### Adicionado

- Menu **Ranking** (A-100): Nível, Moedas, Conchas e Peixes pescados, só jogadores reais. No jogo
  local aparece só você; pronto para o online. Botão ao lado das Conchas e dos Dólares.

### Não verificado

- Não aberto no Editor do Unity ainda.

## [0.2.0-m17.2] — 02/10/2026

### Mudado

- Toda compra e melhoria de equipamento pede Conchas (A-099): Vara 1 com 5, Vara 2 com 40, melhorias
  de 2 a 40 por nível, barcos de 5 a 250, iscas de 1 a 12.
- Conchas um pouco mais raras: 7% por peixe puxado (antes 10%). A Vara Inicial agora também dá
  Conchas.

## [0.2.0-m17.1] — 02/10/2026

### Adicionado

- Moeda **Dólares** (A-098): começa em zero, sem ganho nem uso por enquanto (OD-021). Conchas e
  Dólares aparecem sempre numa faixa abaixo das Moedas, no alto à direita. Save versão 11.
- Pedido para o GPT com nomes próprios de varas, barcos e iscas (`docs/PEDIDO_NOMES_EQUIPAMENTO.md`).

### Não verificado

- Não aberto no Editor do Unity ainda.

## [0.2.0-m16.30] — 02/10/2026

### Mudado

- Iscas bem mais caras: 1.000, 6.000 e 4.000 + 12 Conchas (A-097).
- Ajuste geral novo do preço de venda dos peixes (`npc_fish_sale.price_multiplier`, no Painel de
  Balanceamento). Esteve em 0,5 por algumas horas e voltou a 1: os peixes vendem pelo preço de antes.

### Adicionado

- Pedidos para o GPT: pescador segurando a vara e varas finas para a cena (`M16-T13`).

## [0.2.0-m16.29] — 01/10/2026

### Mudado

- XP para subir de nível um pouco menor entre os níveis 20 e 50, para voltar às metas dos mapas 3 e 4
  depois da redução dos bônus (A-095): 20→30 em 3,8 h e 30→40 em 4,0 h de pesca online.

### Adicionado

- O jogo já aceita a pintura das iscas (`Resources/Arte/Iscas`); até ela chegar, continua a forma
  colorida. Pedido para o GPT em `docs/PEDIDO_ARTE_ISCAS.md`.

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
