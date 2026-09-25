import { readFile } from "node:fs/promises";
import { existsSync } from "node:fs";
import path from "node:path";

import { buildSummary } from "./summary";
import { t } from "./strings";
import type {
  BuildInfo,
  GameConfigListing,
  RoadmapDocument,
  RoadmapResponse,
  Sourced,
} from "./types";

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

async function fetchJson<T>(route: string): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${route}`, {
    ...NO_STORE,
    signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS),
    headers: { accept: "application/json" },
  });

  if (!response.ok) {
    throw new Error(`${route} answered ${response.status} ${response.statusText}`);
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

/**
 * Estado do roadmap, preferindo o backend e recorrendo ao arquivo do repositório.
 *
 * O backend é o caminho normal porque é também a fonte que todos os outros consumidores leem.
 * A leitura alternativa existe para que o dono ainda veja o status quando o servidor estiver
 * fora do ar — e o `origin` devolvido diz qual das duas respondeu, para que o painel nunca
 * fique desatualizado em silêncio.
 */
export async function loadRoadmap(): Promise<Sourced<RoadmapResponse>> {
  try {
    const data = await fetchJson<RoadmapResponse>("/api/dev/roadmap");
    return { data, origin: "api", error: null, warning: null };
  } catch (apiError) {
    const apiReason = describe(apiError);

    const root = repositoryRoot();
    if (root === null) {
      return {
        data: null,
        origin: null,
        error: t.errors.noRepositoryRoot(API_BASE_URL, apiReason),
        warning: null,
      };
    }

    const roadmapPath = path.join(root, "docs", "roadmap.json");
    try {
      const document = JSON.parse(await readFile(roadmapPath, "utf8")) as RoadmapDocument;

      return {
        data: {
          summary: buildSummary(document),
          roadmap: document,
          source_path: roadmapPath,
          loaded_at_utc: new Date().toISOString(),
        },
        origin: "repository-file",
        error: null,
        warning: t.errors.fallbackUsed(API_BASE_URL, apiReason),
      };
    } catch (fileError) {
      return {
        data: null,
        origin: null,
        error: t.errors.bothFailed(API_BASE_URL, apiReason, roadmapPath, describe(fileError)),
        warning: null,
      };
    }
  }
}

/** Informações de build e versão. Só o servidor responde isso, então não há leitura alternativa. */
export async function loadBuildInfo(): Promise<Sourced<BuildInfo>> {
  try {
    const data = await fetchJson<BuildInfo>("/api/dev/version");
    return { data, origin: "api", error: null, warning: null };
  } catch (error) {
    return {
      data: null,
      origin: null,
      error: t.errors.apiUnreachable(API_BASE_URL, describe(error)),
      warning: null,
    };
  }
}

/** Os arquivos de balanceamento em /config. Somente leitura até o Milestone 1 entregar a edição validada. */
export async function loadConfigListing(): Promise<Sourced<GameConfigListing>> {
  try {
    const data = await fetchJson<GameConfigListing>("/api/dev/config");
    return { data, origin: "api", error: null, warning: null };
  } catch (error) {
    return {
      data: null,
      origin: null,
      error: t.errors.apiUnreachable(API_BASE_URL, describe(error)),
      warning: null,
    };
  }
}
