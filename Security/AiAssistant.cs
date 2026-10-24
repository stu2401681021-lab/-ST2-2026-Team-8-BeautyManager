using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
namespace BeautyManager.Services;

public sealed class AiAssistant
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    public AiAssistant(HttpClient http, IConfiguration config) { _http = http; _config = config; }

    public async Task<string> AskAsync(string question, CancellationToken ct)
    {
        var key = _config["Ai:ApiKey"];
        if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("AI не е конфигуриран. Задайте Ai__ApiKey като environment variable.");
        var endpoint = _config["Ai:Endpoint"] ?? "https://api.openai.com/v1/chat/completions";
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("AI endpoint трябва да е валиден HTTPS адрес.");
        var payload = new
        {
            model = _config["Ai:Model"] ?? "gpt-4o-mini",
            messages = new[] {
                new { role = "system", content = "Ти си учтив помощник на салон за красота BeautyManager. Отговаряй на български. Не твърди, че знаеш свободни часове или цени, ако не са предоставени. Не поставяй медицински диагнози." },
                new { role = "user", content = question }
            },
            max_tokens = 350
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"AI услугата върна HTTP {(int)response.StatusCode}. Проверете настройките и квотата.");
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "Няма отговор.";
    }
}
