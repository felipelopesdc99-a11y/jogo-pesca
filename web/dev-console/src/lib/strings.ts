/**
 * Todo o texto que aparece na tela do Development Console.
 *
 * Regra de idioma do projeto (docs/DECISOES.md, TD-014): tudo que um humano lê numa
 * interface é PT-BR. Comentários de código e logs técnicos ficam em inglês, porque são
 * lidos por quem programa, não pelo dono do projeto nem pelo jogador.
 *
 * Centralizar aqui tem um motivo prático: se um dia o painel precisar de um segundo idioma,
 * troca-se este arquivo e nada mais.
 */

import type { RoadmapStatus } from "./types";

/** Nomes dos cinco status do roadmap, como o dono deve lê-los. */
export const STATUS_LABELS: Record<RoadmapStatus, string> = {
  DONE: "Concluído",
  IN_PROGRESS: "Em andamento",
  BLOCKED: "Bloqueado",
  NEEDS_OWNER_DECISION: "Precisa da sua decisão",
  TODO: "A fazer",
};

/**
 * Nomes das áreas do projeto. As chaves são os valores gravados em roadmap.json e não
 * mudam com o idioma — só o rótulo exibido muda.
 */
export const SUBSYSTEM_LABELS: Record<string, string> = {
  repo: "repositório",
  docs: "documentação",
  config: "configuração",
  ops: "infraestrutura",
  server: "servidor",
  client: "cliente",
  web: "web",
};

export function statusLabel(status: string): string {
  return STATUS_LABELS[status as RoadmapStatus] ?? status;
}

export function subsystemLabel(subsystem: string): string {
  return SUBSYSTEM_LABELS[subsystem] ?? subsystem;
}

