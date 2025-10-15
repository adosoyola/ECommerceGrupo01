namespace ECommerce.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }

        // CORRECCIÓN: La propiedad Quantity estaba comentada
        private int _quantity;
        public int Quantity
        {
            get => _quantity <= 0 ? 1 : _quantity;
            set => _quantity = value;
        }

        public decimal Total => UnitPrice * Quantity;

        public decimal GetSubtotal()
        {
            return UnitPrice * Quantity;
        }
    }
}