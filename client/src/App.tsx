import { GitBranch, LoaderCircle, Moon, Search, Sun } from "lucide-react";
import { useEffect, useState } from "react";
import ReportView from "./components/ReportView";
import type { AnalysisReport } from "./api/types";
import { apiClient } from "./api";

const themeStorageKey = "exception-inspector-theme";

type Theme = "light" | "dark";
type ViewState =
  | "input"
  | "loading"
  | "results"
  | "shared-loading"
  | "shared-error";

function appUrl(path: string): string {
  const basePath = import.meta.env.BASE_URL.replace(/\/$/, "");
  return `${basePath}${path}` || "/";
}

function getShareSlug(): string | null {
  const basePath = import.meta.env.BASE_URL.replace(/\/$/, "");
  const pathname =
    basePath.length > 0 && window.location.pathname.startsWith(basePath)
      ? window.location.pathname.slice(basePath.length)
      : window.location.pathname;
  const match = pathname.match(/^\/s\/([^/]+)\/?$/);
  return match === null ? null : decodeURIComponent(match[1]);
}

function getInitialTheme(): Theme {
  const savedTheme = localStorage.getItem(themeStorageKey);

  if (savedTheme == "light" || savedTheme == "dark") {
    return savedTheme;
  }

  return window.matchMedia("(prefers-color-scheme: dark)").matches
    ? "dark"
    : "light";
}

