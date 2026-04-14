using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Modelos
{
    public class CarritoItems
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string ProductoId { get; set; } = null!;
        public Producto Producto { get; set; } = null!;

        [Required]
        public int Cantidad { get; set; }

        public string CarritoId { get; set; } = null!;
        public Carrito Carrito { get; set; } = null!;
    }
}
