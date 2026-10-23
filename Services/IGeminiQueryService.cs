
using BeautyManager.Models;

namespace BeautyManager.Services;

public interface IGeminiQueryService
{
    Task<GeminiQueryResult> GenerateJsonQueryAsync(
        string userQuery,
        CancellationToken cancellationToken = default);
}
