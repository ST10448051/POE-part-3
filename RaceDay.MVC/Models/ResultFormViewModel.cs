
using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models;

public class ResultFormViewModel
{
    public int ResultID { get; set; }

    public int EventID { get; set; }

    [Range(1, int.MaxValue,
        ErrorMessage = "Please select a participant.")]
    public int EnrolmentID { get; set; }

    [Display(Name = "Finish Time")]
    public TimeSpan? FinishTime { get; set; }

    [Range(1, int.MaxValue,
        ErrorMessage = "Position must be greater than zero.")]
    public int? Position { get; set; }

    [Required]
    [RegularExpression(
        "^(Finished|DidNotFinish|Disqualified)$",
        ErrorMessage = "Please select a valid result status.")]
    [Display(Name = "Result Status")]
    public string ResultStatus { get; set; } = "Finished";
}
