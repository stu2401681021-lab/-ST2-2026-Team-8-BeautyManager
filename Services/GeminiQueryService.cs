using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using BeautyManager.Configuration;
using BeautyManager.Models;
using System.Runtime.CompilerServices;

namespace BeautyManager.Services;

public sealed class GeminiQueryService : IGeminiQueryService
{
    private readonly HttpClient _httpClient;
    private readonly ExternalApisOptions _options;

    public GeminiQueryService(
        HttpClient httpClient,
        IOptions<ExternalApisOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> GenerateAnswerAsync(
        string userQuery,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userQuery))
        {
            throw new ArgumentException(
                "Въпросът не може да бъде празен.",
                nameof(userQuery));
        }

        const string systemInstruction = """
            Ти си AI асистент на BeautyManager — система за салон за красота.
            Отговаряй на български език, ясно, подредено и изчерпателно.
            Когато потребителят поиска списък с услуги, цени или служители, предостави пълната налична информация.
            Не измисляй услуги, цени, клиенти или свободни часове извън предоставената ти информация.
            """;

        return await GenerateContentAsync(
            systemInstruction,
            userQuery,
            false,
            cancellationToken);
    }

    public async Task<GeminiQueryResult> GenerateJsonQueryAsync(
        string userQuery,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userQuery))
        {
            return GeminiQueryResult.Failed(
                "Заявката не може да бъде празна.");
        }

        const string systemInstruction = """
            Ти преобразуваш въпроси в JSON заявки за BeautyManager.

            Връщай само един валиден JSON обект, без Markdown
            и без текст преди или след JSON обекта.

            Използвай точно следната структура:
            {
              "table": "Services",
              "operation": "select",
              "columns": ["Name", "Category"],
              "filters": [],
              "limit": 50
            }

            Разрешени таблици и полета:
            Clients: Id, Name, Phone, Email, Service, Specialist
            Employees: Id, Name, Specialty, Phone
            Services: Id, Name, Category, Price, DurationMinutes

            Разрешена операция: select.

            Всеки филтър трябва да има полета:
            column, operator, value.

            Разрешени оператори:
            eq, neq, contains, gt, gte, lt, lte.

            Използвай текстови стойности за текстови полета
            и числови стойности за числовите полета.

            Не генерирай SQL.
            Не генерирай операции за добавяне, промяна или изтриване.
            """;

        try
        {
            var json = await GenerateContentAsync(
                systemInstruction,
                userQuery,
                true,
                cancellationToken);

            using var document = JsonDocument.Parse(json);

            if (document.RootElement.ValueKind !=
                JsonValueKind.Object)
            {
                return GeminiQueryResult.Failed(
                    "Gemini не върна JSON обект.");
            }

            var root = document.RootElement;

            if (!root.TryGetProperty("table", out _) ||
                !root.TryGetProperty("operation", out _) ||
                !root.TryGetProperty("columns", out _) ||
                !root.TryGetProperty("filters", out _) ||
                !root.TryGetProperty("limit", out _))
            {
                return GeminiQueryResult.Failed(
                    "JSON заявката няма необходимите полета.");
            }

            return GeminiQueryResult.Succeeded(json);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return GeminiQueryResult.Failed(
                "Заявката към Gemini изтече по време.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (
            ex is HttpRequestException or
            JsonException or
            InvalidOperationException)
        {
            return GeminiQueryResult.Failed(ex.Message);
        }
    }

    private async Task<string> GenerateContentAsync(
        string systemInstruction,
        string userQuery,
        bool jsonMode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "Липсва Gemini API ключ. " +
                "Необходима е конфигурация на средата.");
        }

        if (string.IsNullOrWhiteSpace(_options.Model))
        {
            throw new InvalidOperationException(
                "Не е конфигуриран Gemini модел.");
        }

        object generationConfig = jsonMode
            ? new
            {
                temperature = 0.1,
                maxOutputTokens = 1024,
                responseMimeType = "application/json"
            }
            : new
            {
                temperature = 0.7,
                maxOutputTokens = 2048
            };

        var requestBody = new
        {
            systemInstruction = new
            {
                parts = new[]
                {
                    new { text = systemInstruction }
                }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = userQuery }
                    }
                }
            },
            generationConfig
        };

        var url =
            "https://generativelanguage.googleapis.com/" +
            $"v1beta/models/{_options.Model}:generateContent";

        int maxRetries = 3;
        HttpResponseMessage? response = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                url);

            request.Headers.Add("x-goog-api-key", _options.ApiKey);
            request.Content = JsonContent.Create(requestBody);

            response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                break;
            }

            if ((int)response.StatusCode == 503 || (int)response.StatusCode == 429)
            {
                if (attempt == maxRetries) break;
                await Task.Delay(1500 * attempt, cancellationToken);
                continue;
            }

            break;
        }

        if (response == null || !response.IsSuccessStatusCode)
        {
            var errorDetails = response != null
                ? await response.Content.ReadAsStringAsync(cancellationToken)
                : "No response";

            if (errorDetails.Length > 1500)
            {
                errorDetails = errorDetails[..1500];
            }

            throw new HttpRequestException(
                $"Gemini върна HTTP " +
                $"{(response != null ? (int)response.StatusCode : 0)} " +
                $"({response?.ReasonPhrase}). " +
                $"Подробности: {errorDetails}");
        }

        using var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("candidates", out var candidates) ||
            candidates.ValueKind != JsonValueKind.Array ||
            candidates.GetArrayLength() == 0)
        {
            throw new InvalidOperationException(
                "Gemini не върна отговор. Провери заявката и настройките на модела.");
        }

        var candidate = candidates[0];

        if (!candidate.TryGetProperty("content", out var content) ||
            !content.TryGetProperty("parts", out var parts) ||
            parts.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                "Gemini върна отговор в неочакван формат.");
        }

        var textParts = new List<string>();

        foreach (var part in parts.EnumerateArray())
        {
            if (part.ValueKind == JsonValueKind.Object &&
                part.TryGetProperty("text", out var textElement) &&
                textElement.ValueKind == JsonValueKind.String)
            {
                var text = textElement.GetString();

                if (!string.IsNullOrWhiteSpace(text))
                {
                    textParts.Add(text);
                }
            }
        }

        var answer = string.Join("", textParts).Trim();

        if (string.IsNullOrWhiteSpace(answer))
        {
            throw new InvalidOperationException(
                "Gemini върна празен текстов отговор.");
        }

        return answer;
    }

    public async IAsyncEnumerable<string> GenerateAnswerStreamAsync(
        string userQuery,
        [EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userQuery))
            throw new ArgumentException("Въпросът не може да бъде празен.");

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Липсва Gemini API ключ.");

        if (string.IsNullOrWhiteSpace(_options.Model))
            throw new InvalidOperationException("Не е конфигуриран Gemini модел.");

        const string systemInstruction = """
            Ти си AI асистент на BeautyManager — система за салон за красота.
            Отговаряй на български език, ясно и подредено.
            Не измисляй услуги, цени, клиенти или свободни часове.
            """;

        var requestBody = new
        {
            systemInstruction = new
            {
                parts = new[] { new { text = systemInstruction } }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = userQuery } }
                }
            },
            generationConfig = new
            {
                temperature = 0.7,
                maxOutputTokens = 2048
            }
        };

        var url =
            "https://generativelanguage.googleapis.com/" +
            $"v1beta/models/{_options.Model}:streamGenerateContent?alt=sse";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);

        request.Headers.Add("x-goog-api-key", _options.ApiKey);
        request.Content = JsonContent.Create(requestBody);

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var details = await response.Content.ReadAsStringAsync(cancellationToken);

            if (details.Length > 1000)
                details = details[..1000];

            throw new HttpRequestException(
                $"Gemini върна HTTP {(int)response.StatusCode}: {details}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(
            cancellationToken);

        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(line) ||
                !line.StartsWith("data:"))
                continue;

            var json = line["data:".Length..].Trim();

            if (json == "[DONE]")
                yield break;

            using var document = JsonDocument.Parse(json);

            if (!document.RootElement.TryGetProperty(
                    "candidates", out var candidates) ||
                candidates.ValueKind != JsonValueKind.Array ||
                candidates.GetArrayLength() == 0)
                continue;

            var candidate = candidates[0];

            if (!candidate.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("parts", out var parts) ||
                parts.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var textElement) &&
                    textElement.ValueKind == JsonValueKind.String)
                {
                    var text = textElement.GetString();

                    if (!string.IsNullOrEmpty(text))
                        yield return text;
                }
            }
        }
    }
}