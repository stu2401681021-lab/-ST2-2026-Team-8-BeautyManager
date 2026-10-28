using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BeautyManager.Models;

namespace BeautyManager.Validation;

public static class ServiceValidator
{
    public static IEnumerable<ValidationResult> Validate(Service service, ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(service.Name))
        {
            yield return new ValidationResult("Името на услугата е задължително.", new[] { nameof(service.Name) });
        }
        else if (service.Name.Length < 2 || service.Name.Length > 100)
        {
            yield return new ValidationResult("Името на услугата трябва да е между 2 и 100 символа.", new[] { nameof(service.Name) });
        }

        if (string.IsNullOrWhiteSpace(service.Category))
        {
            yield return new ValidationResult("Категорията е задължителна.", new[] { nameof(service.Category) });
        }
        else if (service.Category.Length < 2 || service.Category.Length > 40)
        {
            yield return new ValidationResult("Категорията трябва да е между 2 и 40 символа.", new[] { nameof(service.Category) });
        }

        if (service.Price < 0 || service.Price > 10000)
        {
            yield return new ValidationResult("Цената трябва да бъде между 0 и 10 000 лв.", new[] { nameof(service.Price) });
        }
        else if (decimal.Round(service.Price, 2) != service.Price)
        {
            yield return new ValidationResult("Цената може да има най-много два знака след запетаята.", new[] { nameof(service.Price) });
        }

        if (service.DurationMinutes < 1 || service.DurationMinutes > 600)
        {
            yield return new ValidationResult("Продължителността трябва да бъде между 1 и 600 минути.", new[] { nameof(service.DurationMinutes) });
        }
        else if (service.DurationMinutes % 5 != 0)
        {
            yield return new ValidationResult("Продължителността трябва да бъде кратна на 5 минути (напр. 15, 20, 30 или 45 мин).", new[] { nameof(service.DurationMinutes) });
        }

    }
}