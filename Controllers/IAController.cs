using EcommerceAPI.DTOs.IADTOs;
using EcommerceAPI.Servicios.IA;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    public class IAController : Controller
    {
        private readonly GeminiServicio _gemini;

        public IAController(GeminiServicio gemini)
        {
            _gemini = gemini;
        }

        [HttpPost("preguntar")]
        public async Task<IActionResult> Preguntar([FromBody] PreguntaDTO pregunta)
        {
            var respuesta = await _gemini.PreguntarIA(pregunta.Pregunta);
            return Ok(respuesta);
        }
    }
}
