using EcommerceAPI.DTOs.UsuariosDTO;

namespace EcommerceAPI.DTOs.GoogleDTO
{
    public class RespuestaAutenticacionCompletaDTO : RespuestaAutenticacionDTO
    {
        public UserInfoDTO Usuario { get; set; } = new();
    }
}
