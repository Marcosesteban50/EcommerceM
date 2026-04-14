using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace EcommerceAPI.Servicios.IA
{
    public class GeminiServicio
    {

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly string _systemPrompt = @"Eres el asistente inteligente de EcommerceApp. Tu función es ayudar a los usuarios únicamente con temas relacionados con la tienda en línea.

Puedes asistir en:

Productos

Información de productos

Características, precios y disponibilidad

Recomendaciones de productos

Pedidos

Estado de pedidos

Historial de compras

Problemas con pedidos

Pagos

Métodos de pago disponibles

Problemas al pagar

Confirmación de pagos

Envíos

Información de envíos

Tiempo estimado de entrega

Costos de envío

Cuenta de usuario

Registro

Inicio de sesión

Gestión de cuenta

Carrito de compras

Agregar o quitar productos

Revisar carrito

Proceder al checkout

Si el usuario pregunta algo que no esté relacionado con la tienda, productos,
pedidos o servicios del ecommerce, 
responde educadamente que solo puedes ayudar con temas relacionados con EcommerceApp.";

        private HttpClient? httpClient;
        private IConfiguration? config;

        public GeminiServicio(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }



        public async Task<string> PreguntarIA(string pregunta)
        {

            //Llave
            var apiKey = _config["Gemini:ApiKey"];


            //Url de Gemini IA
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";




            //Body de mensaje a la IA en JSON
            var body = new
            {
                contents = new[]
             {
        // Respuesta Sistema
        new
        {
            role = "user",
            parts = new[] { new { text = _systemPrompt } }
        },
        //Respuesta de Ia ante peticion del prompt forzamos al modelo de IA  a aceptar las reglas del prompt
        new
        {

            role = "model",
            parts = new[] { new { text = "Entendido. Solo asistiré con las funciones asignadas del prompt." } }
        },
        // 2. La pregunta real del usuario
        new
        {
            //Mandamos lo que escribio el usuario
            role = "user",
            parts = new[] { new { text = pregunta } }
        }
    }
            };

            //Convirtiendo a JsonString el Body
            var json = JsonSerializer.Serialize(body);

            //Enviando la peticion
            var response = await _httpClient.PostAsync(
                url,
                new StringContent(json, Encoding.UTF8, "application/json")
            );

            //Leyendo la respuesta como string asyncronamente
            var result = await response.Content.ReadAsStringAsync();
            Console.WriteLine(result);

            //Convirtiendo a Json document  el resultado
            using var doc = JsonDocument.Parse(result);

            // verificando si gemini respondio sin errores
            if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                return candidates[0]
                    //mensajes de chat = content
                    .GetProperty("content")
                    //Contenido del mensaje = parts
                    .GetProperty("parts")[0]
                    //Mensaje como tal
                    .GetProperty("text")
                    //Convertimos estos JSOn a string
                    .GetString()!;
            }

            return "No response generated or content was blocked.";
        }

    }


}
