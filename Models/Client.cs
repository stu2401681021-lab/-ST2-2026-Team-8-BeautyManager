
using System.ComponentModel.DataAnnotations;



namespace BeautyManager.Models;

public class Client
{

    public const string NamePattern = "^[A-Za-z\u0400-\u04FF][A-Za-z\u0400-\u04FF '.-]{1,99}$";


    public const string PhonePattern = @"^\+?[0-9]{7,15}$";

    public const string EmailPattern = @"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9\-]+(\.[A-Za-z0-9\-]+)*\.[A-Za-z]{2,}$";

    public int Id { get; set; }

    [Display(Name = "Име")]
    [Required(ErrorMessage = "Името е задължително.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Името трябва да е между 2 и 100 символа.")]
    [RegularExpression(NamePattern, ErrorMessage = "Името може да съдържа само букви, интервал, точка, апостроф и тире.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Телефон")]
    [Required(ErrorMessage = "Телефонът е задължителен.")]
    [RegularExpression(PhonePattern, ErrorMessage = "Невалиден телефон. Пример: +359888123456 или 0888123456.")]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "Е-маил")]
    [Required(ErrorMessage = "Е-маилът е задължителен.")]
    [StringLength(100, ErrorMessage = "Е-маилът може да е най-много 100 символа.")]
    [RegularExpression(EmailPattern, ErrorMessage = "Невалиден е-маил. Пример: ivan@example.com.")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Услуга")]
    [StringLength(100, ErrorMessage = "Услугата може да е най-много 100 символа.")]
    public string? Service { get; set; }

    [Display(Name = "Специалист")]
    [StringLength(60, ErrorMessage = "Специалистът може да е най-много 60 символа.")]
    public string? Specialist { get; set; }
}
