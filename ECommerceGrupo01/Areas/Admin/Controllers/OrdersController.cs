using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ECommerce.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq; // Necesario para LINQ (Cast, Select)
using Microsoft.AspNetCore.Identity.UI.Services;
using ECommerce.Services;

namespace ECommerce.Areas.Admin.Controllers
{
    // Usaremos "ADMIN" en mayúsculas. Recuerda que si 'Admin' te funcionó, 
    // debes corregir Program.cs para usar 'Admin' y ser consistente.
    [Area("Admin")]
    [Authorize(Roles = "Admin,ADMIN")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailService _emailService;

        public OrdersController(ApplicationDbContext context, UserManager<IdentityUser> userManager,IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
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
    var order = await _context.Orders
        .Include(o => o.User)   // Necesario para obtener Email
        .FirstOrDefaultAsync(o => o.Id == id);

    if (order == null) return NotFound();

    var oldStatus = order.Status; // Guardamos estado anterior
    order.Status = newStatus;

    try
    {
        _context.Update(order);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"El estado de la Orden #{order.Id} ha sido actualizado a {newStatus}.";

        // 📧 Enviar correo SOLO si el estado cambió
        if (oldStatus != newStatus)
        {
            string subject = $"Actualización de tu pedido #{order.Id}";
            string body = GenerateStatusEmailBody(order, newStatus);

            await _emailService.SendEmailAsync(
                order.User.Email!,
                subject,
                body
            );
        }
    }
    catch (DbUpdateConcurrencyException)
    {
        if (!_context.Orders.Any(e => e.Id == id)) return NotFound();
        else throw;
    }

    return RedirectToAction(nameof(Index));
}

private string GenerateStatusEmailBody(Order order, OrderStatus newStatus)
{
    string username = order.User.UserName;
    string orderId = order.Id.ToString();

    return newStatus switch
    {
        OrderStatus.EnPreparacion => $@"
            <div style='font-family: Arial, sans-serif; padding: 20px; color:#333;'>
                <h2 style='color:#0d6efd;'>🛠️ Tu pedido está en preparación</h2>
                <p>Hola <strong>{username}</strong>,</p>
                <p>Hemos recibido tu pedido <strong>#{orderId}</strong> y ahora se encuentra <strong>en preparación</strong>.</p>

                <div style='background:#eef5ff; padding:12px; border-left:4px solid #0d6efd; margin:15px 0;'>
                    Estado actual: <strong>En Preparación</strong>
                </div>

                <p>Te notificaremos nuevamente cuando tu pedido esté listo para envío.</p>
                <p style='margin-top:25px;'>Gracias por confiar en nosotros 💙</p>
            </div>
        ",

        OrderStatus.EnTransito => $@"
            <div style='font-family: Arial, sans-serif; padding: 20px; color:#333;'>
                <h2 style='color:#198754;'>🚚 ¡Tu pedido está en camino!</h2>
                <p>Hola <strong>{username}</strong>,</p>
                <p>Tu pedido <strong>#{orderId}</strong> ha salido de nuestro almacén y ya está en tránsito hacia tu dirección.</p>

                <div style='background:#e8f6ec; padding:12px; border-left:4px solid #198754; margin:15px 0;'>
                    Estado actual: <strong>En Tránsito</strong>
                </div>

                <p>Muy pronto llegará. Gracias por preferirnos 🙌</p>
            </div>
        ",

        OrderStatus.Entregado => $@"
            <div style='font-family: Arial, sans-serif; padding: 20px; color:#333;'>
                <h2 style='color:#dc3545;'>📦 Tu pedido ha sido entregado</h2>
                <p>Hola <strong>{username}</strong>,</p>
                <p>Tu pedido <strong>#{orderId}</strong> ha sido entregado con éxito ✔️.</p>

                <div style='background:#fdeaea; padding:12px; border-left:4px solid #dc3545; margin:15px 0;'>
                    Estado actual: <strong>Entregado</strong>
                </div>

                <p>Esperamos que disfrutes tu compra ❤️</p>
                <p>Si tienes dudas o necesitas ayuda, estamos aquí para ayudarte.</p>
            </div>
        ",

        _ => $@"
            <div style='font-family: Arial, sans-serif; padding: 20px;'>
                <h2>Actualización de estado</h2>
                <p>Tu pedido #{orderId} cambió su estado a: <strong>{newStatus}</strong>.</p>
            </div>
        "
    };
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