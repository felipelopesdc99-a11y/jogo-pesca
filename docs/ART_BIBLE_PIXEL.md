# FISHING IDLE — BÍBLIA DE ARTE EM PIXEL ART (ESTILO ARC)

**Fonte de verdade visual do jogo desde 19/10/2026.** Escolha do proprietário entre três propostas: o **Estilo 1
"ARC"**, pixel art no traço das artes do ARC MMO (GDD_ADENDO A-182). Este documento substitui o
`docs/ART_BIBLE_V0_1.md` ("Lago Dourado — Clean Premium") como fonte visual; a Bíblia antiga fica no repositório,
travada e sem edições, como histórico das decisões da V0.1.

Como a antiga, esta Bíblia não se edita para registrar um detalhe decidido na implementação: o detalhe vai para o
`docs/GDD_ADENDO.md`. Uma mudança de direção visual só entra aqui por decisão do proprietário.

Documentos irmãos:

| Assunto | Arquivo |
|---|---|
| Lista viva das artes (inventário, lotes, nomes de arquivo, status) | `docs/ASSETS_PIXEL.md` |
| Como cada folha é recortada (grade, nomes, contorno, âncora) | `tools/Arte/pixel_pedidos.json` |
| Recorte e limpeza das folhas do ChatGPT | `tools/Arte/pixel_folhas.py` (teste: `tools/Arte/pixel_folhas_teste.py`) |
| Decisão técnica do pipeline (importação, câmera, fontes, pastas) | `docs/DECISOES.md`, TD-059 |
| Tarefas da troca de estilo | `docs/roadmap.json`, M25 |

---

## 1. Em uma frase

> **Uma pintura pastel em pixel art, calma de olhar por horas, com personagens chibi e peixes de contorno escuro,
> sob uma interface azul-noite de moldura dourada fina, botões-pílula e duas fontes pixel.**

As referências mandadas pelo proprietário (cena do píer 320×256 com ~26 cores, pescador chibi ~77×96 em 8 quadros,
peixes ~100×60, siri, gaivota, barco, baú) servem **só de estilo**. Nenhuma delas entra no jogo, nem recortada.

---

## 2. O que muda e o que não muda

### Muda (só apresentação)

- Toda a arte do jogo vira pixel art: cenários dos 10 mapas, pescador, barcos, peixes, itens, ícones, retratos,
  fundos das telas e o Boss Mundial.
- A interface ganha a pele ARC, desenhada pelo código: painéis azul-noite, moldura dourada fina, pílulas, barras e
  abas em pixel. As peças pintadas da pele atual (Diário de bordo, molduras 9-slice, botões pintados) saem.
- As fontes passam a ser **Press Start 2P** (títulos e números curtos) e **VT323** (textos), as duas com licença OFL.
- Céu e água dos mapas passam a ser desenhados pelo código, em faixas de cor da paleta de cada mapa.
- Ícones passam a ser coloridos (hoje são brancos e tingidos pelo código), menos os 12 símbolos simples.
- A cena é desenhada numa grade de 216 pixels de altura e ampliada sem suavizar.

### Não muda

- **Nenhuma regra, número, sorteio, preço, recompensa ou save.** A parte visual continua só mostrando o que o serviço
  do jogo informa (CLAUDE.md, seção 2).
- Nenhum sistema, moeda, raridade ou tela nova por causa da referência visual.
- As raridades e as cores delas (`Resources/Visual/tema_visual.json`): Comum branco, Raro azul, Épico roxo, Lendário
  vermelho-rubi, Mítico rosa-framboesa. O dourado do tamanho **Excepcional** continua sendo só dele.
- Todo texto continua em PT-BR, em `Texts/GameTexts.cs`, com a formatação brasileira de `Texts/Format.cs`.
- A organização das telas, os menus, os atalhos e o que cada botão faz.
- Os princípios da Bíblia antiga que valem para qualquer estilo: o jogo vem antes da interface; o peixe é o
  protagonista; brilho é recompensa (nada brilha o tempo todo); raridade é acento, não tinta; tamanho e raridade nunca
  se confundem; em dúvida entre mais efeito e mais limpo, **mais limpo**.

### Dourado: estrutura x recompensa

