
namespace RaceDay.MVC.Models;

public class EventCategoryViewModel
{
    public int EventCategoryID { get; set; }

    public int EventID { get; set; }

    public int CategoryID { get; set; }

    public string CategoryName { get; set; } = "";

    public string? Description { get; set; }

    public decimal EntryFee { get; set; }

    public int? MaxParticipants { get; set; }
}
