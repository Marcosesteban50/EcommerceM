namespace EcommerceAPI.DTOs.ProductosDTOs
{
    namespace EcommerceAPI.DTOs
    {
        public class ProductoHistorialDTO
        {
            // Identidad
            public string? Id { get; set; }

            // Producto
            public string ProductoId { get; set; } = null!;
            public string? ProductoNombre { get; set; }   // opcional (si lo traes por join)
            public string? ImagenUrl { get; set; }         // opcional

            // Usuario
            public string UsuarioId { get; set; } = null!;
            public string UsuarioNombre { get; set; } = null!;

            // Categoría
            public string? CategoriaId { get; set; }
            public string? CategoriaNombre { get; set; }

            // Historial
            public string Accion { get; set; } = null!;
            public string? DatosAntes { get; set; }
            public string? DatosDespues { get; set; }
            public DateTime FechaCreacion { get; set; }
        }
    }

}