A moldura dourada fina da pele ARC é **estrutura**: um dourado envelhecido (`#D6AD57` e o tom escuro `#7D6127`),
sempre de 1 pixel de interface, sem brilho. O **dourado de recompensa** é o amarelo vivo (`#FFD667`, claro
`#FFE3A0`) com piscada ou brilho, e continua reservado a moedas, recordes, recompensas e ao Excepcional.

---

## 3. Grades, escala e unidades

Tudo que é pixel art aparece em **múltiplos inteiros** do seu pixel. Nunca se estica, gira (a não ser em 90°) ou
suaviza um sprite pixel.

### 3.1 A cena de pesca

| Item | Valor |
|---|---|
| Grade da cena | **216 pixels de altura**; a largura acompanha a janela (384 em 16:9, 432 em 2:1) |
| Câmera | ortográfica de 10,8 unidades de altura (a mesma de hoje) |
| Pixels por unidade (PPU) | **20** (216 ÷ 10,8): as posições da cena em unidades continuam valendo |
| Ampliação | 1080p = 5×, 4K = 10×; quando a altura não dá um inteiro (1440p = 6,67×), amplia pelo inteiro de cima e reduz com filtro bilinear ("nítido-suave"), sem faixas pretas |
| Movimento | tudo que anda na cena para em pixel inteiro da grade (barco, boia, nuvens, animais) |

A grade de 216 dá 5× exato no 1080p, que é o quadro virtual da interface, e mantém as 10,8 unidades da câmera, então
nenhuma posição da cena em `/config` ou em `Resources/Visual/*.json` precisa mudar por causa da grade. (As propostas
falavam em ~320×180; 384×216 é a mesma ideia com a conta exata do jogo.)

### 3.2 A interface

O HUD continua no quadro virtual de 1080 de altura. **Um pixel de arte de interface = 2 pixels do quadro** (escala 2×).
Os desenhos pequenos podem aparecer a 1× (lista compacta) e os grandes a 3× (ficha do peixe, Boss), sempre inteiros.
A função de desenho troca o `ScaleToFit` de hoje pela **maior escala inteira que cabe** e, se nem 1× cabe, usa a versão
pequena `_p` (metade) do peixe.

### 3.3 Resolução nativa de cada tipo

| Tipo | Grade nativa | Na tela | Pedido |
|---|---|---|---|
| Cenário — faixa distante (`bg_far`) | 384 × ~48, repete na horizontal, sem contorno | cena, PPU 20 | (a) |
| Cenário — faixa do meio (`bg_mid`) | 384 × ~40, repete, sem contorno | cena | (a) |
| Cenário — margem (`bg_near`) | 384 × ~40, pontas cheias e centro vazio | cena | (a) |
| Cenário — laterais (`near_left/right`) | ~128 × 96 | cena | (a) |
| Cenário — cantos da frente (`fg_left/right`) | ~96 × 80 | cena | (a) |
| Nuvens (6, comuns a todos os mapas, tingidas) | até 96 × 48 | cena | (a) |
| Céu e água | faixas de cor desenhadas pelo código | cena | (c) |
| Plantas e animais da paisagem viva | a altura de hoje em unidades × 20 (ex.: pato voando 0,42 un → ~8 px) | cena | (b) |
| Pescador (todas as poses e quadros) | quadro 64 × 64, figura ~48 px de altura, sentado | cena (3,2 un de quadro) | (a) |
| Barcos | 104–124 × 36–52 (célula 128 × 64), proa à direita | cena e interface 1×/2× | (a) |
| Caixa de Pesca (no barco) | ~44 × 32 | cena | (a) |
| Boia | ~8 × 14 | cena | (a) |
| Vara na cena | linha de 1–2 px com cabo e molinete, nas cores da vara | cena | (c) |
| Peixe (master) | até 120 × 60 (raias, peixe-lua e peixes altos até 100 de altura), virado à direita | interface 1×/2×/3× | (a) |
| Peixe pequeno `_p` | metade do master, gerado pelo script | listas compactas e o peixe pulando na cena | (b) |
| Ícone pequeno | 24 × 24 | interface 1× (24) ou 2× (48) | (a) |
| Ícone grande (abas, atalhos) | 32 × 32 | interface 2× (64) | (a) |
| Símbolos simples (+, X, ✓, setas, play, pausa, parar, ordenar, compacto, i, !) | 1 cor, desenhados pelo código, tingíveis | interface | (c) |
| Objetos de interface (escudos, selos, coroa, troféu, rosa dos ventos…) | até 48 × 48 | interface 2× | (a) |
| Ilhas da tela do Mapa | 64 × 64 | interface 2× | (a) |
| Pódios da Arena | ~56 × 30–52 | interface 2× | (a) |
| Retratos (pescador e 6 avatares) | 64 × 64, busto | interface 1× (barra de cima) / 2× | (a) |
| Varas (item) | 48 × 48, na diagonal | interface 2× | (a) |
| Iscas, roupas, fragmentos | 32 × 32 | interface 2× | (a) |
| Imagens das expedições | 96 × 64 (cena completa) | interface 2×/3× | (a) |
| Fundos e faixas das telas | 384 × 256 de grade (usa 384 × 216, 384 × 168 ou 384 × 120) | interface na maior escala inteira, cortando as bordas | (a) |
| Boss Mundial (4 poses) | 160 × 112, virado à esquerda | interface 2×/3× | (a) |

