using BeautyManager.Models;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace BeautyManager.Data;

public sealed class Database : IDatabase
{
    private static readonly object Lock = new();
    private static Database? _instance;

    private readonly string _connectionString;

    private Database(string connectionString)
    {
        _connectionString = connectionString;
        EnsureCreated();
    }

    public static Database Instance =>
        _instance ?? throw new InvalidOperationException(
            "Database не е инициализирана. Извикайте Database.Initialize(connectionString).");

    public static Database Initialize(string connectionString)
    {
        if (_instance is null)
        {
            lock (Lock)
            {
                _instance ??= new Database(connectionString);
            }
        }

        return _instance;
    }

    public SqliteConnection CreateOpenConnection() => Open();

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private void EnsureCreated()
    {
        var builder = new SqliteConnectionStringBuilder(_connectionString);

        var dir = Path.GetDirectoryName(
            Path.GetFullPath(builder.DataSource));

        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var isNewFile = !File.Exists(builder.DataSource);

        using var connection = Open();
        using var cmd = connection.CreateCommand();

        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Clients (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                NameKey TEXT NOT NULL UNIQUE,
                Phone TEXT NOT NULL UNIQUE,
                Email TEXT NOT NULL,
                Service TEXT NULL,
                Specialist TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS Employees (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Specialty TEXT NULL,
                Phone TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS Services (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Category TEXT NOT NULL,
                Price REAL NOT NULL,
                DurationMinutes INTEGER NOT NULL
            );
            """;

        cmd.ExecuteNonQuery();

        if (isNewFile)
        {
            Seed(connection);
        }
    }

    private static void Seed(SqliteConnection connection)
    {
        using var tx = connection.BeginTransaction();

        var serviceList = new[]
        {
            new { Name = "Дамско подстригване и сешоар", Category = "Коса", Price = 45.0, Duration = 60 },
            new { Name = "Мъжко подстригване и оформяне на брада", Category = "Коса", Price = 30.0, Duration = 40 },
            new { Name = "Боядисване на коса (корени)", Category = "Коса", Price = 60.0, Duration = 90 },
            new { Name = "Балеаж / Кичури", Category = "Коса", Price = 140.0, Duration = 180 },
            new { Name = "Възстановяваща кератинова терапия", Category = "Коса", Price = 80.0, Duration = 75 },
            new { Name = "Детско подстригване", Category = "Коса", Price = 20.0, Duration = 30 },

            new { Name = "Класически маникюр с обикновен лак", Category = "Нокти", Price = 30.0, Duration = 40 },
            new { Name = "Маникюр с гел лак", Category = "Нокти", Price = 45.0, Duration = 60 },
            new { Name = "Изграждане на нокти с гел", Category = "Нокти", Price = 85.0, Duration = 120 },
            new { Name = "Поддръжка на гел / Ноктопластика", Category = "Нокти", Price = 60.0, Duration = 90 },
            new { Name = "СПА Педикюр с гел лак", Category = "Нокти", Price = 65.0, Duration = 75 },
            new { Name = "Сваляне на гел лак", Category = "Нокти", Price = 15.0, Duration = 20 },

            new { Name = "Дълбоко почистване на лице", Category = "Козметика", Price = 70.0, Duration = 75 },
            new { Name = "Хидратираща терапия за лице с хиалурон", Category = "Козметика", Price = 90.0, Duration = 60 },
            new { Name = "Анти-ейдж терапия с колаген", Category = "Козметика", Price = 110.0, Duration = 60 },
            new { Name = "Ламиниране на вежди и мигли", Category = "Козметика", Price = 55.0, Duration = 60 },
            new { Name = "Миглопластика (Косъм по косъм)", Category = "Козметика", Price = 95.0, Duration = 120 },
            new { Name = "Миглопластика (3D/Volume)", Category = "Козметика", Price = 120.0, Duration = 150 },

            new { Name = "Дневен / Вечерен професионален грим", Category = "Грим", Price = 75.0, Duration = 60 },
            new { Name = "Сватбен грим (с пробен грим)", Category = "Грим", Price = 150.0, Duration = 120 },
            new { Name = "Класически релаксиращ масаж (цяло тяло)", Category = "Масажи", Price = 70.0, Duration = 60 },
            new { Name = "Антицелулитен масаж", Category = "Масажи", Price = 50.0, Duration = 45 }
        };

        foreach (var s in serviceList)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO Services (Name, Category, Price, DurationMinutes) VALUES ($name, $category, $price, $duration)";
            cmd.Parameters.AddWithValue("$name", s.Name);
            cmd.Parameters.AddWithValue("$category", s.Category);
            cmd.Parameters.AddWithValue("$price", s.Price);
            cmd.Parameters.AddWithValue("$duration", s.Duration);
            cmd.ExecuteNonQuery();
        }

        var employeeList = new[]
        {
            new { Name = "Анна Иванова", Specialty = "Топ Маникюрист и Педикюрист", Phone = "+359888111222" },
            new { Name = "Мария Петрова", Specialty = "Фризьор - Стилист и Колорист", Phone = "+359888333444" },
            new { Name = "Елена Георгиева", Specialty = "Козметик и Естетик", Phone = "+359888555666" },
            new { Name = "София Димитрова", Specialty = "Професионален Гримьор", Phone = "+359888777888" },
            new { Name = "Виктор Игов", Specialty = "Барбър и Мъжки стилист", Phone = "+359888999000" },
            new { Name = "Даниела Николова", Specialty = "Специалист Миглопластика", Phone = "+359887123456" },
            new { Name = "Георги Колев", Specialty = "Масажист и Терапевт", Phone = "+359887654321" },
            new { Name = "Кристина Стоянова", Specialty = "Маникюрист - Ноктопластика", Phone = "+359886987654" }
        };

        foreach (var e in employeeList)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO Employees (Name, Specialty, Phone) VALUES ($name, $specialty, $phone)";
            cmd.Parameters.AddWithValue("$name", e.Name);
            cmd.Parameters.AddWithValue("$specialty", e.Specialty);
            cmd.Parameters.AddWithValue("$phone", e.Phone);
            cmd.ExecuteNonQuery();
        }

        string[] clientNames =
        {
            "Иван Петров", "Мария Георгиева", "Георги Димитров", "Елена Николова", "Стефан Иванов",
            "Десислава Тодорова", "Николай Стоянов", "Калина Маринова", "Петър Христов", "Виктория Колева",
            "Александър Попов", "Радостина Илиева", "Борис Андреев", "Лора Савова", "Димитър Ненов"
        };

        for (var i = 0; i < clientNames.Length; i++)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO Clients (Name, NameKey, Phone, Email, Service, Specialist) VALUES ($name, $nameKey, $phone, $email, $service, $specialist)";
            cmd.Parameters.AddWithValue("$name", clientNames[i]);
            cmd.Parameters.AddWithValue("$nameKey", NameKey(clientNames[i]));
            cmd.Parameters.AddWithValue("$phone", $"+35987{8000000 + i * 137}");
            cmd.Parameters.AddWithValue("$email", $"client{i + 1}@beauty.bg");
            cmd.Parameters.AddWithValue("$service", serviceList[i % serviceList.Length].Name);
            cmd.Parameters.AddWithValue("$specialist", employeeList[i % employeeList.Length].Name);
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
    }

    private static string NameKey(string name) =>
        Regex.Replace(name.Trim(), @"\s+", " ")
            .ToLowerInvariant();

    private static void Bind(SqliteCommand cmd, Client c)
    {
        cmd.Parameters.AddWithValue("$name", c.Name.Trim());
        cmd.Parameters.AddWithValue("$nameKey", NameKey(c.Name));
        cmd.Parameters.AddWithValue("$phone", c.Phone.Trim());
        cmd.Parameters.AddWithValue("$email", c.Email.Trim());

        cmd.Parameters.AddWithValue(
            "$service",
            string.IsNullOrWhiteSpace(c.Service)
                ? DBNull.Value : c.Service.Trim());

        cmd.Parameters.AddWithValue(
            "$specialist",
            string.IsNullOrWhiteSpace(c.Specialist)
                ? DBNull.Value : c.Specialist.Trim());
    }

    public int Add(Client client)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();

        cmd.CommandText = """
            INSERT INTO Clients
            (Name, NameKey, Phone, Email, Service, Specialist)
            VALUES
            ($name, $nameKey, $phone, $email, $service, $specialist);

            SELECT last_insert_rowid();
            """;

        Bind(cmd, client);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public bool NameExists(string name) =>
        Exists("NameKey", NameKey(name));

    public bool PhoneExists(string phone) =>
        Exists("Phone", phone.Trim());

    private bool Exists(string column, string value)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();

        cmd.CommandText =
            $"SELECT 1 FROM Clients WHERE {column} = $value LIMIT 1";

        cmd.Parameters.AddWithValue("$value", value);

        return cmd.ExecuteScalar() is not null;
    }

    public IReadOnlyList<Client> GetFiltered(string? search)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();

        cmd.CommandText = """
            SELECT Id, Name, Phone, Email, Service, Specialist
            FROM Clients
            WHERE
                $search = ''
                OR instr(lower(Name), lower($search)) > 0
                OR instr(Phone, $search) > 0
                OR instr(lower(Email), lower($search)) > 0
                OR instr(lower(COALESCE(Service, '')), lower($search)) > 0
                OR instr(lower(COALESCE(Specialist, '')), lower($search)) > 0
            ORDER BY Name;
            """;

        cmd.Parameters.AddWithValue(
            "$search", search?.Trim() ?? "");

        using var reader = cmd.ExecuteReader();

        var clients = new List<Client>();

        while (reader.Read())
        {
            clients.Add(new Client
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Phone = reader.GetString(2),
                Email = reader.GetString(3),

                Service = reader.IsDBNull(4)
                    ? null : reader.GetString(4),

                Specialist = reader.IsDBNull(5)
                    ? null : reader.GetString(5)
            });
        }

        return clients;
    }

    public Client? GetById(int id)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();

        cmd.CommandText = """
            SELECT Id, Name, Phone, Email, Service, Specialist
            FROM Clients
            WHERE Id = $id;
            """;

        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
        {
            return null;
        }

        return new Client
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Phone = reader.GetString(2),
            Email = reader.GetString(3),

            Service = reader.IsDBNull(4)
                ? null : reader.GetString(4),

            Specialist = reader.IsDBNull(5)
                ? null : reader.GetString(5)
        };
    }

    public bool Update(Client client)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();

        cmd.CommandText = """
            UPDATE Clients
            SET
                Name = $name,
                NameKey = $nameKey,
                Phone = $phone,
                Email = $email,
                Service = $service,
                Specialist = $specialist
            WHERE Id = $id;
            """;

        Bind(cmd, client);

        cmd.Parameters.AddWithValue("$id", client.Id);

        return cmd.ExecuteNonQuery() > 0;
    }

    public bool Delete(int id)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();

        cmd.CommandText = """
            DELETE FROM Clients
            WHERE Id = $id;
            """;

        cmd.Parameters.AddWithValue("$id", id);

        return cmd.ExecuteNonQuery() > 0;
    }

    public IReadOnlyList<Service> GetAllServices()
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Category, Price, DurationMinutes FROM Services ORDER BY Name";
        using var reader = cmd.ExecuteReader();
        var result = new List<Service>();
        while (reader.Read())
        {
            result.Add(new Service
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Category = reader.GetString(2),
                Price = Convert.ToDecimal(reader.GetDouble(3)),
                DurationMinutes = reader.GetInt32(4)
            });
        }
        return result;
    }

    public IReadOnlyList<Employee> GetAllEmployees()
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Specialty, Phone FROM Employees ORDER BY Name";
        using var reader = cmd.ExecuteReader();
        var result = new List<Employee>();
        while (reader.Read())
        {
            result.Add(new Employee
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Specialty = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Phone = reader.IsDBNull(3) ? string.Empty : reader.GetString(3)
            });
        }
        return result;
    }
}