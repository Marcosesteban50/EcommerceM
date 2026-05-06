using EcommerceAPI.Data;
using EcommerceAPI.DTOs.AdminBoardDTO;
using EcommerceAPI.Modelos.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Controllers
{
    [ApiController]

    [Route("api/[controller]")]
    [Authorize] 


    public class AdminController : ControllerBase
    {
        // DbContext para consultar la base de datos
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Endpoint para obtener los datos principales del dashboard admin
        [HttpGet("dashboard")]
        public async Task<ActionResult<AdminDashBoardDTO>> Dashboard()
        {
            // Convertimos el historial en una consulta para poder hacer conteos
            var historial = _context.ProductoHistorial.AsQueryable();

            // Creamos el DTO del dashboard con los contadores que queremos mostrar
            var dashboard = new AdminDashBoardDTO
            {
                // Contamos cuántos productos fueron creados
                ProductosCreados = await historial.CountAsync(x => x.Accion == AccionProducto.Creado),

                // Contamos cuántos productos fueron editados
                ProductosEditados = await historial.CountAsync(x => x.Accion == AccionProducto.Editado),

                // Contamos cuántos productos fueron eliminados
                ProductosEliminados = await historial.CountAsync(x => x.Accion == AccionProducto.Eliminado),

                // Contamos cuántos productos fueron aprobados
                ProductosAprobados = await historial.CountAsync(x => x.Accion == AccionProducto.Aprobado),

                // Contamos cuántos productos fueron rechazados
                ProductosRechazados = await historial.CountAsync(x => x.Accion == AccionProducto.Rechazado),

                // Contamos todas las órdenes registradas
                OrdenesTotales = await _context.Ordenes.CountAsync(),

                // Esto queda comentado por si luego quiero contar órdenes por estado de pago
                // OrdenesPagadas = await _context.Ordenes.CountAsync(x => x.EstadoPago == EstadoPago.Pagado),
                // OrdenesPendientes = await _context.Ordenes.CountAsync(x => x.EstadoPago == EstadoPago.Pendiente)
            };

            // Retornamos la información del dashboard al frontend
            return Ok(dashboard);
        }

        // Endpoint para obtener todas las órdenes desde el panel admin
        [HttpGet("OrdenesAdmin")]
        public async Task<ActionResult> TodasLasOrdenes()
        {
            // Buscamos todas las órdenes, incluyendo sus items
            var ordenes = await _context.Ordenes
                .Include(o => o.Items)

                // Ordenamos las órdenes desde la más reciente hasta la más vieja
                .OrderByDescending(o => o.Fecha)

                // Seleccionamos solo los datos que quiero mandar al frontend
                .Select(o => new
                {
                    o.Id,
                    o.UsuarioId,
                    o.Fecha,
                    o.Total,

                    // Convertimos el estado de pago a texto para mostrarlo fácil en Angular
                    EstadoPago = o.EstadoPago.ToString(),

                    // Contamos cuántos items tiene esa orden
                    Items = o.Items.Count
                })
                .ToListAsync();

            // Retornamos la lista de órdenes
            return Ok(ordenes);
        }

        // Endpoint para cambiar el estado de pago de una orden
        [HttpPut("ordenes/{ordenId}/estado")]
        public async Task<ActionResult> CambiarEstado(string ordenId, [FromBody] EstadoPago nuevoEstado)
        {
            // Buscamos la orden por su id
            var orden = await _context.Ordenes.FindAsync(ordenId);

            // Si no existe, retornamos NotFound
            if (orden == null) return NotFound();

            // Cambiamos el estado de pago por el nuevo que llega desde el frontend
            orden.EstadoPago = nuevoEstado;

            // Guardamos el cambio en la base de datos
            await _context.SaveChangesAsync();

            // Retornamos NoContent porque salió bien pero no necesitamos devolver nada
            return NoContent();
        }
    }
}