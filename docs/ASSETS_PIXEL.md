# Artes em pixel art (Estilo ARC) — lista viva

Fonte da lista de pedidos do novo estilo visual escolhido pelo proprietário em 19/10/2026 (GDD_ADENDO A-182, regras em `docs/ART_BIBLE_PIXEL.md`). Gerada junto com o manifesto `tools/Arte/pixel_pedidos.json`, que o `tools/Arte/pixel_folhas.py` usa para recortar cada folha. Atualize o **Status** de cada pedido quando a folha chegar e quando entrar no jogo.

Status: `PEDIR` (ainda não pedido) · `RECEBIDO` (folha chegou e foi recortada em `Resources/ArtePixel/`) · `PEDIR DE NOVO` (veio errada ou ruim demais; o motivo está na linha) · `NO_JOGO` (ligado no jogo).

## Resumo

- **77 pedidos** ao ChatGPT, **339 imagens** no total (as folhas juntam vários desenhos por pedido).
- Hoje o jogo tem **659 PNGs** em `Resources/Arte`. Deles, **316** são refeitos pelos pedidos (a), **240** são convertidos automaticamente da arte atual (b), **60** passam a ser desenhados pelo código (c) e **43** saem (d). Os pedidos também criam **23** arquivos novos (quadros do pescador, boia, nuvens comuns).

| Lote | Pedidos | Imagens |
|---|---:|---:|
| Lote 1 · Kit de teste (piloto do Lago Sereno) | 8 | 28 |
| Lote 2 · Pescador completo e retratos | 2 | 15 |
| Lote 3 · Interface: ícones e objetos | 9 | 84 |
| Lote 4 · Peixes por mapa | 20 | 97 |
| Lote 5 · Itens | 5 | 34 |
| Lote 6 · Cenários dos mapas 2 a 10 | 18 | 63 |
| Lote 7 · Telas especiais e Boss | 15 | 18 |
| **Total** | **77** | **339** |

Os arquivos novos vão para `client-unity/Assets/Resources/ArtePixel/`, com as mesmas subpastas e os mesmos nomes de hoje (TD-059); o jogo procura lá primeiro e usa a arte antiga enquanto um arquivo não existir. A limpeza final (M25) apaga o que sobrar em `Arte/`.

## Recebimento de 20/10/2026

O proprietário mandou as 77 folhas (lotes 1 a 7). Recortadas com `tools/Arte/pixel_folhas.py` (TD-060) e conferidas
uma a uma (cada peixe contra a descrição da espécie): **323 das 339 imagens** estão em
`client-unity/Assets/Resources/ArtePixel/` (mais as 94 versões pequenas `_p` dos peixes). O jogo ainda não lê essa
pasta (M25-T04); só fica verificado no Play.

**Pedir de novo (16 imagens, 4 pedidos):**

| Pedido | Imagens | Motivo |
|---|---:|---|
| L4-03a · Peixes do Pantanal Dourado (1 de 2) | 6 | O arquivo trazia os 6 peixes do Estuário (1 de 2); mandi-amarelo, pacu-peva, jurupensém, piavuçu, jundiá e muçum não vieram em nenhuma folha. |
| L1-06b · Lago Sereno: laterais | 2 (`near_left`, `near_right`) | As duas laterais vieram emendadas numa paisagem só, com morros e o lago no meio; separar cortaria a cena ao meio. Os cantos da frente estão bons. |
| L6-08b · Banco das Baleias: laterais e cantos | 4 | Bordas borradas em listras horizontais e água cortada em blocos retangulares, com halo rosa. |
| L6-09b · Talude Noturno: laterais e cantos | 4 | Mesmo defeito: cada peça virou um bloco retangular de mar com bordas em listras. |

Os pedidos corrigidos, prontos para colar, estão na página de conferência entregue ao proprietário.

**Trocas resolvidas pelo nome (não precisa pedir de novo):** a folha "Lago Sereno (2 de 2)" trazia jaú, pirarucu e
aruanã (Rio Selvagem 2 de 2) e a "Rio Selvagem (2 de 2)" trazia matrinxã, carpa e tambaqui; a "Estuário (1 de 2)"
trazia os 6 do Rio Selvagem (1 de 2) e a "Pantanal (1 de 2)" os 6 do Estuário (1 de 2); a "Rio Selvagem (1 de 2)" era
outra versão do Estuário (2 de 2) (usada a do arquivo extra). No pescador, a 2ª linha da animação A tinha fisgada (2),
mostrar e escapou, e a 2ª linha da animação B tinha o arremesso (4): cada quadro foi gravado com o nome certo.

**Observações para a ligação no jogo:** as faixas do fundo do Estuário e da Costa de Coral vêm com céu ou água
pintados atrás (são retângulos inteiros, não recortes); a boia saiu com 12 × 16.

## Pedidos por lote

### Lote 1 · Kit de teste (piloto do Lago Sereno)

Pescador e a primeira animação, 3 peixes, Caixa e boia, o barco inicial, o cenário do Lago Sereno e as nuvens. Com isso eu monto a cena piloto e conferimos o estilo antes de pedir o resto. Se você já fez o kit antigo, não precisa refazer o que serve: eu reduzo.

| Pedido | Folha | Grade | Célula | Arquivos (em ordem) | Status |
|---|---|---|---|---|---|
| L1-01 · Pescador parado | `L1-01_pescador.png` | 1×1 | 64×64 | `Cena/pescador` | RECEBIDO |
| L1-02 · Pescador: esperando e arremessando (8 quadros) | `L1-02_pescador_animacao_a.png` | 4×2 | 64×64 | `Cena/pescador_parado_1`<br>`Cena/pescador_parado_2`<br>`Cena/pescador_parado_3`<br>`Cena/pescador_parado_4`<br>`Cena/pescador_arremesso_1`<br>`Cena/pescador_arremesso_2`<br>`Cena/pescador_arremesso_3`<br>`Cena/pescador_arremesso_4` | RECEBIDO (a 2ª linha veio com fisgada, mostrar e escapou: gravados com esses nomes) |
| L1-03 · 3 peixes de teste | `L1-03_peixes_teste.png` | 3×1 | 128×128 | `Peixes/fish_lambari_master`<br>`Peixes/fish_tucunare_master`<br>`Peixes/fish_marlim_azul_master` | RECEBIDO |
| L1-04 · Caixa de Pesca e boia | `L1-04_caixa_e_boia.png` | 2×1 | 64×64 | `Cena/caixa_de_pesca`<br>`Cena/boia` | RECEBIDO |
| L1-05 · Barco inicial (Água Mansa) | `L1-05_barco_inicial.png` | 1×1 | 128×64 | `Barcos/boat_00` | RECEBIDO |
| L1-06a · Lago Sereno: fundo em 3 faixas | `L1-06a_map_lago_sereno_fundo.png` | 1×3 | 384×80 | `Mapas/LagoSereno/map_lago_sereno_bg_far`<br>`Mapas/LagoSereno/map_lago_sereno_bg_mid`<br>`Mapas/LagoSereno/map_lago_sereno_bg_near` | RECEBIDO |
| L1-06b · Lago Sereno: laterais e cantos | `L1-06b_map_lago_sereno_primeiro_plano.png` | 2×2 | 192×128 | `Mapas/LagoSereno/map_lago_sereno_near_left`<br>`Mapas/LagoSereno/map_lago_sereno_near_right`<br>`Mapas/LagoSereno/map_lago_sereno_fg_left`<br>`Mapas/LagoSereno/map_lago_sereno_fg_right` | RECEBIDO só os cantos (`fg_left`, `fg_right`) · **PEDIR DE NOVO** `near_left` e `near_right`: as laterais vieram emendadas numa paisagem só |
| L1-07 · Nuvens (servem para todos os mapas) | `L1-07_nuvens.png` | 3×2 | 96×48 | `Mapas/Comum/nuvem_01`<br>`Mapas/Comum/nuvem_02`<br>`Mapas/Comum/nuvem_03`<br>`Mapas/Comum/nuvem_04`<br>`Mapas/Comum/nuvem_05`<br>`Mapas/Comum/nuvem_06` | RECEBIDO |

### Lote 2 · Pescador completo e retratos

O resto das animações do pescador e os 7 retratos (o do pescador e os 6 avatares).

| Pedido | Folha | Grade | Célula | Arquivos (em ordem) | Status |
|---|---|---|---|---|---|
| L2-01 · Pescador: fisgada, recolher, mostrar e escapou (8 quadros) | `L2-01_pescador_animacao_b.png` | 4×2 | 64×64 | `Cena/pescador_recolhe_1`<br>`Cena/pescador_recolhe_2`<br>`Cena/pescador_recolhe_3`<br>`Cena/pescador_recolhe_4`<br>`Cena/pescador_fisgada_1`<br>`Cena/pescador_fisgada_2`<br>`Cena/pescador_mostra_1`<br>`Cena/pescador_escapou_1` | RECEBIDO (a 2ª linha veio com o arremesso: gravado como arremesso) |
| L2-02 · Retrato do pescador e 6 avatares | `L2-02_retratos.png` | 4×2 | 64×64 | `Cena/retrato`<br>`Avatares/avatar_01`<br>`Avatares/avatar_02`<br>`Avatares/avatar_03`<br>`Avatares/avatar_04`<br>`Avatares/avatar_05`<br>`Avatares/avatar_06` | RECEBIDO |

### Lote 3 · Interface: ícones e objetos

Os ícones coloridos em pixel e os objetos das telas. Molduras, painéis, pílulas e barras eu desenho por código.

