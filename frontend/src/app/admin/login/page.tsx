"use client";

import { Suspense, useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import { describeError } from "@/lib/admin-api";
import { Button, Field, Input } from "@/components/ui/primitives";

function LoginForm() {
  const { status, isAdmin, signIn } = useAuth();
  const router = useRouter();
  const params = useSearchParams();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const next = params.get("next") ?? "/admin";

  useEffect(() => {
    if (status === "authenticated" && isAdmin) {
      router.replace(next);
    }
  }, [status, isAdmin, router, next]);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);

    try {
      await signIn(email, password);
      router.replace(next);
    } catch (caught) {
      setError(describeError(caught));
      setBusy(false);
    }
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="w-full max-w-sm rounded-tailored border border-sand-soft bg-chalk p-7 shadow-raised"
    >
      <div className="measure w-32" aria-hidden="true">
        {Array.from({ length: 16 }).map((_, index) => (
          <i key={index} />
        ))}
      </div>

      <h1 className="mt-5 font-display text-2xl leading-tight text-ink">Shop admin</h1>
      <p className="mt-2 text-sm text-slate">
        Sign in with your staff account to manage orders, stock and content.
      </p>

      <div className="mt-6 flex flex-col gap-4">
        <Field label="Email" htmlFor="email" required>
          <Input
            id="email"
            name="email"
            type="email"
            autoComplete="username"
            required
            value={email}
            onChange={(event) => setEmail(event.target.value)}
          />
        </Field>

        <Field label="Password" htmlFor="password" required>
          <Input
            id="password"
            name="password"
            type="password"
            autoComplete="current-password"
            required
            value={password}
            onChange={(event) => setPassword(event.target.value)}
          />
        </Field>

        {error && (
          <p role="alert" className="rounded-tailored bg-danger-soft px-3 py-2 text-sm text-danger">
            {error}
          </p>
        )}

        <Button type="submit" disabled={busy} className="w-full">
          {busy ? "Signing in…" : "Sign in"}
        </Button>
      </div>
    </form>
  );
}

export default function AdminLoginPage() {
  return (
    <main className="flex min-h-screen items-center justify-center bg-paper px-4 py-12">
      <Suspense fallback={null}>
        <LoginForm />
      </Suspense>
    </main>
  );
}
