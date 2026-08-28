// Este controlador maneja todo lo relacionado con las órdenes,
// confirmar compras, estados de orden y estados de pago.
using AutoMapper;
using AutoMapper.QueryableExtensions;
using EcommerceAPI.Data;
using EcommerceAPI.DTOs.CarritoDTOs;
using EcommerceAPI.DTOs.OrdenDTOs;
using EcommerceAPI.Modelos;
using EcommerceAPI.Modelos.Enums;
using EcommerceAPI.Servicios.ServicioUsuarios;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "Admin")]
public class OrdenesController : ControllerBase
{
    private readonly ApplicationDbContext dbContext;
    private readonly IServicioUsuarios servicioUsuarios;
    private readonly UserManager<IdentityUser> userManager;
    private readonly IMapper mapper;
    private readonly IOutputCacheStore outputCacheStore;

    // Tag que uso para limpiar la cache relacionada con las órdenes
    private const string cacheTag = "ordenes";

    public OrdenesController(
        ApplicationDbContext dbContext,
        IServicioUsuarios servicioUsuarios,
        UserManager<IdentityUser> userManager,
        IMapper mapper,
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
        // Obtengo el id del usuario que está logueado
        var usuarioId = await servicioUsuarios.ObtenerUsuarioId();
        if (usuarioId == null) return Unauthorized();

        // Busco el usuario en Identity
        var usuario = await userManager.FindByIdAsync(usuarioId);
        if (usuario == null) return Unauthorized();

        // Busco el carrito del usuario con sus items y productos
        var carrito = await dbContext.Carritos
            .Include(c => c.Items)
            .ThenInclude(i => i.Producto)
            .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);

        // Valido que el carrito exista y tenga productos
        if (carrito == null || !carrito.Items.Any())
            return BadRequest(new { mensaje = "Carrito vacío" });

        // Busco el perfil del usuario para obtener la dirección de envío
        var perfil = await dbContext.PerfilesUsuarios
            .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

        // Si no tiene dirección, no puede confirmar la compra
        if (perfil == null || string.IsNullOrEmpty(perfil.DireccionEnvio))
        {
            return BadRequest(new { mensaje = "Debe completar su dirección de envío" });
        }

        // Obtengo el primer estado de orden disponible, por ejemplo: Creado
        var estadoOrdenCreado = await dbContext.EstadoOrdenes
            .Select(x => x.Id)
            .FirstOrDefaultAsync();

        // Obtengo el primer estado de pago disponible, por ejemplo: Pendiente
        var estadoPagoPendiente = await dbContext.EstadoPagos
            .Select(x => x.Id)
            .FirstOrDefaultAsync();

        // Valido que existan estados creados en la base de datos
        if (estadoOrdenCreado == null)
            return BadRequest(new { mensaje = "No existen ordenes aun espera!" });

        if (estadoPagoPendiente == null)
            return BadRequest(new { mensaje = "No existen ordenes aun espera!" });

        // Uso una transacción para que si algo falla, se deshaga todo
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            // Creo la orden en memoria antes de guardarla en la base de datos
            var orden = new Orden
            {
                UsuarioId = usuarioId,
                DireccionEnvio = perfil.DireccionEnvio,
                EmailUsuario = usuario.Email!,
                EstadoOrdenId = estadoOrdenCreado!,
                EstadoPagoId = estadoPagoPendiente!,
                Fecha = DateTime.UtcNow
            };

            // Recorro cada producto del carrito
            foreach (var item in carrito.Items)
            {
                // Valido que haya suficiente stock antes de comprar
                if (item.Cantidad > item.Producto.Stock)
                    return BadRequest(new { mensaje = $"Stock insuficiente para {item.Producto.Nombre}" });

                // Descuento del stock la cantidad comprada
                item.Producto.Stock -= item.Cantidad;

                // Agrego el producto comprado como item dentro de la orden
                orden.Items.Add(new OrdenItems
                {
                    ProductoId = item.ProductoId,
                    NombreProducto = item.Producto.Nombre!,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.Producto.Precio
                });
            }

            // Calculo el total de la orden sumando cantidad * precio
            orden.Total = orden.Items.Sum(i => i.Cantidad * i.PrecioUnitario);

            // Guardo la orden y vacío el carrito
            dbContext.Ordenes.Add(orden);
            carrito.Items.Clear();

