using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ECommerce.Data;
using ECommerce.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;


public class CheckoutController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private const string SessionKey = "CartSession";

    public CheckoutController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    //

   [HttpGet]
public IActionResult PreConfirm()
{
    if (!User.Identity.IsAuthenticated)
    {
        // redirige al login de Identity
        return Redirect("/Identity/Account/Login?returnUrl=/Checkout/Payment");
    }

    return RedirectToAction("Payment");
}

    [Authorize]// Solo usuarios logueados pueden ver su historial
     
     [HttpGet]
        public async Task<IActionResult> History()
    {
        var userId = _userManager.GetUserId(User);
        var orders = await _db.Orders
        .Where(o => o.UserId == userId)
        .Include(o => o.Items) // para traer los productos de la orden
        .ThenInclude(i => i.Product) // para mostrar nombre y datos del producto
        .OrderByDescending(o => o.Id)
        .ToListAsync();

        return View(orders);
    }

    private List<CartItem> GetCart()
    {
        var json = HttpContext.Session.GetString(SessionKey);
        return json == null ? new List<CartItem>() : JsonConvert.DeserializeObject<List<CartItem>>(json);
    }

    private void SaveCart(List<CartItem> cart)
    {
        HttpContext.Session.SetString(SessionKey, JsonConvert.SerializeObject(cart));
    }
    [HttpGet]
    public IActionResult Confirm()
    {
    var cart = GetCart();
    return View(cart);
    }

    public IActionResult Payment()
    {
        var cart = GetCart();
        if (cart == null || !cart.Any())
            return RedirectToAction("Index", "Cart");

        var total = cart.Sum(x => x.UnitPrice * x.Quantity);

        return View(new PaymentViewModel { Amount = total });
    }

[HttpPost]
public async Task<IActionResult> Payment(PaymentViewModel model)
{
    if (!ModelState.IsValid)
        return View(model);

    // Simulación de validación
    if (model.CardNumber.StartsWith("4"))
    {
        // Tarjetas que empiezan con 4 → pago aprobado
       return RedirectToAction("Confirm");

    }
    else
    {
        ModelState.AddModelError("", "Pago rechazado. Intente con otra tarjeta.");
        return View(model);
    }
}

    [Authorize]
    [HttpPost, ActionName("Confirm")]
    
    
    public async Task<IActionResult> ConfirmPost()
    {
        var cart = GetCart();
        if (cart == null || !cart.Any())
        {
            TempData["Error"] = "Tu carrito está vacío";
            return RedirectToAction("Index", "Cart");
        }

        // Verificar stock
        foreach (var item in cart)
        {
            var product = await _db.Products.FindAsync(item.ProductId);
            if (product == null) continue;

            if (product.Stock < item.Quantity)
            {
                TempData["Error"] = $"No hay suficiente stock de {product.Name}";
                return RedirectToAction("Index", "Cart");
            }
        }



        // Crear Order
        var userId = _userManager.GetUserId(User) ?? "anonimo";
        if (userId == null)
        {
            return RedirectToAction("Login", "Account", new { area = "Identity" });
        }
        var order = new Order
        {
            UserId = userId,
            Total = cart.Sum(x => x.UnitPrice * x.Quantity),
            Items = new List<ECommerce.Models.OrderItem>() // asegurar inicialización

        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // Crear OrderItems y descontar stock
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

            order.Items.Add(orderItem);
            _db.OrderItems.Add(orderItem);
        }

        await _db.SaveChangesAsync();

        // Vaciar carrito
        SaveCart(new List<CartItem>());

        TempData["Success"] = "Compra realizada con éxito ✅";
        return RedirectToAction("Index", "Home");
    }
}
