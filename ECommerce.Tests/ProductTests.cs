using Xunit;
using ECommerce.Models;

namespace ECommerce.Tests
{
    public class ProductTests
    {
        [Fact]
        public void EstaEnStock_DeberiaRetornarTrue_CuandoStockMayorACero()
        {
            var producto = new Product
            {
                Id = 1,
                Name = "Laptop",
                Price = 1500m,
                Stock = 5
            };

            Assert.True(producto.EstaEnStock());
        }

        [Fact]
        public void EstaEnStock_DeberiaRetornarFalse_CuandoStockEsCero()
        {
            var producto = new Product
            {
                Id = 2,
                Name = "Mouse",
                Price = 50m,
                Stock = 0
            };

            Assert.False(producto.EstaEnStock());
        }
    }
}
