
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EcommerceAPI.Servicios.ServicioUsuarios
{

    public class ServicioUsuarios : IServicioUsuarios
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly UserManager<IdentityUser> userManager;
        private readonly IConfiguration configuration;

        public ServicioUsuarios(IHttpContextAccessor httpContextAccessor,
            UserManager<IdentityUser> userManager,IConfiguration configuration)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.userManager = userManager;
            this.configuration = configuration;
        }

        public async Task<string> ObtenerUsuarioId()
        {



            var email = httpContextAccessor.HttpContext!.User
                //2 types uno para usuario local y otro para externo ej == google
                .Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email || x.Type == "email")!.Value;

            // Si no hay usuario autenticado, devuelve un Id fijo para pruebas
            if (string.IsNullOrEmpty(email))
            {
                throw new ApplicationException("No esta autenticado");


            }


            if (email == null)
            {
                throw new ApplicationException("No esta autenticado");
            }

            var usuario = await userManager.FindByEmailAsync(email);

            if (usuario == null)
            {
                throw new ApplicationException("No se encontro ese correo");

            }

            return usuario!.Id;
        }

       

    }
}
