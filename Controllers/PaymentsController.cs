using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using ECommerce.Data;
using ECommerce.Models;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;



namespace ECommerce.Controllers
{
    [Authorize] // obliga a estar logueado
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string SessionKey = "CartSession";

        private readonly UserManager<IdentityUser> _userManager;

        public PaymentsController(ApplicationDbContext context, IConfiguration config, UserManager<IdentityUser> userManager)
        {
            _context = context;
           
            _userManager = userManager;
        }

        private List<CartItem> GetCart()
        {
            var json = HttpContext.Session.GetString(SessionKey);
            return string.IsNullOrEmpty(json)
                ? new List<CartItem>()
                : JsonConvert.DeserializeObject<List<CartItem>>(json);
        }

        [HttpPost]
        public IActionResult CreateCheckoutSession()
        {

            
            var domain = $"{Request.Scheme}://{Request.Host}";
            var cart = GetCart();

            if (cart == null || !cart.Any())
            {
                return BadRequest("El carrito está vacío.");
            }

            var lineItems = new List<SessionLineItemOptions>();

            foreach (var item in cart)
            {
                var product = _context.Products.FirstOrDefault(p => p.Id == item.ProductId);
                if (product == null) continue;

                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(product.Price * 100), // Stripe trabaja en centavos
                        Currency = "pen",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = product.Name,
                            Description = product.Description
                        }
                    },
                    Quantity = item.Quantity
                });
            }

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = lineItems,
                Mode = "payment",
                SuccessUrl = domain + "/?payment=success",
                CancelUrl = domain + "/?payment=cancel",
                Metadata = new Dictionary<string, string>
                {
                    { "Cart", JsonConvert.SerializeObject(cart) },// 🔑 guardamos carrito en metadata
                    { "UserId", User.Identity.IsAuthenticated ? _userManager.GetUserId(User) : "anonimo" }
                }
            };

            var service = new SessionService();
            Session session = service.Create(options);

            return Redirect(session.Url);
        }
    }
}
//stripe listen --forward-to http://localhost:5012/webhook