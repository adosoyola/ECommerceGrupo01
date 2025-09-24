using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Models;

namespace ECommerce.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index(string payment)
{
        if (payment == "success")
        {
            ViewBag.Message = "✅ Pago realizado con éxito.";
        
        
    }
        else if (payment == "cancel")
        {
            ViewBag.Message = "⚠️ El pago fue cancelado.";
        }

    return View();
}

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
