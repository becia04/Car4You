using System.ComponentModel.DataAnnotations;

namespace Car4You.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Login")]
        public string UserName { get; set; } = null!;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Hasło")]
        public string Password { get; set; } = null!;

        public bool RememberMe { get; set; }
    }
}
