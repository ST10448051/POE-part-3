
namespace RaceDay.MVC.Models;

public class EventMediaViewModel
{
    public int EventMediaID { get; set; }

    public int EventID { get; set; }

    public string FileName { get; set; } = "";

    public string ContentType { get; set; } = "";

    public long FileSize { get; set; }

    public DateTime UploadedAt { get; set; }

    public string ImageUrl { get; set; } = "";
}
