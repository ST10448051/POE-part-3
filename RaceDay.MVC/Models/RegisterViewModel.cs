
using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage =
        "Password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage =
        "Passwords do not match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = "";

    [Required]
    public string Role { get; set; } = "Participant";
}
