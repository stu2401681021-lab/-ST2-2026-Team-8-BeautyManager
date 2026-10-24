using System.ComponentModel.DataAnnotations;
namespace BeautyManager.Models;
public class AiQuestion
{
    [Required(ErrorMessage = "Въведете въпрос.")]
    [StringLength(800)]
    public string Question { get; set; } = "";
    public string? Answer { get; set; }
    public string? Error { get; set; }
}
