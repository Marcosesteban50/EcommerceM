using AutoMapper;
using EcommerceAPI.Data;
using EcommerceAPI.DTOs.CarritoDTOs;
using EcommerceAPI.DTOs.FavoritoDTOs;
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
    [Authorize]
    public class FavoritoController : ControllerBase
    {
        private readonly IMapper mapper;
        private readonly IServicioUsuarios servicioUsuarios;
        private readonly ApplicationDbContext dbContext;
        private readonly UserManager<IdentityUser> userManager;
        private readonly ILogger<FavoritoController> logger;

        public FavoritoController(IMapper mapper,
            IServicioUsuarios servicioUsuarios,
            ApplicationDbContext dbContext,
            UserManager<IdentityUser> userManager,
            ILogger<FavoritoController> logger)
        {
            this.mapper = mapper;
            this.servicioUsuarios = servicioUsuarios;
            this.dbContext = dbContext;
            this.userManager = userManager;
            this.logger = logger;
        }

        // Obtiene todos los favoritos del usuario logueado
        [HttpGet]
        public async Task<ActionResult<FavoritoDTO>> ObtenerFavoritos()
        {
            // Obtenemos el id del usuario desde el token
            var usuario = await servicioUsuarios.ObtenerUsuarioId();

            // Si no hay usuario, significa que no está autorizado
            if (usuario == null) return Unauthorized();

            // Buscamos todos los favoritos del usuario
            // Incluimos el producto para tener toda la información
            var lista = await dbContext.Favoritos
                .Include(f => f.Items)
                .ThenInclude(x => x.Producto)
                .ThenInclude(x => x.Imagenes)
                .FirstOrDefaultAsync(x => x.UsuarioId == usuario);

            // Si el usuario no tiene una lista todavía, devolvemos una lista vacía
            if (lista == null)
            {
                return Ok(new FavoritoDTO());
            }



            // Convertimos los favoritos a DTO para mandarlos al frontend
            var dto = mapper.Map<FavoritoDTO>(lista);

            // Retornamos la lista de favoritos
            return Ok(dto);
        }



        // Agrega un producto al carrito del usuario logueado
        [HttpPost("AgregarItem")]
        public async Task<ActionResult> AgregarItem(FavoritoAgregarItemsDTO dto)
        {
            // Esto lo puedo usar para depurar si quiero ver qué llega desde Angular
            // logger.LogInformation("Llegó al endpoint AgregarItem: {@dto}", dto);

            // Obtenemos el usuario logueado
            var usuario = await servicioUsuarios.ObtenerUsuarioId();

            // logger.LogInformation("Usuario ID detectado: {usuario}", usuario);

            // Si no hay usuario, no puede agregar al carrito
            if (usuario == null) return Unauthorized();

            // Buscamos el producto que el usuario quiere agregar a favoritos
            var producto = await dbContext.Productos
                .FirstOrDefaultAsync(p => p.Id == dto.ProductoId);

            // Si el producto no existe, retornamos error
            if (producto == null)
            {
                return NotFound(new { mensaje = "Producto no encontrado" });
            }

         


            // Buscamos si el usuario ya tiene una lista de favoritos creada
            var ListaFavorito = await dbContext.Favoritos
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UsuarioId == usuario);

            // Si no tiene carrito, se lo creamos en este momento
            if (ListaFavorito == null)
            {
                ListaFavorito = new Favorito
                {
                    UsuarioId = usuario
                };

                dbContext.Favoritos.Add(ListaFavorito);
            }

            // Buscamos si ese producto ya está dentro de la lista de favoritos
            var item = ListaFavorito!.Items
                .FirstOrDefault(i => i.ProductoId == dto.ProductoId);

            // Si el producto no está en la lista, lo agregamos como nuevo
            if (item == null)
            {
                ListaFavorito.Items.Add(new FavoritoItems
                {
                    ProductoId = dto.ProductoId
                 
                });
            }
           

            // Guardamos los cambios en la base de datos
            await dbContext.SaveChangesAsync();

            // NoContent porque todo salió bien pero no necesitamos devolver data
            return NoContent();
        }
        // Elimina un producto completo del carrito sin importar la cantidad
        [HttpDelete("Eliminar/{favoritoId}")]
        public async Task<ActionResult> EliminarItem(string favoritoId)
        {
            // Obtenemos el usuario logueado
            var usuario = await servicioUsuarios.ObtenerUsuarioId();

            // Si no hay usuario, no puede eliminar
            if (usuario == null) return Unauthorized();

            // Buscamos el carrito del usuario con sus items
            var lista = await dbContext.Favoritos
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UsuarioId == usuario);

            // Si no tiene lista, retornamos error
            if (lista == null) return NotFound("Lista vacía");

            // Buscamos el producto dentro de la lista
            var item = lista.Items
                .FirstOrDefault(i => i.ProductoId == favoritoId);

           

            // Si el producto no está en la lista, retornamos error
            if (item == null) return NotFound("Producto no está en la lista");

            // Eliminamos el producto completo del carrito
            lista.Items.Remove(item);

            // Guardamos cambios
            await dbContext.SaveChangesAsync();

            return NoContent();
        }

        // Vacía completamente el carrito del usuario
        [HttpDelete("Vaciar")]
        public async Task<ActionResult> LimpiarLista()
        {
            // Obtenemos el usuario logueado
            var usuario = await servicioUsuarios.ObtenerUsuarioId();

            // Si no hay usuario, retornamos unauthorized
            if (usuario == null) return Unauthorized();

            // Buscamos la lista con sus items
            var lista = await dbContext.Favoritos
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UsuarioId == usuario);

            // Si no hay lista, no pasa nada porque ya está vacío
            if (lista == null) return NoContent();

            // Eliminamos todos los items de la lista
            lista.Items.Clear();

            // Guardamos los cambios
            await dbContext.SaveChangesAsync();

            return NoContent();
        }


    }
}