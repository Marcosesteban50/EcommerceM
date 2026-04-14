using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.DTOs.GoogleDTO
{
    public class GoogleLoginRequestDTO
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }
}
