namespace EcommerceAPI.DTOs.CarritoDTOs
{
    public class CarritoDTO
    {
        public string Id { get; set; } = null!;
        public List<CarritoItemsDTO> Items { get; set; } = new List<CarritoItemsDTO>();
        public decimal Total => Items.Sum(x => x.Subtotal);


    }
}
