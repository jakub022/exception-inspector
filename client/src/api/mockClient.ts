import { diUnresolvedServiceReport } from "../mocks/reports/diUnresolvedService";
import type { ApiClient } from "./client";
import type {
  AnalyzeRequest,
  AnalysisReport,
  CreateShareRequest,
  CreateShareResponse,
  SharedReport,
} from "./types";

const mockDelay = 2000;

export class MockApiClient implements ApiClient {
  private readonly sharedReports = new Map<string, SharedReport>();

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

  async createShare(
    _request: CreateShareRequest,
  ): Promise<CreateShareResponse> {
    await delay(300);

    const slug = "mock-share";
    const expiresAt = new Date(
      Date.now() + 7 * 24 * 60 * 60 * 1000,
    ).toISOString();
    this.sharedReports.set(slug, {
      report: diUnresolvedServiceReport,
      expiresAt,
    });

    return {
      slug,
      url: `${window.location.origin}/s/${slug}`,
      expiresAt,
      deleteToken: "mock-delete-token",
    };
  }

  async getShare(slug: string): Promise<SharedReport> {
    await delay(300);
    const shared = this.sharedReports.get(slug) ??
      (slug === "mock-share"
        ? {
            report: diUnresolvedServiceReport,
            expiresAt: new Date(
              Date.now() + 7 * 24 * 60 * 60 * 1000,
            ).toISOString(),
          }
        : undefined);

    if (shared === undefined) {
      throw new Error("This share link was not found or has expired.");
    }

    return shared;
  }
}

function delay(milliseconds: number): Promise<void> {
  return new Promise((resolve) => {
    window.setTimeout(resolve, milliseconds);
  });
}
