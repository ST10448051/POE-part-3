
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RaceDay.MVC.Models;

public class AddEventCategoryViewModel
{
    public int EventID { get; set; }

    [Range(1, int.MaxValue,
        ErrorMessage = "Please select a category.")]
    [Display(Name = "Race Category")]
    public int CategoryID { get; set; }

    [Range(0, 99999999.99)]
    [Display(Name = "Entry Fee (R)")]
    public decimal EntryFee { get; set; }

    [Range(1, int.MaxValue,
        ErrorMessage = "The limit must be at least 1.")]
    [Display(Name = "Maximum Participants")]
    public int? MaxParticipants { get; set; }

    public List<SelectListItem> AvailableCategories { get; set; }
        = new();
}
