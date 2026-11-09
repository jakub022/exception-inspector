using ExceptionInspector.Api.Models;
using ExceptionInspector.Api.Services;
using ExceptionInspector.Core.Analysis;
using ExceptionInspector.Core.Models;

using CoreFinding = ExceptionInspector.Core.Models.Finding;

namespace ExceptionInspector.Api.Tests;

public sealed class ParserAnalysisServiceTests
{
  [Fact]
  public async Task AnalyzeUsesDiagnosticFindingForVerdict()
  {
    var analyzer = new StubAnalyzer();
    var service = new ParserAnalysisService(analyzer);

    var report = await service.AnalyzeAsync(
      new AnalyzeRequest { Text = "trace" },
      CancellationToken.None);

    Assert.Equal("Missing dependency registration", report.Verdict.Title);
    Assert.Equal(
      "The dependency-injection container could not create OrderService.",
      report.Verdict.Summary);
    Assert.NotNull(report.Fix);
    Assert.Equal("Register the missing service.", report.Fix.Explanation);
  }

  [Fact]
  public async Task AnalyzeMapsParserResultToApiReport()
  {
    var service = new ParserAnalysisService(new ExceptionAnalyzer());

    var report = await service.AnalyzeAsync(
      new AnalyzeRequest
      {
        Text =
          "System.InvalidCastException: Unable to cast object of type 'System.String' to type 'System.Int32'.\n" +
          "   at Orders.Load() in /Users/dev/src/Orders.cs:line 42",
      },
      CancellationToken.None);

    Assert.Equal("System.InvalidCastException", report.Verdict.ExceptionType);
    Assert.Equal(
      "An object was cast to an incompatible type",
      report.Verdict.Title);
    Assert.Equal("invalid-cast", report.Extended.Findings[0].RuleId);
    Assert.Equal("<user-path>/Orders.cs", report.Location?.File);
    Assert.Equal(42, report.Location?.Line);
    Assert.NotNull(report.Fix);
  }

  private sealed class StubAnalyzer : IExceptionAnalyzer
  {
    public AnalysisResult Analyze(string input)
    {
      var result = new AnalysisResult
      {
        ExceptionType = "System.AggregateException",
        ExceptionMessage = "Several operations failed.",
        Fix = new SuggestedFix
        {
          Explanation = "Register the missing service.",
        },
      };

      var wrapperFinding = new CoreFinding
      {
        RuleId = "wrapped-exception",
        Title = "An exception was wrapped by another exception",
        Explanation = "The outer exception added context.",
      };
      wrapperFinding.Evidence.Add("wrapper");
      result.Findings.Add(wrapperFinding);

      var diagnosticFinding = new CoreFinding
      {
        RuleId = "dependency-injection-unresolved-service",
        Title = "Missing dependency registration",
        Explanation =
          "The dependency-injection container could not create OrderService.",
      };
      diagnosticFinding.Evidence.Add("missing service");
      result.Findings.Add(diagnosticFinding);

      return result;
    }
  }
}
