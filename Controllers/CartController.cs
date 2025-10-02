using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ECommerce.Data;
using ECommerce.Models; // <- para CartItem
using Microsoft.AspNetCore.Http; // <- para Session


public class CartController : Controller
{
    private readonly ApplicationDbContext _db;
    private const string SessionKey = "CartSession";

    public CartController(ApplicationDbContext db) => _db = db;

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
    [HttpPost]
    public async Task<IActionResult> Add(int productId, int qty = 1)
    {
        var product = await _db.Products.FindAsync(productId);
        if (product == null) return NotFound();

        var cart = GetCart();
        var item = cart.FirstOrDefault(c => c.ProductId == productId);
        if (item == null)
        {
            cart.Add(new CartItem
            {
                ProductId = product.Id,
                Name = product.Name,
                UnitPrice = product.Price,
                Quantity = qty
            });
        }
        else
        {
            item.Quantity += qty;
        }

        SaveCart(cart);
        return RedirectToAction("Index", "Cart");
    }

    public IActionResult Index()
    {
        var cart = GetCart();
        return View(cart);
    }

    [HttpPost]
    public IActionResult Update(int productId, int qty)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(c => c.ProductId == productId);
        if (item != null)
        {
            if (qty <= 0) cart.Remove(item);
            else item.Quantity = qty;
        }
        SaveCart(cart);
        return RedirectToAction("Index");
    }
}


/*

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ECommerce.Data;
using ECommerce.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public class CartController : Controller
{
    private readonly ApplicationDbContext _db;
    private const string SessionKey = "CartSession";

    public CartController(ApplicationDbContext db) => _db = db;

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
    [HttpPost]
    public async Task<IActionResult> Add(int productId, int qty = 1)
    {
        try
        {
            // Obtener producto con tracking para validar stock en tiempo real
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null) 
            {
                TempData["Error"] = "Producto no encontrado.";
                return RedirectToAction("Index", "Products");
            }

            // Validar stock disponible
            if (product.Stock <= 0)
            {
                TempData["Error"] = $"El producto '{product.Name}' no tiene stock disponible.";
                return RedirectToAction("Index", "Products");
            }

            var cart = GetCart();
            var existingItem = cart.FirstOrDefault(c => c.ProductId == productId);
            var totalRequested = existingItem != null ? existingItem.Quantity + qty : qty;

            // Validar que la cantidad solicitada no exceda el stock
            if (totalRequested > product.Stock)
            {
                TempData["Error"] = $"No hay suficiente stock. Stock disponible: {product.Stock} unidades.";
                return RedirectToAction("Index", "Products");
            }

            if (existingItem == null)
            {
                cart.Add(new CartItem 
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    UnitPrice = product.Price,
                    Quantity = qty
                });
            }
            else 
            {
                existingItem.Quantity += qty;
            }

            SaveCart(cart);
            TempData["Success"] = $"Producto agregado al carrito correctamente.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error al agregar el producto al carrito.";
            // Log the exception here
        }

        return RedirectToAction("Index", "Products");
    }

    public IActionResult Index()
    {
        var cart = GetCart();
        
        // Validar stock actualizado para cada item en el carrito
        var validatedCart = new List<CartItem>();
        foreach (var item in cart)
        {
            var product = _db.Products.Find(item.ProductId);
            if (product != null && product.Stock > 0)
            {
                // Ajustar cantidad si excede el stock actual
                if (item.Quantity > product.Stock)
                {
                    item.Quantity = product.Stock;
                    TempData["Warning"] = "Algunos productos fueron ajustados por stock insuficiente.";
                }
                validatedCart.Add(item);
            }
        }

        // Si hubo cambios, guardar el carrito validado
        if (validatedCart.Count != cart.Count)
        {
            SaveCart(validatedCart);
            cart = validatedCart;
        }

        return View(cart);
    }

    [HttpPost]
    public IActionResult Update(int productId, int qty)
    {
        var product = _db.Products.Find(productId);
        if (product == null)
        {
            TempData["Error"] = "Producto no encontrado.";
            return RedirectToAction("Index");
        }

        var cart = GetCart();
        var item = cart.FirstOrDefault(c => c.ProductId == productId);
        
        if (item != null)
        {
            if (qty <= 0)
            {
                cart.Remove(item);
                TempData["Success"] = "Producto eliminado del carrito.";
            }
            else if (qty > product.Stock)
            {
                TempData["Error"] = $"No hay suficiente stock. Stock disponible: {product.Stock} unidades.";
            }
            else
            {
                item.Quantity = qty;
                TempData["Success"] = "Cantidad actualizada correctamente.";
            }
        }

        SaveCart(cart);
        return RedirectToAction("Index");
    }

    // Nuevo método para validar stock antes del checkout
    public IActionResult ValidateStock()
    {
        var cart = GetCart();
        var errors = new List<string>();

        foreach (var item in cart)
        {
            var product = _db.Products.Find(item.ProductId);
            if (product == null)
            {
                errors.Add($"El producto '{item.Name}' ya no está disponible.");
            }
            else if (product.Stock <= 0)
            {
                errors.Add($"El producto '{item.Name}' no tiene stock disponible.");
            }
            else if (item.Quantity > product.Stock)
            {
                errors.Add($"No hay suficiente stock de '{item.Name}'. Stock disponible: {product.Stock} unidades.");
            }
        }

        if (errors.Any())
        {
            TempData["ValidationErrors"] = JsonConvert.SerializeObject(errors);
            return Json(new { success = false, errors = errors });
        }

        return Json(new { success = true });
    }
}

*/