"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import { useRouter } from "next/navigation";
import { adminApi, refreshAccessToken, setAccessToken, describeError } from "@/lib/admin-api";
import type { AuthResponse, AuthenticatedUser } from "@/types/admin";

type AuthStatus = "loading" | "authenticated" | "anonymous";

interface AuthContextValue {
  status: AuthStatus;
  user: AuthenticatedUser | null;
  isAdmin: boolean;
  signIn: (email: string, password: string) => Promise<void>;
  signOut: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

/** Rotate a minute before expiry so a request never races the deadline. */
const REFRESH_MARGIN_MS = 60_000;

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const [status, setStatus] = useState<AuthStatus>("loading");
  const [user, setUser] = useState<AuthenticatedUser | null>(null);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const clearTimer = useCallback(() => {
    if (timerRef.current) {
      clearTimeout(timerRef.current);
      timerRef.current = null;
    }
  }, []);

  // hydrate and scheduleRefresh call each other: hydrate arms the timer, and
  // the timer re-runs hydrate to pick up the next expiry. The ref breaks that
  // cycle without referencing hydrate before it is declared.
  const hydrateRef = useRef<() => Promise<void>>(async () => {});

  const scheduleRefresh = useCallback(
    (expiresAtUtc: string) => {
      clearTimer();

      const delay = new Date(expiresAtUtc).getTime() - Date.now() - REFRESH_MARGIN_MS;

      timerRef.current = setTimeout(
        () => {
          void hydrateRef.current();
        },
        Math.max(delay, 5_000),
      );
    },
    [clearTimer],
  );

  /**
   * Recovers a session from the HttpOnly refresh cookie. Runs on mount, so a
   * page reload does not sign the admin out even though the access token only
   * ever lived in memory.
   */
  const hydrate = useCallback(async () => {
    // Through refreshAccessToken, never straight to the endpoint: it is the one
    // place concurrent rotations are collapsed into a single request.
    const auth = (await refreshAccessToken()) as AuthResponse | null;

    if (!auth) {
      setAccessToken(null);
      setUser(null);
      setStatus("anonymous");
      return;
    }

    setUser(auth.user);
    setStatus("authenticated");
    scheduleRefresh(auth.expiresAtUtc);
  }, [scheduleRefresh]);

  useEffect(() => {
    hydrateRef.current = hydrate;
  }, [hydrate]);

  useEffect(() => {
    // Recovering the session is a fetch against an external system, which is
    // what effects are for; the rule cannot distinguish that from derived state.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void hydrate();
    return clearTimer;
  }, [hydrate, clearTimer]);

  const signIn = useCallback(
    async (email: string, password: string) => {
      const auth = await adminApi.post<AuthResponse>("/api/auth/login", { email, password });

      setAccessToken(auth.accessToken);
      setUser(auth.user);
      setStatus("authenticated");
      scheduleRefresh(auth.expiresAtUtc);
    },
    [scheduleRefresh],
  );

  const signOut = useCallback(async () => {
    clearTimer();

    try {
      await adminApi.post("/api/auth/logout");
    } catch {
      // Signing out locally must succeed even if the server call does not.
    }

    setAccessToken(null);
    setUser(null);
    setStatus("anonymous");
    router.push("/admin/login");
  }, [clearTimer, router]);

  const value = useMemo<AuthContextValue>(
    () => ({
      status,
      user,
      isAdmin: user?.role === "Admin",
      signIn,
      signOut,
    }),
    [status, user, signIn, signOut],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error("useAuth must be used inside an AuthProvider.");
  }

  return context;
}

export { describeError };
