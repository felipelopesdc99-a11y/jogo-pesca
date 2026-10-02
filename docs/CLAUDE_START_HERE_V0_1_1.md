# START HERE — MVP LOCAL JOGÁVEL (UNITY + PC + CLAUDE)

## REGRA OBRIGATÓRIA DE IDIOMA

O proprietário do projeto é brasileiro.

- Todo conteúdo visível ao proprietário deve estar em **Português do Brasil (PT-BR)**.
- Toda interface do jogo deve estar em **PT-BR**.
- Painéis, menus, botões, mensagens, roadmap, status, logs amigáveis, instruções e relatórios de progresso devem estar em **PT-BR**.
- Código, nomes de classes, variáveis, APIs e comentários técnicos podem ficar em inglês quando isso for tecnicamente adequado.
- Não criar painel, site ou interface para o proprietário em inglês.

---

# OBJETIVO PRINCIPAL

Você está implementando um jogo idle de pesca 2.5D em **Unity 6.3 LTS + C#**.

Leia `FISHING_IDLE_V0_1_GDD_TECH_SPEC.md` por completo antes de implementar sistemas de gameplay.

Porém, a prioridade mudou:

> **PRIMEIRO ENTREGAR UM MVP LOCAL, JOGÁVEL E DIVERTIDO NO PC DO PROPRIETÁRIO.**

O proprietário precisa conseguir abrir o projeto no Unity, apertar Play e realmente jogar o loop principal do jogo.

Neste momento, NÃO priorizar infraestrutura de produção, hospedagem, site público, deploy na nuvem ou serviços pagos.

---

# AMBIENTE DE DESENVOLVIMENTO DA V0.1

O fluxo desejado agora é:

**PC do proprietário + Unity + Claude = jogo local funcionando.**

Tudo deve funcionar localmente sempre que for razoável.

Evite exigir:

- Vercel;
- domínio;
- hospedagem paga;
- servidores cloud;
- banco de dados cloud;
- serviços SaaS pagos;
- CI/CD pago;
- contas externas desnecessárias;
- Docker quando ele não for necessário para o MVP;
- qualquer assinatura apenas para começar a jogar.

Se algum passo realmente exigir pagamento, NÃO prossiga automaticamente. Explique primeiro:

1. por que é necessário;
2. qual é o custo esperado;
3. se existe alternativa local/gratuita;
4. se pode ser adiado para depois do MVP.

Na dúvida, escolha a solução local e simples.

---

# PAPEL DO CLAUDE

Atue como **technical lead + executor do projeto**.

O GDD contém as decisões de design já aprovadas. Não redesenhe o jogo e não adicione sistemas desnecessários.

Seu objetivo não é construir uma infraestrutura perfeita antes do jogo existir.

Seu objetivo é:

1. construir;
2. colocar para rodar;
3. permitir que o proprietário jogue;
4. receber feedback;
5. ajustar rapidamente.

Priorize código claro, modular e substituível.

---

# PRINCÍPIO DO MVP

A V0.1 deve ser um **vertical slice real do jogo**, mas rodando localmente.

O proprietário deve conseguir experimentar o fluxo:

**abrir jogo → pescar → receber peixe → Caixa de Pesca → guardar/vender → Aquário → montar Cardume → evoluir peixe → mudar de mapa → usar vara → Expedição → Arena → Mercado/Leilão.**

Nem tudo precisa ter arte final.

Pode usar:

- sprites provisórios;
- placeholders bonitos;
- formas simples;
- animações temporárias;
- dados locais;
- bots locais para PvP;
- mercado simulado localmente;

Desde que a arquitetura permita substituir essas partes depois.

A prioridade é validar se o jogo é divertido.

---

# ARQUITETURA LOCAL TEMPORÁRIA

## Regra geral

Mesmo no MVP local, mantenha a separação conceitual:

**Cliente = apresentação + intenção**  
**Game Service local = regras + RNG + validação + persistência**

Para o MVP, esse "servidor" pode ser uma camada local em C# dentro da própria solução/projeto, desde que esteja claramente separada do código visual da Unity.

