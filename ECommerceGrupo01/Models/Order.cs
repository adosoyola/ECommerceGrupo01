using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Models
{
    public enum OrderStatus
    {
        Pendiente,
        EnPreparacion,
        EnTransito,
        ListoParaRecoger, // ✅ NUEVO ESTADO
        Entregado,
        Cancelado
    }

    public enum DeliveryMethod
    {
        StorePickup, 
        HomeDelivery 
    }

    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public IdentityUser? User { get; set; }

        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Pendiente;

        [Required]
        public DeliveryMethod DeliveryMethod { get; set; } = DeliveryMethod.HomeDelivery;

        [DataType(DataType.Currency)]
        public decimal Total { get; set; } // Total (Productos + Envío)

        // ✅ NUEVO: Propiedad para guardar cuánto costó el envío
        [DataType(DataType.Currency)]
        public decimal ShippingCost { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    }

    // ✅ NUEVO: Clase para guardar configuraciones (como el precio de envío)
    // Esto es lo que le faltaba a tu proyecto para compilar
    public class AppSetting
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty; // Ej: "ShippingCost"
        public string Value { get; set; } = string.Empty; // Ej: "6.00"
    }
}