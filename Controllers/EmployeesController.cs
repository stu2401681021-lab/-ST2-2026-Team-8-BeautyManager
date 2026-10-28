using BeautyManager.Data;
using BeautyManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace BeautyManager.Controllers;

public sealed class EmployeesController : Controller
{
	private readonly RepositoryFactory _factory;
	public EmployeesController(RepositoryFactory factory)
	{
		_factory = factory;
	}
	private SqliteRepository<Employee> Repo => _factory.Employees;


    public IActionResult Index(string? filter, int page = 1, int pageSize = 10)
    {
        int[] allowedSizes = { 5, 10, 25, 50 };

        if (!allowedSizes.Contains(pageSize))
            pageSize = 10;

        var employees = Repo.GetAll(filter).ToList();

        int totalCount = employees.Count;
        int totalPages = Math.Max(1,
            (int)Math.Ceiling(totalCount / (double)pageSize));

        page = Math.Clamp(page, 1, totalPages);

        ViewBag.Filter = filter;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalCount = totalCount;

        return View(employees
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList());
    }

    public IActionResult Details(int id) { 
		var item = Repo.Get(id); 
		return item is null ? NotFound() : View(item); 
	}
	public IActionResult Create() { return View(new Employee()); }

	[HttpPost, ValidateAntiForgeryToken]
	public IActionResult Create(Employee model)
	{
		if (!ModelState.IsValid) { return View(model); }
		try { Repo.Insert(model); TempData["Message"] = "Записът е добавен."; return RedirectToAction(nameof(Index)); }
		catch (SqliteException) { ModelState.AddModelError("", "Неуспешен запис. Проверете свързаните данни."); return View(model); }
	}
	public IActionResult Edit(int id) { var item = Repo.Get(id); if (item is null) return NotFound(); return View(item); }
	[HttpPost, ValidateAntiForgeryToken]
	public IActionResult Edit(int id, Employee model)
	{
		if (id != model.Id) return BadRequest();
		if (!ModelState.IsValid) { return View(model); }
		try { if (!Repo.Update(id, model)) return NotFound(); TempData["Message"] = "Записът е обновен."; return RedirectToAction(nameof(Index)); }
		catch (SqliteException) { ModelState.AddModelError("", "Неуспешна промяна на записа."); return View(model); }
	}
	public IActionResult Delete(int id) { var item = Repo.Get(id); return item is null ? NotFound() : View(item); }
	[HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
	public IActionResult DeleteConfirmed(int id)
	{
		try { if (!Repo.Delete(id)) return NotFound(); TempData["Message"] = "Записът е изтрит."; }
		catch (SqliteException) { TempData["Message"] = "Записът е свързан с други данни и не може да бъде изтрит."; }
		return RedirectToAction(nameof(Index));
	}
}
