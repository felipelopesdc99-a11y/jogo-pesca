import { describeOrigin } from "@/lib/data";
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
  const readAt = loadedAtUtc ? formatDateTime(loadedAtUtc) : null;

  return (
    <div className="source-line mono">
      {t.common.sourcePrefix} {describeOrigin(origin)} · {path ?? t.common.unknownPath}
      {readAt ? ` · ${t.common.readAt} ${readAt} UTC` : ""}
      {extra ? ` · ${extra}` : ""}
    </div>
  );
}
