
using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models;

public class CategoryViewModel
{
    public int CategoryID { get; set; }

    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(100)]
    [Display(Name = "Category Name")]
    public string CategoryName { get; set; } = "";

    [Display(Name = "Description")]
    public string? Description { get; set; }
}
