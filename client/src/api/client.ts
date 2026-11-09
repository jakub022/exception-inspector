import type {
  AnalysisReport,
  AnalyzeRequest,
  CreateShareRequest,
  CreateShareResponse,
  SharedReport,
} from "./types";

export interface ApiClient {
  analyze(
    request: AnalyzeRequest,
    signal?: AbortSignal,
  ): Promise<AnalysisReport>;
  createShare(request: CreateShareRequest): Promise<CreateShareResponse>;
  getShare(slug: string): Promise<SharedReport>;
}
