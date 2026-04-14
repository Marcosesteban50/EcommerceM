using EcommerceAPI.Data;
using EcommerceAPI.DTOs.AdminBoardDTO;
using EcommerceAPI.Modelos.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Controllers
{
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<AdminDashBoardDTO>> Dashboard()
        {
            var historial = _context.ProductoHistorial.AsQueryable();

            var dashboard = new AdminDashBoardDTO
            {
                ProductosCreados = await historial.CountAsync(x => x.Accion == AccionProducto.Creado),
                ProductosEditados = await historial.CountAsync(x => x.Accion == AccionProducto.Editado),
                ProductosEliminados = await historial.CountAsync(x => x.Accion == AccionProducto.Eliminado),
                ProductosAprobados = await historial.CountAsync(x => x.Accion == AccionProducto.Aprobado),
                ProductosRechazados = await historial.CountAsync(x => x.Accion == AccionProducto.Rechazado),

                OrdenesTotales = await _context.Ordenes.CountAsync(),
                //OrdenesPagadas = await _context.Ordenes.CountAsync(x => x.EstadoPago == EstadoPago.Pagado),
                //OrdenesPendientes = await _context.Ordenes.CountAsync(x => x.EstadoPago == EstadoPago.Pendiente)
            };

            return Ok(dashboard);
        }

        [HttpGet("OrdenesAdmin")]
        public async Task<ActionResult> TodasLasOrdenes()
        {
            var ordenes = await _context.Ordenes
                .Include(o => o.Items)
                .OrderByDescending(o => o.Fecha)
                .Select(o => new
                {
                    o.Id,
                    o.UsuarioId,
                    o.Fecha,
                    o.Total,
                    EstadoPago = o.EstadoPago.ToString(),
                    Items = o.Items.Count
                })
                .ToListAsync();

            return Ok(ordenes);
        }


        [HttpPut("ordenes/{ordenId}/estado")]
        public async Task<ActionResult> CambiarEstado(string ordenId, [FromBody] EstadoPago nuevoEstado)
        {
            var orden = await _context.Ordenes.FindAsync(ordenId);
            if (orden == null) return NotFound();

            orden.EstadoPago = nuevoEstado;
            await _context.SaveChangesAsync();

            return NoContent();
        }



    }
}
