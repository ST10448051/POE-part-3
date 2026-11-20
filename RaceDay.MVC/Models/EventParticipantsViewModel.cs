
namespace RaceDay.MVC.Models;

public class EventParticipantsViewModel
{
    public int EventID { get; set; }

    public string EventName { get; set; } = "";

    public int TotalEnrolments { get; set; }

    public int Confirmed { get; set; }

    public int Pending { get; set; }

    public int Cancelled { get; set; }

    public int Completed { get; set; }

    public List<EventParticipantViewModel> Participants
    { get; set; } = new();
}
