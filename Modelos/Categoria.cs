using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Modelos
{
    public class Categoria
    {
        public  string Id { get; set; } = Guid.NewGuid().ToString();   

        [Required]
        public required string Nombre { get; set; }   

        public List<Producto>? Productos { get; set; }
    }
}
