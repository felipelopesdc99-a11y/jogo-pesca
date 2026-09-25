import { readFile } from "node:fs/promises";
import { existsSync } from "node:fs";
import path from "node:path";

import {
  gitHubLabel,
  isGitHubConfigured,
  listGitHubDirectory,
  readGitHubHeadCommit,
  readGitHubJson,
} from "./github";
import { t } from "./strings";
import { buildSummary } from "./summary";
import type {
  BuildInfo,
  DataOrigin,
  GameConfigFile,
  GameConfigListing,
  RoadmapDocument,
  RoadmapResponse,
  Sourced,
} from "./types";

const API_BASE_URL_SET = Boolean(process.env.FISHING_IDLE_API_BASE_URL);
const API_BASE_URL = (process.env.FISHING_IDLE_API_BASE_URL ?? "http://localhost:5080").replace(
  /\/+$/,
  "",
);

const REQUEST_TIMEOUT_MS = 4000;
const ROOT_MARKER_FILE = "version.json";
const MAX_WALK_UP_LEVELS = 8;

/** O painel nunca guarda cache do status: um painel desatualizado é pior que um painel lento. */
const NO_STORE: RequestInit = { cache: "no-store" };

export function apiBaseUrl(): string {
  return API_BASE_URL;
}

/**
 * Localiza a raiz do repositório para que a leitura alternativa consiga abrir
 * `docs/roadmap.json` diretamente. Defina FISHING_IDLE_REPO_ROOT quando o painel rodar
 * em um lugar de onde a busca para cima não alcança a raiz.
 */
export function repositoryRoot(): string | null {
  const configured = process.env.FISHING_IDLE_REPO_ROOT;
  if (configured && existsSync(path.join(configured, ROOT_MARKER_FILE))) {
    return configured;
  }

  let directory = process.cwd();
  for (let level = 0; level < MAX_WALK_UP_LEVELS; level++) {
    if (existsSync(path.join(directory, ROOT_MARKER_FILE))) {
      return directory;
    }
    const parent = path.dirname(directory);
    if (parent === directory) {
      break;
    }
    directory = parent;
  }

  return null;
}

/**
 * Em que ordem as fontes são tentadas.
 *
 * Rodando de um clone local, o backend vem primeiro porque é a única fonte com dados de execução,
 * e o arquivo em disco é a rede de segurança. Rodando publicado, sem clone e sem backend, só o
 * GitHub responde — e o backend só é tentado se alguém configurou um endereço de propósito, para
 * que a página não espere um tempo limite inútil a cada carregamento.
 */
export function sourceOrder(): DataOrigin[] {
  const hasCheckout = repositoryRoot() !== null;
  const order: DataOrigin[] = [];

  if (hasCheckout || API_BASE_URL_SET) {
    order.push("api");
  }
  if (hasCheckout) {
    order.push("repository-file");
  }
  if (isGitHubConfigured()) {
    order.push("github");
  }

  return order.length > 0 ? order : ["api"];
}

export function describeOrigin(origin: DataOrigin | null): string {
  switch (origin) {
    case "api":
      return t.common.sourceApi;
    case "repository-file":
      return t.common.sourceFile;
    case "github":
      return `${t.common.sourceGitHub} (${gitHubLabel()})`;
    default:
      return t.common.unavailable;
  }
}

