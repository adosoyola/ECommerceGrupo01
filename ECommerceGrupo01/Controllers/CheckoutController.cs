using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ECommerce.Data;
using ECommerce.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ECommerce.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace ECommerce.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly PaymentProcessor _paymentProcessor;
        private readonly IEmailService _emailService;
        private const string SessionKey = "CartSession";

        public CheckoutController(ApplicationDbContext db, UserManager<IdentityUser> userManager, PaymentProcessor paymentProcessor, IEmailService emailService)
        {
            _db = db;
            _userManager = userManager;
            _paymentProcessor = paymentProcessor;
            _emailService = emailService;
        }

        // --- MÉTODOS PRIVADOS ---
        private List<CartItem> GetCart()
        {
            var json = HttpContext.Session.GetString(SessionKey);
            return string.IsNullOrEmpty(json)
                ? new List<CartItem>()
                : JsonConvert.DeserializeObject<List<CartItem>>(json) ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString(SessionKey, JsonConvert.SerializeObject(cart));
        }

        private List<CartItem> GetCartItems()
        {
            return GetCart();
        }

        // --- VISTAS DE PROCESO DE COMPRA ---

        // 1. CONFIRMAR (GET)
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Confirm()
        {
            var cart = GetCart();
            if (cart == null || !cart.Any())
            {
                TempData["Error"] = "El carrito está vacío.";
                return RedirectToAction("Index", "Cart");
            }

            // Obtener costo de envío de la BD
            var shippingSetting = await _db.AppSettings.FirstOrDefaultAsync(x => x.Key == "ShippingCost");
            
            // Si existe lo usamos, si no, usamos 6.00 por defecto
            decimal shippingCost = shippingSetting != null && decimal.TryParse(shippingSetting.Value, out decimal val) ? val : 6.00m;
            
            ViewBag.ShippingCost = shippingCost;

            return View(cart);
        }

        // 2. PROCESAR CONFIRMACIÓN (POST) -> ¡AQUÍ ESTABA EL ERROR 404!
        // Este es el método que faltaba o estaba mal escrito
        [Authorize]
        [HttpPost]
        public IActionResult ProcessConfirm(string deliveryMethod)
        {
            // Guardamos la elección en TempData
            TempData["DeliveryMethod"] = deliveryMethod;
            TempData.Keep("DeliveryMethod"); // Mantener para la siguiente petición
            
            // Redirigimos a la simulación de pago
            return RedirectToAction("PaymentSimulation");
        }

        // 3. SIMULACIÓN DE PAGO (GET)
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> PaymentSimulation()
        {
            var cartItems = GetCartItems();
            if (!cartItems.Any()) return RedirectToAction("Index", "Cart");

            // Recuperar elección
            string methodString = TempData["DeliveryMethod"]?.ToString() ?? "HomeDelivery";
            TempData.Keep("DeliveryMethod"); 

            // Calcular Costos
            decimal subtotal = cartItems.Sum(x => x.UnitPrice * x.Quantity);
            decimal shippingCost = 0;

            // Obtener precio actualizado de la BD
            var shippingSetting = await _db.AppSettings.FirstOrDefaultAsync(x => x.Key == "ShippingCost");
            decimal dbShippingPrice = shippingSetting != null && decimal.TryParse(shippingSetting.Value, out decimal val) ? val : 6.00m;

            // Solo cobramos si es envío a domicilio
            if (methodString == "HomeDelivery")
            {
                shippingCost = dbShippingPrice;
            }

            ViewBag.Subtotal = subtotal;
            ViewBag.ShippingCost = shippingCost;
            ViewBag.Total = subtotal + shippingCost;
            ViewBag.DeliveryMethod = methodString;

            return View(cartItems);
        }

        // 4. PROCESAR PAGO (POST)
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPaymentSimulation(PaymentViewModel model)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (userId == null)
                {
                    TempData["Error"] = "Usuario no encontrado.";
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                var cart = GetCart();

                if (cart == null || !cart.Any())
                {
                    TempData["Error"] = "El carrito está vacío.";
                    return RedirectToAction("Index", "Cart");
                }

                // A. RECUPERAR MÉTODO DE ENTREGA Y COSTO
                string methodString = TempData["DeliveryMethod"]?.ToString() ?? "HomeDelivery";
                var method = methodString == "StorePickup" ? DeliveryMethod.StorePickup : DeliveryMethod.HomeDelivery;

                decimal subtotal = cart.Sum(x => x.UnitPrice * x.Quantity);
                decimal shippingCost = 0;

                if (method == DeliveryMethod.HomeDelivery)
                {
                    var shippingSetting = await _db.AppSettings.FirstOrDefaultAsync(x => x.Key == "ShippingCost");
                    shippingCost = shippingSetting != null && decimal.TryParse(shippingSetting.Value, out decimal val) ? val : 6.00m;
                }

                // B. VALIDAR STOCK
                var errors = new List<string>();
                foreach (var item in cart)
                {
                    var product = await _db.Products.FindAsync(item.ProductId);
                    if (product == null)
                        errors.Add($"El producto '{item.Name}' ya no está disponible.");
                    else if (product.Stock < item.Quantity)
                        errors.Add($"Stock insuficiente para '{item.Name}'. Disponible: {product.Stock}.");
                }

                if (errors.Any())
                {
                    TempData["Error"] = string.Join("\n", errors);
                    return View("PaymentSimulation", cart);
                }

                // C. SIMULAR PAGO
                decimal saldoDisponibleSimulado = 10000.00m;
                var paymentResult = _paymentProcessor.ProcessPayment(model.Amount, saldoDisponibleSimulado);

                if (!paymentResult.IsSuccess)
                {
                    TempData["Error"] = $"Error de pago: {paymentResult.Message}";
                    // Importante: Volver a cargar ViewBag para que no explote la vista al volver
                    ViewBag.Subtotal = subtotal;
                    ViewBag.ShippingCost = shippingCost;
                    ViewBag.Total = subtotal + shippingCost;
                    ViewBag.DeliveryMethod = methodString;
                    return View("PaymentSimulation", cart);
                }

                // D. CREAR ORDEN
                var order = new Order
                {
                    UserId = userId,
                    Total = subtotal + shippingCost, // Total final
                    ShippingCost = shippingCost,     // Guardar costo envío
                    Status = OrderStatus.EnPreparacion,
                    DeliveryMethod = method,         // Guardar método
                    CreatedAt = DateTime.Now,
                    Items = cart.Select(ci => new OrderItem
                    {
                        ProductId = ci.ProductId,
                        ProductName = ci.Name,
                        Quantity = ci.Quantity,
                        UnitPrice = ci.UnitPrice
                    }).ToList()
                };

                // E. DESCONTAR STOCK
                foreach (var item in cart)
                {
                    var product = await _db.Products.FindAsync(item.ProductId);
                    if (product != null) product.Stock -= item.Quantity;
                }

                _db.Orders.Add(order);
                await _db.SaveChangesAsync();

                // F. LIMPIAR CARRITO
                SaveCart(new List<CartItem>());

                // G. ENVIAR CORREO (Intento seguro)
                try 
                {
                    var email = user.Email;
                    string subject = $"Confirmación de Pedido #{order.Id} - SystemCusco";
                    
                    string infoEntrega = method == DeliveryMethod.HomeDelivery 
                        ? $"<p style='color:#0d6efd;'><strong>🚚 Envío a Domicilio</strong> (Llega en 2 días)</p>"
                        : $"<p style='color:#ffc107; color:#B38F00;'><strong>🏪 Recojo en Tienda</strong> (Av. La Cultura 123)</p>";

                    string productRows = string.Join("", order.Items.Select(i => $@"
                        <tr>
                            <td style='padding:8px; border-bottom:1px solid #ddd;'>{i.ProductName}</td>
                            <td style='padding:8px; border-bottom:1px solid #ddd; text-align:center;'>{i.Quantity}</td>
                            <td style='padding:8px; border-bottom:1px solid #ddd; text-align:right;'>S/ {(i.UnitPrice * i.Quantity):0.00}</td>
                        </tr>"));

                    string body = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #ddd; border-radius: 8px;'>
                            <div style='background-color: #4CAF50; color: white; padding: 20px; text-align: center;'>
                                <h2>¡Gracias por tu compra!</h2>
                            </div>
                            <div style='padding: 20px;'>
                                <p>Hola <strong>{user.UserName}</strong>,</p>
                                <p>Hemos recibido tu pedido correctamente.</p>
                                {infoEntrega}
                                <table style='width:100%; border-collapse:collapse; margin-top:15px;'>
                                    <thead>
                                        <tr style='background-color:#f9f9f9;'>
                                            <th style='text-align:left; padding:8px;'>Producto</th>
                                            <th style='text-align:center; padding:8px;'>Cant.</th>
                                            <th style='text-align:right; padding:8px;'>Subtotal</th>
                                        </tr>
                                    </thead>
                                    <tbody>{productRows}</tbody>
                                    <tfoot>
                                        <tr>
                                            <td colspan='2' style='text-align:right; padding:10px;'>Envío:</td>
                                            <td style='text-align:right; padding:10px;'>S/ {order.ShippingCost:0.00}</td>
                                        </tr>
                                        <tr>
                                            <td colspan='2' style='text-align:right; font-weight:bold; padding:10px;'>TOTAL:</td>
                                            <td style='text-align:right; font-weight:bold; color:#4CAF50; font-size:18px;'>S/ {order.Total:0.00}</td>
                                        </tr>
                                    </tfoot>
                                </table>
                            </div>
                        </div>";

                    await _emailService.SendEmailAsync(email, subject, body);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error enviando correo: {ex.Message}");
                }

                TempData["SuccessMessage"] = $"¡Pedido #{order.Id} realizado con éxito!";
                return RedirectToAction("Success");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ocurrió un error inesperado: {ex.Message}";
                return View("PaymentSimulation", GetCart());
            }
        }

        // --- VISTAS POST-COMPRA ---

        [Authorize]
        [HttpGet]
        public IActionResult Success()
        {
            return View();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> History()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

            var orders = await _db.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

            var order = await _db.Orders
                .Where(o => o.Id == id && o.UserId == userId)
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync();

            if (order == null)
            {
                TempData["Error"] = "Pedido no encontrado.";
                return RedirectToAction("History");
            }

            return View(order);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ExportToPdf(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

            var order = await _db.Orders
                .Where(o => o.Id == id && o.UserId == userId)
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync();

            if (order == null)
            {
                TempData["Error"] = "Pedido no encontrado.";
                return RedirectToAction("History");
            }

            return View("InvoicePdf", order);
        }
    }
}