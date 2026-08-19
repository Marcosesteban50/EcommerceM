using EcommerceAPI.DTOs.CarritoDTOs;
using EcommerceAPI.DTOs.ProductosDTOs;

namespace EcommerceAPI.DTOs.FavoritoDTOs
{
    public class FavoritoDTO
    {
        public string Id { get; set; } = null!;
        public List<FavoritoItemsDTO> Items { get; set; } = new List<FavoritoItemsDTO>();
    }
}
