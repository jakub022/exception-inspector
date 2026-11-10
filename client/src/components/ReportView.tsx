import {
  AlertTriangle,
  ExternalLink,
  FileCode2,
  Info,
  RotateCcw,
  Share2,
  Wrench,
} from "lucide-react";
import type { TokensResult } from "shiki/types";
import { useEffect, useState } from "react";
import type { AnalysisReport, ExceptionNode, StackFrame } from "../api/types";

interface ReportViewProps {
  report: AnalysisReport;
  handleAnalyzeAnother: () => void;
  handleShare: () => void;
  isSharing: boolean;
}

function getCsharpHighlighter() {
  return Promise.all([
    import("shiki/core"),
    import("shiki/engine/javascript"),
    import("@shikijs/langs/csharp"),
    import("@shikijs/themes/vitesse-dark"),
  ]).then(([core, engine, language, theme]) =>
    core.createHighlighterCore({
      engine: engine.createJavaScriptRegexEngine(),
      langs: [language.default],
      themes: [theme.default],
    }),
  );
}

function formatFrameKind(kind: string): string {
  if (kind.trim().length === 0) {
    return "Unknown";
  }

  return kind.charAt(0).toUpperCase() + kind.slice(1);
}

function formatFrameLocation(frame: StackFrame): string | null {
  if (frame.file === null) {
    return null;
  }

  return frame.line === null ? frame.file : `${frame.file}:${frame.line}`;
}

function HighlightedCode({ code }: { code: string }) {
  const [tokens, setTokens] = useState<TokensResult["tokens"] | null>(null);

  useEffect(() => {
    let isCurrent = true;

    void getCsharpHighlighter()
      .then((highlighter) =>
        highlighter.codeToTokens(code, {
          lang: "csharp",
          theme: "vitesse-dark",
        }),
      )
      .then((result) => {
        if (isCurrent) {
          setTokens(result.tokens);
        }
      })
      .catch((error: unknown) => {
        console.error("Unable to highlight C# code snippet.", error);
      });

    return () => {
      isCurrent = false;
    };
  }, [code]);

  if (tokens === null) {
    return <code>{code}</code>;
  }

  return (
    <code>
      {tokens.map((line, lineIndex) => (
        <span className="block" key={lineIndex}>
          {line.map((token, tokenIndex) => (
            <span
              key={`${lineIndex}-${tokenIndex}`}
              style={{ color: token.color }}
            >
              {token.content}
            </span>
          ))}
        </span>
      ))}
    </code>
  );
}

function FrameRow({ frame }: { frame: StackFrame }) {
  const location = formatFrameLocation(frame);

  return (
    <li className="border-t border-base-300 py-4 first:border-t-0">
      <div className="flex items-start gap-3">
        <FileCode2
          className="mt-1 shrink-0 text-base-content/50"
          size={17}
          aria-hidden="true"
        />

        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <code className="break-all text-sm font-medium">
              {frame.method}
            </code>

            <span className="badge badge-ghost badge-sm">
              {formatFrameKind(frame.kind)}
            </span>

            {frame.isCompilerGenerated && (
              <span className="badge badge-ghost badge-sm">
                Compiler generated
              </span>
            )}
          </div>

          {frame.parameters !== null && frame.parameters.length > 0 && (
            <p className="mt-1 break-all font-mono text-xs text-base-content/60">
              ({frame.parameters})
            </p>
          )}

          {location !== null && (
            <p className="mt-2 text-xs text-base-content/60">{location}</p>
          )}
        </div>
      </div>
    </li>
  );
}

function ExceptionNodeView({ node }: { node: ExceptionNode }) {
  return (
    <li className="space-y-3">
      <div className="rounded-box bg-base-200/60 p-4">
        <div className="flex flex-wrap items-center gap-2">
          <code className="break-all text-sm font-semibold">{node.type}</code>

          {node.isWrapper && (
            <span className="badge badge-warning badge-sm">Wrapper</span>
          )}
        </div>

        {node.message.length > 0 ? (
          <p className="mt-2 text-sm text-base-content/70">{node.message}</p>
        ) : (
          <p className="mt-2 text-sm italic text-base-content/50">
            No exception message was available.
          </p>
        )}

        {node.truncated && (
          <p className="mt-3 text-xs text-warning">
            Some nested exceptions were omitted from this response.
          </p>
        )}
      </div>

      {node.children.length > 0 && (
        <ul className="ml-4 space-y-3 border-l border-base-300 pl-4">
          {node.children.map((child, index) => (
            <ExceptionNodeView key={`${child.type}-${index}`} node={child} />
          ))}
        </ul>
      )}
    </li>
  );
}

