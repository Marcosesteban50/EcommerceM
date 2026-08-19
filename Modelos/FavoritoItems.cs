using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Modelos
{
    public class FavoritoItems
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string ProductoId { get; set; } = null!;
        public Producto Producto { get; set; } = null!;
        public string FavoritoId { get; set; } = null!;
        public Favorito Favorito { get; set; } = null!;
    }
}
