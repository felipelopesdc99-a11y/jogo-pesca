# FISHING IDLE — DOCUMENTO DE IDENTIDADE FORTE POR MAPA
## Expansão de ambientação, sensação de capítulo e diferenciação entre regiões

> **Situação no repositório (30/09/2026):** documento de direção enviado pelo proprietário, guardado
> como está. O que vale para os dois mapas que existem (Lago Sereno e Rio Selvagem) já está no jogo ou
> registrado (GDD_ADENDO A-078, A-081, A-085). Os mapas 3 e 4 (Pantanal Dourado e Estuário das Marés),
> a raridade Épico e a Vara 2 entraram em 30/09/2026 com `docs/PROGRESSAO_MAPAS_3_4.md` (GDD_ADENDO
> A-093), com arte provisória até as imagens finais chegarem.

**Idioma:** PT-BR  
**Objetivo:** orientar o Claude a reforçar a identidade de cada mapa do Fishing Idle, para que cada nova região pareça um novo capítulo da jornada do jogador, e não apenas um novo background.

---

# 1. CONTEXTO

O jogo já possui uma base jogável e um estilo visual consolidado (Lago Dourado / Art Bible).  
Este documento **não cria um jogo novo** e **não altera os sistemas centrais** de pesca, progressão, cardume, mercado, arena, expedição ou saves.

O foco aqui é:

- fortalecer a personalidade de cada mapa;
- fazer a progressão geográfica parecer uma jornada;
- aumentar o interesse do jogador por troca de região;
- melhorar a retenção pela sensação de descoberta;
- fazer cada mapa parecer um “capítulo” novo.

---

# 2. OBJETIVO DE DESIGN

## Regra principal

> Cada mapa precisa ser imediatamente reconhecível por cenário, clima, som, fauna ambiente, cor da água, ritmo visual e identidade dos peixes.

Quando o jogador mudar de mapa, ele deve sentir:

- que saiu de um lugar e chegou em outro;
- que existe um novo ecossistema;
- que a pescaria mudou de humor;
- que os peixes daquele lugar pertencem de verdade àquela região;
- que vale a pena ficar ali pescando, não apenas pelo número, mas pela sensação.

---

# 3. O QUE NÃO MUDA

Claude deve preservar:

- loop principal de pesca;
- estilo visual aprovado do jogo;
- layout macro da interface;
- lógica de progressão já definida;
- duração da viagem entre mapas (30 s, salvo ajuste futuro);
- identidade relaxante do jogo;
- PT-BR em todos os textos visíveis.

Este documento trata de:

- ambientação;
- direção sensorial;
- diferenciação temática;
- pequenos reforços de UX visual e sonora;
- apresentação do mapa ao jogador.

---

# 4. PILARES DE IDENTIDADE POR MAPA

Cada mapa deve ser construído usando estes 6 pilares.

## 4.1 Cenário-base
Formato físico do lugar.

Exemplos:
- lago aberto;
- rio com correnteza;
- planície alagada;
- estuário costeiro.

## 4.2 Horário e luz
Cada mapa deve ter uma sensação de hora do dia predominante.

## 4.3 Água
Cada mapa precisa ter sua própria leitura de água:
- cor;
- movimento;
- brilho/reflexo;
- espuma ou calma;
- profundidade percebida.

## 4.4 Fauna ambiente
Animais e vida de fundo ajudam a vender a região.

Exemplos:
- pássaros;
- capivaras;
- jacarés;
- garças;
- araras;
- caranguejos;
- gaivotas.

## 4.5 Som ambiente
O áudio ajuda o jogador a “entrar” no mapa.

## 4.6 Catálogo de peixes
Os peixes do mapa precisam parecer naturais para aquela região.

---

# 5. DIRETRIZ DE EXPERIÊNCIA

O Fishing Idle é um jogo relaxante, mas a progressão precisa causar pequenas emoções.

## O que queremos provocar

