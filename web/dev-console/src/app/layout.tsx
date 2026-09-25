import type { Metadata } from "next";
import Link from "next/link";

import "./globals.css";

export const metadata: Metadata = {
  title: "Fishing Idle — Development Console",
  description: "Private development console: roadmap, build status and game configuration.",
  robots: { index: false, follow: false },
};

const NAV = [
  { href: "/", label: "Dashboard" },
  { href: "/roadmap", label: "Roadmap" },
  { href: "/config", label: "Game Config" },
  { href: "/build", label: "Build / Version" },
];

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>
        <div className="shell">
          <aside className="sidebar">
            <div className="sidebar-brand">Fishing Idle</div>
            <div className="sidebar-sub">Development Console</div>

            <nav className="nav">
              {NAV.map((item) => (
                <Link key={item.href} href={item.href} className="nav-link">
                  {item.label}
                </Link>
              ))}
            </nav>

            <div className="nav-note">
              This panel reflects repository state, not background activity. Status changes when a
              task is completed and <code>docs/roadmap.json</code> is updated as part of that change.
            </div>
          </aside>

          <main className="main">{children}</main>
        </div>
      </body>
    </html>
  );
}
