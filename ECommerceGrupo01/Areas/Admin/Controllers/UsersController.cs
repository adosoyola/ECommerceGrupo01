using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.Areas.Admin.Models; // ✅ Importante: Usa tu modelo
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ECommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,ADMIN")]
    public class UsersController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        private const string SuperAdminEmail = "admin@ecommerce.com";

        public UsersController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // ✅ GET: Admin/Users (MÉTODO CORREGIDO)
        public async Task<IActionResult> Index()
        {
            // 1. Traemos todos los usuarios de la BD
            var users = await _userManager.Users.ToListAsync();

            // 2. Preparamos la lista vacía del modelo que espera la vista
            var model = new List<UserWithRolesViewModel>();

            // 3. Recorremos cada usuario para llenar la lista
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var isAdmin = roles.Any(r => r.ToUpper() == "ADMIN");

                model.Add(new UserWithRolesViewModel
                {
                    User = user,
                    IsAdmin = isAdmin
                });
            }

            // 4. Enviamos la lista convertida (UserWithRolesViewModel)
            return View(model);
        }

        // GET: Admin/Users/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new IdentityUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Usuario creado correctamente.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        // GET: Admin/Users/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (user.Email.Equals(SuperAdminEmail, System.StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "🚫 No puedes eliminar la cuenta Principal.";
                return RedirectToAction(nameof(Index));
            }

            return View(user);
        }

        // POST: Admin/Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return RedirectToAction(nameof(Index));

            if (user.Email.Equals(SuperAdminEmail, System.StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "🚫 ACCESO DENEGADO: La cuenta Principal es intocable.";
                return RedirectToAction(nameof(Index));
            }

            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId)
            {
                TempData["ErrorMessage"] = "⚠️ No puedes eliminar tu propia cuenta mientras estás conectado.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
                TempData["SuccessMessage"] = "Usuario eliminado correctamente.";
            else
                TempData["ErrorMessage"] = "Error al eliminar usuario.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Users/EditRoles/5
        [HttpGet]
        public async Task<IActionResult> EditRoles(string id)
        {
            if (id == null) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.IsAdmin = roles.Contains("ADMIN") || roles.Contains("Admin");

            return View(user);
        }

        // POST: Admin/Users/EditRoles
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoles(string id, bool isAdmin)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Lógica para agregar o quitar rol ADMIN
            if (isAdmin)
            {
                if (!await _userManager.IsInRoleAsync(user, "ADMIN"))
                {
                    await _userManager.AddToRoleAsync(user, "ADMIN");
                }
            }
            else
            {
                // Evitar quitarse admin a sí mismo o al SuperAdmin
                if (user.Email.Equals(SuperAdminEmail, System.StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] = "No puedes quitarle permisos al SuperAdmin.";
                    return RedirectToAction(nameof(Index));
                }

                if (await _userManager.IsInRoleAsync(user, "ADMIN"))
                {
                    await _userManager.RemoveFromRoleAsync(user, "ADMIN");
                }
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    await _userManager.RemoveFromRoleAsync(user, "Admin");
                }
            }

            TempData["SuccessMessage"] = "Roles actualizados correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}