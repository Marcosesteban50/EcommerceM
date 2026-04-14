using EcommerceAPI.Modelos.Enums;
using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Modelos
{
    public class ProductoHistorial
    {
        public string? Id { get; set; }

        
        [Required]
        public string ProductoId { get; set; } = null!;
        [Required]

        public string UsuarioId { get; set; } = null!;
        [Required]

        public string UsuarioNombre { get; set; } = null!;
      
        public string? ImagenUrl { get; set; }

        public string? CategoriaId { get; set; }
       

        public Categoria? Categoria { get; set; }
        [Required]

        public AccionProducto Accion { get; set; } // "Creado", "Editado", "Eliminado", "Stock aumentado"

        public string? DatosAntes { get; set; }
        public string? DatosDespues { get; set; }


        [Required]

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
