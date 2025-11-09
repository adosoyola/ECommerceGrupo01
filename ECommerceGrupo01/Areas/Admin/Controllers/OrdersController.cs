using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ECommerce.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq; // Necesario para LINQ (Cast, Select)

namespace ECommerce.Areas.Admin.Controllers
{
    // Usaremos "ADMIN" en mayúsculas. Recuerda que si 'Admin' te funcionó, 
    // debes corregir Program.cs para usar 'Admin' y ser consistente.
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public OrdersController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Admin/Orders
        public async Task<IActionResult> Index()
        {
            // Incluye el usuario para obtener el email en la vista
            var orders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        // GET: Admin/Orders/Details/5
        // 🎯 ESTE ES EL MÉTODO QUE SOLUCIONA EL BOTÓN NO OPERATIVO
        public async Task<IActionResult> Details(int? id)
        {

            if (id == null) return NotFound();

            var order = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product) // Detalle del producto por cada ítem (para ver el nombre)
            .Include(o => o.User)       // Incluye el usuario (para ver el email)
            .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null) return NotFound();

            // Pasa todos los valores del enum OrderStatus para el Dropdown en Details.cshtml
            var statuses = Enum.GetValues(typeof(OrderStatus))
                                   .Cast<OrderStatus>()
                                   .Select(e => new SelectListItem
                                   {
                                       Value = e.ToString(),
                                       Text = e.ToString()
                                   }).ToList();

            ViewBag.Statuses = statuses;

            return View(order);
        }

        // POST: Admin/Orders/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus newStatus)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null) return NotFound();

            order.Status = newStatus;

            try
            {
                _context.Update(order);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"El estado de la Orden #{order.Id} ha sido actualizado a {newStatus}.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Orders.Any(e => e.Id == id)) return NotFound();
                else throw;
            }

            return RedirectToAction(nameof(Index));
        }
        // GET: Admin/Orders/ExportToPdf/5
        [HttpGet]
        public async Task<IActionResult> ExportToPdf(int id)
        {
            // Buscamos la orden por ID (sin check de usuario, porque somos Admin)
            var order = await _context.Orders
                .Where(o => o.Id == id)
                .Include(o => o.Items)
                .ThenInclude(i => i.Product) // Incluye Productos (para nombre)
                .Include(o => o.User)       // Incluye Usuario (para email)
                .FirstOrDefaultAsync();

            if (order == null)
            {
                TempData["ErrorMessage"] = "Pedido no encontrado.";
                return RedirectToAction("Index");
            }

            // 🔑 Reutilizamos la vista pública de la factura.
            // Le decimos a Razor que la busque fuera del área "Admin".
            return View("~/Views/Checkout/InvoicePdf.cshtml", order);
        }
    }
}