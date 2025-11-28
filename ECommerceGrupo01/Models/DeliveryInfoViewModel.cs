using System;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Models // Asegúrate que el namespace coincida con tu proyecto
{
    public class DeliveryInfoViewModel
    {
        // Estos campos son obligatorios solo si eligen envío a domicilio
        [Display(Name = "Nombre Completo")]
        public string FullName { get; set; }

        [Display(Name = "Dirección de Entrega")]
        public string Address { get; set; }

        [Display(Name = "Ciudad")]
        public string City { get; set; }

        [Display(Name = "Teléfono de Contacto")]
        [Phone]
        public string PhoneNumber { get; set; }

        [Display(Name = "Instrucciones Especiales")]
        public string SpecialInstructions { get; set; }

        // Este campo lo calcularemos nosotros, el usuario solo lo ve
        public string EstimatedDeliveryDate { get; set; } = DateTime.Now.AddDays(2).ToString("dd/MM/yyyy");
        
        // Para saber qué eligió el usuario (Domicilio o Tienda)
        public string DeliveryMethod { get; set; } 
    }
}