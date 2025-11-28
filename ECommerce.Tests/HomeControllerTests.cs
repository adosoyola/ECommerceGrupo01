using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ECommerce.Controllers; // Usamos el namespace 'ECommerce' para Controllers

namespace ECommerce.Tests
{
    // Clase de prueba para verificar la funcionalidad básica de HomeController
    public class HomeControllerTests
    {
        private readonly Mock<ILogger<HomeController>> _mockLogger;
        private readonly HomeController _controller;

        public HomeControllerTests()
        {
            // ARRANGE GLOBAL: Configuración mínima
            _mockLogger = new Mock<ILogger<HomeController>>();
            
            // Creamos la instancia real del Controller
            // El constructor de HomeController solo pide ILogger, que podemos mockear fácilmente.
            _controller = new HomeController(_mockLogger.Object);
        }

        [Fact]
        public void Privacy_ShouldReturnView()
        {
            // ACT
            var result = _controller.Privacy();

            // ASSERT
            // 1. Verificar el tipo de retorno: debe ser ViewResult
            Assert.IsType<ViewResult>(result);
        }
    }
}