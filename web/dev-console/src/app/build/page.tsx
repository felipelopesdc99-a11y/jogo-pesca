import { Notice, SourceNotices } from "@/components/Notice";
import { loadBuildInfo, loadRoadmap } from "@/lib/data";

export const dynamic = "force-dynamic";

function formatUptime(seconds: number | undefined): string {
  if (seconds === undefined) {
    return "—";
  }
  const total = Math.floor(seconds);
  const hours = Math.floor(total / 3600);
  const minutes = Math.floor((total % 3600) / 60);
  const rest = total % 60;
  return hours > 0 ? `${hours}h ${minutes}m` : minutes > 0 ? `${minutes}m ${rest}s` : `${rest}s`;
}

export default async function BuildPage() {
  const [build, roadmap] = await Promise.all([loadBuildInfo(), loadRoadmap()]);

  return (
    <>
      <h1 className="page-title">Build / Version</h1>
      <p className="page-lede">
        Component versions come from <code>version.json</code> at the repository root, the single
        source of truth for the convention documented in <code>docs/VERSIONING.md</code>. Runtime
        facts come from the running server.
      </p>

      <SourceNotices error={build.error} warning={build.warning} />

      {build.data === null ? null : (
        <>
          <section className="section">
            <h2 className="section-title">Component versions</h2>
            <div className="card">
              <table className="table">
                <thead>
                  <tr>
                    <th>Component</th>
                    <th>Version</th>
                  </tr>
                </thead>
                <tbody>
                  {Object.entries(build.data.components ?? {}).map(([component, version]) => (
                    <tr key={component}>
                      <td>{component}</td>
                      <td className="mono">{version}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>

          <section className="section">
            <h2 className="section-title">Build</h2>
            <div className="grid grid-stats">
              <div className="card">
                <div className="stat-label">Build number</div>
                <div className="stat-value">{build.data.build?.number ?? "—"}</div>
                <div className="stat-detail">{build.data.build?.channel ?? "unknown channel"}</div>
              </div>
              <div className="card">
                <div className="stat-label">Build date</div>
                <div className="stat-value" style={{ fontSize: 18 }}>
                  {build.data.build?.date ?? "—"}
                </div>
                <div className="stat-detail mono">{build.data.build?.commit ?? "no commit recorded"}</div>
              </div>
              <div className="card">
                <div className="stat-label">Environment</div>
                <div className="stat-value" style={{ fontSize: 18 }}>
                  {build.data.environment ?? "—"}
                </div>
                <div className="stat-detail">.NET {build.data.dotnet_version ?? "—"}</div>
              </div>
              <div className="card">
                <div className="stat-label">Server uptime</div>
                <div className="stat-value" style={{ fontSize: 18 }}>
                  {formatUptime(build.data.uptime_seconds)}
                </div>
                <div className="stat-detail mono">
                  {build.data.server_time_utc?.slice(0, 19).replace("T", " ") ?? "—"} UTC
                </div>
              </div>
            </div>
          </section>

          <section className="section">
            <h2 className="section-title">Downloads</h2>
            <div className="card">
              <table className="table">
                <thead>
                  <tr>
                    <th>Channel</th>
                    <th>Link</th>
                  </tr>
                </thead>
                <tbody>
                  <tr>
                    <td>Windows (direct download)</td>
                    <td>
                      {build.data.downloads?.windows ? (
                        <a className="mono" href={build.data.downloads.windows}>
                          {build.data.downloads.windows}
                        </a>
                      ) : (
                        <span className="empty">
                          No build yet — the first Windows build is task M12-T09.
                        </span>
                      )}
                    </td>
                  </tr>
                  <tr>
                    <td>Steam</td>
                    <td>
                      {build.data.downloads?.steam ? (
                        <a className="mono" href={build.data.downloads.steam}>
                          {build.data.downloads.steam}
                        </a>
                      ) : (
                        <span className="empty">
                          Placeholder until a Steam page exists — task M11-T06.
                        </span>
                      )}
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </section>
        </>
      )}

      <section className="section">
        <h2 className="section-title">Changelog</h2>
        <Notice kind="info">
          The changelog lives in <code>docs/CHANGELOG.md</code> and is updated with every milestone.
          {roadmap.data
            ? ` Current milestone: ${roadmap.data.summary.current_milestone} — ${
                roadmap.data.summary.current_milestone_title ?? ""
              }.`
            : ""}{" "}
          Rendering it in this panel is part of Milestone 11 (task M11-T04), which also delivers the
          public version and release-notes surface.
        </Notice>
      </section>
    </>
  );
}
