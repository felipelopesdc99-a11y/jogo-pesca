# Segurança

## A regra central

> **CLIENTE = intenção + apresentação**
> **SERVIDOR = verdade + validação + sorteio + persistência**

Parta do princípio de que existem clientes modificados e bots. Toda ação com valor econômico, de
progressão ou competitivo é decidida pelo servidor.

## Na fase do MVP local

Enquanto o jogo roda inteiro no PC (`docs/CLAUDE_START_HERE_V0_1_1.md`), o papel do "servidor" é do
**serviço de jogo local** (`client-unity/Assets/Scripts/GameService`), separado da apresentação e
proibido de usar o Unity. A regra acima vale igual entre essas duas partes: a tela só pede e mostra.

O que o MVP local **não** protege, e só o servidor vai proteger (Milestone 12): o relógio é o do PC
(a regra da TD-019 evita o abuso grosseiro, como adiantar o relógio com o jogo aberto) e o save é um
arquivo no PC, que pode ser editado — por isso o save é validado ao carregar, mas não é tratado
como a arquitetura definitiva.

### Boss Mundial (M24-T29, A-165, TD-049)

- **Horário pelo relógio do serviço**, nunca pelo da tela: o `IClock` da `GameSession` (o `SteadyClock` da TD-030).
  Voltar o relógio do PC congela o tempo, então não dá ataques nem pagamento a mais; adiantar o relógio para as 19:00
  de outros dias é o abuso que o MVP local não impede (TD-019) — mas cada dia paga uma vez só, e quem adianta não volta.
- **Só conta com o jogo aberto.** A primeira atualização depois de abrir o jogo só marca o ponto de partida; o tempo com
  o jogo fechado nunca vira ataque. Um congelamento curto com o jogo aberto conta no máximo `max_catch_up_attacks`.
- **Sem repetição.** Cada ataque tem um índice no dia (k intervalos depois das 19:00) e o seu sorteio vem do `Rng` do
  save com o dia e o índice; o save guarda o último índice processado, então o mesmo ataque nunca conta duas vezes. O
  pagamento marca `paid` no mesmo save que credita as moedas.
- **Faixas relativas ao Cardume mais forte do dia**, guardado como máximo (`reference_damage` nunca desce): trocar para
  um Cardume fraco no começo e forte depois não deixa a faixa mais fácil.
- **Dólares do top 10** só com `online_rewards_enabled`, desligado no jogo local (no ranking local só existe o jogador).
  Com o servidor, o ranking, o dano e o pagamento passam a ser dele.

### Passe do Pescador (M24-T17, A-158, TD-042)

- **Cada marco se resgata uma vez só.** O save guarda os níveis resgatados em cada faixa (`PassState.FreeClaimed` e
  `PremiumClaimed`); um segundo pedido do mesmo marco é recusado (`PassAlreadyClaimed`) e "Resgatar tudo" passa pela
  mesma regra. Só se resgata marco alcançado pelo nível do Pescador, que é do serviço.
- **Premium pago com Dólares, pelo serviço.** `BuyPremium` confere o saldo (`NotEnoughDollars`) e o preço do config e
  debita no mesmo save que liga o Premium; comprar de novo é recusado (`PassPremiumOwned`). Marco Premium sem o
  Premium é recusado (`PassPremiumLocked`); marco acima do nível, também (`PassMilestoneNotReached`).
- **Sem Dólares em dobro.** Os Dólares que o A-111 já pagou até o Nv.100 ficam marcados na migração
  (`PassState.LegacyDollarsUpToLevel` = mín(nível, 100)); o resgate desses marcos entrega o resto da recompensa, mas
  não os Dólares de novo.

### Fragmentos de barco, vara e roupa (A-157, A-163, A-164, TD-042, TD-048)

- **Sorteados pelo serviço, depois da captura.** Cada peixe puxado tenta o fragmento do barco do mapa, depois o da vara e
  depois o de roupa, com o `Rng` da mesma tentativa (`BoatFragmentRules.RollOnCatch` e as regras de vara e roupa): a
  captura nunca muda por causa do fragmento e a tela não sorteia nada. Peixe que escapa, Caixa cheia e Expedição não dão
  fragmento. Montar é do serviço (automático para barco e vara; a roupa, por pedido, conferindo os fragmentos).

### Pesca offline do VIP (A-160, TD-045)

- **O ciclo de 15 s vale só enquanto o VIP vale.** O serviço calcula a pesca offline tentativa por tentativa com o
  relógio do serviço: cada tentativa usa 15 s se termina antes do fim do VIP; dali em diante, 20 s. O limite de 24 h
  continua; relógio voltado para trás não gera tentativas (TD-030).

### Volta para 5 barcos (A-168, TD-051)

- **Migração única.** O save v20 entra com `BoatModelVersion` 1; a `BoatModelMigration` converte os barcos 6 a 10
  (fragmentos para o barco do mesmo mapa; barco montado vira Moedas e Conchas dos níveis e os fragmentos que custou) e
  grava a versão atual no mesmo save. Abrir o jogo de novo não converte outra vez, então não dá para repetir o
  reembolso.

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

*A Tripulação e as Melhorias saíram do jogo em 12/10/2026 (M24-T46, TD-053); os dois parágrafos abaixo ficam como
histórico. O reembolso de um save antigo vem só do que está gravado nele e dos preços do config, somado com teto
(`SafeMath`), e acontece uma vez: depois os campos somem do save.*

**Tripulação acelerada (M24, TD-039).** O cliente só pede "sincronizar" ou "contratar"; nunca diz quanto a
Tripulação rendeu. O serviço paga do cursor gravado (`LastCreditedAtMs`) até agora, pelo relógio que nunca volta
(TD-030), e move o cursor: pedir de novo no mesmo instante não paga nada. Relógio voltado não paga nem tira; relógio
adiantado com o jogo fechado vale no máximo o teto offline (10 h, metade da renda depois das 2 primeiras); um cursor
no futuro (save editado) é trazido para agora sem pagar. Preços que não cabem num número nunca são cobrados nem
pagos. No servidor, o mesmo cálculo roda com o relógio dele.

**Melhorias (M24, TD-040).** O cliente só pede "comprar esta Melhoria"; preço, desbloqueio e efeito vêm do
`upgrades.json` e do save. A de tripulante não se compra duas vezes nem antes da quantidade; a geral não passa do
nível máximo, e um nível maior que o máximo num save editado conta só até o máximo. Preços que não cabem num número
nunca são cobrados. A renda da Tripulação é paga antes da compra, na taxa antiga, então comprar não muda o passado.

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
