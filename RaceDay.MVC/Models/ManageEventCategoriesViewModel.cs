
namespace RaceDay.MVC.Models;

public class ManageEventCategoriesViewModel
{
    public int EventID { get; set; }

    public string EventName { get; set; } = "";

    public List<EventCategoryViewModel> AssignedCategories
    { get; set; } = new();

    public AddEventCategoryViewModel NewCategory
    { get; set; } = new();
}
