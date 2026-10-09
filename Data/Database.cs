
using BeautyManager.Models;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace BeautyManager.Data;

/// <summary>
/// Singleton реализация на IDatabase върху локална SQLite БД.
/// </summary>
public sealed class Database : IDatabase
{
    // =====================================
    // УЧАСТНИК 1 - SINGLETON DESIGN PATTERN
    // =====================================

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

    // =====================================
    // УЧАСТНИК 1 - СЪЗДАВАНЕ НА БАЗАТА
    // =====================================

    private void EnsureCreated()
    {
        var builder = new SqliteConnectionStringBuilder(
            _connectionString);

        var dir = Path.GetDirectoryName(
            Path.GetFullPath(builder.DataSource));

        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

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
            """;

        cmd.ExecuteNonQuery();
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

    // =====================================
    // УЧАСТНИК 1 - INSERT
    // =====================================

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
}
