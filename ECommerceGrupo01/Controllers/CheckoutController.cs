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
using Microsoft.AspNetCore.Http; // Necesario para Session

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

        // 1. CONFIRMAR (GET) - Muestra el formulario
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
            decimal shippingCost = shippingSetting != null && decimal.TryParse(shippingSetting.Value, out decimal val) ? val : 6.00m;
            
            // Pasamos datos a la Vista
            ViewBag.ShippingCost = shippingCost;
            ViewBag.CartItems = cart; // IMPORTANTE: Para que el resumen de productos funcione

            // Modelo inicial para el formulario
            var model = new DeliveryInfoViewModel
            {
                EstimatedDeliveryDate = DateTime.Now.AddDays(2).ToString("dd/MM/yyyy"),
                DeliveryMethod = "Home", // Por defecto
                City = "Cusco"
            };

            return View(model);
        }

        // 2. PROCESAR DATOS DE ENVÍO (POST) - Recibe el formulario
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Payment(DeliveryInfoViewModel info)
        {
            var cart = GetCart(); // Recuperamos carrito para validar

            if (ModelState.IsValid)
            {
                // A. GUARDAR DATOS EN SESIÓN
                // Guardamos esto temporalmente para usarlo en el paso final (PaymentSimulation)
                HttpContext.Session.SetString("DeliveryMethod", info.DeliveryMethod);

                if (info.DeliveryMethod == "Home")
                {
                    HttpContext.Session.SetString("ShipName", info.FullName ?? "");
                    HttpContext.Session.SetString("ShipAddress", info.Address ?? "");
                    HttpContext.Session.SetString("ShipPhone", info.PhoneNumber ?? "");
                    HttpContext.Session.SetString("ShipNotes", info.SpecialInstructions ?? "");
                }
                else
                {
                    // Limpiamos datos si es tienda
                    HttpContext.Session.SetString("ShipName", "Cliente en Tienda");
                    HttpContext.Session.SetString("ShipAddress", "RECOJO EN TIENDA");
                    HttpContext.Session.SetString("ShipPhone", info.PhoneNumber ?? ""); // Aún guardamos el teléfono
                }

                // Redirigir a la simulación de pago
                return RedirectToAction("PaymentSimulation");
            }

            // B. SI HAY ERRORES (ej: faltó dirección)
            // Recargamos los datos necesarios para que la vista no falle
            var shippingSetting = await _db.AppSettings.FirstOrDefaultAsync(x => x.Key == "ShippingCost");
            decimal shippingCost = shippingSetting != null && decimal.TryParse(shippingSetting.Value, out decimal val) ? val : 6.00m;

            ViewBag.ShippingCost = shippingCost;
            ViewBag.CartItems = cart; 

            return View("Confirm", info);
        }

        // 3. SIMULACIÓN DE PAGO (GET)
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> PaymentSimulation()
        {
            var cartItems = GetCartItems();
            if (!cartItems.Any()) return RedirectToAction("Index", "Cart");

            // Recuperar elección de SESIÓN (Más seguro que TempData)
            string methodString = HttpContext.Session.GetString("DeliveryMethod") ?? "Home";
            
            // Calcular Costos
            decimal subtotal = cartItems.Sum(x => x.UnitPrice * x.Quantity);
            decimal shippingCost = 0;

            var shippingSetting = await _db.AppSettings.FirstOrDefaultAsync(x => x.Key == "ShippingCost");
            decimal dbShippingPrice = shippingSetting != null && decimal.TryParse(shippingSetting.Value, out decimal val) ? val : 6.00m;

            // Solo cobramos si es envío a domicilio
            if (methodString == "Home")
            {
                shippingCost = dbShippingPrice;
            }

            ViewBag.Subtotal = subtotal;
            ViewBag.ShippingCost = shippingCost;
            ViewBag.Total = subtotal + shippingCost;
            ViewBag.DeliveryMethod = methodString;

            // Pasamos datos de envío para mostrarlos en el resumen (Opcional)
            ViewBag.ShipAddress = HttpContext.Session.GetString("ShipAddress");

            return View(cartItems);
        }

        // 4. PROCESAR PAGO Y CREAR ORDEN (POST)
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPaymentSimulation(PaymentViewModel model)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (userId == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

                var user = await _userManager.FindByIdAsync(userId);
                var cart = GetCart();

                if (cart == null || !cart.Any())
                {
                    TempData["Error"] = "El carrito está vacío.";
                    return RedirectToAction("Index", "Cart");
                }

                // A. RECUPERAR DATOS DE SESIÓN
                string methodString = HttpContext.Session.GetString("DeliveryMethod") ?? "Home";
                var method = methodString == "Store" ? DeliveryMethod.StorePickup : DeliveryMethod.HomeDelivery;
                
                // Datos extras del formulario
                string shipAddress = HttpContext.Session.GetString("ShipAddress") ?? "Dirección no especificada";
                string shipPhone = HttpContext.Session.GetString("ShipPhone") ?? "";
                string shipNotes = HttpContext.Session.GetString("ShipNotes") ?? "";
                string shipName = HttpContext.Session.GetString("ShipName") ?? user.UserName;

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
                    
                    // Recargar ViewBag para la vista de error
                    ViewBag.Subtotal = subtotal;
                    ViewBag.ShippingCost = shippingCost;
                    ViewBag.Total = subtotal + shippingCost;
                    return View("PaymentSimulation", cart);
                }

                // C. SIMULAR PAGO
                decimal saldoDisponibleSimulado = 10000.00m;
                var paymentResult = _paymentProcessor.ProcessPayment(model.Amount, saldoDisponibleSimulado);

                if (!paymentResult.IsSuccess)
                {
                    TempData["Error"] = $"Error de pago: {paymentResult.Message}";
                    ViewBag.Subtotal = subtotal;
                    ViewBag.ShippingCost = shippingCost;
                    ViewBag.Total = subtotal + shippingCost;
                    return View("PaymentSimulation", cart);
                }

                // D. CREAR ORDEN EN BASE DE DATOS
                var order = new Order
                {
                    UserId = userId,
                    Total = subtotal + shippingCost,
                    ShippingCost = shippingCost,
                    Status = OrderStatus.EnPreparacion,
                    DeliveryMethod = method,
                    CreatedAt = DateTime.Now,
                    // NOTA: Si quisieras guardar dirección en BD, aquí asignarías: order.Address = shipAddress;
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

                // F. LIMPIAR CARRITO Y SESIÓN
                SaveCart(new List<CartItem>());
                HttpContext.Session.Remove("DeliveryMethod"); // Limpieza

                // G. ENVIAR CORREO CON DETALLES
                try 
                {
                    var email = user.Email;
                    string subject = $"Confirmación de Pedido #{order.Id} - SystemCusco";
                    
                    // Construcción del HTML del correo con la info nueva
                    string detallesEnvioHtml = "";
                    
                    if (method == DeliveryMethod.HomeDelivery)
                    {
                        detallesEnvioHtml = $@"
                            <div style='background-color:#e7f1ff; padding:15px; border-radius:5px; margin-bottom:15px;'>
                                <h3 style='color:#0d6efd; margin-top:0;'>🚚 Envío a Domicilio</h3>
                                <p><strong>Destinatario:</strong> {shipName}</p>
                                <p><strong>Dirección:</strong> {shipAddress}</p>
                                <p><strong>Teléfono:</strong> {shipPhone}</p>
                                <p><strong>Notas:</strong> {shipNotes}</p>
                            </div>";
                    }
                    else
                    {
                        detallesEnvioHtml = $@"
                            <div style='background-color:#e8f5e9; padding:15px; border-radius:5px; margin-bottom:15px;'>
                                <h3 style='color:#198754; margin-top:0;'>🏪 Recojo en Tienda</h3>
                                <p><strong>Titular:</strong> {user.UserName}</p>
                                <p><strong>Lugar:</strong> Av. La Cultura 123, Cusco</p>
                                <p><strong>Horario:</strong> Lunes a Viernes 9am - 6pm</p>
                            </div>";
                    }

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
                                <p>Hola,</p>
                                <p>Tu pedido ha sido confirmado.</p>
                                
                                {detallesEnvioHtml}

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
                // Recargar datos para la vista en caso de crash
                ViewBag.Subtotal = 0; // Valor seguro
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