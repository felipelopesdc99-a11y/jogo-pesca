import { formatDateTime, t } from "@/lib/strings";
import type { DataOrigin } from "@/lib/types";

/** Diz de onde veio o que está na tela, para que a página nunca pareça atual sem ser. */
export function SourceLine({
  origin,
  path,
  loadedAtUtc,
  extra,
}: {
  origin: DataOrigin | null;
  path?: string | null;
  loadedAtUtc?: string;
  extra?: string;
}) {
  const source = origin === "api" ? t.common.sourceApi : t.common.sourceFile;
  const readAt = loadedAtUtc ? formatDateTime(loadedAtUtc) : null;

  return (
    <div className="source-line mono">
      {t.common.sourcePrefix} {source} · {path ?? t.common.unknownPath}
      {readAt ? ` · ${t.common.readAt} ${readAt} UTC` : ""}
      {extra ? ` · ${extra}` : ""}
    </div>
  );
}
