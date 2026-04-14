namespace EcommerceAPI.DTOs.CarritoDTOs
{
    public class CarritoItemsDTO
    {
        public string? ProductoId { get; set; }
        public string? NombreProducto { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Subtotal => Cantidad * Precio;

        public string? ImagenUrl { get; set; }

    }
}
