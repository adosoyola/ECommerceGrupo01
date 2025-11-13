using Microsoft.AspNetCore.Identity;

namespace ECommerce.Areas.Admin.Models
{
    public class UserWithRolesViewModel
    {
        public IdentityUser User { get; set; }
        public bool IsAdmin { get; set; }
    }
}