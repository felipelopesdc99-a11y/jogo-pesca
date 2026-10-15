# FISHING IDLE — ART BIBLE VISUAL V0.1
## Fonte de verdade visual para Claude + Unity
**Idioma de toda interface visível:** Português do Brasil (PT-BR)  
**Engine:** Unity  
**Direção geral:** 2.5D bonito, relaxante, limpo e legível, com celebrações pontuais fortes  
**Nome interno da direção:** **Lago Dourado — Clean Premium**

---

# 0. INSTRUÇÃO OBRIGATÓRIA PARA O CLAUDE

Este documento é a **fonte de verdade visual da V0.1**.

Ao trabalhar na interface, cenas, prefabs, cards, animações, efeitos e assets:

1. **Não inventar uma nova linguagem visual** sem aprovação do proprietário.
2. Manter a interface em **PT-BR**.
3. Preservar todas as mecânicas já implementadas; este documento trata principalmente de apresentação visual.
4. Não adicionar moedas, recursos, sistemas, pets, iscas, equipamentos ou raridades apenas porque aparecem em uma referência visual.
5. Todo asset importante deve ser **substituível**, sem obrigar a refazer layout ou lógica.
6. Se um asset final ainda não existir, usar placeholder limpo e registrar no painel/roadmap como `ASSET_PENDENTE`.
7. Não criar a interface como site/dashboard empresarial. Ela deve parecer um **jogo casual premium para PC**, não uma aplicação SaaS.
8. Priorizar consistência. Um conjunto menor de elementos bem feitos é melhor do que dezenas de estilos diferentes.
9. Evitar “cara de IA”: não usar exagero de brilho, pintura hiper detalhada inconsistente, texturas aleatórias ou ornamentação sem função.
10. O jogo deve continuar agradável quando deixado aberto por horas em um segundo monitor.

---

# 1. VISÃO VISUAL

Fishing Idle deve transmitir:

- calma;
- água;
- contemplação;
- coleção;
- progressão;
- pequenas surpresas;
- sensação de “quero deixar isso aberto enquanto faço outra coisa”.

A tela normal deve ser bonita mesmo quando nada importante está acontecendo.

A recompensa não deve estar em toda parte.  
Ela deve **entrar em cena quando algo realmente acontece**.

## Regra principal

> **Calmo no dia a dia. Dourado na recompensa.**

O jogo combina duas ideias:

### Base — estilo limpo
- painéis escuros;
- leitura imediata;
- bordas simples;
- turquesa para ações;
- cards consistentes;
- pouca poluição;
- peixe e informação como protagonistas.

### Camada emocional
- luz quente de fim de tarde;
- reflexos na água;
- pequenos brilhos;
- respingos;
- partículas;
- animação curta de recompensa;
- dourado apenas em momentos especiais.

A interface **não deve brilhar inteira**.

---

# 2. PRINCÍPIOS NÃO NEGOCIÁVEIS

## 2.1 O jogo vem antes da interface

O jogador deve enxergar:

1. cenário;
2. pescador/barco/peixe;
3. ação atual;
4. interface.

A HUD nunca deve roubar permanentemente o foco da cena.

---

## 2.2 Visual first, data second

Sempre que possível:

- peixe grande visualmente;
- nome e informação abaixo;
- dados numéricos como suporte.

Evitar telas compostas apenas de textos, linhas e números.

---

## 2.3 Uma linguagem única

Todos os menus devem compartilhar:

- mesmo formato de painel;
- mesmo raio de borda;
- mesma hierarquia tipográfica;
- mesma linguagem de botões;
- mesmos espaçamentos;
- mesma lógica de raridade;
- mesmos ícones;
- mesma intensidade de sombra.

O usuário deve sentir que Mercado, Aquário, Arena e Loja pertencem ao mesmo jogo.

---

## 2.4 Brilho é recompensa

Glow permanente = não.

Glow curto em algo importante = sim.

