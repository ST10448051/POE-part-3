
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RaceDay.MVC.Models;

public class CreateResultPageViewModel
{
    public int EventID { get; set; }

    public string EventName { get; set; } = "";

    public ResultFormViewModel Result { get; set; } = new();

    public List<SelectListItem> Participants { get; set; }
        = new();
}