Não é necessário subir um servidor web real agora apenas para simular segurança de produção.

Crie interfaces/serviços que permitam migrar depois para um backend remoto sem reescrever o gameplay inteiro.

Exemplo conceitual:

- `IFishingService`
- `IArenaService`
- `IMarketService`
- `IPlayerRepository`
- implementação atual: `Local...Service`
- implementação futura: `Remote...Service`

A Unity NÃO deve gerar diretamente resultados de economia em componentes visuais.

Mesmo localmente, o resultado da pesca deve vir do serviço de gameplay autoritativo local.

---

# PERSISTÊNCIA LOCAL

Para a V0.1, prefira persistência simples no PC.

Pode usar, em ordem de preferência conforme necessidade:

1. JSON local estruturado;
2. SQLite local se os dados relacionais começarem a justificar isso;
3. outra solução local simples somente se houver motivo técnico real.

Não exigir PostgreSQL, Docker ou cloud neste estágio apenas por arquitetura futura.

Toda persistência deve ter:

- versão do save;
- backup simples;
- validação básica;
- possibilidade de reset de save no painel de desenvolvimento.

---

# PAINEL DE DESENVOLVIMENTO LOCAL

O proprietário ainda quer uma área visual para acompanhar o projeto e configurar o jogo.

Por enquanto, crie isso **LOCALMENTE**, sem Vercel.

A solução preferida é uma ferramenta interna acessível dentro do próprio projeto, por exemplo:

- uma janela de Editor do Unity; e/ou
- uma cena/tela `Dev Panel` disponível somente em development build; e/ou
- uma página local muito simples somente se for realmente necessária.

Não criar um site hospedado apenas para isso.

## O painel deve mostrar

### Roadmap

Estados:

- `A FAZER`
- `EM ANDAMENTO`
- `CONCLUÍDO`
- `BLOQUEADO`
- `PRECISA DE DECISÃO DO PROPRIETÁRIO`

### Informações

- milestone atual;
- tarefas concluídas;
- tarefas pendentes;
- próximo passo;
- bloqueios;
- versão atual do jogo;
- data/hora da última atualização manual do roadmap.

### Configuração de balanceamento

Quando possível, permitir editar sem alterar código:

- tempo de pesca online;
- tempo de pesca offline;
- limite offline;
- chances de espécies;
- distribuição de tamanho;
- stats-base dos peixes;
- XP;
- valores de venda;
- custos de upgrade;
- bônus das varas;
- energia da Arena;
- Honra;
- parâmetros de Expedição;
- taxa de Mercado;
- parâmetros de Leilão.

Esses valores devem vir de dados configuráveis, não ficar espalhados/hardcoded no código.

---

# ROADMAP DE IMPLEMENTAÇÃO LOCAL

Não tente fazer tudo ao mesmo tempo.

## MILESTONE 0 — JOGO ABRE E É EDITÁVEL

Entregar:

1. Projeto Unity 6.3 LTS funcionando.
2. Estrutura organizada de pastas e assemblies.
3. Cena principal inicial.
4. Sistema de configuração data-driven.
5. Save local.
6. Dev Panel local.
7. Roadmap local em PT-BR.
8. `DECISIONS.md`.
9. `CHANGELOG.md`.
10. `README.md` com instruções extremamente simples para abrir e rodar.

### Critério de conclusão

O proprietário abre o projeto, aperta Play e vê a aplicação rodando sem configurar serviços externos.

---

## MILESTONE 1 — LOOP DE PESCA JOGÁVEL

Entregar primeiro o coração do jogo.

- cena 2.5D provisória bonita;
- mar;
- barco;
- pescador;
- animação básica/loop de pesca;
- iniciar/parar pesca;
- ciclo online de 30 segundos configurável;
- servidor/game-service local controla os ciclos;
- RNG de espécie e tamanho;
- captura aparece visualmente;
- Caixa de Pesca;
- venda para o jogo;
- moeda principal;
- XP do Pescador;
- primeiro mapa;
- primeiros peixes configurados por dados.