Se tudo brilha, nada parece raro.

---

# 3. PALETA BASE

## Interface

| Token | Uso | Cor inicial |
|---|---|---|
| Noite do Lago | fundo escuro profundo | `#0A1422` |
| Painel | janelas e cards | `#11223A` |
| Painel Elevado | cards selecionados / superfícies elevadas | `#183050` |
| Borda | contorno neutro | `#274A70` |
| Texto | texto principal | `#EEF4FB` |
| Texto Suave | secundário | `#9DB2C9` |
| Turquesa | ação, seleção, menu ativo | `#25C4C1` |
| Ouro | prêmio, moedas, recordes | `#F6B93B` |
| Ouro Claro | highlight de recompensa | `#FFE08A` |
| Perigo | confirmação destrutiva | `#EF6B5B` |

## Regra de cor

**Turquesa = posso fazer algo.**  
**Dourado = ganhei / consegui algo valioso.**  
**Vermelho = ação destrutiva ou alerta real.**

Não usar dourado como cor genérica de interface.

---

# 4. CORES DE RARIDADE

A raridade deve ser reconhecida em menos de 1 segundo.

## Slots visuais previstos

> Usar apenas as raridades realmente existentes nos dados do jogo.  
> Nunca criar um tier novo apenas porque existe uma cor preparada.

| Tier visual | Cor | Hex sugerido |
|---|---|---|
| Comum | azul acinzentado | `#8193A8` |
| Incomum | verde | `#2CCB7F` |
| Raro | azul | `#4D8DFF` |
| Épico | roxo | `#A855F7` |
| Lendário | dourado | `#F6B93B` |
| Tier máximo futuro | coral/vermelho | `#EF6B5B` |

## Onde a cor da raridade aparece

A cor pode alterar:

- borda do card;
- selo de raridade;
- pequeno detalhe da barra;
- texto do nível ou detalhe de destaque;
- glow muito leve em tiers realmente altos;
- estado selecionado.

## Onde NÃO aparece

Não pintar:

- card inteiro;
- fundo da tela inteira;
- peixe inteiro;
- todos os textos;
- todos os botões.

### Regra visual

O card continua escuro/neutro.

A raridade é um **acento**, não uma tinta jogada em toda a tela.

---

# 5. TAMANHO E RARIDADE NÃO PODEM SE CONFUNDIR

O sistema de tamanho do peixe e a raridade da espécie são informações diferentes.

Exemplo:

- raridade controla **cor do card/selo de raridade**;
- categoria de tamanho recebe um **selo próprio**, separado.

Se o jogo usar a categoria de tamanho **Excepcional**, ela não deve ser confundida visualmente com um tier de raridade.

## Tamanho Excepcional

Tratamento sugerido:

- pequeno selo especial;
- dourado suave;
- gleam lento;
- sem mudar toda a borda se a borda já representa raridade.

Assim um peixe pode ser, por exemplo:

> **Raro + Tamanho Excepcional**

e o jogador entende as duas informações imediatamente.

---

# 6. CARDS DE PEIXE — TEMPLATE OFICIAL

Todos os cards usam a mesma anatomia.

## Estrutura

1. selo da raridade no topo;
2. arte do peixe;
3. nome da espécie;
4. tamanho em cm;
5. categoria de tamanho;
6. nível;
7. barra de XP quando relevante;
8. preço ou valor quando a tela exigir;
9. botão/ação apenas quando necessário.

## Não fazer

- colocar 8 ícones sem explicação;
- mudar completamente o layout por raridade;
- preencher o card inteiro com a cor da raridade;
- usar fontes diferentes por tier;
- adicionar molduras ornamentadas gigantes.

## Hover / seleção

Hover:

- card sobe muito pouco;
- borda clareia;
- sombra aumenta discretamente.

Selecionado:

- contorno claro;
- fundo levemente elevado;
- nunca usar zoom exagerado.

