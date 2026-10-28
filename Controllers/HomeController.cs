using Microsoft.AspNetCore.Mvc;
using BeautyManager.Data;

namespace BeautyManager.Controllers
{
    public class HomeController : Controller
    {
        private readonly RepositoryFactory _repositoryFactory;

        public HomeController(RepositoryFactory repositoryFactory)
        {
            _repositoryFactory = repositoryFactory;
        }

        public IActionResult Index()
        {
            ViewBag.ClientsCount = _repositoryFactory.Clients.GetAll().Count();
            ViewBag.ServicesCount = _repositoryFactory.Services.GetAll().Count();
            ViewBag.EmployeesCount = _repositoryFactory.Employees.GetAll().Count();

            return View();
        }
    }
}