### Quando o jogador entra em um mapa novo:
- curiosidade;
- sensação de novidade;
- vontade de ficar mais tempo;
- expectativa por espécies novas;
- percepção imediata de “esse lugar é diferente”.

### Quando ele permanece no mapa:
- conforto visual;
- familiaridade crescente;
- desejo de completar coleção;
- vontade de conseguir o “peixe sonho” daquela região.

---

# 6. ESTRATÉGIA DE IMPLEMENTAÇÃO

Claude deve tratar cada mapa como um pacote de identidade composto por:

1. **Cena principal** do mapa;
2. **Paleta ambiental** predominante;
3. **Loop de som ambiente**;
4. **Fauna viva do cenário**;
5. **Transição de viagem / chegada**;
6. **Mini texto ou título visual de chegada** (se já houver suporte);
7. **Conjunto próprio de peixes**;
8. **Elementos visuais de expedição** relacionados àquele mapa;
9. **Coerência com a Enciclopédia e Mercado**;
10. **Reforço visual no menu de mapa**.

---

# 7. MAPA 1 — LAGO SERENO

## Função no jogo
Mapa inicial. Deve passar segurança, calma e acolhimento.

## Sensação central
> “Começo tranquilo e bonito.”

## Identidade
- água calma;
- lago amplo e sereno;
- reflexos suaves;
- luz de fim de tarde dourada;
- ambiente limpo e agradável.

## Horário predominante
Fim de tarde / golden hour.

## Paleta
- azul suave;
- verde delicado;
- dourado quente;
- turquesa discreto.

## Água
- praticamente sem correnteza;
- leves ondulações;
- reflexos quentes;
- brilho agradável sem exagero.

## Fauna ambiente sugerida
- patos;
- garças;
- peixes pequenos saltando;
- pássaros distantes.

## Som ambiente
- água leve;
- vento suave;
- pássaros calmos;
- madeira do barco.

## Objetivo psicológico
Fazer o jogador gostar de deixar o jogo aberto.

---

# 8. MAPA 2 — RIO SELVAGEM

## Função no jogo
Primeira grande mudança de ambiente. Ainda relaxante, mas com mais energia.

## Sensação central
> “A aventura começou a sair do conforto do lago.”

## Identidade
- rio mais largo e vivo;
- correnteza visível;
- rochas;
- mata mais presente;
- cachoeira/nascente ou desnível de água;
- atmosfera um pouco mais selvagem.

## Horário predominante
Manhã clara ou manhã levemente fria.

## Paleta
- verdes mais fortes;
- azul mais frio;
- espuma branca;
- pedra cinza esverdeada.

## Água
- movimento perceptível;
- faixas de espuma;
- direção de fluxo mais clara;
- mais energia visual que o lago.

## Fauna ambiente sugerida
- martim-pescador;
- macacos ao longe;
- araras;
- jacarés discretos;
- capivaras ocasionais.

## Som ambiente
- água correndo;
- cachoeira distante;
- pássaros da mata;
- insetos discretos.

## Objetivo psicológico
Mostrar que o mundo do jogo é maior e mais vivo do que o primeiro mapa.

---

# 9. MAPA 3 — PANTANAL DOURADO

## Função no jogo
Primeiro mapa intermediário da jornada. Deve parecer um ecossistema amplo, rico, vivo e muito brasileiro.

## Sensação central
> “Agora o mundo se abriu de verdade.”

## Papel na progressão
- desbloqueia no nível 20;
- ainda usa Vara 1;
- precisa aumentar claramente a sensação de descoberta;
- introduz peixes mais interessantes e já traz Épico.

## Identidade
- grandes áreas alagadas;
- água rasa e larga;
- vegetação baixa;
- árvores esparsas;
- capim, aguapés, margens pantanosas;
- fauna abundante.

## Horário predominante
Amanhecer dourado.

## Paleta
- dourado claro do sol da manhã;
- verde úmido;
- marrom suave de terra molhada;
- azul claro pouco saturado.