---

# 7. ESTILO DOS PEIXES

## Direção

**Semi-realista estilizado / ilustração limpa de jogo casual premium.**

Não deve parecer:

- fotografia recortada;
- pintura hiper realista;
- clipart infantil;
- render 3D genérico;
- arte com excesso de detalhes de IA.

## Regras

- vista lateral;
- direção consistente;
- anatomia reconhecível;
- silhueta específica da espécie;
- olhos, nadadeiras e padrão coerentes;
- shading suave;
- textura controlada;
- contraste suficiente para funcionar pequeno;
- fundo transparente no sprite master.

## Consistência

Todos os peixes devem parecer feitos pelo **mesmo artista**.

Não misturar:

- um peixe flat;
- outro realista;
- outro 3D;
- outro aquarela.

---

# 8. PIPELINE DE SPRITES DE PEIXE

## Master asset

Para cada espécie:

`fish_<species>_master.png`

Sugestão:

- PNG transparente;
- 2048 × 1024 como master de trabalho;
- peixe centralizado;
- sem sombra externa baked;
- sem glow;
- sem texto;
- sem raridade;
- sem fundo.

Unity gera versões menores via import/downscale.

## Usos derivados

O mesmo master deve servir para:

- Caixa de Pesca;
- Aquário;
- Mercado;
- Cardume;
- Arena;
- Enciclopédia;
- toast de captura;
- tela de recorde.

## Silhueta da Enciclopédia

Gerar em runtime ou asset derivado:

- alpha do peixe;
- preenchimento escuro;
- sem revelar cor.

Não desenhar uma segunda espécie só para fazer a silhueta.

---

# 9. CENÁRIO PRINCIPAL — PESCA

## Composição

Cena 2.5D lateral.

Elementos:

- lago/rio;
- barco;
- pescador;
- vara;
- boia/linha;
- montanhas/mata;
- vegetação próxima;
- reflexos;
- céu;
- elementos ambientais discretos.

## Movimento constante

Mesmo sem captura:

- água;
- reflexos;
- nuvens;
- vegetação;
- barco balançando;
- micro movimento do pescador;
- boia;
- pássaros ocasionais;
- peixe distante saltando raramente.

## Ritmo

O ciclo lógico continua sendo definido pelo jogo.

A animação pode ter loop próprio de ~20–30 s.

Não precisa sincronizar cada frame ao servidor.

---

# 10. LUZ E ATMOSFERA

A direção “Lago Dourado” usa luz natural bonita.

## Preferência

- manhã suave;
- fim de tarde;
- golden hour;
- azul profundo com reflexo quente.

## Evitar

- saturação extrema;
- HDR “estourado”;
- glow permanente;
- laranja em tudo;
- neblina excessiva;
- lens flare chamativo.

A cena deve parecer relaxante antes de parecer épica.

---

# 11. TIPOGRAFIA

## Títulos e números importantes

**Fredoka**

Usos:

- títulos;
- nível;
- moedas;
- grandes números;
- banners curtos.

Peso sugerido:
- 600;
- 700 apenas quando realmente necessário.

## Corpo

**Nunito**

Usos:

- descrição;
- labels;
- filtros;
- mensagens;
- informações do peixe.

## Regra

Nada de 5 fontes diferentes.

A fonte deve ajudar a interface a parecer jogo, mas continuar extremamente legível.

---

# 12. ÍCONES

Direção:

- simples;
- arredondados;
- espessura consistente;
- 1–2 cores;
- leitura em tamanhos pequenos.

Não misturar:

- outline finíssimo;
- 3D;
- emoji;
- pintura realista;
- ícones sólidos pesados.

Criar uma família única.

---

# 13. PAINÉIS E JANELAS

Todos os menus abrem sobre a cena.

## Painel principal

- fundo azul-marinho escuro;
- leve transparência apenas se não afetar leitura;
- borda fina;
- cantos arredondados;
- sombra suave;
- cenário continua visível atrás, desfocado ou escurecido discretamente.