async function fetchJson<T>(route: string): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${route}`, {
    ...NO_STORE,
    signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS),
    headers: { accept: "application/json" },
  });

  if (!response.ok) {
    throw new Error(`${route} respondeu ${response.status} ${response.statusText}`);
  }

  return (await response.json()) as T;
}

function describe(error: unknown): string {
  if (error instanceof Error) {
    return error.name === "TimeoutError"
      ? t.errors.apiTimedOut(REQUEST_TIMEOUT_MS / 1000)
      : error.message;
  }
  return String(error);
}

/** Uma falha registrada por fonte, para que o aviso na tela diga o que foi tentado. */
interface AttemptFailure {
  origin: DataOrigin;
  reason: string;
}

function summariseFailures(failures: AttemptFailure[]): string {
  return failures.map((f) => `${describeOrigin(f.origin)}: ${f.reason}`).join(" · ");
}

/**
 * Estado do roadmap, tentando cada fonte configurada na ordem de {@link sourceOrder}.
 *
 * O `origin` devolvido diz qual respondeu, para que o painel nunca fique desatualizado em
 * silêncio, e o `warning` explica quando não foi a fonte preferida.
 */
export async function loadRoadmap(): Promise<Sourced<RoadmapResponse>> {
  const failures: AttemptFailure[] = [];

  for (const origin of sourceOrder()) {
    try {
      const response = await loadRoadmapFrom(origin);
      return {
        data: response,
        origin,
        error: null,
        warning:
          failures.length > 0
            ? t.errors.fallbackUsed(describeOrigin(origin), summariseFailures(failures))
            : null,
      };
    } catch (error) {
      failures.push({ origin, reason: describe(error) });
    }
  }

  return {
    data: null,
    origin: null,
    error: t.errors.allSourcesFailed(summariseFailures(failures)),
    warning: null,
  };
}

async function loadRoadmapFrom(origin: DataOrigin): Promise<RoadmapResponse> {
  if (origin === "api") {
    return fetchJson<RoadmapResponse>("/api/dev/roadmap");
  }

  const document =
    origin === "github"
      ? await readGitHubJson<RoadmapDocument>("docs/roadmap.json")
      : await readRepositoryJson<RoadmapDocument>("docs", "roadmap.json");

  return {
    summary: buildSummary(document),
    roadmap: document,
    source_path: origin === "github" ? `${gitHubLabel()}:docs/roadmap.json` : repositoryPath("docs", "roadmap.json"),
    loaded_at_utc: new Date().toISOString(),
  };
}

function repositoryPath(...segments: string[]): string {
  const root = repositoryRoot();
  return root === null ? segments.join("/") : path.join(root, ...segments);
}

async function readRepositoryJson<T>(...segments: string[]): Promise<T> {
  const root = repositoryRoot();
  if (root === null) {
    throw new Error("não há um clone do repositório neste ambiente");
  }
  return JSON.parse(await readFile(path.join(root, ...segments), "utf8")) as T;
}

/**
 * Informações de build e versão.
 *
 * Só o backend conhece os dados de execução (ambiente, tempo no ar). As outras fontes entregam o
 * `version.json`, e a página deixa claro que a parte de execução não está disponível.
 */
export async function loadBuildInfo(): Promise<Sourced<BuildInfo>> {
  const failures: AttemptFailure[] = [];

  for (const origin of sourceOrder()) {
    try {
      if (origin === "api") {
        return { data: await fetchJson<BuildInfo>("/api/dev/version"), origin, error: null, warning: null };
      }

      const file =
        origin === "github"
          ? await readGitHubJson<BuildInfo>("version.json")
          : await readRepositoryJson<BuildInfo>("version.json");

      return {
        data: { ...file, version_file_available: true },
        origin,
        error: null,
        warning: t.errors.noRuntimeFacts(describeOrigin(origin)),
      };
    } catch (error) {
      failures.push({ origin, reason: describe(error) });
    }
  }

  return {
    data: null,
    origin: null,
    error: t.errors.allSourcesFailed(summariseFailures(failures)),
    warning: null,
  };
}

/** Os arquivos de balanceamento em /config. Somente leitura até o Milestone 1 entregar a edição validada. */
export async function loadConfigListing(): Promise<Sourced<GameConfigListing>> {
  const failures: AttemptFailure[] = [];

  for (const origin of sourceOrder()) {
    try {
      if (origin === "api") {
        return {
          data: await fetchJson<GameConfigListing>("/api/dev/config"),
          origin,
          error: null,
          warning: null,
        };
      }

      if (origin === "github") {
        return { data: await listConfigFromGitHub(), origin, error: null, warning: null };
      }

      failures.push({ origin, reason: "a listagem de /config vem da API ou do GitHub" });
    } catch (error) {
      failures.push({ origin, reason: describe(error) });
    }
  }

  return {
    data: null,
    origin: null,
    error: t.errors.allSourcesFailed(summariseFailures(failures)),
    warning: null,
  };
}

interface ConfigFileHeader {
  balance_status?: string;
  config_schema_version?: number;
  description?: string;
}

/**
 * The V0.1 balance files, used only when the directory listing is unavailable.
 *
 * The GitHub API allows 60 unauthenticated calls an hour per IP, and a shared hosting IP burns
 * through that quota on its own. Raw file reads are not subject to that limit, so when the listing
 * fails the panel reads these names directly and still shows the page. The listing, when it works,
 * always wins — so a config file added later appears without touching this list. If a new file is
 * added and the listing is unavailable, the page shows one fewer row, which is visible and
 * harmless, rather than a wrong number.
 */
const KNOWN_CONFIG_FILES = [
  "arena.json",
  "economy.json",
  "expeditions.json",
  "fish_catalog.json",
  "maps.json",
  "progression.json",
  "rods.json",
] as const;

async function describeConfigFile(name: string, sizeBytes: number | null): Promise<GameConfigFile | null> {
  let header: ConfigFileHeader;
  try {
    header = await readGitHubJson<ConfigFileHeader>(`config/${name}`);
  } catch {
    // Missing or unparseable: leave it out rather than showing a row with nothing in it.
    return null;
  }

  return {
    name,
    balance_status: header.balance_status ?? null,
    schema_version: header.config_schema_version ?? null,
    description: header.description ?? null,
    size_bytes: sizeBytes ?? 0,
    last_modified_utc: new Date().toISOString(),
  };
}

async function listConfigFromGitHub(): Promise<GameConfigListing> {
  let names: { name: string; size: number | null }[];

  try {
    names = (await listGitHubDirectory("config"))
      .filter((entry) => entry.name.endsWith(".json"))
      .map((entry) => ({ name: entry.name, size: entry.size }));
  } catch {
    names = KNOWN_CONFIG_FILES.map((name) => ({ name, size: null }));
  }

  const described = await Promise.all(names.map((n) => describeConfigFile(n.name, n.size)));
  const files = described.filter((file): file is GameConfigFile => file !== null);

  if (files.length === 0) {
    throw new Error("nenhum arquivo de configuração pôde ser lido");
  }

  files.sort((a, b) => a.name.localeCompare(b.name));

  return {
    files,
    directory: `${gitHubLabel()}:config`,
    editable: false,
    editing_note: t.config.editingNoteFallback,
  };
}

/** O commit que a branch aponta agora, para o painel dizer o que está refletindo. */
export async function loadHeadCommit() {
  return isGitHubConfigured() ? readGitHubHeadCommit() : null;
}

/** Endereço do arquivo de configuração para abrir em outra aba, conforme a fonte. */
export function configFileHref(origin: DataOrigin | null, fileName: string): string {
  if (origin === "github") {
    return `https://github.com/${process.env.FISHING_IDLE_GITHUB_REPO}/blob/${
      process.env.FISHING_IDLE_GITHUB_BRANCH ?? "main"
    }/config/${fileName}`;
  }
  return `${API_BASE_URL}/api/dev/config/${fileName}`;
}
