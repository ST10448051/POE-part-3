
namespace RaceDay.MVC.Models;

public class EventParticipantViewModel
{
    public int EnrolmentID { get; set; }

    public int ParticipantID { get; set; }

    public string FirstName { get; set; } = "";

    public string LastName { get; set; } = "";

    public string Email { get; set; } = "";

    public int EventCategoryID { get; set; }

    public int CategoryID { get; set; }

    public string CategoryName { get; set; } = "";

    public DateTime EnrolmentDate { get; set; }

    public string Status { get; set; } = "";

    public string FullName => $"{FirstName} {LastName}";
}
