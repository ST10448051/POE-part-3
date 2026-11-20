
namespace RaceDay.MVC.Models;

public class EventResultsViewModel
{
    public int EventID { get; set; }

    public string EventName { get; set; } = "";

    public List<ResultViewModel> Results { get; set; } = new();

    public int TotalResults => Results.Count;

    public int FinishedCount =>
        Results.Count(r => r.ResultStatus == "Finished");

    public int DidNotFinishCount =>
        Results.Count(r => r.ResultStatus == "DidNotFinish");

    public int DisqualifiedCount =>
        Results.Count(r => r.ResultStatus == "Disqualified");
}
