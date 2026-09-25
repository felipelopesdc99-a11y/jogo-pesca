import Link from "next/link";

export default function NotFound() {
  return (
    <>
      <h1 className="page-title">Page not found</h1>
      <p className="page-lede">
        This console has four sections: <Link href="/">Dashboard</Link>,{" "}
        <Link href="/roadmap">Roadmap</Link>, <Link href="/config">Game Config</Link> and{" "}
        <Link href="/build">Build / Version</Link>.
      </p>
    </>
  );
}
