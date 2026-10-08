# Adendo ao GDD — detalhes de design definidos durante a implementação

O GDD (`docs/GDD_V0_1.md`) é a fonte de verdade travada e continua no original, sem edições (regra
do `CLAUDE.md`). Durante a implementação aparecem detalhes que o GDD não fixa por completo. Cada um
é registrado **aqui**, em PT-BR, para que o design do jogo continue num lugar só e nada fique
decidido apenas dentro do código.

Como ler:

- **Seção do GDD:** a parte do GDD que o detalhe complementa.
- **Situação:**
  - **Em vigor:** já implementado dessa forma.
  - **Confirmar:** implementado assim, mas é uma escolha que o proprietário pode querer mudar.
  - **Decidido pelo proprietário:** confirmado pelo proprietário.
- Mudar qualquer item é, em geral, uma mudança de balanceamento em `/config` ou uma alteração
  pequena de código. Nenhum deles é uma mecânica nova.

Aspectos técnicos (como o código funciona) não entram aqui: ficam em `docs/BASE_TECNICA.md`.

---

## Prioridade atual do projeto

Desde o `docs/CLAUDE_START_HERE_V0_1_1.md`, o proprietário mudou a ordem de construção: primeiro um
**MVP local, jogável no PC**, depois a infraestrutura online. O design do jogo (GDD) não mudou; mudou
a ordem e o lugar onde as regras rodam por enquanto (no próprio PC, num "serviço de jogo local"
separado da parte visual, pronto para virar servidor).

---

## Milestone 1 — Loop de pesca

### A-001 · Só conta como pesca online o tempo em que o jogo está aberto
**Seção do GDD:** 10 · **Situação:** Em vigor

Com a pesca ligada, sai 1 peixe a cada 30 segundos **enquanto o jogo estiver rodando**. Se o jogo
ficar sem rodar por mais de 2 minutos (fechado, PC em suspensão, relógio adiantado), esse período
não gera capturas online: o ciclo recomeça do zero quando o jogo volta. Esse período é exatamente o
que a pesca offline (60s por captura, até 24h) vai recompensar no **Milestone 5**.

Desde o Milestone 5, esse período vira pesca offline (A-034).

### A-002 · A pesca continua ligada depois de fechar e abrir o jogo
**Seção do GDD:** 10 · **Situação:** Confirmar

Se você fechou o jogo pescando, ao abrir ele continua pescando sozinho, sem precisar apertar
"Iniciar pesca" de novo. Só "Parar pesca" desliga.

### A-003 · Todo jogador novo começa com a Vara Inicial equipada
**Seção do GDD:** 19, 40 · **Situação:** Em vigor (provisório)

O GDD prevê que o jogador resgate a Vara Inicial na Loja durante o tutorial. Como a Loja e o
tutorial são do Milestone 10, até lá a vara já vem equipada. O tutorial substitui isso quando chegar.

### A-004 · Quais peixes pedem confirmação numa venda em lote
**Seção do GDD:** 11 · **Situação:** Confirmar

Pedem a confirmação única **Revisar peixes / Confirmar** os peixes **Raros** (ou acima) e os de
tamanho **Excepcional**. Espécie nova e recorde pessoal **não** pedem, porque a descoberta e o
recorde ficam guardados para sempre mesmo depois de vender. Configurável em
`economy.json → fishing_box.bulk_sale_protection`.

"Revisar peixes" mostra na Caixa exatamente os peixes valiosos daquela venda, para você desmarcar
algum; "Confirmar" vende tudo; "Cancelar" não vende nada.

### A-005 · Filtros da Caixa de Pesca
**Seção do GDD:** 11 · **Situação:** Em vigor

Um filtro por vez: **Todos**, cada raridade (Comum, Raro) e cada categoria de tamanho (Pequeno,
Adulto, Grande, Excepcional). A lista vem de `/config`, então uma raridade nova aparece sozinha.
Seleção múltipla com **Selecionar todos** (do filtro atual) e **Limpar seleção**.

### A-006 · A Caixa de Pesca não tem limite de quantidade
**Seção do GDD:** 11 · **Situação:** Confirmar

O GDD define o limite do Aquário (100), mas não da Caixa. Por enquanto a Caixa aceita qualquer
quantidade. Se o proprietário quiser um limite, ele entra como um valor em `/config`.

### A-007 · "Guardar" no Milestone 1
**Seção do GDD:** 11, 12 · **Situação:** Substituído pelo Milestone 2 (A-015)

No Milestone 1, o peixe que você não vende **fica guardado na Caixa de Pesca**, que é persistente.
O botão "Guardar no Aquário", que transforma a captura num peixe completo com nível e atributos,
chega com o Aquário no **Milestone 2**. A tela avisa isso.

### A-008 · Descobertas e recordes são registrados desde a primeira captura
**Seção do GDD:** 38 · **Situação:** Em vigor

A Enciclopédia é do Milestone 3, mas o jogo já guarda, desde a primeira captura, a data em que cada
espécie foi descoberta, o maior exemplar e quantos foram pescados. Assim ninguém perde o histórico
de antes da Enciclopédia existir. Na Caixa, capturas marcam **NOVA ESPÉCIE** ou **RECORDE**.

### A-009 · Destaque visual das capturas importantes
**Seção do GDD:** 8 · **Situação:** Confirmar

Aura **dourada** para tamanho Excepcional, **lilás** para Raro e **azul-claro** para espécie nova.
O peixe na tela fica maior ou menor conforme o tamanho real do exemplar. Nas cartas da Caixa, os
peixes importantes têm borda dourada.

### A-010 · Avisos na tela (toasts)
**Seção do GDD:** 39 · **Situação:** Em vigor

Um aviso por captura, com a mensagem mais importante: espécie nova > Excepcional > recorde >
captura comum. Avisos separados para subida de nível e Conchas. No máximo 5 avisos na tela ao mesmo
tempo; nada é guardado em histórico (a central de notificações é do Milestone 10).

### A-011 · Menus que ainda não existem não aparecem
**Seção do GDD:** 7, 20 · **Situação:** Em vigor

A barra superior mostra só **Pesca**. Mapa, Aquário, Arena, Expedição, Mercado, Loja e Perfil
aparecem quando o milestone de cada um for entregue, nunca como botões mortos. Conchas só aparecem
no topo quando você tiver alguma (a Vara Inicial não gera Conchas).

### A-012 · Tamanho gravado em milímetros e categoria fixada na captura
**Seção do GDD:** 14 · **Situação:** Em vigor

O tamanho exato é guardado em milímetros e exibido com uma casa decimal (`35,2 cm`). A categoria
(Pequeno, Adulto…) é a do momento da captura: se os limites de categoria mudarem no balanceamento,
peixes já pescados não mudam de categoria. O preço, que depende do tamanho exato, acompanha o
balanceamento atual.

### A-013 · Como os bônus da vara entram no sorteio
**Seção do GDD:** 19 · **Situação:** Em vigor (usado a partir do Milestone 4)

- **Raridade:** multiplica só o peso das espécies não comuns já presentes no mapa. Com o Aruanã em
  0,5%, o bônus máximo da Vara 1 (+22%) leva a cerca de 0,6%, nunca a 22%.
- **Tamanho:** multiplica o peso das categorias Grande e Excepcional; o total é renormalizado.
- **Conchas:** multiplica a chance base de Concha por captura.
- A vara nunca cria uma raridade que o mapa não tem, e a Vara Inicial nunca pega Raro.

### A-014 · Arredondamentos
**Seção do GDD:** 17, 36 · **Situação:** Em vigor

XP do Pescador por captura e preço de venda são arredondados para inteiro (mínimo 1 XP; preço
mínimo configurável). No Mapa 1, o Nível 10 sai em cerca de 2 horas de pesca online, como pede o
GDD (verificado por simulação nos testes).

---

## Milestone 2 — Aquário e peixe persistente

### A-015 · Guardar no Aquário
**Seção do GDD:** 11, 12 · **Situação:** Em vigor

Na Caixa de Pesca, selecione os peixes e clique em **Guardar no Aquário**. Se não houver vagas para
todos, nada é guardado e o jogo avisa quantas vagas faltam. O Aquário mostra a ocupação no menu
(ex.: `Aquário 12/100`).

### A-016 · Fórmula dos atributos
**Seção do GDD:** 13, 24 · **Situação:** Confirmar

`atributo = base da espécie × raridade × tamanho × nível`, onde tamanho vai de −10% (menor exemplar)
a +10% (maior) e cada nível acima do 1 soma +4% (nível 10 = +36%). Raro = ×1,15. Os quatro atributos
usam a mesma conta. Todos os números estão em `/config` e no Painel de Desenvolvimento.

### A-017 · Alimento pode vir da Caixa ou do Aquário
**Seção do GDD:** 11, 22 · **Situação:** Em vigor

O GDD permite usar a Caixa como fonte de alimento. Na tela de alimentação há duas abas: **Da Caixa
de Pesca** e **Do Aquário**. Dá para misturar as duas numa mesma alimentação.

### A-018 · XP acima do nível 10 é perdido
**Seção do GDD:** 22 · **Situação:** Confirmar

Um peixe no nível 10 não pode mais ser alimentado. Se uma alimentação passar do nível 10, o que
sobrar é perdido; a prévia mostra quanto e pede confirmação antes.

### A-019 · Quais alimentos pedem confirmação
**Seção do GDD:** 22 · **Situação:** Confirmar

Pede confirmação consumir peixe **Raro**, de tamanho **Excepcional** ou já **evoluído (nível 2+)**
(`progression.json → feeding.valuable_feed_rules`). Vender um peixe do Aquário sempre pede
confirmação, porque ele é único.

### A-020 · Ordenação do Aquário
**Seção do GDD:** 12 · **Situação:** Confirmar

Padrão: Excepcional → Grande → Adulto → Pequeno; dentro da categoria, primeiro o maior **para a sua
espécie** (percentual dentro da faixa de tamanho), não o maior em centímetros — assim um lambari
recorde não fica atrás de qualquer pirarucu. Também dá para ordenar por nível, espécie ou mais
recentes.

### A-021 · Venda de peixe do Aquário
**Seção do GDD:** 36 · **Situação:** Em vigor

Mesmo preço que teria na Caixa (espécie, raridade e tamanho). O XP investido não é reembolsado.

---

## Milestone 3 — Perfil, Cardume, Inventário e Vara

### A-022 · Posições do Cardume podem ficar vazias
**Seção do GDD:** 23, 26 · **Situação:** Confirmar

O Cardume tem 1 a 6 peixes em posições fixas, e qualquer posição pode ficar vazia (por exemplo,
só a fileira de trás). Na Arena, o alvo segue a ordem 1 → 6 pulando as posições vazias, como o GDD
já define para peixes derrotados.

### A-023 · Montar o Cardume
**Seção do GDD:** 23 · **Situação:** Em vigor

Clique numa posição e depois num peixe do Aquário. Se o peixe já estava em outra posição, ele muda
de lugar (cada peixe aparece uma vez só). "Tirar da posição" esvazia a posição escolhida.

### A-024 · Onde a Força do Cardume aparece
**Seção do GDD:** 31, 37 · **Situação:** Em vigor

Só no Perfil do próprio jogador: no topo e na aba Cardume, com a Força de cada peixe. Nunca em telas
de outros jogadores (quando existirem).

### A-025 · Peixe do Cardume consumido ou vendido
**Seção do GDD:** 22 · **Situação:** Em vigor

Alimentar outro peixe com um peixe do Cardume, ou vendê-lo, pede confirmação e deixa a posição vazia.
A trava durante Expedições chega no Milestone 6.

### A-026 · Enciclopédia
**Seção do GDD:** 38 · **Situação:** Em vigor

As 20 espécies, na ordem do catálogo. Não descobertas: silhueta escura, nome "???". Descobertas:
nome, mapa, raridade, maior exemplar já pescado e quantas foram pescadas. Espécies com raridade
acima de Comum têm borda dourada.

### A-027 · Destaques do Perfil
**Seção do GDD:** 37 · **Situação:** Confirmar

Capturas totais, espécies descobertas, maior peixe já pescado (em cm), peixe de nível mais alto no
Aquário, capturas Excepcionais, capturas Raras, peixes vendidos e moedas com vendas. As contagens de
Excepcionais e Raras começaram a ser registradas nesta versão; capturas anteriores não entram nelas.

### A-028 · Equipar vara
**Seção do GDD:** 19, 20 · **Situação:** Em vigor

O Perfil mostra a vara equipada (tier, nível, bônus, se pesca Raros e se gera Conchas) e o Inventário
com todas as varas. Uma vara que não serve no mapa atual não pode ser equipada. Trocar de vara no
meio da pesca vale a partir do próximo ciclo.

---

## Milestone 4 — Mapas e Varas

### A-029 · Viagem
**Seção do GDD:** 18 · **Situação:** Em vigor

Menu Mapa → Viajar. A viagem leva 10 segundos desde 08/10/2026, antes 30 (`maps.json → travel`, A-140). Ao partir, o que já estava
pescado é entregue e a pesca pausa; na chegada, ela volta sozinha se estava ligada. Durante a
viagem não dá para pescar nem viajar para outro lugar. Se o jogo for fechado no meio, a viagem é
concluída ao abrir. Nunca há viagem automática ao subir de nível.

### A-030 · Requisitos do Rio Selvagem
**Seção do GDD:** 17, 19 · **Situação:** Em vigor

Nível 10 **e** a Vara 1 equipada. No Rio Selvagem, a Vara Inicial não pode ser equipada.

### A-031 · Comprar vara
**Seção do GDD:** 19 · **Situação:** Confirmar

Na Loja. A vara comprada vai para o Inventário e **já fica equipada**. Por enquanto só dá para ter
**uma de cada vara** (não dá para comprar a Vara 1 duas vezes); isso pode mudar quando o Mercado
existir.

### A-032 · Melhorar, vender e destruir vara
**Seção do GDD:** 19 · **Situação:** Confirmar

No Perfil → Inventário. Melhorar custa as moedas de `rods.json → upgrade_costs`, sem falha. Vender ao
jogo paga 40% do preço de compra + 25% do que foi gasto em melhorias. Vender e destruir pedem
confirmação. **A vara equipada e a Vara Inicial não podem ser vendidas nem destruídas**, para o
jogador nunca ficar sem vara.

### A-033 · Aparência do Rio Selvagem
**Seção do GDD:** 18 · **Situação:** Em vigor (arte provisória)

Morros mais altos, mata densa e escura, água mais funda, brilhos que correm com a correnteza,
pedras com espuma e uma cachoeira ao longe com névoa. A luz é mais fria que a do Lago Sereno.

---

## Milestone 5 — Pesca offline

