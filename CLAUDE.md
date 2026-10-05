# Instruções permanentes do projeto Fishing Idle

Este arquivo é lido automaticamente no início de toda sessão de trabalho neste repositório.
As regras abaixo valem sempre, sem precisar ser repetidas.

---

## 1. REGRA OBRIGATÓRIA DE IDIOMA

**O proprietário do projeto é brasileiro e precisa ler todo o conteúdo em Português do Brasil.**

### Deve ser criado em PT-BR

Todo conteúdo visível ao proprietário ou ao jogador:

- painel administrativo e de desenvolvimento
- roadmap
- status de tarefas
- menus
- botões
- títulos
- mensagens
- notificações
- descrições
- textos de interface
- relatórios de progresso
- **todas as respostas no chat com o proprietário**
- mensagens de erro amigáveis
- conteúdo do site público
- conteúdo exibido dentro do jogo

Isso inclui, sem exceção: os nomes de espécies, varas, mapas, expedições, raridades, categorias de
tamanho e moedas; as mensagens que o proprietário vê ao rodar os scripts no terminal; os comentários
dos arquivos que ele abre para editar (`.env.example`, arquivos de `/config`); e toda a documentação
do repositório.

**Não criar interfaces ou painéis em inglês, salvo pedido explícito do proprietário.**

### Pode permanecer em inglês

Apenas o que é técnico e não é apresentado a uma pessoa:

- nomes de classes, variáveis, métodos, arquivos e pastas
- comentários de código
- logs estruturados do servidor
- documentação técnica interna, quando isso for tecnicamente mais adequado
- **chaves técnicas gravadas em dados**, porque não podem mudar quando o idioma muda:
  - status: `TODO`, `IN_PROGRESS`, `DONE`, `BLOCKED`, `NEEDS_OWNER_DECISION`
  - subsistemas: `repo`, `docs`, `config`, `ops`, `server`, `client`, `web`
  - identificadores: `lambari`, `rod_01`, `map_02`, `common`, `exceptional`

Essas chaves são **armazenadas** em inglês e **traduzidas na hora de exibir**.

### Onde ficam os textos da interface

No jogo (Unity), todo texto — do jogo, do Painel de Desenvolvimento do Unity e das mensagens de
validação — fica em um único arquivo:

```
client-unity/Assets/Scripts/Texts/GameTexts.cs
```

Ao adicionar uma tela ou um rótulo, o texto vai para lá — nunca escrito direto no código. Isso
mantém tudo traduzível e evita que uma frase em inglês volte a aparecer por descuido.

### Formatação brasileira

Datas em `DD/MM/AAAA`. Números com vírgula decimal (`10,3%`, `8,5 KB`). Horas em 24h.
As funções ficam em `client-unity/Assets/Scripts/Texts/Format.cs`.

### Exceção registrada

`docs/GDD_V0_1.md` e `docs/CLAUDE_START_HERE_V0_1.md` permanecem no original em inglês, como
documentação técnica interna. São os documentos de design travados, citados como fonte de verdade
pelo resto do projeto; traduzir a fonte de verdade correria o risco de deslocar o sentido de uma
decisão já fechada. Uma versão em PT-BR, se solicitada, deve ser um arquivo **separado** de
tradução, nunca substituindo o original.

---

## 2. As regras são a autoridade, separadas da apresentação

**Apresentação = intenção + tela. Serviço de jogo = verdade + validação + sorteio + persistência.**

No MVP local, o "servidor" é o serviço de jogo em `client-unity/Assets/Scripts/GameService`, que não
pode usar `UnityEngine`. A parte visual (`Assets/Scripts/Game`) nunca calcula uma captura, um
atributo, um preço, um resultado de batalha ou uma recompensa: ela pede ao serviço e mostra a
resposta. Todo sorteio de jogo usa o `Rng` do serviço; o relógio é o `IClock`. No futuro, os mesmos
serviços ganham versões remotas e o servidor vira a autoridade. Detalhes em `docs/BASE_TECNICA.md`
e `docs/SEGURANCA.md`.

## 3. Balanceamento mora em dados

Peixes, mapas, varas, curvas de XP, preços, chances, tempos e taxas ficam em `/config`, nunca dentro
do código do jogo. Detalhes em `config/README.md`.

## 4. O escopo da V0.1 é pequeno de propósito — e a prioridade é o MVP local

A ordem de trabalho é a de `docs/CLAUDE_START_HERE_V0_1_1.md`: primeiro um MVP **local, jogável no
PC do proprietário**, um milestone de cada vez. Não usar Vercel, domínio, hospedagem, banco na
nuvem, Docker obrigatório nem nenhum serviço pago. Se algum passo exigir pagamento, **parar e
explicar antes**: por que é necessário, quanto custa, se há alternativa gratuita e se pode esperar.

Não construir sistemas fora da lista da V0.1 do GDD "porque seriam úteis". Quando algo parecer
realmente faltar, registrar como uma tarefa `NEEDS_OWNER_DECISION` no `docs/roadmap.json` e avisar
o proprietário — nunca inventar a mecânica.

### V0.2 em andamento

Desde 29/09/2026, tudo que o proprietário pedir entra na **V0.2** (milestones a partir do M13 no
`docs/roadmap.json`). **Todos os testes pelo proprietário ficam para o final do jogo** (decisão dele em
05/10/2026): as decisões marcadas com `deferred_to_end: true` no roadmap e as tarefas "Testar no Unity".
Não pedir esses testes nem lembrá-lo deles antes disso; seguir o trabalho sem esperar por eles. As
decisões de design abertas (por exemplo OD-009, Loja da Arena, e OD-021, Dólares) continuam valendo.