function ReportView({
  report,
  handleAnalyzeAnother,
  handleShare,
  isSharing,
}: ReportViewProps) {
  const { verdict, fix, location, evidence, extended } = report;
  const hasLocation =
    location !== null &&
    (location.method.length > 0 ||
      location.file !== null ||
      location.line !== null);

  return (
    <section className="app-card card w-full animate-[fade-in_500ms_ease-out] bg-base-100/95 shadow-sm">
      <div className="card-body gap-10 p-6 sm:p-10 lg:p-14">
        <header className="flex flex-col gap-5 border-b border-base-300 pb-6 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <p className="text-sm font-medium uppercase tracking-wide text-primary">
              Analysis results
            </p>
            <h1 className="mt-2 text-3xl font-bold tracking-tight sm:text-4xl">
              {verdict.title}
            </h1>
          </div>

          <div className="flex shrink-0 flex-col gap-2 sm:flex-row">
            <button
              className="btn btn-primary btn-sm gap-2"
              type="button"
              onClick={handleAnalyzeAnother}
            >
              <RotateCcw size={16} aria-hidden="true" />
              Analyze another
            </button>

            <button
              className="btn btn-outline btn-sm gap-2"
              type="button"
              onClick={handleShare}
              disabled={isSharing}
            >
              <Share2 size={16} aria-hidden="true" />
              {isSharing ? "Creating link..." : "Share report"}
            </button>
          </div>
        </header>

        <section
          className="rounded-box bg-primary/8 p-5"
          aria-labelledby="verdict-heading"
        >
          <div className="flex items-start gap-3">
            <Info
              className="mt-1 shrink-0 text-primary"
              size={20}
              aria-hidden="true"
            />

            <div className="min-w-0">
              <h2 id="verdict-heading" className="font-semibold">
                Root cause
              </h2>
              <p className="mt-2 text-base leading-7">{verdict.summary}</p>
              <p className="mt-3 wrap-break-word font-mono text-sm text-base-content/60">
                {verdict.exceptionType}
              </p>
              {verdict.exceptionMessage.length > 0 && (
                <p className="mt-2 text-sm text-base-content/70">
                  {verdict.exceptionMessage}
                </p>
              )}
            </div>
          </div>
        </section>

        {fix !== null && (
          <section
            className="rounded-box bg-secondary/10 p-5 sm:p-6"
            aria-label="Suggested next step"
          >
            <div className="flex items-start gap-3">
              <Wrench
                className="mt-1 shrink-0 text-secondary"
                size={20}
                aria-hidden="true"
              />

              <div className="min-w-0">
                <p className="font-semibold text-base-content">
                  Suggested next step
                </p>
                <p className="mt-2 leading-7 text-base-content/80">
                  {fix.explanation}
                </p>
              </div>
            </div>

            {fix.code !== null && fix.code.length > 0 && (
              <pre className="mt-4 ml-8 overflow-x-auto rounded-box bg-base-200/80 p-4 font-mono text-sm leading-6 text-base-content sm:ml-8">
                <HighlightedCode code={fix.code} />
              </pre>
            )}

            {fix.docsUrl !== null && fix.docsUrl.length > 0 && (
              <div className="ml-8">
                <a
                  className="btn btn-ghost btn-sm mt-4 gap-2 px-0"
                  href={fix.docsUrl}
                  target="_blank"
                  rel="noreferrer"
                >
                  Open documentation
                  <ExternalLink size={15} aria-hidden="true" />
                </a>
              </div>
            )}
          </section>
        )}

        <section aria-labelledby="location-heading">
          <h2 id="location-heading" className="text-xl font-semibold">
            Where it surfaced
          </h2>

          {hasLocation && location !== null ? (
            <div className="mt-3 rounded-box bg-base-200/60 p-4">
              <code className="break-all text-sm">{location.method}</code>

              {(location.file !== null || location.line !== null) && (
                <p className="mt-2 text-sm text-base-content/60">
                  {location.file ?? "Source file unavailable"}
                  {location.line !== null && `:${location.line}`}
                </p>
              )}
            </div>
          ) : (
            <p className="mt-3 text-sm text-base-content/60">
              No application code location was identified.
            </p>
          )}
        </section>

        <section aria-labelledby="evidence-heading">
          <h2 id="evidence-heading" className="text-xl font-semibold">
            Evidence
          </h2>

          {evidence.length > 0 ? (
            <ul className="mt-3 space-y-2">
              {evidence.map((item, index) => (
                <li
                  className="flex items-start gap-3 text-sm text-base-content/80"
                  key={`${item}-${index}`}
                >
                  <span className="mt-2 h-1.5 w-1.5 shrink-0 rounded-full bg-primary" />
                  <span className="wrap-break-word">{item}</span>
                </li>
              ))}
            </ul>
          ) : (
            <p className="mt-3 text-sm text-base-content/60">
              No supporting evidence was extracted.
            </p>
          )}
        </section>

        <details className="collapse-arrow collapse bg-base-200/35">
          <summary className="collapse-title flex items-center gap-2 font-semibold">
            Extended information
          </summary>

          <div className="collapse-content space-y-8">
            <section aria-labelledby="chain-heading">
              <h2 id="chain-heading" className="text-lg font-semibold">
                Exception chain
              </h2>

              {extended.exceptionChain.length > 0 ? (
                <ul className="mt-4 space-y-3">
                  {extended.exceptionChain.map((node, index) => (
                    <ExceptionNodeView
                      key={`${node.type}-${index}`}
                      node={node}
                    />
                  ))}
                </ul>
              ) : (
                <p className="mt-3 text-sm text-base-content/60">
                  No exception chain was extracted.
                </p>
              )}
            </section>

            <section aria-labelledby="frames-heading">
              <h2 id="frames-heading" className="text-lg font-semibold">
                Stack frames
              </h2>

              {extended.frames.length > 0 ? (
                <ul className="mt-3">
                  {extended.frames.map((frame, index) => (
                    <FrameRow
                      key={`${frame.method}-${frame.file}-${frame.line}-${index}`}
                      frame={frame}
                    />
                  ))}
                </ul>
              ) : (
                <p className="mt-3 text-sm text-base-content/60">
                  No stack frames were parsed.
                </p>
              )}
            </section>

            <section aria-labelledby="findings-heading">
              <h2 id="findings-heading" className="text-lg font-semibold">
                Additional findings
              </h2>

              {extended.findings.length > 0 ? (
                <div className="mt-4 space-y-4">
                  {extended.findings.map((finding) => (
                    <article
                      className="rounded-box bg-base-200/60 p-4"
                      key={finding.ruleId}
                    >
                      <h3 className="font-semibold">{finding.title}</h3>
                      <p className="mt-2 text-sm leading-6 text-base-content/70">
                        {finding.explanation}
                      </p>

                      {finding.evidence.length > 0 && (
                        <ul className="mt-3 space-y-1 text-sm text-base-content/60">
                          {finding.evidence.map((item, index) => (
                            <li key={`${item}-${index}`}>- {item}</li>
                          ))}
                        </ul>
                      )}
                    </article>
                  ))}
                </div>
              ) : (
                <p className="mt-3 text-sm text-base-content/60">
                  No additional findings were produced.
                </p>
              )}
            </section>

            {extended.warnings.length > 0 && (
              <section aria-labelledby="warnings-heading">
                <h2 id="warnings-heading" className="text-lg font-semibold">
                  Analysis warnings
                </h2>

                <div className="alert alert-warning mt-4">
                  <AlertTriangle size={18} aria-hidden="true" />
                  <ul className="space-y-1 text-sm">
                    {extended.warnings.map((warning, index) => (
                      <li key={`${warning}-${index}`}>{warning}</li>
                    ))}
                  </ul>
                </div>
              </section>
            )}

            <section aria-labelledby="summary-heading">
              <h2 id="summary-heading" className="text-lg font-semibold">
                Analysis summary
              </h2>

              <dl className="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-4">
                <div className="rounded-box bg-base-200 p-3">
                  <dt className="text-xs text-base-content/60">Total frames</dt>
                  <dd className="mt-1 text-lg font-semibold">
                    {extended.stats.totalFrames}
                  </dd>
                </div>

                <div className="rounded-box bg-base-200 p-3">
                  <dt className="text-xs text-base-content/60">User frames</dt>
                  <dd className="mt-1 text-lg font-semibold">
                    {extended.stats.userFrames}
                  </dd>
                </div>

                <div className="rounded-box bg-base-200 p-3">
                  <dt className="text-xs text-base-content/60">
                    Framework frames
                  </dt>
                  <dd className="mt-1 text-lg font-semibold">
                    {extended.stats.frameworkFrames}
                  </dd>
                </div>

                <div className="rounded-box bg-base-200 p-3">
                  <dt className="text-xs text-base-content/60">
                    Unparsed lines
                  </dt>
                  <dd className="mt-1 text-lg font-semibold">
                    {extended.stats.unparsedLines}
                  </dd>
                </div>
              </dl>
            </section>

            {extended.redactions.length > 0 && (
              <section aria-labelledby="privacy-heading">
                <h2 id="privacy-heading" className="text-lg font-semibold">
                  Privacy
                </h2>

                <ul className="mt-3 space-y-2 text-sm text-base-content/70">
                  {extended.redactions.map((redaction, index) => (
                    <li key={`${redaction}-${index}`}>{redaction}</li>
                  ))}
                </ul>
              </section>
            )}

            <p className="text-xs text-base-content/50">
              Analysis {report.analysisId} · Engine {report.engineVersion}
            </p>
          </div>
        </details>
      </div>
    </section>
  );
}

export default ReportView;
