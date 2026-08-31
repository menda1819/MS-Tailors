/**
 * Mirrors MSTailors.Application.Health.HealthResponse.
 * Keep in sync with the API DTO — this is the single source of truth on the client.
 */
export interface HealthResponse {
  status: "Healthy" | "Degraded";
  application: string;
  version: string;
  environment: string;
  timestampUtc: string;
  database: DatabaseStatusResponse;
}

export interface DatabaseStatusResponse {
  status: "Connected" | "Disconnected";
  name: string;
  latencyMs: number;
  error: string | null;
}
