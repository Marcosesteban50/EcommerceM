using EcommerceAPI.Modelos;
using EcommerceAPI.Modelos.Enums;

namespace EcommerceAPI.DTOs.OrdenDTOs
{
    public class OrdenListadoDTO
    {
        public string Id { get; set; } = null!;
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
        public string DireccionEnvio { get; set; } = null!;

        public string EmailUsuario { get; set; } = null!;
        public EstadoOrden EstadoOrden { get; set; } = null!;
        public EstadoPago EstadoPago { get; set; } = null!;
        public List<OrdenItemDTO> Items { get; set; } = new();

    }
}
