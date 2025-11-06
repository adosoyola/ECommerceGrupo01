using Microsoft.AspNetCore.Mvc;
using ECommerce.Data;
using System;
using System.Linq;

namespace ECommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

       public IActionResult Index(DateTime? fechaInicio, DateTime? fechaFin)
{
    // Si no se selecciona rango, mostrar últimos 7 días
    var inicio = fechaInicio?.Date ?? DateTime.Now.Date.AddDays(-7);
    // Para incluir todo el día 'fechaFin', tomamos hasta el final del día
    var fin = (fechaFin?.Date).HasValue 
                ? fechaFin.Value.Date.AddDays(1) // usamos < fin (start of next day)
                : DateTime.Now.Date.AddDays(1);

    // Filtrar las órdenes por rango de fechas: CreatedAt >= inicio AND CreatedAt < fin (fin exclusivo)
    var ordenesFiltradas = _context.Orders
        .Where(o => o.CreatedAt >= inicio && o.CreatedAt < fin)
        .ToList();

    // Datos generales (ya filtrados)
    var totalVentas = ordenesFiltradas.Sum(o => (decimal?)o.Total) ?? 0;
    var ordenesTotales = ordenesFiltradas.Count;
    var ordenesEntregadas = ordenesFiltradas.Count(o => o.Status == Models.OrderStatus.Entregado);

    // Ventas por día (dentro del rango)
    var ventasPorDia = ordenesFiltradas
        .GroupBy(o => o.CreatedAt.Date)
        .Select(g => new { Fecha = g.Key, Total = g.Sum(x => x.Total) })
        .OrderBy(g => g.Fecha)
        .ToList();

    // Productos más vendidos DENTRO DEL RANGO: 
    // asumimos que OrderItem tiene OrderId y Quantity, y relación con Product
    var productosMasVendidos = _context.OrderItems
        .Where(oi => oi.Order.CreatedAt >= inicio && oi.Order.CreatedAt < fin)
        .GroupBy(oi => oi.Product.Name)
        .Select(g => new { Producto = g.Key, Cantidad = g.Sum(x => x.Quantity) })
        .OrderByDescending(g => g.Cantidad)
        .Take(5)
        .ToList();

    // Pasar datos a la vista
    ViewBag.FechaInicio = inicio.ToString("yyyy-MM-dd");
    // Para mostrar el date input usamos la fecha exacta (no el +1)
    ViewBag.FechaFin = (fechaFin?.Date ?? DateTime.Now.Date).ToString("yyyy-MM-dd");

    ViewBag.TotalVentas = totalVentas;
    ViewBag.OrdenesTotales = ordenesTotales;
    ViewBag.OrdenesEntregadas = ordenesEntregadas;
    ViewBag.VentasPorDia = ventasPorDia;
    ViewBag.ProductosMasVendidos = productosMasVendidos;

    return View();
}
    }
}
