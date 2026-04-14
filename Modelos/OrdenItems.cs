using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Modelos
{
    public class OrdenItems
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string ProductoId { get; set; } = null!;
        public Producto Producto { get; set; } = null!;

        [Required]
        public string NombreProducto { get; set; } = null!;

        [Required]
        public int Cantidad { get; set; }

        [Required]

        //precio Al momento para ser congelado y no cambiado si el precio sube
        //[Precision(18,2)]
        public decimal PrecioUnitario { get; set; }

        
        public string OrdenId { get; set; } = null!;
        public Orden Orden { get; set; } = null!;
    }
}
