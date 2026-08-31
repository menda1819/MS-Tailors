import type { ProblemDetails } from "@/types/problem";

/**
 * The single HTTP entry point to the .NET API.
 *
 * Components never call `fetch` directly (CLAUDE.md > Conventions >
 * TypeScript/Next.js). Everything goes through here so error shape, timeouts,
 * and caching policy are decided in one place.
 */

const BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL;

/**
 * Thrown for any non-2xx response.
 *
 * `payload` is the parsed body exactly as the API sent it. That is usually a
 * ProblemDetails, but not always — some endpoints answer an error status with a
 * domain body (health returns 503 with a full HealthResponse), so the raw
 * payload is kept rather than forced into the wrong shape.
 */
export class ApiError extends Error {
  readonly status: number;
  readonly payload: unknown;

  constructor(message: string, status: number, payload?: unknown) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.payload = payload;
  }

  /** The payload as a ProblemDetails, when it actually is one. */
  get problem(): ProblemDetails | undefined {
    if (typeof this.payload !== "object" || this.payload === null) {
      return undefined;
    }
    const candidate = this.payload as ProblemDetails;
    return typeof candidate.title === "string" || typeof candidate.status === "number"
      ? candidate
      : undefined;
  }
}

/** Thrown when the API is unreachable or the request timed out — no HTTP status exists. */
export class ApiUnreachableError extends Error {
  readonly cause?: unknown;

  constructor(message: string, cause?: unknown) {
    super(message);
    this.name = "ApiUnreachableError";
    this.cause = cause;
  }
}

export interface RequestOptions extends Omit<RequestInit, "body" | "method"> {
  /** Abort the request after this many milliseconds. Defaults to 10s. */
  timeoutMs?: number;
  /** Next.js fetch caching. Defaults to no-store so health checks are never cached. */
  cache?: RequestCache;
  next?: { revalidate?: number | false; tags?: string[] };
}

function resolveUrl(path: string): string {
  if (!BASE_URL) {
    throw new ApiUnreachableError(
      "NEXT_PUBLIC_API_BASE_URL is not set. Copy frontend/.env.example to frontend/.env.local.",
    );
  }

  const base = BASE_URL.replace(/\/+$/, "");
  const suffix = path.startsWith("/") ? path : `/${path}`;
  return `${base}${suffix}`;
}

async function request<TResponse>(
  method: string,
  path: string,
  body?: unknown,
  options: RequestOptions = {},
): Promise<TResponse> {
  const { timeoutMs = 10_000, headers, cache = "no-store", ...rest } = options;

  const url = resolveUrl(path);
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), timeoutMs);

  let response: Response;

  try {
    response = await fetch(url, {
      ...rest,
      method,
      cache,
      signal: controller.signal,
      headers: {
        Accept: "application/json",
        ...(body !== undefined ? { "Content-Type": "application/json" } : {}),
        ...headers,
      },
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
  } catch (error) {
    // fetch rejects on DNS failure, connection refused, and abort — never on 4xx/5xx.
    const reason =
      error instanceof Error && error.name === "AbortError"
        ? `Request to ${url} timed out after ${timeoutMs}ms.`
        : `Could not reach the API at ${url}. Is the backend running?`;
    throw new ApiUnreachableError(reason, error);
  } finally {
    clearTimeout(timeout);
  }

  if (response.status === 204) {
    return undefined as TResponse;
  }

  const isJson = response.headers
    .get("content-type")
    ?.toLowerCase()
    .includes("json");

  const payload: unknown = isJson ? await response.json() : await response.text();

  if (!response.ok) {
    const title =
      isJson && typeof payload === "object" && payload !== null
        ? (payload as ProblemDetails).title
        : undefined;

    throw new ApiError(
      title ?? `Request failed with status ${response.status}`,
      response.status,
      payload,
    );
  }

  return payload as TResponse;
}

export const api = {
  get: <TResponse>(path: string, options?: RequestOptions) =>
    request<TResponse>("GET", path, undefined, options),

  post: <TResponse>(path: string, body?: unknown, options?: RequestOptions) =>
    request<TResponse>("POST", path, body, options),

  put: <TResponse>(path: string, body?: unknown, options?: RequestOptions) =>
    request<TResponse>("PUT", path, body, options),

  patch: <TResponse>(path: string, body?: unknown, options?: RequestOptions) =>
    request<TResponse>("PATCH", path, body, options),

  delete: <TResponse>(path: string, options?: RequestOptions) =>
    request<TResponse>("DELETE", path, undefined, options),
};
