using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.DTOs.CategoriasDTOs
{
    public class CategoriaCreacionDTO
    {
        [Required]
        public required string Nombre { get; set; }
    }
}
