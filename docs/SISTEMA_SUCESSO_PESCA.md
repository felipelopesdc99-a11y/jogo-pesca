# FISHING IDLE — SISTEMA DE SUCESSO DA PESCA, VARAS, BARCOS E ISCAS
## Especificação de expansão para implementação sobre o jogo existente

> **Situação no repositório (30/09/2026):** especificação do proprietário, guardada como está.
> Implementada no Milestone 15 (`docs/roadmap.json`): regras em `CatchRules.Attempt` e `GearRules`,
> números em `config/progression.json`, `config/rods.json` e `config/equipment.json`, detalhes de tela
> em `docs/GDD_ADENDO.md` (A-088 a A-092) e a comparação antes/depois em
> `docs/relatorios/SIMULACAO_BALANCEAMENTO.md`, seção 7. Ainda não existem: a raridade Épico (a
> chance-base de 24% entra com ela, `OD-019`), a Vara 2, os mapas 3 e 4, o bônus temporário e o som
> de escape (`OD-020`).

**Destino:** Claude  
**Idioma visível ao jogador:** PT-BR  
**Status:** nova decisão de design do proprietário  
**Escopo:** somente o loop de pesca e sua progressão/economia associada. Não recriar o jogo nem os sistemas existentes.

---

# 1. OBJETIVO

Adicionar uma segunda etapa ao resultado de cada ciclo de pesca:

1. o jogo determina qual peixe mordeu;
2. o jogador **pode ou não conseguir puxar/capturar esse peixe**;
3. a chance de sucesso começa relativamente baixa e melhora ao longo da progressão;
4. raridades mais altas podem ser mais difíceis de capturar;
5. Vara, Barco e Isca passam a ser importantes fontes de progressão e gasto de recursos.

A intenção é criar:

- mais expectativa em cada pescaria;
- progressão perceptível de equipamento;
- novos gastos de Moedas e Conchas;
- maior importância para upgrades;
- uma sensação de que o pescador realmente está ficando melhor ao longo do jogo.

---

# 2. REGRA CENTRAL

A pesca não deve mais ser tratada como 100% de captura após a espécie ser sorteada.

A partir desta expansão, cada ciclo possui duas resoluções:

### Etapa A — Peixe encontrado
O sistema sorteia normalmente:

- mapa;
- espécie;
- raridade;
- tamanho potencial;

### Etapa B — Tentativa de captura
Depois disso, o jogo calcula a **Chance de Sucesso da Captura**.

Se passar no teste:

- peixe é efetivamente capturado;
- entra na Fishing Box;
- recebe tamanho final;
- gera Fisher XP;
- pode gerar Conchas;
- pode ser vendido/guardado/alimentado normalmente.

Se falhar:

- o peixe escapa;
- nenhum FishInstance é criado;
- nenhum peixe entra na Fishing Box;
- o ciclo termina como tentativa malsucedida;
- mostrar feedback visual e textual claro.

---

# 3. TEXTO DE FALHA

Mensagem principal aprovada pelo proprietário:

> **Você ainda não é bom o suficiente.**

Complemento opcional menor:

> Melhore sua vara, barco ou isca para aumentar suas chances.

A mensagem não deve soar punitiva demais. Deve comunicar progressão futura.

Não usar popup modal bloqueante a cada falha.

Preferir:

- toast curto;
- feedback próximo da área da pesca;
- animação do peixe escapando;
- som leve de linha escapando/água.

---

# 4. CHANCE BASE DE SUCESSO

A primeira versão deve trabalhar com aproximadamente **50% de chance base de sucesso** para um peixe comum em condições neutras.

Todos os números desta seção são **provisórios e configuráveis**.

Sugestão inicial:

| Raridade | Chance-base de captura |
|---|---:|
| Comum | 50% |
| Raro | 38% |
| Épico | 24% |

Objetivo:

- Comum ainda escapa com frequência no início;
- Raro gera expectativa real;
- Épico é um acontecimento e exige progressão de equipamento para se tornar razoavelmente capturável.

Não codificar estes números diretamente na lógica.

---

# 5. FÓRMULA DE SUCESSO

Estrutura recomendada:

```text
chance_final = chance_base_da_raridade
             + bonus_da_vara
             + bonus_do_barco
             + bonus_da_isca
             + bonus_temporario_configuravel
```

Aplicar limite:

```text
chance_final = clamp(chance_final, 5%, 95%)
```

## Motivo do teto

Mesmo com equipamento excelente:

- nunca chegar a 100%;
- manter alguma tensão;
- evitar automatismo absoluto;
- preservar valor de upgrades futuros.

## Motivo do piso

Nenhum peixe deve ser praticamente impossível.

---

