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

Todo o texto do Painel de Desenvolvimento está em um único arquivo:

```
web/dev-console/src/lib/strings.ts
```

Ao adicionar uma tela ou um rótulo, o texto vai para lá — nunca escrito direto no JSX. Isso mantém
o painel traduzível por inteiro e evita que uma frase em inglês volte a aparecer por descuido.

O jogo (Unity) ainda não tem textos de interface. Quando tiver, seguir o mesmo princípio: um ponto
central, nunca texto solto no código.

### Formatação brasileira

Datas em `DD/MM/AAAA`. Números com vírgula decimal (`10,3%`, `8,5 KB`). Horas em 24h.
As funções já existem em `strings.ts`: `formatDate`, `formatDateTime`, `formatNumber`, `formatUptime`.

### Exceção registrada

`docs/GDD_V0_1.md` e `docs/CLAUDE_START_HERE_V0_1.md` permanecem no original em inglês, como
documentação técnica interna. São os documentos de design travados, citados como fonte de verdade
pelo resto do projeto; traduzir a fonte de verdade correria o risco de deslocar o sentido de uma
decisão já fechada. Uma versão em PT-BR, se solicitada, deve ser um arquivo **separado** de
tradução, nunca substituindo o original.

---

## 2. O servidor é a autoridade

**Cliente = intenção + apresentação. Servidor = verdade + validação + sorteio + persistência.**

Nunca calcular no cliente uma captura, um atributo, um preço, um resultado de batalha ou uma
recompensa. Nunca confiar no relógio local para nada que importe. Detalhes em `docs/SEGURANCA.md`.

## 3. Balanceamento mora em dados

Peixes, mapas, varas, curvas de XP, preços, chances, tempos e taxas ficam em `/config`, nunca dentro
do código do jogo. Detalhes em `config/README.md`.

## 4. O escopo da V0.1 é pequeno de propósito

Não construir sistemas fora da lista da V0.1 do GDD "porque seriam úteis". Quando algo parecer
realmente faltar, registrar como uma tarefa `NEEDS_OWNER_DECISION` no `docs/roadmap.json` e avisar
o proprietário — nunca inventar a mecânica.

## 5. O painel reflete o repositório

`docs/roadmap.json` é a fonte de verdade do progresso. Uma tarefa vira `DONE` **na mesma mudança que
conclui o trabalho**, com uma nota em `completion_notes` dizendo o que foi entregue. Nunca declarar
progresso separadamente do código que o produziu.

## 6. Antes de versionar

```bash
./ops/scripts/verify.sh
```

Compila e testa o servidor, faz typecheck e build do painel. Precisa passar.

---

## Referências

| Assunto | Arquivo |
|---|---|
| Design do jogo (fonte de verdade) | `docs/GDD_V0_1.md` |
| Decisões técnicas e princípios travados | `docs/DECISOES.md` |
| Progresso | `docs/roadmap.json` e `docs/ROADMAP.md` |
| API | `docs/API.md` |
| Segurança e anti-trapaça | `docs/SEGURANCA.md` |
| Versionamento | `docs/VERSIONAMENTO.md` |
| Balanceamento | `config/README.md` |
