using AutoMapper;
using EcommerceAPI.Data;
using EcommerceAPI.DTOs;
using EcommerceAPI.DTOs.GoogleDTO;
using EcommerceAPI.DTOs.UsuariosDTO;
using EcommerceAPI.Modelos;
using EcommerceAPI.Servicios.ServicioUsuarios;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EcommerceAPI.Controllers
{
    [Route("api/[Controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "Admin")]


    public class UsuariosController : ControllerBase
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly SignInManager<IdentityUser> signInManager;
        private readonly IConfiguration configuration;
        private readonly ApplicationDbContext dbContext;
        private readonly IMapper mapper;
        private readonly ILogger<UsuariosController> logger;
        private readonly IServicioUsuarios servicioUsuarios;

        public UsuariosController(UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager, IConfiguration configuration,
            ApplicationDbContext dbContext, IMapper mapper, ILogger<UsuariosController> logger, IServicioUsuarios servicioUsuarios)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.configuration = configuration;
            this.dbContext = dbContext;
            this.mapper = mapper;
            this.logger = logger;
            this.servicioUsuarios = servicioUsuarios;
        }





        [HttpGet("ListadoUsuarios")]
        [AllowAnonymous]
        public async Task<ActionResult<List<UsuarioDTO>>> ListadoUsuarios()
        {

            var usuarios = await userManager.Users.ToListAsync();

            var lista = new List<UsuarioDTO>();

            foreach (var usuario in usuarios)
            {

                var claims = await userManager.GetClaimsAsync(usuario);
                var roles = claims.Select(c => $"{c.Type}: {c.Value}").ToList();

                lista.Add(new UsuarioDTO
                {
                    Email = usuario.Email!,
                    Roles = roles

                });
            }


            return lista;

        }


      



        [AllowAnonymous]
        [HttpPost("registrar")]
        public async Task<ActionResult<RespuestaAutenticacionDTO>> Registrar(CredencialesUsuarioDTO credencialesUsuarioDTO)
        {



            var usuario = new IdentityUser
            {
                Email = credencialesUsuarioDTO.Email,
                UserName = credencialesUsuarioDTO.Email
            };

            var resultado = await userManager.CreateAsync(usuario, credencialesUsuarioDTO.Password);

            if (!resultado.Succeeded)
            {
                return BadRequest(resultado.Errors);
            }


            var perfil = new PerfilUsuario
            {
                UsuarioId = usuario.Id,
                Email = usuario.Email,
                DireccionEnvio = "", // o null
                Telefono = null,
                NombreCompleto = null
            };

            await dbContext.PerfilesUsuarios.AddAsync(perfil);
            await dbContext.SaveChangesAsync();
            
            await HacerCliente(credencialesUsuarioDTO);
            return await ConstruirToken(usuario);
        }


        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<RespuestaAutenticacionDTO>> Login(CredencialesUsuarioDTO credencialesUsuarioDTO)
        {
            var usuario = await userManager.FindByEmailAsync(credencialesUsuarioDTO.Email);

            if (usuario == null)
            {
                return BadRequest(new { mensaje = "Login Incorrecto" });
            }

            var resultado = await signInManager.CheckPasswordSignInAsync(usuario,
                credencialesUsuarioDTO.Password, lockoutOnFailure: false);


            if (!resultado.Succeeded)
            {

                return BadRequest(new { mensaje = "Error al logear" });
            }

            var perfil = await dbContext.PerfilesUsuarios.AnyAsync(x => x.UsuarioId == usuario.Id);

            if (!perfil)
            {
                var nuevoPerfil = new PerfilUsuario
                {
                    UsuarioId = usuario.Id,
                    Email = usuario.Email,
                    DireccionEnvio = "", // o null
                    Telefono = null,
                    NombreCompleto = null
                };
                await dbContext.PerfilesUsuarios.AddAsync(nuevoPerfil);
                await dbContext.SaveChangesAsync();

            }

            return await ConstruirToken(usuario);

        }

        //El token puede dar problemas por ser corto despues ojo*
        private async Task<RespuestaAutenticacionDTO> ConstruirToken(IdentityUser identityUser)
        {
            var claims = new List<Claim>
            {
              new Claim(ClaimTypes.NameIdentifier, identityUser.Id),

              //email = identityUser.Email
                new Claim("email",identityUser.Email!),

            };


            var claimsDB = await userManager.GetClaimsAsync(identityUser);


            //Agregando ClaimsDB A Claims
            claims.AddRange(claimsDB);

            var llave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes
                (configuration["llavejwt"]!));

            var creds = new SigningCredentials(llave, SecurityAlgorithms.HmacSha256);

            var expiracion = DateTime.UtcNow.AddYears(1);

            var tokenDeSeguridad = new JwtSecurityToken(issuer: null, audience: null,
                claims: claims, expires: expiracion, signingCredentials: creds);

            var token = new JwtSecurityTokenHandler().WriteToken(tokenDeSeguridad);

            return new RespuestaAutenticacionDTO
            {
                Token = token,
                Expiracion = expiracion
            };
        }


        //Agregar Roles


        [HttpPost("HacerAdmin")]

        //Cambiar despues para solo admins
        [AllowAnonymous]
        public async Task<IActionResult> HacerAdmin(EditarClaimDTO editarClaimDTO)
        {

            var usuario = await userManager.FindByEmailAsync(editarClaimDTO.email);

            if (usuario == null)
            {
                return NotFound(new { mensaje = $"No se encontro {usuario}" });
            }

            var claims = await userManager.GetClaimsAsync(usuario);

            var yaEsAdmin = claims.Any(x => x.Type == "Admin");

            if (yaEsAdmin)
            {
                return BadRequest(new { mensaje = "Ël usuario ya es admin" });
            }

            await userManager.AddClaimAsync(usuario, new Claim("Admin", "true"));


            return NoContent();

        }


        [HttpPost("HacerVendedor")]
        public async Task<IActionResult> HacerVendedor(EditarClaimDTO editarClaimDTO)
        {

            var usuario = await userManager.FindByEmailAsync(editarClaimDTO.email);

            if (usuario == null)
            {
                return NotFound(new { mensaje = $"No se encontro {usuario}" });
            }



            var claims = await userManager.GetClaimsAsync(usuario);

            var yaEsVendedor = claims.Any(x => x.Type == "Vendedor");

            if (yaEsVendedor)
            {
                return BadRequest(new { mensaje = "Ël usuario ya es vendedor" });
            }

            await userManager.AddClaimAsync(usuario, new Claim("Vendedor", "true"));

            return NoContent();

        }





        //Remover Roles 


        [HttpPost("RemoverAdmin")]
        public async Task<IActionResult> RemoverAdmin(EditarClaimDTO editarClaimDTO)
        {

            var usuario = await userManager.FindByEmailAsync(editarClaimDTO.email);

            if (usuario == null)
            {
                return NotFound(new { mensaje = $"No se encontro {usuario}" });
            }


            var claims = await userManager.GetClaimsAsync(usuario);

            var yaEsAdmin = claims.Any(x => x.Type == "Admin");

            if (!yaEsAdmin)
            {
                return BadRequest(new { mensaje = "Ël usuario no es admin" });
            }

            await userManager.RemoveClaimAsync(usuario, new Claim("Admin", "true"));

            return NoContent();

        }


        [HttpPost("RemoverVendedor")]
        public async Task<IActionResult> RemoverVendedor(EditarClaimDTO editarClaimDTO)
        {

            var usuario = await userManager.FindByEmailAsync(editarClaimDTO.email);

            if (usuario == null)
            {
                return NotFound(new { mensaje = $"No se encontro {usuario}" });
            }


            var claims = await userManager.GetClaimsAsync(usuario);

            var yaEsVendedor = claims.Any(x => x.Type == "Vendedor");

            if (!yaEsVendedor)
            {
                return BadRequest(new { mensaje = "Ël usuario ya no es vendedor" });
            }

            await userManager.RemoveClaimAsync(usuario, new Claim("Vendedor", "true"));

            return NoContent();

        }





        [AllowAnonymous]
        [HttpPost("google-login")]
        public async Task<ActionResult<RespuestaAutenticacionCompletaDTO>> GoogleLogin(
    [FromBody] GoogleLoginRequestDTO request)
        {
            try
            {
                // Validar token de Google
                var payload = await GoogleJsonWebSignature.ValidateAsync(
                    request.Token,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        //Solo aceptamos tokens para ClienteID
                        Audience = new[] { configuration["GoogleClientId"] }
                    });

                // Buscar usuario por email o GoogleId
                var user = await userManager.FindByEmailAsync(payload.Email);

                if (user == null)
                {
                    // Crear nuevo usuario si no existe
                    user = new IdentityUser
                    {
                        Email = payload.Email,
                        UserName = payload.Email,
                        EmailConfirmed = true // Google ya confirmó el email
                    };

                    var result = await userManager.CreateAsync(user);
                    if (!result.Succeeded)
                    {
                        return BadRequest("Error al crear usuario: " +
                            string.Join(", ", result.Errors.Select(e => e.Description)));
                    }

                    // Agregar claim de GoogleId para referencia futura
                    await userManager.AddClaimAsync(user, new Claim("GoogleId", payload.Subject));

                    // Hacerlo cliente automáticamente
                    await userManager.AddClaimAsync(user, new Claim("Cliente", "True"));
                }
                //Si ya existe en la base de datos
                else
                {
                    // Verificar si ya tiene claim de GoogleId
                    var existingClaims = await userManager.GetClaimsAsync(user);
                    if (!existingClaims.Any(c => c.Type == "GoogleId"))
                    {
                        await userManager.AddClaimAsync(user, new Claim("GoogleId", payload.Subject));
                    }
                }

                // Generar JWT (usa  método existente ConstruirToken)
                var authResponse = await ConstruirToken(user);

                // Obtener claims del usuario
                var claims = await userManager.GetClaimsAsync(user);
                var roles = claims.Where(c => c.Type == "Admin" || c.Type == "Vendedor" || c.Type == "Cliente")
                                 .Select(c => $"{c.Type}: {c.Value}")
                                 .ToList();

                var perfil = new PerfilUsuario
                {
                    UsuarioId = user.Id,
                    Email = user.Email,
                    DireccionEnvio = "", // o null
                    Telefono = null,
                    NombreCompleto = null
                };

                await dbContext.PerfilesUsuarios.AddAsync(perfil);
                await dbContext.SaveChangesAsync();


                return new RespuestaAutenticacionCompletaDTO
                {
                    Token = authResponse.Token, //El JWT
                    Expiracion = authResponse.Expiracion,
                    Usuario = new UserInfoDTO
                    {
                        Email = user.Email!,
                        Name = payload.Name ?? user.Email!.Split('@')[0],
                        Picture = payload.Picture, //Foto de Google
                        GoogleId = payload.Subject // Id de Google
                    }
                };
            }
            catch (InvalidJwtException ex)
            {
                logger.LogWarning("Token de Google inválido: {Message}", ex.Message);
                return Unauthorized(new { mensaje = "Token de Google inválido o expirado" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en Google Login");
                return StatusCode(500, "Error interno del servidor");
            }
        }


        //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        //[HttpGet("mi-perfil")]
        //public async Task<ActionResult<UserInfoDTO>> ObtenerMiPerfil()
        //{

        //    //Obteniendo el claim de email
        //    var emailClaim = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
        //    if (string.IsNullOrEmpty(emailClaim))
        //    {
        //        //si no hay claim email retornamos esto
        //        return Unauthorized();
        //    }

        //    //buscamos el email del usuario
        //    var user = await userManager.FindByEmailAsync(emailClaim);
        //    if (user == null)
        //    {
        //        return NotFound();
        //    }

        //    //obtenemos los claims del usuario
        //    var claims = await userManager.GetClaimsAsync(user);
        //    var googleId = claims.FirstOrDefault(c => c.Type == "GoogleId")?.Value;

        //    return new UserInfoDTO
        //    {
        //        Email = user.Email!,
        //        Name = user.UserName!,
        //        Picture = claims.FirstOrDefault(c => c.Type == "Picture")?.Value ?? string.Empty,
        //        GoogleId = googleId
        //    };
        //}



        //Fucion hacer Cliente automaticamente


        private async Task HacerCliente(CredencialesUsuarioDTO credencialesUsuarioDTO)
        {

            var usuario = await userManager.FindByEmailAsync(credencialesUsuarioDTO.Email);

            if (usuario == null)
            {
                throw new Exception("Usuario no encontrado");
            }

            //Evitar Duplicados
            var yaTieneClaim = (await userManager.GetClaimsAsync(usuario))
        .Any(c => c.Type == "Cliente");



            if (!yaTieneClaim)
            {
                await userManager.AddClaimAsync(usuario, new Claim("Cliente", "True"));
            }

        }








    }

}
