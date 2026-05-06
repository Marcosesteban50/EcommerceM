using AutoMapper;
using EcommerceAPI.Data;
using EcommerceAPI.DTOs.CarritoDTOs;
using EcommerceAPI.Modelos;
using EcommerceAPI.Servicios.ServicioUsuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Solo usuarios logueados pueden usar el carrito
    public class CarritoController : ControllerBase
    {
        // Mapper para convertir entidades a DTOs
        private readonly IMapper mapper;

        // Servicio que uso para obtener el usuario logueado
        private readonly IServicioUsuarios servicioUsuarios;

        // DbContext para trabajar con la base de datos
        private readonly ApplicationDbContext dbContext;

        // UserManager por si necesito trabajar con usuarios de Identity
        private readonly UserManager<IdentityUser> userManager;

        // Logger para hacer pruebas o ver errores en consola
        private readonly ILogger<CarritoController> logger;

        public CarritoController(
            IMapper mapper,
            IServicioUsuarios servicioUsuarios,
            ApplicationDbContext dbContext,
            UserManager<IdentityUser> userManager,
            ILogger<CarritoController> logger)
        {
            this.mapper = mapper;
            this.servicioUsuarios = servicioUsuarios;
            this.dbContext = dbContext;
            this.userManager = userManager;
            this.logger = logger;
        }

        // Obtiene el carrito del usuario que está logueado
        [HttpGet]
        public async Task<ActionResult<CarritoDTO>> ObtenerCarrito()
        {
            // Obtenemos el id del usuario desde el token
            var usuario = await servicioUsuarios.ObtenerUsuarioId();

            // Si no hay usuario, significa que no está autorizado
            if (usuario == null) return Unauthorized();

            // Buscamos el carrito del usuario
            // Incluimos los items y también el producto de cada item
            var carrito = await dbContext.Carritos
                .Include(c => c.Items)
                .ThenInclude(i => i.Producto)
                .FirstOrDefaultAsync(c => c.UsuarioId == usuario);

            // Si el usuario no tiene carrito todavía, devolvemos un carrito vacío
            if (carrito == null)
            {
                return Ok(new CarritoDTO());
            }

            // Convertimos el carrito de entidad a DTO para mandarlo al frontend
            var dto = mapper.Map<CarritoDTO>(carrito);

            // Retornamos el carrito ya listo
            return Ok(dto);
        }

        // Agrega un producto al carrito del usuario logueado
        [HttpPost("AgregarItem")]
        public async Task<ActionResult> AgregarItem(CarritoAgregarItemsDTO dto)
        {
            // Esto lo puedo usar para depurar si quiero ver qué llega desde Angular
            // logger.LogInformation("Llegó al endpoint AgregarItem: {@dto}", dto);

            // Obtenemos el usuario logueado
            var usuario = await servicioUsuarios.ObtenerUsuarioId();

            // logger.LogInformation("Usuario ID detectado: {usuario}", usuario);

            // Si no hay usuario, no puede agregar al carrito
            if (usuario == null) return Unauthorized();

            // Buscamos el producto que el usuario quiere agregar
            var producto = await dbContext.Productos
                .FirstOrDefaultAsync(p => p.Id == dto.ProductoId);

            // Si el producto no existe, retornamos error
            if (producto == null)
            {
                return NotFound(new { mensaje = "Producto no encontrado" });
            }

            // Si el producto tiene stock 0, no dejamos agregarlo
            if (producto.Stock == 0)
            {
                return BadRequest(new { mensaje = "No tenemos suficientes" });
            }

            // Si el usuario quiere más cantidad de la disponible, retornamos error
            if (dto.Cantidad > producto.Stock)
            {
                return BadRequest(new
                {
                    mensaje = $"No tenemos {dto.Cantidad} unidades disponibles"
                });
            }

            // OJO: no reducimos el stock aquí
            // Es mejor reducirlo cuando se confirme la compra
            // producto.Stock -= dto.Cantidad;

            // Buscamos si el usuario ya tiene un carrito creado
            var carrito = await dbContext.Carritos
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UsuarioId == usuario);

            // Si no tiene carrito, se lo creamos en este momento
            if (carrito == null)
            {
                carrito = new Carrito
                {
                    UsuarioId = usuario
                };

                dbContext.Carritos.Add(carrito);
            }

            // Buscamos si ese producto ya está dentro del carrito
            var item = carrito.Items
                .FirstOrDefault(i => i.ProductoId == dto.ProductoId);

            // Si el producto no está en el carrito, lo agregamos como nuevo
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
                // Si ya existe, solo aumentamos la cantidad
                item.Cantidad += dto.Cantidad;
            }

            // Guardamos los cambios en la base de datos
            await dbContext.SaveChangesAsync();

            // NoContent porque todo salió bien pero no necesitamos devolver data
            return NoContent();
        }

        // Elimina una sola unidad de un producto del carrito
        [HttpDelete("EliminarUno/{id}")]
        public async Task<ActionResult> EliminarUnaUnidad(string id)
        {
            // Obtenemos el usuario logueado
            var usuario = await servicioUsuarios.ObtenerUsuarioId();

            // Si no hay usuario, retornamos unauthorized
            if (usuario == null)
            {
                return Unauthorized(new { mensaje = "No estas autorizado" });
            }

            // Buscamos el carrito del usuario con sus items
            var carrito = await dbContext.Carritos
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UsuarioId == usuario);

            // Si no tiene carrito, no hay nada que eliminar
            if (carrito == null)
            {
                return NotFound(new { mensaje = "Carrito vacío" });
            }

            // Buscamos el producto dentro del carrito
            var item = carrito.Items.FirstOrDefault(i => i.ProductoId == id);

            // Si no existe ese producto en el carrito, retornamos error
            if (item == null)
            {
                return NotFound(new { mensaje = "Producto no está en el carrito" });
            }

            // Si tiene más de 1 unidad, solo le restamos una
            if (item.Cantidad > 1)
            {
                item.Cantidad--;
            }
            else
            {
                // Si solo queda 1, lo quitamos completo del carrito
                carrito.Items.Remove(item);
            }

            // Guardamos los cambios
            await dbContext.SaveChangesAsync();

            return NoContent();
        }

        // Elimina un producto completo del carrito sin importar la cantidad
        [HttpDelete("Eliminar/{productoId}")]
        public async Task<ActionResult> EliminarItem(string productoId)
        {
            // Obtenemos el usuario logueado
            var usuario = await servicioUsuarios.ObtenerUsuarioId();

            // Si no hay usuario, no puede eliminar
            if (usuario == null) return Unauthorized();

            // Buscamos el carrito del usuario con sus items
            var carrito = await dbContext.Carritos
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UsuarioId == usuario);

            // Si no tiene carrito, retornamos error
            if (carrito == null) return NotFound("Carrito vacío");

            // Buscamos el producto dentro del carrito
            var item = carrito.Items
                .FirstOrDefault(i => i.ProductoId == productoId);

            // Si el producto no está en el carrito, retornamos error
            if (item == null) return NotFound("Producto no está en el carrito");

            // Eliminamos el producto completo del carrito
            carrito.Items.Remove(item);

            // Guardamos cambios
            await dbContext.SaveChangesAsync();

            return NoContent();
        }

        // Vacía completamente el carrito del usuario
        [HttpDelete("Vaciar")]
        public async Task<ActionResult> VaciarCarrito()
        {
            // Obtenemos el usuario logueado
            var usuario = await servicioUsuarios.ObtenerUsuarioId();

            // Si no hay usuario, retornamos unauthorized
            if (usuario == null) return Unauthorized();

            // Buscamos el carrito con sus items
            var carrito = await dbContext.Carritos
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UsuarioId == usuario);

            // Si no hay carrito, no pasa nada porque ya está vacío
            if (carrito == null) return NoContent();

            // Eliminamos todos los items del carrito
            carrito.Items.Clear();

            // Guardamos los cambios
            await dbContext.SaveChangesAsync();

            return NoContent();
        }

        // Sincroniza el carrito invitado del localStorage con el carrito real del usuario
        [HttpPost("Sincronizar")]
        public async Task<ActionResult> Sincronizar([FromBody] List<SincronizarCarritoDTO> items)
        {
            // Obtenemos el usuario logueado desde el token
            var usuarioId = await servicioUsuarios.ObtenerUsuarioId();

            // Si no hay usuario, no dejamos sincronizar
            if (usuarioId is null)
            {
                return Unauthorized();
            }

            // Buscamos el carrito del usuario
            // Incluimos los items para comparar con los productos del localStorage
            var carrito = await dbContext.Carritos
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.UsuarioId == usuarioId);

            // Si el usuario todavía no tiene carrito, lo creamos
            if (carrito is null)
            {
                carrito = new Carrito
                {
                    UsuarioId = usuarioId,
                    Items = new List<CarritoItems>()
                };

                dbContext.Carritos.Add(carrito);
            }

            // Recorremos los productos que vienen desde el carrito invitado
            foreach (var item in items)
            {
                // Buscamos si este producto ya existe en el carrito del usuario
                var itemExistente = carrito.Items
                    .FirstOrDefault(x => x.ProductoId == item.ProductoId);

                // Si ya existe, sumamos la cantidad del localStorage
                if (itemExistente is not null)
                {
                    itemExistente.Cantidad += item.Cantidad;
                }
                else
                {
                    // Si no existe, lo agregamos como nuevo producto al carrito
                    carrito.Items.Add(new CarritoItems
                    {
                        ProductoId = item.ProductoId,
                        Cantidad = item.Cantidad
                    });
                }
            }

            // Guardamos la sincronización en la base de datos
            await dbContext.SaveChangesAsync();

            // Retornamos OK para decirle al frontend que todo salió bien
            return Ok();
        }
    }
}