namespace EcommerceAPI.DTOs.UsuariosDTO
{
    public class UsuarioDTO
    {
        public required string Email { get; set; }
        public List<string>? Roles { get; set; }
    }
}
