# Segurança

## A regra central

> **CLIENTE = intenção + apresentação**
> **SERVIDOR = verdade + validação + sorteio + persistência**

Parta do princípio de que existem clientes modificados e bots. Toda ação com valor econômico, de
progressão ou competitivo é decidida pelo servidor.

## Nunca confiar no cliente para

Hora atual · conclusão de ciclo de pesca · qual peixe foi capturado · tamanho ou raridade do peixe ·
XP · saldos de Moedas, Conchas, Honra ou Energia · nível da vara · nível do peixe · posse no
mercado · resultado de lance · resultado de batalha · conclusão ou recompensa de expedição.

O cliente pode calcular livremente **apenas para apresentação**: tempo de animação, movimento da
água, pássaros, nuvens, avisos de captura gerados a partir de uma resposta autoritativa. Nada disso
pode voltar como entrada de um resultado.

## Proteções obrigatórias

| Proteção | Onde se aplica |
|---|---|
| Requisições autenticadas | Todo endpoint de jogo e de administração |
| Verificação de posse | Toda requisição que cita um peixe, vara, anúncio, leilão ou Cardume |
| Carimbos de tempo do servidor | Ciclos de pesca, Energia, acúmulo offline, expedições, expiração de leilão |
| Limite de requisições | Por conta, em todos os endpoints |
| Chaves de idempotência | Toda mutação que move moeda, XP, peixes, anúncios, lances ou recompensas |
| Transações de banco | Toda mutação que toca em mais de uma linha |
| Travas de linha e versão | Mudanças de estado de peixe, lances, trocas de posição, compras no mercado |
| Proteção contra repetição e gasto duplicado | Toda mutação econômica |
| Sorteio no servidor | Geração de captura, tamanho, variação de dano, peixe encontrado em expedição |
| Registros imutáveis de auditoria | Operações de economia, mercado e administração |
| Separação de papéis administrativos | Todo endpoint `/api/dev` e de administração |
| Publicação validada de configuração | Toda mudança de balanceamento |

## Exemplos concretos

**Pesca acelerada.** Um cliente modificado pede 500 ciclos de uma vez. O servidor calcula quantos
ciclos são elegíveis a partir do carimbo de tempo autoritativo do início da sessão e do cursor de
ciclo gravado, e processa apenas os ciclos que realmente passaram. Requisições a mais devolvem o
mesmo estado.

**Uso duplo do peixe.** Duas requisições tentam vender e alimentar o mesmo `FishInstance` no mesmo
instante. As duas rodam em transação contra a versão de estado do peixe; uma confirma e a outra é
rejeitada com um conflito claro.

**Corrida de lances.** Dois lances chegam juntos. A linha do leilão é travada, os lances são
ordenados, um vira o maior lance e o outro é rejeitado ou devolvido. A taxa de 1% é cobrada por
lance aceito e nunca é devolvida; o valor principal fica reservado enquanto aquele participante
lidera e é liberado imediatamente quando ele é superado.

**Raro com a vara errada.** Um cliente afirma ter capturado um Raro no Mapa 1, ou no Mapa 2 usando a
Vara Inicial. A elegibilidade de raridade é o cruzamento entre `available_rarities` do mapa e
`can_catch_rarities` da vara equipada, ambos lidos no servidor a partir da configuração. A afirmação
do cliente nunca é uma entrada.

**Vazamento de dado privado.** Um jogador pede o perfil de outro. A projeção pública é montada no
servidor e não tem como incluir Força do Cardume, saldos de Moedas ou Conchas, Caixa de Pesca,
Inventário ou Aquário. A omissão é garantida pela projeção, não pela interface escolher não mostrar.

## Situação atual do código

O Milestone 0 não tem endpoints de jogo, então ainda não existe nada que possa ser explorado por
valor. O que vale dizer com clareza:

### Ponto em aberto — os endpoints do painel não têm autenticação

`/api/dev/roadmap`, `/api/dev/version` e `/api/dev/config` não têm autenticação no Milestone 0.

- **Impacto se exposto.** Acesso de leitura ao status do projeto, às informações de versão e aos
  dados de balanceamento. Não existe caminho de escrita, não existe dado de jogador, e nenhuma
  credencial é servida: o endpoint de configuração aceita apenas um nome simples de arquivo `.json`
  dentro de `/config`, e tanto `version.json` quanto `appsettings.json` são recusados por ele.
- **Contenção.** O ambiente Compose publica as portas apenas em localhost. O CORS permite somente as
  origens exatas configuradas, nunca curinga, e somente `GET`.
- **Correção.** Tarefas `M1-T01` (autenticação) e `M1-T08` (separação de papéis e auditoria). Até
  lá, não exponha este ambiente a uma rede que você não controla.

### Já em vigor

- **Os arquivos do repositório são montados como somente leitura** (`..:/repo:ro`). O servidor lê o
  estado do projeto; não consegue escrevê-lo, então uma API comprometida não pode reescrever o
  roadmap nem os arquivos de balanceamento.
- **O container da API roda com um usuário sem privilégios de root.**
- **As migrations nunca rodam ao subir o servidor.** Um deploy não consegue remodelar o schema em
  silêncio.
- **Os carimbos de tempo são gravados como `timestamptz`,** com UTC imposto por convenção do modelo,
  então nenhum horário local ambíguo entra no banco.
- **`config_versions` tem um índice único filtrado** permitindo no máximo uma versão de
  balanceamento ativa, então "qual balanceamento produziu este resultado" sempre tem uma resposta só.
- **As tabelas de auditoria e de configuração existem desde a primeira migration,** antes de haver
  qualquer economia a auditar.
- **As respostas de saúde e de erro nunca contêm credenciais.** A verificação do banco informa o tipo
  da exceção e uma causa em linguagem simples, não a string de conexão.
- **Nenhum segredo é versionado.** O `.env` está no `.gitignore`; o `.env.example` traz apenas
  valores de exemplo para desenvolvimento local.

## Como reportar

Este é um projeto privado durante a V0.1. Se um problema de segurança for encontrado, registre-o
como uma tarefa `BLOCKED` em `docs/roadmap.json`, com o subsistema afetado, para que ele apareça na
lista de atenção da Visão geral em vez de ficar perdido numa conversa.
