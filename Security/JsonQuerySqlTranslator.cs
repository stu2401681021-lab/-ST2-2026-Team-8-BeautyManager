
using BeautyManager.Models;
using Microsoft.Data.Sqlite;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace BeautyManager.Security;



public sealed class JsonQuerySqlTranslator
{
    public SqlTranslationResult Translate(JSONQuery query)
    {
        if (query == null)
            throw new ArgumentException("Липсва JSON заявка.");


        if (string.IsNullOrWhiteSpace(query.Operation) ||
            !AllowedQuerySchema.AllowedOperations.Contains(
                query.Operation))
        {
            throw new ArgumentException(
                "Неразрешена SQL операция. Позволен е само SELECT.");
        }


        if (string.IsNullOrWhiteSpace(query.Table) ||
            !AllowedQuerySchema.Tables.TryGetValue(
                query.Table, out var allowedColumns))
        {
            throw new ArgumentException("Неразрешена таблица.");
        }


        var tableName = AllowedQuerySchema.Tables.Keys
            .First(t => string.Equals(
                t, query.Table,
                StringComparison.OrdinalIgnoreCase));

        if (query.Columns == null ||
            query.Columns.Count == 0 ||
            query.Columns.Count > 20)
        {
            throw new ArgumentException(
                "Посочете между 1 и 20 колони.");
        }


        var selectedColumns = new List<string>();

        foreach (var column in query.Columns)
        {
            if (string.IsNullOrWhiteSpace(column) ||
                !allowedColumns.ContainsKey(column))
            {
                throw new ArgumentException(
                    $"Неразрешена колона: {column}");
            }

            var safeColumn = allowedColumns.Keys.First(
                c => string.Equals(c, column,
                    StringComparison.OrdinalIgnoreCase));

            selectedColumns.Add($"\"{safeColumn}\"");
        }


        if (query.Limit < 1 ||
            query.Limit > AllowedQuerySchema.MaxLimit)
        {
            throw new ArgumentException(
                $"LIMIT трябва да е между 1 и " +
                $"{AllowedQuerySchema.MaxLimit}.");
        }

        var sql = new StringBuilder();

        sql.Append("SELECT ");
        sql.Append(string.Join(", ", selectedColumns));
        sql.Append(" FROM \"");
        sql.Append(tableName);
        sql.Append("\"");

        var parameters = new List<SqliteParameter>();
        var conditions = new List<string>();


        var filters = query.Filters
            ?? throw new ArgumentException("Невалидни филтри.");

        if (filters.Count > 10)
            throw new ArgumentException(
                "Позволени са максимум 10 филтъра.");

        for (int i = 0; i < filters.Count; i++)
        {
            var filter = filters[i];

            if (filter == null ||
                string.IsNullOrWhiteSpace(filter.Column) ||
                !allowedColumns.TryGetValue(
                    filter.Column, out var columnType))
            {
                throw new ArgumentException(
                    "Неразрешено поле за филтриране.");
            }

            var columnName = allowedColumns.Keys.First(
                c => string.Equals(c, filter.Column,
                    StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(filter.Operator) ||
                !AllowedQuerySchema.AllowedOperators.Contains(
                    filter.Operator))
            {
                throw new ArgumentException(
                    "Неподдържан оператор.");
            }

            string op = filter.Operator.ToLowerInvariant();


            bool numeric = columnType == typeof(int) ||
                           columnType == typeof(double);

            if (op == "contains" && columnType != typeof(string))
                throw new ArgumentException(
                    "contains е позволен само за текст.");

            if (new[] { "gt", "gte", "lt", "lte" }.Contains(op)
                && !numeric)
                throw new ArgumentException(
                    "Сравненията gt/gte/lt/lte са само за числа.");

            string parameterName = $"$p{i}";
            object value;


            var jsonValue = filter.Value;

            if (columnType == typeof(string))
            {
                if (jsonValue.ValueKind != JsonValueKind.String)
                    throw new ArgumentException(
                        "Очаква се текстова стойност.");

                value = jsonValue.GetString()
                    ?? throw new ArgumentException(
                        "Невалидна текстова стойност.");
            }
            else if (columnType == typeof(int))
            {
                if (jsonValue.ValueKind != JsonValueKind.Number ||
                    !jsonValue.TryGetInt32(out int number))
                    throw new ArgumentException(
                        "Очаква се цяло число.");

                value = number;
            }
            else if (columnType == typeof(double))
            {
                if (jsonValue.ValueKind != JsonValueKind.Number ||
                    !jsonValue.TryGetDouble(out double number) ||
                    double.IsNaN(number) ||
                    double.IsInfinity(number))
                    throw new ArgumentException(
                        "Очаква се валидно число.");

                value = number;
            }
            else
            {
                throw new ArgumentException(
                    "Неподдържан тип стойност.");
            }

            string sqlOperator = op switch
            {
                "eq" => "=",
                "neq" => "<>",
                "gt" => ">",
                "gte" => ">=",
                "lt" => "<",
                "lte" => "<=",
                "contains" => "LIKE",
                _ => throw new ArgumentException(
                    "Неподдържан оператор.")
            };

            if (op == "contains")
            {
                
                var text = (string)value;

                text = text.Replace("\\", "\\\\")
                           .Replace("%", "\\%")
                           .Replace("_", "\\_");

                value = $"%{text}%";

                conditions.Add(
                    $"\"{columnName}\" LIKE {parameterName} " +
                    "ESCAPE '\\'");
            }
            else
            {
                conditions.Add(
                    $"\"{columnName}\" {sqlOperator} {parameterName}");
            }

            parameters.Add(
                new SqliteParameter(parameterName, value));
        }

        if (conditions.Count > 0)
        {
            sql.Append(" WHERE ");
            sql.Append(string.Join(" AND ", conditions));
        }

        sql.Append(" LIMIT ");
        sql.Append(query.Limit);
        sql.Append(";");

        return new SqlTranslationResult(
            sql.ToString(), parameters);
    }
}


public sealed record SqlTranslationResult(
    string Sql,
    IReadOnlyList<SqliteParameter> Parameters);
