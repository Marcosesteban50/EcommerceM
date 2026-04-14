using AutoMapper;
using AutoMapper.QueryableExtensions;
using EcommerceAPI.Data;
using EcommerceAPI.DTOs.CategoriasDTOs;
using EcommerceAPI.DTOs.ProductosDTOs;
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
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "Admin")]

    public class CategoriaController : ControllerBase
    {
        private readonly ApplicationDbContext dbContext;
        private readonly IMapper mapper;
        private readonly IOutputCacheStore outputCacheStore;
        private const string cacheTag = "categorias";


        public CategoriaController(ApplicationDbContext dbContext, IMapper mapper,
            IOutputCacheStore outputCacheStore)
        {
            this.dbContext = dbContext;
            this.mapper = mapper;
            this.outputCacheStore = outputCacheStore;
        }




        [HttpGet("ObtenerCategorias")]

        [OutputCache(Tags = [cacheTag])]
        [AllowAnonymous]
        

        public async Task<ActionResult<List<CategoriaDTO>>> ObtenerCategorias()
        {
            var categorias = await dbContext.Categorias
        .ProjectTo<CategoriaDTO>(mapper.ConfigurationProvider)
        .ToListAsync();

            if (categorias.Count == 0)
            {

                return NotFound(new { mensaje = "No Hay Categorias" });
            }


            return Ok(categorias);
        }


        [HttpGet("{id}", Name = "ObtenerCategoriaPorId")]
        [OutputCache(Tags = [cacheTag])]
        [AllowAnonymous]
        
        public async Task<ActionResult<CategoriaDTO>> Get(string id)
        {

            var categoria = await dbContext.Categorias
                 .ProjectTo<CategoriaDTO>(mapper.ConfigurationProvider).FirstOrDefaultAsync(x => x.Id == id);




            if (categoria == null)
            {
                return NotFound(new { mensaje = $"No se encontro la categoria {id}" });
            }

            return categoria;

        }

     



        [HttpPost]
        [OutputCache(Tags = [cacheTag])]
        [AllowAnonymous]

        public async Task<ActionResult> Post([FromBody] CategoriaCreacionDTO categoriaCreacionDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var categoria = mapper.Map<Categoria>(categoriaCreacionDTO);


            dbContext.Add(categoria);
            await dbContext.SaveChangesAsync();

            return Ok();
        }



        [HttpPut("{id}")]

        public async Task<ActionResult> Put(string id, [FromBody] CategoriaCreacionDTO categoriaCreacionDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var categoriaExiste = await dbContext.Categorias.AnyAsync(x => x.Id == id);


            if (!categoriaExiste)
            {
                return NotFound();
            }

            var categoria = mapper.Map<Categoria>(categoriaCreacionDTO);

            categoria.Id = id;
            dbContext.Update(categoria);
            await outputCacheStore.EvictByTagAsync(cacheTag, default);

            await dbContext.SaveChangesAsync();

            return NoContent();
        }


        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(string id)
        {
            var Categorias = await
                 dbContext.Categorias.
                 Where(x => x.Id == id).ExecuteDeleteAsync();

            if (Categorias == 0)
            {
                return NotFound();
            }
            await outputCacheStore.EvictByTagAsync(cacheTag, default);

            return NoContent();

        }

    }
}
