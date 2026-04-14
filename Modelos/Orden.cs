using EcommerceAPI.Modelos.Enums;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Modelos
{
    public class Orden
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string UsuarioId { get; set; } = null!;
        public IdentityUser Usuario { get; set; } = null!;

        [Required]
        public string EmailUsuario { get; set; } = null!; 

        [Required]
        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        [Required]
        public string DireccionEnvio { get; set; } = null!;

        public decimal Total { get; set; }

        public string EstadoOrdenId { get; set; } = null!;
        public string EstadoPagoId { get; set; } = null!;

        public EstadoOrden EstadoOrden { get; set; } = null!;
        public EstadoPago EstadoPago { get; set; } = null!;

        public List<OrdenItems> Items { get; set; } = new List<OrdenItems>();

       

    }
}
