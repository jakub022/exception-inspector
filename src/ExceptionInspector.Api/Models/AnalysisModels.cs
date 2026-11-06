namespace ExceptionInspector.Api.Models;

/// <summary>Request body for analyzing a .NET exception trace.</summary>
public sealed record AnalyzeRequest
{
    /// <summary>The exception text to analyze.</summary>
    public required string Text { get; init; }
}

/// <summary>Structured analysis returned for an exception trace.</summary>
public sealed record AnalysisReport(
    string AnalysisId,
    string EngineVersion,
    Verdict Verdict,
    Fix? Fix,
    BlameLocation? Location,
    IReadOnlyList<string> Evidence,
    ExtendedInfo Extended);

/// <summary>The primary explanation of the analyzed exception.</summary>
public sealed record Verdict(
    string Title,
    string Summary,
    string ExceptionType,
    string ExceptionMessage);

/// <summary>A recommended action for addressing the exception.</summary>
public sealed record Fix(
    string Explanation,
    string? Code,
    string? DocsUrl);

/// <summary>The first relevant application-code location.</summary>
public sealed record BlameLocation(
    string Method,
    string? File,
    int? Line);

/// <summary>Technical details supporting the primary analysis.</summary>
public sealed record ExtendedInfo(
    IReadOnlyList<ExceptionNode> ExceptionChain,
    IReadOnlyList<StackFrame> Frames,
    IReadOnlyList<Finding> Findings,
    IReadOnlyList<string> Warnings,
    AnalysisStats Stats,
    IReadOnlyList<string> Redactions);

/// <summary>A node in the exception and inner-exception tree.</summary>
public sealed record ExceptionNode(
    string Type,
    string Message,
    bool IsWrapper,
    IReadOnlyList<StackFrame> Frames,
    IReadOnlyList<ExceptionNode> Children,
    bool Truncated);

/// <summary>A parsed stack trace frame.</summary>
public sealed record StackFrame(
    string Method,
    string? Parameters,
    string? File,
    int? Line,
    string Kind,
    bool IsCompilerGenerated,
    bool AsyncBoundaryBefore);

/// <summary>An additional deterministic observation about the trace.</summary>
public sealed record Finding(
    string RuleId,
    string Title,
    string Explanation,
    IReadOnlyList<string> Evidence);

/// <summary>Counts gathered while parsing the trace.</summary>
public sealed record AnalysisStats(
    int TotalFrames,
    int UserFrames,
    int FrameworkFrames,
    int ThirdPartyFrames,
    int UnparsedLines);
