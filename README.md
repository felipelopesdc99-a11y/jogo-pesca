# Fishing Idle

Um jogo idle 2.5D relaxante de pesca e coleção, feito em **Unity 6.3 LTS + C#**.

**Situação atual: MVP local completo (Milestones 0 a 11) e V0.2 em andamento — sons novos (M13) e o visual "Lago Dourado" (M14).** O jogo roda inteiro no seu PC, sem
servidor, sem internet e sem nenhum serviço pago. Estão no jogo: pesca online e offline, Caixa de
Pesca, Aquário, alimentação e níveis dos peixes, Cardume, Perfil e Enciclopédia, dois mapas e viagem,
varas com melhorias, Expedições, Arena, Mercado, Leilão e um tutorial curto. Os números do
balanceamento ainda são provisórios (veja `docs/relatorios/SIMULACAO_BALANCEAMENTO.md`).

O progresso do projeto está em **`docs/roadmap.json`** e aparece no Painel de Desenvolvimento,
dentro do próprio Unity.

---

## Como jogar (passo a passo)

Você só precisa do **Unity**. Nada mais.

1. Instale o **Unity Hub**: <https://unity.com/download>.
2. No Unity Hub, aba **Installs**, instale o **Unity 6.3 LTS** (qualquer versão 6000.3.x).
3. Na aba **Projects**, clique em **Add → Add project from disk** e escolha a pasta **`client-unity`**
   deste repositório.
4. Abra o projeto. Se o Unity perguntar se quer atualizar para a versão instalada, responda que sim.
   A primeira abertura demora alguns minutos (o Unity está preparando o projeto).
5. Aperte **Play** (▶, no topo da tela).
6. Siga o tutorial: ele pede para pegar a **Vara Inicial** (grátis, na **Loja**) e depois clicar em
   **Iniciar pesca**. Dá para pular o tutorial a qualquer momento.

A cada 30 segundos sai um peixe. Clique em **Caixa de Pesca** (canto inferior direito) para ver os
peixes, vender ou guardar no Aquário. Os outros menus ficam no alto da tela.

> Se aparecer algum erro vermelho no **Console** do Unity, copie a mensagem e mande no chat.

## Painel de Desenvolvimento

No menu do Unity: **Fishing Idle → Painel de Desenvolvimento**.

| Aba | O que tem |
|---|---|
| **Visão geral** | Milestone atual, tarefas concluídas e pendentes, próximo passo, bloqueios, versão do jogo e do balanceamento |
| **Roadmap** | Todas as tarefas por milestone, com filtro por status |
| **Balanceamento** | Tempos de pesca, chances, tamanhos, preços, XP, varas, economia, arena, expedições — edite e salve sem mexer em código |
| **Save** | Onde o save está, um resumo dele, e o botão para apagar e começar do zero |

**Dica para testar mais rápido:** em *Balanceamento → Pesca*, mude o tempo de pesca online para
5 segundos e clique em **Salvar e aplicar no jogo** com o jogo rodando. Lembre de voltar para 30
depois (ou clique em *Descartar alterações* antes de salvar).

---

## Onde está cada coisa

| Se você quer… | Veja |
|---|---|
| Ver o que está pronto e o que vem a seguir | Painel de Desenvolvimento, ou `docs/ROADMAP.md` |
| Entender o design do jogo | `docs/GDD_V0_1.md` (fonte de verdade, original em inglês) e `docs/GDD_ADENDO.md` (detalhes definidos durante a implementação) |
| Ver como o jogo deve parecer (cores, fontes, cards, cenário, celebrações) | `docs/ART_BIBLE_V0_1.md` |
| Entender como o código funciona (para desenvolvedores) | **`docs/BASE_TECNICA.md`** |
| Saber por que algo foi feito de determinado jeito | `docs/DECISOES.md` |
| O que mudou em cada versão | `docs/CHANGELOG.md` |
| Mudar um número de balanceamento à mão | `config/README.md` |
| Trocar uma imagem do jogo (peixe, cenário, ícone, vara) | `docs/ASSETS_PENDENTES.md` — coloque o novo arquivo por cima, com o mesmo nome |
| Mudar cores, brilho e tempos dos efeitos | `client-unity/Assets/Resources/Visual/tema_visual.json` |
| Trocar um som do jogo | Coloque o novo arquivo em `client-unity/Assets/Resources/Sons` com o mesmo nome |
| A prioridade atual do projeto | `docs/CLAUDE_START_HERE_V0_1_1.md` |

---

## Estrutura do repositório

```
jogo-pesca/
├── client-unity/        O jogo (Unity 6.3 LTS). É aqui que tudo acontece no MVP.
│   └── Assets/Scripts/
│       ├── Texts/         Todos os textos em PT-BR, num lugar só
│       ├── GameService/   Regras do jogo: sorteio, XP, preços, save (o "servidor local")
│       ├── Game/          Cena, animação e interface
│       └── Editor/        Painel de Desenvolvimento e configuração automática do projeto
├── config/              Todo o balanceamento, em JSON. Nunca dentro do código.
├── docs/                GDD, adendo, base técnica, decisões, roadmap, histórico.
├── tools/               Testes das regras e checagens do código do Unity, sem precisar do Unity.
├── ops/scripts/         verify.sh, simular.sh (relatório de balanceamento) e os scripts da infraestrutura online (adiada).
├── server/  web/  shared-contracts/
│                        Infraestrutura online já construída e guardada para depois (Milestone 12).
└── version.json         Versão de cada parte do projeto.
```

---

## Para desenvolvedores

Antes de versionar qualquer mudança:

```bash
./ops/scripts/verify.sh
```

Precisa do SDK do .NET 8 ou mais novo. Roda os testes das regras do jogo e confere se o código do
Unity compila, sem precisar do Editor. Detalhes em `docs/BASE_TECNICA.md`.

### A infraestrutura online (adiada)

O servidor ASP.NET Core, o PostgreSQL, o Docker Compose e o painel web em Next.js continuam no
repositório e funcionando, mas **não são necessários para jogar**. Pela prioridade atual, voltam a
ser usados depois que o MVP local for validado. Como subir cada parte, se precisar:
`docs/INFRA_ONLINE.md`.

---

## Como este projeto é construído

1. **As regras decidem; a tela mostra.** Captura, tamanho, XP, preço e save são calculados pelo
   serviço de jogo (`GameService`), separado da parte visual. Hoje ele roda no seu PC; no futuro,
   a mesma interface roda num servidor, sem reescrever o jogo.
2. **O balanceamento mora em dados.** Tudo em `/config`, editável pelo Painel de Desenvolvimento.
3. **O escopo é pequeno de propósito.** Um milestone de cada vez, na ordem do roadmap.
4. **O painel reflete o repositório.** Uma tarefa vira concluída na mesma mudança que termina o
   trabalho.
5. **Tudo que uma pessoa lê está em PT-BR.** Regra obrigatória, em `CLAUDE.md`.
