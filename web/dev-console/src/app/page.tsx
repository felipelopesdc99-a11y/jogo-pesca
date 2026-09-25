import { SourceNotices } from "@/components/Notice";
import { ProgressBar, StatusBar } from "@/components/Progress";
import { SourceLine } from "@/components/SourceLine";
import { TaskRefList } from "@/components/TaskCard";
import { loadBuildInfo, loadHeadCommit, loadRoadmap } from "@/lib/data";
import { formatDateTime, formatNumber, t } from "@/lib/strings";

export const dynamic = "force-dynamic";

export default async function DashboardPage() {
  const [roadmap, build, headCommit] = await Promise.all([
    loadRoadmap(),
    loadBuildInfo(),
    loadHeadCommit(),
  ]);

  if (roadmap.data === null) {
    return (
      <>
        <h1 className="page-title">{t.dashboard.title}</h1>
        <SourceNotices error={roadmap.error} warning={roadmap.warning} />
      </>
    );
  }

  const summary = roadmap.data.summary;
  const currentMilestone = summary.milestones.find(
    (m) => m.milestone_id === summary.current_milestone,
  );
  const serverVersion = build.data?.components?.server ?? t.common.unavailable;

  return (
    <>
      <h1 className="page-title">{t.dashboard.title}</h1>
      <p className="page-lede">{t.dashboard.lede(summary.project, summary.target_version)}</p>

      <SourceNotices error={roadmap.error} warning={roadmap.warning} />

      <section className="section">
        <div className="grid grid-stats">
          <div className="card">
            <div className="stat-label">{t.dashboard.completionLabel}</div>
            <div className="stat-value">{formatNumber(summary.completion_percent)}%</div>
            <div className="stat-detail">
              {t.dashboard.completionDetail(summary.done_tasks, summary.total_tasks)}
            </div>
            <div style={{ marginTop: 12 }}>
              <ProgressBar percent={summary.completion_percent} />
            </div>
          </div>

          <div className="card">
            <div className="stat-label">{t.dashboard.currentMilestoneLabel}</div>
            <div className="stat-value">{summary.current_milestone}</div>
            <div className="stat-detail">{summary.current_milestone_title ?? t.common.none}</div>
            {currentMilestone ? (
              <>
                <div className="stat-detail">
                  {t.dashboard.milestoneTaskDetail(
                    currentMilestone.done_tasks,
                    currentMilestone.total_tasks,
                    formatNumber(currentMilestone.completion_percent),
                  )}
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
            <div className="stat-label">{t.dashboard.currentBuildLabel}</div>
            <div className="stat-value mono" style={{ fontSize: 18 }}>
              {serverVersion}
            </div>
            <div className="stat-detail">
              {t.dashboard.buildDetail(build.data?.build?.number, build.data?.environment)}
            </div>
            {build.error ? (
              <div className="stat-detail">{t.dashboard.serverUnreachable}</div>
            ) : null}
          </div>

          <div className="card">
            <div className="stat-label">{t.dashboard.attentionLabel}</div>
            <div className="stat-value">{summary.needs_attention.length}</div>
            <div className="stat-detail">
              {summary.needs_attention.length === 0
                ? t.dashboard.attentionNone
                : t.dashboard.attentionSome}
            </div>
          </div>
        </div>
      </section>

      <section className="section">
        <h2 className="section-title">{t.dashboard.sectionBlockers}</h2>
        <TaskRefList
          items={summary.needs_attention}
          emptyText={t.dashboard.emptyBlockers}
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
          <h2 className="section-title">{t.dashboard.sectionInProgress}</h2>
          <TaskRefList
            items={summary.in_progress}
            emptyText={t.dashboard.emptyInProgress}
            showDescription
          />
        </div>

        <div>
          <h2 className="section-title">{t.dashboard.sectionNextUp}</h2>
          <TaskRefList items={summary.next_up} emptyText={t.dashboard.emptyNextUp} />
        </div>
      </div>

      <section className="section">
        <h2 className="section-title">{t.dashboard.sectionRecent}</h2>
        <TaskRefList items={summary.recently_completed} emptyText={t.dashboard.emptyRecent} />
      </section>

      {headCommit ? (
        <div className="commit-line mono">
          {t.refresh.commitPrefix} {headCommit.sha} · {headCommit.message} ·{" "}
          {formatDateTime(headCommit.committedAt)} UTC
        </div>
      ) : null}

      <SourceLine
        origin={roadmap.origin}
        path={roadmap.data.source_path}
        loadedAtUtc={roadmap.data.loaded_at_utc}
      />
    </>
  );
}
