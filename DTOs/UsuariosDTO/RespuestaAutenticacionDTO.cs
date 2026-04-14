namespace EcommerceAPI.DTOs.UsuariosDTO
{
    public class RespuestaAutenticacionDTO
    {
        public required string Token { get; set; }
        public DateTime Expiracion { get; set; }    
    }
}