### A-034 · Pesca offline
**Seção do GDD:** 10 · **Situação:** Em vigor

Se a pesca estava ligada quando o jogo fechou (ou quando o PC entrou em suspensão), o tempo fora
vira capturas: 1 a cada 60 segundos, contando no máximo 24 horas. Tudo é calculado ao voltar. Se a
pesca estava parada, ou se o jogo fechou no meio de uma viagem, não há pesca offline nesse período.

### A-035 · Tela de retorno
**Seção do GDD:** 10 · **Situação:** Em vigor

Aparece ao voltar, se houve pelo menos um ciclo offline: tempo fora (e o aviso de limite de 24 h,
quando passou), peixes pescados, XP, espécies novas, Conchas, subida de nível e até 4 capturas em
destaque. Botões: "Abrir a Caixa de Pesca" e "Continuar".

---

## Milestone 6 — Expedições

### A-036 · Força fotografada na partida
**Seção do GDD:** 32 · **Situação:** Em vigor

A recompensa usa a Força do Cardume do momento em que ele saiu. O Cardume fica travado durante a
Expedição, então ela não muda até a volta.

### A-037 · Moedas e peixe
**Seção do GDD:** 32 · **Situação:** Confirmar

Moedas = recompensa da Expedição × aproveitamento. A chance de achar um peixe é a configurada
quando o Cardume está na força recomendada ou acima, e cai proporcionalmente quando está abaixo (o
bônus acima da recomendada vale só para as moedas). O peixe vem das espécies do mapa de onde o
Cardume saiu, com as raridades daquele mapa, e vai para a Caixa de Pesca.

### A-038 · Travas durante a Expedição
**Seção do GDD:** 32 · **Situação:** Em vigor

Não dá para mudar a formação, alimentar, usar como alimento ou vender peixes do Cardume. Peixes
fora do Cardume, a pesca, a viagem e a Loja continuam normais. A trava de ataque na Arena entra com
a Arena (Milestone 7).

### A-039 · Resultado
**Seção do GDD:** 32 · **Situação:** Em vigor

Com o jogo aberto: aviso na tela e a janela do resultado. Com o jogo fechado: a janela aparece na
próxima vez que o jogo abrir (depois do Bem-vindo de volta, se houver).

---

## Milestone 7 — Arena local

### A-040 · Adversários simulados
**Seção do GDD:** 28, START HERE M7 · **Situação:** Em vigor (só no MVP local)

200 adversários gerados no seu PC (`config/arena_bots.json`), sempre iguais: quanto melhor a posição
inicial, maior o Cardume (2 a 6 peixes), maior o nível (1 a 10) e maior o tamanho; os de cima usam
peixes do Rio Selvagem. Eles colocam os peixes mais resistentes na frente. Você começa em último.

### A-041 · Seleção de adversários perto do topo
**Seção do GDD:** 28 · **Situação:** Confirmar

A janela é de ~10% acima da sua posição (arredondada para cima, no mínimo 1). Se ela tiver menos de
três adversários (perto do 1º lugar), completa com os mais próximos logo abaixo de você. No 1º
lugar, os três adversários são o 2º, o 3º e o 4º.

### A-042 · Vencer contra alguém abaixo de você
**Seção do GDD:** 29 · **Situação:** Confirmar

Só acontece perto do topo. Você ganha a Honra da vitória, mas ninguém troca de posição.

### A-043 · Ataques recebidos
**Seção do GDD:** 28–30 · **Situação:** Confirmar

A cada hora (também com o jogo fechado, até 24 verificações seguidas) há 35% de chance de um
adversário até ~10% abaixo de você atacar. Se ele vence, as posições trocam; se você defende, +8 de
Honra. Perder uma defesa não tira Honra. Quem está em último não é atacado (não há ninguém abaixo).

### A-044 · Formação dos adversários na tela
**Seção do GDD:** 26–27 · **Situação:** Em vigor

Os dois Cardumes aparecem espelhados, frente perto do centro. Cada ataque é um pequeno avanço do
peixe, um número de dano que sobe e some, e a barra de vida caindo. Não há efeito especial por
espécie nesta versão.

### A-045 · Loja da Arena vazia
**Seção do GDD:** 30 · **Situação:** Precisa de decisão (`OD-009`)

A aba existe e lê os itens de `arena.json → shop.items`, que está vazio. Nenhum item foi inventado.

---

## Milestone 8 — Mercado local

### A-046 · O que pode ir para o Mercado
**Seção do GDD:** 33 · **Situação:** Confirmar

Peixes do Aquário e varas do Inventário. Capturas da Caixa de Pesca não: elas ainda não são peixes
completos (seção 11), então precisam ser guardadas no Aquário antes. Peixe que está no Cardume
precisa sair dele antes de ser anunciado. A Vara Inicial e a vara equipada não podem ser anunciadas.

### A-047 · Duração do anúncio
**Seção do GDD:** 33 · **Situação:** Confirmar

Todo anúncio dura o máximo, 7 dias. O jogador pode cancelar a qualquer momento (o item vai para
Itens a Retirar). Não há escolha de duração menor.

### A-048 · Vara comprada no Mercado e revenda ao NPC
**Seção do GDD:** 19, 33 · **Situação:** Confirmar

A vara mantém tier, nível e bônus. A revenda ao NPC passa a considerar o que **você** pagou por ela
no Mercado (e o que você gastar em melhorias depois), não o que o dono anterior gastou. Assim não
dá para comprar barato no Mercado e revender ao NPC com lucro.

### A-049 · Mercado simulado
**Seção do GDD:** 33–34, START HERE M8 · **Situação:** Em vigor (só no MVP local)

Os outros jogadores são simulados (`config/market_bots.json`). Vendedores: cerca de 30 anúncios de
peixes dos dois mapas (alguns já com nível) e, às vezes, varas; novos anúncios de hora em hora,
cada um válido por 48 h. Eles nunca vendem abaixo do que o NPC paga. Compradores: a cada 20 minutos
cada anúncio seu tem uma chance de ser comprado — 25% no preço de referência, até o dobro se estiver
barato, e zero a partir de 4× a referência. Referência de um peixe = venda ao NPC × 1,5 × (1 + 15% por
nível acima do 1). A tela Vender mostra essa referência para ajudar a escolher o preço.

### A-050 · Filtros e ordenação
**Seção do GDD:** 34 · **Situação:** Em vigor

Filtros combinados: tipo (peixes/varas), espécie, raridade, categoria de tamanho, tamanho mín./máx.,
nível mín./máx. e preço mín./máx. Ordenação: menor preço, maior preço, maior tamanho, menor tamanho,
mais recentes. "Maior tamanho" ordena pelo tamanho relativo à espécie (percentil), para comparar
espécies diferentes de forma justa.

---

## Milestone 9 — Leilão local

### A-051 · Quem já lidera não dá outro lance
**Seção do GDD:** 35 · **Situação:** Confirmar

Enquanto você tem o maior lance, não pode aumentar o próprio lance. Isso evita pagar a taxa de 1%
duas vezes por engano (clique duplo, reconexão) e é o que torna um pedido repetido seguro.

### A-052 · Arredondamentos
**Seção do GDD:** 35 · **Situação:** Em vigor

Lance mínimo seguinte = lance atual × 1,03 arredondado para cima (no exemplo do GDD, 10.000 →
10.300). Taxa de 1% arredondada para o inteiro mais próximo (10.300 → 103). O primeiro lance
precisa ser pelo menos o lance inicial.

### A-053 · Leilão e anúncios
**Seção do GDD:** 33, 35 · **Situação:** Confirmar

O leilão conta separado dos 5 anúncios de preço fixo. As regras do que pode ir são as mesmas do
Mercado (peixe fora do Cardume; nem a Vara Inicial nem a vara equipada). Vara ganha em leilão: a
revenda ao NPC considera o lance vencedor, como em A-048.

### A-054 · Leilões simulados
**Seção do GDD:** 35, START HERE M9 · **Situação:** Em vigor (só no MVP local)

Cerca de 8 leilões de outros jogadores ficam abertos, com 2 novos por hora; o lance inicial fica
entre 50% e 90% da referência e nunca abaixo do que o NPC paga. A cada 10 minutos cada leilão —
inclusive o seu — tem 30% de chance de receber um lance simulado, de até 1,4× a referência. Tudo em
`config/market_bots.json → auctions`.

---

## Milestone 10 — Tutorial, UX e polimento

### A-055 · Passos do tutorial
**Seção do GDD:** 40 · **Situação:** Confirmar

Nove passos: boas-vindas → pegar a Vara Inicial na Loja (grátis, equipa sozinha) → iniciar a pesca →
primeira captura (o aviso mostra espécie, tamanho e categoria) → abrir a Caixa de Pesca → vender um
peixe → guardar um peixe no Aquário → colocar um peixe no Cardume → conhecer a Expedição (basta abrir
o menu ou clicar em Entendi; não precisa enviar). Os passos "ver as informações do peixe" e "a
Caixa" do GDD ficaram juntos no aviso da primeira captura e na abertura da Caixa. Arena, Mercado e
Leilão não entram no tutorial.

### A-056 · Pular o tutorial e saves antigos
**Seção do GDD:** 40 · **Situação:** Em vigor

O botão Pular tutorial aparece em todos os passos. Quem pula antes de pegar a vara recebe a Vara
Inicial na hora, para nunca ficar sem poder pescar. Quem já jogava antes desta versão não vê o
tutorial.

### A-057 · Central de notificações
**Seção do GDD:** 39 · **Situação:** Confirmar

O sino guarda os últimos 50 avisos relevantes enquanto o jogo está aberto (não são gravados). Entram:
espécie nova, recorde pessoal, Excepcional, subir de nível, venda no Mercado, leilão vendido/sem
lances/ganho/perdido, lance superado, anúncio vencido, Expedição concluída e ataques recebidos na
Arena. Capturas comuns e mensagens de erro ficam só como aviso rápido.

### A-058 · Modo compacto
**Seção do GDD:** 9 · **Situação:** Em vigor

Opções → Modo compacto muda a janela para 480×270 e esconde tudo, menos a cena, uma linha com o estado
da pesca e a Caixa, e o nome da última captura por 5 segundos. Expandir volta ao tamanho anterior.

### A-059 · Menu secundário
**Seção do GDD:** 7 · **Situação:** Em vigor

À direita do menu de cima: Moedas, Avisos (o sino, com contador de não lidos) e Opções (som, som
ambiente, volume e modo compacto). As Conchas foram para o cartão do jogador, só quando houver alguma.

### A-060 · Destaque de captura importante
**Seção do GDD:** 8 · **Situação:** Em vigor

Além da aura pulsante (roxa para rara, dourada para Excepcional, azul-clara para espécie nova), a
captura importante solta um punhado de faíscas da mesma cor que sobem e somem.

---

## Milestone 11 — Preparação do balanceamento

### A-061 · Nenhum número foi balanceado
**Seção do GDD:** 46, START HERE M11 · **Situação:** Em vigor

A pedido do proprietário, o Milestone 11 não mudou valores de `/config`. Ele entregou o simulador
(`./ops/scripts/simular.sh`) e o relatório com os números atuais; as revisões de progressão,
economia, raridade, combate, Expedições, varas, Mercado e Leilão ficam como tarefas abertas até o
proprietário decidir as metas.

### A-062 · Comprador simulado e vendedor
**Seção do GDD:** 35 · **Situação:** Em vigor (só no MVP local)

No leilão, o jogador simulado que dá o lance nunca é o próprio vendedor nem quem já tem o maior
lance.

---

## Revisão geral (após o Milestone 11)

### A-063 · Pesca depois de uma viagem com o jogo fechado
**Seção do GDD:** 10, 18 · **Situação:** Em vigor

Se a pesca estava ligada ao partir, ela volta a contar a partir do momento em que o barco **chega**,
mesmo com o jogo fechado: o tempo entre a chegada e a volta do jogador vira pesca offline (1 captura
por minuto, até 24 h), como qualquer outro tempo fora do jogo.

### A-064 · Trocar de vara durante a viagem
**Seção do GDD:** 18, 19 · **Situação:** Em vigor

Durante a viagem, só dá para equipar uma vara que sirva tanto no mapa de onde o barco saiu quanto no
de destino. Assim ninguém chega a um mapa com uma vara que ele não aceita.

### A-065 · Vender um peixe do Cardume pelo Aquário
**Seção do GDD:** 12, 23 · **Situação:** Em vigor

A confirmação de venda avisa, em destaque, quando o peixe está no Cardume e vai sair dele.

---

## V0.2 — Milestone 13: Áudio e ambiente

### A-066 · Som de cada momento
**Seção do GDD:** 8, 39 · **Situação:** Confirmar

| Momento | Som |
|---|---|
| Peixe pescado | Respingo curto e uma nota de marimba |
| Captura rara, Excepcional ou espécie nova | Respingo e um arpejo de sininhos |
| Recorde pessoal | Duas notas de marimba com brilho |
| Subir de nível (Pescador ou peixe) | Arpejo subindo com um acorde suave |
| Moedas (venda) | Tilintar de moedas |
| Momento importante (Expedição, leilão ganho…) | Dois sininhos |
| Aviso ou erro | Duas notas graves de marimba |
| Outros avisos (iniciar pesca…) | Clique bem discreto |

Se vários chegam juntos, toca só o mais importante, nesta ordem: recorde, subir de nível, captura
rara, momento importante, moedas, peixe pescado, aviso, clique.

### A-067 · Ambiente
**Seção do GDD:** 8 · **Situação:** Confirmar

Mar calmo (ondas a cada 6–9 s) na frente e uma brisa bem baixa atrás, os dois em loop. O mesmo
ambiente nos dois mapas por enquanto. Entra e sai com fade de 3 s. "Som ambiente" em Opções liga e
desliga os dois.

---

## V0.2 — Milestone 14: Visual Lago Dourado

### A-068 · A Bíblia de Arte decide o visual
**Seção do GDD:** 8 · **Situação:** Em vigor

O visual do jogo segue `docs/ART_BIBLE_V0_1.md`, enviada pelo proprietário em 29/09/2026. Ela
responde três dúvidas que estavam em aberto:

| Dúvida | Decisão | Seção da Bíblia |
|---|---|---|
| Câmera da cena de pesca | Continua lateral 2.5D, com camadas de profundidade | 9 |
| Quais raridades aparecem | Só as que existem nos dados (hoje Comum e Raro); as outras cores ficam reservadas | 4 |
| Fontes | Fredoka nos títulos e números, Nunito no corpo (as duas gratuitas) | 11 |

O tamanho Excepcional continua sendo tamanho, não raridade: tem um selo próprio com brilho dourado,
separado da cor de raridade do card.

