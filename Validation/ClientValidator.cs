using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using BeautyManager.Data;
using BeautyManager.Models;

namespace BeautyManager.Validation;

public static class ClientValidator
{
    private const string NamePattern = @"^[A-Za-z\u0400-\u04FF][A-Za-z\u0400-\u04FF '.-]{1,99}$";
    private const string PhonePattern = @"^\+?[0-9]{7,15}$";
    private const string EmailPattern = @"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9\-]+(\.[A-Za-z0-9\-]+)*\.[A-Za-z]{2,}$";

    public static IEnumerable<ValidationResult> Validate(Client client, ValidationContext context)
    {
        var db = context.GetService(typeof(IDatabase)) as IDatabase ?? Database.Instance;

        if (string.IsNullOrWhiteSpace(client.Name))
        {
            yield return new ValidationResult("Името е задължително.", new[] { nameof(client.Name) });
        }
        else
        {
            if (client.Name.Length < 2 || client.Name.Length > 100)
            {
                yield return new ValidationResult("Името трябва да е между 2 и 100 символа.", new[] { nameof(client.Name) });
            }

            if (!Regex.IsMatch(client.Name, NamePattern))
            {
                yield return new ValidationResult("Името може да съдържа само букви, интервал, точка, апостроф и тире.", new[] { nameof(client.Name) });
            }

            var nameParts = client.Name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (nameParts.Length < 2)
            {
                yield return new ValidationResult("Моля, въведете поне две имена (Име и Фамилия).", new[] { nameof(client.Name) });
            }

            if (!char.IsUpper(client.Name[0]))
            {
                yield return new ValidationResult("Името трябва да започва с главна буква.", new[] { nameof(client.Name) });
            }

            if (db.NameExists(client.Name))
            {
                yield return new ValidationResult("Вече съществува клиент с това име.", new[] { nameof(client.Name) });
            }
        }

        if (string.IsNullOrWhiteSpace(client.Phone))
        {
            yield return new ValidationResult("Телефонът е задължителен.", new[] { nameof(client.Phone) });
        }
        else
        {
            if (!Regex.IsMatch(client.Phone, PhonePattern))
            {
                yield return new ValidationResult("Невалиден телефон. Пример: +359888123456 или 0888123456.", new[] { nameof(client.Phone) });
            }

            if (db.PhoneExists(client.Phone))
            {
                yield return new ValidationResult("Вече съществува клиент с този телефон.", new[] { nameof(client.Phone) });
            }
        }

        if (string.IsNullOrWhiteSpace(client.Email))
        {
            yield return new ValidationResult("Е-маилът е задължителен.", new[] { nameof(client.Email) });
        }
        else
        {
            if (client.Email.Length > 100)
            {
                yield return new ValidationResult("Е-маилът може да е най-много 100 символа.", new[] { nameof(client.Email) });
            }

            if (!Regex.IsMatch(client.Email, EmailPattern))
            {
                yield return new ValidationResult("Невалиден е-маил адрес.", new[] { nameof(client.Email) });
            }
        }

        if (!string.IsNullOrWhiteSpace(client.Service))
        {
            if (client.Service.Length > 100)
            {
                yield return new ValidationResult("Услугата може да е най-много 100 символа.", new[] { nameof(client.Service) });
            }

            if (Regex.IsMatch(client.Service, @"[<>{}[\]\\#\$%]"))
            {
                yield return new ValidationResult("Полето за услуга съдържа неразрешени специални символи.", new[] { nameof(client.Service) });
            }
        }

        if (!string.IsNullOrWhiteSpace(client.Specialist))
        {
            if (client.Specialist.Length > 60)
            {
                yield return new ValidationResult("Специалистът може да е най-много 60 символа.", new[] { nameof(client.Specialist) });
            }

            if (Regex.IsMatch(client.Specialist, @"[<>{}[\]\\#\$%]"))
            {
                yield return new ValidationResult("Полето за специалист съдържа неразрешени специални символи.", new[] { nameof(client.Specialist) });
            }
        }
    }
}