## Regra

Não transformar cada informação em um card.

Criar agrupamentos.

Menos retângulos = aparência mais premium.

---

# 14. BOTÕES

## Primário

Turquesa.

Uso:
- iniciar;
- confirmar;
- viajar;
- atacar;
- comprar;
- enviar expedição.

## Secundário

Painel elevado/neutro.

## Destrutivo

Vermelho/coral.

## Recompensa especial

Dourado apenas quando o botão representa:
- coletar prêmio especial;
- destaque de conquista;
- ação rara justificável.

---

# 15. SISTEMA DE INTENSIDADE VISUAL

Este sistema é obrigatório.

A UI possui **3 níveis de intensidade**.

---

## NÍVEL 1 — CALMO

### Quando

- menus;
- pescando normalmente;
- esperando ciclo;
- navegando no Aquário;
- olhando Mercado;
- Perfil.

### Visual

- interface escura;
- turquesa discreto;
- cenário vivo;
- sem partículas chamativas.

### Movimento

- água;
- boia;
- barco;
- hover;
- transições leves.

### Som

- ambiente;
- brisa;
- água;
- clique discreto.

---

## NÍVEL 2 — RECOMPENSA

### Quando

- captura normal;
- venda;
- moedas recebidas;
- expedição concluída normal;
- item indo para Caixa.

### Visual

- toast com peixe;
- `+XP`;
- `+Moedas`;
- contador animado;
- Caixa de Pesca pulsa brevemente.

### Movimento

Curtíssimo.

O usuário não deve ser interrompido.

---

## NÍVEL 3 — CELEBRAÇÃO

### Quando

- captura rara;
- tamanho Excepcional;
- recorde pessoal;
- espécie nova;
- subir de nível;
- leilão vencido;
- recompensa extremamente incomum.

### Visual

- fundo escurece suavemente ao redor do foco;
- peixe aparece maior;
- raios muito suaves;
- poucas partículas;
- aura correspondente;
- texto de celebração;
- dourado quando o evento for de recorde/progressão.

### Duração

~2–4 segundos dependendo do evento.

Nunca bloquear o jogador por muito tempo.

---

# 16. MOMENTOS ESPECIAIS

## 16.1 Peixe pescado

Fluxo:

1. pequena reação da vara;
2. respingo;
3. peixe aparece;
4. nome/tamanho rapidamente;
5. `+XP` sobe;
6. contador da Caixa aumenta;
7. volta ao loop.

Captura normal não abre modal.

---

## 16.2 Captura rara

Além do fluxo normal:

- aura na cor da raridade;
- halo leve;
- partículas pequenas;
- som diferenciado;
- toast fica mais tempo.

Evitar explosão cinematográfica a cada peixe raro.

---

## 16.3 Tamanho Excepcional

- selo especial;
- brilho dourado controlado;
- pequeno gleam;
- partículas curtas.

Se também for Raro, mostrar:

- cor da raridade no card;
- selo Excepcional separado.

---

## 16.4 Novo recorde pessoal

Mostrar:

**NOVO RECORDE**

- espécie;
- imagem grande;
- tamanho anterior;
- novo tamanho;
- barra mostrando proximidade do máximo conhecido da espécie.

Exemplo:

`Anterior: 81,2 cm → Novo: 84,7 cm`

Dourado pode dominar este momento.

---

## 16.5 Nova espécie

A Enciclopédia participa visualmente.

Fluxo:

1. silhueta;
2. flash suave;
3. silhueta ganha cor;
4. nome revelado;
5. texto: **Nova espécie descoberta!**

---

## 16.6 Subir de nível do pescador

- barra chega ao fim;
- pequeno flash turquesa → dourado;
- anel expande a partir do Perfil;
- texto `Nível 11!`;
- número antigo muda para novo;
- barra reinicia.

