# FISHING IDLE — NOMES DEFINITIVOS, LOJA E PREÇOS DE EQUIPAMENTOS
## Documento de implementação para Claude

**Idioma da UI:** PT-BR  
**Escopo:** nomes próprios de Varas, Barcos e Iscas + preços + desbloqueios + integração com o sistema de Chance de Sucesso da captura.  
**Importante:** este documento complementa o jogo existente. Não recriar sistemas prontos e não resetar saves.

---

# 1. PRINCÍPIO GERAL

Os equipamentos não devem aparecer para o jogador como nomes técnicos do tipo “Vara 1”, “Barco 3” ou “Isca Premium”.

A partir desta etapa, cada equipamento deve ter um **nome próprio**, curto e memorável, com personalidade de pesca brasileira.

A progressão visual e nominal deve transmitir:

> começo humilde → equipamento confiável → equipamento profissional → equipamento de alto nível.

Os nomes técnicos/IDs internos podem continuar existindo no código para preservar compatibilidade com saves e configurações.

Exemplo:

- ID interno: `rod_01`
- nome visível: `Ponta Selvagem`

**Não renomear IDs persistidos em save apenas para trocar o nome exibido.**

---

# 2. NOMES DEFINITIVOS — VARAS

A família das Varas usa nomes ligados à água, natureza e força da pesca.

| Item técnico atual | Nome visível definitivo | Papel |
|---|---|---|
| Vara Inicial | **Caniço Manso** | equipamento humilde do começo |
| Vara 1 | **Ponta Selvagem** | primeira vara de fibra, Rio Selvagem/Pantanal |
| Vara 2 | **Maré Dourada** | vara profissional para o Estuário |

## Descrições sugeridas

### Caniço Manso
> Um equipamento simples e confiável para dar os primeiros passos na pesca.

### Ponta Selvagem
> Fibra resistente para encarar rios fortes, peixes maiores e águas mais exigentes.

### Maré Dourada
> Uma vara profissional, preparada para peixes grandes e águas salobras.

---

# 3. PREÇOS E PROGRESSÃO — VARAS

## Caniço Manso
- adquirido automaticamente no início;
- custo: **0 Moedas**;
- manter a lógica atual da Vara Inicial.

## Ponta Selvagem
- é a Vara 1 já existente no jogo;
- **manter exatamente o preço atual configurado no projeto**;
- manter níveis, upgrades e custos atuais;
- não cobrar novamente de jogadores que já possuem a Vara 1;
- apenas trocar o nome visível e a arte/referência quando necessário.

## Maré Dourada
A Vara 2 já foi definida na expansão dos Mapas 3–4.

- desbloqueio: **Pescador Nv.30**;
- compra: **90.000 Moedas**;
- níveis internos: 1–10;
- Mapas 4–5;
- raridades permitidas: Comum, Raro e Épico.

### Custos de melhoria já definidos

| Para nível | Custo |
|---:|---:|
| 2 | 10.000 Moedas |
| 3 | 15.000 Moedas |
| 4 | 23.000 Moedas |
| 5 | 35.000 Moedas |
| 6 | 52.000 Moedas |
| 7 | 78.000 Moedas |
| 8 | 117.000 Moedas |
| 9 | 175.000 Moedas |
| 10 | 260.000 Moedas |

Total de upgrades 1→10: **765.000 Moedas**, fora a compra inicial.

Esses valores continuam configuráveis.

---

# 4. NOMES DEFINITIVOS — BARCOS

A família dos Barcos usa nomes ligados à navegação e à evolução do pescador.

| Item técnico | Nome visível definitivo | Identidade |
|---|---|---|
| Barco Inicial | **Água Mansa** | canoa simples |
| Barco 1 | **Remo Valente** | bote de madeira |
| Barco 2 | **Rastro Azul** | primeiro barco com motor |
| Barco 3 | **Proa Selvagem** | barco de pesca equipado |
| Barco 4 | **Costa Nobre** | lancha profissional |
| Barco 5 | **Horizonte Dourado** | melhor barco do jogo |

A evolução visual deve ser evidente mesmo sem ler stats.

