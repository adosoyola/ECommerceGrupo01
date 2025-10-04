using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ECommerce.Data;
using ECommerce.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
// using Stripe.Checkout; // Eliminamos la referencia a Stripe para un flujo de pago interno

public class CheckoutController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private const string SessionKey = "CartSession";
    private const string PaymentSessionKey = "HasPaymentData";

    public CheckoutController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    private List<CartItem> GetCart()
    {
        var json = HttpContext.Session.GetString(SessionKey);
        return string.IsNullOrEmpty(json)
            ? new List<CartItem>()
            : JsonConvert.DeserializeObject<List<CartItem>>(json);
    }

    private void SaveCart(List<CartItem> cart)
    {
        HttpContext.Session.SetString(SessionKey, JsonConvert.SerializeObject(cart));
    }

    // ACCIÓN 1: Muestra la página de resumen (GET /Checkout/Confirm). 
    // Ahora redirige a la simulación.
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

        // Redirige al nuevo paso de simulación de pago
        return RedirectToAction("PaymentSimulation");
    }

    // ACCIÓN 2: Muestra la vista de simulación de pago (GET /Checkout/PaymentSimulation)
    // Se necesita crear la vista PaymentSimulation.cshtml
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

        // Pasa el carrito a la vista para mostrar el total y permitir el POST
        return View(cart);
    }

    // ACCIÓN 3: Procesa la creación de la orden (POST /Checkout/Confirm)
    // 🔑 Solución al error CS0111: Usamos [ActionName] y un nombre de método diferente.
    [HttpPost]
    [ActionName("Confirm")] // Esto mapea este método a la URL /Checkout/Confirm (POST)
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmOrder(string paymentMethod) // paymentMethod viene del formulario de simulación
    {
        var cart = GetCart();
        if (cart == null || !cart.Any())
        {
            TempData["Error"] = "El carrito está vacío.";
            return RedirectToAction("Index", "Cart");
        }

        // 1. Verificación de Stock y usuario
        foreach (var item in cart)
        {
            var product = await _db.Products.FindAsync(item.ProductId);
            if (product == null || product.Stock < item.Quantity)
            {
                TempData["Error"] = $"Stock insuficiente para {item.Name}.";
                return RedirectToAction("Index", "Cart");
            }
        }

        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return RedirectToAction("Login", "Account", new { area = "Identity" });
        }

        // 2. Crear Order (Conexión directa a la DB)
        var order = new Order
        {
            UserId = userId,
            Total = cart.Sum(x => x.UnitPrice * x.Quantity),
            CreatedAt = DateTime.Now,
            // Si el modelo Order tiene una propiedad PaymentMethod, úsala aquí:
            // PaymentMethod = paymentMethod, 
            Items = new List<ECommerce.Models.OrderItem>()
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // 3. Crear OrderItems y descontar stock
        foreach (var item in cart)
        {
            var product = await _db.Products.FindAsync(item.ProductId);
            if (product == null) continue;

            product.Stock -= item.Quantity;

            var orderItem = new ECommerce.Models.OrderItem
            {
                ProductId = item.ProductId,
                ProductName = item.Name,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                OrderId = order.Id
            };

            _db.OrderItems.Add(orderItem);
        }

        await _db.SaveChangesAsync();

        // 4. Finalizar
        SaveCart(new List<CartItem>()); // Vaciar carrito

        TempData["Success"] = $"Compra realizada con éxito ✅. Método de pago simulado: {paymentMethod}.";
        return RedirectToAction("History", "Checkout");
    }

    // ACCIÓN 4: Muestra el historial de compras
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> History()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return RedirectToAction("Login", "Account", new { area = "Identity" });
        }

        // 🔑 Solución al NullReferenceException: Carga explícita de las relaciones.
        var orders = await _db.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return View(orders);
    }
}