# 6. ORDEM CORRETA DO RNG

Para manter o sistema claro e auditável:

1. determinar espécie/raridade elegível;
2. calcular Chance de Sucesso;
3. rolar captura;
4. se sucesso, gerar tamanho e demais resultados;
5. se falha, registrar somente a tentativa/escape.

Alternativamente, o tamanho pode ser pré-calculado internamente para efeitos visuais, mas **não deve entrar em inventário nem gerar economia se o peixe escapar**.

O servidor continua sendo a autoridade do RNG.

---

# 7. IMPACTO NA PROGRESSÃO

Essa mudança reduz o número de peixes efetivamente capturados por hora.

Portanto, Claude deve **recalibrar a progressão** para não dobrar artificialmente o tempo necessário para avançar.

O alvo é:

- manter a sensação atual de progresso;
- usar a taxa de sucesso como camada de emoção/equipamento;
- não transformar a progressão em grind excessivo.

## Regra importante

Ao implementar, recalcular:

- XP efetivo por hora;
- Moedas efetivas por hora;
- Conchas efetivas por hora;
- quantidade de espécies descobertas por hora;
- tempo médio até um Raro;
- tempo médio até um Épico.

A economia dos Mapas 3 e 4 deve considerar **capturas bem-sucedidas**, não apenas mordidas.

---

# 8. XP EM FALHA

Decisão inicial recomendada:

### Falha de captura
- não concede Fisher XP principal;
- não concede valor de venda;
- não concede Conchas;
- não registra a espécie como descoberta;
- não atualiza recordes.

Isso mantém a captura bem-sucedida como evento valioso.

Se, após teste, a progressão ficar frustrante, pode existir futuramente uma quantidade mínima de XP por tentativa, mas **não implementar agora sem nova decisão**.

---

# 9. VARAS — NOVA FUNÇÃO

Varas já possuem níveis internos 1–10 e bônus de pesca.

Adicionar um novo eixo configurável:

> **Bônus de Sucesso de Captura**

As varas continuam mantendo os bônus existentes e passam também a melhorar a capacidade de puxar o peixe.

## Exemplo provisório

### Vara 1

| Nível | Bônus de sucesso |
|---|---:|
| 1 | +2% |
| 2 | +3% |
| 3 | +4% |
| 4 | +5% |
| 5 | +6% |
| 6 | +7% |
| 7 | +8% |
| 8 | +9% |
| 9 | +10% |
| 10 | +12% |

### Vara 2

Deve começar acima da Vara 1 equivalente, sem tornar a Vara 1 inútil instantaneamente.

Exemplo:

| Nível | Bônus de sucesso |
|---|---:|
| 1 | +8% |
| 5 | +14% |
| 10 | +20% |

Valores exatos configuráveis.

---

# 10. BARCOS

Adicionar **Barcos** como progressão persistente de pesca.

## Papel do barco

O barco representa:

- estabilidade;
- acesso a melhores pontos;
- facilidade de controlar peixes grandes;
- progressão visual do jogador.

## Efeito mecânico inicial

Barcos concedem:

- bônus de Chance de Sucesso;
- opcionalmente pequeno bônus secundário futuro, desde que configurável.

Na primeira implementação, manter simples:

> Barco = bônus de sucesso da captura.

Não adicionar velocidade de pesca, inventário extra ou sistemas paralelos agora.

## Estrutura sugerida

- Barco Inicial
- Barco 1
- Barco 2
- Barco 3
- Barco 4
- Barco 5

A quantidade final pode ser ajustada futuramente.

## Progressão econômica

Barcos devem ser compras relevantes e mais espaçadas do que upgrades de Vara.

Podem consumir:

- Moedas;
- Conchas;
- combinação dos dois em tiers mais altos.

A intenção é criar um **sink econômico importante**.

---

# 11. ISCAS

Adicionar Iscas como recurso consumível/equipável de pesca.

## Objetivo

Criar gasto recorrente sem obrigar o jogador a microgerenciar cada lançamento.

## Regra de UX

Isca deve funcionar por lote/duração, e não exigir clique a cada captura.

Exemplos possíveis:

- 30 minutos;
- 1 hora;
- X tentativas de pesca.

Preferência inicial: **duração em número de tentativas**, porque é simples de auditar e funciona online/offline.

## Exemplo de tiers

### Isca Simples
- bônus pequeno;
- comprada com Moedas.

### Isca Melhorada
- bônus médio;
- custo maior em Moedas.

### Isca Premium de Pesca
- bônus alto;
- pode usar Conchas ou Moedas + Conchas.

Não usar dinheiro real nesta especificação.

## Efeito inicial

Isca aumenta somente:

> Chance de Sucesso da Captura

