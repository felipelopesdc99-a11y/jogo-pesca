import { statusLabel } from "@/lib/strings";
import type { RoadmapStatus } from "@/lib/types";

export function StatusBadge({ status }: { status: RoadmapStatus }) {
  return <span className={`badge badge-${status.toLowerCase()}`}>{statusLabel(status)}</span>;
}