### Critério de conclusão

O proprietário consegue ficar 10–20 minutos pescando e sentir o loop do jogo.

---

## MILESTONE 2 — AQUÁRIO + PEIXE PERSISTENTE

- Aquário com limite fixo de 100;
- guardar peixe da Caixa;
- entidade persistente somente quando peixe é guardado;
- ficha do peixe;
- tamanho;
- raridade;
- nível 1–10;
- Vida, Ataque, Defesa, Velocidade;
- alimentação;
- transferência de 50% do XP investido quando peixe evoluído é consumido;
- ordenação visual;
- proteções de confirmação.

---

## MILESTONE 3 — PERFIL + CARDUME + INVENTÁRIO + VARA

- Perfil em PT-BR;
- Equipamentos;
- slot de Vara;
- Inventário geral dentro do Perfil;
- Cardume 1–6;
- formação 3 frente + 3 atrás;
- ordem fixa 1→6;
- bônus de +3% com 6/6;
- Força do Cardume visível somente ao dono;
- Enciclopédia dentro do Perfil;
- recordes pessoais.

---

## MILESTONE 4 — MAPAS + VARAS

- Menu Mapa;
- Mapa 1;
- Mapa 2;
- 10 peixes por mapa;
- desbloqueio do mapa 2 no Nv.10;
- viagem visual de 30 segundos;
- pesca pausa durante viagem;
- Vara Inicial;
- Vara 1;
- requisitos de vara por mapa;
- vara Nv.1–10;
- upgrade com moedas;
- bônus pequenos de raridade, tamanho e Conchas;
- varas antigas permanecem no Inventário.

---

## MILESTONE 5 — PESCA OFFLINE

- 60 segundos por captura offline;
- máximo de 24h;
- cálculo por timestamp;
- processamento agregado;
- tela simples de retorno;
- captura vai para Caixa de Pesca;
- sem timers individuais rodando enquanto jogador está fora.

---

## MILESTONE 6 — EXPEDIÇÕES

- Cardume pode ser enviado;
- 30min / 1h / 3h / 6h;
- funcionamento offline;
- Força Recomendada;
- eficiência abaixo da recomendação;
- bônus limitado acima da recomendação;
- recompensa moderada em moedas;
- pequena chance de peixe;
- peixe encontrado vai para Caixa de Pesca;
- popup de conclusão.

---

## MILESTONE 7 — ARENA LOCAL JOGÁVEL

Para o MVP, use jogadores/bots simulados localmente.

Implementar:

- Arena;
- 24 Energias;
- 1 Energia por ataque;
- 1 Energia regenerada/hora;
- Honra;
- ranking;
- troca direta de posição;
- 3 adversários;
- janela de aproximadamente 10% acima do rank;
- 1 reroll por ciclo;
- seleção fica travada até atacar;
- cards mostram Cardume, nível e raridade;
- NÃO mostrar Força do Cardume do adversário;
- combate automático;
- micro-RNG de dano;
- slots mortos permanecem vazios;
- prioridade 1→2→3→4→5→6;
- 1x / 2x / Pular;
- maioria das lutas mirando até ~60 segundos sem hard cap;
- vitória troca ranking e dá Honra;
- derrota perde pequena Honra;
- defesa bem-sucedida dá Honra.

A simulação deve ser separada da apresentação.

---

## MILESTONE 8 — MERCADO LOCAL

Inicialmente o Mercado pode ser simulado com anúncios gerados localmente/bots, mas o fluxo deve ser o real.

- Comprar;
- Vender;
- Meus Anúncios;
- Itens a Retirar;
- cards visuais estilo mercado de MMO simplificado;
- 5 anúncios ativos;
- 7 dias;
- preço fixo;
- sem taxa para anunciar;
- taxa de 3% quando vende;
- preço livre;
- item/peixe mantém todos os dados;
- toda saída do Mercado passa por Itens a Retirar;
- Aquário cheio não impede comprar;
- não pode retirar peixe se Aquário estiver 100/100.

---

## MILESTONE 9 — LEILÃO LOCAL

