import type { ApiClient } from "./client";
import type { AnalyzeRequest, AnalysisReport, ProblemDetails } from "./types";

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
}