export const t = {
  app: {
    title: "Fishing Idle — Painel de Desenvolvimento",
    metaDescription:
      "Painel interno de desenvolvimento: roadmap, status de build e configuração do jogo.",
    brand: "Fishing Idle",
    subtitle: "Painel de Desenvolvimento",
    sidebarNote:
      "Este painel reflete o estado do repositório, não atividade em segundo plano. O status muda quando uma tarefa é concluída e o arquivo docs/roadmap.json é atualizado como parte dessa mesma mudança.",
    nav: {
      dashboard: "Visão geral",
      roadmap: "Roadmap",
      config: "Configuração do jogo",
      build: "Build / Versão",
    },
  },

  common: {
    sourceApi: "API do servidor",
    sourceFile: "arquivo do repositório",
    sourcePrefix: "Fonte:",
    readAt: "lido em",
    dependsOn: "depende de",
    updatedAt: "atualizado em",
    unknownPath: "caminho desconhecido",
    unavailable: "indisponível",
    none: "—",
    of: "de",
    tasks: "tarefas",
    done: "concluídas",
  },

  notices: {
    errorTitle: "Não foi possível ler o status",
    fallbackTitle: "Exibindo a partir do arquivo do repositório",
  },

  dashboard: {
    title: "Visão geral",
    lede: (project: string, version: string) =>
      `${project} — meta ${version}. O percentual considera todas as tarefas do roadmap da V0.1, dos Milestones 0 ao 12.`,
    completionLabel: "Conclusão da V0.1",
    completionDetail: (done: number, total: number) =>
      `${done} de ${total} tarefas concluídas`,
    currentMilestoneLabel: "Milestone atual",
    milestoneTaskDetail: (done: number, total: number, percentText: string) =>
      `${done} de ${total} tarefas (${percentText}%)`,
    currentBuildLabel: "Build atual",
    buildDetail: (build: number | undefined, environment: string | undefined) => {
      const buildPart = build !== undefined ? `build ${build}` : "build desconhecido";
      return environment ? `${buildPart} · ${environment}` : buildPart;
    },
    serverUnreachable: "Servidor inacessível.",
    attentionLabel: "Precisa da sua atenção",
    attentionNone: "Nada bloqueado, nenhuma decisão pendente",
    attentionSome: "Tarefas bloqueadas e decisões suas",
    sectionBlockers: "Bloqueios e decisões suas",
    emptyBlockers: "Nada está bloqueado e nenhuma decisão de design depende de você.",
    sectionInProgress: "Em andamento",
    emptyInProgress: "Nenhuma tarefa marcada como em andamento.",
    sectionNextUp: "Próximas da fila",
    emptyNextUp: "Nenhuma tarefa na fila.",
    sectionRecent: "Concluídas mais recentemente",
    emptyRecent: "Nada concluído ainda.",
  },

  roadmap: {
    title: "Roadmap",
    lede: "Todas as tarefas da V0.1 com status, dependências e notas de conclusão.",
    filterLabel: "Filtrar por status",
    filterAll: "Todas",
    showingOnly: "Mostrando apenas",
    showingOnlySuffix: ". Milestones sem tarefas nesse status ficam ocultos.",
    current: "atual",
    milestoneProgress: (done: number, total: number, percentText: string) =>
      `${done}/${total} concluídas · ${percentText}%`,
    criteriaTitle: "Critérios de conclusão",
    roadmapUpdatedAt: "roadmap atualizado em",
  },

  config: {
    title: "Configuração do jogo",
    lede: "Todos os valores de balanceamento ficam em /config, nunca dentro do código do jogo. Os atributos base valem para nível 1 e tamanho no percentil 0,50; os modificadores de raridade, tamanho e nível são aplicados por cima disso pelo servidor.",
    readOnlyTitle: "Somente leitura no Milestone 0",
    readOnlyBody:
      "Por enquanto, edite os arquivos em /config diretamente e reinicie o servidor. Todo arquivo está marcado como PROVISÓRIO: os números são um balanceamento inicial com hierarquia clara, a ser ajustado por simulação. A estrutura não é provisória.",
    tableFile: "Arquivo",
    tableOwns: "Responsável por",
    tableSchema: "Esquema",
    tableBalance: "Balanceamento",
    tableSize: "Tamanho",
    emptyFiles: (directory: string | null | undefined) =>
      directory
        ? `Nenhum arquivo de configuração encontrado em ${directory}.`
        : "Nenhum arquivo de configuração encontrado.",
    contentBrowserTitle: "Navegador de conteúdo",
    contentBrowserBody:
      "As tabelas e cards visuais de Peixes, Mapas, Varas e Expedições fazem parte do Milestone 1 (tarefa M1-T07), junto com a edição validada, o versionamento de configuração e o registro de auditoria. Até lá, os arquivos brutos estão linkados acima.",
  },

  build: {
    title: "Build / Versão",
    lede: "As versões de cada componente vêm do arquivo version.json na raiz do repositório, a fonte única da convenção descrita em docs/VERSIONAMENTO.md. Os dados de execução vêm do servidor rodando.",
    sectionComponents: "Versões dos componentes",
    tableComponent: "Componente",
    tableVersion: "Versão",
    componentNames: {
      client: "cliente (Unity)",
      server: "servidor (API)",
      web: "web (painel e site)",
    } as Record<string, string>,
    sectionBuild: "Build",
    buildNumber: "Número do build",
    unknownChannel: "canal desconhecido",
    buildDate: "Data do build",
    noCommit: "nenhum commit registrado",
    environment: "Ambiente",
    uptime: "Tempo no ar",
    sectionDownloads: "Downloads",
    tableChannel: "Canal",
    tableLink: "Link",
    windowsLabel: "Windows (download direto)",
    steamLabel: "Steam",
    noWindowsBuild: "Nenhum build ainda — o primeiro build para Windows é a tarefa M12-T09.",
    noSteam: "Espaço reservado até existir uma página na Steam — tarefa M11-T06.",
    sectionChangelog: "Histórico de mudanças",
    changelogBody: (milestone: string | null) =>
      `O histórico fica em docs/CHANGELOG.md e é atualizado a cada milestone.${
        milestone ? ` Milestone atual: ${milestone}.` : ""
      } Exibi-lo dentro deste painel faz parte do Milestone 11 (tarefa M11-T04), que também entrega a página pública de versão e notas de lançamento.`,
  },

  notFound: {
    title: "Página não encontrada",
    lede: "Este painel tem quatro seções:",
  },

  errors: {
    apiUnreachable: (url: string, reason: string) =>
      `A API em ${url} não respondeu (${reason}).`,
    apiTimedOut: (seconds: number) => `a API não respondeu em ${seconds}s`,
    noRepositoryRoot: (url: string, reason: string) =>
      `A API em ${url} não respondeu (${reason}) e a raiz do repositório não foi localizada para a leitura alternativa. Inicie o servidor com ops/scripts/dev-up.sh ou defina a variável FISHING_IDLE_REPO_ROOT.`,
    fallbackUsed: (url: string, reason: string) =>
      `A API em ${url} não respondeu (${reason}), então esta página foi montada lendo docs/roadmap.json diretamente. Os status estão atualizados; o que depende do servidor (saúde, dados de build, arquivos de configuração) está indisponível.`,
    bothFailed: (url: string, apiReason: string, filePath: string, fileReason: string) =>
      `A API em ${url} não respondeu (${apiReason}) e o arquivo ${filePath} não pôde ser lido (${fileReason}).`,
  },
} as const;

/** Formata uma data ISO como DD/MM/AAAA. Devolve null quando não há data. */
export function formatDate(value: string | null | undefined): string | null {
  if (!value) {
    return null;
  }
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return value;
  }
  return parsed.toLocaleDateString("pt-BR", { timeZone: "UTC" });
}

/** Formata um instante ISO como DD/MM/AAAA HH:MM (UTC). */
export function formatDateTime(value: string | null | undefined): string | null {
  if (!value) {
    return null;
  }
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return value;
  }
  return parsed.toLocaleString("pt-BR", {
    timeZone: "UTC",
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

/** Formata um número com vírgula decimal, como se escreve em português. */
export function formatNumber(value: number, decimals = 1): string {
  return value.toLocaleString("pt-BR", {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  });
}

/** Segundos em texto curto: "2h 13min", "13min 5s", "5s". */
export function formatUptime(seconds: number | undefined): string {
  if (seconds === undefined) {
    return t.common.none;
  }
  const total = Math.floor(seconds);
  const hours = Math.floor(total / 3600);
  const minutes = Math.floor((total % 3600) / 60);
  const rest = total % 60;

  if (hours > 0) {
    return `${hours}h ${minutes}min`;
  }
  return minutes > 0 ? `${minutes}min ${rest}s` : `${rest}s`;
}