---

# 5. PAPEL MECÂNICO DOS BARCOS

Nesta primeira implementação:

> **Barco aumenta a Chance de Sucesso ao puxar o peixe.**

Não adicionar agora:
- combustível;
- durabilidade;
- espaço extra de inventário;
- velocidade de pesca;
- slots extras;
- reparos.

O barco é uma progressão persistente e um grande **sink de Moedas/Conchas**.

## Bônus provisórios de sucesso

Bônus em **pontos percentuais**, somados à chance final.

| Barco | Bônus |
|---|---:|
| Água Mansa | +0 p.p. |
| Remo Valente | +3 p.p. |
| Rastro Azul | +6 p.p. |
| Proa Selvagem | +9 p.p. |
| Costa Nobre | +13 p.p. |
| Horizonte Dourado | +18 p.p. |

Manter o teto global de Chance de Sucesso definido pelo sistema de pesca.

---

# 6. DESBLOQUEIO DOS BARCOS

Proposta inicial:

| Barco | Nível do Pescador |
|---|---:|
| Água Mansa | 1 |
| Remo Valente | 10 |
| Rastro Azul | 20 |
| Proa Selvagem | 30 |
| Costa Nobre | 50 |
| Horizonte Dourado | 80 |

Objetivo:

- Nv.10 acompanha a chegada ao Rio Selvagem;
- Nv.20 acompanha o Pantanal Dourado;
- Nv.30 acompanha o Estuário das Marés;
- tiers altos ficam para a progressão futura.

Esses níveis devem ficar em configuração, não hardcoded.

---

# 7. PREÇOS DOS BARCOS

A economia atual do jogo deve ser a referência final.

Valores iniciais sugeridos:

| Barco | Preço sugerido |
|---|---:|
| Água Mansa | Grátis |
| Remo Valente | **12.000 Moedas** |
| Rastro Azul | **45.000 Moedas** |
| Proa Selvagem | **160.000 Moedas** |
| Costa Nobre | **600.000 Moedas + 120 Conchas** |
| Horizonte Dourado | **2.500.000 Moedas + 500 Conchas** |

## Regra importante para Claude

Antes de consolidar estes preços, comparar com a **economia real da build atual**.

O projeto atual é a fonte de verdade.

O preço precisa considerar:
- Moedas/h reais no mapa em que o item desbloqueia;
- taxa real de captura depois do sistema de sucesso/escape;
- quantidade de Conchas/h;
- gastos simultâneos com Vara;
- upgrades;
- Mercado;
- outros sinks existentes.

### Alvo de esforço econômico

Não ajustar por sensação isolada. Usar como referência aproximada:

| Tier | Tempo de economia desejável |
|---|---|
| Remo Valente | 20–40 min |
| Rastro Azul | 40–75 min |
| Proa Selvagem | 1–2 h |
| Costa Nobre | 3–5 h |
| Horizonte Dourado | compra de longo prazo |

Se a economia atual estiver diferente, **alterar o preço, não a economia inteira, só para encaixar esta tabela**.

### Regra de migração
Se o jogador já possuir um barco correspondente em save de desenvolvimento:
- preservar posse;
- não cobrar novamente;
- migrar apenas nome/configuração.

---

# 8. NOMES DEFINITIVOS — ISCAS

A família das Iscas usa nomes ligados ao ambiente de onde o atrativo vem.

| Item técnico | Nome visível definitivo | Tipo |
|---|---|---|
| Isca Simples | **Terra Viva** | minhoca |
| Isca Melhorada | **Maré Viva** | camarão |
| Isca Premium | **Ouro de Maré** | artificial dourada/turquesa |

---

# 9. PAPEL MECÂNICO DAS ISCAS

Iscas são consumíveis.

Elas:
- aumentam a Chance de Sucesso;
- duram um número de tentativas;
- gastam 1 carga por tentativa, mesmo se o peixe escapar;
- funcionam online e offline;
- não precisam ser reaplicadas a cada pesca.

Não adicionar nesta etapa:
- bônus de raridade;
- bônus de tamanho;
- bônus de XP;
- iscas específicas por espécie;
- múltiplas iscas simultâneas.

