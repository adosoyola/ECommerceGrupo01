using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ECommerce.Data;
using ECommerce.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class CartController : Controller
{
    private readonly ApplicationDbContext _db;
    private const string SessionKey = "CartSession";

    public CartController(ApplicationDbContext db) => _db = db;

    private List<CartItem> GetCart()
    {
        var json = HttpContext.Session.GetString(SessionKey);
        return json == null ? new List<CartItem>() : JsonConvert.DeserializeObject<List<CartItem>>(json) ?? new List<CartItem>();
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
            // Obtener producto de la BD
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null) 
            {
                TempData["Error"] = "Producto no encontrado.";
                return RedirectToAction("Index", "Products");
            }

            // Validar si el producto está agotado
            if (product.Stock <= 0)
            {
                TempData["Error"] = $"El producto '{product.Name}' no tiene stock disponible.";
                return RedirectToAction("Index", "Products");
            }

            var cart = GetCart();
            var existingItem = cart.FirstOrDefault(c => c.ProductId == productId);
            var totalRequested = existingItem != null ? existingItem.Quantity + qty : qty;

            // Validar que la cantidad total no exceda el stock real
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
                    Quantity = qty,
                    Image = product.ImagePath // Guardamos la ruta de la imagen
                });
            }
            else 
            {
                existingItem.Quantity += qty;
            }

            SaveCart(cart);
            TempData["Success"] = $"Producto agregado al carrito correctamente.";
        }
        catch (Exception)
        {
            TempData["Error"] = "Error al agregar el producto al carrito.";
        }

        // ✅ CAMBIO AQUÍ: Redirigir al índice del CARRITO, no a Productos
        return RedirectToAction("Index"); 
    }

    public IActionResult Index()
    {
        var cart = GetCart();
        
        // Validar stock actualizado en tiempo real al entrar al carrito
        var validatedCart = new List<CartItem>();
        foreach (var item in cart)
        {
            var product = _db.Products.Find(item.ProductId);
            if (product != null && product.Stock > 0)
            {
                // Si la cantidad en el carrito es mayor al stock real, la ajustamos
                if (item.Quantity > product.Stock)
                {
                    item.Quantity = product.Stock;
                    TempData["Warning"] = "Algunos productos fueron ajustados por stock insuficiente.";
                }
                
                // Aseguramos que la imagen esté actualizada si cambió
                if (string.IsNullOrEmpty(item.Image))
                {
                    item.Image = product.ImagePath;
                }

                validatedCart.Add(item);
            }
        }

        // Si hubo cambios (productos eliminados por falta de stock), actualizamos la sesión
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

    // Método auxiliar para validar stock antes del checkout
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