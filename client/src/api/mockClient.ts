import { diUnresolvedServiceReport } from "../mocks/reports/diUnresolvedService";
import type { ApiClient } from "./client";
import type { AnalyzeRequest, AnalysisReport } from "./types";

const mockDelay = 2000;

export class MockApiClient implements ApiClient {
  async analyze(
    _request: AnalyzeRequest,
    signal?: AbortSignal,
  ): Promise<AnalysisReport> {
    await new Promise<void>((resolve, reject) => {
      const timeout = window.setTimeout(resolve, mockDelay);

      signal?.addEventListener(
        "abort",
        () => {
          window.clearTimeout(timeout);
          reject(new DOMException("Analysis cancelled.", "AbortError"));
        },
        {
          once: true,
        },
      );
    });

    return diUnresolvedServiceReport;
  }
}
