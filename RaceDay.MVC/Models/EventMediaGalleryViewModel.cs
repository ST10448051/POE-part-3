
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models;

public class EventMediaGalleryViewModel
{
    public int EventID { get; set; }

    public string EventName { get; set; } = "";

    public List<EventMediaViewModel> Images { get; set; } = new();

    [Display(Name = "Select Image")]
    public IFormFile? UploadFile { get; set; }

    public string? ErrorMessage { get; set; }
}
