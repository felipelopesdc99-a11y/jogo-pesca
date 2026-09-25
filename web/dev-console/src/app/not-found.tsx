import Link from "next/link";

import { t } from "@/lib/strings";

export default function NotFound() {
  return (
    <>
      <h1 className="page-title">{t.notFound.title}</h1>
      <p className="page-lede">
        {t.notFound.lede} <Link href="/">{t.app.nav.dashboard}</Link>,{" "}
        <Link href="/roadmap">{t.app.nav.roadmap}</Link>,{" "}
        <Link href="/config">{t.app.nav.config}</Link> e{" "}
        <Link href="/build">{t.app.nav.build}</Link>.
      </p>
    </>
  );
}
