using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using System.IO;
using System.Threading.Tasks;
using ECommerce.Data;
using ECommerce.Models;
using Newtonsoft.Json;

namespace ECommerce.Controllers
{
    [ApiController]
    [Route("webhook")]
    public class WebhookController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly string _webhookSecret;

        public WebhookController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _webhookSecret = config["Stripe:WebhookSecret"];
        }

        [HttpPost]
        public async Task<IActionResult> Index()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _webhookSecret
                );

                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Session;

                    if (session.Metadata.ContainsKey("Cart"))
                    {
                        var cart = JsonConvert.DeserializeObject<List<CartItem>>(session.Metadata["Cart"]);
                        var userId = session.Metadata.ContainsKey("UserId")
                            ? session.Metadata["UserId"]
                            : "anonimo";

                        // 🔹 Crear la orden
                        var order = new Order
                        {
                            UserId = userId,
                            Total = cart.Sum(x => x.UnitPrice * x.Quantity),
                            Items = new List<OrderItem>()
                        };

                        _context.Orders.Add(order);
                        await _context.SaveChangesAsync();

                        foreach (var item in cart)
                        {
                            var product = await _context.Products.FindAsync(item.ProductId);
                            if (product != null)
                            {
                                // Descontar stock
                                if (product.Stock >= item.Quantity)
                                {
                                    product.Stock -= item.Quantity;
                                }

                                // Crear OrderItem
                                var orderItem = new OrderItem
                                {
                                    OrderId = order.Id,
                                    ProductId = product.Id,
                                    ProductName = product.Name,
                                    UnitPrice = product.Price,
                                    Quantity = item.Quantity
                                };

                                order.Items.Add(orderItem);
                                _context.OrderItems.Add(orderItem);
                            }
                        }

                        await _context.SaveChangesAsync();
                    }
                }

                return Ok();
            }
            catch (StripeException e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
