namespace ECommerce.Models
{
    public class PaymentResult
    {
        // Indica si el pago fue procesado con éxito (True) o no (False).
        public bool IsSuccess { get; set; }

        // Contiene el mensaje de la transacción (éxito, error, o motivo del rechazo).
        public string Message { get; set; } = string.Empty;
    }
}