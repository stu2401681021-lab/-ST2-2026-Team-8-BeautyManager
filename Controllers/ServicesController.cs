
using BeautyManager.Data;
using BeautyManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace BeautyManager.Controllers;

public sealed class ServicesController : Controller
{
    private readonly RepositoryFactory _factory;

    public ServicesController(RepositoryFactory factory)
    {
        _factory = factory;
    }

    private SqliteRepository<Service> Repo => _factory.Services;

    public IActionResult Index(string? filter)
    {
        ViewBag.Filter = filter;
        return View(Repo.GetAll(filter));
    }

    public IActionResult Details(int id)
    {
        var item = Repo.Get(id);
        return item is null ? NotFound() : View(item);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new Service());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Service model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            Repo.Insert(model);
            TempData["Message"] = "Услугата е добавена успешно.";
            return RedirectToAction(nameof(Index));
        }
        catch (SqliteException)
        {
            ModelState.AddModelError(
                string.Empty,
                "Неуспешен запис. Проверете въведените данни.");

            return View(model);
        }
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var item = Repo.Get(id);

        if (item is null)
            return NotFound();

        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, Service model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            if (!Repo.Update(id, model))
                return NotFound();

            TempData["Message"] = "Услугата е редактирана успешно.";
            return RedirectToAction(nameof(Index));
        }
        catch (SqliteException)
        {
            ModelState.AddModelError(
                string.Empty,
                "Неуспешна промяна на услугата.");

            return View(model);
        }
    }

    [HttpGet]
    public IActionResult Delete(int id)
    {
        var item = Repo.Get(id);

        if (item is null)
            return NotFound();

        return View(item);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        try
        {
            if (!Repo.Delete(id))
                return NotFound();

            TempData["Message"] = "Услугата е изтрита успешно.";
        }
        catch (SqliteException)
        {
            TempData["Message"] =
                "Услугата е свързана с други данни и не може да бъде изтрита.";
        }

        return RedirectToAction(nameof(Index));
    }
}