| Pedido | Folha | Grade | Célula | Arquivos (em ordem) | Status |
|---|---|---|---|---|---|
| L3-01 · Ícones grandes (abas e atalhos) | `L3-01_icones_grandes.png` | 4×4 | 32×32 | `Icones/ico_aba_pesca`<br>`Icones/ico_aba_aventura`<br>`Icones/ico_aba_comercio`<br>`Icones/ico_aba_perfil`<br>`Icones/ico_atalho_mapa`<br>`Icones/ico_atalho_aquario`<br>`Icones/ico_atalho_passe`<br>`Icones/ico_atalho_missoes`<br>`Icones/ico_atalho_boss`<br>`Icones/ico_atalho_ranking`<br>`Icones/ico_atalho_eventos`<br>`Icones/ico_boss`<br>`Icones/ico_cla`<br>`Icones/ico_eventos`<br>`Icones/ico_leilao`<br>`Icones/ico_passe` | RECEBIDO |
| L3-02 · Ícones pequenos (1 de 3) | `L3-02_icones_pequenos_1.png` | 4×3 | 24×24 | `Icones/ico_alimentar`<br>`Icones/ico_ampulheta`<br>`Icones/ico_aquario`<br>`Icones/ico_arena`<br>`Icones/ico_atacar`<br>`Icones/ico_avisos`<br>`Icones/ico_barco`<br>`Icones/ico_buscar`<br>`Icones/ico_cadeado`<br>`Icones/ico_caixa`<br>`Icones/ico_comprar`<br>`Icones/ico_concha` | RECEBIDO |
| L3-03 · Ícones pequenos (2 de 3) | `L3-03_icones_pequenos_2.png` | 4×3 | 24×24 | `Icones/ico_dolar`<br>`Icones/ico_energia`<br>`Icones/ico_especies`<br>`Icones/ico_estrela`<br>`Icones/ico_expedicao`<br>`Icones/ico_honra`<br>`Icones/ico_inicio`<br>`Icones/ico_isca`<br>`Icones/ico_local`<br>`Icones/ico_loja`<br>`Icones/ico_mapa`<br>`Icones/ico_mercado` | RECEBIDO |
| L3-04 · Ícones pequenos (3 de 3) | `L3-04_icones_pequenos_3.png` | 4×3 | 24×24 | `Icones/ico_moeda`<br>`Icones/ico_nivel`<br>`Icones/ico_ondas`<br>`Icones/ico_opcoes`<br>`Icones/ico_peixe`<br>`Icones/ico_perfil`<br>`Icones/ico_pesca`<br>`Icones/ico_ranking`<br>`Icones/ico_relogio`<br>`Icones/ico_trocar`<br>`Icones/ico_vara`<br>`Icones/ico_vender` | RECEBIDO |
| L3-05 · Ranking: coroa, troféu e escudos | `L3-05_ranking_objetos.png` | 3×2 | 48×48 | `UI/ui_rk_coroa`<br>`UI/ui_rk_trofeu`<br>`UI/ui_rk_escudo_ouro`<br>`UI/ui_rk_escudo_prata`<br>`UI/ui_rk_escudo_bronze`<br>`UI/ui_rk_escudo_azul` | RECEBIDO |
| L3-06 · Expedição: objetos | `L3-06_expedicao_objetos.png` | 3×2 | 48×48 | `UI/ui_exp_medalhao`<br>`UI/ui_exp_selo`<br>`UI/ui_exp_rosa`<br>`UI/ui_exp_bussola`<br>`UI/ui_exp_marcador`<br>`UI/ui_exp_pier` | RECEBIDO |
| L3-07 · Loja, Mapa, Clã e Passe: objetos | `L3-07_loja_mapa_cla.png` | 4×2 | 48×48 | `UI/ui_loja_caixa`<br>`UI/ui_loja_gancho`<br>`UI/ui_loja_boia`<br>`UI/ui_map_boat`<br>`UI/cla_brasao`<br>`UI/passe_premium_selo`<br>`UI/ui_enc_pedestal` | RECEBIDO |
| L3-08 · Ilhas da tela do Mapa (uma por mapa) | `L3-08_ilhas_mapa.png` | 5×2 | 64×64 | `UI/ui_map_island_01`<br>`UI/ui_map_island_02`<br>`UI/ui_map_island_03`<br>`UI/ui_map_island_04`<br>`UI/ui_map_island_05`<br>`UI/ui_map_island_06`<br>`UI/ui_map_island_07`<br>`UI/ui_map_island_08`<br>`UI/ui_map_island_09`<br>`UI/ui_map_island_10` | RECEBIDO |
| L3-09 · Pódios da Arena | `L3-09_podios_arena.png` | 3×1 | 64×64 | `Arena/podio_1`<br>`Arena/podio_2`<br>`Arena/podio_3` | RECEBIDO |

### Lote 4 · Peixes por mapa

Os outros 97 peixes, de 6 em 6 e de 4 em 4, sempre do mesmo mapa juntos (os comuns primeiro). A versão pequena de cada peixe eu gero sozinho.

| Pedido | Folha | Grade | Célula | Arquivos (em ordem) | Status |
|---|---|---|---|---|---|
| L4-01a · Peixes do Lago Sereno (1 de 2) | `L4-01a_peixes_lago_sereno_a.png` | 3×2 | 128×128 | `Peixes/fish_cascudo_master`<br>`Peixes/fish_piau_master`<br>`Peixes/fish_tilapia_master`<br>`Peixes/fish_curimbata_master`<br>`Peixes/fish_traira_master`<br>`Peixes/fish_pacu_master` | RECEBIDO |
| L4-01b · Peixes do Lago Sereno (2 de 2) | `L4-01b_peixes_lago_sereno_b.png` | 3×1 | 128×128 | `Peixes/fish_matrinxa_master`<br>`Peixes/fish_carpa_master`<br>`Peixes/fish_tambaqui_master` | RECEBIDO (veio no arquivo L4-02b) |
| L4-02a · Peixes do Rio Selvagem (1 de 2) | `L4-02a_peixes_rio_selvagem_a.png` | 3×2 | 128×128 | `Peixes/fish_piranha_master`<br>`Peixes/fish_piracanjuba_master`<br>`Peixes/fish_peixe_cachorra_master`<br>`Peixes/fish_dourado_master`<br>`Peixes/fish_cachara_master`<br>`Peixes/fish_pintado_master` | RECEBIDO (veio no arquivo L4-04a) |
| L4-02b · Peixes do Rio Selvagem (2 de 2) | `L4-02b_peixes_rio_selvagem_b.png` | 3×1 | 128×128 | `Peixes/fish_jau_master`<br>`Peixes/fish_pirarucu_master`<br>`Peixes/fish_aruana_master` | RECEBIDO (veio no arquivo L4-01b) |
| L4-03a · Peixes do Pantanal Dourado (1 de 2) | `L4-03a_peixes_pantanal_dourado_a.png` | 3×2 | 128×128 | `Peixes/fish_mandi_amarelo_master`<br>`Peixes/fish_pacu_peva_master`<br>`Peixes/fish_jurupensem_master`<br>`Peixes/fish_piavucu_master`<br>`Peixes/fish_jundia_master`<br>`Peixes/fish_mucum_master` | **PEDIR DE NOVO**: o arquivo trazia os peixes do Estuário (1 de 2); estes 6 não vieram |
| L4-03b · Peixes do Pantanal Dourado (2 de 2) | `L4-03b_peixes_pantanal_dourado_b.png` | 2×2 | 128×128 | `Peixes/fish_piraputanga_master`<br>`Peixes/fish_jurupoca_master`<br>`Peixes/fish_armado_master`<br>`Peixes/fish_barbado_master` | RECEBIDO |
| L4-04a · Peixes do Estuário das Marés (1 de 2) | `L4-04a_peixes_estuario_das_mares_a.png` | 3×2 | 128×128 | `Peixes/fish_parati_master`<br>`Peixes/fish_carapeba_master`<br>`Peixes/fish_tainha_master`<br>`Peixes/fish_corvina_master`<br>`Peixes/fish_bagre_marinho_master`<br>`Peixes/fish_robalo_peva_master` | RECEBIDO (veio no arquivo L4-03a) |
| L4-04b · Peixes do Estuário das Marés (2 de 2) | `L4-04b_peixes_estuario_das_mares_b.png` | 2×2 | 128×128 | `Peixes/fish_xareu_master`<br>`Peixes/fish_pescada_amarela_master`<br>`Peixes/fish_camurupim_master`<br>`Peixes/fish_mero_master` | RECEBIDO (arquivo extra; o arquivo L4-02a trazia outra versão desta mesma folha) |
| L4-05a · Peixes do Costa de Coral (1 de 2) | `L4-05a_peixes_costa_de_coral_a.png` | 3×2 | 128×128 | `Peixes/fish_sargentinho_master`<br>`Peixes/fish_salema_master`<br>`Peixes/fish_sargo_master`<br>`Peixes/fish_peixe_porco_master`<br>`Peixes/fish_pampo_master`<br>`Peixes/fish_ariaco_master` | RECEBIDO |
| L4-05b · Peixes do Costa de Coral (2 de 2) | `L4-05b_peixes_costa_de_coral_b.png` | 2×2 | 128×128 | `Peixes/fish_cioba_master`<br>`Peixes/fish_badejo_master`<br>`Peixes/fish_dentao_master`<br>`Peixes/fish_caranha_master` | RECEBIDO |
| L4-06a · Peixes do Arquipélago do Sol (1 de 2) | `L4-06a_peixes_arquipelago_do_sol_a.png` | 3×2 | 128×128 | `Peixes/fish_olhete_master`<br>`Peixes/fish_peixe_galo_master`<br>`Peixes/fish_bonito_cachorro_master`<br>`Peixes/fish_serra_master`<br>`Peixes/fish_beijupira_master`<br>`Peixes/fish_cavala_verdadeira_master` | RECEBIDO |
| L4-06b · Peixes do Arquipélago do Sol (2 de 2) | `L4-06b_peixes_arquipelago_do_sol_b.png` | 2×2 | 128×128 | `Peixes/fish_olho_de_boi_master`<br>`Peixes/fish_dourado_do_mar_master`<br>`Peixes/fish_atum_amarelo_master`<br>`Peixes/fish_veleiro_master` | RECEBIDO |
| L4-07a · Peixes do Corrente Azul (1 de 2) | `L4-07a_peixes_corrente_azul_a.png` | 3×2 | 128×128 | `Peixes/fish_cavalinha_master`<br>`Peixes/fish_bonito_pintado_master`<br>`Peixes/fish_bicuda_master`<br>`Peixes/fish_agulha_branca_master`<br>`Peixes/fish_albacora_branca_master`<br>`Peixes/fish_atum_patudo_master` | RECEBIDO |
| L4-07b · Peixes do Corrente Azul (2 de 2) | `L4-07b_peixes_corrente_azul_b.png` | 3×1 | 128×128 | `Peixes/fish_peixe_lua_master`<br>`Peixes/fish_marlim_branco_master`<br>`Peixes/fish_espadarte_master` | RECEBIDO |
| L4-08a · Peixes do Banco das Baleias (1 de 2) | `L4-08a_peixes_banco_das_baleias_a.png` | 3×2 | 128×128 | `Peixes/fish_pargo_rosa_master`<br>`Peixes/fish_batata_master`<br>`Peixes/fish_namorado_master`<br>`Peixes/fish_congro_rosa_master`<br>`Peixes/fish_cacao_anjo_master`<br>`Peixes/fish_cherne_master` | RECEBIDO |
| L4-08b · Peixes do Banco das Baleias (2 de 2) | `L4-08b_peixes_banco_das_baleias_b.png` | 2×2 | 128×128 | `Peixes/fish_arraia_chita_master`<br>`Peixes/fish_tubarao_lixa_master`<br>`Peixes/fish_tubarao_martelo_master`<br>`Peixes/fish_raia_manta_master` | RECEBIDO |
| L4-09a · Peixes do Talude Noturno (1 de 2) | `L4-09a_peixes_talude_noturno_a.png` | 3×2 | 128×128 | `Peixes/fish_abrotea_fundo_master`<br>`Peixes/fish_escolar_master`<br>`Peixes/fish_peixe_espada_master`<br>`Peixes/fish_quimera_master`<br>`Peixes/fish_peixe_lanceta_master`<br>`Peixes/fish_congro_negro_master` | RECEBIDO |
| L4-09b · Peixes do Talude Noturno (2 de 2) | `L4-09b_peixes_talude_noturno_b.png` | 2×2 | 128×128 | `Peixes/fish_tubarao_lanterna_master`<br>`Peixes/fish_peixe_opah_master`<br>`Peixes/fish_tubarao_duende_master`<br>`Peixes/fish_tubarao_seis_guelras_master` | RECEBIDO |
| L4-10a · Peixes do Abismo Atlântico (1 de 2) | `L4-10a_peixes_abismo_atlantico_a.png` | 3×2 | 128×128 | `Peixes/fish_peixe_machado_master`<br>`Peixes/fish_peixe_vibora_master`<br>`Peixes/fish_peixe_dragao_master`<br>`Peixes/fish_peixe_ogro_master`<br>`Peixes/fish_enguia_pelicano_master`<br>`Peixes/fish_peixe_pescador_abissal_master` | RECEBIDO |
| L4-10b · Peixes do Abismo Atlântico (2 de 2) | `L4-10b_peixes_abismo_atlantico_b.png` | 2×2 | 128×128 | `Peixes/fish_quimera_azul_master`<br>`Peixes/fish_tubarao_cobra_master`<br>`Peixes/fish_peixe_fita_gigante_master`<br>`Peixes/fish_tubarao_boca_grande_master` | RECEBIDO |

