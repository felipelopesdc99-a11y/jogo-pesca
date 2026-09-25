import { readFile } from "node:fs/promises";
import { existsSync } from "node:fs";
import path from "node:path";

import { buildSummary } from "./summary";
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

/** The console never caches project status: a stale panel is worse than a slow one. */
const NO_STORE: RequestInit = { cache: "no-store" };

export function apiBaseUrl(): string {
  return API_BASE_URL;
}

/**
 * Locates the repository root so the fallback can read `docs/roadmap.json` directly.
 * Set FISHING_IDLE_REPO_ROOT when the console runs somewhere the walk-up cannot reach it.
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
      ? `the API did not answer within ${REQUEST_TIMEOUT_MS / 1000}s`
      : error.message;
  }
  return String(error);
}

/**
 * Roadmap state, preferring the backend and falling back to the repository file.
 *
 * The backend is the normal path because it is also the source every other consumer reads.
 * The fallback exists so the owner can still see project status when the server is down —
 * and the returned `origin` says which one answered, so the panel is never silently stale.
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
        error:
          `The API at ${API_BASE_URL} could not be reached (${apiReason}), and the repository ` +
          `root could not be located for the fallback. Start the server with ops/scripts/dev-up.sh, ` +
          `or set FISHING_IDLE_REPO_ROOT.`,
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
        warning:
          `The API at ${API_BASE_URL} could not be reached (${apiReason}), so this page was ` +
          `rendered from docs/roadmap.json directly. Statuses are current; anything that needs ` +
          `the server (health, build runtime facts, config files) is unavailable.`,
      };
    } catch (fileError) {
      return {
        data: null,
        origin: null,
        error:
          `The API at ${API_BASE_URL} could not be reached (${apiReason}) and ${roadmapPath} ` +
          `could not be read (${describe(fileError)}).`,
        warning: null,
      };
    }
  }
}

/** Build and version information. Only the server can answer this, so there is no fallback. */
export async function loadBuildInfo(): Promise<Sourced<BuildInfo>> {
  try {
    const data = await fetchJson<BuildInfo>("/api/dev/version");
    return { data, origin: "api", error: null, warning: null };
  } catch (error) {
    return {
      data: null,
      origin: null,
      error: `The API at ${API_BASE_URL} could not be reached (${describe(error)}).`,
      warning: null,
    };
  }
}

/** The /config balance files. Read-only until Milestone 1 delivers validated editing. */
export async function loadConfigListing(): Promise<Sourced<GameConfigListing>> {
  try {
    const data = await fetchJson<GameConfigListing>("/api/dev/config");
    return { data, origin: "api", error: null, warning: null };
  } catch (error) {
    return {
      data: null,
      origin: null,
      error: `The API at ${API_BASE_URL} could not be reached (${describe(error)}).`,
      warning: null,
    };
  }
}
