using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Areas.Admin.Models;

namespace ECommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,ADMIN")] // Aceptamos ambas variantes por seguridad
    public class UsersController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        // 🔒 CONFIGURACIÓN BLINDADA SEGÚN TU PROGRAM.CS
        private const string SuperAdminEmail = "admin@ecommerce.com";
        private const string AdminRoleName = "ADMIN";

        public UsersController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Admin/Users
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userViewModels = new List<UserWithRolesViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                // Comprobamos si tiene el rol (ignorando mayúsculas/minúsculas)
                var isUserAdmin = roles.Any(r => r.Equals(AdminRoleName, StringComparison.OrdinalIgnoreCase));

                userViewModels.Add(new UserWithRolesViewModel
                {
                    User = user,
                    IsAdmin = isUserAdmin
                });
            }

            return View(userViewModels);
        }

        // GET: Admin/Users/EditRoles/userId
        public async Task<IActionResult> EditRoles(string id)
        {
            if (id == null) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            // Chequeo flexible para que el switch aparezca activado correctamente
            ViewBag.IsAdmin = roles.Any(r => r.Equals(AdminRoleName, StringComparison.OrdinalIgnoreCase));

            return View(user);
        }

        // POST: Admin/Users/EditRoles
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoles(string id, bool isAdmin)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // 🛡️ PROTECCIÓN TOTAL: Nadie toca al Super Admin
            if (user.Email.Equals(SuperAdminEmail, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "🚫 ACCESO DENEGADO: No se pueden modificar los permisos de la cuenta Principal (admin@ecommerce.com).";
                return RedirectToAction(nameof(Index));
            }

            // 1. Verificar estado actual
            var roles = await _userManager.GetRolesAsync(user);
            bool currentlyHasRole = roles.Any(r => r.Equals(AdminRoleName, StringComparison.OrdinalIgnoreCase));

            // Nombre exacto del rol a usar (si el usuario ya lo tiene con otro casing, usamos ese)
            string roleToUse = roles.FirstOrDefault(r => r.Equals(AdminRoleName, StringComparison.OrdinalIgnoreCase)) ?? AdminRoleName;

            IdentityResult result;

            // CASO 1: Asignar Admin
            if (isAdmin && !currentlyHasRole)
            {
                // Crear el rol si no existe (por si acaso)
                if (!await _roleManager.RoleExistsAsync(AdminRoleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(AdminRoleName));
                }

                result = await _userManager.AddToRoleAsync(user, AdminRoleName);
                if (result.Succeeded)
                    TempData["SuccessMessage"] = $"Rol ADMIN asignado a {user.Email}.";
                else
                    TempData["ErrorMessage"] = "Error al asignar rol.";
            }
            // CASO 2: Quitar Admin
            else if (!isAdmin && currentlyHasRole)
            {
                // Protección extra: No quitarse el rol a uno mismo (si no es el super admin)
                var currentUserId = _userManager.GetUserId(User);
                if (user.Id == currentUserId)
                {
                    TempData["ErrorMessage"] = "⚠️ No puedes quitarte el rol de Admin a ti mismo.";
                    return RedirectToAction(nameof(Index));
                }

                result = await _userManager.RemoveFromRoleAsync(user, roleToUse);
                if (result.Succeeded)
                    TempData["SuccessMessage"] = $"Rol ADMIN quitado a {user.Email}.";
                else
                    TempData["ErrorMessage"] = "Error al quitar rol.";
            }

            return RedirectToAction(nameof(Index));
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
            if (!ModelState.IsValid) return View(model);

            // 🔑 Lógica de Nombre de Usuario: Usar la parte antes del @
            // Ejemplo: juan@gmail.com -> UserName = "juan"
            var userName = model.Email.Split('@').FirstOrDefault();

            // Validación: Evitar duplicados de UserName
            if (await _userManager.FindByNameAsync(userName) != null)
            {
                ModelState.AddModelError("Email", $"El nombre de usuario '{userName}' ya está en uso. Por favor use otro correo.");
                return View(model);
            }

            var user = new IdentityUser
            {
                UserName = userName,
                Email = model.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Usuario creado con éxito: {user.UserName} ({user.Email})";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // GET: Admin/Users/Delete/userId
        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // 🛡️ Protección Visual
            if (user.Email.Equals(SuperAdminEmail, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "🚫 No puedes eliminar la cuenta Principal.";
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
            if (user == null) return RedirectToAction(nameof(Index));

            // 🛡️ PROTECCIÓN AL BORRAR
            if (user.Email.Equals(SuperAdminEmail, StringComparison.OrdinalIgnoreCase))
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
    }
}