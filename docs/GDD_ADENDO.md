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

Menu Mapa → Viajar. A viagem leva 30 segundos (`maps.json → travel`). Ao partir, o que já estava
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
