import { Notice, SourceNotices } from "@/components/Notice";
import { configFileHref, loadConfigListing } from "@/lib/data";
import { t } from "@/lib/strings";

export const dynamic = "force-dynamic";

function formatSize(bytes: number): string {
  return bytes < 1024 ? `${bytes} B` : `${(bytes / 1024).toFixed(1).replace(".", ",")} KB`;
}

export default async function ConfigPage() {
  const listing = await loadConfigListing();

  return (
    <>
      <h1 className="page-title">{t.config.title}</h1>
      <p className="page-lede">{t.config.lede}</p>

      <SourceNotices error={listing.error} warning={listing.warning} />

      {listing.data === null ? null : (
        <>
          <Notice kind="info" title={t.config.readOnlyTitle}>
            {listing.data.editing_note} {t.config.readOnlyBody}
          </Notice>

          <section className="section">
            <div className="card">
              <table className="table">
                <thead>
                  <tr>
                    <th>{t.config.tableFile}</th>
                    <th>{t.config.tableOwns}</th>
                    <th>{t.config.tableSchema}</th>
                    <th>{t.config.tableBalance}</th>
                    <th>{t.config.tableSize}</th>
                  </tr>
                </thead>
                <tbody>
                  {listing.data.files.map((file) => (
                    <tr key={file.name}>
                      <td className="mono">
                        <a href={configFileHref(listing.origin, file.name)}>{file.name}</a>
                      </td>
                      <td>{file.description ?? t.common.none}</td>
                      <td className="mono">{file.schema_version ?? t.common.none}</td>
                      <td>
                        <span className="chip">{file.balance_status ?? t.common.none}</span>
                      </td>
                      <td className="mono">{formatSize(file.size_bytes)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            {listing.data.files.length === 0 ? (
              <p className="empty">{t.config.emptyFiles(listing.data.directory)}</p>
            ) : null}
          </section>
        </>
      )}

      <section className="section">
        <h2 className="section-title">{t.config.contentBrowserTitle}</h2>
        <Notice kind="info">{t.config.contentBrowserBody}</Notice>
      </section>

      {listing.data?.directory ? (
        <div className="source-line mono">
          {t.common.sourcePrefix} {listing.data.directory}
        </div>
      ) : null}
    </>
  );
}
