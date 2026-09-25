/**
 * Reads the project's own repository from GitHub.
 *
 * This is what lets the console run somewhere with no checkout and no backend — deployed on a host
 * like Vercel — and still show current status: it reads the same `docs/roadmap.json` that every
 * other source reads, straight from the repository, seconds after a commit is pushed.
 */

const REPO = process.env.FISHING_IDLE_GITHUB_REPO ?? "";
const BRANCH = process.env.FISHING_IDLE_GITHUB_BRANCH ?? "main";

/** Only needed for a private repository. A public one is read without any credential. */
const TOKEN = process.env.FISHING_IDLE_GITHUB_TOKEN ?? "";

const REQUEST_TIMEOUT_MS = 8000;

/**
 * How long a directory listing may be reused.
 *
 * Raw file reads are never cached, because roadmap status is the whole point of the panel.
 * The listing of `/config` is a different matter: it changes rarely, and it is the only call that
 * goes through the rate-limited GitHub API, so caching it keeps an auto-refreshing panel well
 * inside the unauthenticated limit.
 */
const LISTING_REVALIDATE_SECONDS = 300;

export function isGitHubConfigured(): boolean {
  return REPO.trim().length > 0;
}

export function gitHubRepo(): string {
  return REPO;
}

export function gitHubBranch(): string {
  return BRANCH;
}

/** Human-readable source label, e.g. `owner/repo@branch`. */
export function gitHubLabel(): string {
  return `${REPO}@${BRANCH}`;
}

function headers(accept: string): HeadersInit {
  const value: Record<string, string> = { accept };
  if (TOKEN) {
    value.authorization = `Bearer ${TOKEN}`;
  }
  return value;
}

function rawUrl(path: string): string {
  return `https://raw.githubusercontent.com/${REPO}/${BRANCH}/${path}`;
}

/** Fetches and parses one file from the repository. Never cached. */
export async function readGitHubJson<T>(path: string): Promise<T> {
  const response = await fetch(rawUrl(path), {
    cache: "no-store",
    signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS),
    headers: headers("application/json"),
  });

  if (!response.ok) {
    throw new Error(
      response.status === 404
        ? `${path} não existe em ${gitHubLabel()}`
        : `${path} respondeu ${response.status} ${response.statusText}`,
    );
  }

  return (await response.json()) as T;
}

export interface GitHubDirectoryEntry {
  name: string;
  path: string;
  size: number;
  type: string;
}

/**
 * Lists a directory through the GitHub API.
 *
 * This is the rate-limited call (60/hour per IP without a token), so its result is cached for
 * {@link LISTING_REVALIDATE_SECONDS}.
 */
export async function listGitHubDirectory(path: string): Promise<GitHubDirectoryEntry[]> {
  const url = `https://api.github.com/repos/${REPO}/contents/${path}?ref=${encodeURIComponent(BRANCH)}`;

  const response = await fetch(url, {
    next: { revalidate: LISTING_REVALIDATE_SECONDS },
    signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS),
    headers: headers("application/vnd.github+json"),
  });

  if (!response.ok) {
    throw new Error(
      response.status === 403
        ? "o limite de requisições do GitHub foi atingido; configure FISHING_IDLE_GITHUB_TOKEN"
        : `a listagem de ${path} respondeu ${response.status} ${response.statusText}`,
    );
  }

  const entries = (await response.json()) as GitHubDirectoryEntry[];
  return entries.filter((entry) => entry.type === "file");
}

/** The commit the branch currently points at, used to show what the panel is reflecting. */
export async function readGitHubHeadCommit(): Promise<{
  sha: string;
  message: string;
  committedAt: string;
} | null> {
  try {
    const url = `https://api.github.com/repos/${REPO}/commits/${encodeURIComponent(BRANCH)}`;
    const response = await fetch(url, {
      next: { revalidate: 30 },
      signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS),
      headers: headers("application/vnd.github+json"),
    });

    if (!response.ok) {
      return null;
    }

    const body = (await response.json()) as {
      sha: string;
      commit: { message: string; committer: { date: string } };
    };

    return {
      sha: body.sha.slice(0, 7),
      // Only the subject line; commit bodies in this project are long by design.
      message: body.commit.message.split("\n")[0] ?? "",
      committedAt: body.commit.committer.date,
    };
  } catch {
    // The head commit is a nicety. Never let it break the page.
    return null;
  }
}
