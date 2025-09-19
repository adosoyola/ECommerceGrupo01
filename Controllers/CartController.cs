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
            cart.Add(new CartItem {
                ProductId = product.Id,
                Name = product.Name,
                UnitPrice = product.Price,
                Quantity = qty
            });
        } else {
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
