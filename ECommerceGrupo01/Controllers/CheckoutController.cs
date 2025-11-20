using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ECommerce.Data;
using ECommerce.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ECommerce.Services;

public class CheckoutController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly PaymentProcessor _paymentProcessor;
    private const string SessionKey = "CartSession";
    private readonly IEmailService _emailService;

    public CheckoutController(ApplicationDbContext db, UserManager<IdentityUser> userManager, PaymentProcessor paymentProcessor, IEmailService emailService)
    {
        _db = db;
        _userManager = userManager;
        _paymentProcessor = paymentProcessor;
        _emailService = emailService; // <-- guardar en variable
    }

    private List<CartItem> GetCart()
    {
        var json = HttpContext.Session.GetString(SessionKey);
        return string.IsNullOrEmpty(json)
            ? new List<CartItem>()
            // Corrección Warning CS8603: Añadir ?? new List<CartItem>()
            : JsonConvert.DeserializeObject<List<CartItem>>(json) ?? new List<CartItem>();
    }

    private void SaveCart(List<CartItem> cart)
    {
        HttpContext.Session.SetString(SessionKey, JsonConvert.SerializeObject(cart));
    }

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
                return View("PaymentSimulation", GetCart());
            }

            var user = await _userManager.FindByIdAsync(userId);
            var cart = GetCart();

            if (cart == null || !cart.Any())
            {
                TempData["Error"] = "El carrito está vacío.";
                return View("PaymentSimulation", GetCart());
            }

            // =================================================================
            // 🔑 INICIO DE LA CORRECCIÓN
            // =================================================================

            // 1. Simulación de Pago (usando el servicio)

            // Tu PaymentProcessor espera un "saldo disponible".
            // Vamos a simular un saldo alto (ej: 10,000) para que la compra pase.
            decimal saldoDisponibleSimulado = 10000.00m;

            // Corrección 1: Llamamos al método correcto (ProcessPayment)
            // y le pasamos los argumentos que espera (monto y saldo simulado).
            // También quitamos el 'await' porque el método no es asíncrono.
            var paymentResult = _paymentProcessor.ProcessPayment(model.Amount, saldoDisponibleSimulado);

            // Corrección 2: Usamos 'IsSuccess' (como está en PaymentResult.cs)
            if (!paymentResult.IsSuccess)
            {
                // Corrección 3: Usamos 'Message' (como está en PaymentResult.cs)
                TempData["Error"] = $"Error de pago: {paymentResult.Message}";
                return View("PaymentSimulation", GetCart());
            }

            // =================================================================
            // 🔑 FIN DE LA CORRECCIÓN
            // =================================================================

            // 2. Lógica de Stock (la movimos aquí)
            var errors = new List<string>();
            foreach (var item in cart)
            {
                var product = await _db.Products.FindAsync(item.ProductId);
                if (product == null)
                {
                    errors.Add($"El producto '{item.Name}' ya no está disponible.");
                }
                else if (product.Stock < item.Quantity)
                {
                    errors.Add($"Stock insuficiente para '{item.Name}'. Disponible: {product.Stock}.");
                }
            }

            if (errors.Any())
            {
                TempData["Error"] = string.Join("\n", errors);
                // NOTA: No revertimos el pago porque es una simulación,
                // pero en un caso real, aquí se llamaría a _paymentProcessor.RevertPaymentAsync(paymentResult.TransactionId);
                return View("PaymentSimulation", GetCart());
            }

            // 3. Crear la Orden (si todo está OK)
            var order = new Order
            {
                UserId = userId,
                Total = model.Amount,
                Status = OrderStatus.EnPreparacion, // O usa OrderStatus.Pendiente si lo prefieres
                Items = cart.Select(ci => new OrderItem
                {
                    ProductId = ci.ProductId,
                    ProductName = ci.Name, // Guardar el nombre por si el producto se borra
                    Quantity = ci.Quantity,
                    UnitPrice = ci.UnitPrice
                }).ToList()
            };

            // 4. Actualizar Stock
            foreach (var item in cart)
            {
                var product = await _db.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.Stock -= item.Quantity;
                }
            }

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            // 5. Limpiar Carrito
            SaveCart(new List<CartItem>());

           

            // ============================
            // 📧 ENVÍO DE CORREO AL USUARIO
            // ============================

            var email = user.Email;
            string subject = $"Tu pedido #{order.Id} ha sido realizado con éxito";
            string body = $@"
               <table style='background-color:#f4f4f4; padding:20px; font-family:Arial; width:100%;'>
    <tr>
        <td>
            <table style='max-width:600px; margin:auto; background:white; padding:30px; border-radius:10px;'>
                
                <tr>
                    <td style='text-align:center'>
                        <h2 style='color:#4CAF50;'>¡Gracias por tu compra!</h2>
                        <p style='font-size:16px; color:#555;'>Tu pedido ha sido procesado correctamente.</p>
                    </td>
                </tr>

                <tr>
                    <td>
                        <h3 style='color:#333;'>📦 Detalles del Pedido</h3>
                        <p style='font-size:15px; color:#555;'>
                            <strong>N° de Pedido:</strong> #{order.Id}<br>
                            <strong>Total pagado:</strong> S/ {order.Total}<br>
                            <strong>Estado:</strong> {order.Status}<br>
                            <strong>Fecha:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}
                        </p>
                    </td>
                </tr>

                <tr>
                    <td>
                        <h3 style='color:#333;'>🛒 Productos</h3>
                        <table style='width:100%; border-collapse:collapse;'>
                            <tr>
                                <th style='border-bottom:1px solid #ddd; padding:8px; text-align:left;'>Producto</th>
                                <th style='border-bottom:1px solid #ddd; padding:8px; text-align:center;'>Cantidad</th>
                                <th style='border-bottom:1px solid #ddd; padding:8px; text-align:right;'>Subtotal</th>
                            </tr>
                            {string.Join("", order.Items.Select(i => $@"
                                <tr>
                                    <td style='padding:8px;'>{i.ProductName}</td>
                                    <td style='padding:8px; text-align:center;'>{i.Quantity}</td>
                                    <td style='padding:8px; text-align:right;'>S/ {(i.UnitPrice * i.Quantity):0.00}</td>
                                </tr>
                            "))}
                        </table>
                    </td>
                </tr>

                <tr>
                    <td style='text-align:center; padding-top:25px;'>
                        <p style='color:#555;'>Nos pondremos en contacto cuando tu pedido esté listo para envío.</p>
                        <p style='font-size:14px; color:#888;'>© {DateTime.Now.Year} - ECommerce Group 01</p>
                    </td>
                </tr>

            </table>
        </td>
    </tr>
</table>";

            await _emailService.SendEmailAsync(email, subject, body);


            
            // 6. Redirigir a "Success"
            TempData["SuccessMessage"] = $"¡Pedido #{order.Id} realizado con éxito!";
            // Corrección: El nombre de tu vista de éxito es 'Success.cshtml'
            // El controlador usa 'Success()'
            return RedirectToAction("Success");
        }
        catch (Exception ex)
{
    TempData["Error"] = $"ERROR DETALLADO: {ex.Message}";
    return View("PaymentSimulation", GetCart());
}
    }

    // ACCIÓN NUEVA: Página de éxito
    [Authorize]
    [HttpGet]
    public IActionResult Success()
    {
        return View();
    }

    // ACCIONES EXISTENTES (mantener igual)
    [Authorize]
    [HttpGet]
    public IActionResult Confirm()
    {
        var cart = GetCart();
        if (cart == null || !cart.Any())
        {
            TempData["Error"] = "El carrito está vacío.";
            return RedirectToAction("Index", "Cart");
        }
        return RedirectToAction("PaymentSimulation");
    }

    [Authorize]
    [HttpGet]
    public IActionResult PaymentSimulation()
    {
        var cart = GetCart();
        if (cart == null || !cart.Any())
        {
            TempData["Error"] = "El carrito está vacío.";
            return RedirectToAction("Index", "Cart");
        }
        return View(cart);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> History()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return RedirectToAction("Login", "Account", new { area = "Identity" });
        }

        var orders = await _db.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt) // Ordenar por fecha
            .ToListAsync();

        return View(orders);
    }

    // ========================================================
    // 🔑 MÉTODO NUEVO 1: DETAILS
    // ========================================================
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return RedirectToAction("Login", "Account", new { area = "Identity" });
        }

        // Buscamos la orden, asegurándonos que sea del usuario
        // e incluimos los "Items" y los "Products" de esos items.
        var order = await _db.Orders
            .Where(o => o.Id == id && o.UserId == userId)
            .Include(o => o.Items)
            .ThenInclude(i => i.Product) // Necesario para item.Product.Name
            .FirstOrDefaultAsync();

        if (order == null)
        {
            // No se encontró la orden o no pertenece al usuario
            TempData["Error"] = "Pedido no encontrado.";
            return RedirectToAction("History");
        }

        // Enviamos el pedido a la vista "Details.cshtml"
        return View(order);
    }

    // ========================================================
    // 🔑 MÉTODO NUEVO 2: EXPORTTOPDF
    // ========================================================
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> ExportToPdf(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return RedirectToAction("Login", "Account", new { area = "Identity" });
        }

        // Buscamos la orden, incluyendo Items, Productos y el Usuario (para el email)
        var order = await _db.Orders
            .Where(o => o.Id == id && o.UserId == userId)
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Include(o => o.User) // 🔑 ¡IMPORTANTE! Para que Model.User.Email funcione
            .FirstOrDefaultAsync();

        if (order == null)
        {
            TempData["Error"] = "Pedido no encontrado.";
            return RedirectToAction("History");
        }

        // Enviamos el pedido a la vista "InvoicePdf.cshtml"
        // Esta vista se mostrará en una pestaña nueva (por el target="_blank")
        // y el usuario podrá imprimirla a PDF desde su navegador.
        return View("InvoicePdf", order);
    }
}