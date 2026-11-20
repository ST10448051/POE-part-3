
using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models;

public class CreateEnrolmentViewModel
{
    public int EventID { get; set; }

    public string EventName { get; set; } = "";

    [Range(1, int.MaxValue,
        ErrorMessage = "Please select a race category.")]
    [Display(Name = "Race Category")]
    public int EventCategoryID { get; set; }

    public List<EventCategoryViewModel> Categories
    { get; set; } = new();
}
