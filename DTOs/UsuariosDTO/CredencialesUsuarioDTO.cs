using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.DTOs.UsuariosDTO
{
    public class CredencialesUsuarioDTO
    {
        [EmailAddress]
        [Required]
        public required string Email { get; set; }
        [Required]
        public required string Password { get; set; }

    }
}
