using System.ComponentModel.DataAnnotations;

namespace ECommerce.Models
{
    public class PaymentViewModel
    {
        [Required, CreditCard, Display(Name = "Número de tarjeta")]
        public string CardNumber { get; set; } = string.Empty;

        [Required, Display(Name = "Fecha de expiración (MM/AA)")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{2}$", ErrorMessage = "Formato inválido (MM/AA)")]
        public string Expiration { get; set; } = string.Empty;

        [Required, StringLength(4, MinimumLength = 3)]
        [Display(Name = "CVV")]
        public string CVV { get; set; } = string.Empty;

        [Required, Range(1, double.MaxValue, ErrorMessage = "Monto inválido")]
        public decimal Amount { get; set; }
    }
}
