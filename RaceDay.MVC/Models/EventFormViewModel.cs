
using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models;

public class EventFormViewModel
{
    public int EventID { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Event Name")]
    public string EventName { get; set; } = "";

    [Required]
    [StringLength(1000)]
    public string Description { get; set; } = "";

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Event Date")]
    public DateTime? EventDate { get; set; }

    [Required]
    public string Location { get; set; } = "";

    [Range(0.01, 9999.99)]
    [Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; }

    [Range(0, 99999999.99)]
    [Display(Name = "Entry Fee (R)")]
    public decimal EntryFee { get; set; }

    [Required]
    [Display(Name = "Event Type")]
    public string EventType { get; set; } = "";

    [Required]
    public string Status { get; set; } = "Draft";
}
