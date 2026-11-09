using ExceptionInspector.Core.Models;

namespace ExceptionInspector.Core.Analysis;

public interface IExceptionAnalyzer
{
  AnalysisResult Analyze(string input);
}