using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ECommerce.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ECommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // Asegúrate de que el rol sea "ADMIN"
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
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product) // Detalle del producto por cada ítem
            .Include(o => o.User)
            .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // GET: Admin/Orders/UpdateStatus/5
        // Formulario para actualizar el estado
        public async Task<IActionResult> UpdateStatus(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            // Pasa todos los valores del enum para el select
            var statuses = Enum.GetValues(typeof(OrderStatus))
                                   .Cast<OrderStatus>()
                                   .Select(e => new SelectListItem
                                   {
                                       Value = e.ToString(),
                                       Text = e.ToString() // Opcional: Si usaste [Display], podrías usar un método de extensión aquí
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

    }
}