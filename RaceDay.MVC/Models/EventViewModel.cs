
using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models;

public class EventViewModel
{
    public int EventID { get; set; }

    [Required(ErrorMessage = "Event name is required.")]
    [StringLength(200)]
    [Display(Name = "Event Name")]
    public string EventName { get; set; } = "";

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(1000)]
    public string Description { get; set; } = "";

    [Required(ErrorMessage = "Event date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Event Date")]
    public DateTime EventDate { get; set; }

    [Required(ErrorMessage = "Location is required.")]
    [StringLength(200)]
    public string Location { get; set; } = "";

    [Range(0.01, 9999.99)]
    [Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; }

    [Range(0, 99999999.99)]
    [Display(Name = "Entry Fee (R)")]
    public decimal EntryFee { get; set; }

    public string Status { get; set; } = "Upcoming";

    [Display(Name = "Event Type")]
    public string EventType { get; set; } = "";
}