---

# 10. BÔNUS E CARGAS DAS ISCAS

Proposta inicial:

| Isca | Bônus de sucesso | Cargas |
|---|---:|---:|
| Terra Viva | +4 p.p. | 60 tentativas |
| Maré Viva | +8 p.p. | 60 tentativas |
| Ouro de Maré | +12 p.p. | 60 tentativas |

Com pesca online a cada 30 s, 60 cargas equivalem aproximadamente a 30 minutos de uso contínuo.

---

# 11. PREÇOS DAS ISCAS

Valores iniciais:

| Isca | Preço sugerido |
|---|---:|
| Terra Viva | **1.500 Moedas** |
| Maré Viva | **6.000 Moedas** |
| Ouro de Maré | **15.000 Moedas + 10 Conchas** |

A intenção é:

- Terra Viva: barata o suficiente para uso frequente;
- Maré Viva: escolha economicamente relevante;
- Ouro de Maré: consumível premium de gameplay, não dinheiro real.

## Verificação econômica obrigatória

Claude deve calcular o custo por hora de uso e comparar com Moedas/h do jogador.

Com 60 cargas:

- Terra Viva ≈ 3.000 Moedas/h;
- Maré Viva ≈ 12.000 Moedas/h;
- Ouro de Maré ≈ 30.000 Moedas/h + 20 Conchas/h.

Esses valores são apenas a referência inicial.

Se a geração real da build atual tornar alguma isca irrelevante ou impossível de sustentar, ajustar o **preço/cargas**, mantendo a hierarquia.

---

# 12. INTEGRAÇÃO COM A CHANCE DE CAPTURA

A Chance de Sucesso deve considerar:

```text
chance_base_da_raridade
+ bônus_da_vara
+ bônus_do_barco
+ bônus_da_isca
= chance_final
```

Depois aplicar piso/teto definidos na configuração.

Valores-base provisórios já definidos no documento do sistema:

| Raridade | Chance-base |
|---|---:|
| Comum | 50% |
| Raro | 38% |
| Épico | 24% |

Exemplo:

```text
Épico
24% base
+ 8% Maré Dourada Nv.1
+ 9% Proa Selvagem
+ 8% Maré Viva
= 49% de sucesso
```

Todos os valores devem continuar configuráveis.

---

# 13. RELAÇÃO COM MAPAS 3 E 4

## Pantanal Dourado — Nv.20
O jogador já pode:
- usar Ponta Selvagem;
- ter acesso ao Rastro Azul;
- usar Terra Viva / Maré Viva;
- começar a sentir necessidade real de melhorar sua capacidade de puxar peixes;
- encontrar Raros e o primeiro Épico.

O Barbado deve ser possível, mas ainda representar uma captura difícil.

## Estuário das Marés — Nv.30
O jogador passa a:
- precisar da Maré Dourada;
- poder adquirir Proa Selvagem;
- enfrentar dois Épicos;
- ter mais incentivo para consumir Iscas;
- gastar mais Moedas e, gradualmente, Conchas.

Camurupim e Mero devem funcionar como grandes testes de progressão de equipamento.

---

# 14. UI / LOJA

Na Loja, cada equipamento deve mostrar:

### Varas
- nome;
- nível;
- bônus de sucesso;
- bônus atuais já existentes;
- preço/upgrade;
- requisito de nível;
- mapas compatíveis.

### Barcos
- nome;
- miniatura;
- bônus de sucesso;
- preço;
- requisito de nível;
- adquirido/equipado.

### Iscas
- nome;
- miniatura;
- bônus de sucesso;
- número de cargas;
- preço;
- quantidade possuída.

Evitar texto excessivo.

Exemplo compacto:

> **Rastro Azul**  
> +6% Sucesso de Captura  
> Desbloqueia no Nv.20  
> 45.000 Moedas

---

# 15. DESCRIÇÕES CURTAS DOS BARCOS

### Água Mansa
> Uma canoa simples. Foi aqui que tudo começou.

### Remo Valente
> Mais firme e confiável para jornadas maiores.