### A-069 · O documento de 185 referências
**Seção do GDD:** 8 · **Situação:** Em vigor

O documento "Visual Bible 0.1 — 185 referências", enviado pelo proprietário em 29/09/2026, é
vocabulário visual: composição das telas, cards, cenário e momentos especiais. Ele **não** cria
mecânicas. Onde uma referência mostra algo que o jogo não tem (filtros extras no Mercado, cards
Incomum/Épico/Lendário, "Excepcional" como raridade, cachorro no barco, Força do Cardume na tela de
pesca), vale o que existe nos dados e o GDD. Em especial, **Excepcional continua sendo tamanho**, com
o seu selo dourado, separado da cor de raridade.

### A-070 · Três níveis de intensidade
**Seção do GDD:** 8, 39 · **Situação:** Confirmar

| Nível | Quando | O que aparece |
|---|---|---|
| Calmo | Cena e menus | Água, nuvens, sol, barco, boia e juncos sempre se movendo, sem brilho chamativo |
| Recompensa | Peixe pescado, venda, moedas | Aviso com acento colorido à esquerda (turquesa, dourado ou vermelho), contador de moedas que sobe com "+N" |
| Celebração | Captura rara, tamanho Excepcional, recorde, espécie nova, subir de nível, leilão vencido | Faixa grande no alto da tela com raios de luz, 3,2 s (ajustável), sem travar o jogo |

Quando vários peixes chegam juntos, só o mais valioso ganha faixa. As faixas nunca se empilham: uma
espera a outra (no máximo três na fila). A volta do offline continua sendo **um resumo só**.

### A-071 · Cor de cada momento
**Seção do GDD:** 8 · **Situação:** Confirmar

| Momento | Cor da faixa | Texto |
|---|---|---|
| Captura rara | A cor da raridade (Raro = azul) | "Captura rara!" · espécie · raridade · tamanho |
| Tamanho Excepcional | Dourado | "Tamanho Excepcional!" · espécie · tamanho |
| Novo recorde pessoal | Dourado | "Novo recorde!" · espécie · tamanho antigo → novo |
| Nova espécie | Turquesa | "Nova espécie!" · a silhueta do peixe se colore |
| Subir de nível | Dourado | "Nível N!" |
| Leilão vencido | Dourado | "Você venceu o leilão!" · onde o item está |
| Vitória na Arena | Dourado, com raios, no resultado da batalha | — |

A aura do peixe na cena segue a mesma regra: cor da raridade, dourado para Excepcional, turquesa para
espécie nova ou recorde.

### A-072 · Card de peixe
**Seção do GDD:** 11, 12, 44 · **Situação:** Em vigor

Todo peixe aparece no mesmo card: selo da raridade no alto à esquerda (com estrela), selo de status
no alto à direita ("NOVA ESPÉCIE", "RECORDE") ou o ✓ quando selecionado, o peixe grande no meio,
nome, tamanho, barra na cor da raridade e, embaixo, nível à esquerda e preço à direita. A borda do
card é a cor da raridade (Comum bem discreta). O tamanho Excepcional ganha um selo dourado sobre o
peixe, com um brilho que passa de tempos em tempos, e um halo dourado suave atrás do peixe.

### A-073 · Tela principal
**Seção do GDD:** 7, 8 · **Situação:** Em vigor

Barra do topo com o nome do jogo e o mapa, os menus com ícone e nome (só ícones quando a tela é
estreita), o contador de moedas, o sino com a bolinha vermelha de não lidos e Opções. Cartão do
pescador com retrato, nível, barra de XP e linhas com ícone. Painel de pesca com ícone do anzol,
estado, próxima captura, barra do ciclo e o botão Iniciar/Parar pesca. Botão da Caixa de Pesca no
canto com a caixa dourada. O tutorial passa a destacar em turquesa (dourado fica para recompensa).

### A-074 · Fotos nos menus
**Seção do GDD:** 18, 30, 31 · **Situação:** Em vigor

Os cards do Mapa mostram uma foto do lugar (escurecida com cadeado se ainda bloqueado), as quatro
Expedições têm cada uma a sua paisagem, e a Loja e o Mercado mostram a vara desenhada.

### A-075 · Vida na cena
**Seção do GDD:** 8 · **Situação:** Confirmar

Pequenos sinais de vida, sempre discretos (nível Calmo da Bíblia de Arte):

| O quê | Onde | Com que frequência |
|---|---|---|
| Poeira dourada flutuando na luz | Ar sobre o lago (no Rio, clara) | Sempre, 14 partículas no Lago e 9 no Rio |
| Libélula que entra, paira perto dos juncos e vai embora | Cantos de baixo | A cada 25–55 s |
| Sombra de peixe passando embaixo d'água | Água | A cada 12–26 s |
| Raios de sol bem fracos saindo do sol | Céu e água | Sempre, "respirando" devagar |
| Névoa fina correndo no horizonte | Horizonte | Sempre |
| Brilhos em estrela no reflexo do sol | Coluna de luz (só no Lago) | A cada 0,5–1,3 s |
| Anéis na água em volta do barco e da boia | Água | A cada 2–4 s |
| Respingos quando o peixe sai da água, e gotas pingando dele | Captura | Em toda captura; mais respingo na captura importante |
| Brilho suave pulsando atrás de "Iniciar pesca" | Painel de pesca | Só com a pesca parada |

Tudo pode ser reduzido ou desligado em `ambient_life` no `tema_visual.json` (0 desliga, 1 é o normal,
até 2).

### A-076 · A arte final do proprietário
**Seção do GDD:** 8, 44 · **Situação:** Em vigor

Os 32 pedidos feitos no ChatGPT (29/09/2026) substituíram a arte provisória: céu, montanhas, morros,
margens, juncos e nuvens dos dois mapas; os 20 peixes; barco, pescador, retrato e caixa de pesca; 16
ícones, a moeda e a concha; as duas varas; as quatro Expedições; e as fotos dos mapas. Na cena, o
pescador fica sentado no banco, dentro do barco (o casco é desenhado em duas camadas, atrás e na
frente dele), com as mãos no joelho e a vara apoiada nelas; as margens e montanhas se
refletem na água, ondulando devagar. O retrato aparece no cartão do pescador e no Perfil.

### A-077 · Água viva
**Seção do GDD:** 8 · **Situação:** Confirmar

A superfície da água se mexe: dez fileiras de ondinhas (um brilho claro com uma sombra fina embaixo)
deslizam devagar, pequenas perto do horizonte e maiores perto de quem olha. No Lago Sereno as
fileiras vão para lados alternados; no Rio Selvagem todas descem com a correnteza, mais rápido na
frente. Os reflexos das margens e montanhas são cortados em faixas que balançam fora de compasso,
mais longe do horizonte, mais balançam e mais apagados ficam. A textura das ondas é o arquivo
`Arte/Agua/ondas.png` (pode ser trocada).

### A-078 · Paisagem viva
**Seção do GDD:** 8 · **Situação:** Confirmar

Plantas e árvores soltas balançando na frente das margens, e animais que aparecem de vez em quando
(aves voando; garça, capivara, tartaruga, sapo e patos nas margens e na água; jacaré e macacos só no
Rio Selvagem) entram quando o proprietário mandar as imagens do documento "Pedidos de arte: paisagem
viva". São só cenário: nenhuma regra, recompensa ou mecânica nova.

Ritmo, a pedido do proprietário (sem "spam" de animação): um diretor de cenário deixa aparecer no
máximo um animal por vez, com descanso mínimo entre eles; os intervalos são sorteados (nunca fixos),
cada animal tem a sua raridade e não repete logo em seguida; as plantas balançam em rajadas de vento
que atravessam a tela, quase paradas entre elas; nada novo aparece durante uma celebração ou com
uma janela aberta. O ritmo do que já existe (peixes saltando, pássaros, libélulas, sombras) passa
pela mesma regra quando isso for feito.

**Feito (30/09/2026), com as imagens Vivo 01 a 18.** Plantas: pinheiros e uma árvore de copa redonda
nas pontas das margens do Lago Sereno, palmeiras e bananeiras nas do Rio Selvagem (com reflexo na
água), juncos e taboas (Lago) ou folhagens tropicais (Rio) junto das moitas dos cantos, e
vitórias-régias boiando no Lago. Elas vergam nas rajadas de vento, que atravessam a tela da esquerda
para a direita a cada 8 a 20 segundos; entre as rajadas, ficam quase paradas.

Animais (um por vez; depois que um sai, a cena descansa de 25 a 60 segundos; o primeiro aparece de
12 a 25 segundos depois de abrir o jogo):

| Animal | Onde | Como se comporta |
|---|---|---|
| Patos-do-mato voando | Céu, os dois mapas | Bando de 3 a 5 em fila solta, batendo as asas rápido |
| Garça-branca voando | Céu, os dois mapas | Sozinha, asas lentas (umas duas batidas por segundo) |
| Garça-branca na beira | Água rasa junto da moita da direita | Pousa planando, fica parada muito tempo, às vezes espreita e dá uma bicada na água; depois voa embora |
| Andorinhas | Rente à água | 2 ou 3, rasantes rápidos, batendo as asas em rajadas |
| Martim-pescador | Galho na moita da esquerda | Chega voando, espera no galho, mergulha com respingo e sai voando (às vezes só vai embora); o galho fica |
| Patos nadando | Meio do lago/rio | Casal atravessando devagar (no Rio, com a correnteza), às vezes mergulha a cabeça ou sacode as asas |
| Capivaras | Margem esquerda, ao fundo | Chega andando (às vezes com filhote), pasta um tempo e volta |
| Tartaruga | Pedra do canto direito | Sobe da água, toma sol, estica a cabeça de vez em quando e escorrega de volta |
| Sapo | Vitória-régia (só Lago) | Pula na folha, coaxa em sequências curtas e pula na água |
| Araras | Céu (só Rio) | Sempre em casal |
| Tucano | Céu (só Rio) | Voo em "bate e plana" |
| Jacaré | Água (só Rio) | Sobe à tona, desce o rio só com olhos e focinho de fora e afunda |
| Jacaré na margem | Margem direita (só Rio) | Sai da água e fica deitado de boca entreaberta |
| Macaco-prego e bugio | Árvores das margens (só Rio) | Aparecem entre as folhas, um pendurado pela cauda balançando |

As posições, a chance de cada animal e os tempos ficam em `Resources/Visual/paisagem_viva.json` e
podem ser ajustados sem mexer em código.

### A-079 · Dois filtros na Caixa de Pesca e cores de raridade e tamanho
**Seção do GDD:** 11 · **Situação:** Decidido pelo proprietário (29/09/2026)

Muda a regra "um filtro principal por vez" da seção 11, a pedido do proprietário: a Caixa de Pesca
tem **dois grupos de filtro separados**, Raridade (Todas · Comum · Raro) e Tamanho (Todos · Pequeno ·
Adulto · Grande · Excepcional). Um de cada grupo pode estar escolhido ao mesmo tempo e eles se
combinam (Raro + Grande mostra só os Raros grandes). "Revisar peixes", na confirmação de venda,
continua mostrando só os peixes valiosos da venda, por cima dos dois filtros.

Cores (primeira definição, ajustáveis em `Resources/Visual/tema_visual.json`):

| | Cor |
|---|---|
| Comum | branco |
| Raro | azul |
| Pequeno | verde |
| Adulto | lilás |
| Grande | laranja |
| Excepcional | dourado (o mesmo selo com brilho de sempre) |

A cor continua sendo só acento, como manda a Bíblia de Arte: um ponto colorido no filtro (fundo e
borda na cor quando escolhido), a borda, o selo e a barra do card para a raridade, e o nome do
tamanho escrito na cor dele no card. No tamanho Excepcional, o selo dourado ocupa esse mesmo lugar (o canto
direito da linha do tamanho, abaixo do nome), em vez de ficar por cima do desenho do peixe. As raridades reservadas para depois (Incomum, Épico, Lendário,
Mítico) têm cores guardadas no tema; se alguma entrar no jogo, as cores de tamanho são revistas para
não se confundirem.

A cor do tamanho vale em todas as telas, não só na Caixa de Pesca: nos cards do Aquário (inclusive
na escolha de alimento) e do Mercado, na ficha do peixe no Aquário (um selo na cor do tamanho ao lado
do selo da raridade), nas linhas do Mercado (anúncios, retiradas e detalhes), nos filtros de
raridade e tamanho do Mercado e na lista de peixes do Cardume, no Perfil.

### A-080 · Som só nos momentos que importam
**Seção do GDD:** 8, 39 · **Situação:** Decidido pelo proprietário (29/09/2026)

Muda a A-066: o proprietário achou cansativo ter som em todo aviso. Agora os avisos só fazem som em
três momentos:

| Momento | Som |
|---|---|
| Subir de nível (Pescador ou peixe) | Arpejo subindo com um acorde suave |
| Captura Excepcional | Respingo e um arpejo de sininhos |
| Espécie nova | Respingo e um arpejo de sininhos |

Os outros avisos (peixe pescado, peixe raro, recorde, moedas, Expedição, Mercado, avisos e erros,
iniciar pesca…) continuam aparecendo na tela, mas em silêncio. O som ambiente (mar e brisa) não muda
e continua com a sua própria chave em Opções. Os arquivos dos sons silenciados ficam guardados em
`Resources/Sons`, sem uso.

### A-081 · Ambiente com gravações longas
**Seção do GDD:** 8, 39 · **Situação:** Decidido pelo proprietário (30/09/2026)

Muda a A-067: o proprietário achou esquisito o som de ondas feito por código, que se repete a cada
poucos segundos. O ambiente de cada mapa passa a ser uma lista de gravações reais e sem direitos
autorais, de 2 a 3 minutos cada (Lago Sereno: água calma batendo na margem, pássaros ao longe; Rio
Selvagem: correnteza, mata), tocadas uma depois da outra em ordem sorteada, com 8 segundos de
passagem suave entre elas e sem repetir a que acabou de tocar. Com 4 gravações por mapa, o que se
ouviu só volta depois de uns 10 minutos. Ao trocar de mapa, o som passa suavemente para o do mapa
novo. Enquanto as gravações não chegam, fica o som antigo. `ASSET_PENDENTE`: as gravações.

### A-082 · Relatório da Expedição dentro da aba Expedição
**Seção do GDD:** 32 · **Situação:** Decidido pelo proprietário (30/09/2026)