Duração curta.

Não abrir uma tela inteira obrigatória.

---

## 16.7 Venda

Ao confirmar venda:

- itens desaparecem com micro fade;
- moedas fazem pequeno movimento em direção ao contador;
- contador sobe animado;
- som curto de moedas.

---

## 16.8 Voltar ao jogo / offline

Apresentar como **Resumo da Pesca**.

Mostrar:

- tempo offline válido;
- total de capturas;
- XP;
- melhores peixes;
- eventos especiais encontrados.

Se houver algo realmente especial, mostrar primeiro.

Não abrir 40 popups.

---

## 16.9 Expedição concluída

Toast:

**Expedição concluída**

Ao abrir:

- moedas;
- se encontrou peixe, o peixe ganha destaque;
- sem XP;
- sem Conchas, conforme regra atual.

---

## 16.10 Mercado

Venda realizada:

- toast discreto;
- moeda animada;
- nome do item.

Compra:

- confirmação limpa;
- item vai para **Itens a Retirar**.

---

## 16.11 Leilão

Lance superado:

- notificação;
- sem celebração.

Leilão vencido:

- intensidade 3 moderada;
- item em destaque;
- dourado;
- botão para ir a Itens a Retirar.

---

## 16.12 Arena

Vitória:

- pequena celebração;
- mudança de rank;
- Honra ganha;
- sem fogos artificiais gigantes.

Derrota:

- neutra;
- mostrar Energia/Honra alterada;
- sem tela humilhante.

---

# 17. TELA — PESCA

## Deve conter

Topo:
- Pesca;
- Mapa;
- Aquário;
- Arena;
- Expedição;
- Mercado;
- Loja;
- Perfil;
- moedas;
- avisos;
- opções.

Esquerda:
- card retrátil do pescador.

Centro:
- cena.

Inferior:
- status do ciclo;
- Iniciar/Parar pesca quando necessário.

Inferior direito:
- Caixa de Pesca.

## Não mostrar permanentemente

- Energia da Arena;
- Honra;
- Força do Cardume;
- dezenas de recursos.

Cada sistema mostra seus próprios recursos quando relevante.

---

# 18. TELA — MAPA

Cards grandes e visuais.

Cada mapa mostra:

- paisagem;
- nome;
- nível necessário;
- vara mínima;
- espécies descobertas;
- estado atual/bloqueado;
- botão Viajar.

Viagem:

- 30 segundos;
- barquinho em movimento;
- transição visual curta.

Não deve parecer lista de texto.

---

# 19. TELA — AQUÁRIO

Objetivo: fazer o jogador gostar dos peixes que possui.

Layout:

- grade de cards à esquerda/centro;
- ficha selecionada à direita;
- filtros no topo.

Card grande o suficiente para o peixe ser protagonista.

Ficha:

- arte maior;
- nome;
- tamanho;
- categoria;
- raridade;
- nível;
- XP;
- stats quando necessários;
- ações.

Capacidade sempre visível:

`37 / 100`

---

# 20. TELA — PERFIL

Perfil é identidade + gerenciamento.

Abas:

- Equipamentos;
- Inventário;
- Cardume;
- Enciclopédia;
- Destaques.

## Cardume

- 3 slots de frente;
- 3 slots de trás;
- diferença de profundidade visual pequena, mas explícita;
- ordem 1 → 6;
- bônus 6/6 visível;
- Força do Cardume apenas para o proprietário.

## Enciclopédia

- espécies descobertas;
- silhuetas das desconhecidas;
- maior exemplar pessoal.

## Destaques

Poucos.

Exemplo:
- maior peixe;
- recorde;
- rank.

Não inventar troféus sem sistema real.

---

# 21. TELA — ARENA

Topo:

- posição;
- Energia;
- Honra.

Tabs:

- Adversários;
- Ranking;
- Histórico;
- Loja da Arena.

## Adversários