## Água
- menos “lisa” que no Lago Sereno;
- menos agressiva que o Rio Selvagem;
- água alagada, larga, reflexiva e quente;
- pequenos movimentos superficiais.

## Fauna ambiente sugerida
- garças;
- tuiuiús;
- capivaras;
- jacarés ao fundo;
- pássaros de brejo;
- borboletas/insetos leves;
- peixes rompendo a superfície raramente.

## Som ambiente
- pássaros do pantanal;
- insetos suaves;
- água calma com movimento leve;
- sons distantes de vida selvagem.

## Diretriz emocional
Pantanal Dourado não deve parecer “mais um rio”.
Ele precisa passar amplitude, riqueza natural e presença de vida.

## Peixe-sonho do mapa
**Barbado (Épico)** deve ser a espécie que causa fascínio de coleção e poder.

## Reforços de UX
- ao chegar no mapa, destacar o novo bioma na tela de viagem;
- no menu do mapa, usar thumbnail muito distinta de lago/rio;
- nas expedições desse mapa, imagens com visual pantaneiro;
- dar sensação de “capítulo 3 da jornada”.

---

# 10. MAPA 4 — ESTUÁRIO DAS MARÉS

## Função no jogo
Primeira transição forte para ambiente costeiro/brackish. O jogador deve sentir que começou uma nova fase do mundo.

## Sensação central
> “Cheguei em uma nova fronteira do jogo.”

## Papel na progressão
- desbloqueia no nível 30;
- exige Vara 2;
- introduz 2 peixes Épicos;
- precisa ser perceptivelmente mais valioso e diferente do Mapa 3.

## Identidade
- encontro de rio e mar;
- raízes expostas de mangue;
- canais de maré;
- vegetação costeira;
- água mais escura e salobra;
- ambiente úmido e denso.

## Horário predominante
Fim de tarde quente / pôr do sol costeiro.

## Paleta
- verde escuro;
- marrom das raízes;
- azul-esverdeado profundo;
- dourado quente do fim do dia;
- sombras mais pesadas que nos mapas anteriores.

## Água
- mais pesada visualmente;
- mistura de água doce e costeira;
- canais com leve fluxo;
- lama rasa em pontos próximos às raízes;
- reflexos mais contrastados.

## Fauna ambiente sugerida
- garças costeiras;
- caranguejos nas margens;
- aves de mangue;
- pequenos peixes de superfície;
- som de água batendo em raízes.

## Som ambiente
- água costeira calma;
- aves costeiras;
- vento úmido;
- ambiente de maré, porém sem virar praia aberta.

## Diretriz emocional
Este mapa precisa parecer o primeiro grande “salto de mundo”.
O jogador não deve pensar “troquei de cor”; deve pensar “saí do interior e cheguei a uma região nova”.

## Peixes-sonho do mapa
- **Camurupim (Épico)**
- **Mero (Épico)**

Esses dois devem ser tratados como grandes marcos de coleção/poder.

## Reforços de UX
- chegada ao mapa com sensação de fronteira nova;
- ícone/thumbnail muito diferente dos anteriores;
- lojas/expedições/mercado podem usar mini artes que reforcem raízes, maré e mangue;
- esteticamente, esse mapa já prepara o jogador para o futuro oceano.

---

# 11. TABELA DE DIFERENCIAÇÃO RÁPIDA

| Mapa | Sensação | Horário | Água | Fauna dominante | Emoção principal |
|---|---|---|---|---|---|
| Lago Sereno | conforto | fim de tarde | calma | aves tranquilas | acolhimento |
| Rio Selvagem | energia controlada | manhã | correnteza | mata/rio | descoberta |
| Pantanal Dourado | amplitude viva | amanhecer | larga e alagada | pantanal | riqueza natural |
| Estuário das Marés | fronteira nova | fim de tarde quente | salobra/costeira | mangue | transição de mundo |

---