Quando o Cardume volta, o resultado não aparece mais solto por cima da tela, onde o jogador estiver.
Aparece um aviso ("Seu Cardume voltou da Expedição!") e um ponto vermelho no botão Expedição. Na
próxima vez que o jogador abrir a Expedição, o **relatório** abre por cima da aba: a foto da
Expedição, quando ela voltou (data e hora), as moedas que trouxe, o aproveitamento e o peixe
encontrado, com o card oficial em destaque. Enquanto o relatório está aberto, a aba fica por trás,
sem poder ser usada. Ao clicar em "Ótimo!", o relatório some e não volta; se o jogador fechar a aba
sem clicar, ele continua lá na próxima vez (também depois de fechar o jogo). As moedas e o peixe já
estão com o jogador desde a volta: o relatório só mostra.

### A-083 · Peixes mais raros vêm grandes um pouco menos
**Seção do GDD:** 36 · **Situação:** Decidido pelo proprietário (30/09/2026)

Muda o sorteio de tamanho da seção 36, que era igual para toda raridade: quanto mais rara a raridade
do peixe, um pouco menor a chance de ele vir Grande ou Excepcional, e maior a de vir Pequeno ou
Adulto. A mudança é pequena de propósito. Com os valores atuais (`progression.json`,
`size_weight_multipliers` de cada raridade):

| Raridade | Pequeno | Adulto | Grande | Excepcional |
|---|---|---|---|---|
| Comum | 20% | 60% | 19% | 1% |
| Raro | 20,7% | 62,0% | 16,7% | 0,72% |

O bônus de qualidade de tamanho da vara continua valendo por cima (ele multiplica Grande e
Excepcional, e a raridade multiplica de novo). Raridades novas, quando entrarem, ganham os seus
próprios multiplicadores, cada vez menores.

### A-084 · Busca pelo nome do peixe
**Seção do GDD:** 11, 12, 30 · **Situação:** Decidido pelo proprietário (30/09/2026)

A Caixa de Pesca, o Aquário e o Mercado têm um campo "Buscar peixe pelo nome", com uma lupa:

- **Caixa de Pesca e Aquário:** no alto da janela, ao lado de "Fechar". Na Caixa, a busca se soma
  aos filtros de raridade e tamanho; no Aquário, vale para a lista e para a escolha de alimento.
- **Mercado:** no alto, à direita, nas abas Comprar, Vender e Leilão (também na escolha do peixe para
  criar um leilão). Trocar de aba começa a busca vazia. *(Revisto em 01/10/2026: antes, em Comprar, a
  busca era o primeiro filtro da coluna e passava despercebida, e o Leilão não tinha busca.)*

Basta digitar parte do nome, sem se preocupar com maiúsculas ou acentos ("tilapia" acha "Tilápia",
"dour" acha "Dourado"). O ✕ limpa a busca. Ao abrir a janela de novo, a busca começa vazia. É só um
jeito de achar o peixe na tela: não muda nenhuma regra.

### A-085 · Cada mapa é um capítulo da jornada
**Seção do GDD:** 18 · **Situação:** Decidido pelo proprietário (30/09/2026, `docs/IDENTIDADE_POR_MAPA.md`)

Ao chegar a um mapa depois da viagem, o nome dele aparece por cima da cena como o título de um
capítulo: "CAPÍTULO 2" em dourado, o nome do mapa grande e uma frase curta com a sensação do lugar
(Lago Sereno: "Um começo tranquilo e bonito."; Rio Selvagem: "A aventura sai do conforto do lago.").
Ele surge devagar, fica uns 4 segundos e some sozinho, sem caixa, sem raios e sem bloquear nada; o
aviso "Você chegou a…" saiu, porque o título faz esse papel. No menu do Mapa, cada card mostra o
capítulo e a mesma frase. O número do capítulo segue o nível que libera o mapa.

O resto do documento de identidade já vale para os dois mapas: fauna própria (A-078), água própria
(A-077: calma no Lago, correnteza no Rio), luz própria (fim de tarde e manhã) e som ambiente próprio
por mapa (A-081, gravações pendentes). Os mapas 3 e 4 dependem da decisão `OD-018`.

### A-086 · Tamanho Perfeição, acima do Excepcional
**Seção do GDD:** 36 · **Situação:** Decidido pelo proprietário (30/09/2026)

Nova categoria de tamanho, acima do Excepcional: **Perfeição**. De cada 100 peixes que viriam
Excepcionais, 1 vem Perfeição (no Comum: Excepcional 0,99% e Perfeição 0,01%; no Raro, os dois caem
na mesma proporção, 0,72% e 0,007%). É o maior tamanho possível da espécie (percentil 99,8 a 100) e dá
**+5% em todos os atributos** daquele peixe (Vida, Ataque, Defesa e Velocidade), para sempre.

Como o Excepcional, ela tem selo próprio (PERFEIÇÃO, em azul-diamante com o brilho que passa),
aviso com som, celebração própria ("Perfeição!"), pede confirmação na venda em lote, conta como
alimento valioso e ganha um filtro na Caixa de Pesca. O Perfil conta as capturas Perfeição. O bônus
de tamanho da vara vale para ela como vale para o Excepcional. Tudo ajustável em
`config/progression.json` (`draw_weight`, `stat_multiplier`).

### A-087 · Ordenar a Caixa de Pesca pelos mais caros
**Seção do GDD:** 11 · **Situação:** Decidido pelo proprietário (30/09/2026, aprovado pela prévia)

Abaixo dos filtros da Caixa de Pesca, uma linha "Ordenar:" com **Mais recentes** (a ordem de sempre,
a que aparece ao abrir) e **Mais caros** (o peixe que vale mais moedas primeiro, pelo mesmo valor do
card; em empate, o mais recente primeiro). A ordem se soma aos filtros e à busca.

### A-088 · Sucesso da Captura: o peixe pode escapar
**Seção do GDD:** 8, 9, 10 · **Situação:** Decidido pelo proprietário (30/09/2026, `docs/SISTEMA_SUCESSO_PESCA.md`)

Cada tentativa de pesca tem duas etapas. Primeiro o jogo sorteia o peixe que mordeu, como sempre.
Depois sorteia se o pescador consegue puxá-lo, com a **Chance de Sucesso da Captura**:

> chance = chance-base da raridade + vara + barco + isca, sempre entre 5% e 95%

Chance-base: **Comum 50%**, **Raro 38%**. A do **Épico (24%)** entra quando essa raridade existir
(mapas 3 e 4, decisão `OD-019`). Se o peixe escapa, nada acontece com o save além da contagem de
escapes: sem peixe na Caixa, sem XP, sem Conchas, sem espécie descoberta, sem recorde. A pesca
offline usa a mesma regra, tentativa por tentativa. O intervalo da pesca não mudou (30 s online,
60 s offline); os textos agora falam em "tentativa" e "fisgada" em vez de "captura".

O escape dura poucos segundos e nunca bloqueia: a vara luta, a boia afunda e uma sombra escura se
mexe na água; depois a linha afrouxa, sobe um respingo curto, a sombra foge e o pescador lança de
novo. Por cima da boia sobe **"Você ainda não é bom o suficiente."** e, menor, "Melhore sua vara,
barco ou isca para aumentar suas chances.". A espécie que escapou não é revelada, só a raridade.
Peixe mais raro luta mais tempo e mais forte, a mensagem ganha a cor da raridade e fica um aviso na
lista ("Um peixe Raro escapou! Sua chance de puxar era 43%."). O escape de um Comum não gera aviso na
lista, para não virar spam. **Sem som:** decidido pelo proprietário em 01/10/2026 (`OD-020`); vale a
regra de som só no nível, no Excepcional/Perfeição e na espécie nova (A-080). O "Bem-vindo de volta" mostra quantos peixes escaparam. O Perfil ainda não mostra
os escapes (só ficam contados no save).

### A-089 · Barcos
**Seção do GDD:** 7, 19 · **Situação:** Decidido pelo proprietário (30/09/2026)

Nova aba **Barcos** na Loja. O barco é para sempre, soma pontos na Chance de Sucesso e pode ser
trocado por outro que o jogador já tem. Comprar já coloca em uso.

| Barco | Bônus | Custo | Nível |
|---|---:|---:|---:|
| Barco Inicial | +0% | todo jogador tem | 1 |
| Barco 1 | +3% | 3.000 moedas | 5 |
| Barco 2 | +6% | 15.000 moedas | 12 |
| Barco 3 | +9% | 45.000 moedas + 30 conchas | 20 |
| Barco 4 | +12% | 130.000 moedas + 120 conchas | 30 |
| Barco 5 | +15% | 350.000 moedas + 350 conchas | 40 |

São as primeiras compras que gastam **Conchas**. O barco ainda não aparece na cena: cada um usa o
ícone de barco até a arte existir (`ASSET_PENDENTE`, `M15-T07`). Números provisórios, em
`config/equipment.json`.

### A-090 · Iscas
**Seção do GDD:** 7 · **Situação:** Decidido pelo proprietário (30/09/2026)

Nova aba **Iscas** na Loja. Cada compra dá **100 tentativas**; comprar de novo soma. A isca em uso
gasta 1 tentativa por pescaria, puxando o peixe ou não, também offline. Só uma fica em uso;
"Guardar" tira a isca de uso sem perder as tentativas, e "Usar" troca por outra que ainda tem
tentativas. Quando a última tentativa acaba, a isca sai de uso sozinha e aparece o aviso "Sua Isca
Simples acabou. Compre mais na Loja → Iscas." (também no "Bem-vindo de volta"). O jogo não troca
sozinho para outra isca.

| Isca | Bônus | Custo (100 tentativas) | Nível |
|---|---:|---:|---:|
| Isca Simples | +5% | 200 moedas | 1 |
| Isca Melhorada | +10% | 1.200 moedas | 10 |
| Isca Premium | +15% | 800 moedas + 6 conchas | 18 |

### A-091 · Onde o jogador vê a chance
**Seção do GDD:** 8 · **Situação:** Decidido na implementação (30/09/2026)

- **Painel de pesca:** a segunda linha da direita (antes, o nome do mapa, que continua na barra de
  cima) mostra a chance de cada raridade que morde ali ("Comum 50% · Raro 38%") e, com isca em uso, o
  ícone da isca com as tentativas que faltam. Clicar abre a Loja.
- **Loja:** à esquerda, "Seu equipamento" com a vara, o barco e a isca, o bônus de cada um e o total,
  e "Chance de puxar o peixe" por raridade, com barra. Raridade que não morde no mapa atual aparece
  apagada, com "não morde aqui". A vara mostra o novo bônus "Sucesso da captura" (Vara Inicial +0%,
  Vara 1 de +2% a +12%).

### A-092 · Rebalanceamento do Sucesso da Captura
**Seção do GDD:** 17, 36 · **Situação:** Decidido na implementação (30/09/2026), números provisórios

Com metade das mordidas escapando no começo, o jogo ficaria duas vezes mais lento. Para manter o
ritmo sem mexer no intervalo de pesca:

- XP do Pescador, valor de venda e XP como alimento de todas as espécies foram **dobrados**;
- a chance de Conchas por peixe puxado foi de 5% para **10%**;
- o Aruanã (Raro) morde duas vezes mais: peso de 5 para **10** no Rio Selvagem.

Resultado medido no simulador (5 jogadores, estratégia da seção 1 do relatório):

| Medida | Antes | Agora |
|---|---:|---:|
| Nível 10 | 2,1 h | 2,1 h |
| Nível 20 | 4,6 h | 4,4 h |
| Nível 30 | 10,1 h | 8,8 h |
| Peixes na Caixa por hora | 120 | 78 (e 42 escapes) |
| Moedas por hora, Lago Sereno | 3.375 | 3.298 |
| Moedas por hora, Rio Selvagem | 27.182 | 37.481 |
| Conchas por hora, Rio Selvagem | 5,6 | 7,6 |
| Primeiro peixe Raro | 3,8 h | 3,9 h |

O Rio Selvagem rende mais que antes porque o jogador simulado compra barcos. Os barcos (543 mil
moedas e 500 conchas no total) e as iscas são o gasto novo. Detalhes em
`docs/relatorios/SIMULACAO_BALANCEAMENTO.md`, seção 7.

### A-093 · Mapas 3 e 4, raridade Épico e Vara 2
**Seção do GDD:** 17, 19, 21, 22 · **Situação:** Decidido pelo proprietário (30/09/2026, `docs/PROGRESSAO_MAPAS_3_4.md`)

| Mapa | Nível | Vara mínima | Espécies | Épicos |
|---|---:|---|---|---|
| Pantanal Dourado (`map_03`) | 20 | Vara 1 | 6 Comuns, 3 Raros, 1 Épico | Barbado |
| Estuário das Marés (`map_04`) | 30 | Vara 2 | 5 Comuns, 3 Raros, 2 Épicos | Camurupim e Mero |

- **Épico**, acima do Raro: roxo `#A855F7`, venda ×4, atributos ×1,3, XP do Pescador ×5, XP como
  alimento ×3,5. Chance de puxar 24% (A-088). Vem Grande ×0,7, Excepcional ×0,45 e Perfeição ×0,45 (a
  regra do A-083 levada ao Épico). Pede confirmação na venda em lote e conta como alimento valioso.
- **Vara 1** agora pesca Épico, mas só onde o mapa tem Épico: o Rio Selvagem continua sem nenhum.
- **Vara 2:** 90.000 moedas, liberada no nível 30, Nv.1 a 10 (melhorias de 10.000 a 260.000; 765.000 no
  total), bônus de raridade de +24% a +50%, tamanho de +23% a +42%, Conchas de +55% a +110% e puxar de
  +8% a +20%. O Nv.1 começa um pouco acima da Vara 1 Nv.10.
- Atributos, tamanhos e pesos das 20 espécies são os do documento. **Venda, XP e XP como alimento
  foram multiplicados por 1,75 no Pantanal e 1,55 no Estuário.** O documento mede com 100% de captura;
  nessa fase a vara e o barco já puxam 60% a 70% dos peixes, então o fator é menor que o ×2 dos mapas 1 e
  2. Assim as metas do documento se mantêm: Nível 20→30 em 3,7 h online (meta 3,8 h) e 30→40 em 4,0 h
  (meta 4,0 h).
- Com a Chance de Sucesso, um Épico puxado leva em média umas 11 h no Pantanal com a Vara 1 Nv.1 sem
  barco (0,30% de mordida × 26%), umas 5 h com a Vara 1 Nv.10 e o Barco 3, e umas 2,5 h no Estuário
  com a Vara 2 e o Barco 4. Sem pity timer.
- Viagem igual (manual, 30 s). Ao abrir o jogo, quem já está no nível 20 ou 30 vê os mapas liberados,
  sem ser levado para eles. A Enciclopédia passa a ter 40 espécies. O título de capítulo tem as frases
  "Agora o mundo se abriu de verdade." (Capítulo 3) e "Cheguei em uma nova fronteira do jogo."
  (Capítulo 4).
