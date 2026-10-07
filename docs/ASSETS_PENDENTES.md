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

## Som ambiente dos mapas

Os 40 arquivos listados em `Resources/Sons/ambiente.json` (4 por mapa) ainda não existem. Quando vierem,
salvar como `.ogg` (o jogo aceita): em `.wav`, 2 a 3 minutos de som estéreo pesam ~25 MB cada.

## Prioridade C

Refinamento, skins, equipamentos futuros e mapas posteriores (seção 38) — nada a fazer agora.

## Fontes (já finais)

Fredoka e Nunito, em `Fontes/`, com as licenças `OFL-*.txt`. São gratuitas e podem ser distribuídas
com o jogo.
