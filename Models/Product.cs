using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;//nuevo


namespace ECommerce.Models
{


    public class Product
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        public int Stock { get; set; }

        public string Description { get; set; } = string.Empty;

        //public string ImageUrl { get; set; }

         public string? ImagePath { get; set; } // ruta de la imagen
       
       [NotMapped] // 👈 evita que EF intente mapearlo a la BD
        public IFormFile? ImageFile { get; set; }
    }
}