### Lote 5 · Itens

Varas, barcos, iscas, roupas, fragmentos e as imagens das expedições.

| Pedido | Folha | Grade | Célula | Arquivos (em ordem) | Status |
|---|---|---|---|---|---|
| L5-01 · Varas (6) | `L5-01_varas.png` | 3×2 | 48×48 | `Varas/rod_00_starter`<br>`Varas/rod_01`<br>`Varas/rod_02`<br>`Varas/rod_03`<br>`Varas/rod_04`<br>`Varas/rod_05` | RECEBIDO |
| L5-02 · Barcos (5) | `L5-02_barcos.png` | 3×2 | 128×64 | `Barcos/boat_01`<br>`Barcos/boat_02`<br>`Barcos/boat_03`<br>`Barcos/boat_04`<br>`Barcos/boat_05` | RECEBIDO |
| L5-03 · Iscas (3) e roupas (5) | `L5-03_iscas_roupas.png` | 4×2 | 32×32 | `Iscas/bait_01`<br>`Iscas/bait_02`<br>`Iscas/bait_03`<br>`Roupas/hat`<br>`Roupas/shirt`<br>`Roupas/pants`<br>`Roupas/gloves`<br>`Roupas/boots` | RECEBIDO |
| L5-04 · Fragmentos (11) | `L5-04_fragmentos.png` | 4×3 | 32×32 | `Fragmentos/frag_boat_01`<br>`Fragmentos/frag_boat_02`<br>`Fragmentos/frag_boat_03`<br>`Fragmentos/frag_boat_04`<br>`Fragmentos/frag_boat_05`<br>`Fragmentos/frag_rod_01`<br>`Fragmentos/frag_rod_02`<br>`Fragmentos/frag_rod_03`<br>`Fragmentos/frag_rod_04`<br>`Fragmentos/frag_rod_05`<br>`Fragmentos/frag_roupa` | RECEBIDO |
| L5-05 · Imagens das 4 expedições | `L5-05_expedicoes.png` | 2×2 | 96×64 | `Expedicoes/exp_30m`<br>`Expedicoes/exp_1h`<br>`Expedicoes/exp_3h`<br>`Expedicoes/exp_6h` | RECEBIDO |

### Lote 6 · Cenários dos mapas 2 a 10

Duas folhas por mapa: o fundo em 3 faixas e as laterais com os cantos. Céu e água eu desenho por código, na paleta de cada mapa.

| Pedido | Folha | Grade | Célula | Arquivos (em ordem) | Status |
|---|---|---|---|---|---|
| L6-02a · Rio Selvagem: fundo em 3 faixas | `L6-02a_map_rio_selvagem_fundo.png` | 1×3 | 384×80 | `Mapas/RioSelvagem/map_rio_selvagem_bg_far`<br>`Mapas/RioSelvagem/map_rio_selvagem_bg_mid`<br>`Mapas/RioSelvagem/map_rio_selvagem_bg_near` | RECEBIDO |
| L6-02b · Rio Selvagem: laterais e cantos | `L6-02b_map_rio_selvagem_primeiro_plano.png` | 2×2 | 192×128 | `Mapas/RioSelvagem/map_rio_selvagem_near_left`<br>`Mapas/RioSelvagem/map_rio_selvagem_near_right`<br>`Mapas/RioSelvagem/map_rio_selvagem_fg_left`<br>`Mapas/RioSelvagem/map_rio_selvagem_fg_right` | RECEBIDO |
| L6-03a · Pantanal Dourado: fundo em 3 faixas | `L6-03a_map_pantanal_dourado_fundo.png` | 1×3 | 384×80 | `Mapas/PantanalDourado/map_pantanal_dourado_bg_far`<br>`Mapas/PantanalDourado/map_pantanal_dourado_bg_mid`<br>`Mapas/PantanalDourado/map_pantanal_dourado_bg_near` | RECEBIDO |
| L6-03b · Pantanal Dourado: laterais e cantos | `L6-03b_map_pantanal_dourado_primeiro_plano.png` | 2×2 | 192×128 | `Mapas/PantanalDourado/map_pantanal_dourado_near_left`<br>`Mapas/PantanalDourado/map_pantanal_dourado_near_right`<br>`Mapas/PantanalDourado/map_pantanal_dourado_fg_left`<br>`Mapas/PantanalDourado/map_pantanal_dourado_fg_right` | RECEBIDO |
| L6-04a · Estuário das Marés: fundo em 3 faixas | `L6-04a_map_estuario_das_mares_fundo.png` | 1×3 | 384×80 | `Mapas/EstuarioDasMares/map_estuario_das_mares_bg_far`<br>`Mapas/EstuarioDasMares/map_estuario_das_mares_bg_mid`<br>`Mapas/EstuarioDasMares/map_estuario_das_mares_bg_near` | RECEBIDO |
| L6-04b · Estuário das Marés: laterais e cantos | `L6-04b_map_estuario_das_mares_primeiro_plano.png` | 2×2 | 192×128 | `Mapas/EstuarioDasMares/map_estuario_das_mares_near_left`<br>`Mapas/EstuarioDasMares/map_estuario_das_mares_near_right`<br>`Mapas/EstuarioDasMares/map_estuario_das_mares_fg_left`<br>`Mapas/EstuarioDasMares/map_estuario_das_mares_fg_right` | RECEBIDO |
| L6-05a · Costa de Coral: fundo em 3 faixas | `L6-05a_map_costa_de_coral_fundo.png` | 1×3 | 384×80 | `Mapas/CostaDeCoral/map_costa_de_coral_bg_far`<br>`Mapas/CostaDeCoral/map_costa_de_coral_bg_mid`<br>`Mapas/CostaDeCoral/map_costa_de_coral_bg_near` | RECEBIDO |
| L6-05b · Costa de Coral: laterais e cantos | `L6-05b_map_costa_de_coral_primeiro_plano.png` | 2×2 | 192×128 | `Mapas/CostaDeCoral/map_costa_de_coral_near_left`<br>`Mapas/CostaDeCoral/map_costa_de_coral_near_right`<br>`Mapas/CostaDeCoral/map_costa_de_coral_fg_left`<br>`Mapas/CostaDeCoral/map_costa_de_coral_fg_right` | RECEBIDO |
| L6-06a · Arquipélago do Sol: fundo em 3 faixas | `L6-06a_map_arquipelago_do_sol_fundo.png` | 1×3 | 384×80 | `Mapas/ArquipelagoDoSol/map_arquipelago_do_sol_bg_far`<br>`Mapas/ArquipelagoDoSol/map_arquipelago_do_sol_bg_mid`<br>`Mapas/ArquipelagoDoSol/map_arquipelago_do_sol_bg_near` | RECEBIDO |
| L6-06b · Arquipélago do Sol: laterais e cantos | `L6-06b_map_arquipelago_do_sol_primeiro_plano.png` | 2×2 | 192×128 | `Mapas/ArquipelagoDoSol/map_arquipelago_do_sol_near_left`<br>`Mapas/ArquipelagoDoSol/map_arquipelago_do_sol_near_right`<br>`Mapas/ArquipelagoDoSol/map_arquipelago_do_sol_fg_left`<br>`Mapas/ArquipelagoDoSol/map_arquipelago_do_sol_fg_right` | RECEBIDO |
| L6-07a · Corrente Azul: fundo em 3 faixas | `L6-07a_map_corrente_azul_fundo.png` | 1×3 | 384×80 | `Mapas/CorrenteAzul/map_corrente_azul_bg_far`<br>`Mapas/CorrenteAzul/map_corrente_azul_bg_mid`<br>`Mapas/CorrenteAzul/map_corrente_azul_bg_near` | RECEBIDO |
| L6-07b · Corrente Azul: laterais e cantos | `L6-07b_map_corrente_azul_primeiro_plano.png` | 2×2 | 192×128 | `Mapas/CorrenteAzul/map_corrente_azul_near_left`<br>`Mapas/CorrenteAzul/map_corrente_azul_near_right`<br>`Mapas/CorrenteAzul/map_corrente_azul_fg_left`<br>`Mapas/CorrenteAzul/map_corrente_azul_fg_right` | RECEBIDO |
| L6-08a · Banco das Baleias: fundo em 3 faixas | `L6-08a_map_banco_das_baleias_fundo.png` | 1×3 | 384×80 | `Mapas/BancoDasBaleias/map_banco_das_baleias_bg_far`<br>`Mapas/BancoDasBaleias/map_banco_das_baleias_bg_mid`<br>`Mapas/BancoDasBaleias/map_banco_das_baleias_bg_near` | RECEBIDO |
| L6-08b · Banco das Baleias: laterais e cantos | `L6-08b_map_banco_das_baleias_primeiro_plano.png` | 2×2 | 192×128 | `Mapas/BancoDasBaleias/map_banco_das_baleias_near_left`<br>`Mapas/BancoDasBaleias/map_banco_das_baleias_near_right`<br>`Mapas/BancoDasBaleias/map_banco_das_baleias_fg_left`<br>`Mapas/BancoDasBaleias/map_banco_das_baleias_fg_right` | **PEDIR DE NOVO**: bordas borradas em listras horizontais e água em bloco retangular |
| L6-09a · Talude Noturno: fundo em 3 faixas | `L6-09a_map_talude_noturno_fundo.png` | 1×3 | 384×80 | `Mapas/TaludeNoturno/map_talude_noturno_bg_far`<br>`Mapas/TaludeNoturno/map_talude_noturno_bg_mid`<br>`Mapas/TaludeNoturno/map_talude_noturno_bg_near` | RECEBIDO |
| L6-09b · Talude Noturno: laterais e cantos | `L6-09b_map_talude_noturno_primeiro_plano.png` | 2×2 | 192×128 | `Mapas/TaludeNoturno/map_talude_noturno_near_left`<br>`Mapas/TaludeNoturno/map_talude_noturno_near_right`<br>`Mapas/TaludeNoturno/map_talude_noturno_fg_left`<br>`Mapas/TaludeNoturno/map_talude_noturno_fg_right` | **PEDIR DE NOVO**: bordas borradas em listras horizontais e água em bloco retangular |
| L6-10a · Abismo Atlântico: fundo em 3 faixas | `L6-10a_map_abismo_atlantico_fundo.png` | 1×3 | 384×80 | `Mapas/AbismoAtlantico/map_abismo_atlantico_bg_far`<br>`Mapas/AbismoAtlantico/map_abismo_atlantico_bg_mid`<br>`Mapas/AbismoAtlantico/map_abismo_atlantico_bg_near` | RECEBIDO |
| L6-10b · Abismo Atlântico: laterais e cantos | `L6-10b_map_abismo_atlantico_primeiro_plano.png` | 2×2 | 192×128 | `Mapas/AbismoAtlantico/map_abismo_atlantico_near_left`<br>`Mapas/AbismoAtlantico/map_abismo_atlantico_near_right`<br>`Mapas/AbismoAtlantico/map_abismo_atlantico_fg_left`<br>`Mapas/AbismoAtlantico/map_abismo_atlantico_fg_right` | RECEBIDO |

