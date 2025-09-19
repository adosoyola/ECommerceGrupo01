namespace ECommerce.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }

        public decimal Total => UnitPrice * Quantity;
    }
}
