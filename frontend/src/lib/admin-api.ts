import { ApiError, ApiUnreachableError } from "@/lib/api";
import type { ProblemDetails } from "@/types/problem";

/**
 * Authenticated API client for the admin dashboard.
 *
 * The access token lives in memory only — never localStorage, which any injected
 * script can read. It is recovered after a refresh by calling /api/auth/refresh,
 * which authenticates with the HttpOnly cookie the API set at login.
 */

const BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL;

interface RefreshResult {
  accessToken: string;
  expiresAtUtc: string;
  user: unknown;
}

let accessToken: string | null = null;
let refreshInFlight: Promise<RefreshResult | null> | null = null;

export function setAccessToken(token: string | null): void {
  accessToken = token;
}

export function getAccessToken(): string | null {
  return accessToken;
}

function resolveUrl(path: string): string {
  if (!BASE_URL) {
    throw new ApiUnreachableError(
      "NEXT_PUBLIC_API_BASE_URL is not set. Copy frontend/.env.example to frontend/.env.local.",
    );
  }
  const base = BASE_URL.replace(/\/+$/, "");
  return `${base}${path.startsWith("/") ? path : `/${path}`}`;
}

/**
 * Exchanges the refresh cookie for a new access token.
 *
 * EVERY refresh must go through here, never straight to the endpoint. Refresh
 * tokens rotate and are single-use, so two concurrent calls would race: the
 * first consumes the token and issues a new one, the second presents the
 * consumed token, gets a 401, and signs the admin out. That happens easily —
 * React re-invokes effects in development, and two open tabs do it in
 * production. Sharing one in-flight promise makes concurrent callers wait on
 * the same rotation instead of starting their own.
 */
export async function refreshAccessToken(): Promise<RefreshResult | null> {
  if (refreshInFlight) return refreshInFlight;

  refreshInFlight = (async () => {
    try {
      const response = await fetch(resolveUrl("/api/auth/refresh"), {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({}),
      });

      if (!response.ok) {
        accessToken = null;
        return null;
      }

      const auth = (await response.json()) as RefreshResult;
      accessToken = auth.accessToken;
      return auth;
    } catch {
      accessToken = null;
      return null;
    } finally {
      refreshInFlight = null;
    }
  })();

  return refreshInFlight;
}

interface RequestOptions {
  method?: string;
  body?: unknown;
  /** Multipart uploads set their own content type and boundary. */
  formData?: FormData;
  signal?: AbortSignal;
  /** Internal: prevents a refresh loop if the retry also 401s. */
  retryOnUnauthorized?: boolean;
}

async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const {
    method = "GET",
    body,
    formData,
    signal,
    retryOnUnauthorized = true,
  } = options;

  const headers: Record<string, string> = { Accept: "application/json" };

  if (accessToken) headers.Authorization = `Bearer ${accessToken}`;
  if (body !== undefined && !formData) headers["Content-Type"] = "application/json";

  let response: Response;

  try {
    response = await fetch(resolveUrl(path), {
      method,
      headers,
      credentials: "include",
      signal,
      body: formData ?? (body !== undefined ? JSON.stringify(body) : undefined),
    });
  } catch (error) {
    if (error instanceof Error && error.name === "AbortError") throw error;
    throw new ApiUnreachableError(
      "Could not reach the API. Check that the backend is running.",
      error,
    );
  }

  // An expired access token is normal — they last 15 minutes. Rotate once and
  // replay, so the admin never sees a spurious sign-out mid-task.
  if (response.status === 401 && retryOnUnauthorized) {
    const refreshed = await refreshAccessToken();
    if (refreshed) {
      return request<T>(path, { ...options, retryOnUnauthorized: false });
    }
  }

  if (response.status === 204) return undefined as T;

  const isJson = response.headers.get("content-type")?.toLowerCase().includes("json");
  const payload: unknown = isJson ? await response.json() : await response.text();

  if (!response.ok) {
    const problem =
      isJson && typeof payload === "object" && payload !== null
        ? (payload as ProblemDetails)
        : undefined;

    throw new ApiError(
      problem?.title ?? `Request failed with status ${response.status}`,
      response.status,
      payload,
    );
  }

  return payload as T;
}

export const adminApi = {
  get: <T>(path: string, signal?: AbortSignal) => request<T>(path, { signal }),
  post: <T>(path: string, body?: unknown) => request<T>(path, { method: "POST", body }),
  put: <T>(path: string, body?: unknown) => request<T>(path, { method: "PUT", body }),
  patch: <T>(path: string, body?: unknown) => request<T>(path, { method: "PATCH", body }),
  delete: <T>(path: string) => request<T>(path, { method: "DELETE" }),
  upload: <T>(path: string, formData: FormData) =>
    request<T>(path, { method: "POST", formData }),
};

/**
 * Turns any thrown value into a sentence worth showing a shop owner.
 *
 * Field-level validation errors are joined in, because "One or more validation
 * errors occurred" alone tells the person nothing about what to fix.
 */
export function describeError(error: unknown): string {
  if (error instanceof ApiUnreachableError) return error.message;

  if (error instanceof ApiError) {
    const problem = error.problem;

    if (problem?.errors) {
      const messages = Object.values(problem.errors).flat();
      if (messages.length > 0) return messages.join(" ");
    }

    const details = (error.payload as { details?: string[] } | undefined)?.details;
    if (details?.length) return details.join(" ");

    return problem?.detail ?? error.message;
  }

  if (error instanceof Error) return error.message;
  return "Something went wrong. Try again.";
}

/** Builds a query string, dropping empty values so URLs stay readable. */
export function toQuery(params: Record<string, string | number | boolean | undefined | null>): string {
  const search = new URLSearchParams();

  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === "") continue;
    search.set(key, String(value));
  }

  const query = search.toString();
  return query ? `?${query}` : "";
}