- aba Leilão dentro do Mercado;
- 1 leilão ativo por vendedor;
- duração de 6h;
- vendedor define lance inicial;
- não pode cancelar sem oferta;
- cada lance deve ser pelo menos 3% acima do atual;
- comprador paga 1% do próprio lance em taxa não reembolsável;
- valor principal do lance fica bloqueado;
- se superado, principal retorna;
- últimos <60s + novo lance → cronômetro volta para 60s;
- se houver oferta, vendedor pode encerrar antecipadamente;
- encerramento antecipado: maior lance vence e vendedor paga 3%;
- sem oferta, precisa esperar o tempo acabar;
- maior lance válido vence automaticamente ao final.

---

## MILESTONE 10 — TUTORIAL + UX + POLIMENTO

- tutorial curto;
- primeira vara;
- primeira pesca;
- Caixa;
- primeira venda;
- Cardume;
- Expedição;
- UI principal;
- menus superiores;
- card retrátil do jogador;
- notificações/toasts;
- captura importante com glow/aura;
- modo compacto/barra de tarefas;
- áudio provisório;
- animações melhores;
- feedback visual.

---

## MILESTONE 11 — BALANCEAMENTO DO MVP

Somente depois de tudo acima estar jogável:

- revisar progressão Lv.1–20;
- revisar economia;
- revisar frequência de raridades;
- revisar tamanhos;
- revisar stats;
- revisar duração de combate;
- revisar força das Expedições;
- revisar preço/upgrade das varas;
- revisar Mercado/Leilão;
- testes de save/load;
- testes de abuso local;
- corrigir bugs.

---

# O QUE NÃO FAZER AGORA

Não implementar nesta fase:

- Vercel;
- site público hospedado;
- domínio;
- Steamworks;
- launcher próprio;
- servidor de produção;
- banco cloud;
- Kubernetes;
- microserviços;
- arquitetura de escala para milhões de usuários;
- pagamentos reais;
- monetização completa;
- equipamentos além da vara;
- refinamento;
- passe;
- temporadas;
- guildas;
- chat;
- sistema social grande;
- habilidades de peixe;
- classes;
- crítico;
- esquiva;
- precisão;
- quests diárias;
- achievements complexos;
- mapas 3–10;
- conteúdo de endgame.

Esses sistemas serão adicionados depois que o proprietário já estiver jogando o MVP e validar o núcleo.

---

# REGRA DE SIMPLICIDADE

Este é um projeto indie conduzido pelo proprietário com ajuda de IA.

Sempre que houver duas opções tecnicamente válidas:

> **escolha a mais simples que preserve a possibilidade de evoluir depois.**

Não transforme uma feature de 2 horas em uma infraestrutura de 2 dias sem necessidade.

Não adicione abstrações, frameworks ou serviços apenas porque seriam adequados para uma empresa grande.

---

# REGRA DE SEGURANÇA FUTURA

Embora o MVP seja local, organize o código para que sistemas economicamente importantes possam migrar para servidor autoritativo depois.

Não trate o save local como arquitetura definitiva de produção.

A versão online futura deverá manter:

**Cliente = intenção + apresentação**  
**Servidor = verdade + validação + RNG + persistência**

Mas isso NÃO deve impedir o MVP local de existir agora.

---

# PRIMEIRA TAREFA AGORA

Implemente apenas **Milestone 0** e depois avance imediatamente para **Milestone 1 — Loop de Pesca Jogável**.

O objetivo da primeira grande entrega não é um backend sofisticado.

O objetivo é o proprietário conseguir:

1. abrir Unity;
2. apertar Play;
3. ver o barco e o pescador;
4. iniciar pesca;
5. esperar o ciclo;
6. capturar um peixe;
7. ver o peixe na Caixa de Pesca;
8. vender ou guardar;
9. repetir e sentir que já existe um jogo.

Depois disso, continue o roadmap em ordem.

Sempre informe em PT-BR:

- o que foi implementado;
- quais arquivos foram criados/alterados;
- como testar;
- o que ainda falta;
- qual será o próximo passo.