function App() {
  const [theme, setTheme] = useState<Theme>(getInitialTheme);
  const [exceptionText, setExceptionText] = useState("");
  const [viewState, setViewState] = useState<ViewState>(() =>
    getShareSlug() === null ? "input" : "shared-loading",
  );
  const [report, setReport] = useState<AnalysisReport | null>(null);
  const [shareSlug, setShareSlug] = useState<string | null>(getShareSlug);
  const [shareError, setShareError] = useState<string | null>(null);
  const [isSharing, setIsSharing] = useState(false);

  useEffect(() => {
    document.documentElement.dataset.theme = theme;
    localStorage.setItem(themeStorageKey, theme);
  }, [theme]);

  useEffect(() => {
    if (shareSlug === null) {
      return;
    }

    let active = true;

    void apiClient
      .getShare(shareSlug)
      .then((response) => {
        if (!active) {
          return;
        }

        setReport(response.report);
        setViewState("results");
      })
      .catch((error: unknown) => {
        if (!active) {
          return;
        }

        setShareError(
          error instanceof Error
            ? error.message
            : "This share link could not be loaded.",
        );
        setViewState("shared-error");
      });

    return () => {
      active = false;
    };
  }, [shareSlug]);

  useEffect(() => {
    const handlePopState = () => {
      const nextSlug = getShareSlug();
      setShareSlug(nextSlug);

      if (nextSlug === null) {
        setReport(null);
        setShareError(null);
        setViewState("input");
      } else {
        setViewState("shared-loading");
      }
    };

    window.addEventListener("popstate", handlePopState);
    return () => window.removeEventListener("popstate", handlePopState);
  }, []);

  const toggleTheme = () => {
    setTheme((currentTheme) => (currentTheme === "light" ? "dark" : "light"));
  };

  const handleInspect = async () => {
    setViewState("loading");

    try {
      const response = await apiClient.analyze({ text: exceptionText });

      setReport(response);
      setViewState("results");
    } catch (error) {
      if (error instanceof DOMException && error.name === "AbortError") {
        return;
      }

      setViewState("input");
      console.error(error);
    }
  };

  const handleAnalyzeAnother = () => {
    window.history.pushState({}, "", appUrl("/"));
    setExceptionText("");
    setReport(null);
    setShareSlug(null);
    setShareError(null);
    setViewState("input");
  };

  const handleShare = async () => {
    setIsSharing(true);
    setShareError(null);

    try {
      const response = await apiClient.createShare({
        text: exceptionText,
      });

      window.history.pushState({}, "", appUrl(`/s/${response.slug}`));
      setShareSlug(response.slug);
      await navigator.clipboard?.writeText(response.url);
    } catch (error) {
      setShareError(
        error instanceof Error
          ? error.message
          : "The share link could not be created.",
      );
    } finally {
      setIsSharing(false);
    }
  };

  const isDarkMode = theme === "dark";
  const hasExceptionText = exceptionText.trim().length > 0;
  const isLoading = viewState === "loading";
  const showingResults = viewState === "results";
  const showingSharedLoading = viewState === "shared-loading";
  const showingSharedError = viewState === "shared-error";

  return (
    <div className="app-shell min-h-screen bg-base-200 text-base-content">
      <header className="app-header border-b border-base-300">
        <div className="mx-auto flex max-w-5xl items-center justify-between gap-6 px-6 py-5">
          <a
            className="rounded-sm focus:outline-2 focus:outline-offset-4 focus:outline-primary"
            href={appUrl("/")}
            aria-label="Go to Exception Inspector home"
          >
            <p className="font-mono text-xl font-bold tracking-tight text-primary">
              exception-inspector
            </p>
            <p className="text-sm text-base-content/70">
              A .NET exception parser and analyzer
            </p>
          </a>
          <div className="flex items-center gap-2">
            <button
              className="btn btn-outline btn-sm border-base-300 bg-base-100/60"
              type="button"
              onClick={toggleTheme}
              aria-label={`Switch to ${isDarkMode ? "dark" : "light"} mode`}
            >
              {isDarkMode ? (
                <Sun size={16} aria-hidden="true" />
              ) : (
                <Moon size={16} aria-hidden="true" />
              )}
              <span className="hidden sm:inline">
                {isDarkMode ? "Light mode" : "Dark mode"}
              </span>
            </button>
            <a
              className="btn btn-outline btn-sm gap-2 border-base-300 bg-base-100/60"
              href="https://github.com/jakub022/exception-inspector"
              target="_blank"
              rel="noreferrer"
            >
              <GitBranch size={16} aria-hidden="true" />
              <span className="hidden sm:inline">GitHub</span>
            </a>
          </div>
        </div>
      </header>

      <main
        className={`mx-auto flex min-h-[calc(100vh-180px)] w-full items-center px-6 py-12 ${
          showingResults ? "max-w-7xl" : "max-w-5xl"
        }`}
      >
        {(viewState === "input" || viewState === "loading") && (
          <section className="app-card card relative w-full bg-base-100/95 shadow-sm">
            <div
              className={`transition-all duration-500 ${
                isLoading
                  ? "scale-[0.99] opacity-40 blur-sm"
                  : "scale-100 opacity-100 blur-0"
              }`}
            >
              <div className="card-body gap-6 p-6 sm:p-10">
                <div className="max-w-2xl">
                  <h1 className="text-2xl font-semibold tracking-tight sm:text-3xl">
                    Paste a .NET exception to inspect its structure
                  </h1>
                </div>

                <div className="space-y-4">
                  <label className="sr-only" htmlFor="exception-input">
                    .NET exception or stack trace
                  </label>

                  <textarea
                    id="exception-input"
                    className="textarea min-h-64 w-full resize-none border-0 bg-base-200/70 font-mono text-sm leading-6 shadow-inner focus:outline-2 focus:outline-primary"
                    placeholder="Paste your exception or stack trace here..."
                    value={exceptionText}
                    onChange={(event) => setExceptionText(event.target.value)}
                    disabled={isLoading}
                  />

                  <p className="text-sm text-base-content/60">
                    Your exception is used only for this inspection and is not
                    shared publicly.
                  </p>

                  <button
                    className="btn btn-primary gap-2"
                    type="button"
                    onClick={() => void handleInspect()}
                    disabled={!hasExceptionText || isLoading}
                  >
                    <Search size={18} aria-hidden="true" />
                    Inspect
                  </button>
                </div>
              </div>
            </div>

            {isLoading && (
              <div
                className="absolute inset-0 z-10 flex flex-col items-center justify-center gap-3"
                role="status"
                aria-live="polite"
              >
                <LoaderCircle
                  className="animate-spin text-primary"
                  size={42}
                  aria-hidden="true"
                />
                <p className="text-sm font-medium text-base-content/70">
                  Inspecting your exception...
                </p>
              </div>
            )}
          </section>
        )}

        {showingSharedLoading && (
          <section className="flex w-full flex-col items-center gap-3 py-20">
            <LoaderCircle
              className="animate-spin text-primary"
              size={42}
              aria-hidden="true"
            />
            <p className="text-sm font-medium text-base-content/70">
              Loading shared report...
            </p>
          </section>
        )}

        {showingSharedError && (
          <section className="alert alert-error w-full" role="alert">
            <p>{shareError}</p>
          </section>
        )}

        {showingResults && report !== null && (
          <>
            {shareError !== null && (
              <div className="alert alert-error mb-4 w-full" role="alert">
                <p>{shareError}</p>
              </div>
            )}
          <ReportView
            report={report}
            handleAnalyzeAnother={handleAnalyzeAnother}
            handleShare={() => void handleShare()}
            isSharing={isSharing}
          />
          </>
        )}
      </main>
    </div>
  );
}

export default App;
