using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.DTOs.UsuariosDTO
{
    public class PerfilUsuarioDTO
    {
      

        public string? DireccionEnvio { get; set; } 

        public string? Telefono { get; set; } 

        public string? NombreCompleto { get; set; } 
        public string? Email { get; set; }
    }
}
