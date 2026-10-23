
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using BeautyManager.Configuration;
using BeautyManager.Models;

namespace BeautyManager.Services;

public class GeminiQueryService : IGeminiQueryService
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

    public async Task<GeminiQueryResult> GenerateJsonQueryAsync(
        string userQuery,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userQuery))
        {
            return GeminiQueryResult.Failed(
                "Заявката не може да бъде празна.");
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return GeminiQueryResult.Failed(
                "Липсва Gemini API ключ. Настройте го чрез User Secrets.");
        }

        if (string.IsNullOrWhiteSpace(_options.Model))
        {
            return GeminiQueryResult.Failed(
                "Не е конфигуриран Gemini модел.");
        }

        try
        {
            var requestBody = new
            {
                systemInstruction = new
                {
                    parts = new[]
                    {
                        new
                        {
                            text = """
                                Ти си асистент за търсене в приложението
                                BeautyManager.
                                Преобразувай заявката на потребителя
                                в JSON обект.
                                Връщай само валиден JSON без Markdown.
                                Не генерирай SQL и не изпълнявай команди.
                                Докато форматът JSONQuery не бъде
                                уточнен от екипа, използвай:
                                {"query":"текст на заявката"}
                                """
                        }
                    }
                },
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = userQuery }
                        }
                    }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json"
                }
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://generativelanguage.googleapis.com/v1beta/models/{_options.Model}:generateContent");

            request.Headers.Add("x-goog-api-key", _options.ApiKey);

            request.Content = JsonContent.Create(requestBody);

            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return GeminiQueryResult.Failed(
                    $"Gemini върна HTTP грешка: {(int)response.StatusCode}.");
            }

            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(
                    cancellationToken),
                cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty(
                    "candidates", out var candidates) ||
                candidates.ValueKind != JsonValueKind.Array ||
                candidates.GetArrayLength() == 0)
            {
                return GeminiQueryResult.Failed(
                    "Gemini не върна резултат.");
            }

            var candidate = candidates[0];

            if (!candidate.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("parts", out var parts) ||
                parts.ValueKind != JsonValueKind.Array ||
                parts.GetArrayLength() == 0 ||
                !parts[0].TryGetProperty("text", out var textElement))
            {
                return GeminiQueryResult.Failed(
                    "Отговорът на Gemini е в неочакван формат.");
            }

            var text = textElement.GetString();

            if (string.IsNullOrWhiteSpace(text))
            {
                return GeminiQueryResult.Failed(
                    "Gemini върна празен отговор.");
            }

            using var jsonDocument = JsonDocument.Parse(text);

            if (jsonDocument.RootElement.ValueKind !=
                JsonValueKind.Object)
            {
                return GeminiQueryResult.Failed(
                    "Отговорът не е JSON обект.");
            }

            return GeminiQueryResult.Succeeded(text);
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
        catch (HttpRequestException)
        {
            return GeminiQueryResult.Failed(
                "Не може да се осъществи връзка с Gemini.");
        }
        catch (JsonException)
        {
            return GeminiQueryResult.Failed(
                "Gemini върна невалиден JSON отговор.");
        }
    }
}
