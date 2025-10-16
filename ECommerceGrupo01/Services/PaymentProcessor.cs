using ECommerce.Models;
using System; // Necesario si quieres usar excepciones o tipos de datos complejos

namespace ECommerce.Services
{
    public class PaymentProcessor
    {
        /// <summary>
        /// Procesa un pago simulado verificando el monto y el saldo disponible.
        /// </summary>
        /// <param name="amount">Monto total del pedido a pagar.</param>
        /// <param name="availableBalance">Saldo o límite de crédito disponible.</param>
        /// <returns>Un objeto PaymentResult que indica el estado de la transacción.</returns>
        public PaymentResult ProcessPayment(decimal amount, decimal availableBalance)
        {
            // 1. Validación de Monto (Requisito de la prueba: Monto > 0)
            if (amount <= 0)
            {
                return new PaymentResult
                {
                    IsSuccess = false,
                    Message = "El monto del pago debe ser positivo."
                };
            }

            // 2. Validación de Saldo (Requisito de la prueba: Saldo suficiente)
            if (amount > availableBalance)
            {
                return new PaymentResult
                {
                    IsSuccess = false,
                    Message = "Saldo insuficiente para completar la transacción."
                };
            }

            // 3. Simulación de Transacción Exitosa
            // Aquí iría la lógica real de comunicación con una pasarela (PayPal, Stripe).
            return new PaymentResult
            {
                IsSuccess = true,
                Message = "Pago interno procesado con éxito."
            };
        }
    }
}