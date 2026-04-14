namespace EcommerceAPI.DTOs.GoogleDTO
{
    public class UserInfoDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Picture { get; set; } = string.Empty;
        public string? GoogleId { get; set; }
    }
}
