namespace ExceptionInspector.Core.Models;

public sealed class AnalysisResult
{
  public string ExceptionType { get; set; } = string.Empty;
  public string ExceptionMessage { get; set; } = string.Empty;
  public List<ExceptionNode> ExceptionChain { get; } = [];
  public List<StackFrame> Frames { get; } = [];
  public List<Finding> Findings { get; } = [];
  public SuggestedFix? Fix { get; set; }
  public BlameLocation? Location { get; set; }
  public AnalysisStats Stats { get; set; } = new();
  public List<string> Warnings { get; } = [];
  public List<string> Redactions { get; } = [];
}

public sealed class ExceptionNode
{
  public string Type { get; set; } = string.Empty;
  public string Message { get; set; } = string.Empty;
  public bool IsWrapper { get; set; }
  public List<StackFrame> Frames { get; } = [];
  public List<ExceptionNode> Children { get; } = [];
  public bool Truncated { get; set; }
}

public sealed class StackFrame
{
  public string Method { get; set; } = string.Empty;
  public string? Parameters { get; set; }
  public string? File { get; set; }
  public int? Line { get; set; }
  public string Kind { get; set; } = "unknown";
  public bool IsCompilerGenerated { get; set; }
  public bool AsyncBoundaryBefore { get; set; }
}

public sealed class Finding
{
  public string RuleId { get; set; } = string.Empty;
  public string Title { get; set; } = string.Empty;
  public string Explanation { get; set; } = string.Empty;
  public List<string> Evidence { get; } = [];
}

public sealed class SuggestedFix
{
  public string Explanation { get; set; } = string.Empty;
  public string? Code { get; set; }
  public string? DocsUrl { get; set; }
}

public sealed class BlameLocation
{
  public string Method { get; set; } = string.Empty;
  public string? File { get; set; }
  public int? Line { get; set; }
}

public sealed class AnalysisStats
{
  public int TotalFrames { get; set; }
  public int UserFrames { get; set; }
  public int FrameworkFrames { get; set; }
  public int ThirdPartyFrames { get; set; }
  public int UnparsedLines { get; set; }
}