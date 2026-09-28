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

Até o Milestone 5, fechar o jogo simplesmente pausa a pesca.

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
