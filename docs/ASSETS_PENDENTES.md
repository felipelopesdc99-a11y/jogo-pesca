# Artes pendentes (`ASSET_PENDENTE`)

Lista de toda arte do jogo que hoje é **provisória**: foi pintada por script para o jogo já ficar
bonito e coerente, mas deve ser trocada pela arte final produzida pelo proprietário (processo da
Bíblia de Arte, seção 39). Tarefa `M14-T14` no roadmap.

> **Situação em 29/09/2026:** os 32 pedidos chegaram e já estão no jogo (arte final). Ainda
> provisórios: 29 dos 47 ícones (os que não estavam no pedido 26, mais a lupa `ico_buscar` da busca por
> nome e, desde 30/09, `ico_barco` e `ico_isca` da Loja), a água dos dois mapas (feita por
> script, com os reflexos desenhados pelo jogo a partir das camadas) e as camadas `bg_near` antigas,
> que só aparecem se as margens novas faltarem.
>
> Para colocar no jogo uma leva nova de imagens do ChatGPT: `python3 tools/Arte/processar_pedidos.py
> <pasta com os Pedido_XX_*.png>`. Tudo que ele grava entra em `tools/Arte/finais.txt`, e os geradores de
> arte provisória nunca sobrescrevem esses arquivos.

