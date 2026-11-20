
using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models;

public class EditEventCategoryViewModel
{
    public int EventID { get; set; }

    public int CategoryID { get; set; }

    public string CategoryName { get; set; } = "";

    [Range(0, 99999999.99,
        ErrorMessage = "Entry fee cannot be negative.")]
    [Display(Name = "Entry Fee (R)")]
    public decimal EntryFee { get; set; }

    [Range(1, int.MaxValue,
        ErrorMessage = "Maximum participants must be at least 1.")]
    [Display(Name = "Maximum Participants")]
    public int? MaxParticipants { get; set; }
}