**Propostas tiradas do GDD.** Em 05/10/2026 tudo que o GDD pede e o jogo ainda não tem entrou no roadmap
marcado com `from_gdd_review: true` (M11-T10 a T13, M12-T16 a T26, M18, M19, M20).

**Mapas 5 a 10 aprovados.** O M21 (progressão até o Nv.100) foi aprovado pelo proprietário em 05/10/2026.
A fonte de verdade é `docs/PROGRESSAO_MAPAS_5_A_10_ADAPTADA.md` (o original dele, adaptado às regras atuais),
com os números em `docs/propostas/mapas_5_10.json`, gerados por `tools/Progressao/adaptar_mapas_5_10.py`.
Mapas, espécies, nomes, níveis e raridades não mudam; números se ajustam com simulação. São propostas para o
proprietário analisar: **não começar nenhuma sem ele aprovar**, e não mudar o que já foi feito e aprovado
para "seguir o GDD" — o que o jogo já faz diferente do GDD (registrado no `GDD_ADENDO.md`) vale. Continua
valendo: não inventar sistemas; fazer o que foi pedido e registrar o resto como decisão pendente.

### Visual: a Bíblia de Arte

`docs/ART_BIBLE_V0_1.md` ("Lago Dourado — Clean Premium") é a **fonte de verdade visual**, travada
como o GDD: não se edita; um detalhe visual decidido na implementação vai para o `docs/GDD_ADENDO.md`.
Uma referência visual nunca cria sistema, moeda ou raridade nova. Arte que ainda não existe vira um
placeholder limpo, marcado `ASSET_PENDENTE` na descrição da tarefa. Em dúvida entre mais efeito e
mais limpo, usar mais limpo.

## 5. O painel reflete o repositório

`docs/roadmap.json` é a fonte de verdade do progresso. Uma tarefa vira `DONE` **na mesma mudança que
conclui o trabalho**, com uma nota em `completion_notes` dizendo o que foi entregue. Nunca declarar
progresso separadamente do código que o produziu.

O proprietário vê esse arquivo no Painel de Desenvolvimento dentro do Unity (menu Fishing Idle →
Painel de Desenvolvimento) — então **todo commit enviado aparece para o proprietário**. Nunca existe um indicador de "trabalho em
segundo plano": o painel reflete commits, nunca atividade (seção 5 do GDD).

## 6. Onde registrar o que for criado

Todo o projeto fica organizado como o de um jogo profissional. Cada mudança atualiza, **na mesma
mudança**, o documento certo:

| O que mudou | Onde registrar |
|---|---|
| Um detalhe de design (o que o jogador vê, sente ou pode fazer) decidido na implementação | `docs/GDD_ADENDO.md` — nunca editar o GDD travado |
| Arquitetura, pastas, fluxos, como adicionar um sistema | `docs/BASE_TECNICA.md` |
| Uma escolha técnica e o porquê | `docs/DECISOES.md` (nova TD) |
| Progresso | `docs/roadmap.json` |
| O que entrou numa versão | `docs/CHANGELOG.md` |

## 7. Antes de versionar

```bash
./ops/scripts/verify.sh
```

Precisa do SDK do .NET 8+. Roda os testes das regras do jogo e compila o código do Unity (jogo e
Painel de Desenvolvimento) contra as bibliotecas de referência do Unity, sem precisar do Editor.
Precisa passar. As partes arquivadas só entram com `--completo`.

Compilar não é rodar: o que depende do Editor do Unity (visual, jogabilidade) só é declarado
verificado depois que o proprietário apertar Play. Diga isso com clareza no relatório.

## 8. Partes arquivadas: não gastar tokens com elas

A infraestrutura online (Milestone 12) fica guardada no repositório, mas **não é lida, buscada,
testada nem alterada**, a não ser que o proprietário peça explicitamente:

- `server/`, `web/`, `shared-contracts/`
- `ops/docker-compose.yml` e `ops/scripts/dev-up.sh`, `dev-down.sh`, `migrate.sh`, `new-migration.sh`
- `client-unity/Assets/Scripts/Core/` e `Diagnostics/` (cliente HTTP dormente)
- `docs/API.md` e `docs/INFRA_ONLINE.md`

O arquivo `.ignore` na raiz faz as buscas pularem essas pastas. Nada de ler esses arquivos "para
entender o contexto": tudo que o MVP precisa está em `client-unity/`, `config/`, `docs/` e `tools/`.

---

## Referências

| Assunto | Arquivo |
|---|---|
| Prioridade atual (MVP local) | `docs/CLAUDE_START_HERE_V0_1_1.md` |
| Design do jogo (fonte de verdade) | `docs/GDD_V0_1.md` |
| Visual do jogo (fonte de verdade) | `docs/ART_BIBLE_V0_1.md` |
| Detalhes de design decididos na implementação | `docs/GDD_ADENDO.md` |
| Como o código funciona | `docs/BASE_TECNICA.md` |
| Decisões técnicas e princípios travados | `docs/DECISOES.md` |
| Progresso | `docs/roadmap.json` e `docs/ROADMAP.md` |
| Segurança e anti-trapaça | `docs/SEGURANCA.md` |
| Versionamento | `docs/VERSIONAMENTO.md` |
| Balanceamento | `config/README.md` |
| Infraestrutura online (adiada) | `docs/INFRA_ONLINE.md` |
