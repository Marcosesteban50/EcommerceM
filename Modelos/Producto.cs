using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Modelos
{
    public class Producto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string? Nombre { get; set; }

        public string? Descripcion {  get; set; }

        [Required]
        public decimal Precio { get; set; }

        public string? ImagenUrl { get; set; }   
        public string? CategoriaId { get; set; }
      


        public Categoria? Categoria { get; set; }

        public int Stock { get; set; }  

        public string? UsuarioId { get; set; }
         
        public IdentityUser? Usuario { get; set; }

        public bool Aprobado { get; set; } = false;
    }
}
