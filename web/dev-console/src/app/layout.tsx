import type { Metadata } from "next";
import Link from "next/link";

import { AutoRefresh } from "@/components/AutoRefresh";
import { t } from "@/lib/strings";
import "./globals.css";

export const metadata: Metadata = {
  title: t.app.title,
  description: t.app.metaDescription,
  robots: { index: false, follow: false },
};

const NAV = [
  { href: "/", label: t.app.nav.dashboard },
  { href: "/roadmap", label: t.app.nav.roadmap },
  { href: "/config", label: t.app.nav.config },
  { href: "/build", label: t.app.nav.build },
];

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="pt-BR">
      <body>
        <div className="shell">
          <aside className="sidebar">
            <div className="sidebar-brand">{t.app.brand}</div>
            <div className="sidebar-sub">{t.app.subtitle}</div>

            <nav className="nav">
              {NAV.map((item) => (
                <Link key={item.href} href={item.href} className="nav-link">
                  {item.label}
                </Link>
              ))}
            </nav>

            <AutoRefresh seconds={60} />

            <div className="nav-note">{t.app.sidebarNote}</div>
          </aside>

          <main className="main">{children}</main>
        </div>
      </body>
    </html>
  );
}
