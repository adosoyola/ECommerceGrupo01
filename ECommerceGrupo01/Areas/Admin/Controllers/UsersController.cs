using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ECommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // Asegúrate de que solo los ADMINs puedan gestionar usuarios
    public class UsersController : Controller
    {
        // UserManager para gestionar usuarios (crear, encontrar, roles)
        private readonly UserManager<IdentityUser> _userManager;

        // RoleManager para gestionar los roles (necesario para las vistas de rol)
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager; // Inyección de dependencia para RoleManager
        }

        // GET: Admin/Users/Index
        // Muestra la lista de todos los usuarios
        public async Task<IActionResult> Index()
        {
            // Obtiene todos los usuarios
            var users = await _userManager.Users.ToListAsync();

            // Nota: Para mostrar los roles en la vista Index, necesitarás un ViewModel,
            // pero para la lista básica, basta con el modelo IdentityUser.
            return View(users);
        }

        // GET: Admin/Users/EditRoles/userId
        // Muestra el formulario para modificar si un usuario es ADMIN o no
        public async Task<IActionResult> EditRoles(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // 1. Obtener los roles actuales del usuario
            var roles = await _userManager.GetRolesAsync(user);

            // 2. Pasar a la vista si el usuario ya tiene el rol 'ADMIN'
            ViewBag.IsAdmin = roles.Contains("ADMIN");

            return View(user); // Muestra la vista EditRoles.cshtml
        }

        // POST: Admin/Users/EditRoles/userId
        // Procesa la solicitud para añadir/eliminar el rol de ADMIN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoles(string id, bool isAdmin)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Definición del rol
            const string adminRole = "ADMIN";
            var roles = await _userManager.GetRolesAsync(user);

            if (isAdmin && !roles.Contains(adminRole))
            {
                // Si se marca como Admin y no lo es: añadir el rol
                var result = await _userManager.AddToRoleAsync(user, adminRole);
                if (!result.Succeeded)
                {
                    TempData["ErrorMessage"] = $"Error al asignar el rol ADMIN a {user.Email}.";
                }
            }
            else if (!isAdmin && roles.Contains(adminRole))
            {
                // Si se desmarca como Admin y lo es: eliminar el rol
                var result = await _userManager.RemoveFromRoleAsync(user, adminRole);
                if (!result.Succeeded)
                {
                    TempData["ErrorMessage"] = $"Error al remover el rol ADMIN a {user.Email}.";
                }
            }

            // Mensaje de éxito
            TempData["SuccessMessage"] = $"Roles de {user.Email} actualizados correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // Puedes añadir más acciones aquí, como LockoutUser, etc.
    }
}