### Rastro Azul
> Seu primeiro motor muda a forma de explorar as águas.

### Proa Selvagem
> Feito para pescadores que já enfrentam peixes de verdade.

### Costa Nobre
> Conforto e estabilidade para águas mais exigentes.

### Horizonte Dourado
> O auge da pesca esportiva. Potência, controle e presença.

---

# 16. DESCRIÇÕES CURTAS DAS ISCAS

### Terra Viva
> Simples, barata e eficiente. Um clássico que nunca sai de moda.

### Maré Viva
> Um atrativo melhor para peixes mais exigentes.

### Ouro de Maré
> Isca artificial de alto nível para maximizar suas chances.

---

# 17. PREÇOS DEVEM SER CONFIGURÁVEIS

Não hardcodear:

- custo de compra;
- nível de desbloqueio;
- bônus de sucesso;
- cargas;
- custo em Conchas;
- preço de upgrades;
- teto/piso de chance.

Claude deve colocar tudo nos mesmos padrões de configuração usados pelo jogo atual.

---

# 18. NÃO QUEBRAR O SAVE

Mudanças de nome são visuais.

Preservar:
- IDs das varas existentes;
- posse;
- nível interno;
- upgrades;
- moedas;
- conchas;
- peixes;
- progresso;
- mapa atual;
- cardume;
- aquário;
- Fishing Box.

Equipamentos novos devem ter defaults seguros em saves antigos.

Exemplo:
- barco ativo inexistente → `Água Mansa`;
- isca ativa inexistente → nenhuma;
- cargas inexistentes → 0.

---

# 19. BALANCEAMENTO OBRIGATÓRIO

Depois de implementar preços e bônus, executar simulações usando a build atual.

Validar pelo menos:

1. jogador Nv.10;
2. jogador Nv.20;
3. jogador Nv.30;
4. jogador Nv.40;
5. sem isca;
6. com cada isca;
7. barco disponível para aquele estágio;
8. vara mínima e vara evoluída.

Registrar:

- taxa de sucesso;
- escapes/h;
- capturas/h;
- XP/h;
- Moedas/h;
- Conchas/h;
- custo de consumíveis/h;
- tempo necessário para comprar próximo equipamento.

## Regra de decisão

Se houver conflito entre os números provisórios deste documento e a economia real da build:

> **preservar a progressão do jogo e ajustar os preços dos equipamentos.**

Não alterar preço de peixe, XP ou economia global apenas para justificar um preço de Barco/Isca.

---

# 20. RESUMO PARA IMPLEMENTAÇÃO

## Nomes finais

### Varas
**Caniço Manso → Ponta Selvagem → Maré Dourada**

### Barcos
**Água Mansa → Remo Valente → Rastro Azul → Proa Selvagem → Costa Nobre → Horizonte Dourado**

### Iscas
**Terra Viva → Maré Viva → Ouro de Maré**

## Preços-base

### Varas
- Caniço Manso: grátis
- Ponta Selvagem: manter preço atual da Vara 1
- Maré Dourada: 90.000 Moedas

### Barcos
- Água Mansa: grátis
- Remo Valente: 12.000
- Rastro Azul: 45.000
- Proa Selvagem: 160.000
- Costa Nobre: 600.000 + 120 Conchas
- Horizonte Dourado: 2.500.000 + 500 Conchas

### Iscas
- Terra Viva: 1.500 / 60 cargas
- Maré Viva: 6.000 / 60 cargas
- Ouro de Maré: 15.000 + 10 Conchas / 60 cargas

---

# 21. REGRA FINAL PARA CLAUDE

O objetivo desta implementação não é apenas trocar nomes.

Ela precisa fazer o jogador sentir:

> “Estou ganhando dinheiro para melhorar meu equipamento, e cada compra realmente aumenta minha capacidade de trazer peixes difíceis para o barco.”

Os nomes dão identidade.
Os equipamentos dão progressão.
Os preços dão propósito às Moedas e Conchas.
A melhoria da Chance de Sucesso precisa ser claramente percebida pelo jogador.

**A build atual e suas configurações são a fonte final de verdade para o balanceamento econômico.**
