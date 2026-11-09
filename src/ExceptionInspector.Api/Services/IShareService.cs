using ExceptionInspector.Api.Models;

namespace ExceptionInspector.Api.Services;

public interface IShareService
{
  Task<CreateShareResult> CreateAsync(
    CreateShareRequest request,
    CancellationToken cancellationToken);

  Task<SharedReportResponse?> GetAsync(
    string slug,
    CancellationToken cancellationToken);

  Task<bool> DeleteAsync(
    string slug,
    string deleteToken,
    CancellationToken cancellationToken);
}