            // Guardo los cambios y confirmo la transacción
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            // Retorno el id de la orden creada
            return Ok(new { orden.Id });
        }
        catch
        {
            // Si algo falla, cancelo todos los cambios
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpPost("CrearEstadoOrden")]
    [OutputCache(Tags = [cacheTag])]
    [AllowAnonymous]
    public async Task<ActionResult> Post([FromBody] EstadoOrdenCreacionDTO estadoOrdenCreacionDTO)
    {
        // Valido que los datos enviados sean correctos
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Convierto el DTO a la entidad EstadoOrden
        var estado = mapper.Map<EstadoOrden>(estadoOrdenCreacionDTO);

        // Guardo el estado en la base de datos
        dbContext.Add(estado);
        await dbContext.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("CrearEstadoPago")]
    [OutputCache(Tags = [cacheTag])]
    [AllowAnonymous]
    public async Task<ActionResult> Post([FromBody] EstadoPagoCreacionDTO estadoPagoCreacionDTO)
    {
        // Valido que los datos enviados sean correctos
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Convierto el DTO a la entidad EstadoPago
        var estado = mapper.Map<EstadoPago>(estadoPagoCreacionDTO);

        // Guardo el estado de pago en la base de datos
        dbContext.Add(estado);
        await dbContext.SaveChangesAsync();

        return Ok();
    }

    [HttpGet("TodasLasOrdenes")]
   
    public async Task<ActionResult<List<OrdenListadoDTO>>> TodasLasOrdenes()
    {
        // Verifico que el usuario esté autenticado
        var usuarioId = await servicioUsuarios.ObtenerUsuarioId();
        if (usuarioId == null) return Unauthorized();

        // Traigo todas las órdenes con usuario, estado de orden y estado de pago
        var ordenes = await dbContext.Ordenes
            .Include(x => x.Usuario)
            .Include(x => x.EstadoOrden)
            .Include(x => x.EstadoPago)
            .OrderByDescending(o => o.Fecha)
            .ToListAsync();

        return Ok(ordenes);
    }

    [HttpGet("EstadosOrdenes")]
 
    public async Task<ActionResult<List<OrdenListadoDTO>>> EstadosOrdenes()
    {
        // Verifico que el usuario esté autenticado
        var usuarioId = await servicioUsuarios.ObtenerUsuarioId();
        if (usuarioId == null)
            return Unauthorized(new { mensaje = "NO estas autenticado" });

        // Traigo todos los estados de órdenes
        var ordenes = await dbContext.EstadoOrdenes
            .OrderByDescending(o => o.Id)
            .ToListAsync();

        // Si no hay estados, devuelvo un mensaje
        if (ordenes.Count == 0)
            return NotFound(new { mensaje = "NO hay Estados" });

        return Ok(ordenes);
    }

    [HttpGet("EstadosPagos")]
  
    public async Task<ActionResult<List<OrdenListadoDTO>>> EstadosPagos()
    {
        // Verifico que el usuario esté autenticado
        var usuarioId = await servicioUsuarios.ObtenerUsuarioId();
        if (usuarioId == null) return Unauthorized();

        // Traigo todos los estados de pago
        var ordenes = await dbContext.EstadoPagos
            .OrderByDescending(o => o.Id)
            .ToListAsync();

        return Ok(ordenes);
    }

    [HttpGet("{id}", Name = "ObtenerOrdenPorId")]
    [OutputCache(Tags = [cacheTag])]
 
    public async Task<ActionResult<OrdenListadoDTO>> Get(string id)
    {
        // Busco una orden por su id y la proyecto al DTO
        var orden = await dbContext.Ordenes
            .ProjectTo<OrdenListadoDTO>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(x => x.Id == id);

        // Si no existe, devuelvo NotFound
        if (orden == null)
            return NotFound(new { mensaje = $"No se encontro la orden {id}" });

        return orden;
    }

    [HttpGet("{id}/EstadoOrden")]
    [OutputCache(Tags = [cacheTag])]
    
    public async Task<ActionResult<EstadoOrdenDTO>> GetEstadoOrdenPorId(string id)
    {
        // Busco un estado de orden por id
        var orden = await dbContext.EstadoOrdenes
            .ProjectTo<EstadoOrdenDTO>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (orden == null)
            return NotFound(new { mensaje = $"No se encontro la orden {id}" });

        return orden;
    }

    [HttpGet("{id}/EstadoPago")]
    [OutputCache(Tags = [cacheTag])]
  
    public async Task<ActionResult<EstadoPagoDTO>> GetEstadoPagoPorId(string id)
    {
        // Busco un estado de pago por id
        var orden = await dbContext.EstadoPagos
            .ProjectTo<EstadoPagoDTO>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (orden == null)
            return NotFound(new { mensaje = $"No se encontro la orden {id}" });

        return orden;
    }

    [HttpPut("{id}")]
 
    public async Task<ActionResult> Put(string id, [FromBody] OrdenCreacionDTO ordenCreacionDTO)
    {
        // Obtengo el usuario logueado
        var usuarioId = await servicioUsuarios.ObtenerUsuarioId();
        if (usuarioId == null)
            return Unauthorized(new { mensaje = "No está autorizado" });

        // Valido el modelo recibido
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Busco la orden que se quiere editar
        var orden = await dbContext.Ordenes
            .Include(x => x.EstadoOrden)
            .Include(x => x.EstadoPago)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (orden == null)
            return NotFound(new { mensaje = "No se encontro la orden" });

        // Actualizo los estados de la orden
        orden.EstadoOrdenId = ordenCreacionDTO.EstadoOrdenId;
        orden.EstadoPagoId = ordenCreacionDTO.EstadoPagoId;

        // Mapeo los datos del DTO hacia la orden existente
        mapper.Map(ordenCreacionDTO, orden);

        // Guardo los cambios
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("EditarEstadoOrden/{id}")]

    public async Task<ActionResult> Put(string id, [FromBody] EstadoOrdenCreacionDTO estadoOrdenCreacionDTO)
    {
        // Valido el modelo enviado
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Verifico si el estado de orden existe
        var estadoExiste = await dbContext.EstadoOrdenes.AnyAsync(x => x.Id == id);

        if (!estadoExiste)
            return NotFound(new { mensaje = $"No existe {estadoExiste}" });

        // Mapeo el DTO a la entidad
        var estado = mapper.Map<EstadoOrden>(estadoOrdenCreacionDTO);
        estado.Id = id;

        // Actualizo el estado
        dbContext.Update(estado);

        // Limpio la cache de órdenes
        await outputCacheStore.EvictByTagAsync(cacheTag, default);

        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("EditarEstadoPago/{id}")]
   
    public async Task<ActionResult> Put(string id, [FromBody] EstadoPagoCreacionDTO estadoPagoCreacionDTO)
    {
        // Valido el modelo enviado
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Verifico si existe el estado de pago
        var estadoPagoExiste = await dbContext.EstadoPagos.AnyAsync(x => x.Id == id);

        if (!estadoPagoExiste)
            return NotFound(new { mensaje = $"No existe {estadoPagoExiste}" });

        // Mapeo el DTO a la entidad
        var estadoPago = mapper.Map<EstadoPago>(estadoPagoCreacionDTO);
        estadoPago.Id = id;

        // Actualizo el estado de pago
        dbContext.Update(estadoPago);

        // Limpio la cache de órdenes
        await outputCacheStore.EvictByTagAsync(cacheTag, default);

        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("BorrarEstadoOrden/{id}")]
    
    public async Task<ActionResult> BorrarEstadoOrden(string id)
    {
        // Borro el estado de orden directamente en la base de datos
        var ordenes = await dbContext.EstadoOrdenes
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync();

        // Si no se borró nada, significa que no existía
        if (ordenes == 0)
            return NotFound();

        // Limpio la cache
        await outputCacheStore.EvictByTagAsync(cacheTag, default);

        return NoContent();
    }

    [HttpDelete("BorrarEstadoPago/{id}")]
  
    public async Task<ActionResult> BorrarEstadoPago(string id)
    {
        // Borro el estado de pago directamente en la base de datos
        var ordenes = await dbContext.EstadoPagos
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync();

        // Si no se borró nada, significa que no existía
        if (ordenes == 0)
            return NotFound();

        // Limpio la cache
        await outputCacheStore.EvictByTagAsync(cacheTag, default);

        return NoContent();
    }
}