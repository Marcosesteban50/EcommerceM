using AutoMapper.QueryableExtensions;
using AutoMapper;
using EcommerceAPI.Data;
using EcommerceAPI.DTOs.CategoriasDTOs;
using EcommerceAPI.DTOs.ProductosDTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using EcommerceAPI.Modelos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using EcommerceAPI.Servicios.ServicioUsuarios;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;
using EcommerceAPI.Servicios.Archivos;
using EcommerceAPI.DTOs.ProductosDTOs.EcommerceAPI.DTOs;
using EcommerceAPI.Modelos.Enums;

namespace EcommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "Admin")]
    public class ProductoController : ControllerBase
    {
        private readonly ApplicationDbContext dbContext;
        private readonly IMapper mapper;
        private readonly IOutputCacheStore outputCacheStore;
        private readonly IServicioUsuarios servicioUsuarios;
        private readonly UserManager<IdentityUser> userManager;
        private readonly IAlmacenadorArchivos almacenadorArchivos;

        // Tag que uso para limpiar la cache de productos cuando hago cambios
        private const string cacheTag = "productos";

        // Nombre del contenedor donde se guardan las imágenes de los productos
        private readonly string contenedor = "productos";

        public ProductoController(
            ApplicationDbContext dbContext,
            IMapper mapper,
            IOutputCacheStore outputCacheStore,
            IServicioUsuarios servicioUsuarios,
            UserManager<IdentityUser> userManager,
            IAlmacenadorArchivos almacenadorArchivos)
        {
            this.dbContext = dbContext;
            this.mapper = mapper;
            this.outputCacheStore = outputCacheStore;
            this.servicioUsuarios = servicioUsuarios;
            this.userManager = userManager;
            this.almacenadorArchivos = almacenadorArchivos;
        }

        // ------------------- GET: OBTENER PRODUCTOS APROBADOS -------------------

        [HttpGet("ObtenerProductos")]
        [OutputCache(Tags = [cacheTag])]
        [AllowAnonymous]
        public async Task<ActionResult<List<ProductoDTO>>> ObtenerProductos()
        {
            // Traigo solamente los productos aprobados para mostrarlos al público
            var productos = await dbContext.Productos
                .Where(x => x.Aprobado)
                .ProjectTo<ProductoDTO>(mapper.ConfigurationProvider)
                .ToListAsync();

            // Si no hay productos, devuelvo un mensaje
            if (productos.Count == 0)
                return NotFound(new { mensaje = "No hay productos" });

            return Ok(productos);
        }

        // ------------------- GET: HISTORIAL POR ID -------------------

        [HttpGet("{Id}/historial")]
        [AllowAnonymous]
        public async Task<ActionResult<ProductoHistorial>> ObtenerHistorial(string Id)
        {
            // Busco un registro del historial por su id
            var productos = await dbContext.ProductoHistorial
                .Include(x => x.Categoria)
                .ProjectTo<ProductoHistorialDTO>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(x => x.Id == Id);

            Console.WriteLine($"Id recibido: {Id}");

            if (productos == null)
                return NotFound(new { mensaje = $"No se encontro {Id}" });

            return Ok(productos);
        }

        // ------------------- GET: PRODUCTO POR ID -------------------

        [HttpGet("{id}")]
        [OutputCache(Tags = [cacheTag])]
        [AllowAnonymous]
        public async Task<ActionResult<ProductoDTO>> Get(string id)
        {
            // Busco un producto aprobado por su id
            var producto = await dbContext.Productos
                .Where(x => x.Aprobado)
                .ProjectTo<ProductoDTO>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (producto == null)
                return NotFound(new { mensaje = $"No se encontró -> {id}" });

            return producto;
        }

        // ------------------- GET: TODO EL HISTORIAL DE PRODUCTOS -------------------

        [HttpGet("HistorialProductos")]
        [AllowAnonymous]
        public async Task<ActionResult<List<ProductoHistorial>>> HistorialTodosProductos()
        {
            // Traigo todo el historial de productos ordenado desde el más reciente
            var historial = await dbContext.ProductoHistorial
                .Include(x => x.Categoria)
                .ProjectTo<ProductoHistorialDTO>(mapper.ConfigurationProvider)
                .OrderByDescending(x => x.FechaCreacion)
                .ToListAsync();

            if (historial.Count == 0)
                return NotFound(new { mensaje = "No se encontro historial" });

            return Ok(historial);
        }

        // ------------------- POST: CREAR PRODUCTO -------------------

        [HttpPost]
        [OutputCache(Tags = [cacheTag])]
        public async Task<ActionResult> Post([FromForm] ProductoCreacionDTO productoCreacionDTO)
        {
            // Obtengo el usuario que está creando el producto
            var usuarioId = await servicioUsuarios.ObtenerUsuarioId();

            if (usuarioId == null)
                return Unauthorized(new { mensaje = "No está autorizado" });

            // Valido que el formulario venga correcto
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Convierto el DTO a la entidad Producto
            var producto = mapper.Map<Producto>(productoCreacionDTO);

            // Si viene una imagen, la guardo en el contenedor de productos
            if (productoCreacionDTO.ImagenUrl is not null)
            {
                var url = await almacenadorArchivos.Almacenar(contenedor, productoCreacionDTO.ImagenUrl);
                producto.ImagenUrl = url;
            }

            // El producto se crea pendiente de aprobación
            producto.Aprobado = false;

            // Guardo quién creó el producto
            producto.UsuarioId = usuarioId;

            dbContext.Add(producto);
            await dbContext.SaveChangesAsync();

            // Busco el usuario para guardar su nombre en el historial
            var usuario = await userManager.FindByIdAsync(usuarioId);

            // Registro en historial que el producto fue creado
            await RegistrarLog(
                productoAntes: null,
                productoDespues: producto,
                usuarioId: usuarioId,
                usuarioNombre: usuario?.UserName ?? usuario?.Email ?? "Desconocido",
                accion: AccionProducto.Creado
            );

            return Ok(new { mensaje = "Producto enviado para aprobación" });
        }

        // ------------------- PUT: AGREGAR STOCK -------------------

        [HttpPut("AgregarStock")]
        public async Task<ActionResult> AgregarStock(string id, [FromBody] AgregarMasProductosDTO agregarMasProductosDTO)
        {
            // Obtengo el usuario logueado
            var usuarioId = await servicioUsuarios.ObtenerUsuarioId();

            if (usuarioId == null)
                return Unauthorized(new { mensaje = "No está autorizado" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Busco el producto al que le voy a aumentar el stock
            var producto = await dbContext.Productos.FirstOrDefaultAsync(x => x.Id == id);

            if (producto == null)
                return NotFound();

            // Guardo cómo estaba el producto antes del cambio
            var productoAntes = new Producto
            {
                Id = producto.Id,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                Precio = producto.Precio,
                Stock = producto.Stock
            };

            // Aumento el stock
            producto.Stock += agregarMasProductosDTO.Stock;

            // Guardo quién hizo el cambio
            producto.UsuarioId = usuarioId;

            await dbContext.SaveChangesAsync();

            var usuario = await userManager.FindByIdAsync(usuarioId);

            // Registro en historial que se aumentó el stock
            await RegistrarLog(
                productoAntes,
                producto,
                usuarioId,
                usuario?.UserName ?? usuario?.Email ?? "Desconocido",
                accion: AccionProducto.StockAumentado
            );

            return NoContent();
        }

        // ------------------- PUT: EDITAR PRODUCTO -------------------

        [HttpPut("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult> Put(string id, [FromForm] ProductoCreacionDTO productoCreacionDTO)
        {
            // Obtengo el usuario que está editando
            var usuarioId = await servicioUsuarios.ObtenerUsuarioId();

            if (usuarioId == null)
                return Unauthorized(new { mensaje = "No está autorizado" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Busco el producto con su categoría para poder guardar bien el historial
            var producto = await dbContext.Productos
                .Include(x => x.Categoria)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (producto == null)
                return NotFound(new { mensaje = "No se encontro el producto" });

            // Guardo el estado anterior del producto antes de modificarlo
            var productoAntes = new Producto
            {
                Id = producto.Id,
                Nombre = producto.Nombre,
                Categoria = producto.Categoria,
                CategoriaId = producto.CategoriaId,
                ImagenUrl = producto.ImagenUrl,
                Descripcion = producto.Descripcion,
                Precio = producto.Precio,
                Stock = producto.Stock
            };

            // Guardo quién editó el producto
            producto.UsuarioId = usuarioId;

            // Paso los datos nuevos del DTO al producto
            mapper.Map(productoCreacionDTO, producto);

            // Si el producto tiene imagen, la edito/reemplazo
            if (producto.ImagenUrl != null)
            {
                producto.ImagenUrl = await almacenadorArchivos.Editar(
                    producto.ImagenUrl,
                    contenedor,
                    productoCreacionDTO.ImagenUrl!
                );
            }

            await dbContext.SaveChangesAsync();

            var usuario = await userManager.FindByIdAsync(usuarioId);

            // Registro en historial los datos antes y después
            await RegistrarLog(
                productoAntes,
                producto,
                usuarioId,
                usuario?.UserName ?? usuario?.Email ?? "Desconocido",
                accion: AccionProducto.Editado
            );

            return NoContent();
        }

        // ------------------- DELETE: ELIMINAR PRODUCTO -------------------

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(string id)
        {
            // Obtengo el usuario que quiere eliminar
            var usuarioId = await servicioUsuarios.ObtenerUsuarioId();

            if (usuarioId == null)
                return Unauthorized(new { mensaje = "No está autorizado" });

            // Busco el producto
            var producto = await dbContext.Productos.FirstOrDefaultAsync(x => x.Id == id);

            if (producto == null)
                return NotFound($"El producto con ID {id} no existe");

            var usuario = await userManager.FindByIdAsync(usuarioId);

            // Guardo quién eliminó el producto
            producto.UsuarioId = usuarioId;

            // Elimino el producto
            dbContext.Productos.Remove(producto);
            await dbContext.SaveChangesAsync();

            // Registro en historial que el producto fue eliminado
            await RegistrarLog(
                producto,
                productoDespues: null,
                usuarioId,
                usuario?.UserName ?? usuario?.Email ?? "Desconocido",
                accion: AccionProducto.Eliminado
            );

            // Limpio la cache de productos
            await outputCacheStore.EvictByTagAsync(cacheTag, default);

            return NoContent();
        }

        // ------------------- PUT: APROBAR PRODUCTO -------------------

        [HttpPut("aprobar/{id}")]
        public async Task<ActionResult> AprobarProducto(string id)
        {
            // Obtengo el usuario que está aprobando
            var usuarioId = await servicioUsuarios.ObtenerUsuarioId();
            var usuario = await userManager.FindByIdAsync(usuarioId);

            // Busco el producto pendiente
            var producto = await dbContext.Productos.FindAsync(id);

            if (producto == null)
                return NotFound(new { mensaje = "Producto no encontrado" });

            // Marco el producto como aprobado
            producto.Aprobado = true;
            producto.UsuarioId = usuarioId;

            await dbContext.SaveChangesAsync();

            // Registro en historial que fue aprobado
            await RegistrarLog(
                producto,
                producto,
                usuarioId,
                usuario?.UserName ?? usuario?.Email ?? "Desconocido",
                accion: AccionProducto.Aprobado
            );

            return Ok(new { mensaje = "Producto Aprobado" });
        }

        // ------------------- PUT: RECHAZAR PRODUCTO -------------------

        [HttpPut("rechazar/{id}")]
        public async Task<ActionResult> RechazarProducto(string id)
        {
            // Obtengo el usuario que está rechazando
            var usuarioId = await servicioUsuarios.ObtenerUsuarioId();
            var usuario = await userManager.FindByIdAsync(usuarioId);

            // Busco el producto
            var producto = await dbContext.Productos.FindAsync(id);

            if (producto == null)
                return NotFound(new { mensaje = "Producto no encontrado" });

            // Guardo quién hizo el rechazo
            producto.UsuarioId = usuarioId;

            await dbContext.SaveChangesAsync();

            // Registro en historial que el producto fue rechazado
            await RegistrarLog(
                producto,
                producto,
                usuarioId,
                usuario?.UserName ?? usuario?.Email ?? "Desconocido",
                accion: AccionProducto.Rechazado
            );

            // Luego de rechazarlo, elimino el producto
            dbContext.Productos.Remove(producto);
            await dbContext.SaveChangesAsync();

            return Ok(new { mensaje = "Producto Rechazado" });
        }

        // ------------------- GET: PRODUCTOS PENDIENTES -------------------

        [HttpGet("Pendientes")]
        public async Task<ActionResult<List<ProductoDTO>>> ObtenerPendientes()
        {
            // Traigo todos los productos que todavía no han sido aprobados
            var pendientes = await dbContext.Productos
                .Where(p => !p.Aprobado)
                .ProjectTo<ProductoDTO>(mapper.ConfigurationProvider)
                .ToListAsync();

            return Ok(pendientes);
        }

        // ------------------- GET: FILTRAR PRODUCTOS -------------------

        [HttpGet("Filtrar")]
        [AllowAnonymous]
        public async Task<ActionResult<List<ProductoDTO>>> Filtrar([FromQuery] ProductoFiltrarDTO productoFiltrarDTO)
        {
            // Empiezo mostrando solamente productos aprobados y con stock
            var productosQueryable = dbContext.Productos
                .Where(p => p.Aprobado && p.Stock > 0)
                .AsQueryable();

            // Filtro por nombre si el usuario escribió algo
            if (!string.IsNullOrWhiteSpace(productoFiltrarDTO.Nombre))
            {
                productosQueryable = productosQueryable
                    .Where(x => x.Nombre!.Contains(productoFiltrarDTO.Nombre));
            }

            // Filtro por categoría si se seleccionó una
            if (!string.IsNullOrWhiteSpace(productoFiltrarDTO.CategoriaId))
            {
                productosQueryable = productosQueryable
                    .Where(x => x.CategoriaId == productoFiltrarDTO.CategoriaId);
            }

            // Filtro por precio mínimo
            if (productoFiltrarDTO.PrecioMin != null && productoFiltrarDTO.PrecioMin > 0)
            {
                productosQueryable = productosQueryable
                    .Where(x => x.Precio >= productoFiltrarDTO.PrecioMin);
            }

            // Filtro por precio máximo
            if (productoFiltrarDTO.PrecioMax != null && productoFiltrarDTO.PrecioMax > 0)
            {
                productosQueryable = productosQueryable
                    .Where(x => x.Precio <= productoFiltrarDTO.PrecioMax);
            }

            // Ejecuto la consulta
            var productos = await productosQueryable.ToListAsync();

            // Convierto los productos a DTO
            var productoDTO = mapper.Map<List<ProductoDTO>>(productos);

            return productoDTO;
        }

        // ------------------- GET: FILTRAR PRODUCTOS LANDING -------------------

        [HttpGet("FiltrarLanding")]
        [AllowAnonymous]
        public async Task<ActionResult<List<ProductoDTO>>> FiltrarLanding([FromQuery] ProductoFiltrarDTO productoFiltrarDTO)
        {
            // Empiezo con productos aprobados y con stock
            var productosQueryable = dbContext.Productos
                .Where(p => p.Aprobado && p.Stock > 0)
                .AsQueryable();

            // Filtro por nombre
            if (!string.IsNullOrWhiteSpace(productoFiltrarDTO.Nombre))
            {
                productosQueryable = productosQueryable
                    .Where(x => x.Nombre!.Contains(productoFiltrarDTO.Nombre));
            }

            // Filtro por categoría
            if (!string.IsNullOrWhiteSpace(productoFiltrarDTO.CategoriaId))
            {
                productosQueryable = productosQueryable
                    .Where(x => x.CategoriaId == productoFiltrarDTO.CategoriaId);
            }

            // Ordeno de menor a mayor precio
            if (productoFiltrarDTO.PrecioMinBoolean != false)
            {
                productosQueryable = productosQueryable.OrderBy(x => x.Precio);
            }

            // Ordeno de mayor a menor precio
            if (productoFiltrarDTO.PrecioMaxBoolean != false)
            {
                productosQueryable = productosQueryable.OrderByDescending(x => x.Precio);
            }

            var productos = await productosQueryable.ToListAsync();

            var productoDTO = mapper.Map<List<ProductoDTO>>(productos);

            return productoDTO;
        }

        // ------------------- MÉTODO PRIVADO: REGISTRAR HISTORIAL -------------------

        private async Task RegistrarLog(
            Producto? productoAntes,
            Producto? productoDespues,
            string usuarioId,
            string usuarioNombre,
            AccionProducto accion)
        {
            // Si el producto después tiene categoría pero no está cargada,
            // la cargo para poder guardar el nombre de la categoría en el historial
            if (productoDespues != null &&
                productoDespues.Categoria == null &&
                productoDespues.CategoriaId != null)
            {
                await dbContext.Entry(productoDespues)
                    .Reference(p => p.Categoria)
                    .LoadAsync();
            }

            // Creo el registro del historial con los datos antes y después
            var historial = new ProductoHistorial
            {
                // Si existe productoDespues uso ese id, si no uso el de productoAntes
                ProductoId = productoDespues?.Id ?? productoAntes!.Id,

                CategoriaId = productoDespues?.CategoriaId ?? productoAntes?.CategoriaId,
                ImagenUrl = productoDespues?.ImagenUrl ?? productoAntes?.ImagenUrl,

                UsuarioId = usuarioId,
                UsuarioNombre = usuarioNombre,
                Accion = accion,

                // Guardo los datos anteriores en formato JSON
                DatosAntes = productoAntes != null ? JsonSerializer.Serialize(new
                {
                    productoAntes.Nombre,
                    productoAntes.Descripcion,
                    productoAntes.CategoriaId,
                    CategoriaNombre = productoAntes.Categoria?.Nombre,
                    productoAntes.ImagenUrl,
                    productoAntes.Precio,
                    productoAntes.Stock
                }) : null,

                // Guardo los datos nuevos en formato JSON
                DatosDespues = productoDespues != null ? JsonSerializer.Serialize(new
                {
                    productoDespues.Nombre,
                    productoDespues.Descripcion,
                    productoDespues.CategoriaId,
                    CategoriaNombre = productoDespues.Categoria?.Nombre,
                    productoDespues.ImagenUrl,
                    productoDespues.Precio,
                    productoDespues.Stock
                }) : null,

                // Fecha en que se hizo la acción
                FechaCreacion = DateTime.UtcNow
            };

            // Guardo el historial en la base de datos
            dbContext.ProductoHistorial.Add(historial);
            await dbContext.SaveChangesAsync();
        }
    }
}