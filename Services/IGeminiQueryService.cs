using BeautyManager.Models;

namespace BeautyManager.Services;

public interface IGeminiQueryService
{
    Task<string> GenerateAnswerAsync(
        string userQuery,
        CancellationToken cancellationToken = default);

    Task<GeminiQueryResult> GenerateJsonQueryAsync(
        string userQuery,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> GenerateAnswerStreamAsync(
    string userQuery,
    CancellationToken cancellationToken = default);
}