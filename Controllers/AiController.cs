using System.Net.Http;
using System.Text.Json;
using BeautyManager.Models;
using BeautyManager.Services;
using Microsoft.AspNetCore.Mvc;

namespace BeautyManager.Controllers;

public sealed class AIController : Controller
{
    private readonly IGeminiQueryService _gemini;

    public AIController(IGeminiQueryService gemini)
    {
        _gemini = gemini;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new AiQuestion());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        AiQuestion model,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            model.Answer = await _gemini.GenerateAnswerAsync(
                model.Question,
                ct);
        }
        catch (OperationCanceledException)
            when (!ct.IsCancellationRequested)
        {
            model.Error =
                "Заявката към Gemini изтече по време. Опитай отново.";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (
            ex is HttpRequestException or
            InvalidOperationException or
            ArgumentException or
            JsonException)
        {
            model.Error = "AI заявката не успя: " + ex.Message;
        }

        return View(model);
    }
}