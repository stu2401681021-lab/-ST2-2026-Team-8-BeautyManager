using System.Net.Http;
using System.Text.Json;
using System.Linq;
using BeautyManager.Data;
using BeautyManager.Models;
using BeautyManager.Services;
using Microsoft.AspNetCore.Mvc;

namespace BeautyManager.Controllers;

public sealed class AIController : Controller
{
    private readonly IGeminiQueryService _gemini;
    private readonly RepositoryFactory _repositoryFactory;

    public AIController(IGeminiQueryService gemini, RepositoryFactory repositoryFactory)
    {
        _gemini = gemini;
        _repositoryFactory = repositoryFactory;
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
            var services = _repositoryFactory.Services.GetAll();
            var employees = _repositoryFactory.Employees.GetAll();

            var servicesList = string.Join("\n", services.Select(s => $"- {s.Name} ({s.Category}): {s.Price} лв., времетраене: {s.DurationMinutes} мин."));
            var employeesList = string.Join("\n", employees.Select(e => $"- {e.Name} - {e.Specialty} (Тел: {e.Phone})"));

            string promptWithContext =
                "Ти си любезен и полезен асистент на салон за красота 'BeautyManager'.\n" +
                "Отговаряй подредено, структурирано и изчерпателно на български език, като използваш следната актуална информация от базата данни:\n\n" +
                $"ПРЕДЛАГАНИ УСЛУГИ:\n{servicesList}\n\n" +
                $"ЕКИП И СПЕЦИАЛИСТИ:\n{employeesList}\n\n" +
                $"Запитване от клиент: {model.Question}";

            model.Answer = await _gemini.GenerateAnswerAsync(promptWithContext, ct);
        }
        catch (OperationCanceledException)
            when (!ct.IsCancellationRequested)
        {
            model.Error = "Заявката към Gemini изтече по време. Опитай отново.";
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