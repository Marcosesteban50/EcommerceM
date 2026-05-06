using AutoMapper;
using EcommerceAPI.Data;
using EcommerceAPI.DTOs.UsuariosDTO;
using EcommerceAPI.Servicios.ServicioUsuarios;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Controllers
{
    [Route("api/[Controller]")]
    [ApiController]

    // Solo usuarios con rol Cliente pueden acceder a este controller
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "Cliente")]
    public class PerfilController : ControllerBase
    {
        // DbContext para acceder a la base de datos
        private readonly ApplicationDbContext dbContext;

        // Mapper para convertir entidades a DTOs
        private readonly IMapper mapper;

        // Servicio para obtener el usuario logueado desde el token
        private readonly IServicioUsuarios servicioUsuarios;

        // UserManager para trabajar con Identity (usuarios)
        private readonly UserManager<IdentityUser> userManager;

        public PerfilController(
            ApplicationDbContext dbContext,
            IMapper mapper,
            IServicioUsuarios servicioUsuarios,
            UserManager<IdentityUser> userManager)
        {
            this.dbContext = dbContext;
            this.mapper = mapper;
            this.servicioUsuarios = servicioUsuarios;
            this.userManager = userManager;
        }

        // ================== OBTENER PERFIL COMPLETO ==================
        [HttpGet("mi-perfil-completo")]
        public async Task<ActionResult<PerfilUsuarioDTO>> ObtenerMiPerfilCompleto()
        {
            // Obtenemos el id del usuario desde el token
            var userId = await servicioUsuarios.ObtenerUsuarioId();

            // Si no hay usuario, no está autorizado
            if (userId == null) return Unauthorized();

            // Buscamos el usuario en Identity para obtener su email
            var email = await userManager.FindByIdAsync(userId);

            // Buscamos el perfil del usuario en nuestra tabla personalizada
            var perfil = await dbContext.PerfilesUsuarios
                .FirstOrDefaultAsync(x => x.UsuarioId == userId);

            // Si no existe perfil, retornamos error
            if (perfil == null) return NotFound();

            // Convertimos el perfil a DTO para enviarlo al frontend
            var UsuarioMapeado = mapper.Map<PerfilUsuarioDTO>(perfil);

            // Asignamos el email desde Identity (no siempre lo guardas igual en Perfil)
            UsuarioMapeado.Email = email!.Email;

            // Retornamos el perfil completo
            return UsuarioMapeado;
        }

        // ================== ACTUALIZAR PERFIL ==================
        [HttpPut("Actualizar-perfil")]
        public async Task<IActionResult> ActualizarPerfil(PerfilUsuarioDTO dto)
        {
            // Obtenemos el usuario logueado
            var usuarioId = await servicioUsuarios.ObtenerUsuarioId();

            // Buscamos el perfil del usuario en la base de datos
            var perfil = await dbContext.PerfilesUsuarios
                .FirstOrDefaultAsync(x => x.UsuarioId == usuarioId);

            // Si no existe el perfil, retornamos error
            if (perfil == null)
                return NotFound(new { mensaje = "No se encontro perfil" });

            // Actualizamos los campos que vienen del frontend
            perfil.DireccionEnvio = dto.DireccionEnvio;
            perfil.Telefono = dto.Telefono;
            perfil.NombreCompleto = dto.NombreCompleto;
            perfil.Email = dto.Email;

            // Guardamos los cambios en la base de datos
            await dbContext.SaveChangesAsync();

            // No devolvemos nada, solo confirmamos que se actualizó correctamente
            return NoContent();
        }
    }
}