- Cenários, peixes, Vara 2 e miniaturas dos mapas são **provisórios**, pintados por script
  (`ASSET_PENDENTE`); os pedidos para o ChatGPT estão no documento "Pedidos de arte: Pantanal Dourado e
  Estuário das Marés". A paisagem viva usa os animais que já existem (araras, garças, capivaras,
  jacaré, patos, andorinhas) até as artes novas chegarem. O som ambiente espera gravações
  `pantanal_01..04` e `estuario_01..04` (até lá, o som antigo).

### A-094 · Paisagem viva dos mapas 3 e 4
**Seção do GDD:** 18 · **Situação:** Decidido na implementação (01/10/2026), com a arte do proprietário

Os animais novos reaproveitam os comportamentos que já existem (A-078), com o mesmo diretor de cenário
(um animal por vez, descanso sorteado):

- **Pantanal Dourado:** casal de araras-azuis cruzando o céu; tuiuiú voando devagar e às vezes pousando
  na beira para caçar (como a garça); colhereiros em fila de 1 a 3; andorinhas, patos, jacaré e
  capivaras. Palmeiras carandaá balançam nas margens e aguapés boiam perto dos cantos.
- **Estuário das Marés:** bando de 3 a 6 guarás; trinta-réis rápidos e baixos sobre a água; garça; um
  caranguejo chama-maré que sai da lama do canto, acena com a garra e volta para a toca; um boto-cinza ao
  longe que aparece duas ou três vezes seguidas, às vezes saltando. Capim e samambaias do mangue
  balançam nos cantos.
- No Pantanal, uma ou duas borboletas amarelas (uma limão, uma laranja) passam baixo pelos cantos, com
  batidas rápidas e planadas curtas.
- O fundo de cada mapa alterna duas faixas pintadas diferentes (capões no Pantanal, mangue no
  Estuário), em vez de repetir a mesma espelhada.

### A-095 · Bônus de puxar das varas, barcos e iscas um terço menores
**Seção do GDD:** 19 · **Situação:** Decidido pelo proprietário (01/10/2026)

O proprietário achou altos os bônus de chance do equipamento. Todos caem um terço, e a chance-base de
cada raridade não muda:

| Equipamento | Antes | Agora |
|---|---|---|
| Barcos 1 a 5 | +3%, +6%, +9%, +12%, +15% | +2%, +4%, +6%, +8%, +10% |
| Iscas Simples, Melhorada e Premium | +5%, +10%, +15% | +3%, +7%, +10% |
| Vara 1, do Nv. 1 ao 10 | +2% a +12% | +1,5% a +8% |
| Vara 2, do Nv. 1 ao 10 | +8% a +20% | +5,5% a +13,5% |

Na simulação, do Nível 20 ao 30 passa de 3,7 h para 4,0 h online, e do 30 ao 40, de 4,0 h para 4,5 h.
Escapam cerca de 45 peixes por hora, contra 37 antes.

*Ajuste no mesmo dia (pedido do proprietário):* para voltar às metas de `docs/PROGRESSAO_MAPAS_3_4.md`,
o XP para subir de nível ficou menor entre os níveis 20 e 50 — de 3% a 5% menos do 20 ao 29, 9% menos
do 30 ao 39, e voltando aos poucos ao normal até o 50. Simulação: 20→30 em 3,8 h e 30→40 em 4,0 h.

### A-096 · O barco, a vara e a isca equipados aparecem na cena
**Seção do GDD:** 8, 19 · **Situação:** Pedido do proprietário (01/10/2026)

O que o jogador equipa na Loja aparece na cena de pesca, na hora:

- **Barco:** os Barcos 1 a 5 usam a mesma pintura da Loja. O pescador fica sentado dentro dele, atrás
  da lateral da frente. Barcos maiores aparecem maiores. O Barco Inicial continua com o casco próprio
  da cena.
- **Vara:** a vara na mão dele é a pintura da vara em uso (Vara Inicial, Vara 1 ou Vara 2) e se mexe
  com o arremesso como antes.
- **Isca:** com uma isca em uso, ela fica pendurada no anzol logo abaixo da boia. Dentro da água ela
  aparece mais apagada, e some quando o peixe é puxado (o peixe levou a isca). Sem isca, só a boia.
  Por enquanto a isca é uma forma simples em uma cor para cada tipo (`ASSET_PENDENTE`, ver
  `docs/ASSETS_PENDENTES.md`).

É só visual: não muda nenhuma chance. Os números de posição e tamanho ficam em
`Resources/Visual/equipamento_cena.json`, gerados por `tools/Arte/equipamento_na_cena.py`.

### A-097 · Peixes pela metade do preço e iscas bem mais caras
**Seção do GDD:** 21 · **Situação:** Decidido pelo proprietário (02/10/2026)

O proprietário juntava moedas demais sem ter onde gastar. Então:

- **Venda de peixes:** todo preço de venda ao comerciante vale metade. Um ajuste geral novo
  (`economy.json → npc_fish_sale.price_multiplier`, hoje 0,5) multiplica todos os preços de uma vez,
  sem mexer no valor de cada espécie.
- **Iscas:** Simples 1.000 moedas (antes 200), Melhorada 6.000 (antes 1.200), Premium 4.000 moedas e
  12 Conchas (antes 800 e 6). Continuam com 100 tentativas por compra.

Na simulação, a pesca rende 1.649 moedas por hora no primeiro mapa (antes 3.246) e 13.783 no segundo
(antes 28.215). A Vara 2 chega com 9,2 h de pesca (antes 8,6 h). No primeiro mapa as iscas quase não
se pagam: valem mais nos mapas de peixe caro.

*Revisto no mesmo dia (pedido do proprietário):* a venda de peixes **voltou ao preço normal**
(`price_multiplier` = 1), porque melhorar a vara e comprar barcos já pede bastante. As iscas continuam
mais caras. O ajuste geral fica no Painel de Balanceamento para uso futuro.

### A-098 · Dólares, a terceira moeda
**Seção do GDD:** 21 · **Situação:** Pedido do proprietário (02/10/2026); uso ainda a decidir (OD-021)

O jogo passa a ter três moedas: **Moedas**, **Conchas** e **Dólares**. Os Dólares começam em zero
para todo mundo, inclusive em saves antigos, e por enquanto não se ganham nem se gastam. O que eles
fazem é decisão do proprietário (OD-021). A intenção registrada é que Conchas e Dólares sejam
negociados entre jogadores.

Na tela, as Conchas e os Dólares ficam numa faixa pequena logo abaixo das Moedas, no canto de cima à
direita, sempre visíveis. Os avisos (toasts) descem um pouco para não cobrir a faixa. O ícone dos
Dólares é provisório (`ASSET_PENDENTE`).

### A-099 · Todo equipamento pede Conchas, e as Conchas ficaram mais raras
**Seção do GDD:** 19, 21 · **Situação:** Pedido do proprietário (02/10/2026)

O objetivo do proprietário é que os jogadores negociem Conchas (e Dólares) para comprar os itens
antes. Por isso toda compra e toda melhoria de equipamento pede Conchas, além das Moedas:

| Item | Moedas | Conchas |
|---|---:|---:|
| Vara 1 | 2.500 | 5 |
| Melhorias da Vara 1 (Nv. 2 a 10) | como antes | 2, 3, 4, 5, 6, 8, 10, 12, 15 |
| Vara 2 | 90.000 | 40 |
| Melhorias da Vara 2 (Nv. 2 a 10) | como antes | 10, 12, 15, 18, 22, 26, 30, 35, 40 |
| Barcos 1 a 5 | como antes | 5, 15, 25, 90, 250 (antes 0, 0, 30, 120, 350) |
| Iscas Simples, Melhorada, Premium | 1.000, 6.000, 4.000 | 1, 3, 12 |

As Conchas ficaram um pouco mais difíceis: a chance-base por peixe puxado caiu de 10% para 7%. Para
ninguém ficar travado sem Conchas no começo, a Vara Inicial agora também dá Conchas, na chance-base e
sem bônus.

Na simulação, sem comércio: a Vara 1 chega com 2,2 h de pesca, como antes; a Vara 2 com 10,8 h (antes
8,4 h), e o trecho do nível 30 ao 40 leva 4,9 h (antes 4,0 h). Os Barcos 3, 4 e 5 chegam com 9,9 h,
20,3 h e 40,8 h. A diferença é o espaço do comércio de Conchas.

### A-100 · Ranking só de jogadores reais
**Seção do GDD:** 7 · **Situação:** Pedido do proprietário (02/10/2026)

Um menu **Ranking** mostra a posição dos jogadores em quatro listas: **Nível**, **Moedas**, **Conchas**
e **Peixes pescados**. Só entram jogadores reais: nunca jogadores simulados (nem os da Arena ou do
Mercado).

Enquanto o jogo é local, o único jogador real é o deste computador, então ele aparece sozinho em 1º,
com uma nota explicando que, quando o jogo for online, todos os jogadores reais aparecem ali. O
serviço do ranking já tem o formato da versão online; só a fonte muda.

*Revisto no mesmo dia (pedido do proprietário):* o **Ranking** fica na barra de menus do topo, logo
depois da Arena. Para os nove menus caberem com nome, o sino de avisos e as opções viraram só ícone, e
a barra mede o espaço de verdade dos dois lados em vez de reservar um valor fixo.

### A-101 · Comércio de Conchas e Dólares no Mercado
**Seção do GDD:** 31–33 · **Situação:** Pedido do proprietário (02/10/2026); no jogo local, só a tela

O Mercado ganha a aba **Conchas e Dólares**: o jogador anuncia uma quantidade de Conchas ou de Dólares
por um preço total em Moedas. Enquanto estiver anunciada, a quantidade sai da carteira; cancelar
devolve na hora. Na venda, o vendedor paga a mesma taxa de 3% dos outros anúncios. Até 5 anúncios ao
mesmo tempo (`economy.json → currency_trade`).

Só jogadores reais negociam. Decisão do proprietário: enquanto o jogo for local, ninguém fica do outro
lado (sem jogadores simulados). A lista "Ofertas de outros jogadores" aparece vazia, com uma nota
explicando que os anúncios dos outros jogadores chegam com o jogo online. Comprar uma oferta e receber
pela venda entram junto com o servidor.

### A-102 · Zoom da cena
**Seção do GDD:** 8 · **Situação:** Pedido do proprietário (02/10/2026)

O jogador pode aproximar um pouco a câmera do pescador: girando a rodinha do mouse sobre a cena, ou no
controle **Aproximar** em Configurações. Vai do cenário inteiro até cerca de 20% mais perto, mirando
o barco, sempre com uma transição suave. Com uma janela ou painel aberto, a rodinha rola as listas e
não mexe no zoom. A escolha fica guardada neste PC, como o volume. É só visual: não muda nada na pesca.

### A-103 · Nomes próprios, descrições e preços do equipamento
**Seção do GDD:** 19 · **Situação:** Decidido pelo proprietário (02/10/2026, `docs/EQUIPAMENTOS_NOMES_PRECOS.md`)

Os itens ganham nomes próprios (os ids internos e os saves não mudam):

- **Varas:** Caniço Manso (inicial) → Ponta Selvagem (Vara 1) → Maré Dourada (Vara 2).
- **Barcos:** Água Mansa → Remo Valente → Rastro Azul → Proa Selvagem → Costa Nobre → Horizonte Dourado.
- **Iscas:** Terra Viva (minhoca) → Maré Viva (camarão) → Ouro de Maré (artificial dourada).

Cada um tem uma frase curta na Loja. Preços e desbloqueios:

| Barco | Nível | Moedas | Conchas |
|---|---:|---:|---:|
| Remo Valente | 10 | 12.000 | 5 |
| Rastro Azul | 20 | 45.000 | 15 |
| Proa Selvagem | 30 | 160.000 | 25 |
| Costa Nobre | 50 | 600.000 | 120 |
| Horizonte Dourado | 80 | 2.500.000 | 500 |

| Isca | Tentativas | Moedas | Conchas |
|---|---:|---:|---:|
| Terra Viva | 60 | 1.500 | 1 |
| Maré Viva | 60 | 6.000 | 3 |
| Ouro de Maré | 60 | 15.000 | 5 |

