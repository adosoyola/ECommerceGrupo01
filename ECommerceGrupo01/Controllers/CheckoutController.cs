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

    public CheckoutController(ApplicationDbContext db, UserManager<IdentityUser> userManager, PaymentProcessor paymentProcessor)
    {
        _db = db;
        _userManager = userManager;
        _paymentProcessor = paymentProcessor;
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

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessPaymentSimulation(PaymentViewModel model)
    {
        try
        {
            var cart = GetCart();
            if (cart == null || !cart.Any())
            {
                TempData["Error"] = "El carrito está vacío.";
                return RedirectToAction("Index", "Cart");
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            System.Console.WriteLine("=== INICIANDO PAGO SIMULADO ===");

            // Validar stock antes de procesar
            foreach (var item in cart)
            {
                var product = await _db.Products.FindAsync(item.ProductId);
                if (product == null || product.Stock < item.Quantity)
                {
                    TempData["Error"] = $"Stock insuficiente para {item.Name}.";
                    return RedirectToAction("Index", "Cart");
                }
            }

            var total = cart.Sum(x => x.UnitPrice * x.Quantity);

            // CREAR ORDER SIN PaymentMethod (para evitar el error)
            var order = new Order
            {
                UserId = userId,
                Total = total,
                CreatedAt = DateTime.Now
                // NO incluir PaymentMethod - se elimina temporalmente
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync(); // Guardar orden primero

            System.Console.WriteLine($"Orden creada ID: {order.Id}");

            // CREAR ORDERITEMS y descontar stock
            foreach (var item in cart)
            {
                var product = await _db.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    // Descontar stock
                    product.Stock -= item.Quantity;
                    System.Console.WriteLine($"Stock actualizado: {product.Name} -> {product.Stock}");

                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        ProductName = item.Name,
                        UnitPrice = item.UnitPrice,
                        Quantity = item.Quantity
                    };

                    _db.OrderItems.Add(orderItem);
                }
            }

            await _db.SaveChangesAsync(); // Guardar items y stock

            // VACIAR CARRITO
            SaveCart(new List<CartItem>());

            System.Console.WriteLine("=== PAGO SIMULADO EXITOSO ===");
            TempData["Success"] = $"¡Pago simulado exitoso! Orden #: {order.Id}";
            return RedirectToAction("Success", "Payments");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"=== ERROR: {ex.Message}");
            TempData["Error"] = "Error en el proceso. Intente nuevamente.";
            return RedirectToAction("PaymentSimulation");
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
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return View(orders);
    }
}