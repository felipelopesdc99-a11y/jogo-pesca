import type { RoadmapStatus } from "@/lib/types";

const LABELS: Record<RoadmapStatus, string> = {
  DONE: "Done",
  IN_PROGRESS: "In progress",
  BLOCKED: "Blocked",
  NEEDS_OWNER_DECISION: "Needs owner decision",
  TODO: "To do",
};

export function StatusBadge({ status }: { status: RoadmapStatus }) {
  return (
    <span className={`badge badge-${status.toLowerCase()}`}>{LABELS[status] ?? status}</span>
  );
}

export function statusLabel(status: string): string {
  return LABELS[status as RoadmapStatus] ?? status;
}
