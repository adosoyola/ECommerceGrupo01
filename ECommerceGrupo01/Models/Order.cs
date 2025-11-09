using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Models
{
    // 👈 ESTA ES LA DEFINICIÓN DEL ENUM QUE DEBES MANTENER
    public enum OrderStatus
    {
        Pendiente, // Estado inicial después de la confirmación
        EnPreparacion,
        EnTransito,
        Entregado,
        Cancelado
    }

    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public IdentityUser? User { get; set; }

        // 👈 ESTA ES LA NUEVA PROPIEDAD Status que usa el Enum
        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Pendiente;

        [DataType(DataType.Currency)]
        public decimal Total { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ⚠️ La mantienes comentada o eliminada, como estaba en tu archivo.
        // public string PaymentMethod { get; set; } = "Tarjeta";

        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}