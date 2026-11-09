using ExceptionInspector.Api.Models;
using ExceptionInspector.Core.Analysis;

using CoreExceptionNode = ExceptionInspector.Core.Models.ExceptionNode;
using CoreFinding = ExceptionInspector.Core.Models.Finding;
using CoreStackFrame = ExceptionInspector.Core.Models.StackFrame;
using CoreSuggestedFix = ExceptionInspector.Core.Models.SuggestedFix;
using CoreAnalysisResult = ExceptionInspector.Core.Models.AnalysisResult;

namespace ExceptionInspector.Api.Services;

public sealed class ParserAnalysisService : IAnalysisService
{
  private readonly IExceptionAnalyzer analyzer;

  public ParserAnalysisService(IExceptionAnalyzer analyzer)
  {
    this.analyzer = analyzer;
  }

  public Task<AnalysisReport> AnalyzeAsync(
      AnalyzeRequest request,
      CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var result = analyzer.Analyze(request.Text);
    var primaryFinding = result.Findings
        .OrderByDescending(GetFindingPriority)
        .FirstOrDefault();

    var report = new AnalysisReport(
        Guid.NewGuid().ToString(),
        "0.1.0",
        new Verdict(
            CreateVerdictTitle(result, primaryFinding),
            CreateVerdictSummary(result, primaryFinding),
            result.ExceptionType,
            result.ExceptionMessage),
        result.Fix is null ? null : MapFix(result.Fix),
        result.Location is null
            ? null
            : new BlameLocation(
                result.Location.Method,
                result.Location.File,
                result.Location.Line),
        [],
        new ExtendedInfo(
            result.ExceptionChain
                .Select(MapExceptionNode)
                .ToArray(),
            result.Frames
                .Select(MapStackFrame)
                .ToArray(),
            result.Findings
                .Select(MapFinding)
                .ToArray(),
            result.Warnings,
            new AnalysisStats(
                result.Stats.TotalFrames,
                result.Stats.UserFrames,
                result.Stats.FrameworkFrames,
                result.Stats.ThirdPartyFrames,
                result.Stats.UnparsedLines),
            result.Redactions));

    return Task.FromResult(report);
  }

  private static string CreateVerdictTitle(
      CoreAnalysisResult result,
      CoreFinding? primaryFinding)
  {
    return primaryFinding?.Title ??
      (result.ExceptionType.Length == 0
        ? "Exception detected"
        : result.ExceptionType);
  }

  private static string CreateVerdictSummary(
      CoreAnalysisResult result,
      CoreFinding? primaryFinding)
  {
    return primaryFinding?.Explanation ??
      (result.ExceptionMessage.Length == 0
        ? $"The trace contains {result.ExceptionType}."
        : result.ExceptionMessage);
  }

  private static int GetFindingPriority(CoreFinding finding)
  {
    return finding.RuleId switch
    {
      "aggregate-exception" or "wrapped-exception" => 10,
      _ => 100,
    };
  }

  private static Fix MapFix(CoreSuggestedFix fix)
  {
    return new Fix(fix.Explanation, fix.Code, fix.DocsUrl);
  }

  private static ExceptionNode MapExceptionNode(CoreExceptionNode node)
  {
    return new ExceptionNode(
        node.Type,
        node.Message,
        node.IsWrapper,
        node.Frames.Select(MapStackFrame).ToArray(),
        node.Children.Select(MapExceptionNode).ToArray(),
        node.Truncated);
  }

  private static StackFrame MapStackFrame(CoreStackFrame frame)
  {
    return new StackFrame(
        frame.Method,
        frame.Parameters,
        frame.File,
        frame.Line,
        frame.Kind,
        frame.IsCompilerGenerated,
        frame.AsyncBoundaryBefore);
  }

  private static Finding MapFinding(CoreFinding finding)
  {
    return new Finding(
        finding.RuleId,
        finding.Title,
        finding.Explanation,
        finding.Evidence);
  }
}