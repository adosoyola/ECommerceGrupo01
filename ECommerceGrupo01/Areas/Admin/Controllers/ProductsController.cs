using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Data;
using ECommerce.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment; //nuevo

        public ProductsController(ApplicationDbContext db, IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;//nuevo
        }

        // GET: Admin/Products
        public async Task<IActionResult> Index()
        {
            var products = await _db.Products.ToListAsync();
            return View(products);
        }

        // GET: Admin/Products/Create

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Products/Create
      [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(Product product)
{
    Console.WriteLine($"➡️ Nombre recibido: {product.Name}");
    Console.WriteLine($"➡️ Precio recibido: {product.Price}");
    Console.WriteLine($"➡️ Archivo recibido: {(product.ImageFile != null ? product.ImageFile.FileName : "Ninguno")}");

    try
    {
        if (product.ImageFile != null && product.ImageFile.Length > 0)
        {
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(product.ImageFile.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await product.ImageFile.CopyToAsync(stream);
            }

            product.ImagePath = "/images/" + uniqueFileName;
        }

        // 🔹 Validamos recién después de asignar la ruta de imagen
        if (!ModelState.IsValid)
        {
            foreach (var key in ModelState.Keys)
            {
                var state = ModelState[key];
                foreach (var error in state.Errors)
                {
                    Console.WriteLine($"Error en {key}: {error.ErrorMessage}");
                }
            }
            return View(product);
        }

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        Console.WriteLine("✅ Producto guardado correctamente");
        return RedirectToAction(nameof(Index));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error al guardar producto: {ex.Message}");
        return View(product);
    }
}
           

       // GET: Admin/Products/Edit/5
[HttpGet]
public async Task<IActionResult> Edit(int? id)
{
    if (id == null)
    {
        return NotFound();
    }

    var product = await _db.Products.FindAsync(id);
    if (product == null)
    {
        return NotFound();
    }

    return View(product);
}

// POST: Admin/Products/Edit/5
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(int id, Product product)
{
    if (id != product.Id)
    {
        return NotFound();
    }

    if (ModelState.IsValid)
    {
        try
        {
            var existingProduct = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

            if (existingProduct == null)
            {
                return NotFound();
            }

            // ✅ Si se sube una nueva imagen
            if (product.ImageFile != null && product.ImageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Eliminar imagen anterior
                if (!string.IsNullOrEmpty(existingProduct.ImagePath))
                {
                    string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, existingProduct.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(product.ImageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await product.ImageFile.CopyToAsync(stream);
                }

                product.ImagePath = "/images/" + uniqueFileName;
            }
            else
            {
                // Mantener la imagen anterior si no se reemplaza
                product.ImagePath = existingProduct.ImagePath;
            }

            _db.Update(product);
            await _db.SaveChangesAsync();

            Console.WriteLine($"✅ Producto actualizado: {product.Name}");
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al editar: {ex.Message}");
        }
    }

    return View(product);
}

       // GET: Admin/Products/Delete/5
[HttpGet]
public async Task<IActionResult> Delete(int? id)
{
    if (id == null)
    {
        return NotFound();
    }

    var product = await _db.Products
        .FirstOrDefaultAsync(m => m.Id == id);

    if (product == null)
    {
        return NotFound();
    }

    return View(product);
}

// POST: Admin/Products/Delete/5
[HttpPost, ActionName("Delete")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteConfirmed(int id)
{
    var product = await _db.Products.FindAsync(id);
    if (product != null)
    {
        // 🔹 Eliminar imagen física si existe
        if (!string.IsNullOrEmpty(product.ImagePath))
        {
            string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, product.ImagePath.TrimStart('/'));
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }
        }

        // 🔹 Eliminar el producto de la base de datos
        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        Console.WriteLine($"🗑️ Producto eliminado: {product.Name}");
    }

    return RedirectToAction(nameof(Index));
}

// GET: Admin/Products/Details/5
[HttpGet]
public async Task<IActionResult> Details(int? id)
{
    if (id == null)
    {
        return NotFound();
    }

    var product = await _db.Products
        .FirstOrDefaultAsync(p => p.Id == id);

    if (product == null)
    {
        return NotFound();
    }

    return View(product);
}
    }
}
