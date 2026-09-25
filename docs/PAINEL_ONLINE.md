# Painel online — acompanhar o desenvolvimento sem instalar nada

Este guia publica o Painel de Desenvolvimento em um endereço fixo na internet. Depois disso, você
abre um link no navegador — do computador ou do celular — e vê o andamento do projeto se atualizando
sozinho, poucos segundos depois de cada mudança enviada.

**Você não precisa instalar nenhum programa.** Só um navegador e duas contas gratuitas.

---

## Como funciona

```
  Claude conclui uma tarefa
        │
        ├─ atualiza o código E o arquivo docs/roadmap.json na mesma mudança
        │
        └─ envia para o GitHub
                 │
                 ↓
           O painel lê o GitHub
                 │
                 └─ sua aba do navegador relê sozinha a cada 60 segundos
```

O painel não inventa um "Claude está trabalhando agora". Ele mostra o estado real do repositório —
o que foi concluído, o que está em andamento, o que está bloqueado e o que vem a seguir — porque é
isso que se pode verificar. Essa regra é do próprio documento de design do jogo, seção 5.

---

## O que você vai precisar

| Conta | Para quê | Custo |
|---|---|---|
| **GitHub** | Você já tem — é onde o projeto mora | — |
| **Vercel** | Hospeda o painel | Gratuito no plano pessoal |

Nenhum programa, nenhum cartão de crédito.

---

## Passo a passo

### 1. Criar a conta na Vercel

Acesse **https://vercel.com/signup** e escolha **Continue with GitHub**.

Isso cria a conta já conectada ao seu GitHub, o que evita configurar acesso depois. Autorize quando
o GitHub pedir.

### 2. Importar o projeto

No painel da Vercel, clique em **Add New…** → **Project**.

Vai aparecer a lista dos seus repositórios do GitHub. Encontre **`jogo-pesca`** e clique em
**Import**.

> Se o repositório não aparecer, clique em **Adjust GitHub App Permissions** e autorize a Vercel a
> enxergar o `jogo-pesca`.

### 3. Dizer onde o painel está dentro do projeto

Esta é a única parte que exige atenção. O repositório tem várias pastas, e o painel é só uma delas.

Na tela de configuração que aparece:

- Procure **Root Directory** e clique em **Edit**.
- Escolha a pasta **`web/dev-console`**.

O restante (Framework Preset, comandos de build) a Vercel detecta sozinha como Next.js. Não mexa.

### 4. Informar de onde ler o andamento

Ainda na mesma tela, abra a seção **Environment Variables** e adicione estas duas:

| Name | Value |
|---|---|
| `FISHING_IDLE_GITHUB_REPO` | `felipelopesdc99-a11y/jogo-pesca` |
| `FISHING_IDLE_GITHUB_BRANCH` | `claude/v0-1-milestone-development-dxykdq` |

São elas que dizem ao painel qual repositório e qual branch acompanhar.

> **Não** adicione `FISHING_IDLE_API_BASE_URL`. Essa variável aponta para um servidor rodando na sua
> máquina, que a Vercel não alcança. Sem ela, o painel vai direto ao GitHub.

### 5. Publicar

Clique em **Deploy** e espere cerca de um minuto.

Ao terminar, a Vercel te dá um endereço parecido com:

```
https://jogo-pesca.vercel.app
```

**Esse é o seu painel.** Salve nos favoritos, no computador e no celular.

---

## Pronto. O que acontece a partir de agora

- Toda vez que uma tarefa é concluída e enviada, o painel passa a mostrar isso em até um minuto.
- Sua aba se atualiza sozinha a cada 60 segundos. No canto inferior esquerdo aparece o horário da
  última leitura e um botão **Atualizar agora**, se você não quiser esperar.
- A **Visão geral** mostra o commit que a branch aponta no momento, para você saber exatamente o que
  está vendo.

Você não precisa fazer mais nada. Não precisa atualizar a Vercel, nem reenviar nada.

---

## O que funciona e o que não funciona nesse modo

| Página | Publicado na Vercel | Rodando na sua máquina com `dev-up.sh` |
|---|---|---|
| Visão geral | ✅ completa | ✅ completa |
| Roadmap | ✅ completo | ✅ completo |
| Configuração do jogo | ✅ os arquivos e suas descrições | ✅ igual |
| Build / Versão | ⚠️ versões sim; ambiente, tempo no ar e horário do servidor não | ✅ completa |

O que falta no modo publicado são dados que **só existem quando há um servidor rodando** — não é
falha, e o painel diz isso na própria página. Para vê-los, rode o projeto na sua máquina.

---

## Perguntas que podem aparecer

**O painel fica público?**
Sim, quem tiver o link consegue abrir. Ele mostra apenas o andamento do projeto e os valores de
balanceamento, que já estão no repositório — que hoje é **público**. Não há segredo nem dado de
jogador envolvido. Se quiser restringir o acesso, a Vercel oferece proteção por senha no plano pago,
e o repositório pode ser tornado privado nas configurações do GitHub (nesse caso, veja a seção
seguinte).

**E se eu tornar o repositório privado?**
Aí o painel precisa de uma credencial para ler. Crie um token em
**https://github.com/settings/tokens** com permissão apenas de leitura de conteúdo, e adicione na
Vercel uma terceira variável: `FISHING_IDLE_GITHUB_TOKEN` com o valor do token.

**A página de configuração às vezes reclama de limite?**
O GitHub permite 60 consultas por hora sem credencial, e um endereço compartilhado como o da Vercel
pode consumir isso. O painel já contorna: quando o limite é atingido, ele lê os arquivos por outro
caminho que não tem esse limite. Se ainda assim incomodar, adicionar o
`FISHING_IDLE_GITHUB_TOKEN` acima resolve de vez.

**Quero mudar a frequência de atualização.**
Hoje são 60 segundos, definidos em `web/dev-console/src/app/layout.tsx`. É só pedir.

**Não quero publicar nada. Dá para ver só na minha máquina?**
Dá. É o caminho descrito no `README.md`: `npm run dev` dentro de `web/dev-console`. A diferença é
que ele lê o clone que está no seu computador, então você precisa rodar `git pull` para trazer as
novidades.

---

## Se algo der errado

| Sintoma | Causa provável | O que fazer |
|---|---|---|
| Erro de build na Vercel | O **Root Directory** não é `web/dev-console` | Settings → General → Root Directory → corrigir → Redeploy |
| "Nenhuma fonte respondeu" | Faltou `FISHING_IDLE_GITHUB_REPO`, ou o nome da branch está errado | Settings → Environment Variables → conferir → Redeploy |
| Página abre, mas os dados estão velhos | A aba está aberta há muito tempo sem foco | Clique em **Atualizar agora** |
| O repositório não aparece ao importar | A Vercel não tem permissão para vê-lo | **Adjust GitHub App Permissions** no GitHub |

Depois de alterar qualquer variável na Vercel, é preciso **Redeploy** (Deployments → o mais recente
→ menu `…` → Redeploy) para que ela passe a valer.
