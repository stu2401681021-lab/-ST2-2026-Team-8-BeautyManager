using System.ComponentModel.DataAnnotations;

namespace BeautyManager.Models;

public class Employee
{
    public int Id { get; set; }
    [Display(Name = "Име")]
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;
    [Display(Name = "Специалност")]
    [Required, StringLength(100)]
    public string Specialty { get; set; } = string.Empty;
    [Display(Name = "Телефон")]
    [Required, Phone]
    public string Phone { get; set; } = string.Empty;
}
