using AutoMapper;
using EcommerceAPI.Data;
using EcommerceAPI.DTOs.CarritoDTOs;
using EcommerceAPI.Modelos;
using EcommerceAPI.Servicios.ServicioUsuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // el carrito es por usuario logueado
    public class CarritoController : ControllerBase
    {
        private readonly IMapper mapper;
        private readonly IServicioUsuarios servicioUsuarios;
        private readonly ApplicationDbContext dbContext;
        private readonly UserManager<IdentityUser> userManager;
        private readonly ILogger<CarritoController> logger;

        public CarritoController(IMapper mapper, IServicioUsuarios servicioUsuarios, ApplicationDbContext dbContext, UserManager<IdentityUser> userManager,
            ILogger<CarritoController> logger)
        {
            this.mapper = mapper;
            this.servicioUsuarios = servicioUsuarios;
            this.dbContext = dbContext;
            this.userManager = userManager;
            this.logger = logger;
        }



        // Obtener el carrito actual del usuario
        [HttpGet]
        public async Task<ActionResult<CarritoDTO>> ObtenerCarrito()
        {
            var usuario = await servicioUsuarios.ObtenerUsuarioId();
            if (usuario == null) return Unauthorized();

            var carrito = await dbContext.Carritos
                .Include(c => c.Items)
                .ThenInclude(i => i.Producto)
                .FirstOrDefaultAsync(c => c.UsuarioId == usuario);

            if (carrito == null)
            {
                return Ok(new CarritoDTO());
            }

            var dto = mapper.Map<CarritoDTO>(carrito);

            return Ok(dto);
        }

        // Agregar item al carrito
        [HttpPost("AgregarItem")]
        public async Task<ActionResult> AgregarItem(CarritoAgregarItemsDTO dto)
        {


            //logger.LogInformation("📥 Llegó al endpoint AgregarItem: {@dto}", dto);



            var usuario = await servicioUsuarios.ObtenerUsuarioId();
            //logger.LogInformation("🧑 Usuario ID detectado: {usuario}", usuario);

            if (usuario == null) return Unauthorized();

            var producto = await dbContext.Productos.FirstOrDefaultAsync(p => p.Id == dto.ProductoId);

            //No existe 
            if (producto == null)
            {
                return NotFound(new { mensaje = $"No hay {producto!.Nombre}" });
            }

            //No hay productos
            if (producto.Stock == 0)
            {
                return BadRequest(new { mensaje = "No tenemos suficientes" });
            }

            //No hay suficientes
            if (dto.Cantidad > producto.Stock)
            {
                return BadRequest(new
                {
                    mensaje = $"No tenemos {dto.Cantidad} unidades disponibles"
                });

            }

            //Reducimos cuando se confirma la compra mejor
            //producto.Stock -= dto.Cantidad;


            var carrito = await dbContext.Carritos
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UsuarioId == usuario);

            if (carrito == null)
            {
                //Creamos un nuevo carrito si no hay
                carrito = new Carrito { UsuarioId = usuario };
                dbContext.Carritos.Add(carrito);
            }

            var item = carrito.Items.FirstOrDefault(i => i.ProductoId == dto.ProductoId);

            if (item == null)
            {
                carrito.Items.Add(new CarritoItems
                {
                    ProductoId = dto.ProductoId,
                    Cantidad = dto.Cantidad
                });
            }
            else
            {
                item.Cantidad += dto.Cantidad;
            }

            await dbContext.SaveChangesAsync();
            return NoContent();
        }


        [HttpDelete("EliminarUno/{id}")]
        public async Task<ActionResult> EliminarUnaUnidad(string id)
        {
            var usuario = await servicioUsuarios.ObtenerUsuarioId();
            if (usuario == null) return Unauthorized(new { mensaje = "No estas autorizado" });

            var carrito = await dbContext.Carritos
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UsuarioId == usuario);

            if (carrito == null) return NotFound(new { mensaje = "Carrito vacío" });

            var item = carrito.Items.FirstOrDefault(i => i.ProductoId == id);
            if (item == null) return NotFound(new { mensaje = "Producto no está en el carrito" });

            // Si tiene más de 1, restamos 1
            if (item.Cantidad > 1)
            {
                item.Cantidad--;
            }
            else
            {
                // Si solo queda 1, lo quitamos completo
                carrito.Items.Remove(item);
            }

            await dbContext.SaveChangesAsync();
            return NoContent();
        }





        // Eliminar item del carrito
        [HttpDelete("Eliminar/{productoId}")]
        public async Task<ActionResult> EliminarItem(string productoId)
        {
            var usuario = await servicioUsuarios.ObtenerUsuarioId();

            if (usuario == null) return Unauthorized();

            var carrito = await dbContext.Carritos
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UsuarioId == usuario);

            if (carrito == null) return NotFound("Carrito vacío");

            var item = carrito.Items.FirstOrDefault(i => i.ProductoId == productoId);
            if (item == null) return NotFound("Producto no está en el carrito");

            carrito.Items.Remove(item);
            await dbContext.SaveChangesAsync();

            return NoContent();
        }

        // Vaciar carrito
        [HttpDelete("Vaciar")]
        public async Task<ActionResult> VaciarCarrito()
        {
            var usuario = await servicioUsuarios.ObtenerUsuarioId();

            if (usuario == null) return Unauthorized();

            var carrito = await dbContext.Carritos
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UsuarioId == usuario);

            if (carrito == null) return NoContent();

            carrito.Items.Clear();
            await dbContext.SaveChangesAsync();

            return NoContent();
        }
    }
}
