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
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
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
        private const string cacheTag = "productos";
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

        // ------------------- GET: OBTENER PRODUCTOS -------------------

        [HttpGet("ObtenerProductos")]
        [OutputCache(Tags = [cacheTag])]
        [AllowAnonymous]
        public async Task<ActionResult<List<ProductoDTO>>> ObtenerProductos()
        {
            var productos = await dbContext.Productos.Where(x => x.Aprobado)
                .ProjectTo<ProductoDTO>(mapper.ConfigurationProvider)
                .ToListAsync();

            if (productos.Count == 0)
                return NotFound( new { mensaje = "No hay productos" });

            return Ok(productos);
        }



        [HttpGet("{Id}/historial")]
        [AllowAnonymous]
        public async Task<ActionResult<ProductoHistorial>> ObtenerHistorial(string Id)
        {

            var productos = await dbContext.ProductoHistorial.Include(x => x.Categoria)
               .ProjectTo<ProductoHistorialDTO>(mapper.ConfigurationProvider).FirstOrDefaultAsync(x => x.Id== Id);

            Console.WriteLine($"Id recibido: {Id}");

            if (productos == null)
                return NotFound(new { mensaje = $"No se encontro {Id}" });

            return Ok(productos);

        }


        [HttpGet("{id}")]
        [OutputCache(Tags = [cacheTag])]
        [AllowAnonymous]
        public async Task<ActionResult<ProductoDTO>> Get(string id)
        {
            var producto = await dbContext.Productos.Where(x => x.Aprobado)
                .ProjectTo<ProductoDTO>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (producto == null)
                return NotFound(new { mensaje = $"No se encontró -> {id}" });

            return producto;
        }




        [HttpGet("HistorialProductos")]
        [AllowAnonymous]
        public async Task<ActionResult<List<ProductoHistorial>>> HistorialTodosProductos()
        {
            var historial = await dbContext.ProductoHistorial.Include(x => x.Categoria).ProjectTo<ProductoHistorialDTO>(mapper.ConfigurationProvider)
                .OrderByDescending(x => x.FechaCreacion).ToListAsync();

            if (historial.Count == 0)
                return NotFound(new { mensaje = "No se encontro historial" });

            return Ok(historial);
        }


        // ------------------- POST: CREAR PRODUCTO -------------------

        [HttpPost]
        [OutputCache(Tags = [cacheTag])]
        public async Task<ActionResult> Post([FromForm] ProductoCreacionDTO productoCreacionDTO)
        {
            var usuarioId = await servicioUsuarios.ObtenerUsuarioId();
            if (usuarioId == null)
                return Unauthorized(new { mensaje = "No está autorizado" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var producto = mapper.Map<Producto>(productoCreacionDTO);


            //guardamos la fto si no es null
            if (productoCreacionDTO.ImagenUrl is not null)
            {
                var url = await almacenadorArchivos.Almacenar(contenedor, productoCreacionDTO.ImagenUrl);
                producto.ImagenUrl = url;
            }




            producto.Aprobado = false;
            producto.UsuarioId = usuarioId;



            dbContext.Add(producto);
            await dbContext.SaveChangesAsync();

            var usuario = await userManager.FindByIdAsync(usuarioId);

            await RegistrarLog(
                productoAntes: null,
                productoDespues: producto,
                usuarioId: usuarioId,
                usuarioNombre: usuario?.UserName ?? usuario?.Email ?? "Desconocido",
                accion: AccionProducto.Creado
            );

            return Ok(new { mensaje = "Producto enviado para aprobación" });

        }


        [HttpPut("AgregarStock")]
        public async Task<ActionResult> AgregarStock(string id, [FromBody] AgregarMasProductosDTO agregarMasProductosDTO)
        {
            var usuarioId = await servicioUsuarios.ObtenerUsuarioId();
            if (usuarioId == null)
                return Unauthorized(new { mensaje = "No está autorizado" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var producto = await dbContext.Productos.FirstOrDefaultAsync(x => x.Id == id);
            if (producto == null)
                return NotFound();

            // Guardar estado anterior
            var productoAntes = new Producto
            {
                Id = producto.Id,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                Precio = producto.Precio,
                Stock = producto.Stock
            };

            // Aplicar el cambio
            producto.Stock += agregarMasProductosDTO.Stock;
            producto.UsuarioId = usuarioId;
            await dbContext.SaveChangesAsync();

            // Guardar estado después
            var usuario = await userManager.FindByIdAsync(usuarioId);


            await RegistrarLog(productoAntes, producto,
               usuarioId,
                usuario?.UserName ?? usuario?.Email ?? "Desconocido",
               accion:AccionProducto.StockAumentado

            );

            return NoContent();
        }

        // ------------------- PUT: EDITAR PRODUCTO -------------------

        [HttpPut("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult> Put(string id, [FromForm] ProductoCreacionDTO productoCreacionDTO)
        {
            var usuarioId = await servicioUsuarios.ObtenerUsuarioId();
            if (usuarioId == null)
                return Unauthorized(new { mensaje = "No está autorizado" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);


            //MANANA HACER INCLUDE CATEGORIAS ACA
            var producto = await dbContext.Productos.Include(x => x.Categoria).FirstOrDefaultAsync(x => x.Id == id);
            if (producto == null)
                return NotFound(new {mensaje="No se encontro el producto"});

            

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


            producto.UsuarioId = usuarioId;


            mapper.Map(productoCreacionDTO, producto);


            if (producto.ImagenUrl != null)
            {
                producto.ImagenUrl = await almacenadorArchivos.Editar(producto.ImagenUrl, contenedor, productoCreacionDTO.ImagenUrl!);
            }

            await dbContext.SaveChangesAsync();

            var usuario = await userManager.FindByIdAsync(usuarioId);


            Console.WriteLine(productoAntes);
            await RegistrarLog(productoAntes, producto, usuarioId, usuario?.UserName ?? usuario?.Email ?? "Desconocido", 
               accion:AccionProducto.Editado
                );




            return NoContent();
        }

        // ------------------- DELETE: ELIMINAR PRODUCTO -------------------

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(string id)
        {
            var usuarioId = await servicioUsuarios.ObtenerUsuarioId();
            if (usuarioId == null)
                return Unauthorized(new { mensaje = "No está autorizado" });

            var producto = await dbContext.Productos.FirstOrDefaultAsync(x => x.Id == id);
            if (producto == null)
                return NotFound($"El producto con ID {id} no existe");

            var usuario = await userManager.FindByIdAsync(usuarioId);


            producto.UsuarioId = usuarioId;

            dbContext.Productos.Remove(producto);
            await dbContext.SaveChangesAsync();

            await RegistrarLog(
                producto,
                productoDespues: null,
                usuarioId,
                //username false? -> email false? -> Desconocido
                usuario?.UserName ?? usuario?.Email ?? "Desconocido",
                               accion: AccionProducto.Eliminado

            );

            await outputCacheStore.EvictByTagAsync(cacheTag, default);
            return NoContent();
        }

        // ------------------- GET: HISTORIAL POR PRODUCTO -------------------









        [HttpPut("aprobar/{id}")]

        public async Task<ActionResult> AprobarProducto(string id)
        {

            var usuarioId = await servicioUsuarios.ObtenerUsuarioId();
            var usuario = await userManager.FindByIdAsync(usuarioId);



            var producto = await dbContext.Productos.FindAsync(id);

            if (producto == null)
                return NotFound(new { mensaje = "Producto no encontrado" });

            producto.Aprobado = true;
            producto.UsuarioId = usuarioId;
            await dbContext.SaveChangesAsync();


            await RegistrarLog(
                producto,
                producto,
                usuarioId,
                usuario?.UserName ?? usuario?.Email ?? "Desconocido",
                               accion: AccionProducto.Aprobado

            );




            return Ok(new { mensaje = "Producto Aprobado" });

        }


        [HttpPut("rechazar/{id}")]

        public async Task<ActionResult> RechazarProducto(string id)
        {

            var usuarioId = await servicioUsuarios.ObtenerUsuarioId();
            var usuario = await userManager.FindByIdAsync(usuarioId);



            var producto = await dbContext.Productos.FindAsync(id);

            if (producto == null)
                return NotFound(new { mensaje = "Producto no encontrado" });


            producto.UsuarioId = usuarioId;
            await dbContext.SaveChangesAsync();

            await RegistrarLog(producto, producto, usuarioId,
               usuario?.UserName ?? usuario?.Email ?? "Desconocido",
               accion:AccionProducto.Rechazado
           );


            dbContext.Productos.Remove(producto);
            await dbContext.SaveChangesAsync();
            return Ok(new { mensaje = "Producto Rechazado" });

        }


        [HttpGet("Pendientes")]

        public async Task<ActionResult<List<ProductoDTO>>> ObtenerPendientes()
        {
            var pendientes = await dbContext.Productos
                .Where(p => !p.Aprobado)
                .ProjectTo<ProductoDTO>(mapper.ConfigurationProvider)
                .ToListAsync();

            return Ok(pendientes);
        }



        [HttpGet("Filtrar")]
        [AllowAnonymous]
        public async Task<ActionResult<List<ProductoDTO>>> Filtrar([FromQuery] ProductoFiltrarDTO productoFiltrarDTO)
        {



            //Mostramos los productos aprobados solamente!
            var productosQueryable = dbContext.Productos.Where(p => p.Aprobado && p.Stock > 0).AsQueryable();





            if (!string.IsNullOrWhiteSpace(productoFiltrarDTO.Nombre))
            {
                productosQueryable = productosQueryable.Where(x => x.Nombre!.Contains(productoFiltrarDTO.Nombre));
            }



            if (!string.IsNullOrWhiteSpace(productoFiltrarDTO.CategoriaId))
            {
                productosQueryable = productosQueryable.Where(x => x.CategoriaId == productoFiltrarDTO.CategoriaId);
            }

            if (productoFiltrarDTO.PrecioMin != null && productoFiltrarDTO.PrecioMin > 0)
            {
                productosQueryable = productosQueryable.Where(x => x.Precio >= productoFiltrarDTO.PrecioMin);
            }

            if (productoFiltrarDTO.PrecioMax != null && productoFiltrarDTO.PrecioMax > 0)
            {
                productosQueryable = productosQueryable.Where(x => x.Precio <= productoFiltrarDTO.PrecioMax);
            }

            var productos = await productosQueryable.ToListAsync();

            var productoDTO = mapper.Map<List<ProductoDTO>>(productos);

            return productoDTO;

        }



        [HttpGet("FiltrarLanding")]
        [AllowAnonymous]
        public async Task<ActionResult<List<ProductoDTO>>> FiltrarLanding([FromQuery] ProductoFiltrarDTO productoFiltrarDTO)
        {



            //Mostramos los productos aprobados solamente!
            var productosQueryable = dbContext.Productos.Where(p => p.Aprobado && p.Stock > 0).AsQueryable();


            if (!string.IsNullOrWhiteSpace(productoFiltrarDTO.Nombre))
            {
                productosQueryable = productosQueryable.Where(x => x.Nombre!.Contains(productoFiltrarDTO.Nombre));
            }

            if (!string.IsNullOrWhiteSpace(productoFiltrarDTO.CategoriaId))
            {
                productosQueryable = productosQueryable.Where(x => x.CategoriaId == productoFiltrarDTO.CategoriaId);
            }


            if (productoFiltrarDTO.PrecioMinBoolean != false)
            {
                productosQueryable = productosQueryable.OrderBy(x => x.Precio);
            }

            if (productoFiltrarDTO.PrecioMaxBoolean != false)
            {
                productosQueryable = productosQueryable.OrderByDescending(x => x.Precio);
            }

            var productos = await productosQueryable.ToListAsync();

            var productoDTO = mapper.Map<List<ProductoDTO>>(productos);

            return productoDTO;

        }



        private async Task RegistrarLog(Producto? productoAntes, Producto? productoDespues, string usuarioId, string usuarioNombre, AccionProducto accion)
        {

            // Si productoDespues tiene una nueva categoría, cargarla
            if (productoDespues != null && productoDespues.Categoria == null && productoDespues.CategoriaId != null)
            {
                await dbContext.Entry(productoDespues)
                    .Reference(p => p.Categoria)
                    .LoadAsync();
            }

            var historial = new ProductoHistorial
            {
                ProductoId = productoDespues?.Id ?? productoAntes!.Id, 
                CategoriaId = productoDespues?.CategoriaId ?? productoAntes?.CategoriaId,
                ImagenUrl = productoDespues?.ImagenUrl ?? productoAntes?.ImagenUrl,
                
                UsuarioId = usuarioId,
                UsuarioNombre = usuarioNombre,
                Accion = accion,

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

                FechaCreacion = DateTime.UtcNow
            };

            dbContext.ProductoHistorial.Add(historial);
            await dbContext.SaveChangesAsync();
        }


    }
}