(a) pedido ao ChatGPT · (b) convertido automaticamente · (c) desenhado pelo código. O detalhe de cada arquivo está em
`docs/ASSETS_PIXEL.md`.

---

## 4. Regras de desenho

1. **Pixel de verdade:** pixels quadrados e chapados, sem anti-aliasing, sem degradê suave, sem desfoque, sem
   sub-pixel. Degradês de céu e água são faixas de cor, com no máximo uma linha de pontilhado (2×2 ordenado) na
   transição.
2. **Contorno:** 1 pixel escuro em volta de personagens, peixes, itens e objetos de primeiro plano, **puxado para a cor**
   do que contorna (cerca de 32% da cor vizinha, somado a um toque de azul-noite). Planos distantes (faixas de fundo,
   nuvens, céu) não têm contorno: separam-se pelo valor da cor.
3. **Luz:** de cima à esquerda em todo o jogo. Sombra em 3 a 4 tons por cor; um brilho de 1 pixel na borda do lado da
   luz.
4. **Cores:** no máximo 32 por sprite (48 em cenários e cenas). Paleta de cada mapa na seção 6.
5. **Proporções:** humanos chibi, cerca de 2,5 cabeças de altura, olhos expressivos. Peixes e animais com a forma real
   da espécie, simplificada e de silhueta forte. Todos os peixes ocupam o mesmo comprimento de desenho (~112 px),
   qualquer que seja o tamanho real.
6. **Direção:** peixes e pescador virados para a **direita** (como a arte de hoje, que o código espelha quando
   precisa); o Boss olha para a **esquerda**, de frente para o jogador.
7. **Raridade no peixe:** só um detalhe, nunca texto: Épico, brilho roxo de 1 px em parte da borda de cima; Lendário,
   brilho vermelho-rubi e um reflexo extra; Mítico, brilho rosa-framboesa e 2 ou 3 pontinhos de brilho. Comum e Raro
   sem brilho. O selo, a borda do card e o resto continuam com o código (cores do `tema_visual.json`).
8. **Nada de texto** em arte nenhuma: letras, números e logos são do código (o logo "Fishing Idle" é escrito em
   Press Start 2P pelo jogo).
9. **Sem rotação:** sprites pixel não giram (o balanço do peixe no Aquário vira deslocamento de 1 px e espelhamento).
   Espelhar é permitido.
10. **Sem brilho difuso:** "brilho" em pixel é um contorno de 1 px que pisca, um anel pontilhado ou estrelinhas de
    4 quadros, curtos.

---

## 5. Animação

Quadros no mesmo tamanho de célula, personagem no mesmo lugar (âncora na base), mudando só o movimento.

