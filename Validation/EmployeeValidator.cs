using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using BeautyManager.Models;

namespace BeautyManager.Validation;

public static class EmployeeValidator
{
    private const string NamePattern = @"^[A-Za-z\u0400-\u04FF][A-Za-z\u0400-\u04FF '.-]{1,99}$";
    private const string PhonePattern = @"^\+?[0-9]{7,15}$";

    public static IEnumerable<ValidationResult> Validate(Employee employee, ValidationContext context)
    {
    
        if (string.IsNullOrWhiteSpace(employee.Name))
        {
            yield return new ValidationResult("Името е задължително.", new[] { nameof(employee.Name) });
        }
        else
        {
            if (employee.Name.Length < 2 || employee.Name.Length > 100)
            {
                yield return new ValidationResult("Името трябва да е между 2 и 100 символа.", new[] { nameof(employee.Name) });
            }
            if (!Regex.IsMatch(employee.Name, NamePattern))
            {
                yield return new ValidationResult("Името може да съдържа само букви, интервал, точка, апостроф и тире.", new[] { nameof(employee.Name) });
            }
        }

        if (string.IsNullOrWhiteSpace(employee.Specialty))
        {
            yield return new ValidationResult("Специалността е задължителна.", new[] { nameof(employee.Specialty) });
        }
        else if (employee.Specialty.Length < 2 || employee.Specialty.Length > 100)
        {
            yield return new ValidationResult("Специалността трябва да е между 2 и 100 символа.", new[] { nameof(employee.Specialty) });
        }
        if (string.IsNullOrWhiteSpace(employee.Phone))
        {
            yield return new ValidationResult("Телефонът е задължителен.", new[] { nameof(employee.Phone) });
        }
        else if (!Regex.IsMatch(employee.Phone, PhonePattern))
        {
            yield return new ValidationResult("Невалиден телефон. Пример: +359888123456 или 0888123456.", new[] { nameof(employee.Phone) });
        }
    }
}