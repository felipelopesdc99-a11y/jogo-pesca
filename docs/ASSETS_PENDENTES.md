# Artes pendentes (`ASSET_PENDENTE`)

Lista de toda arte do jogo que hoje é **provisória**: foi pintada por script para o jogo já ficar
bonito e coerente, mas deve ser trocada pela arte final produzida pelo proprietário (processo da
Bíblia de Arte, seção 39). Tarefa `M14-T14` no roadmap.

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
| `Arte/Mapas/LagoSereno/map_lago_sereno_bg_near.png` | Pinheiros e pedras das margens (meio aberto) | 2600×335 | base em y = 0,05 |
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

44 arquivos em `Arte/Icones/ico_<nome>.png`, 96×96, **brancos sobre transparente** (o jogo pinta na
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

## Prioridade C

Refinamento, skins, equipamentos futuros e mapas posteriores (seção 38) — nada a fazer agora.

## Fontes (já finais)

Fredoka e Nunito, em `Fontes/`, com as licenças `OFL-*.txt`. São gratuitas e podem ser distribuídas
com o jogo.
