import type { ReactNode } from "react";

type NoticeKind = "error" | "warning" | "info";

/** Surfaces a data-source problem plainly instead of letting the page look authoritative. */
export function Notice({
  kind,
  title,
  children,
}: {
  kind: NoticeKind;
  title?: string;
  children: ReactNode;
}) {
  return (
    <div className={`notice notice-${kind}`} role={kind === "error" ? "alert" : undefined}>
      {title ? <div className="notice-title">{title}</div> : null}
      <div>{children}</div>
    </div>
  );
}

export function SourceNotices({ error, warning }: { error: string | null; warning: string | null }) {
  return (
    <>
      {error ? (
        <Notice kind="error" title="Status could not be read">
          {error}
        </Notice>
      ) : null}
      {warning ? (
        <Notice kind="warning" title="Rendered from the repository file">
          {warning}
        </Notice>
      ) : null}
    </>
  );
}