3 cards.

Mostrar:

- jogador;
- rank;
- peixes;
- nível;
- raridade.

Não mostrar:

- Força do Cardume do adversário;
- porcentagem de vitória.

Botão:
`Atacar`

Reroll:
`Trocar adversários (1)`

---

# 22. BATALHA DA ARENA

Formação:

### Frente
`[1] [2] [3]`

### Trás
` [4] [5] [6]`

Os lados são espelhados.

O slot derrotado continua vazio.

## Ataque

- micro avanço;
- impacto;
- pequena água/partícula;
- número de dano;
- volta.

Sem skills.

Sem câmera maluca.

## Replay

- `1x`
- `2x`
- `Pular`

Meta visual:
maioria das lutas ~60 s ou menos, mas sem hard cap.

---

# 23. TELA — EXPEDIÇÃO

Cards de duração:

- 30 min;
- 1 h;
- 3 h;
- 6 h.

Cada card:

- imagem do local;
- duração;
- força recomendada;
- eficiência;
- moedas previstas ou faixa;
- pequena chance de peixe.

A imagem deve vender o lugar.

Não usar quatro retângulos idênticos cheios de texto.

---

# 24. TELA — MERCADO

Direção: **álbum visual simplificado**, não MMO.

Abas:

- Comprar;
- Vender;
- Meus Anúncios;
- Leilão;
- Itens a Retirar.

## Comprar

- filtros à esquerda;
- cards no centro;
- ficha do item à direita.

Card:
- peixe;
- raridade;
- tamanho;
- nível;
- preço.

Cor por raridade apenas nos acentos.

---

# 25. TELA — LEILÃO

Dentro de Mercado.

Mostrar:

- item;
- lance atual;
- próximo lance mínimo;
- taxa 1%;
- tempo restante;
- botão Dar Lance.

Regras visíveis de forma resumida:

- 1 leilão ativo por jogador;
- 6 h;
- incremento mínimo 3%;
- lance final abaixo de 1 min volta para 1:00;
- vendedor pode encerrar antecipadamente apenas com oferta e paga 3%.

Não transformar isso em bolsa de valores.

---

# 26. TELA — LOJA

A V0.1 foca em varas.

A vara deve parecer item importante.

Layout sugerido:

- informações à esquerda;
- imagem grande da vara à direita.

Mostrar:

- nome;
- tier;
- nível;
- preço;
- bônus de raridade;
- bônus de tamanho;
- bônus de Conchas;
- requisito.

Cards simples.

---

# 27. VARAS — VISUAL

Varas devem ter visual próprio.

Evitar apenas trocar a cor.

O formato/silhueta deve progredir:

- inicial simples;
- madeira/metais básicos;
- mais refinada;
- acabamento avançado.

Raridade/tier da vara pode usar o mesmo sistema cromático geral, caso o sistema de dados tenha raridade para itens.

---

# 28. MODAIS E FEEDBACK

Modal só quando:

- confirmação relevante;
- ação destrutiva;
- ficha detalhada;
- menu principal.

Toast quando:

- recorde;
- venda;
- leilão;
- expedição;
- espécie nova;
- mudança importante não bloqueante.

Não abrir modal para toda captura.

---

# 29. ANIMAÇÕES — REGRAS

## Duração

Microinteração:
`100–250 ms`

Toast:
`300–450 ms` entrada.

Hover:
`120–180 ms`

Celebração:
`1,5–4 s`

## Curvas

Preferir ease-out.

Evitar elastic/bounce exagerado.

## Movimento ambiente

Lento:
`3–8 s`

---

# 30. COMO EVITAR “CARA DE IA”

Esta é uma prioridade do projeto.

## Não usar