### Lote 7 · Telas especiais e Boss

Fundos e faixas das telas (Aquário, Mapa, Arena, Bem-vindo, Caixa, Mercado, Passe, Clã, Eventos, Expedição, Ranking) e o Boss Mundial.

| Pedido | Folha | Grade | Célula | Arquivos (em ordem) | Status |
|---|---|---|---|---|---|
| L7-01 · Aquário: água | `L7-01_aquario_agua.png` | 1×1 | 384×256 | `UI/ui_aquarium_bg` | RECEBIDO |
| L7-02 · Aquário: areia | `L7-02_aquario_areia.png` | 1×1 | 384×64 | `UI/ui_aquarium_sand` | RECEBIDO |
| L7-03 · Mapa: mar visto de cima | `L7-03_mapa_mar.png` | 1×1 | 384×256 | `UI/ui_map_sea_bg` | RECEBIDO |
| L7-04 · Arena: fundo | `L7-04_arena_fundo.png` | 1×1 | 384×256 | `UI/arena_fundo` | RECEBIDO |
| L7-05 · Bem-vindo de volta | `L7-05_bemvindo.png` | 1×1 | 384×256 | `UI/bemvindo_cena` | RECEBIDO |
| L7-06 · Caixa de Pesca: faixa | `L7-06_caixa_faixa.png` | 1×1 | 384×256 | `UI/caixa_cena` | RECEBIDO |
| L7-07 · Mercado: faixa | `L7-07_mercado_faixa.png` | 1×1 | 384×256 | `UI/merc_feira_porto` | RECEBIDO |
| L7-08 · Passe: faixa | `L7-08_passe_faixa.png` | 1×1 | 384×256 | `UI/passe_faixa` | RECEBIDO |
| L7-09 · Clã: fundo | `L7-09_cla_fundo.png` | 1×1 | 384×256 | `UI/cla_fundo` | RECEBIDO |
| L7-10 · Eventos: quadro de avisos | `L7-10_eventos_quadro.png` | 1×1 | 384×256 | `UI/eventos_quadro` | RECEBIDO |
| L7-11 · Expedição: porto | `L7-11_expedicao_porto.png` | 1×1 | 384×256 | `UI/ui_exp_porto` | RECEBIDO |
| L7-12 · Expedição: carta náutica | `L7-12_expedicao_carta.png` | 1×1 | 384×256 | `UI/ui_exp_carta` | RECEBIDO |
| L7-13 · Ranking: palco | `L7-13_ranking_palco.png` | 1×1 | 384×80 | `UI/ui_rk_palco` | RECEBIDO |
| L7-14 · Boss: fundo do abismo | `L7-14_boss_fundo.png` | 1×1 | 384×256 | `Boss/boss_fundo_abismo` | RECEBIDO |
| L7-15 · Boss Mundial: poses | `L7-15_boss_poses.png` | 2×2 | 160×112 | `Boss/boss_pose_parado`<br>`Boss/boss_pose_respira`<br>`Boss/boss_pose_ataque`<br>`Boss/boss_mundial` | RECEBIDO |

## Inventário da arte atual

Como cada arquivo de `Resources/Arte` fica no novo estilo. (a) pedido ao ChatGPT · (b) convertido da arte atual · (c) desenhado pelo código · (d) sai.

### Agua (1)

Resumo: (c) 1

| Arquivo | Como fica |
|---|---|
| `Agua/ondas` | (c) ondas desenhadas pelo código junto com a água |

### Arena (3)

Resumo: (a) 3

| Arquivo | Como fica |
|---|---|
| `Arena/podio_1` | (a) pedido L3-09 |
| `Arena/podio_2` | (a) pedido L3-09 |
| `Arena/podio_3` | (a) pedido L3-09 |

### Avatares (6)

Resumo: (a) 6

| Arquivo | Como fica |
|---|---|
| `Avatares/avatar_01` | (a) pedido L2-02 |
| `Avatares/avatar_02` | (a) pedido L2-02 |
| `Avatares/avatar_03` | (a) pedido L2-02 |
| `Avatares/avatar_04` | (a) pedido L2-02 |
| `Avatares/avatar_05` | (a) pedido L2-02 |
| `Avatares/avatar_06` | (a) pedido L2-02 |

### Barcos (6)

Barcos na Loja e no Perfil (ScaleToFit) e na cena (sprite por largura em unidades).

Resumo: (a) 6

| Arquivo | Como fica |
|---|---|
| `Barcos/boat_00` | (a) pedido L1-05 |
| `Barcos/boat_01` | (a) pedido L5-02 |
| `Barcos/boat_02` | (a) pedido L5-02 |
| `Barcos/boat_03` | (a) pedido L5-02 |
| `Barcos/boat_04` | (a) pedido L5-02 |
| `Barcos/boat_05` | (a) pedido L5-02 |

### Barcos/Cena (5)

Barcos na Loja e no Perfil (ScaleToFit) e na cena (sprite por largura em unidades).

Resumo: (b) 5

| Arquivo | Como fica |
|---|---|
| `Barcos/Cena/boat_01_frente` | (b) cortado automaticamente do barco em pixel (parte da frente / de trás) |
| `Barcos/Cena/boat_02_frente` | (b) cortado automaticamente do barco em pixel (parte da frente / de trás) |
| `Barcos/Cena/boat_03_frente` | (b) cortado automaticamente do barco em pixel (parte da frente / de trás) |
| `Barcos/Cena/boat_04_frente` | (b) cortado automaticamente do barco em pixel (parte da frente / de trás) |
| `Barcos/Cena/boat_05_frente` | (b) cortado automaticamente do barco em pixel (parte da frente / de trás) |

### Boss (7)

Boss Mundial: 3 poses alternadas, fundo, impacto e bolhas.

Resumo: (a) 5 · (c) 2

| Arquivo | Como fica |
|---|---|
| `Boss/boss_bolhas` | (c) efeito em pixel desenhado pelo código |
| `Boss/boss_fundo_abismo` | (a) pedido L7-14 |
| `Boss/boss_impacto` | (c) efeito em pixel desenhado pelo código |
| `Boss/boss_mundial` | (a) pedido L7-15 |
| `Boss/boss_pose_ataque` | (a) pedido L7-15 |
| `Boss/boss_pose_parado` | (a) pedido L7-15 |
| `Boss/boss_pose_respira` | (a) pedido L7-15 |

### Cena (7)

Pescador (sprite pintado + mãos por cima da vara), barco inicial em 3 camadas, Caixa de Pesca e retrato padrão.

Resumo: (a) 3 · (b) 3 · (d) 1

| Arquivo | Como fica |
|---|---|
| `Cena/barco` | (b) cópia do Barcos/boat_00 (pedido L1-05) |
| `Cena/barco_frente` | (b) cortado automaticamente do barco em pixel (parte da frente / de trás) |
| `Cena/barco_fundo` | (b) cortado automaticamente do barco em pixel (parte da frente / de trás) |
| `Cena/caixa_de_pesca` | (a) pedido L1-04 |
| `Cena/pescador` | (a) pedido L1-01 |
| `Cena/pescador_maos` | (d) sai: as mãos já estão nos quadros do pescador |
| `Cena/retrato` | (a) pedido L2-02 |

### Expedicoes (4)

Resumo: (a) 4

