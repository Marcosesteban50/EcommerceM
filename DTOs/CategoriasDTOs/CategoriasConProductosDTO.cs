namespace EcommerceAPI.DTOs.CategoriasDTOs
{
    public class CategoriasConProductosDTO
    {
        public string? Id { get; set; }
        public string? Nombre { get; set; }
        public List<ProductoSinCategoriaDTO>? Productos { get; set; }
    }


    public class ProductoSinCategoriaDTO
    {
        public string? Id { get; set; }
        public string? Nombre { get; set; }
        
    }
}
