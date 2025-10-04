using Xunit;
using ECommerce.Models; // importa tus modelos

namespace ECommerce.Tests
{
    public class CartItemTests
    {
        [Fact]
        public void CalcularSubtotal_DeberiaMultiplicarPrecioPorCantidad()
        {
            // Arrange (configurar datos de prueba)
            var item = new CartItem
            {
                ProductId = 1,
                Name = "Producto Test",
                UnitPrice = 50m,
                Quantity = 3
            };

            // Act (ejecutar acción)
            var subtotal = item.GetSubtotal();

            // Assert (verificar resultado esperado)
            Assert.Equal(150m, subtotal);
        }

        [Fact]
        public void Quantity_NoDeberiaSerMenorQueUno()
        {
            // Arrange
            var item = new CartItem
            {
                ProductId = 2,
                Name = "Producto con cantidad inválida",
                UnitPrice = 10m,
                Quantity = 0 //intencionalmente inválido
            };

            // Act
            var subtotal = item.GetSubtotal();

            // Assert → si Quantity < 1, el sistema debe corregirlo a 1
            Assert.Equal(10m, subtotal);
        }
    }
}