| Arquivo | Como fica |
|---|---|
| `Expedicoes/exp_1h` | (a) pedido L5-05 |
| `Expedicoes/exp_30m` | (a) pedido L5-05 |
| `Expedicoes/exp_3h` | (a) pedido L5-05 |
| `Expedicoes/exp_6h` | (a) pedido L5-05 |

### Fragmentos (11)

Resumo: (a) 11

| Arquivo | Como fica |
|---|---|
| `Fragmentos/frag_boat_01` | (a) pedido L5-04 |
| `Fragmentos/frag_boat_02` | (a) pedido L5-04 |
| `Fragmentos/frag_boat_03` | (a) pedido L5-04 |
| `Fragmentos/frag_boat_04` | (a) pedido L5-04 |
| `Fragmentos/frag_boat_05` | (a) pedido L5-04 |
| `Fragmentos/frag_rod_01` | (a) pedido L5-04 |
| `Fragmentos/frag_rod_02` | (a) pedido L5-04 |
| `Fragmentos/frag_rod_03` | (a) pedido L5-04 |
| `Fragmentos/frag_rod_04` | (a) pedido L5-04 |
| `Fragmentos/frag_rod_05` | (a) pedido L5-04 |
| `Fragmentos/frag_roupa` | (a) pedido L5-04 |

### Icones (64)

Ícones brancos de 96 e 256 px, tingidos pelo código; desenhados em 14–72 px na tela.

Resumo: (a) 52 · (c) 12

| Arquivo | Como fica |
|---|---|
| `Icones/ico_aba_aventura` | (a) pedido L3-01 |
| `Icones/ico_aba_comercio` | (a) pedido L3-01 |
| `Icones/ico_aba_perfil` | (a) pedido L3-01 |
| `Icones/ico_aba_pesca` | (a) pedido L3-01 |
| `Icones/ico_adicionar` | (c) símbolo simples de 1 cor desenhado pelo código em pixel |
| `Icones/ico_alimentar` | (a) pedido L3-02 |
| `Icones/ico_ampulheta` | (a) pedido L3-02 |
| `Icones/ico_aquario` | (a) pedido L3-02 |
| `Icones/ico_arena` | (a) pedido L3-02 |
| `Icones/ico_atacar` | (a) pedido L3-02 |
| `Icones/ico_atalho_aquario` | (a) pedido L3-01 |
| `Icones/ico_atalho_boss` | (a) pedido L3-01 |
| `Icones/ico_atalho_eventos` | (a) pedido L3-01 |
| `Icones/ico_atalho_mapa` | (a) pedido L3-01 |
| `Icones/ico_atalho_missoes` | (a) pedido L3-01 |
| `Icones/ico_atalho_passe` | (a) pedido L3-01 |
| `Icones/ico_atalho_ranking` | (a) pedido L3-01 |
| `Icones/ico_aviso` | (c) símbolo simples de 1 cor desenhado pelo código em pixel |
| `Icones/ico_avisos` | (a) pedido L3-02 |
| `Icones/ico_barco` | (a) pedido L3-02 |
| `Icones/ico_boss` | (a) pedido L3-01 |
| `Icones/ico_buscar` | (a) pedido L3-02 |
| `Icones/ico_cadeado` | (a) pedido L3-02 |
| `Icones/ico_caixa` | (a) pedido L3-02 |
| `Icones/ico_cla` | (a) pedido L3-01 |
| `Icones/ico_compacto` | (c) símbolo simples de 1 cor desenhado pelo código em pixel |
| `Icones/ico_comprar` | (a) pedido L3-02 |
| `Icones/ico_concha` | (a) pedido L3-02 |
| `Icones/ico_confirmar` | (c) símbolo simples de 1 cor desenhado pelo código em pixel |
| `Icones/ico_dolar` | (a) pedido L3-03 |
| `Icones/ico_energia` | (a) pedido L3-03 |
| `Icones/ico_especies` | (a) pedido L3-03 |
| `Icones/ico_estrela` | (a) pedido L3-03 |
| `Icones/ico_eventos` | (a) pedido L3-01 |
| `Icones/ico_expedicao` | (a) pedido L3-03 |
| `Icones/ico_fechar` | (c) símbolo simples de 1 cor desenhado pelo código em pixel |
| `Icones/ico_honra` | (a) pedido L3-03 |
| `Icones/ico_info` | (c) símbolo simples de 1 cor desenhado pelo código em pixel |
| `Icones/ico_iniciar` | (c) símbolo simples de 1 cor desenhado pelo código em pixel |
| `Icones/ico_inicio` | (a) pedido L3-03 |
| `Icones/ico_isca` | (a) pedido L3-03 |
| `Icones/ico_leilao` | (a) pedido L3-01 |
| `Icones/ico_local` | (a) pedido L3-03 |
| `Icones/ico_loja` | (a) pedido L3-03 |
| `Icones/ico_mapa` | (a) pedido L3-03 |
| `Icones/ico_mercado` | (a) pedido L3-03 |
| `Icones/ico_moeda` | (a) pedido L3-04 |
| `Icones/ico_nivel` | (a) pedido L3-04 |
| `Icones/ico_ondas` | (a) pedido L3-04 |
| `Icones/ico_opcoes` | (a) pedido L3-04 |
| `Icones/ico_ordenar` | (c) símbolo simples de 1 cor desenhado pelo código em pixel |
| `Icones/ico_parar` | (c) símbolo simples de 1 cor desenhado pelo código em pixel |
| `Icones/ico_passe` | (a) pedido L3-01 |
| `Icones/ico_pausa` | (c) símbolo simples de 1 cor desenhado pelo código em pixel |
| `Icones/ico_peixe` | (a) pedido L3-04 |
| `Icones/ico_perfil` | (a) pedido L3-04 |
| `Icones/ico_pesca` | (a) pedido L3-04 |
| `Icones/ico_ranking` | (a) pedido L3-04 |
| `Icones/ico_relogio` | (a) pedido L3-04 |
| `Icones/ico_seta` | (c) símbolo simples de 1 cor desenhado pelo código em pixel |
| `Icones/ico_seta_longa` | (c) símbolo simples de 1 cor desenhado pelo código em pixel |
| `Icones/ico_trocar` | (a) pedido L3-04 |
| `Icones/ico_vara` | (a) pedido L3-04 |
| `Icones/ico_vender` | (a) pedido L3-04 |

### Iscas (3)

Resumo: (a) 3

| Arquivo | Como fica |
|---|---|
| `Iscas/bait_01` | (a) pedido L5-03 |
| `Iscas/bait_02` | (a) pedido L5-03 |
| `Iscas/bait_03` | (a) pedido L5-03 |

### Mapas/AbismoAtlantico (13)

Resumo: (a) 7 · (b) 1 · (c) 2 · (d) 3

| Arquivo | Como fica |
|---|---|
| `Mapas/AbismoAtlantico/map_abismo_atlantico_bg_far` | (a) pedido L6-10a |
| `Mapas/AbismoAtlantico/map_abismo_atlantico_bg_mid` | (a) pedido L6-10a |
| `Mapas/AbismoAtlantico/map_abismo_atlantico_bg_near` | (a) pedido L6-10a |
| `Mapas/AbismoAtlantico/map_abismo_atlantico_bg_sky` | (c) céu em faixas de cor da paleta do mapa (estrelas nos mapas de noite) |
| `Mapas/AbismoAtlantico/map_abismo_atlantico_cloud_01` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/AbismoAtlantico/map_abismo_atlantico_cloud_02` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/AbismoAtlantico/map_abismo_atlantico_cloud_03` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/AbismoAtlantico/map_abismo_atlantico_fg_left` | (a) pedido L6-10b |
| `Mapas/AbismoAtlantico/map_abismo_atlantico_fg_right` | (a) pedido L6-10b |
| `Mapas/AbismoAtlantico/map_abismo_atlantico_near_left` | (a) pedido L6-10b |
| `Mapas/AbismoAtlantico/map_abismo_atlantico_near_right` | (a) pedido L6-10b |
| `Mapas/AbismoAtlantico/map_abismo_atlantico_thumb` | (b) gerado das camadas do mapa já em pixel |
| `Mapas/AbismoAtlantico/map_abismo_atlantico_water` | (c) água em faixas com brilhos e espuma animados, paleta do mapa |

### Mapas/ArquipelagoDoSol (13)

Resumo: (a) 7 · (b) 1 · (c) 2 · (d) 3

| Arquivo | Como fica |
|---|---|
| `Mapas/ArquipelagoDoSol/map_arquipelago_do_sol_bg_far` | (a) pedido L6-06a |
| `Mapas/ArquipelagoDoSol/map_arquipelago_do_sol_bg_mid` | (a) pedido L6-06a |
| `Mapas/ArquipelagoDoSol/map_arquipelago_do_sol_bg_near` | (a) pedido L6-06a |
| `Mapas/ArquipelagoDoSol/map_arquipelago_do_sol_bg_sky` | (c) céu em faixas de cor da paleta do mapa (estrelas nos mapas de noite) |
| `Mapas/ArquipelagoDoSol/map_arquipelago_do_sol_cloud_01` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/ArquipelagoDoSol/map_arquipelago_do_sol_cloud_02` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/ArquipelagoDoSol/map_arquipelago_do_sol_cloud_03` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/ArquipelagoDoSol/map_arquipelago_do_sol_fg_left` | (a) pedido L6-06b |
| `Mapas/ArquipelagoDoSol/map_arquipelago_do_sol_fg_right` | (a) pedido L6-06b |
| `Mapas/ArquipelagoDoSol/map_arquipelago_do_sol_near_left` | (a) pedido L6-06b |
| `Mapas/ArquipelagoDoSol/map_arquipelago_do_sol_near_right` | (a) pedido L6-06b |
| `Mapas/ArquipelagoDoSol/map_arquipelago_do_sol_thumb` | (b) gerado das camadas do mapa já em pixel |
| `Mapas/ArquipelagoDoSol/map_arquipelago_do_sol_water` | (c) água em faixas com brilhos e espuma animados, paleta do mapa |

### Mapas/BancoDasBaleias (13)

Resumo: (a) 7 · (b) 1 · (c) 2 · (d) 3

