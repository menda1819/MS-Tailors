import { api, ApiError } from "@/lib/api";
import type { HealthResponse } from "@/types/health";

function isHealthResponse(value: unknown): value is HealthResponse {
  if (typeof value !== "object" || value === null) {
    return false;
  }
  const candidate = value as Partial<HealthResponse>;
  return (
    typeof candidate.status === "string" &&
    typeof candidate.version === "string" &&
    typeof candidate.database === "object" &&
    candidate.database !== null
  );
}

/**
 * Calls GET /api/health.
 *
 * The endpoint answers 503 with a full HealthResponse body when MongoDB is
 * unreachable — that is a reportable state, not a transport failure, so it is
 * unwrapped back into a HealthResponse instead of thrown.
 */
export async function getHealth(): Promise<HealthResponse> {
  try {
    return await api.get<HealthResponse>("/api/health");
  } catch (error) {
    if (error instanceof ApiError && isHealthResponse(error.payload)) {
      return error.payload;
    }
    throw error;
  }
}
