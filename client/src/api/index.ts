import type { ApiClient } from "./client";
import { HttpApiClient } from "./httpClient";
import { MockApiClient } from "./mockClient";

const apiMode = import.meta.env.VITE_API_MODE ?? "mock";
const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "";

export const apiClient: ApiClient =
  apiMode === "http" ? new HttpApiClient(apiBaseUrl) : new MockApiClient();
