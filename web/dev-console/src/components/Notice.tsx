import type { ReactNode } from "react";

import { t } from "@/lib/strings";

type NoticeKind = "error" | "warning" | "info";

/** Mostra um problema de origem de dados abertamente, em vez de deixar a página parecer confiável. */
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
        <Notice kind="error" title={t.notices.errorTitle}>
          {error}
        </Notice>
      ) : null}
      {warning ? (
        <Notice kind="warning" title={t.notices.fallbackTitle}>
          {warning}
        </Notice>
      ) : null}
    </>
  );
}
