namespace EcommerceAPI.Modelos
{
    public class ImagenProducto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Url { get; set; } = string.Empty;

        public string ProductoId { get; set; } = string.Empty;

        public Producto Producto { get; set; } = null!;
    }
}
