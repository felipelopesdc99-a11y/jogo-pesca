import { ROADMAP_STATUSES } from "@/lib/types";
import type { RoadmapStatus } from "@/lib/types";

const SEGMENT_COLORS: Record<RoadmapStatus, string> = {
  DONE: "var(--done)",
  IN_PROGRESS: "var(--in-progress)",
  BLOCKED: "var(--blocked)",
  NEEDS_OWNER_DECISION: "var(--decision)",
  TODO: "var(--todo)",
};

export function ProgressBar({ percent }: { percent: number }) {
  const clamped = Math.min(100, Math.max(0, percent));
  return (
    <div
      className="bar"
      role="progressbar"
      aria-valuenow={clamped}
      aria-valuemin={0}
      aria-valuemax={100}
    >
      <div className="bar-fill" style={{ width: `${clamped}%` }} />
    </div>
  );
}

/**
 * A single bar split by status, so the owner sees at a glance how much is done, moving,
 * waiting on them, or untouched — not just one completion number.
 */
export function StatusBar({
  counts,
  total,
}: {
  counts: Record<string, number>;
  total: number;
}) {
  if (total === 0) {
    return <div className="bar" />;
  }

  return (
    <div className="bar-segments" title={describe(counts)}>
      {ROADMAP_STATUSES.map((status) => {
        const count = counts[status] ?? 0;
        if (count === 0) {
          return null;
        }
        return (
          <div
            key={status}
            className="bar-segment"
            style={{
              width: `${(count / total) * 100}%`,
              background: SEGMENT_COLORS[status],
            }}
          />
        );
      })}
    </div>
  );
}

function describe(counts: Record<string, number>): string {
  return ROADMAP_STATUSES.filter((status) => (counts[status] ?? 0) > 0)
    .map((status) => `${status}: ${counts[status]}`)
    .join("  ·  ");
}