Futuramente pode haver iscas focadas em raridade ou tamanho, mas **não implementar nesta etapa sem nova decisão**.

---

# 12. EQUIPAMENTO ATIVO

A pesca deve considerar simultaneamente:

- Vara equipada;
- Barco ativo;
- Isca ativa.

Cada um contribui para a Chance de Sucesso.

## UI compacta recomendada

Na tela de pesca, exibir de forma discreta:

- ícone da Vara;
- ícone do Barco;
- ícone da Isca;
- Chance de Sucesso aproximada ou detalhada quando o jogador abrir as informações.

Não poluir o HUD permanente.

---

# 13. COMO MOSTRAR A CHANCE AO JOGADOR

Recomendação:

No painel de equipamento/pesca, mostrar algo como:

> **Chance de puxar peixe Comum: 64%**  
> **Raro: 52%**  
> **Épico: 38%**

Isso ajuda o jogador a entender por que vale a pena melhorar seu equipamento.

Não esconder totalmente a matemática.

O jogador deve conseguir responder:

> “Se eu gastar minhas Moedas/Conchas nisso, o que melhora?”

---

# 14. FEEDBACK VISUAL DE FALHA

Quando o peixe escapar:

1. animação de fisgada começa normalmente;
2. linha/vara reage;
3. peixe quase aparece ou cria movimento na água;
4. linha perde tensão;
5. splash curto;
6. mensagem de falha aparece;
7. pescador volta ao loop normal.

A falha deve durar poucos segundos.

Não transformar 50% das pescarias em interrupções longas.

---

# 15. FEEDBACK POR RARIDADE

A tensão da tentativa pode variar conforme raridade.

### Comum
- feedback curto.

### Raro
- reação um pouco mais forte da vara;
- som mais evidente;
- expectativa perceptível.

### Épico
- fisgada claramente mais intensa;
- pequena pausa dramática;
- efeitos controlados;
- se escapar, jogador deve perceber que perdeu algo importante;
- se capturar, celebração forte já prevista para raridades importantes.

Não revelar necessariamente a espécie antes do resultado, a menos que o sistema atual já faça isso.

---

# 16. ECONOMIA — MOEDAS E CONCHAS

Esta nova decisão **abre oficialmente uso de Conchas como recurso de progressão de pesca**.

Até então, Conchas existiam sem gasto definido.

Agora podem ser usadas em:

- barcos de tier mais alto;
- upgrades específicos de barco;
- iscas melhores;
- upgrades avançados de pesca.

## Regra econômica

### Moedas
Recurso principal e frequente.

### Conchas
Recurso mais valioso e menos abundante.

Conchas não devem substituir Moedas; devem complementar a progressão.

Exemplo conceitual:

- upgrades básicos → Moedas;
- upgrades intermediários → muitas Moedas;
- upgrades avançados → Moedas + Conchas;
- iscas especiais → Conchas ou combinação.

Todos os custos devem ser configuráveis.

---

# 17. PRINCÍPIO DE SINK ECONÔMICO

O objetivo não é apenas “tirar dinheiro do jogador”.

O jogador precisa sentir que:

> gastei recursos → fiquei claramente melhor → agora perco menos peixes.

O crescimento precisa ser legível.

Evitar:

- custos altos com melhoria imperceptível;
- consumo de Conchas sem feedback;
- upgrades que aumentam 0,1% e parecem inúteis;
- equipamentos obrigatórios demais cedo.

---

# 18. INTEGRAÇÃO COM MAPAS 3 E 4

## Mapa 3 — Pantanal Dourado

- Vara 1 continua elegível;
- Raros e Épico passam a testar mais fortemente a qualidade do equipamento;
- jogador já deve perceber valor em evoluir Vara e investir em Barco/Isca;
- Barbado Épico deve ser difícil, mas possível.

## Mapa 4 — Estuário das Marés

- Vara 2 é exigida;
- dois Épicos tornam a Chance de Sucesso mais importante;
- progressão de Barco/Isca deve ganhar peso maior;
- Camurupim e Mero precisam parecer capturas realmente importantes.

---

# 19. OFFLINE FISHING

A mesma Chance de Sucesso deve ser aplicada à pesca offline.

Para cada tentativa offline:

1. sorteia peixe elegível;
2. calcula sucesso com equipamento salvo/ativo;
3. somente sucesso entra na Fishing Box.

## Iscas offline

Se Isca usa número de tentativas:

- consumir uma carga por tentativa, independentemente de sucesso.

Isso é simples e consistente.

Se futuramente Isca for baseada em tempo, deve usar timestamps autoritativos.

---

# 20. SAVE / PERSISTÊNCIA

Adicionar sem quebrar saves existentes.

