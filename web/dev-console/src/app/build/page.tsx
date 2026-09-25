import { Notice, SourceNotices } from "@/components/Notice";
import { loadBuildInfo, loadRoadmap } from "@/lib/data";
import { formatDateTime, formatUptime, t } from "@/lib/strings";

export const dynamic = "force-dynamic";

export default async function BuildPage() {
  const [build, roadmap] = await Promise.all([loadBuildInfo(), loadRoadmap()]);

  const currentMilestone = roadmap.data
    ? `${roadmap.data.summary.current_milestone} — ${
        roadmap.data.summary.current_milestone_title ?? ""
      }`.trim()
    : null;

  return (
    <>
      <h1 className="page-title">{t.build.title}</h1>
      <p className="page-lede">{t.build.lede}</p>

      <SourceNotices error={build.error} warning={build.warning} />

      {build.data === null ? null : (
        <>
          <section className="section">
            <h2 className="section-title">{t.build.sectionComponents}</h2>
            <div className="card">
              <table className="table">
                <thead>
                  <tr>
                    <th>{t.build.tableComponent}</th>
                    <th>{t.build.tableVersion}</th>
                  </tr>
                </thead>
                <tbody>
                  {Object.entries(build.data.components ?? {}).map(([component, version]) => (
                    <tr key={component}>
                      <td>{t.build.componentNames[component] ?? component}</td>
                      <td className="mono">{version}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>

          <section className="section">
            <h2 className="section-title">{t.build.sectionBuild}</h2>
            <div className="grid grid-stats">
              <div className="card">
                <div className="stat-label">{t.build.buildNumber}</div>
                <div className="stat-value">{build.data.build?.number ?? t.common.none}</div>
                <div className="stat-detail">
                  {build.data.build?.channel ?? t.build.unknownChannel}
                </div>
              </div>
              <div className="card">
                <div className="stat-label">{t.build.buildDate}</div>
                <div className="stat-value" style={{ fontSize: 18 }}>
                  {build.data.build?.date
                    ? build.data.build.date.split("-").reverse().join("/")
                    : t.common.none}
                </div>
                <div className="stat-detail mono">
                  {build.data.build?.commit ?? t.build.noCommit}
                </div>
              </div>
              <div className="card">
                <div className="stat-label">{t.build.environment}</div>
                <div className="stat-value" style={{ fontSize: 18 }}>
                  {build.data.environment ?? t.common.none}
                </div>
                <div className="stat-detail">.NET {build.data.dotnet_version ?? t.common.none}</div>
              </div>
              <div className="card">
                <div className="stat-label">{t.build.uptime}</div>
                <div className="stat-value" style={{ fontSize: 18 }}>
                  {formatUptime(build.data.uptime_seconds)}
                </div>
                <div className="stat-detail mono">
                  {formatDateTime(build.data.server_time_utc) ?? t.common.none} UTC
                </div>
              </div>
            </div>
          </section>

          <section className="section">
            <h2 className="section-title">{t.build.sectionDownloads}</h2>
            <div className="card">
              <table className="table">
                <thead>
                  <tr>
                    <th>{t.build.tableChannel}</th>
                    <th>{t.build.tableLink}</th>
                  </tr>
                </thead>
                <tbody>
                  <tr>
                    <td>{t.build.windowsLabel}</td>
                    <td>
                      {build.data.downloads?.windows ? (
                        <a className="mono" href={build.data.downloads.windows}>
                          {build.data.downloads.windows}
                        </a>
                      ) : (
                        <span className="empty">{t.build.noWindowsBuild}</span>
                      )}
                    </td>
                  </tr>
                  <tr>
                    <td>{t.build.steamLabel}</td>
                    <td>
                      {build.data.downloads?.steam ? (
                        <a className="mono" href={build.data.downloads.steam}>
                          {build.data.downloads.steam}
                        </a>
                      ) : (
                        <span className="empty">{t.build.noSteam}</span>
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
        <h2 className="section-title">{t.build.sectionChangelog}</h2>
        <Notice kind="info">{t.build.changelogBody(currentMilestone)}</Notice>
      </section>
    </>
  );
}
