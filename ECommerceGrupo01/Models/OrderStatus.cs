using System.ComponentModel.DataAnnotations;

namespace ECommerce.Models
{
    // Enum para definir los estados de una orden (Logística)
    public enum OrderStatus
    {
        [Display(Name = "Pedido Creado")]
        PedidoCreado = 0, // 0 (Estado inicial)

        [Display(Name = "En Preparación")]
        EnPreparacion = 1,

        [Display(Name = "En Tránsito")]
        EnTransito = 2,

        [Display(Name = "Entregado")]
        Entregado = 3 // (Estado final)
    }
}