Salvar:

- barco possuído/ativo;
- upgrades do barco;
- iscas em inventário;
- isca ativa e cargas restantes;
- novos bônus da Vara via config/nível existente.

Não resetar:

- nível do jogador;
- varas;
- peixes;
- Aquário;
- Fishing Box;
- Cardume;
- Moedas;
- Conchas;
- Enciclopédia.

---

# 21. CONFIGURAÇÃO / LIVE BALANCE

Todos estes valores devem ficar configuráveis:

- chance-base por raridade;
- bônus de sucesso por Vara e nível;
- bônus de sucesso por Barco;
- custos dos Barcos;
- custos de upgrades dos Barcos;
- bônus de Isca;
- custo de Isca;
- duração/cargas da Isca;
- teto e piso da chance;
- mensagens visíveis ao jogador;
- multiplicadores específicos por mapa, caso futuramente necessários.

Não hardcodear números no gameplay.

---

# 22. DEV CONSOLE

Adicionar visualização/editabilidade para:

- chance-base Comum/Raro/Épico;
- bônus de cada Vara por nível;
- bônus de cada Barco;
- bônus/cargas/custo das Iscas;
- chance final simulada por combinação de equipamento;
- capturas por hora estimadas;
- escapes por hora estimados.

Adicionar uma pequena ferramenta de simulação:

> mapa + vara + barco + isca + 10.000 tentativas

Exibir:

- tentativas;
- capturas;
- escapes;
- taxa real de sucesso;
- Comuns/Raros/Épicos capturados;
- XP/h;
- Moedas/h;
- Conchas/h.

---

# 23. REBALANCEAMENTO OBRIGATÓRIO

Depois de implementar o sistema, Claude deve executar simulação comparando:

### Antes
- 100% das mordidas viram captura.

### Depois
- sistema de Chance de Sucesso ativo.

O objetivo é evitar quebrar:

- tempo de nível;
- economia;
- raridade percebida;
- progressão dos Mapas 3 e 4.

Se 50% de sucesso reduzir a progressão demais, ajustar:

- Fisher XP por captura;
- valor dos peixes;
- pesos de espécie;
- custos de equipamentos;

**Não aumentar automaticamente o intervalo de pesca.**

---

# 24. PRIMEIRA CONFIGURAÇÃO DE TESTE

Usar apenas como ponto de partida:

## Chance-base
- Comum: 50%
- Raro: 38%
- Épico: 24%

## Exemplo jogador intermediário
Vara + Barco + Isca = +20 pontos percentuais.

Resultado:

- Comum: 70%
- Raro: 58%
- Épico: 44%

## Jogador muito avançado
Equipamento total = +45 pontos percentuais.

Resultado antes do cap:

- Comum: 95%
- Raro: 83%
- Épico: 69%

Isso cria uma progressão muito perceptível sem trivializar Épicos.

---

# 25. O QUE NÃO IMPLEMENTAR AGORA

Não adicionar nesta etapa:

- minigame manual de puxar linha;
- durabilidade de Vara;
- quebra de Vara;
- combustível de Barco;
- stamina do jogador;
- falha crítica;
- peixe roubando equipamento;
- múltiplos slots de isca simultâneos;
- crafting complexo;
- qualidade aleatória de equipamentos;
- IV/genética de peixe;
- outra moeda nova.

Manter simples.

---

# 26. CHECKLIST PARA CLAUDE

- [ ] Adicionar teste de sucesso após peixe ser selecionado.
- [ ] Implementar chance-base por raridade.
- [ ] Adicionar novo bônus de sucesso às Varas.
- [ ] Criar estrutura de Barcos.
- [ ] Criar estrutura de Iscas.
- [ ] Adicionar custos em Moedas/Conchas.
- [ ] Implementar feedback de escape.
- [ ] Implementar mensagem “Você ainda não é bom o suficiente.”
- [ ] Aplicar mesma lógica ao offline.
- [ ] Preservar save existente.
- [ ] Atualizar Dev Console.
- [ ] Criar simulador de taxa de sucesso.
- [ ] Recalibrar XP/h e Moedas/h.
- [ ] Verificar Mapas 3 e 4 com Raros/Épicos.
- [ ] Não introduzir sistemas fora deste escopo.

---

# 27. RESULTADO ESPERADO

A pesca deve passar a ter este ciclo emocional:

> **Mordeu algo → será que eu consigo puxar? → escapou / consegui → quero melhorar meu equipamento → agora consigo puxar peixes que antes escapavam.**

Isso transforma Vara, Barco e Isca em progressão real, cria novos usos para Moedas e Conchas e aumenta a tensão de uma captura importante sem abandonar o estilo relaxante do Fishing Idle.

