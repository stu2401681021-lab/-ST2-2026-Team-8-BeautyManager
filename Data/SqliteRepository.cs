using System.Globalization;
using Microsoft.Data.Sqlite;

namespace BeautyManager.Data;
public sealed class SqliteRepository<T> where T : class, new()
{
	private readonly Database _db;
	private readonly string _table;
	private readonly string[] _columns;
	private readonly System.Reflection.PropertyInfo[] _properties;

	public SqliteRepository(Database db, string table, params string[] columns)
	{
		_db = db;
		_table = table;
		_columns = columns;
		_properties = columns.Select(c => typeof(T).GetProperty(c) ?? throw new ArgumentException(c)).ToArray();
	}

	private T Read(SqliteDataReader reader)
	{
		var item = new T();
		typeof(T).GetProperty("Id")!.SetValue(item, reader.GetInt32(0));
		for (var i = 0; i < _properties.Length; i++)
		{
			var val = reader.GetValue(i + 1);
			var type = _properties[i].PropertyType;
			object converted = type == typeof(DateTime) ? DateTime.Parse((string)val, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
				: type == typeof(decimal) ? Convert.ToDecimal(val, CultureInfo.InvariantCulture)
				: Convert.ChangeType(val, type, CultureInfo.InvariantCulture);
			_properties[i].SetValue(item, converted);
		}
		return item;
	}

	public List<T> GetAll(string? filter = null)
	{
		using var conn = _db.CreateOpenConnection();
		using var cmd = conn.CreateCommand();

		var searchable = _properties.Where(x => x.PropertyType == typeof(string)).Select(x => x.Name).ToArray();
		var where = !string.IsNullOrWhiteSpace(filter) && searchable.Length > 0
			? " WHERE " + string.Join(" OR ", searchable.Select(c => $"{c} LIKE $filter COLLATE NOCASE")) : "";
		cmd.CommandText = $"SELECT Id,{string.Join(',', _columns)} FROM {_table}{where} ORDER BY Id DESC";
		if (where.Length > 0) cmd.Parameters.AddWithValue("$filter", $"%{filter!.Trim()}%");
		using var r = cmd.ExecuteReader();
		var list = new List<T>();
		while (r.Read()) list.Add(Read(r));
		return list;
	}

	public T? Get(int id)
	{
		using var conn = _db.CreateOpenConnection();
		using var cmd = conn.CreateCommand();
		cmd.CommandText = $"SELECT Id,{string.Join(',', _columns)} FROM {_table} WHERE Id=$id";
		cmd.Parameters.AddWithValue("$id", id);
		using var r = cmd.ExecuteReader();
		return r.Read() ? Read(r) : null;
	}

	private void Bind(SqliteCommand cmd, T entity)
	{
		for (var i = 0; i < _columns.Length; i++)
		{
			object? v = _properties[i].GetValue(entity);
			if (v is DateTime dt) v = dt.ToString("O", CultureInfo.InvariantCulture);
			if (v is decimal d) v = (double)d;
			cmd.Parameters.AddWithValue("$p" + i, v ?? DBNull.Value);
		}
	}

	public int Insert(T entity)
	{
		using var conn = _db.CreateOpenConnection();
		using var cmd = conn.CreateCommand();
		cmd.CommandText = $"INSERT INTO {_table} ({string.Join(',', _columns)}) VALUES ({string.Join(',', Enumerable.Range(0, _columns.Length).Select(i => "$p" + i))}); SELECT last_insert_rowid();";
		Bind(cmd, entity);
		return Convert.ToInt32(cmd.ExecuteScalar());
	}

	public bool Update(int id, T entity)
	{
		using var conn = _db.CreateOpenConnection();
		using var cmd = conn.CreateCommand();
		cmd.CommandText = $"UPDATE {_table} SET {string.Join(',', _columns.Select((c, i) => $"{c}=$p{i}"))} WHERE Id=$id";
		cmd.Parameters.AddWithValue("$id", id);
		Bind(cmd, entity);
		return cmd.ExecuteNonQuery() > 0;
	}

	public bool Delete(int id)
	{
		using var conn = _db.CreateOpenConnection();
		using var cmd = conn.CreateCommand();
		cmd.CommandText = $"DELETE FROM {_table} WHERE Id=$id";
		cmd.Parameters.AddWithValue("$id", id);
		return cmd.ExecuteNonQuery() > 0;
	}
}
