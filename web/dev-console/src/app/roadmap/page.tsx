import { SourceNotices } from "@/components/Notice";
import { StatusBar } from "@/components/Progress";
import { StatusBadge } from "@/components/StatusBadge";
import { TaskCard } from "@/components/TaskCard";
import { loadRoadmap } from "@/lib/data";
import { ROADMAP_STATUSES } from "@/lib/types";
import type { RoadmapStatus } from "@/lib/types";

export const dynamic = "force-dynamic";

/** `?status=IN_PROGRESS` narrows the page to one status. */
function parseStatusFilter(value: string | string[] | undefined): RoadmapStatus | null {
  const candidate = Array.isArray(value) ? value[0] : value;
  if (!candidate) {
    return null;
  }
  const upper = candidate.toUpperCase() as RoadmapStatus;
  return ROADMAP_STATUSES.includes(upper) ? upper : null;
}

export default async function RoadmapPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const [roadmap, params] = await Promise.all([loadRoadmap(), searchParams]);
  const filter = parseStatusFilter(params.status);

  if (roadmap.data === null) {
    return (
      <>
        <h1 className="page-title">Roadmap</h1>
        <SourceNotices error={roadmap.error} warning={roadmap.warning} />
      </>
    );
  }

  const { summary, roadmap: document } = roadmap.data;
  const progressById = new Map(summary.milestones.map((m) => [m.milestone_id, m]));

  return (
    <>
      <h1 className="page-title">Roadmap</h1>
      <p className="page-lede">
        Every V0.1 task with its status, dependencies and completion notes. Statuses are{" "}
        <code>TODO</code>, <code>IN_PROGRESS</code>, <code>DONE</code>, <code>BLOCKED</code> and{" "}
        <code>NEEDS_OWNER_DECISION</code>.
      </p>

      <SourceNotices error={roadmap.error} warning={roadmap.warning} />

      <section className="section">
        <div className="card">
          <div className="task-head" style={{ marginBottom: 10 }}>
            <span className="stat-label" style={{ margin: 0 }}>
              Filter by status
            </span>
            <span className="task-meta">
              <a className="chip" href="/roadmap">
                All ({summary.total_tasks})
              </a>
              {ROADMAP_STATUSES.map((status) => (
                <a key={status} className="chip" href={`/roadmap?status=${status}`}>
                  {status} ({summary.status_counts[status] ?? 0})
                </a>
              ))}
            </span>
          </div>
          <StatusBar counts={summary.status_counts} total={summary.total_tasks} />
        </div>
      </section>

      {filter ? (
        <p className="page-lede">
          Showing <StatusBadge status={filter} /> only. Milestones with no matching task are hidden.
        </p>
      ) : null}

      {document.milestones.map((milestone) => {
        const tasks = filter
          ? milestone.tasks.filter((task) => task.status === filter)
          : milestone.tasks;

        if (tasks.length === 0) {
          return null;
        }

        const progress = progressById.get(milestone.id);
        const isCurrent = milestone.id === summary.current_milestone;

        return (
          <section key={milestone.id} className="milestone">
            <div className="milestone-head">
              <span className="milestone-id">{milestone.id}</span>
              <span className="milestone-title">{milestone.title}</span>
              {isCurrent ? <span className="chip">current</span> : null}
              {progress ? (
                <span className="milestone-count">
                  {progress.done_tasks}/{progress.total_tasks} done ·{" "}
                  {progress.completion_percent}%
                </span>
              ) : null}
            </div>

            <p className="milestone-goal">{milestone.goal}</p>

            {progress ? (
              <StatusBar counts={progress.status_counts} total={progress.total_tasks} />
            ) : null}

            {milestone.completion_criteria && milestone.completion_criteria.length > 0 ? (
              <div className="criteria">
                <div className="stat-label" style={{ marginBottom: 0 }}>
                  Completion criteria
                </div>
                <ul>
                  {milestone.completion_criteria.map((criterion) => (
                    <li key={criterion}>{criterion}</li>
                  ))}
                </ul>
              </div>
            ) : null}

            <ul className="task-list" style={{ marginTop: 14 }}>
              {tasks.map((task) => (
                <TaskCard key={task.id} task={task} milestoneId={milestone.id} />
              ))}
            </ul>
          </section>
        );
      })}

      <div className="source-line mono">
        Source: {roadmap.origin === "api" ? "backend API" : "repository file"} ·{" "}
        {roadmap.data.source_path ?? "unknown path"}
        {document.updated_at ? ` · roadmap updated_at ${document.updated_at}` : ""}
      </div>
    </>
  );
}
