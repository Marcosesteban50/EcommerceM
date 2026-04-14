namespace EcommerceAPI.DTOs.OrdenDTOs
{
    public class OrdenItemDTO
    {
        public string ProductoId { get; set; } = null!;
        public string NombreProducto { get; set; } = null!;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    }

}