# 12. APLICAÇÃO NOS SISTEMAS EXISTENTES

## 12.1 Menu de mapa
Cada card de mapa deve comunicar:
- bioma distinto;
- paleta própria;
- título forte;
- leitura clara do lugar.

## 12.2 Tela principal de pesca
A cena do mapa atual precisa refletir de forma óbvia o bioma.

## 12.3 Expedições
Cada região deve ter artes e nomes coerentes com aquele mapa.

## 12.4 Enciclopédia
Os peixes daquele mapa devem parecer coerentes com o lugar onde o jogador os encontrou.

## 12.5 Mercado
Não precisa mudar a lógica do sistema, mas a experiência do jogador melhora quando os peixes têm identidade regional clara.

## 12.6 Arena
Sem mudar a Arena, mapas mais marcantes tornam os peixes do cardume mais memoráveis.

---

# 13. MICRO-ELEMENTOS QUE AUMENTAM INTERESSE

Claude pode reforçar a identidade do mapa usando pequenos detalhes, sem inventar sistemas novos:

- nome do mapa mais visível ao chegar;
- pequena transição de chegada coerente com a região;
- ambient loop próprio por mapa;
- fauna ocasional exclusiva da região;
- thumbnail exclusiva do mapa;
- arte de expedição associada ao mapa;
- sensação diferente de água e luz;
- peixes visivelmente pertencentes àquele bioma.

---

# 14. O QUE EVITAR

Não fazer:
- quatro mapas com a mesma estrutura visual apenas trocando cor;
- exagerar no número de partículas ou animais a ponto de virar ruído;
- transformar cada mapa em uma tela lotada;
- quebrar a leitura tranquila do Fishing Idle;
- usar efeitos cinematográficos pesados o tempo todo;
- reutilizar o mesmo som ambiente em todos os mapas;
- fazer Pantanal Dourado parecer apenas “Lago Sereno mais verde”; 
- fazer Estuário das Marés parecer apenas “Rio Selvagem com mangue”.

---

# 15. CHECKLIST DE IMPLEMENTAÇÃO PARA O CLAUDE

## Para cada mapa, validar:
- [ ] possui nome forte e coerente;
- [ ] possui cena própria bem distinguível;
- [ ] possui paleta ambiental específica;
- [ ] possui comportamento de água próprio;
- [ ] possui fauna ambiente condizente;
- [ ] possui loop sonoro coerente;
- [ ] possui thumbnail própria no menu de mapa;
- [ ] possui peixes coerentes com o bioma;
- [ ] a troca de mapa gera sensação real de mudança;
- [ ] o mapa continua bonito por longos períodos em idle.

---

# 16. ORDEM DE PRIORIDADE

## Prioridade 1
- consolidar identidade visual do Mapa 3;
- consolidar identidade visual do Mapa 4;
- garantir que a transição entre mapas seja clara.

## Prioridade 2
- aplicar fauna ambiente exclusiva;
- aplicar loops sonoros próprios;
- melhorar thumbs e apresentação de mapa.

## Prioridade 3
- refinar micro-eventos e detalhes de ambiência;
- reforçar expedições e apresentação secundária.

---

# 17. REGRA FINAL PARA O CLAUDE

> Não trate o mapa como um pano de fundo. Trate o mapa como um capítulo jogável da jornada do pescador.

Cada nova região do Fishing Idle deve responder imediatamente à pergunta:

**“Por que este lugar existe?”**

Se a resposta for clara só de olhar e escutar, então a identidade do mapa está funcionando.

---

# 18. RESUMO EXECUTIVO

Claude deve usar este documento para tornar cada mapa uma etapa marcante da progressão do jogo.

### Resultado esperado:
- o jogador reconhece o mapa em segundos;
- a mudança de região gera curiosidade e prazer;
- os peixes parecem pertencer àquele lugar;
- o mundo do jogo parece maior e mais vivo;
- a progressão deixa de ser só numérica e passa a ser também geográfica e emocional.