| Animação | Quadros | Velocidade | Fase do jogo |
|---|---:|---|---|
| Pescador esperando | 4 | 4 por segundo, em loop | Parado / Esperando |
| Pescador arremessando | 4 | 10 por segundo, uma vez | Arremessando |
| Pescador recolhendo | 4 | 8 por segundo, em loop | Recolhendo |
| Pescador fisgando | 2 | 10 por segundo, uma vez | Fisgou |
| Pescador mostrando o peixe | 1 (o peixe é desenhado pelo código nas mãos) | — | Mostrando |
| Pescador quando o peixe escapa | 1 | — | Escapou |
| Boss Mundial | 3 poses alternadas (parado, respira, ataque) | os tempos de hoje (A-178) | Luta |
| Animais da paisagem viva | os quadros de hoje (2–4) | os de hoje (6–14 por segundo) | Paisagem viva (A-078) |
| Plantas | 1 quadro; o balanço do vento é deslocamento por faixas de 1 px | contínuo | Paisagem viva |
| Água | brilhos e espuma que trocam de lugar em passos de pixel | a cada 0,25–0,5 s | Sempre |
| Boia | sobe e desce 1 px; mergulha 2–3 px na fisgada | — | Esperando / Fisgou |
| Nuvens | deslizam em passos inteiros de pixel da cena | lento | Sempre |

---

## 6. Paletas

### 6.1 Interface ARC (desenhada pelo código)

| Token | Cor | Uso |
|---|---|---|
| Painel | `#0D0F20` (opacidade 0,93) | fundo de janelas e barras |
| Painel elevado | `#161A35` | aba ativa, item selecionado |
| Campo | `#090B18` | campos, pílulas secundárias, fundo de barras |
| Card | `#141832` (linha interna `#0B0D1E`) | cards de peixe e de item |
| Moldura | `#D6AD57` | linha dourada de 1 px de interface |
| Moldura escura | `#7D6127` | segunda linha da moldura, bordas de campos |
| Linha interna | `#0A0C1A` | sombra interna entre as duas linhas da moldura |
| Texto | `#F4ECD6` | texto principal |
| Texto suave | `#A7A28F` | texto secundário |
| Título | `#FFE3A0` | títulos em Press Start 2P |
| Ouro de recompensa | `#FFD667` | moedas, recordes, seleção, estrelas |
| Chip inativo | borda `#2A2E50` | filtros e pílulas desligadas |
| Barra (fundo) | `#22264A` | barras de XP, atributos, ciclo |
| Barra de XP | listras `#7FD6C2` / `#62C0AD` | XP do pescador |
| Barra de atributo | `#7FC4EA` | atributos e genes |
| Alerta | `#E0473A` (borda `#FFD667`) | contadores de aviso |
| Botão principal | `#FFD977` → `#D6A440` em 2 faixas, texto `#2A1A08`, borda `#FFF0B8` | ação principal (Vender, Montar, Iniciar) |
| Botão secundário | `#090B18`, borda `#D6AD57`, texto `#FFE3A0` | ações comuns |
| Perigo | o `danger` do `tema_visual.json` | ações destrutivas |

As raridades, as categorias de tamanho e o Excepcional mantêm as cores do `tema_visual.json`. As cores novas entram
nesse mesmo arquivo (chaves técnicas novas), para continuar dando para ajustar tudo sem mexer no código.

### 6.2 Cenários (céu e água pelo código; o resto pelos pedidos)

Céu do alto para o horizonte; água da superfície para o fundo.

