using Microsoft.AspNetCore.Mvc.Rendering;
using BeautyManager.Data;
using BeautyManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace BeautyManager.Controllers;

public class ClientsController : Controller
{
    private readonly IDatabase _database;
    private readonly RepositoryFactory _repositories;

    public ClientsController(
        IDatabase database,
        RepositoryFactory repositories)
    {
        _database = database;
        _repositories = repositories;
    }

    private void LoadServices(string? selectedService = null)
    {
        var services = _repositories.Services.GetAll();

        ViewBag.Services = new SelectList(
            services,
            nameof(Service.Name),
            nameof(Service.Name),
            selectedService);
    }

    private bool ServiceExists(string? serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            return false;

        return _repositories.Services.GetAll().Any(s =>
            string.Equals(
                s.Name,
                serviceName,
                StringComparison.OrdinalIgnoreCase));
    }

    [HttpGet]
    public IActionResult Index(string? search, int page = 1, int pageSize = 10)
    {
        var allClients = _database.GetFiltered(search);

        int totalItems = allClients.Count;
        int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        var pagedClients = allClients
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalItems = totalItems;

        return View(pagedClients);
    }

    [HttpGet]
    public IActionResult Create()
    {
        LoadServices();
        return View(new Client());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Client client)
    {
        if (ModelState.IsValid)
        {
            if (_database.NameExists(client.Name))
            {
                ModelState.AddModelError(
                    nameof(Client.Name),
                    "Вече има клиент с това име.");
            }

            if (!ServiceExists(client.Service))
            {
                ModelState.AddModelError(
                    nameof(Client.Service),
                    "Моля, избери съществуваща услуга.");
            }

            if (_database.PhoneExists(client.Phone))
            {
                ModelState.AddModelError(
                    nameof(Client.Phone),
                    "Вече има клиент с този телефон.");
            }
        }

        if (!ModelState.IsValid)
        {
            LoadServices(client.Service);
            return View(client);
        }

        try
        {
            _database.Add(client);

            TempData["Success"] = "Клиентът е добавен успешно!";

            return RedirectToAction(nameof(Create));
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            ModelState.AddModelError(
                string.Empty,
                "Името или телефонът вече се използва.");
            LoadServices(client.Service);
            return View(client);
        }
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var client = _database.GetById(id);

        if (client == null)
        {
            return NotFound();
        }

        LoadServices(client.Service);
        return View(client);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, Client client)
    {
        if (id != client.Id)
        {
            return BadRequest();
        }

        if (_database.GetById(id) == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            LoadServices(client.Service);
            return View(client);
        }

        try
        {
            bool updated = _database.Update(client);

            if (!updated)
            {
                return NotFound();
            }

            TempData["Success"] = "Данните на клиента са обновени успешно!";

            return RedirectToAction(nameof(Index));
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            ModelState.AddModelError(
                string.Empty,
                "Вече съществува клиент с това име или телефон.");

            LoadServices(client.Service);
            return View(client);
        }
    }

    [HttpGet]
    public IActionResult Delete(int id)
    {
        var client = _database.GetById(id);

        if (client == null)
        {
            return NotFound();
        }

        LoadServices(client.Service);
        return View(client);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        bool deleted = _database.Delete(id);

        if (!deleted)
        {
            return NotFound();
        }

        TempData["Success"] = "Клиентът е изтрит успешно!";

        return RedirectToAction(nameof(Index));
    }
}