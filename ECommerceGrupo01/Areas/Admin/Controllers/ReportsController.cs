using Microsoft.AspNetCore.Mvc;
using ECommerce.Data;
using System;
using System.Linq;
using ECommerce.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,ADMIN")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        // ✅ Constructor limpio: Solo necesitamos la base de datos
        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(DateTime? fechaInicio, DateTime? fechaFin)
        {
            // Configurar rango de fechas (últimos 7 días por defecto)
            var inicio = fechaInicio?.Date ?? DateTime.Now.Date.AddDays(-7);
            var fin = fechaFin?.Date.AddDays(1).AddSeconds(-1) ?? DateTime.Now.Date.AddDays(1).AddSeconds(-1);

            // 1. Obtener Órdenes
            var ordersInRange = _context.Orders
                .Where(o => o.CreatedAt >= inicio && o.CreatedAt <= fin)
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            // 2. Preparar datos para el Gráfico
            var chartData = ordersInRange
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new ChartDataPoint
                {
                    DateLabel = g.Key.ToString("dd/MM/yyyy"),
                    TotalAmount = g.Sum(o => o.Total)
                })
                .OrderBy(x => x.DateLabel)
                .ToList();

            // 3. Llenar el Modelo
            var viewModel = new ReportsViewModel
            {
                FechaInicio = inicio,
                FechaFin = fin.Date,
                OrderReport = ordersInRange,
                ProductReport = _context.Products.OrderBy(p => p.Stock).ToList(),
                ChartData = chartData
            };

            return View(viewModel);
        }
    }

    // --- ViewModels ---
    public class ReportsViewModel
    {
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public List<Order> OrderReport { get; set; } = new List<Order>();
        public List<Product> ProductReport { get; set; } = new List<Product>();
        public List<ChartDataPoint> ChartData { get; set; } = new List<ChartDataPoint>();
    }

    public class ChartDataPoint
    {
        // Inicializamos para evitar warning CS8618
        public string DateLabel { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }
}