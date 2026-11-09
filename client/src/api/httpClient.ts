import type { ApiClient } from "./client";
import type {
  AnalyzeRequest,
  AnalysisReport,
  CreateShareRequest,
  CreateShareResponse,
  ProblemDetails,
  SharedReport,
} from "./types";

export class HttpApiClient implements ApiClient {
  private readonly baseUrl: string;

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl;
  }

  async analyze(
    request: AnalyzeRequest,
    signal?: AbortSignal,
  ): Promise<AnalysisReport> {
    const response = await fetch(`${this.baseUrl}/api/analyze`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(request),
      signal,
    });

    if (response.ok) {
      return (await response.json()) as AnalysisReport;
    }

    let message = `Analysis failed with status ${response.status}.`;

    try {
      const problem = (await response.json()) as ProblemDetails;
      message = problem.detail ?? problem.title;
    } catch {}

    throw new Error(message);
  }

  async createShare(
    request: CreateShareRequest,
  ): Promise<CreateShareResponse> {
    const response = await fetch(`${this.baseUrl}/api/shares`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(request),
    });

    return this.readResponse(response, "Share creation failed.");
  }

  async getShare(slug: string): Promise<SharedReport> {
    const response = await fetch(
      `${this.baseUrl}/api/shares/${encodeURIComponent(slug)}`,
    );

    return this.readResponse(response, "Shared report loading failed.");
  }

  private async readResponse<T>(
    response: Response,
    fallbackMessage: string,
  ): Promise<T> {
    if (response.ok) {
      return (await response.json()) as T;
    }

    let message = `${fallbackMessage} Status ${response.status}.`;

    try {
      const problem = (await response.json()) as ProblemDetails;
      message = problem.detail ?? problem.title;
    } catch {}

    throw new Error(message);
  }
}
