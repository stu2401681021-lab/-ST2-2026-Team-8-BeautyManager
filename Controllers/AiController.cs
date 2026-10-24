using BeautyManager.Models;
using BeautyManager.Services;
using Microsoft.AspNetCore.Mvc;
namespace BeautyManager.Controllers;
public sealed class AIController : Controller
{
    private readonly AiAssistant _assistant;
    public AIController(AiAssistant assistant) => _assistant = assistant;
    [HttpGet] public IActionResult Index() => View(new AiQuestion());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(AiQuestion question, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(question);
        try { question.Answer = await _assistant.AskAsync(question.Question, ct); }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        { question.Error = "AI заявката не успя: " + ex.Message; }
        return View(question);
    }
}
