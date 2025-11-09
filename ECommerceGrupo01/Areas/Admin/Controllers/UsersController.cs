using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Areas.Admin.Models; // 🔑 AÑADIDO: Para los ViewModels

namespace ECommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // 🔑 Ojo: si tu rol es "ADMIN", usa "ADMIN"
    public class UsersController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Admin/Users
        // 🔑 ACTUALIZADO para usar el ViewModel
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();

            var userViewModels = new List<UserWithRolesViewModel>();

            // Usamos "ADMIN" (mayúsculas) como está en EditRoles.cshtml
            const string adminRole = "ADMIN";

            foreach (var user in users)
            {
                userViewModels.Add(new UserWithRolesViewModel
                {
                    User = user,
                    // Comprueba si el usuario tiene el rol "ADMIN"
                    IsAdmin = await _userManager.IsInRoleAsync(user, adminRole)
                });
            }

            // Pasamos la nueva lista de ViewModels a la vista
            return View(userViewModels);
        }

        // GET: Admin/Users/EditRoles/userId
        public async Task<IActionResult> EditRoles(string id)
        {
            if (id == null) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            // 🔑 Usamos "ADMIN" (mayúsculas)
            ViewBag.IsAdmin = roles.Contains("ADMIN");
            return View(user);
        }

        // POST: Admin/Users/EditRoles
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoles(string id, bool isAdmin)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Usamos "ADMIN" (mayúsculas) como lo tienes
            const string adminRole = "ADMIN";
            var roles = await _userManager.GetRolesAsync(user);
            bool userIsCurrentlyAdmin = roles.Contains(adminRole);

            // Caso 1: Se marca como Admin (isAdmin=true) Y NO lo es (!userIsCurrentlyAdmin)
            if (isAdmin && !userIsCurrentlyAdmin)
            {
                var result = await _userManager.AddToRoleAsync(user, adminRole);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = $"Rol ADMIN asignado a {user.Email}.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al asignar el rol.";
                }
            }
            // Caso 2: Se desmarca como Admin (isAdmin=false) Y SÍ lo es (userIsCurrentlyAdmin)
            else if (!isAdmin && userIsCurrentlyAdmin)
            {
                // 🔑 ¡PROTECCIÓN IMPORTANTE!
                // Evita que el admin actual se quite el rol a sí mismo.
                var currentUserId = _userManager.GetUserId(User);
                if (user.Id == currentUserId)
                {
                    TempData["ErrorMessage"] = "Error: No puedes quitarte el rol de Admin a ti mismo.";
                    return RedirectToAction(nameof(Index));
                }

                var result = await _userManager.RemoveFromRoleAsync(user, adminRole);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = $"Rol ADMIN removido de {user.Email}.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al remover el rol.";
                }
            }
            // Caso 3: No hay cambios (se marca y ya era admin, o se desmarca y no era admin)
            // Simplemente redirigimos sin mensaje.

            return RedirectToAction(nameof(Index));
        }

        // --- Acciones CRUD ---

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
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 🔑 INICIO: Lógica para UserName antes del @
            // 1. Extraer el nombre de usuario
            var userName = model.Email.Split('@').FirstOrDefault() ?? model.Email;

            // 2. ⚠️ ¡Validación de Seguridad!
            // Comprobar si ese UserName (ej: "juan") ya existe
            var existingUser = await _userManager.FindByNameAsync(userName);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", $"El nombre de usuario '{userName}' (derivado del email) ya existe. Pruebe con otro email.");
                return View(model);
            }
            // 🔑 FIN: Lógica para UserName

            // 3. Crear el usuario
            var user = new IdentityUser
            {
                UserName = userName, // ⬅️ Usamos el nuevo UserName
                Email = model.Email,
                EmailConfirmed = true // Confirmado porque lo crea el Admin
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Usuario {user.Email} (UserName: {user.UserName}) creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }

            // Si falla, mostrar errores
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        // GET: Admin/Users/Delete/userId
        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId)
            {
                TempData["ErrorMessage"] = "Error: No puedes eliminar tu propia cuenta de administrador.";
                return RedirectToAction(nameof(Index));
            }

            return View(user);
        }

        // POST: Admin/Users/Delete/userId
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Usuario no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId)
            {
                TempData["ErrorMessage"] = "Error: No puedes eliminar tu propia cuenta.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Usuario {user.Email} ha sido eliminado.";
            }
            else
            {
                TempData["ErrorMessage"] = $"Error al eliminar {user.Email}.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}