Plantas soltas que balançam e animais de cenário (aves, garça, capivara, tartaruga, sapo, patos,
jacaré, macacos), pedidos no documento
[Fishing Idle — Pedidos de arte: paisagem viva](https://claude.ai/code/artifact/d385fa64-2459-4fb0-aae8-c22ab6454965),
**chegaram (Vivo 01 a 18) e estão no jogo** em `Resources/Arte/Vivos` (`Plantas/` e `Animais/`). Para uma
leva nova: `python3 tools/Arte/processar_vivos.py <pasta com os Vivo_XX_*.png>`. Onde cada um aparece e
com que frequência fica em `Resources/Visual/paisagem_viva.json`.

Os mapas 3 e 4 (Pantanal Dourado e Estuário das Marés) e a Vara 2 estão pedidos no documento
[Fishing Idle — Pedidos de arte: Pantanal Dourado e Estuário das Marés](https://claude.ai/code/artifact/71e40506-b9ce-48dd-bf16-d9409c2a9442)
(`ASSET_PENDENTE`, tarefa `M14-T29`): cenário em camadas, 10 peixes de cada mapa, fauna e plantas vivas
de cada um. Jacaré, capivara e garça do Pantanal reaproveitam a paisagem viva que já existe.

Os pedidos prontos para colar no ChatGPT, na ordem certa, estão no documento
[Fishing Idle — Pedidos de arte para o ChatGPT](https://claude.ai/code/artifact/320602da-0a64-4e9b-b729-4108aef7656e).
Mande as imagens geradas no chat; o recorte e a padronização ficam por minha conta.

## Como trocar uma arte

1. Produza a imagem seguindo a Bíblia de Arte (`docs/ART_BIBLE_V0_1.md`) e o checklist da seção 41
   (peixes) ou 42 (telas).
2. Salve como **PNG com fundo transparente**, com **o mesmo nome** e **a mesma proporção** (largura ÷
   altura) do arquivo atual. O tamanho em pixels pode ser maior.
3. Coloque o arquivo na mesma pasta, por cima do antigo, e abra o Unity. A importação é automática
   (`ArtImportSettings`); nada no código precisa mudar.
4. Aperte Play e tire um print para revisar (etapa 4 da seção 39).

Se um arquivo for apagado, o jogo não quebra: ele volta para o desenho simples feito por código.

Todos os caminhos abaixo são relativos a `client-unity/Assets/Resources/`.

---

## Prioridade A — precisa agora (seção 38)

### Peixes do Mapa 1 — Lago Sereno

Vista lateral, olhando para a direita, peixe inteiro ocupando cerca de 90% da largura, centralizado,
sem texto, sem brilho embutido, sem sombra no chão.

| Arquivo | Espécie | Tamanho | Onde aparece |
|---|---|---|---|
| `Arte/Peixes/fish_lambari_master.png` | Lambari | 1024×512 (placeholder) → **2048×1024** final | Cena, cards, avisos, Enciclopédia |
| `Arte/Peixes/fish_tilapia_master.png` | Tilápia | 1024×512 (placeholder) → **2048×1024** final | Cena, cards, avisos, Enciclopédia |
| `Arte/Peixes/fish_piau_master.png` | Piau | 1024×512 (placeholder) → **2048×1024** final | Cena, cards, avisos, Enciclopédia |
| `Arte/Peixes/fish_cascudo_master.png` | Cascudo | 1024×512 (placeholder) → **2048×1024** final | Cena, cards, avisos, Enciclopédia |
| `Arte/Peixes/fish_curimbata_master.png` | Curimbatá | 1024×512 (placeholder) → **2048×1024** final | Cena, cards, avisos, Enciclopédia |
| `Arte/Peixes/fish_traira_master.png` | Traíra | 1024×512 (placeholder) → **2048×1024** final | Cena, cards, avisos, Enciclopédia |
| `Arte/Peixes/fish_pacu_master.png` | Pacu | 1024×512 (placeholder) → **2048×1024** final | Cena, cards, avisos, Enciclopédia |
| `Arte/Peixes/fish_matrinxa_master.png` | Matrinxã | 1024×512 (placeholder) → **2048×1024** final | Cena, cards, avisos, Enciclopédia |
| `Arte/Peixes/fish_carpa_master.png` | Carpa | 1024×512 (placeholder) → **2048×1024** final | Cena, cards, avisos, Enciclopédia |
| `Arte/Peixes/fish_tambaqui_master.png` | Tambaqui | 1024×512 (placeholder) → **2048×1024** final | Cena, cards, avisos, Enciclopédia |

### Cenário do Lago Sereno

As camadas são empilhadas de trás para frente, com parallax. **Cada camada cobre 26 unidades de
largura** (a tela mostra 19,2 × 10,8 unidades em 16:9; a sobra cobre telas mais largas e o movimento
da câmera). A linha do horizonte fica a 0,2 unidade acima do centro da tela. O sol está pintado em
x = 5,3, y = 1,55 (unidades), e o jogo põe os brilhos da água embaixo dele.

| Arquivo | O que é | Tamanho atual | Posição no mundo |
|---|---|---|---|
| `Arte/Mapas/LagoSereno/map_lago_sereno_bg_sky.png` | Céu com o sol baixo e nuvens altas | 1248×278 | base em y = 0,0, até y = 5,8 |
| `Arte/Mapas/LagoSereno/map_lago_sereno_bg_far.png` | Montanhas distantes (transparente acima do recorte) | 1820×175 | base em y = 0,1 |
| `Arte/Mapas/LagoSereno/map_lago_sereno_bg_mid.png` | Morros com mata | 2340×144 | base em y = 0,1 |
| `Arte/Mapas/LagoSereno/map_lago_sereno_near_left.png` e `_near_right.png` | Margens esquerda e direita (pedidos 5 e 6) | livre | presas às bordas da tela; altura 3 e 2,5 unidades |
| `Arte/Mapas/LagoSereno/map_lago_sereno_water.png` | Água com reflexos e a coluna de luz do sol | 1664×384 | topo no horizonte (y = 0,2), até y = −5,8 |
| `Arte/Mapas/LagoSereno/map_lago_sereno_fg_left.png` | Juncos, pedras e vitórias-régias do canto esquerdo | 500×420 (5 × 4,2 unidades) | preso ao canto inferior esquerdo da tela |
| `Arte/Mapas/LagoSereno/map_lago_sereno_fg_right.png` | O mesmo, canto direito | 500×420 | preso ao canto inferior direito |
| `Arte/Mapas/LagoSereno/map_lago_sereno_cloud_01.png` a `_03.png` | Nuvens que andam devagar | 520×260 (5,2 unidades) | céu, y entre 2,6 e 4,7 |
| `Arte/Mapas/LagoSereno/map_lago_sereno_thumb.png` | Foto do mapa na tela Mapa | 720×288 | card do Mapa |

### Pescador, barco e efeitos de captura

| Arquivo | O que é | Tamanho atual | Observação |
|---|---|---|---|
| `Arte/Cena/barco.png` | Barco de madeira de lado | 720×200 (3,6 unidades) | o ponto de apoio fica a 30% da altura, de baixo para cima |
| `Arte/Cena/pescador.png` | Pescador sentado, de costas/lado, **sem o braço da vara** | 320×520 (0,8 unidade) | o braço e a vara são animados pelo jogo; a parte de baixo fica escondida atrás da borda do barco |
| `Arte/Cena/caixa_de_pesca.png` | Caixa de pesca no barco | 160×110 | — |

Os efeitos de captura (aura, faíscas, ondas na água, faixa de celebração) são feitos por código e
ajustados pelo `tema_visual.json`; não precisam de arquivo.

### Ícones

45 arquivos em `Arte/Icones/ico_<nome>.png`, 96×96, **brancos sobre transparente** (o jogo pinta na
cor certa). Exceções coloridas: `ico_moeda.png` e `ico_concha.png`. Um traço só, cantos redondos.
Gerados por `tools/Arte/gerar_icones.py`; a lista completa de nomes está no script.

---

## Prioridade B

### Peixes do Mapa 2 — Rio Selvagem

| Arquivo | Espécie | Tamanho | Onde aparece |
|---|---|---|---|
| `Arte/Peixes/fish_piranha_master.png` | Piranha | 1024×512 (placeholder) → **2048×1024** final | Cena, cards, avisos, Enciclopédia |
| `Arte/Peixes/fish_piracanjuba_master.png` | Piracanjuba | 1024×512 (placeholder) → **2048×1024** final | Cena, cards, avisos, Enciclopédia |
| `Arte/Peixes/fish_peixe_cachorra_master.png` | Peixe-cachorra | 1024×512 (placeholder) → **2048×1024** final | Cena, cards, avisos, Enciclopédia |
| `Arte/Peixes/fish_tucunare_master.png` | Tucunaré | 1024×512 (placeholder) → **2048×1024** final | Cena, cards, avisos, Enciclopédia |
| `Arte/Peixes/fish_cachara_master.png` | Cachara | 1024×512 (placeholder) → **2048×1024** final | Cena, cards, avisos, Enciclopédia |
| `Arte/Peixes/fish_dourado_master.png` | Dourado | 1024×512 (placeholder) → **2048×1024** final | Cena, cards, avisos, Enciclopédia |
| `Arte/Peixes/fish_pintado_master.png` | Pintado | 1024×512 (placeholder) → **2048×1024** final | Cena, cards, avisos, Enciclopédia |
| `Arte/Peixes/fish_jau_master.png` | Jaú | 1024×512 (placeholder) → **2048×1024** final | Cena, cards, avisos, Enciclopédia |
| `Arte/Peixes/fish_pirarucu_master.png` | Pirarucu | 1024×512 (placeholder) → **2048×1024** final | Cena, cards, avisos, Enciclopédia |
| `Arte/Peixes/fish_aruana_master.png` | Aruanã | 1024×512 (placeholder) → **2048×1024** final | Cena, cards, avisos, Enciclopédia |

### Cenário do Rio Selvagem

Mesmas regras do Lago Sereno, com os arquivos `map_rio_selvagem_*` em `Arte/Mapas/RioSelvagem/`. O sol
está em x = −4,5, y = 3,9 (alto, sem coluna de luz na água). A cachoeira está pintada na camada
`bg_far`, em x = 3,4.

| Arquivo | Tamanho atual |
|---|---|
| `map_rio_selvagem_bg_sky.png` | 1248×278 |
| `map_rio_selvagem_bg_far.png` (paredões e cachoeira) | 1820×259 |
| `map_rio_selvagem_bg_mid.png` | 2340×207 |
| `map_rio_selvagem_bg_near.png` | 2600×355 |
| `map_rio_selvagem_water.png` | 1664×384 |
| `map_rio_selvagem_fg_left.png`, `_fg_right.png` | 500×420 |
| `map_rio_selvagem_cloud_01.png` a `_03.png` | 520×260 |
| `map_rio_selvagem_thumb.png` | 720×288 |

### Varas

| Arquivo | Vara | Tamanho | Onde aparece |
|---|---|---|---|
| `Arte/Varas/rod_00_starter.png` | Vara Inicial | 1024×512 | Loja, Mercado |
| `Arte/Varas/rod_01.png` | Vara 1 | 1024×512 | Loja, Mercado |

Vara na diagonal, do canto inferior esquerdo ao superior direito, com molinete visível.

### Barcos (V0.2, `M15-T07`)

| Arquivo | Barco | Tamanho | Onde aparece |
|---|---|---|---|
| `Arte/Barcos/boat_00.png` | Barco Inicial | 1024×512 | Loja → Barcos |
| `Arte/Barcos/boat_01.png` a `boat_05.png` | Barcos 1 a 5 | 1024×512 | Loja → Barcos |

Barco de lado, fundo transparente, do mais simples (canoa de madeira) ao mais completo, sem fantasia
(Bíblia de Arte, seção 27). Enquanto o arquivo não existe, a Loja mostra o ícone de barco. O barco
da cena continua o mesmo para todos: trocar o barco da cena pelo barco em uso é uma decisão futura.

### Expedições

| Arquivo | Expedição | Tamanho |
|---|---|---|
| `Arte/Expedicoes/exp_30m.png` | Saída Rápida | 600×390 |
| `Arte/Expedicoes/exp_1h.png` | Volta na Margem | 600×390 |
| `Arte/Expedicoes/exp_3h.png` | Águas Profundas | 600×390 |
| `Arte/Expedicoes/exp_6h.png` | Viagem Longa | 600×390 |

### Arena e Mercado

Hoje usam os ícones e os cards de peixe; não há arte própria pendente. Retratos dos adversários da
Arena ficam como ícone até existir um sistema de avatar (não faz parte da V0.1).

---

## Mapas 3 e 4 (V0.2)

> **Situação em 01/10/2026:** as imagens do documento
> [Pedidos de arte: Pantanal Dourado e Estuário das Marés](https://claude.ai/code/artifact/71e40506-b9ce-48dd-bf16-d9409c2a9442)
> chegaram e estão no jogo (cenários, 20 peixes, Vara 2, seis barcos, paisagem viva). Para uma leva
> nova: `python3 tools/Arte/processar_mapas_3_4.py <pasta com Pantanal_XX_*.png, Estuario_XX_*.png, Novo_XX_*.png>`.

A segunda leva (01/10/2026) trouxe os pedidos da seção "Refazer": duas faixas novas de capões e duas de
mangue (o jogo alterna A e B, sem repetir a mesma faixa espelhada), a margem direita do Pantanal, as
borboletas e os ícones de barco e isca. Nada dos mapas 3 e 4 está pendente. Para processar de novo, passe
as pastas da mais antiga para a mais nova: `python3 tools/Arte/processar_mapas_3_4.py <pacote 1> <pacote 2>`
(a mais nova vale; um arquivo corrompido é trocado pela cópia do pacote anterior).

A água dos dois mapas continua pintada por script, como nos mapas 1 e 2 (os reflexos são desenhados
pelo jogo a partir das camadas).

## Pescador segurando a vara, varas da cena, iscas e ícone dos Dólares — chegaram (05/10/2026)

Os pedidos 33 a 36 do documento
[Pedidos de arte para o ChatGPT — Fishing Idle](https://claude.ai/code/artifact/163db00c-bfd7-418a-be46-26bd4b4109ec)
chegaram e estão no jogo:

- `Cena/pescador.png` (mãos fechadas em volta do cabo) e `Cena/pescador_maos.png` (só os punhos, desenhados
  por cima da vara). Tamanho, assento e ponto entre os punhos ficam em `Visual/equipamento_cena.json`.
- `Varas/Cena/rod_00_starter.png`, `rod_01.png` e `rod_02.png`: as varas finas e retas da cena. A Loja continua
  com `Varas/<id>.png`.
- `Iscas/bait_01.png` a `bait_03.png`: Terra Viva, Maré Viva e Ouro de Maré, penduradas abaixo da boia.
- `Icones/ico_dolar.png`: o ícone final dos Dólares.

Para uma leva nova: `python3 tools/Arte/equipamento_na_cena.py --pescador <Pedido_33> --varas <Pedido_34>
--iscas <Pedido_35> --previa <saida.png>`. O ícone dos Dólares é recortado como o da moeda (fundo magenta).

## Mapas 5 a 10 e Varas 3 a 5 — no jogo (06/10/2026)

A arte de todos os mapas novos e das Varas 3 a 5 está no jogo (`tools/Arte/processar_mapas_5_6.py`,
`processar_mapas_7_8.py` e `processar_mapas_9_10.py`, com as pastas dos pacotes; depois
`tools/Arte/otimizar_png.py`). Os scripts limpam o mar pintado embaixo das faixas, separam os peixes que
encostam um no outro e continuam os céus noturnos com cópias espelhadas.

Nenhuma imagem pendente desses mapas: o `Abismo_12_vulto_gigante` chegou em 07/10/2026.

## Ranking da Arena e raridades (pedido em 07/10/2026)

Pedido no doc "Fishing Idle — Pedido de arte: Ranking da Arena e raridades"
(https://claude.ai/code/artifact/52c78f1e-1d5f-4c42-820e-cd4e0c14a9b1). Enquanto não chegam, o jogo usa blocos
limpos com uma faixa na cor da medalha e o ícone de estrela.

- `Arena_01_podio.png` → `Arena/podio_1.png`, `podio_2.png`, `podio_3.png` (fundo verde).
- `Arena_02_medalhas.png` → `Icones/ico_medalha_ouro.png`, `ico_medalha_prata.png`, `ico_medalha_bronze.png`
  (fundo magenta).
- `Raridade_01_gemas.png` → uma gema por raridade, para o selo de raridade (fundo magenta).

## Avatares do jogador (pedido em 07/10/2026)

Pedido no doc "Fishing Idle — Pedido de arte: avatares do jogador"
(https://claude.ai/code/artifact/8936d3b1-4724-4853-8f39-78003d519633). `Avatares_01_retratos.png` (fundo verde,
3×2) → `Avatares/avatar_01.png` a `avatar_06.png`. Enquanto não chegam, o jogo mostra um selo colorido.

## Barra superior e cartão do jogador (pedido ao ChatGPT em 07/10/2026)

Recebido e colocado no jogo em 08/10/2026: ícones do menu (`tools/Arte/processar_icones_menu.py`) e botões, barra,
logo, moldura do avatar, ícones de Moedas/Conchas/Dólares e área das moedas (`tools/Arte/processar_ui_barra.py`,
em `Resources/Arte/UI/`). Nada pendente nesta seção.

## Tela de Mapa: arquipélago (A-141, escolha do proprietário em 08/10/2026)

A tela já funciona com desenhos provisórios (marcados `ASSET_PENDENTE` em `Game/UI/MapWindow.cs`): fundo em
degradê, miniaturas dos mapas recortadas em círculo e o ícone do barco. Arquivos esperados, em
`Resources/Arte/UI/` (PNG):
- `ui_map_sea_bg.png` — 1920×1080, só mar, sem textos, rota, ilhas nem molduras.
- `ui_map_island_01.png` a `ui_map_island_10.png` — 512×512, fundo transparente, uma ilha redonda por mapa, todas
  no mesmo tamanho e ângulo, sem moldura nem número (o jogo desenha a moldura, o número e o cadeado).
- `ui_map_boat.png` — 128×128, fundo transparente, barquinho visto de cima, proa para a direita.
- Opcional: `ui_map_medal_frame.png` — 512×512, moldura redonda branca (o jogo pinta de turquesa, dourado ou
  cinza), miolo transparente.

Pedidos prontos para colar no ChatGPT (um por imagem):

> Fundo de jogo mobile 1920×1080, visto de cima, só mar: à esquerda água rasa turquesa clara (#7FD8E4), passando
> por azul (#2F93C4) e azul-cobalto (#174D86) até azul-noite quase preto (#050C1D) à direita, com estrelas suaves
> refletidas só na parte escura. Ondulações leves, sem ilhas, sem barcos, sem textos, sem molduras, sem linhas.
> Estilo "Lago Dourado — Clean Premium": pintura digital limpa, cores vivas mas suaves, sem ruído, sem cara de foto.
> Arquivo: ui_map_sea_bg.png

> Ilha redonda vista de cima, para jogo mobile, 512×512 em PNG com fundo transparente, ocupando o círculo inteiro
> (diâmetro 480 px, centralizada), sem moldura, sem número e sem texto. Tema: [Lago Sereno: lago calmo de água doce
> com vegetação e um pequeno píer]. Estilo "Lago Dourado — Clean Premium": pintura digital limpa, formas
> arredondadas, luz quente, sombra suave por baixo. Todas as 10 ilhas devem ter o mesmo tamanho, ângulo e luz.
> Arquivo: ui_map_island_01.png
> (Trocar o tema e o número para cada mapa: 02 Rio Selvagem — rio com correnteza, pedras e cachoeira; 03 Pantanal
> Dourado — área alagada com reflexos dourados e gramíneas; 04 Estuário das Marés — canal de maré com mangue; 05
> Costa de Coral — praia clara com coqueiros e recife turquesa; 06 Arquipélago do Sol — ilhas rochosas altas em mar
> cobalto; 07 Corrente Azul — mar aberto com ondulações longas; 08 Banco das Baleias — mar escuro com cauda de
> baleia; 09 Talude Noturno — oceano à noite com lua e plâncton brilhando; 10 Abismo Atlântico — mar negro-azulado
> sob céu estrelado com brilho discreto.)

> Barquinho de pesca visto de cima, para jogo mobile, 128×128 em PNG com fundo transparente, proa apontando para a
> direita, casco branco com detalhe turquesa (#25C4C1), pequena esteira de espuma atrás. Estilo "Lago Dourado —
> Clean Premium": limpo, arredondado, sem texto. Arquivo: ui_map_boat.png

## Enciclopédia como álbum de cartas (A-143, decisão do proprietário em 08/10/2026)

A tela já funciona com desenhos provisórios (marcados `ASSET_PENDENTE` em `Game/UI/ProfileWindow.cs`). Arquivos
esperados, em `Resources/Arte/UI/` (PNG, fundo transparente):
- `ui_enc_card_frame.png` — 256×384, moldura fina de carta em branco (o jogo pinta na cor da raridade), cantos
  arredondados, miolo transparente; esticável pelas bordas (9-slice, bordas de 24 px).
- `ui_enc_card_back.png` — 256×384, verso da carta para espécie não descoberta: azul-noite com um padrão discreto de
  ondas e um anzol pequeno no centro, sem texto.
- `ui_enc_binder_tab.png` — 256×64, aba de fichário em branco (o jogo pinta), canto direito reto para encostar na
  página; 9-slice, bordas de 16 px.
- `ui_enc_album_page.png` — 512×512, página de álbum escura com textura de papel muito leve; 9-slice, bordas de 32 px.
- `ui_enc_pedestal.png` — 480×96, pedestal de pedra/coral visto de frente, sem peixe.
- `ui_enc_aura.png` — 512×512, brilho radial branco e suave (o jogo pinta na cor da raridade).

O pedestal e a aura ficam em `Game/UI/HeroSheet.cs` e servem também à ficha de herói do Aquário (modo cartas, A-145):
os mesmos dois arquivos atendem as duas telas.

Pedidos prontos para colar no ChatGPT (um por imagem):

> Moldura fina de carta colecionável para jogo mobile, 256×384 em PNG com fundo transparente: só a borda, branca,
> 4 px, cantos arredondados (raio 18 px), com um filete interno fino a 6 px da borda; o miolo totalmente
> transparente, sem texto, sem ornamentos grandes. Estilo "Lago Dourado — Clean Premium": limpo e elegante.
> Arquivo: ui_enc_card_frame.png

> Verso de carta colecionável para jogo de pesca mobile, 256×384 em PNG, cantos arredondados (raio 18 px) e fundo
> transparente fora da carta: azul-noite (#0A1422 a #11223A), padrão discreto de ondas finas em azul (#274A70) e um
> pequeno anzol estilizado no centro, sem texto, sem brilho forte. Estilo "Lago Dourado — Clean Premium".
> Arquivo: ui_enc_card_back.png

> Aba de fichário para interface de jogo mobile, 256×64 em PNG com fundo transparente: forma de aba branca com os
> cantos da esquerda arredondados e o lado direito reto, sem texto, sem sombra. Arquivo: ui_enc_binder_tab.png

> Página de álbum para interface de jogo mobile, 512×512 em PNG: painel azul-marinho escuro (#11223A) com textura
> de papel muito leve, cantos arredondados (raio 16 px), borda fina (#274A70), sem texto. Estilo "Lago Dourado —
> Clean Premium". Arquivo: ui_enc_album_page.png

> Pedestal para exibir um peixe num jogo mobile, 480×96 em PNG com fundo transparente: disco baixo de pedra
> azulada com pequenos corais nas bordas, visto de frente e levemente de cima, luz suave vinda de cima, sem peixe
> e sem texto. Estilo "Lago Dourado — Clean Premium": pintura digital limpa, sem ruído. Arquivo: ui_enc_pedestal.png

> Brilho radial suave para jogo mobile, 512×512 em PNG com fundo transparente: branco no centro sumindo até
> transparente nas bordas, círculo perfeito, sem raios, sem texto. Arquivo: ui_enc_aura.png

## Aquário vivo (A-144, decisão do proprietário em 08/10/2026)

A tela já funciona com desenhos provisórios (marcados `ASSET_PENDENTE` em `Game/UI/AquariumWindow.cs`): água em
degradê com raios, areia com pedrinhas, bolhas em anel e painéis de vidro. As plantas e os peixes usam a arte que já
existe (`Arte/Vivos/Plantas` e `Arte/Peixes`); os peixes não precisam de quadros de nadar. Arquivos esperados, em
`Resources/Arte/UI/` (PNG):
- `ui_aquarium_bg.png` — 1920×1080, só água do tanque com raios de luz vindos de cima, **sem areia**, sem peixes,
  sem plantas, sem textos e sem moldura (a areia é uma faixa à parte porque a gaveta muda de altura).
- `ui_aquarium_sand.png` — 1920×200, fundo transparente na parte de cima: faixa de areia com algumas pedras
  arredondadas, borda superior ondulada suave; esticada na largura do tanque.
- `ui_aquarium_bubble.png` — 64×64, fundo transparente, uma bolha.
- `ui_aquarium_glass.png` — 512×512, painel de vidro escuro translúcido, esticável pelas bordas (9-slice, bordas de
  32 px).

Pedidos prontos para colar no ChatGPT (um por imagem):

> Fundo de jogo 1920×1080: interior de um aquário visto de frente, só água, sem areia, sem peixes, sem plantas, sem
> textos e sem moldura. Água azul-esverdeada clara em cima (#338A9E) escurecendo para azul profundo embaixo (#0D2945),
> com 3 raios de luz suaves e diagonais vindos da superfície e partículas bem discretas. Estilo "Lago Dourado — Clean
> Premium": pintura digital limpa, cores vivas mas suaves, sem ruído, sem cara de foto. Arquivo: ui_aquarium_bg.png

> Faixa de areia de fundo de aquário para jogo, 1920×200 em PNG com fundo transparente acima da areia: areia clara
> e quente (#A8946E em cima, #665A45 embaixo) com borda superior em ondulações suaves e 8 a 10 pedras arredondadas
> cinza-azuladas de tamanhos variados, luz vinda de cima, sem plantas, sem conchas grandes, sem texto. Estilo "Lago
> Dourado — Clean Premium". Arquivo: ui_aquarium_sand.png

> Bolha de ar debaixo d'água para jogo, 64×64 em PNG com fundo transparente: círculo de contorno fino branco-azulado
> (#D9F7FF), miolo quase transparente e um pequeno reflexo branco no canto superior esquerdo, sem sombra, sem texto.
> Arquivo: ui_aquarium_bubble.png

> Painel de vidro para interface de jogo, 512×512 em PNG: retângulo de cantos arredondados (raio 14 px) em
> azul-noite translúcido (#0A1422 com 86% de opacidade), borda fina azul-clara (#BFEBFF com 25% de opacidade) e um
> filete de brilho branco suave na borda de cima, miolo liso, sem texto, sem ícones. Estilo "Lago Dourado — Clean
> Premium". Arquivo: ui_aquarium_glass.png

## Menu Ranking: palco e placar (A-146, arte recebida em 08/10/2026)

Recebido e colocado no jogo em 08/10/2026 (pacote `FISHING_IDLE_RANKING_20_ASSETS`, feito no ChatGPT pelo
proprietário). `tools/Arte/processar_ranking.py` recorta e redimensiona, em `Resources/Arte/UI/`:
- `ui_rk_coroa.png` (128), `ui_rk_emblema_nivel.png`, `ui_rk_emblema_moedas.png`, `ui_rk_emblema_conchas.png`,
  `ui_rk_emblema_peixes.png` (128 cada);
- `ui_rk_palco.png` (píer à noite com lanternas, 1200×245);
- `ui_rk_escudo_ouro.png`, `ui_rk_escudo_prata.png`, `ui_rk_escudo_bronze.png`, `ui_rk_escudo_azul.png` (96×108,
  o jogo escreve o número);
- `ui_rk_trofeu.png` (192, enfeite do cabeçalho).

Os pedestais do palco são desenhados pelo código (bloco escuro com borda na cor da medalha). Nada pendente nesta seção.

Não usados (ficam fora do jogo): as setas `12A_ico_subiu` e `12B_ico_desceu` (o proprietário decidiu não ter
Subiu/Desceu), a parede de madeira, a moldura e as placas `13` a `19` e a placa de troféu (item 20) — são a arte do
"Exemplo 3 — Quadro de recordes", que não foi o escolhido.

## Expedição: carta náutica (A-147, arte recebida em 08/10/2026)

Recebido e colocado no jogo em 08/10/2026 (a arte dos 3 exemplos da tela de Expedição, feita no ChatGPT pelo
proprietário). `tools/Arte/processar_expedicao.py` recorta pelo alfa e redimensiona, em `Resources/Arte/UI/`:
- `ui_exp_carta.png` (carta náutica à noite, 1600×1001, fundo da escolha) e `ui_exp_porto.png` (porto ao entardecer,
  1600×900, fundo do relatório) — as duas cenas grandes, importadas com compressão;
- `ui_exp_rosa.png` (rosa dos ventos, 256), `ui_exp_medalhao.png` (aro de latão com o centro vazio, 256),
  `ui_exp_pier.png` (píer visto de cima, 256), `ui_exp_marcador.png` (marcador do Cardume, 128);
- `ui_exp_moldura_carta.png` (moldura da carta de missão, 412×600), `ui_exp_selo.png` (selo da chance de peixe, 160)
  e `ui_exp_bussola.png` (bússola com o mostrador vazio, 176×320).

O anel de progresso da bússola, as rotas pontilhadas e o escurecimento do porto são desenhados pelo código. Nada
pendente nesta seção.

Recebida e guardada sem uso: a **placa de destino** (poste com placa e bandeirola, do "Exemplo 3 — Porto"). Não foi
copiada para o jogo; fica com o proprietário para um uso futuro.

## Cabeçalho "Placa do Píer" de todos os menus (A-148, escolha do proprietário em 08/10/2026)

O cabeçalho já funciona com desenhos provisórios (marcados `ASSET_PENDENTE` em `Game/UI/WindowFrame.cs`): placa de
três tábuas azul-escuras com borda turquesa e quatro parafusos, duas cordas listradas e argolas de aço. A sombra
embaixo da placa, o ícone do menu, o título, o botão "i" e o balanço são do código — a arte não deve trazer nada
disso. Arquivos esperados, em `Resources/Arte/UI/` (PNG, fundo transparente):
- `ui_hdr_placa.png` — 512×128, placa esticável em 9 partes: as bordas que não esticam são **48 px à esquerda e à
  direita** e **32 px em cima e embaixo** (os parafusos ficam dentro desses cantos); o miolo estica na largura, então
  as tábuas só podem ter veios na horizontal.
- `ui_hdr_corda.png` — 32×128, um trecho de corda na vertical que se repete sem emenda (o topo encaixa no pé). O
  jogo desenha a corda com 6 px de largura.
- `ui_hdr_argola.png` — 64×64, argola de aço vista de frente, centro vazado (transparente). O jogo desenha com 18 px.

Pedidos prontos para colar no ChatGPT (um por imagem):

> Placa de madeira para cabeçalho de jogo mobile, 512×128 px em PNG com fundo transparente, vista de frente, ocupando
> a imagem inteira (cantos arredondados com raio de 18 px). Três tábuas horizontais azul-escuras (de cima para baixo
> #1D4266, #1A3C5E e #173757) separadas por frestas finas #0D2440, veios de madeira suaves e só na horizontal.
> Borda contínua turquesa (#2A8B98) de 6 px com um filete escuro (#0A1A2D) por dentro. Um parafuso de aço (#C9D6E3
> com sombra #6C7D90 e brilho branco) em cada canto, com o centro a 20 px das duas bordas. Sem texto, sem ícone,
> sem cordas, sem argolas e sem sombra projetada. A faixa central (de 48 a 464 px na largura, de 32 a 96 px na altura)
> precisa ser uniforme, porque vai esticar. Estilo "Lago Dourado — Clean Premium": pintura digital limpa, cores
> profundas, luz suave de cima, nada de marrom. Arquivo: ui_hdr_placa.png

> Trecho de corda de sisal na vertical para jogo mobile, 32×128 px em PNG com fundo transparente, corda com 24 px de
> largura centralizada, trançada em diagonal, bege-areia (#C9A77A) com as voltas em marrom-claro (#8E6F47) e brilho
> suave. Tem que repetir sem emenda: a ponta de cima encaixa perfeitamente na de baixo. Sem nós, sem pontas soltas,
> sem sombra. Estilo "Lago Dourado — Clean Premium": limpo, arredondado. Arquivo: ui_hdr_corda.png

> Argola de aço vista de frente para jogo mobile, 64×64 px em PNG com fundo transparente: um anel redondo (diâmetro
> externo 60 px, espessura 10 px), aço claro (#C9D6E3) com sombra (#6C7D90) embaixo e um brilho branco em cima, o
> centro vazado e transparente. Sem corda, sem sombra projetada, sem texto. Estilo "Lago Dourado — Clean Premium":
> limpo, metálico suave. Arquivo: ui_hdr_argola.png

## Loja "Balcão do Píer" (A-150, escolha do proprietário em 09/10/2026)

**Recebidas em 09/10/2026 e processadas** (`tools/Arte/processar_loja.py`). Como a arte chegou um pouco diferente do
pedido, o jogo foi ajustado a ela: a parede foi cortada de fresta a fresta (2 tábuas, 256×768, repete sem emenda); o
balcão ficou 601×96 (proporção da arte); a caixa ficou 512×262, com a tampa nos 100 px de cima (9 partes: laterais e
base 40 px, topo 100 px, desenhada a 14%); o anel da boia ficou a 63,5% da altura, e o jogo pinta a cor da raridade
acima dele (`ShopWindow.BobberRing`). Nada mais pendente nesta seção; o texto abaixo fica como registro do pedido.

A Loja já funciona com desenhos provisórios (marcados `ASSET_PENDENTE` em `Game/UI/ShopWindow.cs`): parede de tábuas
azul-noite, ganchos de latão, tábua do balcão, caixa de pesca aberta e boias. A moldura da ficha reaproveita
`ui_exp_moldura_carta.png`, e as varas, barcos e iscas usam as imagens que já existem. Texto, preços, etiquetas, a
alça da tampa da caixa, as divisórias, a cor da raridade nas boias e todas as sombras são do código — a arte não deve
trazer nada disso. O ChatGPT costuma entregar em 1024×1536 ou 1536×1024; tudo bem: o script
`tools/Arte/processar_loja.py <pasta>` corta a sobra transparente (alfa > 8), redimensiona com LANCZOS e grava no
tamanho final em `Resources/Arte/UI/`. Basta salvar os 5 PNGs com os nomes abaixo numa pasta e rodar o script.

| Arquivo | Tamanho final | Como o jogo usa |
|---|---|---|
| `ui_loja_parede_noite.png` | 512×768 | Repete na largura, na altura do conteúdo (não é 9 partes): as bordas da esquerda e da direita têm de encaixar. |
| `ui_loja_gancho.png` | 64×64 | Gancho de latão desenhado com 18 px, dois por prateleira. |
| `ui_loja_balcao_pier.png` | 1024×96 | Tábua do balcão em 9 partes, só nas laterais: **48 px à esquerda e à direita** não esticam; o miolo estica na largura. Desenhada com 34 px de altura. |
| `ui_loja_caixa.png` | 512×288 | Caixa de pesca em 9 partes, **bordas de 40 px** nos quatro lados (desenhada a 35%: ~14 px). A tampa aberta fica na faixa de cima. |
| `ui_loja_boia.png` | 64×96 | Desenhada com 22×34 px. A metade de cima é pintada pelo jogo na cor da raridade; a de baixo fica como está. O anel escuro do meio tem de cair exatamente na metade da altura. |

Pedidos prontos para colar no ChatGPT (um por imagem):

> Textura de parede de tábuas para o fundo de uma loja de pesca num jogo mobile, PNG 512×768 px (pode ser 1024×1536
> na mesma proporção), sem transparência, preenchendo a imagem inteira. Quatro tábuas verticais de 128 px de largura,
> alternando azul-noite #132A45 e #15304E, separadas por frestas finas e escuras #0B1D33, veios de madeira suaves só
> na vertical, luz difusa e uniforme (nada de vinheta, nada de brilho num canto). Precisa repetir sem emenda na
> horizontal: a borda esquerda encaixa perfeitamente na direita. Sem texto, sem pregos, sem objetos, sem marrom.
> Estilo "Lago Dourado — Clean Premium": pintura digital limpa, cores profundas, calma. Arquivo:
> ui_loja_parede_noite.png

> Gancho de parede de latão para pendurar varas de pesca, jogo mobile, PNG 64×64 px (pode ser 1024×1024 na mesma
> proporção) com fundo transparente, visto de frente: uma plaquinha redonda presa à parede com um parafuso e um gancho
> em "U" curto saindo para baixo, latão #C9A45A com sombra #8A6E36 e um brilho suave em cima, contorno limpo. Sem
> parede, sem vara, sem sombra projetada, sem texto. Estilo "Lago Dourado — Clean Premium": formas simples,
> arredondadas, metal suave. Arquivo: ui_loja_gancho.png

> Tampo de balcão de loja num píer, jogo mobile, PNG 1024×96 px (pode ser 1536×1024 com a tábua centralizada
> ocupando toda a largura) com fundo transparente, vista de frente e levemente de cima: uma tábua grossa e comprida,
> cantos arredondados (raio 12 px), azul #2A5A86 em cima escurecendo para #183A5E e #0E2442 embaixo, com um filete
> turquesa #25C4C1 de 4 px na borda de cima e veios de madeira só na horizontal. As pontas da esquerda e da direita
> podem ter um acabamento (ponteira de latão #C9A45A) dentro dos primeiros 48 px de cada lado; o meio tem de ser
> uniforme, porque vai esticar. Sem texto, sem objetos em cima, sem sombra projetada, nada de marrom. Estilo "Lago
> Dourado — Clean Premium". Arquivo: ui_loja_balcao_pier.png

> Caixa de pesca aberta, vista de frente, para interface de jogo mobile, PNG 512×288 px (pode ser 1536×1024 com a
> caixa centralizada ocupando a largura) com fundo transparente. Corpo retangular de cantos arredondados (raio 20 px)
> em verde-azulado escuro, #2F6A7A em cima escurecendo para #1F4F5E embaixo, borda #133A46 de 4 px; na faixa de cima
> (40 px), a borda da tampa aberta, um pouco mais estreita que o corpo, em #3A7D8E a #2A6474. Interior liso e escuro
> (#0B2230 a 60%), SEM divisórias, SEM alça e SEM fecho — o jogo desenha isso. Os cantos de 40 px não podem ter nada
> que não deva esticar no meio; o miolo tem de ser uniforme. Sem texto, sem iscas, sem sombra projetada. Estilo
> "Lago Dourado — Clean Premium": formas simples, plástico/madeira pintada suave. Arquivo: ui_loja_caixa.png

> Boia de pesca em pé, vista de frente, para jogo mobile, PNG 64×96 px (pode ser 1024×1536 na mesma proporção) com
> fundo transparente, centralizada. Antena fina cinza-clara (#DDDDDD) curta em cima; corpo em forma de gota
> arredondada com a metade de cima BRANCA (#FFFFFF, só um sombreado cinza bem leve, porque o jogo pinta essa metade
> na cor da raridade) e a metade de baixo branca-gelo (#F4F4F4); um anel fino escuro (#2A3646) separando as duas
> metades EXATAMENTE na metade da altura da imagem; um brilho branco pequeno no alto à esquerda. Sem água, sem
> reflexo, sem sombra projetada, sem texto. Estilo "Lago Dourado — Clean Premium": limpo, arredondado. Arquivo:
> ui_loja_boia.png

## Tripulação: retratos e ícone do menu (M24-T08, A-154, pedido em 10/10/2026)

A janela da Tripulação e o botão do menu já funcionam com desenhos provisórios (`ASSET_PENDENTE` em
`Game/UI/CrewWindow.cs` e `Game/Visual/ArtAssets.cs`): cada tripulante aparece num quadradinho com o ícone de pessoa
(os três primeiros) ou de barco (os outros) e o número da ordem; o botão do menu usa o ícone do barco. Quando um
arquivo abaixo existir, o jogo passa a usá-lo sozinho; se for apagado, volta ao provisório. Texto, número, quantidade,
moldura e cadeado são do código — a arte não deve trazer nada disso. O tripulante ainda bloqueado é desenhado
escurecido pelo próprio jogo.

| Arquivo | Tripulante | Tamanho |
|---|---|---|
| `Arte/Tripulacao/trip_crew_01.png` | Ajudante da Isca | 512×512 → o jogo desenha com 64×64 |
| `Arte/Tripulacao/trip_crew_02.png` | Canoeiro | 512×512 → o jogo desenha com 64×64 |
| `Arte/Tripulacao/trip_crew_03.png` | Tarrafeiro | 512×512 → o jogo desenha com 64×64 |
| `Arte/Tripulacao/trip_crew_04.png` | Jangadeiro | 512×512 → o jogo desenha com 64×64 |
| `Arte/Tripulacao/trip_crew_05.png` | Piloto da Voadeira | 512×512 → o jogo desenha com 64×64 |
| `Arte/Tripulacao/trip_crew_06.png` | Mestre do Barco de Linha | 512×512 → o jogo desenha com 64×64 |
| `Arte/Tripulacao/trip_crew_07.png` | Saveiro do Porto | 512×512 → o jogo desenha com 64×64 |
| `Arte/Tripulacao/trip_crew_08.png` | Traineira | 512×512 → o jogo desenha com 64×64 |
| `Arte/Tripulacao/trip_crew_09.png` | Barco de Arrasto | 512×512 → o jogo desenha com 64×64 |
| `Arte/Tripulacao/trip_crew_10.png` | Navio-Fábrica | 512×512 → o jogo desenha com 64×64 |
| `Arte/Icones/ico_tripulacao.png` | Ícone do botão "Tripulação" na barra de cima | 96×96, branco sobre transparente (como os outros ícones do menu, A-139) |

Pedidos prontos para colar no ChatGPT (um por imagem; o ChatGPT costuma entregar 1024×1024, tudo bem — o recorte e o
tamanho final ficam por minha conta):

> Retrato quadrado de personagem/embarcação para a lista da Tripulação de um jogo idle de pesca mobile, PNG 512×512 px com fundo transparente: um menino ribeirinho de uns 12 anos, camiseta regata turquesa e bermuda, segurando um balde pequeno com iscas e uma latinha de minhocas, sorriso tímido. Enquadramento de busto (pessoas) ou do barco inteiro, centralizado, ocupando cerca de 85% da imagem, sem moldura, sem círculo de fundo, sem texto, sem número, sem sombra projetada. Estilo "Lago Dourado — Clean Premium": pintura digital limpa, contornos suaves, cores profundas (azul-noite #0A1422, turquesa #25C4C1, dourado #F6B93B como acento), luz suave de cima, legível em 64 px. Arquivo: trip_crew_01.png

> Retrato quadrado de personagem/embarcação para a lista da Tripulação de um jogo idle de pesca mobile, PNG 512×512 px com fundo transparente: um pescador adulto de chapéu de palha remando uma canoa de madeira estreita (canoa vista de lado, só o pescador e a proa no quadro), camisa azul-clara. Enquadramento de busto (pessoas) ou do barco inteiro, centralizado, ocupando cerca de 85% da imagem, sem moldura, sem círculo de fundo, sem texto, sem número, sem sombra projetada. Estilo "Lago Dourado — Clean Premium": pintura digital limpa, contornos suaves, cores profundas (azul-noite #0A1422, turquesa #25C4C1, dourado #F6B93B como acento), luz suave de cima, legível em 64 px. Arquivo: trip_crew_02.png

> Retrato quadrado de personagem/embarcação para a lista da Tripulação de um jogo idle de pesca mobile, PNG 512×512 px com fundo transparente: um pescador em pé na beira do rio lançando uma tarrafa aberta em leque no ar, a rede em branco-gelo, bermuda azul-noite. Enquadramento de busto (pessoas) ou do barco inteiro, centralizado, ocupando cerca de 85% da imagem, sem moldura, sem círculo de fundo, sem texto, sem número, sem sombra projetada. Estilo "Lago Dourado — Clean Premium": pintura digital limpa, contornos suaves, cores profundas (azul-noite #0A1422, turquesa #25C4C1, dourado #F6B93B como acento), luz suave de cima, legível em 64 px. Arquivo: trip_crew_03.png

> Retrato quadrado de personagem/embarcação para a lista da Tripulação de um jogo idle de pesca mobile, PNG 512×512 px com fundo transparente: uma jangada nordestina de madeira clara com a vela triangular branca inflada e um jangadeiro de chapéu sentado na popa, sobre água calma. Enquadramento de busto (pessoas) ou do barco inteiro, centralizado, ocupando cerca de 85% da imagem, sem moldura, sem círculo de fundo, sem texto, sem número, sem sombra projetada. Estilo "Lago Dourado — Clean Premium": pintura digital limpa, contornos suaves, cores profundas (azul-noite #0A1422, turquesa #25C4C1, dourado #F6B93B como acento), luz suave de cima, legível em 64 px. Arquivo: trip_crew_04.png

> Retrato quadrado de personagem/embarcação para a lista da Tripulação de um jogo idle de pesca mobile, PNG 512×512 px com fundo transparente: uma voadeira de alumínio (barco amazônico com motor de popa) correndo e levantando um pouco de espuma, o piloto de boné segurando o motor. Enquadramento de busto (pessoas) ou do barco inteiro, centralizado, ocupando cerca de 85% da imagem, sem moldura, sem círculo de fundo, sem texto, sem número, sem sombra projetada. Estilo "Lago Dourado — Clean Premium": pintura digital limpa, contornos suaves, cores profundas (azul-noite #0A1422, turquesa #25C4C1, dourado #F6B93B como acento), luz suave de cima, legível em 64 px. Arquivo: trip_crew_05.png

> Retrato quadrado de personagem/embarcação para a lista da Tripulação de um jogo idle de pesca mobile, PNG 512×512 px com fundo transparente: um barco de pesca de linha de madeira pintado de azul e branco, com cabine pequena e varas presas na borda, o mestre de barba na proa. Enquadramento de busto (pessoas) ou do barco inteiro, centralizado, ocupando cerca de 85% da imagem, sem moldura, sem círculo de fundo, sem texto, sem número, sem sombra projetada. Estilo "Lago Dourado — Clean Premium": pintura digital limpa, contornos suaves, cores profundas (azul-noite #0A1422, turquesa #25C4C1, dourado #F6B93B como acento), luz suave de cima, legível em 64 px. Arquivo: trip_crew_06.png

> Retrato quadrado de personagem/embarcação para a lista da Tripulação de um jogo idle de pesca mobile, PNG 512×512 px com fundo transparente: um saveiro baiano de madeira com duas velas brancas, casco vermelho-telha e branco, ancorado junto a um píer curto. Enquadramento de busto (pessoas) ou do barco inteiro, centralizado, ocupando cerca de 85% da imagem, sem moldura, sem círculo de fundo, sem texto, sem número, sem sombra projetada. Estilo "Lago Dourado — Clean Premium": pintura digital limpa, contornos suaves, cores profundas (azul-noite #0A1422, turquesa #25C4C1, dourado #F6B93B como acento), luz suave de cima, legível em 64 px. Arquivo: trip_crew_07.png

> Retrato quadrado de personagem/embarcação para a lista da Tripulação de um jogo idle de pesca mobile, PNG 512×512 px com fundo transparente: uma traineira de pesca de casco azul-escuro e cabine branca, mastro com luzes, rede recolhida no convés. Enquadramento de busto (pessoas) ou do barco inteiro, centralizado, ocupando cerca de 85% da imagem, sem moldura, sem círculo de fundo, sem texto, sem número, sem sombra projetada. Estilo "Lago Dourado — Clean Premium": pintura digital limpa, contornos suaves, cores profundas (azul-noite #0A1422, turquesa #25C4C1, dourado #F6B93B como acento), luz suave de cima, legível em 64 px. Arquivo: trip_crew_08.png

> Retrato quadrado de personagem/embarcação para a lista da Tripulação de um jogo idle de pesca mobile, PNG 512×512 px com fundo transparente: um barco de arrasto maior de casco turquesa, com os dois braços (tangones) abertos dos lados e as redes penduradas. Enquadramento de busto (pessoas) ou do barco inteiro, centralizado, ocupando cerca de 85% da imagem, sem moldura, sem círculo de fundo, sem texto, sem número, sem sombra projetada. Estilo "Lago Dourado — Clean Premium": pintura digital limpa, contornos suaves, cores profundas (azul-noite #0A1422, turquesa #25C4C1, dourado #F6B93B como acento), luz suave de cima, legível em 64 px. Arquivo: trip_crew_09.png

> Retrato quadrado de personagem/embarcação para a lista da Tripulação de um jogo idle de pesca mobile, PNG 512×512 px com fundo transparente: um grande navio-fábrica de pesca branco e azul-noite com guindastes no convés e chaminé, visto de três quartos, imponente mas limpo. Enquadramento de busto (pessoas) ou do barco inteiro, centralizado, ocupando cerca de 85% da imagem, sem moldura, sem círculo de fundo, sem texto, sem número, sem sombra projetada. Estilo "Lago Dourado — Clean Premium": pintura digital limpa, contornos suaves, cores profundas (azul-noite #0A1422, turquesa #25C4C1, dourado #F6B93B como acento), luz suave de cima, legível em 64 px. Arquivo: trip_crew_10.png

> Ícone de menu para jogo mobile, PNG 96×96 px (pode ser 1024×1024) com fundo transparente, desenho BRANCO chapado
> (#FFFFFF) sem outras cores, traço único com cantos arredondados, no mesmo estilo dos ícones de Pesca, Mapa e Loja do
> jogo: a silhueta de dois pescadores lado a lado (um de chapéu de palha) na frente da proa de um barquinho, simples e
> legível em 24 px. Sem texto, sem fundo, sem sombra. Arquivo: ico_tripulacao.png

## Melhorias: ícones das Melhorias gerais (M24-T11, A-155, pedido em 10/10/2026)

A aba "Melhorias" da janela da Tripulação já funciona com desenhos provisórios (`ASSET_PENDENTE` em
`Game/UI/UpgradesPanel.cs`): as Melhorias da Tripulação usam o retrato do tripulante (pedido acima); as gerais
aparecem num quadradinho dourado com um ícone que já existe (sino, vender, caixa, lupa, ondas). Quando um arquivo
abaixo existir, o jogo passa a usá-lo sozinho; se for apagado, volta ao provisório. Nome, nível, efeito, preço e
cadeado são do código — a arte não deve trazer nada disso.

| Arquivo | Melhoria | Tamanho |
|---|---|---|
| `Arte/Melhorias/melhoria_port_radio.png` | Rádio do Porto | 512×512 → o jogo desenha com 56×56 |
| `Arte/Melhorias/melhoria_fair_customers.png` | Freguesia na Feira | 512×512 → o jogo desenha com 56×56 |
| `Arte/Melhorias/melhoria_cooler_box.png` | Caixa Térmica | 512×512 → o jogo desenha com 56×56 |
| `Arte/Melhorias/melhoria_school_sonar.png` | Sonar de Cardume | 512×512 → o jogo desenha com 56×56 |
| `Arte/Melhorias/melhoria_good_tide.png` | Maré Boa | 512×512 → o jogo desenha com 56×56 |

Pedidos prontos para colar no ChatGPT (um por imagem):

> Ícone quadrado de item para a loja de Melhorias de um jogo idle de pesca mobile, PNG 512×512 px com fundo transparente: um rádio antigo de cabine de barco, de madeira escura com alto-falante de tela dourada e uma antena curta, com duas ondinhas de som saindo. Objeto centralizado, ocupando cerca de 80% da imagem, sem moldura, sem círculo de fundo, sem texto, sem sombra projetada. Estilo "Lago Dourado — Clean Premium": pintura digital limpa, contornos suaves, cores profundas (azul-noite #0A1422, turquesa #25C4C1, dourado #F6B93B como acento), luz suave de cima, legível em 56 px. Arquivo: melhoria_port_radio.png

> Ícone quadrado de item para a loja de Melhorias de um jogo idle de pesca mobile, PNG 512×512 px com fundo transparente: uma banca de feira de peixe com toldo listrado turquesa e branco, dois peixes frescos sobre gelo e uma plaquinha de preço em branco (sem número). Objeto centralizado, ocupando cerca de 80% da imagem, sem moldura, sem círculo de fundo, sem texto, sem sombra projetada. Estilo "Lago Dourado — Clean Premium": pintura digital limpa, contornos suaves, cores profundas (azul-noite #0A1422, turquesa #25C4C1, dourado #F6B93B como acento), luz suave de cima, legível em 56 px. Arquivo: melhoria_fair_customers.png

> Ícone quadrado de item para a loja de Melhorias de um jogo idle de pesca mobile, PNG 512×512 px com fundo transparente: uma caixa térmica de pesca azul-noite com tampa branca e alça, um pouco de gelo aparecendo pela fresta da tampa e um floquinho de frio turquesa. Objeto centralizado, ocupando cerca de 80% da imagem, sem moldura, sem círculo de fundo, sem texto, sem sombra projetada. Estilo "Lago Dourado — Clean Premium": pintura digital limpa, contornos suaves, cores profundas (azul-noite #0A1422, turquesa #25C4C1, dourado #F6B93B como acento), luz suave de cima, legível em 56 px. Arquivo: melhoria_cooler_box.png

> Ícone quadrado de item para a loja de Melhorias de um jogo idle de pesca mobile, PNG 512×512 px com fundo transparente: a tela redonda de um sonar de pesca com moldura escura, fundo azul-noite, anéis e um feixe turquesa girando, e três pontinhos dourados de cardume. Objeto centralizado, ocupando cerca de 80% da imagem, sem moldura extra, sem círculo de fundo, sem texto, sem sombra projetada. Estilo "Lago Dourado — Clean Premium": pintura digital limpa, contornos suaves, cores profundas (azul-noite #0A1422, turquesa #25C4C1, dourado #F6B93B como acento), luz suave de cima, legível em 56 px. Arquivo: melhoria_school_sonar.png

> Ícone quadrado de item para a loja de Melhorias de um jogo idle de pesca mobile, PNG 512×512 px com fundo transparente: uma onda de maré cheia turquesa e branca se curvando, com um peixinho dourado pulando por cima e uma moeda dourada brilhando na espuma. Objeto centralizado, ocupando cerca de 80% da imagem, sem moldura, sem círculo de fundo, sem texto, sem sombra projetada. Estilo "Lago Dourado — Clean Premium": pintura digital limpa, contornos suaves, cores profundas (azul-noite #0A1422, turquesa #25C4C1, dourado #F6B93B como acento), luz suave de cima, legível em 56 px. Arquivo: melhoria_good_tide.png

## Som ambiente dos mapas

Desde 08/10/2026 cada mapa tem 1 gravação provisória feita com sons reais (TD-036, `tools/Audio/mixar_ambiente.py`).
Pendente antes do lançamento: conferir a licença de cada som de origem ou trocar pelas gravações definitivas, e,
se quiser mais variedade, até 4 gravações por mapa (`.ogg`, 2 a 3 minutos; listar em `Resources/Sons/ambiente.json`).

## Prioridade C

Refinamento, skins, equipamentos futuros e mapas posteriores (seção 38) — nada a fazer agora.

## Fontes (já finais)

Fredoka e Nunito, em `Fontes/`, com as licenças `OFL-*.txt`. São gratuitas e podem ser distribuídas
com o jogo.
