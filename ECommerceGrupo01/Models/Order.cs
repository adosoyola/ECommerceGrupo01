using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public IdentityUser? User { get; set; }

        [DataType(DataType.Currency)]
        public decimal Total { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ⚠️ COMENTAR o ELIMINAR temporalmente esta línea:
        // public string PaymentMethod { get; set; } = "Tarjeta";

        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}