using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BeautyManager.Validation;

namespace BeautyManager.Models;

public class Client : IValidatableObject
{
    public int Id { get; set; }

    [Display(Name = "Име")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Телефон")]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "Е-маил")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Услуга")]
    public string? Service { get; set; }

    [Display(Name = "Специалист")]
    public string? Specialist { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        return ClientValidator.Validate(this, validationContext);
    }
}