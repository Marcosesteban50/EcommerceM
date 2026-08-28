using AutoMapper;
using AutoMapper.QueryableExtensions;
using EcommerceAPI.Data;
using EcommerceAPI.DTOs.CategoriasDTOs;
using EcommerceAPI.Modelos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]

    // Por defecto este controller requiere usuario Admin
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "Admin")]
    public class CategoriaController : ControllerBase
    {
        // DbContext para trabajar con la base de datos
        private readonly ApplicationDbContext dbContext;

        // Mapper para convertir entidades a DTOs y DTOs a entidades
        private readonly IMapper mapper;

        // Servicio para limpiar el cache cuando cambian las categorías
        private readonly IOutputCacheStore outputCacheStore;

        // Tag que usamos para identificar el cache de categorías
        private const string cacheTag = "categorias";

        public CategoriaController(
            ApplicationDbContext dbContext,
            IMapper mapper,
            IOutputCacheStore outputCacheStore)
        {
            this.dbContext = dbContext;
            this.mapper = mapper;
            this.outputCacheStore = outputCacheStore;
        }

        // Obtiene todas las categorías
        [HttpGet("ObtenerCategorias")]

        // Guardamos esta respuesta en cache usando el tag categorias
        [OutputCache(Tags = [cacheTag])]

        // Permitimos que cualquiera pueda ver las categorías
        [AllowAnonymous]
        public async Task<ActionResult<List<CategoriaDTO>>> ObtenerCategorias()
        {
            // Buscamos las categorías y las convertimos directamente a CategoriaDTO
            var categorias = await dbContext.Categorias
                .ProjectTo<CategoriaDTO>(mapper.ConfigurationProvider)
                .ToListAsync();

            // Si no hay categorías, retornamos NotFound
            if (categorias.Count == 0)
            {
                return NotFound(new { mensaje = "No Hay Categorias" });
            }

            // Retornamos las categorías
            return Ok(categorias);
        }

        // Obtiene una categoría por id
        [HttpGet("{id}", Name = "ObtenerCategoriaPorId")]

        // Cacheamos también esta respuesta
        [OutputCache(Tags = [cacheTag])]

        // Cualquiera puede consultar una categoría
      
        public async Task<ActionResult<CategoriaDTO>> Get(string id)
        {
            // Buscamos la categoría por id y la convertimos a DTO
            var categoria = await dbContext.Categorias
                .ProjectTo<CategoriaDTO>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(x => x.Id == id);

            // Si no existe, retornamos NotFound
            if (categoria == null)
            {
                return NotFound(new { mensaje = $"No se encontro la categoria {id}" });
            }

            // Retornamos la categoría encontrada
            return categoria;
        }

        // Crea una nueva categoría
        [HttpPost]

        // Esto no es muy necesario en POST, porque normalmente cacheamos GET
        [OutputCache(Tags = [cacheTag])]

     
        public async Task<ActionResult> Post([FromBody] CategoriaCreacionDTO categoriaCreacionDTO)
        {
            // Validamos que el DTO cumpla con las reglas del modelo
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Convertimos el DTO de creación a una entidad Categoria
            var categoria = mapper.Map<Categoria>(categoriaCreacionDTO);

            // Agregamos la categoría a la base de datos
            dbContext.Add(categoria);

            // Guardamos los cambios
            await dbContext.SaveChangesAsync();

            // Retornamos OK para indicar que se creó correctamente
            return Ok();
        }

        // Actualiza una categoría existente
        [HttpPut("{id}")]
        public async Task<ActionResult> Put(string id, [FromBody] CategoriaCreacionDTO categoriaCreacionDTO)
        {
            // Validamos el modelo que llega desde el frontend
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Revisamos si existe una categoría con ese id
            var categoriaExiste = await dbContext.Categorias.AnyAsync(x => x.Id == id);

            // Si no existe, retornamos NotFound
            if (!categoriaExiste)
            {
                return NotFound();
            }

            // Convertimos el DTO a entidad Categoria
            var categoria = mapper.Map<Categoria>(categoriaCreacionDTO);

            // Le asignamos el id para actualizar la categoría correcta
            categoria.Id = id;

            // Marcamos la categoría como actualizada
            dbContext.Update(categoria);

            // Limpiamos el cache de categorías para que se vea el cambio nuevo
            await outputCacheStore.EvictByTagAsync(cacheTag, default);

            // Guardamos cambios
            await dbContext.SaveChangesAsync();

            // No devolvemos data, solo indicamos que todo salió bien
            return NoContent();
        }

        // Elimina una categoría por id
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(string id)
        {
            // Eliminamos directamente la categoría que coincida con el id
            var categorias = await dbContext.Categorias
                .Where(x => x.Id == id)
                .ExecuteDeleteAsync();

            // Si no se eliminó nada, significa que no existía
            if (categorias == 0)
            {
                return NotFound();
            }

            // Limpiamos el cache para que no se muestren categorías viejas
            await outputCacheStore.EvictByTagAsync(cacheTag, default);

            // Retornamos NoContent porque se eliminó correctamente
            return NoContent();
        }
    }
}