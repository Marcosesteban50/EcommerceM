using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Modelos
{
    public class PerfilUsuario
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [Required]
        public string? UsuarioId { get; set; }

        public IdentityUser? Usuario { get; set; }

        public  string? Email { get; set; }

       
        [MaxLength(200)]
        public string? DireccionEnvio { get; set; }

        [MaxLength(20)]
        public string? Telefono { get; set; }

        [MaxLength(100)]
        public string? NombreCompleto { get; set; }
    }
}
