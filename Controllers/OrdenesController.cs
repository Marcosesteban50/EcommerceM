using AutoMapper;
using AutoMapper.QueryableExtensions;
using EcommerceAPI.Data;
using EcommerceAPI.DTOs.CarritoDTOs;
using EcommerceAPI.DTOs.CategoriasDTOs;
using EcommerceAPI.DTOs.OrdenDTOs;
using EcommerceAPI.DTOs.ProductosDTOs;
using EcommerceAPI.Modelos;
using EcommerceAPI.Modelos.Enums;
using EcommerceAPI.Servicios.Archivos;
using EcommerceAPI.Servicios.ServicioUsuarios;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "Admin")]

    // el carrito es por usuario logueado
    public class OrdenesController : ControllerBase
    {
        private readonly ApplicationDbContext dbContext;
        private readonly IServicioUsuarios servicioUsuarios;
        private readonly UserManager<IdentityUser> userManager;
        private readonly IMapper mapper;
        private readonly IOutputCacheStore outputCacheStore;
        private const string cacheTag = "ordenes";


        public OrdenesController(ApplicationDbContext dbContext,
            IServicioUsuarios servicioUsuarios, UserManager<IdentityUser> userManager, IMapper mapper,
            IOutputCacheStore outputCacheStore)
        {
            this.dbContext = dbContext;
            this.servicioUsuarios = servicioUsuarios;
            this.userManager = userManager;
            this.mapper = mapper;
            this.outputCacheStore = outputCacheStore;
        }



        [HttpPost("ConfirmarCompra")]
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmarCompra(ConfirmarCompraDTO dto)
        {
            var usuarioId = await servicioUsuarios.ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();

            var usuario = await userManager.FindByIdAsync(usuarioId);
            if (usuario == null) return Unauthorized();


            var carrito = await dbContext.Carritos
                .Include(c => c.Items)
                .ThenInclude(i => i.Producto)
                .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);

            if (carrito == null || !carrito.Items.Any())
                return BadRequest(new { mensaje = "Carrito vacío" });



            var perfil = await dbContext.PerfilesUsuarios.FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

            if (perfil == null || string.IsNullOrEmpty(perfil.DireccionEnvio))
            {
                return BadRequest(new { mensaje = "Debe completar su dirección de envío" });
            }




            var estadoOrdenCreado = await dbContext.EstadoOrdenes
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            var estadoPagoPendiente = await dbContext.EstadoPagos
                .Select(x => x.Id)
                .FirstOrDefaultAsync();


            if (estadoOrdenCreado == null)
            {
                return BadRequest(new { mensaje = "No existen ordenes aun espera!" });

            }

            if (estadoPagoPendiente == null)
            {
                return BadRequest(new { mensaje = "No existen ordenes aun espera!" });

            }


            //Si algo falla apartir de aca→ rollback.
            using var transaction = await dbContext.Database.BeginTransactionAsync();

            try
            {
                //Creando orden en Memoria
                var orden = new Orden
                {
                    UsuarioId = usuarioId,
                    DireccionEnvio = perfil.DireccionEnvio,
                    EmailUsuario = usuario.Email!,
                    EstadoOrdenId = estadoOrdenCreado!,
                    EstadoPagoId = estadoPagoPendiente!,
                    Fecha = DateTime.UtcNow
                };

                foreach (var item in carrito.Items)
                {

                    //No Hay Suficiente stock para comprar eso
                    if (item.Cantidad > item.Producto.Stock)
                        return BadRequest(new { mensaje = $"Stock insuficiente para {item.Producto.Nombre}" });


                    //Reducimos stock
                    item.Producto.Stock -= item.Cantidad;


                    //Agregando items  a la orden
                    orden.Items.Add(new OrdenItems
                    {
                        ProductoId = item.ProductoId,
                        NombreProducto = item.Producto.Nombre!,
                        //Estadoorden y Pago aqui,

                        Cantidad = item.Cantidad,
                        PrecioUnitario = item.Producto.Precio
                    });
                }
                //Sumando Cantidad de productos por precio del producto
                orden.Total = orden.Items.Sum(i => i.Cantidad * i.PrecioUnitario);

                dbContext.Ordenes.Add(orden);
                carrito.Items.Clear();

                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { orden.Id });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }



        //Esto Puede servir despues

        //[HttpPost("PagarOrden/{ordenId}")]
        //public async Task<ActionResult> PagarOrden(string ordenId)
        //{
        //    var usuarioId = await servicioUsuarios.ObtenerUsuarioId();
        //    if (usuarioId == null) return Unauthorized(new { mensaje = "No estas autorizado!" });

        //    var orden = await dbContext.Ordenes
        //        .FirstOrDefaultAsync(o => o.Id == ordenId && o.UsuarioId == usuarioId);

        //    if (orden == null)
        //        return NotFound(new { mensaje = "Orden no encontrada" });



        //    await dbContext.SaveChangesAsync();

        //    return Ok(new { mensaje = "Pago realizado correctamente" });
        //}



        [HttpPost("CrearEstadoOrden")]
        [OutputCache(Tags = [cacheTag])]
        [AllowAnonymous]

        public async Task<ActionResult> Post([FromBody] EstadoOrdenCreacionDTO estadoOrdenCreacionDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var estado = mapper.Map<EstadoOrden>(estadoOrdenCreacionDTO);


            dbContext.Add(estado);
            await dbContext.SaveChangesAsync();

            return Ok();
        }


        [HttpPost("CrearEstadoPago")]
        [OutputCache(Tags = [cacheTag])]
        [AllowAnonymous]


        public async Task<ActionResult> Post([FromBody] EstadoPagoCreacionDTO estadoPagoCreacionDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var estado = mapper.Map<EstadoPago>(estadoPagoCreacionDTO);


            dbContext.Add(estado);
            await dbContext.SaveChangesAsync();

            return Ok();
        }





        [HttpGet("TodasLasOrdenes")]
        [AllowAnonymous]
        public async Task<ActionResult<List<OrdenListadoDTO>>> TodasLasOrdenes()
        {
            var usuarioId = await servicioUsuarios.ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();



            var ordenes = await dbContext.Ordenes.Include(x => x.Usuario)
                .Include(x => x.EstadoOrden).Include(x => x.EstadoPago)
                .OrderByDescending(o => o.Fecha)
                .ToListAsync();

            return Ok(ordenes);
        }


        [HttpGet("EstadosOrdenes")]
        [AllowAnonymous]
        public async Task<ActionResult<List<OrdenListadoDTO>>> EstadosOrdenes()
        {
            var usuarioId = await servicioUsuarios.ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized(new { mensaje = "NO estas autenticado" });



            var ordenes = await dbContext.EstadoOrdenes
                .OrderByDescending(o => o.Id)
                .ToListAsync();


            if (ordenes.Count == 0) return NotFound(new { mensaje = "NO hay Estados" });

            return Ok(ordenes);
        }

        [HttpGet("EstadosPagos")]
        [AllowAnonymous]
        public async Task<ActionResult<List<OrdenListadoDTO>>> EstadosPagos()
        {
            var usuarioId = await servicioUsuarios.ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();



            var ordenes = await dbContext.EstadoPagos
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            return Ok(ordenes);
        }





        [HttpGet("{id}", Name = "ObtenerOrdenPorId")]
        [OutputCache(Tags = [cacheTag])]
        [AllowAnonymous]

        public async Task<ActionResult<OrdenListadoDTO>> Get(string id)
        {

            var Orden = await dbContext.Ordenes
                 .ProjectTo<OrdenListadoDTO>(mapper.ConfigurationProvider).FirstOrDefaultAsync(x => x.Id == id);


            if (Orden == null)
            {
                return NotFound(new { mensaje = $"No se encontro la orden  {id}" });
            }

            return Orden;

        }


        [HttpGet("{id}/EstadoOrden")]
        [OutputCache(Tags = [cacheTag])]
        [AllowAnonymous]

        public async Task<ActionResult<EstadoOrdenDTO>> GetEstadoOrdenPorId(string id)
        {

            var Orden = await dbContext.EstadoOrdenes
                 .ProjectTo<EstadoOrdenDTO>(mapper.ConfigurationProvider).FirstOrDefaultAsync(x => x.Id == id);


            if (Orden == null)
            {
                return NotFound(new { mensaje = $"No se encontro la orden  {id}" });
            }

            return Orden;

        }



        [HttpGet("{id}/EstadoPago")]
        [OutputCache(Tags = [cacheTag])]
        [AllowAnonymous]

        public async Task<ActionResult<EstadoPagoDTO>> GetEstadoPagoPorId(string id)
        {

            var Orden = await dbContext.EstadoPagos
                 .ProjectTo<EstadoPagoDTO>(mapper.ConfigurationProvider).FirstOrDefaultAsync(x => x.Id == id);


            if (Orden == null)
            {
                return NotFound(new { mensaje = $"No se encontro la orden  {id}" });
            }

            return Orden;

        }





        [HttpPut("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult> Put(string id, [FromBody] OrdenCreacionDTO ordenCreacionDTO)
        {
            var usuarioId = await servicioUsuarios.ObtenerUsuarioId();
            if (usuarioId == null)
                return Unauthorized(new { mensaje = "No está autorizado" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);


            var orden = await dbContext.Ordenes.Include(x => x.EstadoOrden).Include(x => x.EstadoPago).FirstOrDefaultAsync(x => x.Id == id);
            if (orden == null)
                return NotFound(new { mensaje = "No se encontro la orden" });



            orden.EstadoOrdenId = ordenCreacionDTO.EstadoOrdenId;
            orden.EstadoPagoId = ordenCreacionDTO.EstadoPagoId;


            //var ordenAntes = new Orden
            //{
            //    Id = orden.Id,
            //    EstadoOrdenId = orden.EstadoOrdenId,
            //     EstadoPagoId = orden.EstadoPagoId

            //};




            mapper.Map(ordenCreacionDTO, orden);



            await dbContext.SaveChangesAsync();

            //var usuario = await userManager.FindByIdAsync(usuarioId);





            return NoContent();
        }




        [HttpPut("EditarEstadoOrden/{id}")]
        [AllowAnonymous]


        public async Task<ActionResult> Put(string id, [FromBody] EstadoOrdenCreacionDTO estadoOrdenCreacionDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var estadoExiste = await dbContext.EstadoOrdenes.AnyAsync(x => x.Id == id);


            if (!estadoExiste)
            {
                return NotFound(new { mensaje = $"No existe {estadoExiste}" });

            }

            var estado = mapper.Map<EstadoOrden>(estadoOrdenCreacionDTO);

            estado.Id = id;
            dbContext.Update(estado);
            await outputCacheStore.EvictByTagAsync(cacheTag, default);

            await dbContext.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("EditarEstadoPago/{id}")]
        [AllowAnonymous]


        public async Task<ActionResult> Put(string id, [FromBody] EstadoPagoCreacionDTO estadoPagoCreacionDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var estadoPagoExiste = await dbContext.EstadoPagos.AnyAsync(x => x.Id == id);


            if (!estadoPagoExiste)
            {
                return NotFound(new { mensaje = $"No existe {estadoPagoExiste}" });
            }

            var estadoPago = mapper.Map<EstadoPago>(estadoPagoCreacionDTO);

            estadoPago.Id = id;
            dbContext.Update(estadoPago);
            await outputCacheStore.EvictByTagAsync(cacheTag, default);

            await dbContext.SaveChangesAsync();

            return NoContent();
        }


        [HttpDelete("BorrarEstadoOrden/{id}")]
        [AllowAnonymous]

        public async Task<ActionResult> BorrarEstadoOrden(string id)
        {
            var Ordenes = await
                 dbContext.EstadoOrdenes.
                 Where(x => x.Id == id).ExecuteDeleteAsync();

            if (Ordenes == 0)
            {
                return NotFound();
            }
            await outputCacheStore.EvictByTagAsync(cacheTag, default);

            return NoContent();

        }

        [HttpDelete("BorrarEstadoPago/{id}")]
        [AllowAnonymous]

        public async Task<ActionResult> BorrarEstadoPago(string id)
        {
            var Ordenes = await
                 dbContext.EstadoPagos.
                 Where(x => x.Id == id).ExecuteDeleteAsync();

            if (Ordenes == 0)
            {
                return NotFound();
            }
            await outputCacheStore.EvictByTagAsync(cacheTag, default);

            return NoContent();

        }








    }
}
