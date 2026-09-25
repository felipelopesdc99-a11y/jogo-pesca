# client-unity — esqueleto do cliente Unity

Unity 6.3 LTS / C#. O cliente é **apenas apresentação e intenção do jogador**. O servidor é dono de
toda regra, de todo sorteio e de todo valor com sentido econômico, de progressão ou competitivo
(seção 41 do GDD).

## O que existe no Milestone 0

| Peça | Arquivo | Faz |
|---|---|---|
| Inicialização | `Assets/Scripts/Core/ClientBootstrap.cs` | Instala os serviços persistentes ao iniciar, por código, para que dar Play em qualquer cena funcione |
| Configuração do servidor | `Assets/Scripts/Core/BackendSettings.cs` | Guarda o endereço do servidor e os tempos limite fora do código |
| Cliente de API | `Assets/Scripts/Core/ApiClient.cs` | Acesso mínimo via GET sobre `UnityWebRequest` |
| Verificador de saúde | `Assets/Scripts/Diagnostics/ServerHealthProbe.cs` | Consulta `/health` e acompanha o estado da conexão |
| Overlay de diagnóstico | `Assets/Scripts/Diagnostics/DiagnosticOverlay.cs` | Painel só de desenvolvimento com o estado da conexão (F1 alterna) |

**Ainda não existe jogabilidade nem cena montada.** A primeira cena de verdade — Mapa 1, Lago
Sereno — é a tarefa `M2-T01` do roadmap.

## Abrindo o projeto

1. Instale o **Unity 6.3 LTS** pelo Unity Hub.
2. Adicione esta pasta (`client-unity`) como um projeto.
3. Suba o servidor antes: `ops/scripts/dev-up.sh`, a partir da raiz do repositório.
4. Aperte **Play**. O overlay no canto superior esquerdo mostra um destes estados:
   - **conectado** — o servidor respondeu e o banco dele está saudável;
   - **degradado** — o servidor respondeu, mas *ele* não alcança o PostgreSQL;
   - **inacessível** — nada respondeu no endereço configurado.

Aperte **F1** para esconder ou mostrar o overlay. Ele é removido dos builds de lançamento.

### Observações da primeira abertura

- O arquivo `ProjectSettings/ProjectVersion.txt` fixa `6000.3.0f1` como valor provisório para o
  Unity 6.3 LTS. O Unity Hub vai oferecer atualizar o projeto para a sua versão instalada; aceite e
  versione o arquivo atualizado. Isso está registrado como a tarefa `M0-T14`.
- Só o `ProjectVersion.txt` é versionado dentro de `ProjectSettings/`. O Unity gera o resto com
  valores padrão na primeira abertura. Defina **Nome do Produto** e **Nome da Empresa** em
  *Edit > Project Settings > Player* uma vez, e então versione o que foi gerado.
- O pipeline de renderização de propósito **ainda não foi escolhido**. O Universal RP é a escolha
  esperada para a apresentação 2.5D, e ele é selecionado junto com a construção da primeira cena
  (`M2-T01`), para que o asset do pipeline e a cena sejam configurados juntos em vez de ficarem meio
  prontos agora.

## Apontando o cliente para outro servidor

Ou defina a variável de ambiente `FISHING_IDLE_API_BASE_URL` (ela vence sobre tudo), ou crie o
arquivo `Assets/Resources/BackendSettings.asset` por *Assets > Create > Fishing Idle > Backend
Settings*. Veja `Assets/Resources/README.md`.

## Regras para o código deste projeto

1. Nunca calcule aqui uma captura, um atributo, um preço, o resultado de uma batalha ou uma
   recompensa. Pergunte ao servidor.
2. Nunca confie no relógio local para nada que importe. O carimbo de tempo do servidor é a verdade.
3. Nunca leia um número de balanceamento a partir do código. O balanceamento mora em `/config` e
   chega ao cliente através do servidor.
4. Aleatoriedade puramente visual (movimento da água, pássaros, nuvens) não precisa de autoridade do
   servidor e não deve pedir nenhuma.
5. **Todo texto que o jogador lê é PT-BR.** Comentários de código ficam em inglês. Veja
   `docs/DECISOES.md`, TD-014.