| Arquivo | Como fica |
|---|---|
| `Mapas/BancoDasBaleias/map_banco_das_baleias_bg_far` | (a) pedido L6-08a |
| `Mapas/BancoDasBaleias/map_banco_das_baleias_bg_mid` | (a) pedido L6-08a |
| `Mapas/BancoDasBaleias/map_banco_das_baleias_bg_near` | (a) pedido L6-08a |
| `Mapas/BancoDasBaleias/map_banco_das_baleias_bg_sky` | (c) céu em faixas de cor da paleta do mapa (estrelas nos mapas de noite) |
| `Mapas/BancoDasBaleias/map_banco_das_baleias_cloud_01` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/BancoDasBaleias/map_banco_das_baleias_cloud_02` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/BancoDasBaleias/map_banco_das_baleias_cloud_03` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/BancoDasBaleias/map_banco_das_baleias_fg_left` | (a) pedido L6-08b |
| `Mapas/BancoDasBaleias/map_banco_das_baleias_fg_right` | (a) pedido L6-08b |
| `Mapas/BancoDasBaleias/map_banco_das_baleias_near_left` | (a) pedido L6-08b |
| `Mapas/BancoDasBaleias/map_banco_das_baleias_near_right` | (a) pedido L6-08b |
| `Mapas/BancoDasBaleias/map_banco_das_baleias_thumb` | (b) gerado das camadas do mapa já em pixel |
| `Mapas/BancoDasBaleias/map_banco_das_baleias_water` | (c) água em faixas com brilhos e espuma animados, paleta do mapa |

### Mapas/CorrenteAzul (13)

Resumo: (a) 7 · (b) 1 · (c) 2 · (d) 3

| Arquivo | Como fica |
|---|---|
| `Mapas/CorrenteAzul/map_corrente_azul_bg_far` | (a) pedido L6-07a |
| `Mapas/CorrenteAzul/map_corrente_azul_bg_mid` | (a) pedido L6-07a |
| `Mapas/CorrenteAzul/map_corrente_azul_bg_near` | (a) pedido L6-07a |
| `Mapas/CorrenteAzul/map_corrente_azul_bg_sky` | (c) céu em faixas de cor da paleta do mapa (estrelas nos mapas de noite) |
| `Mapas/CorrenteAzul/map_corrente_azul_cloud_01` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/CorrenteAzul/map_corrente_azul_cloud_02` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/CorrenteAzul/map_corrente_azul_cloud_03` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/CorrenteAzul/map_corrente_azul_fg_left` | (a) pedido L6-07b |
| `Mapas/CorrenteAzul/map_corrente_azul_fg_right` | (a) pedido L6-07b |
| `Mapas/CorrenteAzul/map_corrente_azul_near_left` | (a) pedido L6-07b |
| `Mapas/CorrenteAzul/map_corrente_azul_near_right` | (a) pedido L6-07b |
| `Mapas/CorrenteAzul/map_corrente_azul_thumb` | (b) gerado das camadas do mapa já em pixel |
| `Mapas/CorrenteAzul/map_corrente_azul_water` | (c) água em faixas com brilhos e espuma animados, paleta do mapa |

### Mapas/CostaDeCoral (13)

Resumo: (a) 7 · (b) 1 · (c) 2 · (d) 3

| Arquivo | Como fica |
|---|---|
| `Mapas/CostaDeCoral/map_costa_de_coral_bg_far` | (a) pedido L6-05a |
| `Mapas/CostaDeCoral/map_costa_de_coral_bg_mid` | (a) pedido L6-05a |
| `Mapas/CostaDeCoral/map_costa_de_coral_bg_near` | (a) pedido L6-05a |
| `Mapas/CostaDeCoral/map_costa_de_coral_bg_sky` | (c) céu em faixas de cor da paleta do mapa (estrelas nos mapas de noite) |
| `Mapas/CostaDeCoral/map_costa_de_coral_cloud_01` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/CostaDeCoral/map_costa_de_coral_cloud_02` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/CostaDeCoral/map_costa_de_coral_cloud_03` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/CostaDeCoral/map_costa_de_coral_fg_left` | (a) pedido L6-05b |
| `Mapas/CostaDeCoral/map_costa_de_coral_fg_right` | (a) pedido L6-05b |
| `Mapas/CostaDeCoral/map_costa_de_coral_near_left` | (a) pedido L6-05b |
| `Mapas/CostaDeCoral/map_costa_de_coral_near_right` | (a) pedido L6-05b |
| `Mapas/CostaDeCoral/map_costa_de_coral_thumb` | (b) gerado das camadas do mapa já em pixel |
| `Mapas/CostaDeCoral/map_costa_de_coral_water` | (c) água em faixas com brilhos e espuma animados, paleta do mapa |

### Mapas/EstuarioDasMares (13)

Resumo: (a) 7 · (b) 1 · (c) 2 · (d) 3

| Arquivo | Como fica |
|---|---|
| `Mapas/EstuarioDasMares/map_estuario_das_mares_bg_far` | (a) pedido L6-04a |
| `Mapas/EstuarioDasMares/map_estuario_das_mares_bg_mid` | (a) pedido L6-04a |
| `Mapas/EstuarioDasMares/map_estuario_das_mares_bg_near` | (a) pedido L6-04a |
| `Mapas/EstuarioDasMares/map_estuario_das_mares_bg_sky` | (c) céu em faixas de cor da paleta do mapa (estrelas nos mapas de noite) |
| `Mapas/EstuarioDasMares/map_estuario_das_mares_cloud_01` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/EstuarioDasMares/map_estuario_das_mares_cloud_02` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/EstuarioDasMares/map_estuario_das_mares_cloud_03` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/EstuarioDasMares/map_estuario_das_mares_fg_left` | (a) pedido L6-04b |
| `Mapas/EstuarioDasMares/map_estuario_das_mares_fg_right` | (a) pedido L6-04b |
| `Mapas/EstuarioDasMares/map_estuario_das_mares_near_left` | (a) pedido L6-04b |
| `Mapas/EstuarioDasMares/map_estuario_das_mares_near_right` | (a) pedido L6-04b |
| `Mapas/EstuarioDasMares/map_estuario_das_mares_thumb` | (b) gerado das camadas do mapa já em pixel |
| `Mapas/EstuarioDasMares/map_estuario_das_mares_water` | (c) água em faixas com brilhos e espuma animados, paleta do mapa |

### Mapas/LagoSereno (13)

Camadas da cena de cada mapa (13 por mapa): céu, 3 faixas de fundo, água, laterais, cantos, 3 nuvens e a miniatura. Sprites em unidades do mundo (câmera de 10,8 unidades de altura).

Resumo: (a) 7 · (b) 1 · (c) 2 · (d) 3

| Arquivo | Como fica |
|---|---|
| `Mapas/LagoSereno/map_lago_sereno_bg_far` | (a) pedido L1-06a |
| `Mapas/LagoSereno/map_lago_sereno_bg_mid` | (a) pedido L1-06a |
| `Mapas/LagoSereno/map_lago_sereno_bg_near` | (a) pedido L1-06a |
| `Mapas/LagoSereno/map_lago_sereno_bg_sky` | (c) céu em faixas de cor da paleta do mapa (estrelas nos mapas de noite) |
| `Mapas/LagoSereno/map_lago_sereno_cloud_01` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/LagoSereno/map_lago_sereno_cloud_02` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/LagoSereno/map_lago_sereno_cloud_03` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/LagoSereno/map_lago_sereno_fg_left` | (a) pedido L1-06b |
| `Mapas/LagoSereno/map_lago_sereno_fg_right` | (a) pedido L1-06b |
| `Mapas/LagoSereno/map_lago_sereno_near_left` | (a) pedido L1-06b |
| `Mapas/LagoSereno/map_lago_sereno_near_right` | (a) pedido L1-06b |
| `Mapas/LagoSereno/map_lago_sereno_thumb` | (b) gerado das camadas do mapa já em pixel |
| `Mapas/LagoSereno/map_lago_sereno_water` | (c) água em faixas com brilhos e espuma animados, paleta do mapa |

### Mapas/PantanalDourado (13)

Resumo: (a) 7 · (b) 1 · (c) 2 · (d) 3

| Arquivo | Como fica |
|---|---|
| `Mapas/PantanalDourado/map_pantanal_dourado_bg_far` | (a) pedido L6-03a |
| `Mapas/PantanalDourado/map_pantanal_dourado_bg_mid` | (a) pedido L6-03a |
| `Mapas/PantanalDourado/map_pantanal_dourado_bg_near` | (a) pedido L6-03a |
| `Mapas/PantanalDourado/map_pantanal_dourado_bg_sky` | (c) céu em faixas de cor da paleta do mapa (estrelas nos mapas de noite) |
| `Mapas/PantanalDourado/map_pantanal_dourado_cloud_01` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/PantanalDourado/map_pantanal_dourado_cloud_02` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/PantanalDourado/map_pantanal_dourado_cloud_03` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/PantanalDourado/map_pantanal_dourado_fg_left` | (a) pedido L6-03b |
| `Mapas/PantanalDourado/map_pantanal_dourado_fg_right` | (a) pedido L6-03b |
| `Mapas/PantanalDourado/map_pantanal_dourado_near_left` | (a) pedido L6-03b |
| `Mapas/PantanalDourado/map_pantanal_dourado_near_right` | (a) pedido L6-03b |
| `Mapas/PantanalDourado/map_pantanal_dourado_thumb` | (b) gerado das camadas do mapa já em pixel |
| `Mapas/PantanalDourado/map_pantanal_dourado_water` | (c) água em faixas com brilhos e espuma animados, paleta do mapa |

### Mapas/RioSelvagem (13)

Resumo: (a) 7 · (b) 1 · (c) 2 · (d) 3

