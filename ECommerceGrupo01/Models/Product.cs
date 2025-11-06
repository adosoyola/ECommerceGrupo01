using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace ECommerce.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El campo Nombre es obligatorio")]
        public string Name { get; set; }

        [Required(ErrorMessage = "El campo Precio es obligatorio")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "El campo Stock es obligatorio")]
        public int Stock { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria")]
        public string Description { get; set; }

        // ⚠️ Quita el [Required] de ImagePath si la validación se hace desde el controlador
        public string? ImagePath { get; set; }

         [NotMapped]

        [Display(Name = "Imagen del Producto")]
        public IFormFile? ImageFile { get; set; }
    }
}