| Mapa | Luz | Céu | Água | Terra / detalhe |
|---|---|---|---|---|
| 1 · Lago Sereno | manhã pastel, névoa | `#FCFCE9` `#F9FAE4` `#F5F7DF` `#F0F4DB` | `#DCEDE4` `#CFE5DD` `#C0DBD4` `#AECDC8` `#99BDBA` `#83A9A8` | névoa `#E3EBE0`, morros `#CCDCD4`, mata `#6F9A6A` `#4F7A55`, madeira `#8F7672` |
| 2 · Rio Selvagem | manhã fria | `#EEF6F4` `#E2F0EE` `#D3E8E6` `#C4DEDD` | `#BFE0DC` `#9FCFCC` `#7DBAB8` `#5EA3A3` `#468A8C` `#34707A` | mata `#4F8A5B` `#2F6A45`, pedra `#8A9A90`, espuma `#F4FBF8` |
| 3 · Pantanal Dourado | amanhecer dourado | `#FFF1CF` `#FDE4B0` `#F9D494` `#F3C37F` | `#E9DCB0` `#D6CFA4` `#BFC19A` `#A6B18F` `#8C9E84` `#738A78` | verde úmido `#8FA85C` `#6B8A45`, terra `#9A7A52` |
| 4 · Estuário das Marés | pôr do sol costeiro | `#FFD8A8` `#F7BB88` `#E99D78` `#D4826F` | `#8AA59A` `#6F8F88` `#587A76` `#446663` `#335250` `#243F3F` | mangue `#3F5A3C` `#1C2D22`, raízes `#6B4A33`, reflexo `#F2B67A` |
| 5 · Costa de Coral | manhã clara | `#E9F7FB` `#D7F0F7` `#C1E7F2` `#A9DCEC` | `#A6ECE6` `#7FDCD8` `#58C8C8` `#3FB0B8` `#2F95A6` `#237A90` | areia `#F2E3C0`, falésia `#7FB07A`, coral `#E98A6B` |
| 6 · Arquipélago do Sol | fim de manhã | `#DFF0FB` `#C8E5F8` `#B0D8F3` `#97C9EC` | `#6AA6E0` `#4D8BD0` `#3A72BD` `#2C5CA6` `#22498E` `#193875` | rocha `#8A8F96`, verde-ilha `#5E9A58`, sol `#F6D27A` |
| 7 · Corrente Azul | fim de tarde, mar aberto | `#8FBDE6` `#A9CBE8` `#D3DFE6` `#FBE1B0` | `#4F86D1` `#3D70BF` `#2F5BA8` `#24498F` `#1B3976` `#142B5E` | espuma `#EEF4FB` |
| 8 · Banco das Baleias | amanhecer frio | `#B9CDD8` `#CDD9DD` `#E2DFD8` `#F3D9C8` | `#6F9A97` `#5B8784` `#497370` `#3A605E` `#2D4E4D` `#213D3D` | ilhotas `#7E8A8F`, pérola `#E9EEF0` |
| 9 · Talude Noturno | noite, lua baixa | `#0D1430` `#141C3E` `#1D2750` `#2A3463` | `#1C2B4F` `#172443` `#121D38` `#0E172E` `#0A1224` `#070D1B` | lua `#E8ECF2`, plâncton `#5FD6E0`, violeta `#6C5A8E` |
| 10 · Abismo Atlântico | noite profunda | `#05070F` `#080B18` `#0B1022` `#10172E` | `#0B1530` `#091228` `#070E20` `#050A18` `#040712` `#02040B` | estrelas `#DFE6F5`, ciano `#3FB7C9`, violeta `#3D2F5C` |

Nos mapas 9 e 10 o céu ganha estrelas de 1 px pelo código; nos outros, nenhuma. A identidade de cada mapa continua a
de `docs/IDENTIDADE_POR_MAPA.md` e `docs/PROGRESSAO_MAPAS_5_A_10.md`.

---

## 7. A interface ARC

- **Painel:** fundo Painel; moldura de 3 linhas de pixel de interface: dourada por fora, linha interna escura, dourada
  escura por dentro; cantos com 2 px de chanfro (degrau), nunca arredondados por suavização.
- **Pílula:** botões, chips, avisos e a placa do mapa são pílulas: as pontas são semicírculos em degraus de pixel.
- **Abas:** a ativa é uma pílula Painel elevado com moldura dourada e texto Título; as outras, só texto suave e ícone.
- **Cards:** fundo Card, borda de 1 px na cor da raridade, selo da raridade em pílula sólida; selecionado ganha um
  contorno externo Ouro de recompensa.
- **Barras:** fundo Barra, preenchimento em blocos de pixel (XP em listras), borda Moldura escura.
- **Atalhos redondos:** disco Painel com moldura dourada e o ícone grande colorido; o rótulo vai numa pílula embaixo.
- **Molduras e painéis são gerados pelo código** como pequenas texturas 9-slice, criadas uma vez e reaproveitadas
  (filtro Point), na escala de interface. Nada novo a cada quadro.
- **Véu** atrás das janelas: `#0A0C1C` a 45%.

### 7.1 Fontes

