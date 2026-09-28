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
**Seção do GDD:** 11, 12 · **Situação:** Em vigor

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
