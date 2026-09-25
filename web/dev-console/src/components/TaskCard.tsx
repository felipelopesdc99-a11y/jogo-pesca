import { StatusBadge } from "./StatusBadge";
import type { RoadmapTask, RoadmapTaskRef } from "@/lib/types";

function formatDate(value: string | null | undefined): string | null {
  if (!value) {
    return null;
  }
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? value : parsed.toISOString().slice(0, 10);
}

export function TaskCard({
  task,
  milestoneId,
  showDescription = true,
}: {
  task: RoadmapTask;
  milestoneId?: string;
  showDescription?: boolean;
}) {
  const updated = formatDate(task.updated_at);

  return (
    <li className="task">
      <div className="task-head">
        <span className="task-id mono">{milestoneId ? `${task.id}` : task.id}</span>
        <span className="task-title">{task.title}</span>
        <span className="task-meta">
          <span className="chip">{task.subsystem}</span>
          <StatusBadge status={task.status} />
        </span>
      </div>

      {showDescription ? <div className="task-body">{task.description}</div> : null}

      {task.completion_notes ? <div className="task-notes">{task.completion_notes}</div> : null}

      {task.dependencies.length > 0 ? (
        <div className="task-deps mono">depends on {task.dependencies.join(", ")}</div>
      ) : null}

      {updated ? <div className="task-deps">updated {updated}</div> : null}
    </li>
  );
}

export function TaskRefList({
  items,
  emptyText,
  showDescription = false,
}: {
  items: RoadmapTaskRef[];
  emptyText: string;
  showDescription?: boolean;
}) {
  if (items.length === 0) {
    return <p className="empty">{emptyText}</p>;
  }

  return (
    <ul className="task-list">
      {items.map((item) => (
        <TaskCard
          key={item.task.id}
          task={item.task}
          milestoneId={item.milestone_id}
          showDescription={showDescription}
        />
      ))}
    </ul>
  );
}
