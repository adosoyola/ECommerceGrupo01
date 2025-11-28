using Xunit;
// Asumimos que el namespace de tus modelos es 'ECommerce.Models'
using ECommerce.Models; 

namespace ECommerce.Tests
{
    // Clase de prueba para verificar la lógica interna del modelo Product.
    public class ProductTests
    {
        [Fact]
        public void EstaEnStock_ShouldReturnTrue_WhenStockIsPositive()
        {
            // ARRANGE: Configurar un producto con stock positivo
            var product = new Product { Stock = 5 };

            // ACT
            var result = product.EstaEnStock();

            // ASSERT
            // Esperamos que el resultado sea verdadero (en stock)
            Assert.True(result);
        }

        [Fact]
        public void EstaEnStock_ShouldReturnFalse_WhenStockIsZero()
        {
            // ARRANGE: Configurar un producto con stock en cero
            var product = new Product { Stock = 0 };

            // ACT
            var result = product.EstaEnStock();

            // ASSERT
            // Esperamos que el resultado sea falso (agotado)
            Assert.False(result);
        }

        [Fact]
        public void EstaEnStock_ShouldReturnFalse_WhenStockIsNegative()
        {
            // ARRANGE: Configurar un producto con stock negativo (por si acaso)
            var product = new Product { Stock = -1 };

            // ACT
            var result = product.EstaEnStock();

            // ASSERT
            // Esperamos que el resultado sea falso (agotado)
            Assert.False(result);
        }
    }
}