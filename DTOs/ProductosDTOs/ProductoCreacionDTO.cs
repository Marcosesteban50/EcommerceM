using EcommerceAPI.Modelos;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.DTOs.ProductosDTOs
{
    public class ProductoCreacionDTO
    {
        [Required]
        public string? Nombre { get; set; }

        public string? Descripcion { get; set; }

        [Required]
        public decimal Precio { get; set; }

        public IFormFile? ImagenUrl { get; set; }
        public string? CategoriaId { get; set; }
        

        

        public int Stock { get; set; }

       
    }
}
