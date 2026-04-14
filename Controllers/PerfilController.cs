using AutoMapper;
using EcommerceAPI.Data;
using EcommerceAPI.DTOs.UsuariosDTO;
using EcommerceAPI.Servicios.ServicioUsuarios;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcommerceAPI.Controllers
{
    [Route("api/[Controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "Cliente")]

    public class PerfilController : ControllerBase
    {
        private readonly ApplicationDbContext dbContext;
        private readonly IMapper mapper;
        private readonly IServicioUsuarios servicioUsuarios;
        private readonly UserManager<IdentityUser> userManager;

        public PerfilController(
            ApplicationDbContext dbContext, IMapper mapper, IServicioUsuarios servicioUsuarios,
            UserManager<IdentityUser> userManager)
        {
            this.dbContext = dbContext;
            this.mapper = mapper;
            this.servicioUsuarios = servicioUsuarios;
            this.userManager = userManager;
        }







        [HttpGet("mi-perfil-completo")]
        

        public async Task<ActionResult<PerfilUsuarioDTO>> ObtenerMiPerfilCompleto()
        {
            var userId = await servicioUsuarios.ObtenerUsuarioId();
            if (userId == null) return Unauthorized();


            //Obteniendo el Email
            var email = await userManager.FindByIdAsync(userId);


            var perfil = await dbContext.PerfilesUsuarios
                .FirstOrDefaultAsync(x => x.UsuarioId == userId);

            if (perfil == null) return NotFound();

            var UsuarioMapeado = mapper.Map<PerfilUsuarioDTO>(perfil);

            //Asignando Email 
            UsuarioMapeado.Email = email!.Email;

            return UsuarioMapeado;
        }


        [HttpPut("Actualizar-perfil")]
      

        public async Task<IActionResult> ActualizarPerfil(PerfilUsuarioDTO dto)
        {
            var usuarioId = await servicioUsuarios.ObtenerUsuarioId();


            var perfil = await dbContext.PerfilesUsuarios
                .FirstOrDefaultAsync(x => x.UsuarioId == usuarioId);

            if (perfil == null)
                return NotFound(new { mensaje = "No se encontro perfil" });

            perfil.DireccionEnvio = dto.DireccionEnvio;
            perfil.Telefono = dto.Telefono;
            perfil.NombreCompleto = dto.NombreCompleto;
            perfil.Email = dto.Email;

            await dbContext.SaveChangesAsync();

            return NoContent();
        }
    }
}
