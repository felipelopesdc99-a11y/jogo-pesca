# Fishing Idle

Um jogo idle 2.5D relaxante de pesca e coleção, feito em **Unity 6.3 LTS + C#**.

**Situação atual: V0.2 em andamento, no novo escopo idle (Milestone 24).** O jogo roda inteiro no seu PC, sem
servidor, sem internet e sem nenhum serviço pago. Estão no jogo:

- **Pesca** online, uma tentativa a cada **10 segundos**, e offline (com o jogo fechado), uma a cada 20 segundos (15 com
  o VIP), até 24 horas; Caixa de Pesca para 1 dia de pesca.
- **10 mapas** e 100 espécies, do Comum ao Mítico; o **Pescador vai até o Nv.1000**, com números grandes ("3,4 mi",
  "12 bi").
- **Tripulação** automática (pescadores e barcos que rendem Moedas e XP sozinhos) e **Melhorias** compradas que
  multiplicam a renda.
- Varas, barcos e peixes até o **Nv.100**; **5 barcos**, as varas e as **5 roupas** são montados com **fragmentos**
  que caem pescando; três iscas com 999 tentativas.
- **Passe do Pescador** (marcos a cada 10 níveis, faixa Grátis e Premium) e o **Boss Mundial**, todo dia às 19:00, no
  menu **Eventos**.
- Aquário, Cardume, Perfil, Enciclopédia, Expedições, Arena, Mercado, Leilão, Loja e um tutorial curto.

Os números do balanceamento são de partida e são ajustados jogando (`config/README.md`).

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
6. Siga o tutorial: ele pede para pegar a primeira vara (grátis, na aba **Comércio → Loja**) e depois clicar em
   **Iniciar pesca**. Dá para pular o tutorial a qualquer momento.

A cada 10 segundos o pescador tenta pegar um peixe. Clique em **Caixa de Pesca** (canto inferior direito) para ver os
peixes, vender ou guardar no Aquário.

- **Menu de cima:** à esquerda, o seu cartão (clique para abrir o Perfil); no centro, **5 abas**: **Pesca**,
  **Tripulação**, **Aventura** (Expedição, Arena e Eventos), **Comércio** (Mercado e Loja) e **Perfil**; à direita, os
  avisos e as opções. Embaixo das abas fica a placa com a renda da Tripulação.
- **Atalhos redondos** na lateral direita: **Mapa**, **Aquário**, **Passe**, **Eventos** e **Ranking**. Durante o Boss
  Mundial, o atalho Eventos fica dourado e abre direto a luta.
- As janelas abrem no estilo **"Diário de bordo"** (papel numa moldura de madeira). O **"i"** ao lado do título explica
  como cada uma funciona; o lacre vermelho ou a tecla Esc fecham.

> Se aparecer algum erro vermelho no **Console** do Unity, copie a mensagem e mande no chat.

## Se o Unity não deixar dar Play

**"Internal build system error" com "Uma política de Controle de Aplicativo bloqueou este arquivo" (Bee.DotNet.dll).**
Não é erro do jogo: o Windows bloqueou um arquivo do próprio Unity, e sem ele o Unity não compila os scripts (aparece o
aviso de Safe Mode e o Play não faz nada). Em ordem:

1. Reinicie o computador e abra o projeto de novo.
2. No Unity Hub, em **Installs**, desinstale e instale de novo a versão do Unity (ou uma versão 6.3 mais nova).
3. Se voltar a acontecer: **Segurança do Windows → Controle de aplicativos e do navegador → Controle Inteligente de
   Aplicativos** e desative. Atenção: depois de desligado, o Windows normalmente não deixa religar sem reinstalar o
   sistema; o antivírus continua funcionando.

Se o jogo abrir a tela **"O jogo encontrou um erro inesperado"**, tire um print dela e mande no chat: ela já mostra o
erro, o momento e onde aconteceu.

Qualquer outro erro: abra a **Console** (Window → General → Console), clique na primeira linha vermelha e mande o
print.

## Painel de Desenvolvimento

No menu do Unity: **Fishing Idle → Painel de Desenvolvimento**.

| Aba | O que tem |
|---|---|
| **Visão geral** | Milestone atual, tarefas concluídas e pendentes, próximo passo, bloqueios, versão do jogo e do balanceamento |
| **Roadmap** | Todas as tarefas por milestone, com filtro por status |
| **Balanceamento** | Tempos de pesca, chances, tamanhos, preços, XP, varas, economia, arena, expedições — edite e salve sem mexer em código |
| **Save** | Onde o save está, um resumo dele, e o botão para apagar e começar do zero |

**Dica para testar mais rápido:** em *Balanceamento → Pesca*, mude o tempo de pesca online para
5 segundos e clique em **Salvar e aplicar no jogo** com o jogo rodando. Lembre de voltar para 10
depois (ou clique em *Descartar alterações* antes de salvar). Com o jogo rodando no Editor, a tecla **F2** abre as
**Ferramentas de teste** (Moedas, Conchas, Dólares, avançar o tempo, atalhos de nível como "Nv. 1000").

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

O GitHub roda a mesma verificação a cada envio (`.github/workflows/verificar.yml`, `docs/DECISOES.md` TD-044) e grava
o resultado na branch `ci-resultados` (`resultado.txt`, `verify.log` e `commit.txt`). Compilar não é rodar: o que
depende do Editor do Unity só fica verificado depois de alguém apertar Play.

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
