import { StatusBadge } from "./StatusBadge";
import { formatDate, subsystemLabel, t } from "@/lib/strings";
import type { RoadmapTask, RoadmapTaskRef } from "@/lib/types";

export function TaskCard({
  task,
  showDescription = true,
}: {
  task: RoadmapTask;
  showDescription?: boolean;
}) {
  const updated = formatDate(task.updated_at);

  return (
    <li className="task">
      <div className="task-head">
        <span className="task-id mono">{task.id}</span>
        <span className="task-title">{task.title}</span>
        <span className="task-meta">
          <span className="chip">{subsystemLabel(task.subsystem)}</span>
          <StatusBadge status={task.status} />
        </span>
      </div>

      {showDescription ? <div className="task-body">{task.description}</div> : null}

      {task.completion_notes ? <div className="task-notes">{task.completion_notes}</div> : null}

      {task.dependencies.length > 0 ? (
        <div className="task-deps mono">
          {t.common.dependsOn} {task.dependencies.join(", ")}
        </div>
      ) : null}

      {updated ? (
        <div className="task-deps">
          {t.common.updatedAt} {updated}
        </div>
      ) : null}
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
        <TaskCard key={item.task.id} task={item.task} showDescription={showDescription} />
      ))}
    </ul>
  );
}