| Arquivo | Como fica |
|---|---|
| `Mapas/RioSelvagem/map_rio_selvagem_bg_far` | (a) pedido L6-02a |
| `Mapas/RioSelvagem/map_rio_selvagem_bg_mid` | (a) pedido L6-02a |
| `Mapas/RioSelvagem/map_rio_selvagem_bg_near` | (a) pedido L6-02a |
| `Mapas/RioSelvagem/map_rio_selvagem_bg_sky` | (c) céu em faixas de cor da paleta do mapa (estrelas nos mapas de noite) |
| `Mapas/RioSelvagem/map_rio_selvagem_cloud_01` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/RioSelvagem/map_rio_selvagem_cloud_02` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/RioSelvagem/map_rio_selvagem_cloud_03` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/RioSelvagem/map_rio_selvagem_fg_left` | (a) pedido L6-02b |
| `Mapas/RioSelvagem/map_rio_selvagem_fg_right` | (a) pedido L6-02b |
| `Mapas/RioSelvagem/map_rio_selvagem_near_left` | (a) pedido L6-02b |
| `Mapas/RioSelvagem/map_rio_selvagem_near_right` | (a) pedido L6-02b |
| `Mapas/RioSelvagem/map_rio_selvagem_thumb` | (b) gerado das camadas do mapa já em pixel |
| `Mapas/RioSelvagem/map_rio_selvagem_water` | (c) água em faixas com brilhos e espuma animados, paleta do mapa |

### Mapas/TaludeNoturno (13)

Resumo: (a) 7 · (b) 1 · (c) 2 · (d) 3

| Arquivo | Como fica |
|---|---|
| `Mapas/TaludeNoturno/map_talude_noturno_bg_far` | (a) pedido L6-09a |
| `Mapas/TaludeNoturno/map_talude_noturno_bg_mid` | (a) pedido L6-09a |
| `Mapas/TaludeNoturno/map_talude_noturno_bg_near` | (a) pedido L6-09a |
| `Mapas/TaludeNoturno/map_talude_noturno_bg_sky` | (c) céu em faixas de cor da paleta do mapa (estrelas nos mapas de noite) |
| `Mapas/TaludeNoturno/map_talude_noturno_cloud_01` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/TaludeNoturno/map_talude_noturno_cloud_02` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/TaludeNoturno/map_talude_noturno_cloud_03` | (d) sai: as 6 nuvens comuns do pedido L1-07, tingidas por mapa |
| `Mapas/TaludeNoturno/map_talude_noturno_fg_left` | (a) pedido L6-09b |
| `Mapas/TaludeNoturno/map_talude_noturno_fg_right` | (a) pedido L6-09b |
| `Mapas/TaludeNoturno/map_talude_noturno_near_left` | (a) pedido L6-09b |
| `Mapas/TaludeNoturno/map_talude_noturno_near_right` | (a) pedido L6-09b |
| `Mapas/TaludeNoturno/map_talude_noturno_thumb` | (b) gerado das camadas do mapa já em pixel |
| `Mapas/TaludeNoturno/map_talude_noturno_water` | (c) água em faixas com brilhos e espuma animados, paleta do mapa |

### Peixes (100)

Master de cada espécie (100). Hoje 1024×512; a interface desenha com ScaleToFit em 32 a 240 px, a cena faz um sprite de 2 unidades. Não é 9-slice, não é tingido (a silhueta da Enciclopédia é tingida de escuro).

Resumo: (a) 100

| Arquivo | Como fica |
|---|---|
| `Peixes/fish_abrotea_fundo_master` | (a) pedido L4-09a |
| `Peixes/fish_agulha_branca_master` | (a) pedido L4-07a |
| `Peixes/fish_albacora_branca_master` | (a) pedido L4-07a |
| `Peixes/fish_ariaco_master` | (a) pedido L4-05a |
| `Peixes/fish_armado_master` | (a) pedido L4-03b |
| `Peixes/fish_arraia_chita_master` | (a) pedido L4-08b |
| `Peixes/fish_aruana_master` | (a) pedido L4-02b |
| `Peixes/fish_atum_amarelo_master` | (a) pedido L4-06b |
| `Peixes/fish_atum_patudo_master` | (a) pedido L4-07a |
| `Peixes/fish_badejo_master` | (a) pedido L4-05b |
| `Peixes/fish_bagre_marinho_master` | (a) pedido L4-04a |
| `Peixes/fish_barbado_master` | (a) pedido L4-03b |
| `Peixes/fish_batata_master` | (a) pedido L4-08a |
| `Peixes/fish_beijupira_master` | (a) pedido L4-06a |
| `Peixes/fish_bicuda_master` | (a) pedido L4-07a |
| `Peixes/fish_bonito_cachorro_master` | (a) pedido L4-06a |
| `Peixes/fish_bonito_pintado_master` | (a) pedido L4-07a |
| `Peixes/fish_cacao_anjo_master` | (a) pedido L4-08a |
| `Peixes/fish_cachara_master` | (a) pedido L4-02a |
| `Peixes/fish_camurupim_master` | (a) pedido L4-04b |
| `Peixes/fish_caranha_master` | (a) pedido L4-05b |
| `Peixes/fish_carapeba_master` | (a) pedido L4-04a |
| `Peixes/fish_carpa_master` | (a) pedido L4-01b |
| `Peixes/fish_cascudo_master` | (a) pedido L4-01a |
| `Peixes/fish_cavala_verdadeira_master` | (a) pedido L4-06a |
| `Peixes/fish_cavalinha_master` | (a) pedido L4-07a |
| `Peixes/fish_cherne_master` | (a) pedido L4-08a |
| `Peixes/fish_cioba_master` | (a) pedido L4-05b |
| `Peixes/fish_congro_negro_master` | (a) pedido L4-09a |
| `Peixes/fish_congro_rosa_master` | (a) pedido L4-08a |
| `Peixes/fish_corvina_master` | (a) pedido L4-04a |
| `Peixes/fish_curimbata_master` | (a) pedido L4-01a |
| `Peixes/fish_dentao_master` | (a) pedido L4-05b |
| `Peixes/fish_dourado_do_mar_master` | (a) pedido L4-06b |
| `Peixes/fish_dourado_master` | (a) pedido L4-02a |
| `Peixes/fish_enguia_pelicano_master` | (a) pedido L4-10a |
| `Peixes/fish_escolar_master` | (a) pedido L4-09a |
| `Peixes/fish_espadarte_master` | (a) pedido L4-07b |
| `Peixes/fish_jau_master` | (a) pedido L4-02b |
| `Peixes/fish_jundia_master` | (a) pedido L4-03a |
| `Peixes/fish_jurupensem_master` | (a) pedido L4-03a |
| `Peixes/fish_jurupoca_master` | (a) pedido L4-03b |
| `Peixes/fish_lambari_master` | (a) pedido L1-03 |
| `Peixes/fish_mandi_amarelo_master` | (a) pedido L4-03a |
| `Peixes/fish_marlim_azul_master` | (a) pedido L1-03 |
| `Peixes/fish_marlim_branco_master` | (a) pedido L4-07b |
| `Peixes/fish_matrinxa_master` | (a) pedido L4-01b |
| `Peixes/fish_mero_master` | (a) pedido L4-04b |
| `Peixes/fish_mucum_master` | (a) pedido L4-03a |
| `Peixes/fish_namorado_master` | (a) pedido L4-08a |
| `Peixes/fish_olhete_master` | (a) pedido L4-06a |
| `Peixes/fish_olho_de_boi_master` | (a) pedido L4-06b |
| `Peixes/fish_pacu_master` | (a) pedido L4-01a |
| `Peixes/fish_pacu_peva_master` | (a) pedido L4-03a |
| `Peixes/fish_pampo_master` | (a) pedido L4-05a |
| `Peixes/fish_parati_master` | (a) pedido L4-04a |
| `Peixes/fish_pargo_rosa_master` | (a) pedido L4-08a |
| `Peixes/fish_peixe_cachorra_master` | (a) pedido L4-02a |
| `Peixes/fish_peixe_dragao_master` | (a) pedido L4-10a |
| `Peixes/fish_peixe_espada_master` | (a) pedido L4-09a |
| `Peixes/fish_peixe_fita_gigante_master` | (a) pedido L4-10b |
| `Peixes/fish_peixe_galo_master` | (a) pedido L4-06a |
| `Peixes/fish_peixe_lanceta_master` | (a) pedido L4-09a |
| `Peixes/fish_peixe_lua_master` | (a) pedido L4-07b |
| `Peixes/fish_peixe_machado_master` | (a) pedido L4-10a |
| `Peixes/fish_peixe_ogro_master` | (a) pedido L4-10a |
| `Peixes/fish_peixe_opah_master` | (a) pedido L4-09b |
| `Peixes/fish_peixe_pescador_abissal_master` | (a) pedido L4-10a |
| `Peixes/fish_peixe_porco_master` | (a) pedido L4-05a |
| `Peixes/fish_peixe_vibora_master` | (a) pedido L4-10a |
| `Peixes/fish_pescada_amarela_master` | (a) pedido L4-04b |
| `Peixes/fish_piau_master` | (a) pedido L4-01a |
| `Peixes/fish_piavucu_master` | (a) pedido L4-03a |
| `Peixes/fish_pintado_master` | (a) pedido L4-02a |
| `Peixes/fish_piracanjuba_master` | (a) pedido L4-02a |
| `Peixes/fish_piranha_master` | (a) pedido L4-02a |
| `Peixes/fish_piraputanga_master` | (a) pedido L4-03b |
| `Peixes/fish_pirarucu_master` | (a) pedido L4-02b |
| `Peixes/fish_quimera_azul_master` | (a) pedido L4-10b |
| `Peixes/fish_quimera_master` | (a) pedido L4-09a |
| `Peixes/fish_raia_manta_master` | (a) pedido L4-08b |
| `Peixes/fish_robalo_peva_master` | (a) pedido L4-04a |
| `Peixes/fish_salema_master` | (a) pedido L4-05a |
| `Peixes/fish_sargentinho_master` | (a) pedido L4-05a |
| `Peixes/fish_sargo_master` | (a) pedido L4-05a |
| `Peixes/fish_serra_master` | (a) pedido L4-06a |
| `Peixes/fish_tainha_master` | (a) pedido L4-04a |
| `Peixes/fish_tambaqui_master` | (a) pedido L4-01b |
| `Peixes/fish_tilapia_master` | (a) pedido L4-01a |
| `Peixes/fish_traira_master` | (a) pedido L4-01a |
| `Peixes/fish_tubarao_boca_grande_master` | (a) pedido L4-10b |
| `Peixes/fish_tubarao_cobra_master` | (a) pedido L4-10b |
| `Peixes/fish_tubarao_duende_master` | (a) pedido L4-09b |
| `Peixes/fish_tubarao_lanterna_master` | (a) pedido L4-09b |
| `Peixes/fish_tubarao_lixa_master` | (a) pedido L4-08b |
| `Peixes/fish_tubarao_martelo_master` | (a) pedido L4-08b |
| `Peixes/fish_tubarao_seis_guelras_master` | (a) pedido L4-09b |
| `Peixes/fish_tucunare_master` | (a) pedido L1-03 |
| `Peixes/fish_veleiro_master` | (a) pedido L4-06b |
| `Peixes/fish_xareu_master` | (a) pedido L4-04b |

### Roupas (5)

Resumo: (a) 5

| Arquivo | Como fica |
|---|---|
| `Roupas/boots` | (a) pedido L5-03 |
| `Roupas/gloves` | (a) pedido L5-03 |
| `Roupas/hat` | (a) pedido L5-03 |
| `Roupas/pants` | (a) pedido L5-03 |
| `Roupas/shirt` | (a) pedido L5-03 |

### UI (73)

Molduras 9-slice (barra de cima, botões, vidro, cartas), fundos e faixas das telas, objetos (escudos, selos, ilhas).

Resumo: (a) 42 · (c) 19 · (d) 12

| Arquivo | Como fica |
|---|---|
| `UI/arena_fundo` | (a) pedido L7-04 |
| `UI/arena_vs` | (c) moldura/painel/pílula ARC desenhada pelo código |
| `UI/bemvindo_cena` | (a) pedido L7-05 |
| `UI/caixa_cena` | (a) pedido L7-06 |
| `UI/cla_brasao` | (a) pedido L3-07 |
| `UI/cla_fundo` | (a) pedido L7-09 |
| `UI/eventos_quadro` | (a) pedido L7-10 |
| `UI/merc_feira_porto` | (a) pedido L7-07 |
| `UI/passe_faixa` | (a) pedido L7-08 |
| `UI/passe_premium_selo` | (a) pedido L3-07 |
| `UI/perfil_carteira` | (c) moldura/painel/pílula ARC desenhada pelo código |
| `UI/ui_aquarium_bg` | (a) pedido L7-01 |
| `UI/ui_aquarium_bubble` | (c) moldura/painel/pílula ARC desenhada pelo código |
| `UI/ui_aquarium_glass` | (c) moldura/painel/pílula ARC desenhada pelo código |
| `UI/ui_aquarium_sand` | (a) pedido L7-02 |
| `UI/ui_atalho_moldura` | (c) moldura/painel/pílula ARC desenhada pelo código |
| `UI/ui_avatar_frame` | (c) moldura/painel/pílula ARC desenhada pelo código |
| `UI/ui_currency_coin` | (d) sai: a tela usa o ícone pequeno (ico_moeda, ico_concha, ico_dolar, ico_peixe, ico_nivel) |
| `UI/ui_currency_dollar` | (d) sai: a tela usa o ícone pequeno (ico_moeda, ico_concha, ico_dolar, ico_peixe, ico_nivel) |
| `UI/ui_currency_shell` | (d) sai: a tela usa o ícone pequeno (ico_moeda, ico_concha, ico_dolar, ico_peixe, ico_nivel) |
| `UI/ui_diario_etiqueta` | (d) sai: Diário de bordo trocado pela moldura ARC |
| `UI/ui_diario_lacre` | (d) sai: Diário de bordo trocado pela moldura ARC |
| `UI/ui_diario_madeira` | (d) sai: Diário de bordo trocado pela moldura ARC |
| `UI/ui_diario_marcador` | (d) sai: Diário de bordo trocado pela moldura ARC |
| `UI/ui_diario_papel` | (d) sai: Diário de bordo trocado pela moldura ARC |
| `UI/ui_enc_album_page` | (c) moldura/painel/pílula ARC desenhada pelo código |
| `UI/ui_enc_aura` | (c) moldura/painel/pílula ARC desenhada pelo código |
| `UI/ui_enc_binder_tab` | (c) moldura/painel/pílula ARC desenhada pelo código |
| `UI/ui_enc_card_back` | (c) moldura/painel/pílula ARC desenhada pelo código |
| `UI/ui_enc_card_frame` | (c) moldura/painel/pílula ARC desenhada pelo código |
| `UI/ui_enc_pedestal` | (a) pedido L3-07 |
| `UI/ui_exp_bussola` | (a) pedido L3-06 |
| `UI/ui_exp_carta` | (a) pedido L7-12 |
| `UI/ui_exp_marcador` | (a) pedido L3-06 |
| `UI/ui_exp_medalhao` | (a) pedido L3-06 |
| `UI/ui_exp_moldura_carta` | (c) moldura/painel/pílula ARC desenhada pelo código |
| `UI/ui_exp_pier` | (a) pedido L3-06 |
| `UI/ui_exp_porto` | (a) pedido L7-11 |
| `UI/ui_exp_rosa` | (a) pedido L3-06 |
| `UI/ui_exp_selo` | (a) pedido L3-06 |
| `UI/ui_logo_fishing_idle` | (c) moldura/painel/pílula ARC desenhada pelo código |
| `UI/ui_loja_balcao_pier` | (c) moldura/painel/pílula ARC desenhada pelo código |
| `UI/ui_loja_boia` | (a) pedido L3-07 |
| `UI/ui_loja_caixa` | (a) pedido L3-07 |
| `UI/ui_loja_gancho` | (a) pedido L3-07 |
| `UI/ui_loja_parede_noite` | (c) moldura/painel/pílula ARC desenhada pelo código |
| `UI/ui_map_boat` | (a) pedido L3-07 |
| `UI/ui_map_island_01` | (a) pedido L3-08 |
| `UI/ui_map_island_02` | (a) pedido L3-08 |
| `UI/ui_map_island_03` | (a) pedido L3-08 |
| `UI/ui_map_island_04` | (a) pedido L3-08 |
| `UI/ui_map_island_05` | (a) pedido L3-08 |
| `UI/ui_map_island_06` | (a) pedido L3-08 |
| `UI/ui_map_island_07` | (a) pedido L3-08 |
| `UI/ui_map_island_08` | (a) pedido L3-08 |
| `UI/ui_map_island_09` | (a) pedido L3-08 |
| `UI/ui_map_island_10` | (a) pedido L3-08 |
| `UI/ui_map_sea_bg` | (a) pedido L7-03 |
| `UI/ui_nav_button_active` | (c) moldura/painel/pílula ARC desenhada pelo código |
| `UI/ui_nav_button_base` | (c) moldura/painel/pílula ARC desenhada pelo código |
| `UI/ui_rk_coroa` | (a) pedido L3-05 |
| `UI/ui_rk_emblema_conchas` | (d) sai: a tela usa o ícone pequeno (ico_moeda, ico_concha, ico_dolar, ico_peixe, ico_nivel) |
| `UI/ui_rk_emblema_moedas` | (d) sai: a tela usa o ícone pequeno (ico_moeda, ico_concha, ico_dolar, ico_peixe, ico_nivel) |
| `UI/ui_rk_emblema_nivel` | (d) sai: a tela usa o ícone pequeno (ico_moeda, ico_concha, ico_dolar, ico_peixe, ico_nivel) |
| `UI/ui_rk_emblema_peixes` | (d) sai: a tela usa o ícone pequeno (ico_moeda, ico_concha, ico_dolar, ico_peixe, ico_nivel) |
| `UI/ui_rk_escudo_azul` | (a) pedido L3-05 |
| `UI/ui_rk_escudo_bronze` | (a) pedido L3-05 |
| `UI/ui_rk_escudo_ouro` | (a) pedido L3-05 |
| `UI/ui_rk_escudo_prata` | (a) pedido L3-05 |
| `UI/ui_rk_palco` | (a) pedido L7-13 |
| `UI/ui_rk_trofeu` | (a) pedido L3-05 |
| `UI/ui_topbar_frame` | (c) moldura/painel/pílula ARC desenhada pelo código |
| `UI/ui_wallet_inset` | (c) moldura/painel/pílula ARC desenhada pelo código |

### Varas (6)

Varas na Loja/Inventário (ScaleToFit) e na cena (sprite do cabo à ponta, com a linha saindo da ponta).

Resumo: (a) 6

| Arquivo | Como fica |
|---|---|
| `Varas/rod_00_starter` | (a) pedido L5-01 |
| `Varas/rod_01` | (a) pedido L5-01 |
| `Varas/rod_02` | (a) pedido L5-01 |
| `Varas/rod_03` | (a) pedido L5-01 |
| `Varas/rod_04` | (a) pedido L5-01 |
| `Varas/rod_05` | (a) pedido L5-01 |

### Varas/Cena (6)

Varas na Loja/Inventário (ScaleToFit) e na cena (sprite do cabo à ponta, com a linha saindo da ponta).

Resumo: (c) 6

| Arquivo | Como fica |
|---|---|
| `Varas/Cena/rod_00_starter` | (c) vara na cena desenhada pelo código (linha de 1–2 px nas cores da vara) |
| `Varas/Cena/rod_01` | (c) vara na cena desenhada pelo código (linha de 1–2 px nas cores da vara) |
| `Varas/Cena/rod_02` | (c) vara na cena desenhada pelo código (linha de 1–2 px nas cores da vara) |
| `Varas/Cena/rod_03` | (c) vara na cena desenhada pelo código (linha de 1–2 px nas cores da vara) |
| `Varas/Cena/rod_04` | (c) vara na cena desenhada pelo código (linha de 1–2 px nas cores da vara) |
| `Varas/Cena/rod_05` | (c) vara na cena desenhada pelo código (linha de 1–2 px nas cores da vara) |

### Vivos/Animais (119)

Paisagem viva: plantas que balançam (103) e animais animados por quadros (119; voo com 3–4 quadros a 6–14 quadros por segundo).

Resumo: (b) 119

Todos (b): convertidos com `pixel_folhas.py` na paleta do mapa; pedido extra só se algum ficar ruim.

### Vivos/Plantas (103)

Resumo: (b) 103

Todos (b): convertidos com `pixel_folhas.py` na paleta do mapa; pedido extra só se algum ficar ruim.

## Arquivos novos (não existem hoje)

`Cena/boia`, `Cena/pescador_arremesso_1`, `Cena/pescador_arremesso_2`, `Cena/pescador_arremesso_3`, `Cena/pescador_arremesso_4`, `Cena/pescador_escapou_1`, `Cena/pescador_fisgada_1`, `Cena/pescador_fisgada_2`, `Cena/pescador_mostra_1`, `Cena/pescador_parado_1`, `Cena/pescador_parado_2`, `Cena/pescador_parado_3`, `Cena/pescador_parado_4`, `Cena/pescador_recolhe_1`, `Cena/pescador_recolhe_2`, `Cena/pescador_recolhe_3`, `Cena/pescador_recolhe_4`, `Mapas/Comum/nuvem_01`, `Mapas/Comum/nuvem_02`, `Mapas/Comum/nuvem_03`, `Mapas/Comum/nuvem_04`, `Mapas/Comum/nuvem_05`, `Mapas/Comum/nuvem_06`
