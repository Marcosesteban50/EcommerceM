using EcommerceAPI.Modelos;

namespace EcommerceAPI.DTOs.CategoriasDTOs
{
    public class CategoriaDTO
    {

        public string? Id { get; set; }
        public string? Nombre { get; set; }
        public List<Producto>? Productos { get; set; }

    }
}
