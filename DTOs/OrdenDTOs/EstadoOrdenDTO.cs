using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.DTOs.OrdenDTOs
{
    public class EstadoOrdenDTO
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

       
        public required string Nombre { get; set; }
    }
}
