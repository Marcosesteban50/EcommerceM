using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Modelos
{
    public class Carrito
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string UsuarioId { get; set; } = null!;
        public IdentityUser Usuario { get; set; } = null!;

        public List<CarritoItems> Items { get; set; } = new List<CarritoItems>();
    }
}
