
namespace RaceDay.MVC.Models;

public class LoginResponse
{
    public string Message { get; set; } = "";
    public int UserID { get; set; }
    public string FirstName { get; set; } = "";
    public string Role { get; set; } = "";
}
