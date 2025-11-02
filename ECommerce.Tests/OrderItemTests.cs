using Xunit;
// Asumimos que el namespace de tus modelos es 'ECommerce.Models'
using ECommerce.Models; 

namespace ECommerce.Tests
{
    // Clase para verificar la estructura y cálculos de un ítem de orden.
    public class OrderItemTests
    {
        [Fact]
        public void OrderItem_ShouldInitializeAllPropertiesCorrectly()
        {
            // ARRANGE
            // Crear una instancia de OrderItem y asignar todos los valores
            var item = new OrderItem
            {
                Id = 1,
                OrderId = 100,
                ProductId = 50,
                ProductName = "Laptop Core i7",
                UnitPrice = 750.00m,
                Quantity = 2
            };

            // ACT & ASSERT (Verificar que los valores se mantengan)
            Assert.Equal(1, item.Id);
            Assert.Equal(100, item.OrderId);
            Assert.Equal(50, item.ProductId);
            Assert.Equal("Laptop Core i7", item.ProductName);
            Assert.Equal(750.00m, item.UnitPrice);
            Assert.Equal(2, item.Quantity);
        }

        [Fact]
        public void OrderItem_CalculatedTotal_ShouldBeCorrect()
        {
            // ARRANGE
            decimal price = 15.50m;
            int quantity = 3;
            decimal expectedTotal = 46.50m; // 15.50 * 3 = 46.50

            var item = new OrderItem
            {
                UnitPrice = price,
                Quantity = quantity
            };

            // ACT
            decimal actualTotal = item.UnitPrice * item.Quantity;

            // ASSERT
            // Verificar que la multiplicación de cantidad * precio unitario es correcta.
            Assert.Equal(expectedTotal, actualTotal);
        }

        [Fact]
        public void OrderItem_DefaultProductName_ShouldNotBeNull()
        {
            // ARRANGE
            var item = new OrderItem();
            
            // ACT & ASSERT
            // Verifica que ProductName se inicializa a string.Empty (como está en el modelo)
            Assert.NotNull(item.ProductName); 
            Assert.Equal(string.Empty, item.ProductName);
        }
    }
}