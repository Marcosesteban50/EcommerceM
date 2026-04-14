using EcommerceAPI.Modelos;

namespace EcommerceAPI.DTOs.ProductosDTOs
{
    public class ProductoFiltrarDTO
    {
        public string? Nombre { get; set; }
        public string? CategoriaId { get; set; }
        public bool EnStock { get; set; } = true;
        public decimal? PrecioMin { get; set; }
        public decimal? PrecioMax { get; set; }

        public bool? PrecioMinBoolean { get; set; }
        public bool? PrecioMaxBoolean { get; set; }

        public bool Aprobado { get; set; }
    }

}
