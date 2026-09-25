import { SourceNotices } from "@/components/Notice";
import { StatusBar } from "@/components/Progress";
import { SourceLine } from "@/components/SourceLine";
import { StatusBadge } from "@/components/StatusBadge";
import { TaskCard } from "@/components/TaskCard";
import { loadRoadmap } from "@/lib/data";
import { formatNumber, statusLabel, t } from "@/lib/strings";
import { ROADMAP_STATUSES } from "@/lib/types";
import type { RoadmapStatus } from "@/lib/types";

export const dynamic = "force-dynamic";

/** `?status=IN_PROGRESS` restringe a página a um único status. */
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
        <h1 className="page-title">{t.roadmap.title}</h1>
        <SourceNotices error={roadmap.error} warning={roadmap.warning} />
      </>
    );
  }

  const { summary, roadmap: document } = roadmap.data;
  const progressById = new Map(summary.milestones.map((m) => [m.milestone_id, m]));

  return (
    <>
      <h1 className="page-title">{t.roadmap.title}</h1>
      <p className="page-lede">{t.roadmap.lede}</p>

      <SourceNotices error={roadmap.error} warning={roadmap.warning} />

      <section className="section">
        <div className="card">
          <div className="task-head" style={{ marginBottom: 10 }}>
            <span className="stat-label" style={{ margin: 0 }}>
              {t.roadmap.filterLabel}
            </span>
            <span className="task-meta">
              <a className="chip" href="/roadmap">
                {t.roadmap.filterAll} ({summary.total_tasks})
              </a>
              {ROADMAP_STATUSES.map((status) => (
                <a key={status} className="chip" href={`/roadmap?status=${status}`}>
                  {statusLabel(status)} ({summary.status_counts[status] ?? 0})
                </a>
              ))}
            </span>
          </div>
          <StatusBar counts={summary.status_counts} total={summary.total_tasks} />
        </div>
      </section>

      {filter ? (
        <p className="page-lede">
          {t.roadmap.showingOnly} <StatusBadge status={filter} />
          {t.roadmap.showingOnlySuffix}
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
              {isCurrent ? <span className="chip">{t.roadmap.current}</span> : null}
              {progress ? (
                <span className="milestone-count">
                  {t.roadmap.milestoneProgress(
                    progress.done_tasks,
                    progress.total_tasks,
                    formatNumber(progress.completion_percent),
                  )}
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
                  {t.roadmap.criteriaTitle}
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
                <TaskCard key={task.id} task={task} />
              ))}
            </ul>
          </section>
        );
      })}

      <SourceLine
        origin={roadmap.origin}
        path={roadmap.data.source_path}
        extra={
          document.updated_at
            ? `${t.roadmap.roadmapUpdatedAt} ${document.updated_at.slice(0, 10)}`
            : undefined
        }
      />
    </>
  );
}
