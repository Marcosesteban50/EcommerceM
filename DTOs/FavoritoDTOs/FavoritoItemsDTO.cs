namespace EcommerceAPI.DTOs.FavoritoDTOs
{
    public class FavoritoItemsDTO
    {
        public string? ProductoId { get; set; }
        public string? NombreProducto { get; set; }
       
        public decimal Precio { get; set; }

        public string? ImagenUrl { get; set; }
    }
}
