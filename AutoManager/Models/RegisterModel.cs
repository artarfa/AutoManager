using System.ComponentModel.DataAnnotations;

namespace AutoManager.Models;

public class RegisterModel
{
    [Required(ErrorMessage = "Email is required")]
    public string Email { get; set; }
    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; }
}