| Fonte | Uso | Tamanhos no quadro de 1080 |
|---|---|---|
| **Press Start 2P** (OFL) | títulos de janela, nome do mapa, nível, números grandes, selos curtos | 16, 24 e 32 (múltiplos de 8); 16 é o mínimo |
| **VT323** (OFL) | todo o resto: textos, rótulos, listas, botões, avisos | corpo 28; pequeno 24; tags e chips 22 (mínimo absoluto) |

A VT323 tem letra miúda: nada que informa fica abaixo de 22 no quadro de 1080, e textos longos (Missões, Mercado,
descrições) usam 28. Em janela pequena (cerca de 600 px de altura) o quadro encolhe para ~0,56×; a legibilidade
nesse tamanho só fica confirmada quando o proprietário apertar Play (A-182). As fontes vão para
`Resources/Fontes/` com os arquivos `OFL-*.txt`, como as de hoje (TD-059).

---

## 8. Importação no Unity e câmera

Detalhes técnicos e o porquê em TD-059. Resumo:

- **Pasta nova:** `Assets/Resources/ArtePixel/`, com as mesmas subpastas e nomes de `Arte/`. O carregador procura em
  `ArtePixel` primeiro e cai na arte antiga enquanto um arquivo não existir, então a troca é feita aos poucos sem
  quebrar nada. A última tarefa do M25 apaga o que sobrar em `Arte/`.
- **Importação de `ArtePixel`:** filtro **Point**, **sem compressão**, sem mipmaps, Clamp, sem redimensionar para
  potência de 2, sem limite de tamanho, alfa como transparência. Sprites da cena com **PPU 20** e pivô vindo do
  config visual de hoje.
- **Câmera da cena:** a cena é desenhada numa textura de 216 px de altura (largura pela proporção da janela) com filtro
  Point e ampliada na tela pelo inteiro (ou "nítido-suave" quando não cabe inteiro). É uma implementação própria, sem
  o pacote 2D Pixel Perfect. O HUD (IMGUI) continua desenhado por cima, na resolução da tela.

---

## 9. Fluxo de produção

1. O proprietário cola as instruções e os pedidos da página "Pedidos de Pixel Art" no ChatGPT, lote por lote.
2. Cada pedido devolve **uma folha** (várias células, fundo magenta), salva com o nome do pedido.
3. `python3 tools/Arte/pixel_folhas.py FOLHA.png --pedido <código>` recorta, acerta a grade (o fator de ampliação é
   detectado, inteiro ou não), reduz a paleta, limpa pixels soltos, põe contorno onde falta e grava em `ArtePixel/`
   com os nomes certos.
4. O que é convertido (b) passa pelo mesmo script, a partir da arte atual, com a paleta do mapa.
5. O `docs/ASSETS_PIXEL.md` marca o status de cada pedido (`PEDIR`, `RECEBIDO`, `NO_JOGO`).
6. Visual só conta como verificado depois que o proprietário apertar Play.

Correções vêm da mesma página ("Se vier errado, cole isto"). Uma folha ruim é refeita inteira, para a série continuar
consistente.

---

## 10. Checklists

### Cada sprite

- [ ] pixels quadrados, do mesmo tamanho, sem borrão nem degradê;
- [ ] na grade nativa da tabela 3.3 (sem ampliação embutida);
- [ ] contorno de 1 px puxado para a cor (menos nos planos distantes);
- [ ] luz de cima à esquerda, 3–4 tons de sombra;
- [ ] até 32 cores (48 em cenários);
- [ ] virado para o lado certo (peixes e pescador à direita, Boss à esquerda);
- [ ] parece a espécie ou o item de verdade;
- [ ] sem texto, sem chão, sem sombra no chão;
- [ ] mesma escala de pixel dos outros da mesma série.

### Cada tela

- [ ] a cena continua sendo o mais importante;
- [ ] tudo em escala inteira, nada esticado, girado ou suavizado;
- [ ] moldura e pílulas da pele ARC, sem peças da pele antiga;
- [ ] textos em PT-BR, no tamanho mínimo da seção 7.1;
- [ ] dourado vivo só em recompensa;
- [ ] raridade como acento, nas cores do `tema_visual.json`;
- [ ] nenhuma mecânica nova;
- [ ] sem sobreposição em ~1200×600 e em ~1320×840 (e em 1920×1080).
