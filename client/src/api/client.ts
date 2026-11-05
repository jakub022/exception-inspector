import type { AnalysisReport, AnalyzeRequest } from "./types";

export interface ApiClient {
  analyze(
    request: AnalyzeRequest,
    signal?: AbortSignal,
  ): Promise<AnalysisReport>;
}
