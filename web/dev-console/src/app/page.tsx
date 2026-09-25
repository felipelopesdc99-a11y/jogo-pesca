import { SourceNotices } from "@/components/Notice";
import { ProgressBar, StatusBar } from "@/components/Progress";
import { TaskRefList } from "@/components/TaskCard";
import { loadBuildInfo, loadRoadmap } from "@/lib/data";

export const dynamic = "force-dynamic";

export default async function DashboardPage() {
  const [roadmap, build] = await Promise.all([loadRoadmap(), loadBuildInfo()]);

  if (roadmap.data === null) {
    return (
      <>
        <h1 className="page-title">Dashboard</h1>
        <SourceNotices error={roadmap.error} warning={roadmap.warning} />
      </>
    );
  }

  const summary = roadmap.data.summary;
  const currentMilestone = summary.milestones.find(
    (m) => m.milestone_id === summary.current_milestone,
  );
  const serverVersion = build.data?.components?.server ?? "unavailable";
  const buildNumber = build.data?.build?.number;
  const environment = build.data?.environment;

  return (
    <>
      <h1 className="page-title">Dashboard</h1>
      <p className="page-lede">
        {summary.project} — target {summary.target_version}. Completion is measured over every task
        in the V0.1 roadmap, Milestones 0 through 12.
      </p>

      <SourceNotices error={roadmap.error} warning={roadmap.warning} />

      <section className="section">
        <div className="grid grid-stats">
          <div className="card">
            <div className="stat-label">V0.1 completion</div>
            <div className="stat-value">{summary.completion_percent}%</div>
            <div className="stat-detail">
              {summary.done_tasks} of {summary.total_tasks} tasks done
            </div>
            <div style={{ marginTop: 12 }}>
              <ProgressBar percent={summary.completion_percent} />
            </div>
          </div>

          <div className="card">
            <div className="stat-label">Current milestone</div>
            <div className="stat-value">{summary.current_milestone}</div>
            <div className="stat-detail">{summary.current_milestone_title ?? "—"}</div>
            {currentMilestone ? (
              <>
                <div className="stat-detail">
                  {currentMilestone.done_tasks} of {currentMilestone.total_tasks} tasks (
                  {currentMilestone.completion_percent}%)
                </div>
                <div style={{ marginTop: 8 }}>
                  <StatusBar
                    counts={currentMilestone.status_counts}
                    total={currentMilestone.total_tasks}
                  />
                </div>
              </>
            ) : null}
          </div>

          <div className="card">
            <div className="stat-label">Current build</div>
            <div className="stat-value mono" style={{ fontSize: 18 }}>
              {serverVersion}
            </div>
            <div className="stat-detail">
              {buildNumber !== undefined ? `build ${buildNumber}` : "build unknown"}
              {environment ? ` · ${environment}` : ""}
            </div>
            {build.error ? <div className="stat-detail">Server unreachable.</div> : null}
          </div>

          <div className="card">
            <div className="stat-label">Needs your attention</div>
            <div className="stat-value">{summary.needs_attention.length}</div>
            <div className="stat-detail">
              {summary.needs_attention.length === 0
                ? "Nothing blocked, no decisions waiting"
                : "Blocked tasks and owner decisions"}
            </div>
          </div>
        </div>
      </section>

      <section className="section">
        <h2 className="section-title">Blockers and owner decisions</h2>
        <TaskRefList
          items={summary.needs_attention}
          emptyText="Nothing is blocked and no design decision is waiting on you."
          showDescription
        />
        {summary.open_owner_decisions.length > 0 ? (
          <div className="card" style={{ marginTop: 12 }}>
            {summary.open_owner_decisions.map((decision) => (
              <div key={decision.id} style={{ marginBottom: 8 }}>
                <div className="task-head">
                  <span className="task-id mono">{decision.id}</span>
                  <span className="task-title">{decision.title}</span>
                  {decision.task_id ? (
                    <span className="task-meta">
                      <span className="chip mono">{decision.task_id}</span>
                    </span>
                  ) : null}
                </div>
                <div className="task-body">{decision.detail}</div>
              </div>
            ))}
          </div>
        ) : null}
      </section>

      <div className="grid grid-two section">
        <div>
          <h2 className="section-title">In progress</h2>
          <TaskRefList
            items={summary.in_progress}
            emptyText="No task is currently marked in progress."
            showDescription
          />
        </div>

        <div>
          <h2 className="section-title">Next up</h2>
          <TaskRefList
            items={summary.next_up}
            emptyText="No queued tasks."
          />
        </div>
      </div>

      <section className="section">
        <h2 className="section-title">Most recently completed</h2>
        <TaskRefList items={summary.recently_completed} emptyText="Nothing completed yet." />
      </section>

      <div className="source-line mono">
        Source: {roadmap.origin === "api" ? "backend API" : "repository file"} ·{" "}
        {roadmap.data.source_path ?? "unknown path"} · read{" "}
        {new Date(roadmap.data.loaded_at_utc).toISOString().replace("T", " ").slice(0, 19)} UTC
      </div>
    </>
  );
}
