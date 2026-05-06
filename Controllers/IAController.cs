using EcommerceAPI.DTOs.IADTOs;
using EcommerceAPI.Servicios.IA;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    public class IAController : Controller
    {
        // Servicio donde está la lógica para hablar con Gemini
        private readonly GeminiServicio _gemini;

        public IAController(GeminiServicio gemini)
        {
            _gemini = gemini;
        }

        // Endpoint para que Angular pueda enviar una pregunta a la IA
        [HttpPost("preguntar")]
        public async Task<IActionResult> Preguntar([FromBody] PreguntaDTO pregunta)
        {
            // Mandamos la pregunta al servicio de Gemini y esperamos la respuesta
            var respuesta = await _gemini.PreguntarIA(pregunta.Pregunta);

            // Retornamos la respuesta al frontend
            return Ok(respuesta);
        }
    }
}