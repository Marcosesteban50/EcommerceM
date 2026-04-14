using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.DTOs.OrdenDTOs
{
    public class EstadoPagoCreacionDTO
    {
        [Required]
        public required string Nombre { get; set; }
    }
}
