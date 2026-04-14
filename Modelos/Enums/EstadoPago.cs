using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Modelos.Enums
{
    public class EstadoPago
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public required string Nombre { get; set; }
    }
}
