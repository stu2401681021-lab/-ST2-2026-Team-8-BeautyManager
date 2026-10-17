
using System.ComponentModel.DataAnnotations;

namespace BeautyManager.Models;

public class Service
{
    public int Id { get; set; }

    [Display(Name = "Име на услугата")]
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Категория")]
    [Required]
    [StringLength(40)]
    public string Category { get; set; } = string.Empty;

    [Display(Name = "Цена")]
    [Range(typeof(decimal), "0", "10000")]
    public decimal Price { get; set; }

    [Display(Name = "Продължителност (минути)")]
    [Range(1, 600)]
    public int DurationMinutes { get; set; }
}
