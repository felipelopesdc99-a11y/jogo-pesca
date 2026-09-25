import { Notice, SourceNotices } from "@/components/Notice";
import { apiBaseUrl, loadConfigListing } from "@/lib/data";

export const dynamic = "force-dynamic";

function formatSize(bytes: number): string {
  return bytes < 1024 ? `${bytes} B` : `${(bytes / 1024).toFixed(1)} KB`;
}

export default async function ConfigPage() {
  const listing = await loadConfigListing();

  return (
    <>
      <h1 className="page-title">Game Config</h1>
      <p className="page-lede">
        Every balance value the game runs on lives in <code>/config</code>, never in gameplay code.
        Base stats are stated at level 1 and size percentile 0.50; rarity, size and level modifiers
        are applied on top by the server.
      </p>

      <SourceNotices error={listing.error} warning={listing.warning} />

      {listing.data === null ? null : (
        <>
          <Notice kind="info" title="Read-only in Milestone 0">
            {listing.data.editing_note} Until then, edit the files in <code>/config</code> directly
            and restart the server. Every file is marked{" "}
            <code>&quot;balance_status&quot;: &quot;PROVISIONAL&quot;</code> — the numbers are
            placeholder-balanced with a clear hierarchy, to be tuned through simulation. The
            structure is not provisional.
          </Notice>

          <section className="section">
            <h2 className="section-title">Balance files</h2>
            <div className="card">
              <table className="table">
                <thead>
                  <tr>
                    <th>File</th>
                    <th>Owns</th>
                    <th>Schema</th>
                    <th>Balance</th>
                    <th>Size</th>
                  </tr>
                </thead>
                <tbody>
                  {listing.data.files.map((file) => (
                    <tr key={file.name}>
                      <td className="mono">
                        <a href={`${apiBaseUrl()}/api/dev/config/${file.name}`}>{file.name}</a>
                      </td>
                      <td>{file.description ?? "—"}</td>
                      <td className="mono">{file.schema_version ?? "—"}</td>
                      <td>
                        <span className="chip">{file.balance_status ?? "unknown"}</span>
                      </td>
                      <td className="mono">{formatSize(file.size_bytes)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            {listing.data.files.length === 0 ? (
              <p className="empty">
                No config files found{listing.data.directory ? ` in ${listing.data.directory}` : ""}.
              </p>
            ) : null}
          </section>
        </>
      )}

      <section className="section">
        <h2 className="section-title">Content Browser</h2>
        <Notice kind="info">
          Visual tables and cards for Fish, Maps, Rods and Expeditions are part of Milestone 1
          (task M1-T07), alongside validated config editing, config versioning and the admin audit
          trail. The raw files are linked above in the meantime.
        </Notice>
      </section>

      {listing.data?.directory ? (
        <div className="source-line mono">Source: {listing.data.directory}</div>
      ) : null}
    </>
  );
}