As varas mantêm preços e melhorias. Onde o documento conflitava com decisões mais recentes do
proprietário, valeu a decisão mais recente, como o próprio documento pede ("a build atual é a fonte de
verdade"):

- **Bônus de puxar:** continuam um terço menores (A-095): barcos +2% a +10%, iscas +3%, +7% e +10%. O
  documento sugeria +3% a +18% e +4%, +8% e +12%.
- **Conchas:** todo item pede Conchas (A-099), então os barcos e iscas baratos também pedem algumas.
  O Ouro de Maré pede 5 Conchas, e não 10: com 10, gastaria mais Conchas por hora do que o jogador
  ganha.

Na simulação (sem comércio): o Remo Valente é comprado cerca de 20 minutos depois de liberar e o
Rastro Azul logo ao liberar (o jogador chega com moedas guardadas); o Proa Selvagem cerca de 3 h
depois, porque disputa com a Maré Dourada; Costa Nobre e Horizonte Dourado ficam para o longo prazo.
No Lago Sereno as iscas não se pagam; elas valem a partir do Rio Selvagem.

### A-104 · Progressão dos Mapas 5 a 10, adaptada às regras atuais
**Seção do GDD:** 2, 15, 16, 17, 19 · **Situação:** Aprovado pelo proprietário (05/10/2026); Mapas 5 e 6 no jogo (A-105), Mapas 7 a 10 a seguir

O proprietário mandou `docs/PROGRESSAO_MAPAS_5_A_10.md` (Mapas 5 a 10, 60 espécies, Lendário e Mítico,
Varas 3 a 5) e pediu para manter os mapas e os peixes, adaptando ao jogo de agora. A versão adaptada é
`docs/PROGRESSAO_MAPAS_5_A_10_ADAPTADA.md`, com os números em `docs/propostas/mapas_5_10.json`. Como na
A-103, onde o documento conflitava com uma regra mais recente, valeu a regra mais recente:

- **Valores dos peixes:** o Mapa 5 do documento rendia menos que o Mapa 4 atual. Venda e XP de alimento
  foram reescalados por raridade (Mapa 5 = 1,5× e 1,3× o Mapa 4), mantendo a escada entre os mapas e a
  proporção entre as espécies.
- **XP do Pescador:** calibrado para cada faixa de 10 níveis levar o tempo da faixa 30–40 de hoje, sem o
  XP de um peixe cair de um mapa para o outro; do Nv.60 em diante as faixas ficam mais rápidas (OD-047).
- **Varas 3 a 5:** chance de puxar um terço menor (A-095), Conchas em toda compra e melhoria (A-099) e
  preço em Moedas proporcional à renda nova (cerca de 3 h online para cada compra).
- **Cores:** Lendário vermelho-rubi `#E5484D` (o laranja do documento colide com o tamanho Grande e o
  dourado com Excepcional); Mítico rosa-framboesa `#EC4899`, como no documento.
- **Tamanho:** Lendário e Mítico vêm Grandes, Excepcionais e Perfeição um pouco menos, seguindo Raro e Épico.
- **Capítulos 5 a 10** com uma frase de chegada cada (A-085).

### A-105 · Mapas 5 e 6 no jogo, com Lendário e Corrente Mestra
**Seção do GDD:** 2, 9, 16, 17, 19 · **Situação:** No jogo (não aberto no Unity ainda)

Primeira leva da A-104: o **Mapa 5 · Costa de Coral** (Nv.40, Vara 2 ou melhor) e o **Mapa 6 ·
Arquipélago do Sol** (Nv.50, Vara 3), com as 20 espécies deles, a raridade **Lendário** e a **Vara 3 ·
Corrente Mestra** (Nv.50, 730.000 Moedas + 85 Conchas). Os números vêm de
`docs/propostas/mapas_5_10.json`, aplicados por `tools/Progressao/aplicar_mapas.py`.

- **Lendário** entra agora (o Veleiro é o primeiro). O **Mítico** fica para o Mapa 10, porque os filtros
  da Caixa, do Aquário e do Mercado mostram todas as raridades que existem, e um filtro Mítico sem
  nenhum peixe confundiria.
- A celebração do Lendário diz **"Captura lendária!"**; a do Mítico, quando entrar, **"Captura mítica!"**.
  Os peixes Lendários puxam a linha com a mesma força dos Épicos.
- A Corrente Mestra aparece na Loja e no Perfil como **"Pesca peixes Raros, Épicos e Lendários"**.
- Venda em lote protege Raros, Épicos e Lendários.
- **Cenário:** arte final do proprietário, completa desde 06/10/2026 (o céu da Costa de Coral e as nuvens
  dos dois mapas chegaram com o pacote dos Mapas 7 e 8). As faixas do horizonte vieram com mar pintado embaixo;
  esse mar é retirado no recorte, porque a água é a do jogo.
- **Paisagem viva:** na Costa de Coral, gaivotas, trinta-réis, tartaruga-marinha e, às vezes, golfinho;
  coqueiros na praia e capim de restinga nos cantos. No Arquipélago do Sol, atobás, fragata, golfinhos
  que saltam mais que o boto, de vez em quando gaivotas e tartaruga.
- **Frase de chegada:** "Finalmente cheguei ao mar aberto da costa." (Mapa 5) e "Longe da costa, os
  peixes viram troféus." (Mapa 6).
- **Som:** os dois mapas já têm a lista de gravações (`coral_01..04`, `arquipelago_01..04`); enquanto os
  arquivos não existem, toca o som antigo de mar e vento.

### A-106 · Mapas 7 a 10 no jogo, com Mítico, Atlântico Nobre e Soberana Abissal
**Seção do GDD:** 2, 9, 16, 17, 19 · **Situação:** No jogo (não aberto no Unity ainda)

O resto da A-104 entrou: **Mapa 7 · Corrente Azul** (Nv.60, Vara 3), **Mapa 8 · Banco das Baleias**
(Nv.70, Vara 4), **Mapa 9 · Talude Noturno** (Nv.80, Vara 4) e **Mapa 10 · Abismo Atlântico** (Nv.90,
Vara 5), com as 40 espécies deles (100 no catálogo), a raridade **Mítico** (só o Tubarão-boca-grande, no
Mapa 10) e as varas **Atlântico Nobre** (Nv.70, 3.280.000 Moedas + 180 Conchas) e **Soberana Abissal**
(Nv.90, 15.610.000 Moedas + 650 Conchas). O jogo agora vai do Nv.1 ao Nv.100 sem buraco de conteúdo.

- **Mítico:** chance-base de puxar 8%, cor rosa-framboesa, celebração **"Captura mítica!"**, protegido na
  venda em lote. A Soberana Abissal aparece como **"Pesca peixes Raros, Épicos, Lendários e Míticos"**.
- **Corrente Azul:** não tem terra; as margens são ondas grandes (com a ponta de dentro esfumada) e os
  cantos são sargaço. Paisagem viva: peixes-voadores que saltam e planam, pardelas rente à água, golfinho
  de vez em quando, fragata e sargaço boiando.
- **Banco das Baleias:** amanhecer frio, ilhotas e rochedos distantes. Paisagem viva: a baleia-jubarte
  respira ao longe (dorso e borrifo) e, mais raramente, salta e mostra a cauda; trinta-réis e atobás.
- **Talude Noturno e Abismo Atlântico:** primeiros mapas à noite. Até a arte chegar, o cenário é
  provisório, pintado pelo gerador: céu estrelado com lua, água escura, brilho frio e alguns pontos de
  plâncton. O brilho da água e o halo da lua usam luz fria. Ainda sem animais.
- **Frases de chegada:** "Só o horizonte e o mar azul em volta." (7), "Aqui o oceano mostra o seu
  tamanho." (8), "A noite revela o que vive no fundo." (9) e "O destino final de todo pescador." (10).
- **Ainda provisórios:** céu da Corrente Azul (Corrente 02) e os 4 peixes da Corrente 11 (Peixe-lua,
  Espadarte, Marlim-branco e Marlim-azul), que não vieram no pacote; todo o visual dos Mapas 9 e 10 e a
  Vara 5. Pedidos de novo no documento dos Mapas 9 e 10.

### A-107 · Correções da revisão de segurança (06/10/2026)
**Seção do GDD:** 21, 33, 36, 40 · **Situação:** No jogo (não aberto no Unity ainda)

Correções de brechas que deixavam o jogador ganhar sem jogar. Nenhuma regra de jogo mudou de propósito.

- **Relógio:** voltar o relógio do PC agora só congela o tempo do jogo até a hora real chegar; não abre
  mais pesca offline, Energia ou Expedição de graça (TD-030).
- **Mercado e varas:** os compradores simulados nunca pagam por uma vara mais do que ela custa nova na
  Loja (mais as melhorias já feitas). Uma vara anunciada no Mercado, no Leilão ou esperando em Itens para
  Retirar conta como sua: não dá para comprar outra igual na Loja enquanto isso.
- **Leilão:** um lance maior que o saldo é recusado antes de calcular a taxa.
- **Save editado:** nível de peixe e de vara volta para dentro do máximo, cargas de isca negativas viram
  zero e um barco que não foi comprado não é usado (vale o barco inicial).

### A-108 · Arte final dos Mapas 9 e 10 e da Soberana Abissal
**Seção do GDD:** 9, 16, 19 · **Situação:** No jogo (não aberto no Unity ainda)

Chegou a arte do Talude Noturno, do Abismo Atlântico, da Vara 5 e as duas imagens que faltavam da
Corrente Azul (céu e a grade com o Marlim-azul). Com isso todos os 100 peixes e as 6 varas têm arte final.

- **Céus noturnos:** a pintura continua para os lados com cópias espelhadas dela mesma (as estrelas e a
  via láctea seguem pela tela toda), com a lua apagada nas cópias para existir uma só.
- **Ondas e cantos:** as ondas grandes das margens e as cristas dos cantos somem suavemente do lado de
  dentro, para não aparecer o corte reto da pintura.
- **Paisagem viva:** no Talude Noturno, petréis voando rente ao mar e, de vez em quando, o vulto escuro de
  um peixe grande passando sob a superfície. No Abismo Atlântico, o vulto de um tubarão gigante passa bem fundo,
  mais devagar e mais apagado (Abismo 12, chegou em 07/10/2026).

### A-109 · Do Nv.1 ao Nv.100 em cerca de 10 dias
**Seção do GDD:** 15, 16 · **Situação:** No jogo (não aberto no Unity ainda)

O proprietário pediu (07/10/2026) que o jogador leve uns 10 dias para chegar ao nível máximo, e depois
definiu a referência: **quem deixa o jogo aberto o dia todo chega em ~10 dias; quem joga menos leva mais.**
A simulação (`docs/relatorios/SIMULACAO_PROGRESSAO_NV100.md`) mostrou que, com a tabela antiga, dava para chegar
ao Nv.100 em 3 a 6 dias. A tabela de XP do Pescador foi recalibrada:

- **Níveis 1 a 9 iguais** (Nv.10 em ~2 h online, como antes).
- **Do Nv.10 em diante** cada nível pede mais XP, numa curva suave que cresce com o nível: cada faixa de 10
  níveis demora um pouco mais que a anterior.
- **Resultado:** deixando o jogo aberto o dia todo, ~10 dias; abrindo 4 vezes por dia, ~19 dias; 2 vezes por
  dia, ~20 dias; 1 vez por dia, ~24 dias. (Uma primeira versão, no mesmo dia, mirava 10 dias para quem abre 4
  vezes por dia; foi trocada pela decisão acima.)
- **O XP dos peixes não mudou**: continua subindo de mapa para mapa (um peixe do mapa novo sempre vale mais
  que um do mapa anterior). Isso fecha a OD-047 (opção A) e a OD-049.
- O dinheiro acompanha: a vara de cada mapa fica paga antes ou logo depois de o nível liberar o mapa, mesmo
  vendendo só um quarto dos peixes.

### A-110 · VIP: +50% de XP na pesca offline
**Seção do GDD:** 10, 15 · **Situação:** No jogo (não aberto no Unity ainda)

Pedido do proprietário (07/10/2026): um VIP que custa **100 Dólares do jogo** (100 Dólares = R$ 4,99) e dá
**+50% de XP de Pescador na pesca offline**, para quem quiser pagar. Ele escolheu **30 dias** de duração.

- Comprado na **Loja**, aba **VIP**. Comprar com o VIP ativo soma 30 dias ao fim do atual.
- Vale só para as capturas **offline** (jogo fechado). A pesca com o jogo aberto, as chances, os tamanhos,
  as vendas e as Conchas não mudam. Cada captura offline confere se o VIP valia na hora dela: se o VIP
  acaba no meio da ausência, o bônus conta só até ali.
- A tela de "Bem-vindo de volta" mostra quanto do XP veio do VIP.
- Valores em `config/economy.json` → `vip`.
- **Efeito simulado** (`docs/relatorios/SIMULACAO_PROGRESSAO_NV100.md`): abrindo 4 vezes por dia, o Nv.100 passa
  de ~19 para ~15 dias; 1 vez por dia, de ~24 para ~18 dias. Quem deixa o jogo aberto o dia todo não muda
  (~10 dias), então o VIP nunca passa da meta de 10 dias.
- **Ainda não existe:** comprar Dólares com dinheiro real. Isso precisa de uma loja de verdade (Steam,
  Google Play) e entra quando o jogo for publicado (M22-T17). Jogando, dá para juntar Dólares devagar (A-111).

### A-111 · Dólares jogando: 10 a cada 10 níveis
**Seção do GDD:** 15 · **Situação:** No jogo (não aberto no Unity ainda)

Decisão do proprietário (07/10/2026, OD-021): dá para ganhar Dólares jogando, "bem pouco, dá para juntar,
mas vai demorar". Cada vez que o Pescador chega a um nível múltiplo de 10 (Nv.10, 20, … 100), ganha
**10 Dólares**: 100 Dólares, o preço de um VIP, ao chegar ao Nv.100. Aviso na tela e linha no "Bem-vindo de
volta". Valores em `config/progression.json` → `fisher.dollars_per_levels`. Saves que já passaram desses
níveis não recebem os Dólares de trás.

### A-112 · Ranking da Arena: pódio e páginas até o top 100
**Seção do GDD:** 29 · **Situação:** No jogo (não aberto no Unity ainda)

Pedido do proprietário (07/10/2026). A aba Ranking da Arena mostra:

- **Pódio** com os 3 primeiros na primeira página: 2º à esquerda, 1º no centro (mais alto) e 3º à direita, com
  medalha, nome, e o peixe mais forte do Cardume de cada um.
- **Lista em páginas de 10** (Anterior / Página X de 10 / Próxima), do 4º ao 100º. Nunca mais que o top 100.
- **Minha posição** leva à página do jogador quando ele está no top 100; quem está abaixo vê "Sua posição: #N
  (fora do top 100)".
- Tamanho da página e do top em `config/arena.json` → `ranking`. Arte do pódio e das medalhas pedida
  (`docs/ASSETS_PENDENTES.md`); até chegar, blocos limpos.

### A-113 · Selos de raridade mais limpos
**Seção do GDD:** 6 · **Situação:** No jogo (não aberto no Unity ainda)

O proprietário viu um risco atravessando o nome da raridade (COMUM, RARO, LENDÁRIO…). O selo era uma caixa
de 9 partes mais baixa que as próprias bordas, e o Unity a desenhava com uma linha no meio. Agora o selo é um
retângulo arredondado de verdade: fundo na cor da raridade, contorno e texto um pouco maior. Lendário e Mítico
ganham um brilho suave em volta. Gemas próprias de cada raridade foram pedidas para trocar a estrela.

### A-114 · Expedições crescem com o mapa
**Seção do GDD:** 26 · **Situação:** No jogo (não aberto no Unity ainda)

Decisão do proprietário (07/10/2026, OD-022): a Expedição acompanha o mapa. Na Força Recomendada, ela rende
cerca de **metade** do que o mesmo tempo de pesca offline renderia no mapa de onde o Cardume saiu.

- Os valores de `expeditions.json` passaram a ser os do Mapa 1 (Saída Rápida 300, Volta na Margem 650, Águas
  Profundas 2.250, Viagem Longa 5.000 Moedas; antes 120, 260, 900 e 2.000).
- Cada mapa tem um multiplicador em `maps.json` → `expedition_reward_multiplier`: Mapa 1 ×1, 2 ×8,5, 3 ×13,5,
  4 ×29, 5 ×50, 6 ×113, 7 ×215, 8 ×485, 9 ×880, 10 ×2.000 (calculados pela venda esperada por tentativa).
- Vale o mapa onde o jogador estava ao mandar o Cardume; trocar de mapa durante a Expedição não muda nada.
- A chance de achar um peixe e a eficiência pela Força não mudaram.

Na mesma decisão, o proprietário manteve as batalhas da Arena rápidas (~20 s, como estão).

### A-115 · Loja da Arena: Conchas e Dólares por Honra
**Seção do GDD:** 30 · **Situação:** No jogo (não aberto no Unity ainda)

Decisão do proprietário (07/10/2026, OD-009): a Loja da Arena vende Conchas e Dólares.

- **Saco de Conchas:** 20 Conchas por 100 de Honra, sem limite.
- **Bolsa de Dólares:** 5 Dólares por 300 de Honra, no máximo 2 a cada 7 dias (10 Dólares por semana: mais um
  jeito lento de juntar o VIP).
- Um jogador ativo ganha ~150–250 de Honra por dia. Itens, preços e limites em `config/arena.json` → `shop`.

### A-116 · Defesa perdida tira um pouco de Honra
**Seção do GDD:** 29 · **Situação:** No jogo (não aberto no Unity ainda)

Decisão do proprietário (07/10/2026, OD-024): quando outro jogador ataca e vence, você perde **2 de Honra**
(metade dos 4 que perde atacando), nunca abaixo do saldo mínimo. Defesa vencida continua dando 8. O aviso
de defesa perdida mostra a Honra perdida. Valor em `config/arena.json` → `honor.defense_defeat_loss`.

### A-117 · Caixa de Pesca com limite de 1.500 peixes
**Seção do GDD:** 11 · **Situação:** No jogo (não aberto no Unity ainda)

Decisão do proprietário (07/10/2026, OD-025): a Caixa guarda até **1.500 peixes** (cabe um dia inteiro de pesca
offline, ~1.440). Cheia, a pesca não puxa mais peixes nem gasta isca até o jogador vender ou guardar algum.
Peixes achados em Expedição e retirados do Mercado ainda entram.

Avisos, a pedido dele: na Caixa ("X de 1.500 peixes", com aviso dourado a partir de 90% e quando cheia), no
botão da Caixa e no painel de pesca ("Caixa cheia: venda peixes"), no "Bem-vindo de volta" (quantas tentativas
ficaram sem pescar) e no tutorial (passo de abrir a Caixa). Valor em `config/economy.json` →
`fishing_box.capacity`.

### A-118 · Caixa de Pesca com cara de jogo
**Seção do GDD:** 11 · **Situação:** No jogo (não aberto no Unity ainda)

Pedido do proprietário (07/10/2026, no teste da pesca): os filtros pareciam um gerenciador. Inspirado nas
bolsas de jogos de coleção (abas por categoria com contagem, filtros secundários em menus):

- **Abas de raridade** com bolinha da cor e quantos peixes há de cada (Todos 44 · Comum 40 · Raro 4…); a aba
  ativa ganha fundo e sublinhado na cor da raridade; raridades sem peixe ficam apagadas.
- **Tamanho e Ordem viram dois menus** ("Tamanho: Todos ▾", "Ordem: Mais recentes ▾") à direita das abas;
  em janela estreita, numa linha curta abaixo, com "N peixes neste filtro".
- **Rodapé:** barra de quanto a Caixa está cheia (dourada a partir de 90%) com o aviso do limite, e as vagas do
  Aquário à direita (antes os dois textos se sobrepunham).
- A grade de peixes fica alinhada à esquerda com as abas.

### A-119 · Ordens da Caixa de Pesca
**Seção do GDD:** 11 · **Situação:** No jogo (não aberto no Unity ainda)

O proprietário escolheu o "Exemplo 2" entre três montagens (07/10/2026): um botão com o ícone de ordenar e o
nome da ordem ("Mais valiosos ▾") abre um menu com ícone e uma frase de explicação em cada opção:

- **Mais recentes** (relógio): o último peixe pescado primeiro. É a ordem ao abrir a Caixa.
- **Mais valiosos** (moeda): quem vale mais na venda primeiro.
- **Maiores** (nível): pelo tamanho em relação à espécie (um Lambari grande vem antes de um Pirarucu pequeno).
- **Mais raros** (estrela): Mítico → Comum, e dentro de cada raridade o maior primeiro.
- **Por espécie** (peixe): mesmos peixes juntos, de A a Z; dentro da espécie, o mais valioso primeiro.

Empates sempre caem no mais recente primeiro.

### A-120 · Card de peixe sem textos sobrepostos
**Seção do GDD:** 6, 44 · **Situação:** No jogo (não aberto no Unity ainda)

Pedido do proprietário (07/10/2026): nenhum texto do card pode passar por cima de outro. O card agora é montado
de baixo para cima (Moedas e rodapé, barra, linha do tamanho, nome) e o peixe fica com o espaço que sobra, em
qualquer altura de card. Nome, linha do tamanho e rodapé que não cabem terminam em "…"; as Moedas nunca são
cortadas e o rodapé fica com o que sobra ao lado delas. Se o selo de raridade e o selo "NOVA ESPÉCIE"/"RECORDE"
não cabem juntos, o selo de raridade perde a estrela e, se ainda não couber, o outro selo desce para o canto do
peixe. Na Enciclopédia o selo de raridade foi para a linha do mapa (não fica mais sob o nome) e, nos cards dos
adversários da Arena, o nome do peixe para antes do selo.

### A-121 · Perfil como central do jogador
**Seção do GDD:** 37 · **Situação:** No jogo (não aberto no Unity ainda)

Pedido do proprietário (07/10/2026, no teste do Perfil): o Perfil próprio é a central de informações do jogador,
como um inventário, diferente do perfil que os outros vão ver. Montagem aprovada ("perfil_resumo.png"):

- **Cabeçalho:** retrato com moldura, nome, selo do nível com a barra de XP, "XP x / y · mapa", selo do VIP com a
  data e a carteira à direita (Moedas, Conchas, Dólares e Honra).
- **Aba nova "Resumo"**, a primeira e a que abre: três colunas.
  - **Arena:** posição (#N de N jogadores), Honra, Energia, última batalha; **Cardume:** Força e os 6 peixes com
    nível e borda da raridade, e o bônus de Cardume completo.
  - **Seu equipamento:** vara (com nível), barco e isca (tentativas) com o bônus de cada e o total;
    **Espaço:** Caixa e Aquário com barra, e a Expedição em andamento com o tempo que falta.
  - **Coleção:** espécies descobertas com barra e, a pedido dele, **todas as raridades com os números** (espécies
    achadas / total e peixes pescados de cada); destaques: capturas, excepcionais, perfeições, maior peixe e
    Moedas em vendas.
- As outras abas (Equipamentos, Inventário, Cardume, Enciclopédia, Destaques) não mudaram.

### A-122 · Cancelar a Expedição
**Seção do GDD:** 32 · **Situação:** No jogo (não aberto no Unity ainda)

Pedido do proprietário (07/10/2026): o jogador pode chamar o Cardume de volta antes da hora. O botão "Cancelar
Expedição" fica na faixa da Expedição em andamento e pede confirmação ("O Cardume volta agora e não traz nada").
Cancelada, a Expedição não paga Moedas nem acha peixe, e o Cardume fica livre na hora (formação, alimentar,
Arena). Uma Expedição cujo tempo já acabou é paga normalmente, nunca cancelada.

### A-123 · Ferramentas de teste do proprietário
**Seção do GDD:** 5 · **Situação:** No jogo (não aberto no Unity ainda)

Pedido do proprietário (07/10/2026): comandos de administrador para testar tudo sem esperar. Painel flutuante
"Ferramentas de teste", que só existe no Editor do Unity ou num build de desenvolvimento (nunca no jogo
publicado). Abre com **F2** ou pelo botão "Testes (F2)" no canto de baixo à esquerda; dá para arrastar.

- **Moedas e recursos:** +Moedas, +Conchas, +Dólares e +Honra em três quantidades.
- **Avançar o tempo:** com o jogo aberto (+1 h, +6 h: pesca online a cada 30 s) ou fechado (+1 h, +8 h, +24 h:
  pesca offline e o Bem-vindo de volta). Expedições, Energia e todos os outros tempos andam junto. O tempo
  adiantado fica guardado no save, para o jogo nunca ver o relógio voltar.
- **Nível do Pescador:** −10, −1, +1, +10 e atalhos Nv 10, 30, 50, 70, 90 e 100.
- **Arena:** encher a Energia.
- **Dar peixes:** busca por nome, tamanho aleatório ou fixo (Pequeno … Perfeição) e +1, +5 ou +20 de qualquer
  espécie, direto na Caixa (sem XP; respeita o limite de 1.500).

Tudo passa pelo serviço de jogo (TD-034); a tela nunca mexe no save.

### A-124 · Números grandes em formato curto
**Seção do GDD:** 44 · **Situação:** No jogo (não aberto no Unity ainda)

Aprovado pelo proprietário (07/10/2026). A partir de 1 milhão, os valores aparecem curtos: "12,4 mi", "3,2 bi",
"1,5 tri" (uma casa decimal, nunca arredondada para cima). Vale nas Moedas, Conchas e Dólares do topo da tela e
da carteira do Perfil, no preço dos cards de peixe, nos preços da Loja e no total selecionado da Caixa. Passando o
mouse sobre as moedas do topo ou da carteira aparece o valor completo. Onde o número exato importa (Mercado,
lances, confirmações) continua completo.

### A-125 · Raridade sempre com o nome, não só a cor
**Seção do GDD:** 6 · **Situação:** No jogo (não aberto no Unity ainda)

Aprovado pelo proprietário (07/10/2026), para quem tem dificuldade de distinguir cores. Onde a raridade aparecia
só como cor, agora o nome vai junto: no Cardume do Perfil (selo da raridade em cada posição), no Resumo (ao passar
o mouse num peixe do Cardume: espécie, raridade e nível), nos destaques do Bem-vindo de volta ("Raro · 35,2 cm")
e no aviso de fuga ("Um peixe Lendário escapou!"). Cards, abas e tabelas já tinham o nome.

### A-126 · Enciclopédia por mapa
**Seção do GDD:** 38 · **Situação:** No jogo (não aberto no Unity ainda)

Aprovado pelo proprietário (07/10/2026). A Enciclopédia tem um botão por mapa com o progresso ("Lago Sereno ·
7/10") e "Todos · 23/100". Espécie ainda não descoberta continua silhueta escura com "???", mas mostra o mapa onde
vive e a raridade, para o jogador saber onde procurar. Depois de descoberta, o card mostra também quanto ela
morde no mapa ("morde 3% das vezes", pelo peso de captura, sem os bônus da vara). Recompensa por completar um mapa
não entrou (seria sistema novo).

### A-127 · Avisos importantes ficam guardados
**Seção do GDD:** 39 · **Situação:** No jogo (não aberto no Unity ainda)

Aprovado pelo proprietário (07/10/2026), com o ajuste dele: só os avisos **importantes** (subir de nível,
capturas e acontecimentos importantes) continuam no sino depois de fechar o jogo, os 20 mais recentes. Ficam
nas preferências do jogo neste PC, não no save. "Limpar" apaga também os guardados.

Também aprovado (técnico, invisível ao jogador): capturas raras ou de tamanho especial, batalhas e Expedições
gravam a versão do balanceamento com que saíram (M18-T07).

### A-128 · Ver o perfil do adversário na Arena
**Seção do GDD:** 28 · **Situação:** No jogo (não aberto no Unity ainda)

Aprovado pelo proprietário (07/10/2026). Cada card de adversário tem o botão "Ver perfil", que abre o perfil
público dele: nome, posição e o Cardume em formação (frente e trás), cada peixe como um card com nível e
raridade. Nunca mostra a Força nem prevê o resultado (GDD §28). No jogo local é o adversário simulado; com o
servidor, a pessoa real.

### A-129 · Nome e avatar; Cardume com cara de jogo
**Seção do GDD:** 37 · **Situação:** No jogo (não aberto no Unity ainda)

**Nome e avatar** (aprovado em 07/10/2026): no Perfil, "Editar" ao lado do nome (ou clique no retrato) abre a
escolha do nome (3 a 16 caracteres: letras, números, espaço, ponto, hífen, sublinhado) e de um entre 6 avatares
(`progression.json` → `player_identity`). O avatar aparece no Perfil e no card do jogador na tela. Arte dos
avatares pedida (`docs/ASSETS_PENDENTES.md`); até chegar, selo colorido.

**Cardume** (pedido do proprietário com print, 07/10/2026): a aba virou uma linha de batalha. Painel em tons de
água; no topo a Força em destaque, as 6 posições como bolinhas e o selo do bônus de Cardume completo. As fileiras
FRENTE e TRÁS têm uma etiqueta vertical; cada posição tem o número num círculo (canto esquerdo), o selo de
raridade (canto direito, não sobrepõe mais o número), o peixe grande, nome, nível e uma barra da parte dele na
Força. Posição vazia: moldura suave com "+". A posição escolhida brilha.


### A-130 · Aquário: venda em lote

Pedido do proprietário (07/10/2026). No Aquário, o botão "Selecionar vários" (ao lado da ordenação), ou Ctrl +
clique num peixe, troca a ficha do peixe por um painel de venda em lote: clicar nos cards marca e desmarca, o painel
mostra quantos estão marcados, quanto o jogador recebe e a lista dos marcados (com "×" para tirar um). "Selecionar
todos" marca os que estão visíveis (respeitando a busca). "Vender N" pede confirmação; se houver peixes valiosos ou
do Cardume, eles aparecem listados no aviso. As regras de venda não mudam (o XP investido não volta; durante uma
Expedição, peixes do Cardume não podem ser vendidos). Esc ou "Sair da seleção" volta ao modo normal.

### A-131 · Revisão geral de textos e telas

Pedido do proprietário (07/10/2026). Decisões visíveis tomadas na revisão:
- "Tier" das varas aparece como "Categoria" (sem termos em inglês na tela). Moedas, Conchas e Dólares sempre com
  maiúscula. Mensagens depois de um nome evitam particípio com gênero ("Compra feita: X", "A vara X foi destruída").
- Com uma janela aberta, só o aviso mais recente aparece, no canto de baixo, para não cobrir o "Fechar".
- Com um diálogo aberto dentro de uma janela, a barra de navegação fica desativada até ele fechar.
- O resultado da batalha na Arena é uma faixa abaixo das formações, sem cobrir os peixes.
- Um card selecionado mantém o selo de status (ex.: "Ganhando" no leilão), que desce para cima da arte.
- No editor de nome, a regra de tamanho vem do config (`player_identity`).

### A-132 · Evoluir um peixe: o mesmo padrão para todos

Pedido do proprietário (07/10/2026): o jogo é PvP, então evoluir peixe segue um padrão igual para todos os jogadores,
sem mudar com o mapa. Do Nv. 1 ao 10, um peixe precisa de 3.885 XP (antes 995). Como alimento, toda espécie vale a
mesma base (39 XP); só a raridade (Raro ×2, Épico ×3,5, Lendário ×6, Mítico ×10) e o tamanho mudam o valor. Assim,
cerca de 100 Comuns levam qualquer peixe ao Nv. 10, pescados em qualquer mapa. Peixes que já tinham subido de nível
mantêm o nível.

### A-133 · Arena: adversários do mais fácil ao mais forte

Pedido do proprietário (07/10/2026). Os três adversários aparecem da esquerda para a direita do mais fácil (pior
posição no ranking, número maior) para o mais forte (melhor posição). A ordem se mantém se as posições mudarem.

### A-134 · Arena: Duelo, cabeçalho de jogador e tela VS

Escolha do proprietário entre 3 exemplos (07/10/2026). Cabeçalho: avatar e nome, a posição numa medalha ("#198 de
201"), Energia em segmentos com o tempo do próximo ponto e Honra. Aba Adversários: os 3 adversários em pedestais que
sobem da esquerda (pior posição) para a direita (melhor), mostrando só avatar, nome e posição; o escolhido aparece
com o Cardume dele de frente para o seu (FRENTE/TRÁS, espelhado, nunca a Força) e um "VS" no meio; o botão diz quanto
de Energia o ataque gasta. Antes do replay, uma tela "VS" de cerca de 1,2 s com os dois lados; clique, Espaço ou Esc
pulam. Adversários usam o retrato padrão enquanto não houver arte de avatar para eles.

### A-135 · Inventário estilo mochila

Escolha do proprietário entre 3 exemplos (07/10/2026). O Perfil tem uma aba Inventário só (Equipamentos entrou
nela). À esquerda, EQUIPADO: vara, barco e isca em quadrados grandes, com nome e uma linha curta, e os bônus que o
jogo já informa. À direita, a mochila: cada vara num quadrado com a moldura na cor da categoria, o nível e a marca de
equipada; vagas vazias completam a grade só como visual (não existe limite de espaço). Passar o mouse mostra a
ficha do item; clicar seleciona e mostra as ações de sempre. Barco e isca continuam sendo trocados na Loja.

### A-136 · Barra superior e cartão do jogador

Pedido do proprietário (07/10/2026). A barra de cima ficou cerca de 25% maior, com botões em relevo e o ativo em
turquesa com brilho. O cartão do jogador saiu da esquerda e foi para a direita, e Moedas, Conchas e Dólares
passaram a ficar dentro dele (saíram da barra). Os avisos aparecem abaixo do cartão. Arte definitiva pedida ao
ChatGPT (docs/ASSETS_PENDENTES.md).

### A-137 · Som ambiente com gravações reais

Pedido do proprietário (08/10/2026). O loop sintetizado do mar e do vento saiu. Cada mapa toca uma gravação de
2,5 minutos montada com sons reais de natureza, baixa e calma, combinando com o lugar: pássaros e um riacho no Lago
Sereno; rio e pássaros no Rio Selvagem; sapos, pássaros e mata no Pantanal Dourado; ondas suaves e gaivotas no
Estuário, na Costa de Coral e no Arquipélago; mar aberto e vento na Corrente Azul e no Talude Noturno; mar e cantos
de baleia ao longe no Banco das Baleias e no Abismo Atlântico. O liga/desliga e o volume continuam em Opções.

### A-138 · Abismo Atlântico mais claro

Pedido do proprietário (08/10/2026). O céu não repete mais a Via Láctea em "V" nem tem listras no topo: a Via Láctea
aparece uma vez, perto da lua, e o resto é céu estrelado. Céu, água e ondas ficaram mais claros (continua sendo
noite), com um brilho azul no horizonte e o reflexo da lua na água.

### A-139 · Ícones definitivos do menu

Arte do proprietário (08/10/2026). Os ícones de Pesca, Mapa, Aquário, Arena, Ranking, Expedição, Mercado, Loja,
Perfil, Avisos e Opções são os desenhos dele: formas cheias e arredondadas, brancas, que o jogo pinta conforme o
estado (turquesa no menu ativo). Eles aparecem em todo lugar que usava os ícones antigos com o mesmo nome.

### A-140 · Viagem em 10 segundos

Pedido do proprietário (08/10/2026). Mudar de mapa leva 10 segundos (antes 30). O barquinho cruza a tela no mesmo
tempo, então anda 3 vezes mais rápido; a chegada no mapa novo continua igual.

### A-141 · Tela de Mapa: arquipélago

Escolha do proprietário entre 3 exemplos (08/10/2026, "Exemplo 2 — Arquipélago"). A tela do Mapa deixou de ser uma
lista de cards: é um mar que vai do turquesa (água doce, primeiros mapas) até o azul-noite com estrelas (últimos
mapas). Cada mapa é uma ilha-medalhão redonda, na ordem do jogo (nível de desbloqueio), em zigue-zague, ligadas por
uma rota pontilhada; o trecho já percorrido (até o mapa atual) fica claro e o resto apagado. Cada medalhão mostra a
miniatura do mapa num círculo, o número numa medalha e o nome embaixo (cortado com "…" e o nome completo ao passar o
mouse). Estados: mapa atual com moldura e brilho dourados, o barquinho ao lado e "Você está aqui"; liberado com
moldura turquesa; bloqueado escurecido, com cadeado e "Nv. X". Clicar num medalhão seleciona (anel turquesa) e o
painel de baixo mostra capítulo, nome, a frase do mapa, nível e vara necessários, espécies descobertas e o botão
"Viajar · 10 s", ou o aviso de por que não dá para viajar, ou "Você está aqui". Passar o mouse no lado esquerdo do
painel mostra a descrição do mapa. Durante a viagem o barquinho anda pela rota do mapa de origem até o destino,
acompanhando o progresso da viagem. A tela cabe inteira, sem rolagem. Enquanto a arte própria não chega, o fundo é
um degradê e as ilhas são as miniaturas dos mapas recortadas em círculo (docs/ASSETS_PENDENTES.md). Não há estrelas
por mapa, fases, prêmio por completar mapa nem caminhos alternativos.

### A-142 · Arte definitiva da barra superior e do cartão

Arte do proprietário (08/10/2026). Botões do menu, barra, logo "Fishing Idle", moldura do avatar e as moedas são os
desenhos dele. Detalhes decididos na implementação: no botão ativo (turquesa claro) texto e ícone ficam escuros para
ler bem; a barra passa 10 px das laterais da tela para as pontas arredondadas não ficarem sob o sino e as Opções; a
moeda pintada nova aparece em todo o jogo (preços, Mercado, Expedição), não só no cartão.

### A-143 · Enciclopédia como álbum de cartas

Decisão do proprietário (08/10/2026): "vai ter o álbum de cartas sim, sendo que vai ser a enciclopédia". A aba
Enciclopédia do Perfil virou um álbum, a partir do Exemplo 2 (álbum de cartas) para as páginas e do Exemplo 3
(ficha de herói) para os detalhes. As "cartas" são só a forma de mostrar as espécies da Enciclopédia que já existe:
não há pacotes, raridade de carta, cartas repetidas nem carta como item.

- **Abas de fichário** na lateral esquerda: "Todas" e uma por mapa, cada uma com "descobertas/total" (em dourado
  quando o mapa está completo). Substituem os botões por mapa da A-126.
- **Página de cartas**: título da aba, "x de y espécies" e uma barra de progresso; as cartas numa grade que se ajusta
  ao espaço (8 na janela grande, cerca de 6 numa janela baixa); setas e "Página n de m" embaixo; a rodinha do mouse
  também vira a página. Bolsos vazios tracejados completam a última página.
- **Carta descoberta**: moldura fina na cor da raridade (mais apagada em Comum e Raro), selo da raridade, "Nº" da
  espécie no catálogo, a arte do peixe, nome, mapa, "Recorde" (maior tamanho) e "Pescou" (quantas). Se o recorde é
  de uma categoria especial (Excepcional, Perfeição), a carta leva o selo dessa categoria sobre a arte e uma luz
  dourada discreta passa por ela de vez em quando.
- **Carta não descoberta**: verso escuro com moldura tracejada, silhueta do peixe com "?", "???", a raridade e o mapa
  (onde procurar) e "Ainda não pescada".
- **Ficha** (clique numa carta; ao lado da página na janela grande, por cima dela com "Voltar" numa janela baixa, e
  Esc volta para a página): o peixe grande num pedestal com aura da cor da raridade (bem fraca em Comum e Raro), a
  posição na aba ("3 de 10"), setas ‹ › (e as setas do teclado) para a anterior e a próxima da aba, com a página
  acompanhando; nome, selo de raridade e mapa; quantas pescou, maior exemplar com a categoria de tamanho, tamanho da
  espécie (mínimo a máximo), chance de mordida no mapa, data da descoberta e os atributos base em barras. Não tem
  Alimentar nem Vender.
- **Atributos base**: os do peixe no Nv. 1 com o tamanho do meio da faixa da espécie (a conta de atributos que o
  jogo já usa); as barras vão até o maior valor de todo o catálogo. A categoria do recorde é deduzida do tamanho (o
  save guarda só o tamanho); num limite que o arredondamento deixa ambíguo, vale a categoria menor.
- Antes de descobrir, a ficha mostra "?" nos números, a raridade, "Vive em <mapa>" e "Pesque em <mapa> para
  descobrir esta espécie."
- Moldura da carta, verso, aba do fichário, página, pedestal e aura são desenhos provisórios
  (`docs/ASSETS_PENDENTES.md`).

### A-144 · Aquário vivo

Decisão do proprietário (08/10/2026): o Aquário passa a ser o "Exemplo 1 — Tanque vivo". O álbum de cartas foi para
a Enciclopédia (A-143), então não há alternância de visual: o Aquário é só o tanque. Muda apenas a forma de mostrar;
ficha, Alimentar, Vender, venda em lote (A-130), busca, ordenação e capacidade continuam iguais.

- **O tanque** ocupa a janela abaixo da ordenação: água em degradê com raios de luz suaves vindos de cima, areia com
  pedrinhas no fundo, plantas que já existem no jogo (junco e sargaço) balançando de leve e bolhas subindo.
- **Os peixes nadam** de um lado para o outro e viram na volta. Cada peixe tem um caminho fixo (tirado do número do
  peixe), então nada muda ao reabrir a janela; o relógio só o move ao longo do caminho. O tamanho na tela acompanha o
  tamanho real (em escala logarítmica, entre um mínimo e um máximo), então um peixe grande parece maior que um
  pequeno. Peixes Excepcional/Perfeição têm o brilho dourado lento; os de raridade acima de Comum, um brilho fraco na
  cor da raridade (discreto, como pede a Bíblia).
- **No máximo 24 peixes nadam**: os primeiros da ordenação e da busca atuais (buscar "pacu" enche o tanque de pacus).
  Os outros ficam só na gaveta; a gaveta avisa "Nadando no tanque: 24 de N". Os peixes no tanque são só desenho: não
  soltam moedas e não pedem comida.
- **Clique num peixe** (no tanque ou na gaveta) abre a ficha num painel de vidro à direita, com os mesmos dados e
  botões de antes. O peixe escolhido ganha um anel turquesa, uma etiqueta com nome e nível que nada com ele, e nada
  mais devagar.
- **A gaveta** fica embaixo, sobre a areia: "Todos os peixes (N)" e o botão Recolher / Mostrar todos. Aberta, mostra
  uma fileira de miniaturas redondas (anel na cor da raridade, anel dourado extra em Excepcional/Perfeição, nome,
  "Nv. X" e a posição no Cardume, C1–C6) com rolagem; recolhida, só a barra.
- **Selecionar vários**: a gaveta abre e sobe para até 3 fileiras; o visto turquesa aparece na miniatura e no peixe
  do tanque (nada junto com ele). Ctrl + clique continua começando a seleção, no tanque e na gaveta. O painel de
  venda em lote fica no lugar da ficha, no mesmo vidro.
- **Alimentar** continua com a grade de cartas atual para escolher a comida (é uma etapa à parte e sai do tanque).
- Fundo do tanque, areia, bolha e vidro são desenhos provisórios (`docs/ASSETS_PENDENTES.md`).

### A-145 · Aquário: modo cartas (ficha de herói)

Decisão do proprietário (08/10/2026): o Aquário ganha um botão para mudar entre o "Tanque vivo" (A-144) e o "Modo
cartas", com o visual do "Exemplo 3 — Ficha de herói". Isto substitui a frase do A-144 que dizia que não haveria
alternância de visual. Muda só a forma de mostrar: os dados vêm do serviço e Alimentar, Vender, venda em lote (A-130),
busca, ordenação e capacidade são os mesmos.

- **Alternância** "Tanque | Cartas" no cabeçalho, à esquerda da busca (some enquanto se escolhe a comida). A escolha
  fica guardada neste PC (preferência de tela, fora do save); o padrão é o Tanque. Trocar mantém o peixe escolhido e
  uma seleção de venda em andamento.
- **Lista de cartas** à esquerda, com rolagem, na ordem e na busca atuais: miniatura, nome, "Nv. X · tamanho",
  moldura e faixa na cor da raridade (mais fracas em Comum e Raro), anel na cor do tamanho em Excepcional/Perfeição e
  a posição no Cardume (C1–C6). A carta escolhida ganha o contorno turquesa.
- **Ficha de herói** ao lado: o peixe grande flutuando num pedestal com aura da cor da raridade (bem fraca em Comum e
  Raro; brilho lento na cor do tamanho em Excepcional/Perfeição), a posição na lista ("3 de 24") e setas ‹ › (e ← →
  do teclado) para o anterior e o próximo da lista atual, com a lista rolando junto. À direita: nome, selo de
  raridade, selo de tamanho, tamanho dentro da faixa da espécie, nível num disco com "Nível X de 10" e a barra de XP,
  os atributos Vida/Ataque/Defesa/Velocidade em barras, valor de venda, valor como alimento, data da pesca e posição
  no Cardume, e os botões grandes Alimentar e Vender (os mesmos fluxos e diálogos do tanque).
- **Barras dos atributos**: o número é o do serviço; a barra vai até o maior valor daquele atributo entre os peixes
  do Aquário ("comparado aos seus peixes"). É só a escala do desenho, não uma regra.
- A ficha sempre mostra um peixe da lista: se a busca esconde o peixe escolhido, a ficha passa para o primeiro da
  lista.
- **Selecionar vários**: as cartas da lista são marcadas com o visto; o painel de venda em lote aparece no lugar da
  ficha. Ctrl + clique numa carta também começa a seleção.
- Atributos em barras, não em losango (mais limpo e mais simples de ler em IMGUI); nível em disco + barra, não em anel.
- Pedestal, aura e setas são os mesmos da ficha da Enciclopédia (A-143), agora num desenho compartilhado; continuam
  provisórios (`ui_enc_pedestal.png`, `ui_enc_aura.png` em `docs/ASSETS_PENDENTES.md`). Nenhuma arte nova.
