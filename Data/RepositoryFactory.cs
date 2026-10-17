
using BeautyManager.Models;

namespace BeautyManager.Data;

public sealed class RepositoryFactory
{
    private readonly Database _db;

    public RepositoryFactory(IDatabase db)
    {
        _db = (Database)db;
    }

    public SqliteRepository<Service> Services =>
        new(_db, "Services", "Name", "Category", "Price", "DurationMinutes");

    public SqliteRepository<Employee> Employees =>
        new(_db, "Employees", "Name", "Specialty", "Phone");
}
