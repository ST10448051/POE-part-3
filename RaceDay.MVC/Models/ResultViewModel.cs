
namespace RaceDay.MVC.Models;

public class ResultViewModel
{
    public int ResultID { get; set; }

    public int EnrolmentID { get; set; }

    public string ParticipantName { get; set; } = "";

    public string EventName { get; set; } = "";

    public string CategoryName { get; set; } = "";

    public TimeSpan? FinishTime { get; set; }

    public int? Position { get; set; }

    public string ResultStatus { get; set; } = "";

    public DateTime RecordedAt { get; set; }

    public string DisplayFinishTime =>
        FinishTime.HasValue
            ? FinishTime.Value.ToString(@"hh\:mm\:ss")
            : "—";

    public string DisplayStatus =>
        ResultStatus switch
        {
            "Finished" => "Finished",
            "DidNotFinish" => "DNF",
            "Disqualified" => "Disqualified",
            _ => ResultStatus
        };
}
