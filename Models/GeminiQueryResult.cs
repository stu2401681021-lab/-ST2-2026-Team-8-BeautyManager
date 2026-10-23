
namespace BeautyManager.Models;

public class GeminiQueryResult
{
    public bool Success { get; init; }

    public string? Json { get; init; }

    public string? ErrorMessage { get; init; }

    public static GeminiQueryResult Succeeded(string json)
    {
        return new GeminiQueryResult
        {
            Success = true,
            Json = json
        };
    }

    public static GeminiQueryResult Failed(string error)
    {
        return new GeminiQueryResult
        {
            Success = false,
            ErrorMessage = error
        };
    }
}
