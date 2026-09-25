import { ROADMAP_STATUSES } from "./types";
import type {
  MilestoneProgress,
  RoadmapDocument,
  RoadmapSummary,
  RoadmapTask,
  RoadmapTaskRef,
} from "./types";

const DASHBOARD_LIST_SIZE = 5;

/**
 * Mirror of `RoadmapService.BuildSummary` in the backend.
 *
 * The backend is the normal source of the summary. This exists only for the offline fallback path
 * (see `loadRoadmap`), so the owner can still read project status when the API is down. Both must
 * agree; the console labels which one answered so a disagreement is visible rather than silent.
 * If the backend's summary rules change, change this with it.
 */
export function buildSummary(document: RoadmapDocument): RoadmapSummary {
  const allTasks: RoadmapTaskRef[] = document.milestones.flatMap((milestone) =>
    milestone.tasks.map((task) => ({
      milestone_id: milestone.id,
      milestone_title: milestone.title,
      task,
    })),
  );

  const doneIds = new Set(
    allTasks.filter((x) => x.task.status === "DONE").map((x) => x.task.id),
  );

  const milestoneOrder = new Map(document.milestones.map((m, index) => [m.id, index]));
  const byRoadmapOrder = (a: RoadmapTaskRef, b: RoadmapTaskRef): number => {
    const left = milestoneOrder.get(a.milestone_id) ?? Number.MAX_SAFE_INTEGER;
    const right = milestoneOrder.get(b.milestone_id) ?? Number.MAX_SAFE_INTEGER;
    return left - right || a.task.id.localeCompare(b.task.id);
  };

  const total = allTasks.length;
  const done = doneIds.size;

  return {
    project: document.project,
    target_version: document.target_version,
    current_milestone: document.current_milestone,
    current_milestone_title:
      document.milestones.find((m) => m.id === document.current_milestone)?.title ?? null,
    total_tasks: total,
    done_tasks: done,
    completion_percent: total === 0 ? 0 : round1((done * 100) / total),
    status_counts: countByStatus(allTasks.map((x) => x.task)),
    milestones: document.milestones.map(milestoneProgress),
    in_progress: allTasks.filter((x) => x.task.status === "IN_PROGRESS").sort(byRoadmapOrder),
    recently_completed: allTasks
      .filter((x) => x.task.status === "DONE")
      .sort(
        (a, b) =>
          (b.task.updated_at ?? "").localeCompare(a.task.updated_at ?? "") ||
          b.task.id.localeCompare(a.task.id),
      )
      .slice(0, DASHBOARD_LIST_SIZE),
    needs_attention: allTasks
      .filter((x) => x.task.status === "BLOCKED" || x.task.status === "NEEDS_OWNER_DECISION")
      .sort(byRoadmapOrder),
    next_up: allTasks
      .filter((x) => x.task.status === "TODO")
      .sort((a, b) => {
        // A task whose dependencies are all done can be started now; show those first.
        const readyA = a.task.dependencies.every((d) => doneIds.has(d)) ? 0 : 1;
        const readyB = b.task.dependencies.every((d) => doneIds.has(d)) ? 0 : 1;
        return readyA - readyB || byRoadmapOrder(a, b);
      })
      .slice(0, DASHBOARD_LIST_SIZE),
    open_owner_decisions: (document.owner_decisions ?? []).filter(
      (d) => d.status.toUpperCase() !== "RESOLVED",
    ),
  };
}

function milestoneProgress(milestone: RoadmapDocument["milestones"][number]): MilestoneProgress {
  const total = milestone.tasks.length;
  const done = milestone.tasks.filter((t) => t.status === "DONE").length;

  return {
    milestone_id: milestone.id,
    milestone_title: milestone.title,
    total_tasks: total,
    done_tasks: done,
    completion_percent: total === 0 ? 0 : round1((done * 100) / total),
    status_counts: countByStatus(milestone.tasks),
  };
}

/** Counts every allowed status, including the ones at zero, so the legend stays stable. */
function countByStatus(tasks: RoadmapTask[]): Record<string, number> {
  const counts: Record<string, number> = {};
  for (const status of ROADMAP_STATUSES) {
    counts[status] = 0;
  }
  for (const task of tasks) {
    if (task.status in counts) {
      counts[task.status] = (counts[task.status] ?? 0) + 1;
    }
  }
  return counts;
}

function round1(value: number): number {
  return Math.round(value * 10) / 10;
}
