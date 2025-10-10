using Microsoft.AspNetCore.Identity;

namespace Car4You.Models
{
    public class AppUser : IdentityUser
    {
        public string? FullName { get; set; }
    }
}