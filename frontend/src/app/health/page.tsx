import type { Metadata } from "next";
import { getHealth } from "@/lib/health";
import { ApiUnreachableError } from "@/lib/api";
import type { HealthResponse } from "@/types/health";

/**
 * TEMPORARY — Phase 1 connectivity proof.
 *
 * Confirms Next.js -> .NET API -> MongoDB end to end. Delete this route
 * once real pages exist; it is noindex and must never ship to production.
 */

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  title: "System health · MS Tailors",
  robots: { index: false, follow: false },
};

type Probe =
  | { ok: true; health: HealthResponse }
  | { ok: false; message: string };

async function probe(): Promise<Probe> {
  try {
    return { ok: true, health: await getHealth() };
  } catch (error) {
    return {
      ok: false,
      message:
        error instanceof ApiUnreachableError || error instanceof Error
          ? error.message
          : "Unknown error contacting the API.",
    };
  }
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between gap-4 border-b border-neutral-200 py-2.5 text-sm last:border-0 dark:border-neutral-800">
      <dt className="text-neutral-500 dark:text-neutral-400">{label}</dt>
      <dd className="text-right font-mono text-neutral-900 dark:text-neutral-100">{value}</dd>
    </div>
  );
}

function Badge({ ok, children }: { ok: boolean; children: React.ReactNode }) {
  return (
    <span
      className={`inline-flex items-center gap-2 rounded-full px-3 py-1 text-xs font-medium ${
        ok
          ? "bg-emerald-50 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300"
          : "bg-red-50 text-red-700 dark:bg-red-950 dark:text-red-300"
      }`}
    >
      <span
        aria-hidden="true"
        className={`h-1.5 w-1.5 rounded-full ${ok ? "bg-emerald-500" : "bg-red-500"}`}
      />
      {children}
    </span>
  );
}

export default async function HealthPage() {
  const result = await probe();

  return (
    <main className="mx-auto flex min-h-screen max-w-xl flex-col justify-center px-6 py-16">
      <h1 className="text-2xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50">
        System health
      </h1>
      <p className="mt-1 text-sm text-neutral-500 dark:text-neutral-400">
        Phase 1 connectivity check — Next.js to .NET API to MongoDB.
      </p>

      {result.ok ? (
        <section className="mt-8 rounded-xl border border-neutral-200 p-6 dark:border-neutral-800">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <h2 className="text-sm font-medium text-neutral-900 dark:text-neutral-100">
              MongoDB
            </h2>
            <Badge ok={result.health.database.status === "Connected"}>
              {result.health.database.status}
            </Badge>
          </div>

          <dl className="mt-4">
            <Row label="API status" value={result.health.status} />
            <Row label="Database" value={result.health.database.name} />
            <Row label="Ping latency" value={`${result.health.database.latencyMs} ms`} />
            <Row label="Application" value={result.health.application} />
            <Row label="Version" value={result.health.version} />
            <Row label="Environment" value={result.health.environment} />
            <Row
              label="Checked at"
              value={new Date(result.health.timestampUtc).toISOString()}
            />
            {result.health.database.error ? (
              <Row label="Error" value={result.health.database.error} />
            ) : null}
          </dl>
        </section>
      ) : (
        <section
          role="alert"
          className="mt-8 rounded-xl border border-red-200 bg-red-50 p-6 dark:border-red-900 dark:bg-red-950/40"
        >
          <div className="flex items-center justify-between gap-3">
            <h2 className="text-sm font-medium text-red-900 dark:text-red-200">
              API unreachable
            </h2>
            <Badge ok={false}>Failed</Badge>
          </div>
          <p className="mt-3 text-sm text-red-800 dark:text-red-300">{result.message}</p>
          <p className="mt-3 text-xs text-red-700 dark:text-red-400">
            Start the backend with{" "}
            <code className="font-mono">dotnet run --project MSTailors.Api</code> from{" "}
            <code className="font-mono">backend/</code>.
          </p>
        </section>
      )}
    </main>
  );
}
