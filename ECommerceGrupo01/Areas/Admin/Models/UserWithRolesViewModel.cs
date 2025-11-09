using Microsoft.AspNetCore.Identity;

namespace ECommerce.Areas.Admin.Models
{
    // Esta clase nos permite pasar el usuario Y sus roles a la vista
    public class UserWithRolesViewModel
    {
        public IdentityUser User { get; set; } = new IdentityUser();
        public bool IsAdmin { get; set; }
    }
}