using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Data;
using System.Linq;

namespace ECommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Panel principal
        public IActionResult Index()
        {
            var products = _context.Products.ToList();
            var orders = _context.Orders.ToList();
            var users = _context.Users.ToList();

            ViewBag.ProductCount = products.Count;
            ViewBag.OrderCount = orders.Count;
            ViewBag.UserCount = users.Count;

            return View();
        }
    }
}