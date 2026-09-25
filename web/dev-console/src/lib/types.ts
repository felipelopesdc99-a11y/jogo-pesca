/**
 * Shapes served by the backend's dev endpoints.
 *
 * These mirror the C# records in server/src/FishingIdle.Api/Roadmap and the JSON files they
 * read, and they use the same snake_case spelling the API puts on the wire so no field has two
 * names across the project.
 */

export const ROADMAP_STATUSES = [
  "DONE",
  "IN_PROGRESS",
  "BLOCKED",
  "NEEDS_OWNER_DECISION",
  "TODO",
] as const;

export type RoadmapStatus = (typeof ROADMAP_STATUSES)[number];

export interface RoadmapTask {
  id: string;
  title: string;
  subsystem: string;
  description: string;
  status: RoadmapStatus;
  dependencies: string[];
  completion_notes?: string | null;
  updated_at?: string | null;
}

export interface RoadmapMilestone {
  id: string;
  title: string;
  goal: string;
  completion_criteria?: string[] | null;
  tasks: RoadmapTask[];
}

export interface RoadmapOwnerDecision {
  id: string;
  task_id?: string | null;
  title: string;
  detail: string;
  raised_at?: string | null;
  status: string;
  resolution?: string | null;
}

export interface RoadmapDocument {
  project: string;
  target_version: string;
  updated_at?: string | null;
  current_milestone: string;
  statuses?: string[] | null;
  notes?: string | null;
  owner_decisions?: RoadmapOwnerDecision[] | null;
  milestones: RoadmapMilestone[];
}

export interface RoadmapTaskRef {
  milestone_id: string;
  milestone_title: string;
  task: RoadmapTask;
}

export interface MilestoneProgress {
  milestone_id: string;
  milestone_title: string;
  total_tasks: number;
  done_tasks: number;
  completion_percent: number;
  status_counts: Record<string, number>;
}

export interface RoadmapSummary {
  project: string;
  target_version: string;
  current_milestone: string;
  current_milestone_title?: string | null;
  total_tasks: number;
  done_tasks: number;
  completion_percent: number;
  status_counts: Record<string, number>;
  milestones: MilestoneProgress[];
  in_progress: RoadmapTaskRef[];
  recently_completed: RoadmapTaskRef[];
  needs_attention: RoadmapTaskRef[];
  next_up: RoadmapTaskRef[];
  open_owner_decisions: RoadmapOwnerDecision[];
}

export interface RoadmapResponse {
  summary: RoadmapSummary;
  roadmap: RoadmapDocument;
  source_path?: string | null;
  loaded_at_utc: string;
}

export interface BuildInfo {
  environment?: string;
  server_time_utc?: string;
  process_started_utc?: string;
  uptime_seconds?: number;
  dotnet_version?: string;
  version_file_available?: boolean;
  version_file_error?: string;
  version_file_path?: string;
  project?: string;
  target_version?: string;
  current_milestone?: string;
  convention?: string;
  components?: Record<string, string>;
  build?: { number?: number; channel?: string; date?: string; commit?: string | null };
  downloads?: { windows?: string | null; steam?: string | null };
  notes?: string;
}

export interface GameConfigFile {
  name: string;
  balance_status?: string | null;
  schema_version?: number | null;
  description?: string | null;
  size_bytes: number;
  last_modified_utc: string;
}

export interface GameConfigListing {
  files: GameConfigFile[];
  directory?: string | null;
  editable: boolean;
  editing_note: string;
}

/**
 * Where a piece of data came from, so the console never renders stale state as if it were live.
 *
 * - `api`: the backend at FISHING_IDLE_API_BASE_URL. The only source with live runtime facts.
 * - `repository-file`: the checkout on disk. Used when the console runs from a local clone.
 * - `github`: the repository on GitHub. Used when the console is deployed and has no checkout.
 */
export type DataOrigin = "api" | "repository-file" | "github";

export interface Sourced<T> {
  data: T | null;
  origin: DataOrigin | null;
  /** Populated when neither the API nor the fallback could answer. */
  error: string | null;
  /** Populated when the API failed but the fallback answered. */
  warning: string | null;
}
