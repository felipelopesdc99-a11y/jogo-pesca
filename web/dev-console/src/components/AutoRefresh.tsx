"use client";

import { useEffect, useState, useTransition } from "react";
import { useRouter } from "next/navigation";

import { t } from "@/lib/strings";

/**
 * Re-reads the page on a timer so the panel keeps up with the repository on its own.
 *
 * The panel reflects repository state, never background activity: this simply asks the server
 * components to run again, which re-reads the roadmap from whichever source answered. Nothing here
 * claims work is happening — it only means "what you are looking at was read a moment ago".
 */
export function AutoRefresh({ seconds = 60 }: { seconds?: number }) {
  const router = useRouter();
  const [pending, startTransition] = useTransition();
  const [lastRefresh, setLastRefresh] = useState<Date | null>(null);

  useEffect(() => {
    // Set on the client only, so the server-rendered HTML and the first client render match.
    setLastRefresh(new Date());

    const timer = setInterval(() => {
      startTransition(() => {
        router.refresh();
        setLastRefresh(new Date());
      });
    }, seconds * 1000);

    return () => clearInterval(timer);
  }, [router, seconds]);

  const refreshNow = () => {
    startTransition(() => {
      router.refresh();
      setLastRefresh(new Date());
    });
  };

  return (
    <div className="refresh">
      <span className={pending ? "refresh-dot refresh-dot-active" : "refresh-dot"} />
      <span>
        {t.refresh.auto(seconds)}
        {lastRefresh ? ` · ${t.refresh.lastAt(lastRefresh.toLocaleTimeString("pt-BR"))}` : ""}
      </span>
      <button type="button" className="refresh-button" onClick={refreshNow} disabled={pending}>
        {pending ? t.refresh.now : t.refresh.manual}
      </button>
    </div>
  );
}
