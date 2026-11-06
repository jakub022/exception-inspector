using ExceptionInspector.Api.Models;

namespace ExceptionInspector.Api.Services;

public interface IAnalysisService
{
    Task<AnalysisReport> AnalyzeAsync(
        AnalyzeRequest request,
        CancellationToken cancellationToken);
}
