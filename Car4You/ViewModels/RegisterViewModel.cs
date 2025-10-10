using System.ComponentModel.DataAnnotations;

namespace Car4You.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Login")]
        public string UserName { get; set; } = null!;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Hasło")]
        public string Password { get; set; } = null!;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Hasła muszą być takie same")]
        [Display(Name = "Potwierdź hasło")]
        public string ConfirmPassword { get; set; } = null!;
    }
}