- excesso de glow;
- excesso de partículas;
- fundos cheios de detalhes sem motivo;
- peixes com estilos diferentes;
- ícones gerados cada um num estilo;
- dezenas de gradientes;
- texto integrado em imagens;
- mãos/personagens com anatomia inconsistente;
- iluminação cinematográfica em todo card;
- molduras “fantasy mobile” excessivas;
- 3 moedas no topo sem existir no jogo;
- gemas inventadas;
- personagens/companheiros inventados;
- arte super detalhada atrás de UI minúscula.

## Usar

- formas simples;
- repetição visual consistente;
- mesma biblioteca de ícones;
- mesma família de bordas;
- mesma fonte;
- mesma direção de luz;
- mesma técnica de ilustração dos peixes;
- cor com função;
- animação curta;
- espaços vazios intencionais.

## Teste

Se uma tela parece um “anúncio de jogo mobile gerado por IA”, reduzir:

1. saturação;
2. glow;
3. decoração;
4. gradientes;
5. quantidade de cards;
6. textos chamativos.

---

# 31. ASSETS DE CENÁRIO

Cada mapa deve ser separado em camadas para parallax:

- céu;
- montanha distante;
- floresta distante;
- margem;
- água;
- elementos próximos;
- partículas;
- reflexos.

Não gerar o mapa inteiro como uma única imagem se pretendemos animar.

---

# 32. MAPA 1 — LAGO SERENO

Atmosfera:

- seguro;
- bonito;
- relaxante;
- água calma;
- verde;
- azul;
- dourado de fim de tarde.

Movimentos:

- água suave;
- folhas;
- nuvens;
- pássaros raros;
- boia;
- barco.

---

# 33. MAPA 2 — RIO SELVAGEM

Atmosfera:

- mais energia;
- correnteza;
- pedras;
- mata mais fechada;
- cachoeira;
- névoa discreta;
- ainda relaxante.

Não transformar em fase de ação.

---

# 34. SOM E VISUAL DEVEM CONVERSAR

Mesmo esta sendo uma Art Bible, os efeitos visuais dependem de som.

## Calmo

- água;
- vento;
- pássaros;
- madeira do barco.

## Recompensa

- splash;
- moeda;
- click mais agradável.

## Celebração

- sininho;
- arpejo;
- som curto de descoberta.

Sem slot machine.

---

# 35. ORGANIZAÇÃO NO UNITY

Estrutura sugerida:

```text
Assets/
  Art/
    Fish/
      Masters/
      UI/
    Maps/
      LagoSereno/
      RioSelvagem/
    UI/
      Icons/
      Panels/
      Badges/
      Cursors/
    Effects/
      Particles/
      Materials/
  Audio/
    Ambient/
    UI/
    Rewards/
  Prefabs/
    UI/
    Fish/
    Effects/
  ScriptableObjects/
    VisualConfig/
  Docs/
    ArtDirection/
```

---

# 36. CONFIGURAÇÕES VISUAIS DATA-DRIVEN

Evitar cores hardcoded em 40 prefabs.

Criar configuração central:

- cor por raridade;
- cor de botão;
- cor de painel;
- intensidade de glow;
- duração de toast;
- duração de celebração;
- ícones;
- materiais.

Exemplo conceitual:

`VisualThemeConfig`

Assim conseguimos ajustar o jogo inteiro sem editar dezenas de objetos.

---

# 37. NOMES DE ARQUIVO

Peixe:

`fish_lambari_master.png`

Ícone:

`ico_market_buy.svg`

Mapa:

`map_lago_sereno_bg_far.png`

Card/Frame:

`ui_card_fish_base.png`

Partícula:

`vfx_reward_gold.prefab`

---

# 38. ORDEM DE PRODUÇÃO DOS ASSETS

## Prioridade A — precisa agora

1. sistema de cards;
2. paleta final;
3. ícones principais;
4. 10 peixes do Mapa 1;
5. Lago Sereno;
6. pescador/barco;
7. efeitos de captura;
8. menus da V0.1.

## Prioridade B

