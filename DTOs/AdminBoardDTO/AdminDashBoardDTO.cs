namespace EcommerceAPI.DTOs.AdminBoardDTO
{
    public class AdminDashBoardDTO
    {
        public int ProductosCreados { get; set; }
        public int ProductosEditados { get; set; }
        public int ProductosEliminados { get; set; }
        public int ProductosAprobados { get; set; }
        public int ProductosRechazados { get; set; }

        public int OrdenesTotales { get; set; }
        public int OrdenesPagadas { get; set; }
        public int OrdenesPendientes { get; set; }
    }
}
