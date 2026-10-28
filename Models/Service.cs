using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BeautyManager.Validation;

namespace BeautyManager.Models;

public class Service : IValidatableObject
{
    public int Id { get; set; }

    [Display(Name = "Име на услугата")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Категория")]
    public string Category { get; set; } = string.Empty;

    [Display(Name = "Цена")]
    public decimal Price { get; set; }

    [Display(Name = "Продължителност (минути)")]
    public int DurationMinutes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        return ServiceValidator.Validate(this, validationContext);
    }
}