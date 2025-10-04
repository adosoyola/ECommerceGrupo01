using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerce.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        // Relación con Order
        public int OrderId { get; set; }
        public Order Order { get; set; }

        // Relación con Product
        public int ProductId { get; set; }
        public Product Product { get; set; }

        // Datos del producto al momento de la compra
        [Required]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        public int Quantity { get; set; }
    }
}
