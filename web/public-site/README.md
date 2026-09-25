# web/public-site — site voltado ao jogador

**Ainda não construído.** É o Milestone 11 (tarefas `M11-T01` a `M11-T06`), e esta pasta existe para
que o layout do monorepo da seção 4 do GDD fique completo.

## O que vai entregar

- Uma página inicial atraente, alinhada à identidade visual do jogo, com uma cena de pesca em destaque
- Uma explicação curta do jogo
- Uma seção de capturas de tela e vídeo
- Um botão **Baixar para Windows**, ligado ao build versionado atual
- Um botão e sistema de link da Steam, como espaço reservado até existir a página na Steam
- Versão e notas de lançamento, requisitos mínimos, privacidade e termos

## Decisões já tomadas

- **Next.js + TypeScript**, a mesma base do Painel de Desenvolvimento (seção 3 do GDD).
- **O download direto e a Steam saem da mesma base de código e do mesmo `version.json`.** O endpoint
  público de dados do build (`M11-T05`) foi projetado para que um atualizador leve possa lê-lo
  depois sem retrabalho — é por isso que a informação de versão é uma resposta de API, e não uma
  string embutida no cliente.
- **Sem instalador próprio complexo no primeiro milestone.** Um build versionado para download basta
  até a atualização automática ser realmente necessária (seção 6 do GDD).
- **O site é voltado ao jogador brasileiro, então todo o conteúdo é em PT-BR**, seguindo a mesma
  regra do resto do projeto (`docs/DECISOES.md`, TD-014).

Nada aqui deve começar antes do Milestone 11, a menos que o dono mude a prioridade.
