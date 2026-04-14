using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.DTOs.OrdenDTOs
{
    public class EstadoOrdenCreacionDTO
    {
        [Required]

        public required string Nombre { get; set; }

    }
}
