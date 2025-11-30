using System;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Models
{
    public class DeliveryInfoViewModel
    {
        // --- DATOS DEL DESTINATARIO ---

        [Display(Name = "Nombre Completo")]
        public string FullName { get; set; }

        [Display(Name = "Dirección de Entrega")]
        public string Address { get; set; }

        [Display(Name = "Código Postal")]
        public string PostalCode { get; set; } // ✅ Nuevo Campo

        [Display(Name = "Ciudad")]
        public string City { get; set; }

        [Display(Name = "Teléfono de Contacto")]
        [Phone]
        public string PhoneNumber { get; set; }

        // --- OPCIONAL ---
        [Display(Name = "Instrucciones Especiales")]
        public string? SpecialInstructions { get; set; } // ✅ Marcado como opcional (?)

        // --- DATOS DE LÓGICA ---
        
        // Fecha calculada (solo visualización)
        public string EstimatedDeliveryDate { get; set; } = DateTime.Now.AddDays(2).ToString("dd/MM/yyyy");
        
        // "Home" o "Store"
        public string DeliveryMethod { get; set; } 
    }
}