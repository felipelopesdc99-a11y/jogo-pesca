# client-unity — o jogo

Unity 6.3 LTS / C#. No MVP local, **o jogo inteiro roda aqui**: regras, save, cena, interface e o
Painel de Desenvolvimento. Não precisa de servidor nem de internet.

## Abrir e jogar

1. Instale o **Unity 6.3 LTS** pelo Unity Hub.
2. No Unity Hub: **Add → Add project from disk** → escolha esta pasta (`client-unity`).
3. Abra. Se perguntar sobre atualizar para a sua versão instalada, aceite.
4. Na primeira abertura, o projeto se configura sozinho: cria e abre a cena
   `Assets/Scenes/Principal.unity`, e define nome do produto, janela redimensionável e "rodar em
   segundo plano" (para a pesca continuar com a janela sem foco).
5. Aperte **Play** e clique em **Iniciar pesca**.

Depois da primeira abertura, versione os arquivos que o Unity gerou: `ProjectSettings/`, os arquivos
`.meta` e `Assets/Scenes/`. As pastas `Library/`, `Temp/`, `Logs/` e `UserSettings/` ficam de fora
(`.gitignore`).

## Menu Fishing Idle

| Item | Faz |
|---|---|
| **Painel de Desenvolvimento** | Visão geral, roadmap, balanceamento e save |
| **Abrir cena principal** | Abre `Assets/Scenes/Principal.unity` |
| **Abrir pasta do save** | Mostra onde o save está gravado |

## Como o código está organizado

| Pasta | Assembly | Papel |
|---|---|---|
| `Assets/Scripts/Texts` | `FishingIdle.Texts` | Todos os textos PT-BR e a formatação brasileira |
| `Assets/Scripts/GameService` | `FishingIdle.GameService` | As regras (o "servidor local"). Não pode usar o Unity |
| `Assets/Scripts/Game` | `FishingIdle.Game` | Cena, animação e interface |
| `Assets/Scripts/Editor` | `FishingIdle.Editor` | Painel de Desenvolvimento, configuração do projeto, passo de build |
| `Assets/Scripts/Core`, `Diagnostics` | `FishingIdle.Client` | Dormente: acesso HTTP ao servidor online (Milestone 12) |

Detalhes completos em `docs/BASE_TECNICA.md`.

## Regras para o código deste projeto

1. A parte visual nunca calcula uma captura, um atributo, um preço, o resultado de uma batalha ou
   uma recompensa. Ela pede ao serviço de jogo e mostra a resposta.
2. Todo sorteio de jogo usa o `Rng` do serviço; o tempo vem do `IClock`. `UnityEngine.Random` e
   `Time` só para efeitos visuais sem valor de jogo (nuvens, pássaros, ondas).
3. Nenhum número de balanceamento no código: tudo em `/config`.
4. **Todo texto que uma pessoa lê está em `Texts/GameTexts.cs`, em PT-BR.** Comentários de código
   ficam em inglês.
5. Antes de versionar: `./ops/scripts/verify.sh` na raiz do repositório.
