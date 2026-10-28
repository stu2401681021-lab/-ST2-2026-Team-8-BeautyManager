using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BeautyManager.Validation;

namespace BeautyManager.Models;

public class Employee : IValidatableObject
{
    public int Id { get; set; }

    [Display(Name = "Име")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Специалност")]
    public string Specialty { get; set; } = string.Empty;

    [Display(Name = "Телефон")]
    public string Phone { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        return EmployeeValidator.Validate(this, validationContext);
    }
}