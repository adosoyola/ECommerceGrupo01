using Microsoft.AspNetCore.Mvc;
using ECommerce.Data;
using System;
using System.Linq;
using ECommerce.Models;
using Microsoft.EntityFrameworkCore;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using DinkToPdf;
using DinkToPdf.Contracts;



namespace ECommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConverter _converter;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;

            // Ruta al DLL nativo
            var wkhtmlPath = Path.Combine(Directory.GetCurrentDirectory(), "DinkToPdf", "libwkhtmltox.dll");

            if (!System.IO.File.Exists(wkhtmlPath))
            {
                throw new FileNotFoundException($"❌ No se encontró el archivo libwkhtmltox.dll en: {wkhtmlPath}");
            }

            // Cargar manualmente la librería nativa
            var contextLoad = new CustomAssemblyLoadContext();
            contextLoad.LoadUnmanagedLibrary(wkhtmlPath);

            _converter = new SynchronizedConverter(new PdfTools());
        }

        public IActionResult Index(DateTime? fechaInicio, DateTime? fechaFin)
        {
            var inicio = fechaInicio?.Date ?? DateTime.Now.Date.AddDays(-7);
            var fin = fechaFin?.Date.AddDays(1) ?? DateTime.Now.Date.AddDays(1);

            var ordenes = _context.Orders
                .Include(o => o.User)
                .Where(o => o.CreatedAt >= inicio && o.CreatedAt < fin)
                .ToList();

            var totalVentas = ordenes.Sum(o => (decimal?)o.Total) ?? 0;
            var ordenesTotales = ordenes.Count;
            var ordenesEntregadas = ordenes.Count(o => o.Status == OrderStatus.Entregado);

            var ventasPorDia = ordenes
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new { Fecha = g.Key, Total = g.Sum(x => x.Total) })
                .OrderBy(g => g.Fecha)
                .ToList();

            var productosMasVendidos = _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Order)
                .Where(oi => oi.Order.CreatedAt >= inicio && oi.Order.CreatedAt < fin)
                .GroupBy(oi => oi.Product.Name)
                .Select(g => new { Producto = g.Key, Cantidad = g.Sum(x => x.Quantity) })
                .OrderByDescending(g => g.Cantidad)
                .Take(5)
                .ToList();

            ViewBag.FechaInicio = inicio.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = (fechaFin?.Date ?? DateTime.Now.Date).ToString("yyyy-MM-dd");
            ViewBag.TotalVentas = totalVentas;
            ViewBag.OrdenesTotales = ordenesTotales;
            ViewBag.OrdenesEntregadas = ordenesEntregadas;
            ViewBag.VentasPorDia = ventasPorDia;
            ViewBag.ProductosMasVendidos = productosMasVendidos;
            ViewBag.Ordenes = ordenes;

            return View();
        }

        [HttpGet]
        public IActionResult ExportarPDF(DateTime? fechaInicio, DateTime? fechaFin)
        {
            var inicio = fechaInicio?.Date ?? DateTime.Now.Date.AddDays(-7);
            var fin = fechaFin?.Date.AddDays(1) ?? DateTime.Now.Date.AddDays(1);

            var ordenes = _context.Orders
                .Include(o => o.User)
                .Where(o => o.CreatedAt >= inicio && o.CreatedAt < fin)
                .ToList();

            var totalVentas = ordenes.Sum(o => (decimal?)o.Total) ?? 0;
            var ordenesTotales = ordenes.Count;
            var ordenesEntregadas = ordenes.Count(o => o.Status == OrderStatus.Entregado);

            var html = $@"
                <html>
                    <head>
                    <meta charset='UTF-8'>
                        <style>
                            body {{
                                font-family: Arial, sans-serif;
                                margin: 20px;
                            }}
                            h1 {{
                                color: #2E8B57;
                                text-align: center;
                            }}
                            table {{
                                width: 100%;
                                border-collapse: collapse;
                                margin-top: 20px;
                            }}
                            th, td {{
                                border: 1px solid #ddd;
                                padding: 8px;
                                text-align: center;
                            }}
                            th {{
                                background-color: #198754;
                                color: white;
                            }}
                        </style>
                    </head>
                    <body>
                        <h1>📊 Reporte de Ventas</h1>
                        <p><b>Desde:</b> {inicio:dd/MM/yyyy} &nbsp; <b>Hasta:</b> {fin.AddDays(-1):dd/MM/yyyy}</p>
                        <p><b>Total de Ventas:</b> S/ {totalVentas:N2}</p>
                        <p><b>Órdenes Entregadas:</b> {ordenesEntregadas}</p>
                        <p><b>Órdenes Totales:</b> {ordenesTotales}</p>

                        <h3>📦 Detalle de Órdenes</h3>
                        <table>
                            <thead>
                                <tr>
                                    <th>ID</th>
                                    <th>Cliente</th>
                                    <th>Fecha</th>
                                    <th>Total</th>
                                    <th>Estado</th>
                                </tr>
                            </thead>
                            <tbody>
                                {string.Join("", ordenes.Select(o =>
                                    $"<tr><td>{o.Id}</td><td>{o.User?.Email}</td><td>{o.CreatedAt:dd/MM/yyyy}</td><td>S/ {o.Total:N2}</td><td>{o.Status}</td></tr>"
                                ))}
                            </tbody>
                        </table>
                    </body>
                </html>";

            var pdfDoc = new HtmlToPdfDocument()
            {
                GlobalSettings = {
                    PaperSize = PaperKind.A4,
                    Orientation = Orientation.Portrait
                },
                Objects = {
                    new ObjectSettings() {
                        HtmlContent = html
                    }
                }
            };

            var file = _converter.Convert(pdfDoc);
            return File(file, "application/pdf", "Reporte.pdf");
        }
    }

    public class CustomAssemblyLoadContext : System.Runtime.Loader.AssemblyLoadContext
    {
        public IntPtr LoadUnmanagedLibrary(string absolutePath)
        {
            return LoadUnmanagedDllFromPath(absolutePath);
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            return IntPtr.Zero;
        }
    }
}