using Xunit;
using ECommerce.Models;
using ECommerce.Services;

namespace ECommerce.Tests
{
    // Esta clase probará la lógica de la clase PaymentProcessor
    public class PaymentProcessorTests
    {
        // Supongamos que tenemos un objeto 'PaymentResult' para devolver el estado y el mensaje

        [Fact]
        public void ProcessPayment_DeberiaRetornarExito_ParaTransaccionValida()
        {
            // Arrange (Configurar datos de prueba)
            decimal orderTotal = 150.75m;
            decimal customerBalance = 500.00m;

            var processor = new PaymentProcessor();

            // Act (Ejecutar acción)
            var result = processor.ProcessPayment(orderTotal, customerBalance);

            // Assert (Verificar resultado esperado)
            Assert.True(result.IsSuccess);
            Assert.Equal("Pago interno procesado con éxito.", result.Message);
        }

        [Fact]
        public void ProcessPayment_DeberiaRetornarFallo_CuandoMontoEsCeroONegativo()
        {
            // Arrange
            decimal orderTotal = -10.00m;
            decimal customerBalance = 100.00m;

            var processor = new PaymentProcessor();

            // Act
            var result = processor.ProcessPayment(orderTotal, customerBalance);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("El monto del pago debe ser positivo.", result.Message);
        }

        [Fact]
        public void ProcessPayment_DeberiaRetornarFallo_CuandoSaldoEsInsuficiente()
        {
            // Arrange
            decimal orderTotal = 1200.00m;
            decimal customerBalance = 1000.00m;

            var processor = new PaymentProcessor();

            // Act
            var result = processor.ProcessPayment(orderTotal, customerBalance);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Saldo insuficiente para completar la transacción.", result.Message);
        }
    }
}