using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Models
{
    // ENUMS (Estados y Métodos)
    public enum OrderStatus
    {
        Pendiente,
        EnPreparacion,
        EnTransito,
        ListoParaRecoger,
        Entregado,
        Cancelado
    }

    public enum DeliveryMethod
    {
        StorePickup, // Recojo en Tienda
        HomeDelivery // Envío a Domicilio
    }

    // CLASE PRINCIPAL DE LA ORDEN
    public class Order
    {
        [Key]
        public int Id { get; set; }

        // --- RELACIÓN CON USUARIO ---
        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public IdentityUser? User { get; set; }

        // --- ESTADOS Y FECHAS ---
        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Pendiente;

        [Required]
        public DeliveryMethod DeliveryMethod { get; set; } = DeliveryMethod.HomeDelivery;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // --- DATOS ECONÓMICOS ---
        // Usamos decimal(18,2) para evitar errores de redondeo en SQL
        [Required]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")] 
        public decimal Total { get; set; } 

        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingCost { get; set; }

        // --- NUEVOS CAMPOS DE ENVÍO (LOGÍSTICA) ---
        // Son nullables (?) porque en "Recojo en tienda" podrían ir vacíos
        
        [Display(Name = "Nombre del Destinatario")]
        public string? RecipientName { get; set; }

        [Display(Name = "Dirección de Entrega")]
        public string? ShippingAddress { get; set; }

        [Display(Name = "Ciudad")]
        public string? ShippingCity { get; set; }

        [Display(Name = "Teléfono de Contacto")]
        public string? ContactPhone { get; set; }

        [Display(Name = "Instrucciones Especiales")]
        public string? SpecialInstructions { get; set; }

        // --- LISTA DE PRODUCTOS ---
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    }

    // CLASE PARA CONFIGURACIONES (Precio de envío, etc.)
    // Nota: Idealmente esto iría en su propio archivo, pero funciona aquí.
    public class AppSetting
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty; 
        public string Value { get; set; } = string.Empty; 
    }
}