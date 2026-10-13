
using BeautyManager.Data;
using BeautyManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace BeautyManager.Controllers;

public class ClientsController : Controller
{
    private readonly IDatabase _database;

    public ClientsController(IDatabase database)
    {
        _database = database;
    }


    [HttpGet]
    public IActionResult Create()
    {
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

            if (_database.PhoneExists(client.Phone))
            {
                ModelState.AddModelError(
                    nameof(Client.Phone),
                    "Вече има клиент с този телефон.");
            }
        }

        if (!ModelState.IsValid)
        {
            return View(client);
        }

        try
        {
            _database.Add(client);

            TempData["Success"] =
                "Клиентът е добавен успешно!";

            return RedirectToAction(nameof(Create));
        }
        catch (SqliteException ex)
            when (ex.SqliteErrorCode == 19)
        {
            ModelState.AddModelError(
                string.Empty,
                "Името или телефонът вече се използва.");

            return View(client);
        }
    }


    [HttpGet]
    public IActionResult Index(string? search)
    {
        
        var clients = _database.GetFiltered(search);

        ViewBag.Search = search;

        return View(clients);
    }


    [HttpGet]
    public IActionResult Edit(int id)
    {
        var client = _database.GetById(id);

        if (client == null)
        {
            return NotFound();
        }

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
            return View(client);
        }

        try
        {
            bool updated = _database.Update(client);

            if (!updated)
            {
                return NotFound();
            }

            TempData["Success"] =
                "Данните на клиента са обновени успешно!";

            return RedirectToAction(nameof(Index));
        }
        catch (SqliteException ex)
            when (ex.SqliteErrorCode == 19)
        {
            ModelState.AddModelError(
                string.Empty,
                "Вече съществува клиент с това име или телефон.");

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

        TempData["Success"] =
            "Клиентът е изтрит успешно!";

        return RedirectToAction(nameof(Index));
    }

}