9. 10 peixes do Mapa 2;
10. Rio Selvagem;
11. varas;
12. Arena;
13. Mercado/Leilão;
14. Expedições.

## Prioridade C

15. refinamento;
16. skins;
17. equipamentos futuros;
18. mapas posteriores.

---

# 39. PROCESSO CHATGPT → CLAUDE → UNITY

## Etapa 1 — Design

O proprietário + ChatGPT:

- aprovam conceito;
- criam referência;
- definem paleta;
- definem comportamento;
- produzem asset ou briefing.

## Etapa 2 — Arquivo

O asset aprovado vai para o projeto com:

- nome;
- resolução;
- transparência;
- uso;
- tela onde aparece.

## Etapa 3 — Claude

Claude:

- importa;
- configura Unity;
- cria Sprite/Atlas;
- conecta ao prefab;
- mantém asset substituível;
- atualiza roadmap.

## Etapa 4 — Revisão

O proprietário tira print do jogo.

ChatGPT + proprietário:

- comparam;
- ajustam;
- geram novo asset ou instrução.

## Etapa 5 — Substituição

Claude troca o asset, sem refazer sistema.

---

# 40. REGRA PARA ASSETS GERADOS COM IA

IA pode ser usada como ferramenta de produção.

Mas o jogo não deve parecer uma coleção de imagens geradas separadamente.

Cada asset deve passar por:

1. seleção;
2. padronização;
3. recorte;
4. ajuste de cor;
5. tamanho;
6. consistência com os demais;
7. teste dentro do Unity.

Não aceitar o primeiro resultado gerado automaticamente.

---

# 41. CHECKLIST PARA CADA NOVO PEIXE

Antes de aprovar:

- [ ] parece com a espécie;
- [ ] mesma direção;
- [ ] mesma proporção artística;
- [ ] mesmo nível de detalhe;
- [ ] fundo transparente;
- [ ] sem glow baked;
- [ ] sem texto;
- [ ] funciona em card pequeno;
- [ ] funciona em ficha grande;
- [ ] silhueta é reconhecível;
- [ ] não parece foto colada;
- [ ] não parece render 3D genérico;
- [ ] não tem artefatos estranhos.

---

# 42. CHECKLIST PARA CADA TELA

- [ ] parece jogo, não site;
- [ ] foco visual claro;
- [ ] cenário ainda respira;
- [ ] botões importantes são óbvios;
- [ ] sem informação duplicada;
- [ ] raridade identificável;
- [ ] sem glow excessivo;
- [ ] texto em PT-BR;
- [ ] nenhuma mecânica inventada;
- [ ] funciona em 1920×1080;
- [ ] permanece legível em resoluções menores;
- [ ] usa componentes já padronizados.

---

# 43. OBJETIVO FINAL DA V0.1

Quando a V0.1 estiver visualmente pronta, o jogador deve poder:

- deixar a tela de pesca aberta e gostar de olhar;
- identificar rapidamente uma captura especial;
- perceber raridade sem ler;
- sentir prazer em descobrir um peixe;
- querer abrir o Aquário;
- entender cada menu sem tutorial enorme;
- reconhecer imediatamente quando algo importante aconteceu;
- não sentir que está olhando para um template web;
- não sentir que cada imagem veio de uma IA diferente.

A interface deve transmitir:

> **“É simples, bonito, relaxante e tem algo especial para eu encontrar.”**

---

# 44. RESUMO EM UMA FRASE PARA O CLAUDE

> **Construa um idle de pesca 2.5D limpo e premium: cenário relaxante, UI escura e legível, turquesa para ações, raridade nos acentos dos cards, dourado reservado para recompensas e celebrações curtas, mantendo todos os assets consistentes e substituíveis.**

---

# 45. REGRA DE OURO

Sempre que houver dúvida entre:

**“mais efeito”**  
e  
**“mais limpo”**

usar **mais limpo**.

Guardar o efeito para quando o jogador realmente merecer vê-lo.
