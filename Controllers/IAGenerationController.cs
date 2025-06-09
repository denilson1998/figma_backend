using figma_backend.Entities;
using figma_backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace figma_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IAGenerationController : ControllerBase
    {

        private const string ApiKeyDeepSeek = "sk-95d032d55713456e81414486eb0e2c13";

        private const string DeepSeekUrl = "https://api.deepseek.com";

        private const string Model = "deepseek-chat";

        private readonly IHttpClientFactory _httpClientFactory;

        public IAGenerationController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }


        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeSketch(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No se envió ningún archivo.");

            try
            {
                // 1. Convertir imagen a Base64
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var base64Image = Convert.ToBase64String(memoryStream.ToArray());

                // 2. Llamar a DeepSeek API
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKeyDeepSeek);

                var requestData = new
                {
                    model = Model,
                    messages = new[]
                    {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new {
                                type = "text",
                                text = "Analiza este boceto y devuelve un JSON con componentes UI. Formato: { type: 'text|input|button', content: string, width: number, position: { top: number, left: number }, properties?: { isPassword?: boolean } }"
                            },
                            new {
                                type = "image_url",
                                image_url = new { url = $"data:image/jpeg;base64,{base64Image}" }
                            }
                        }
                    }
                },
                    max_tokens = 2000
                };

                var response = await client.PostAsJsonAsync("https://api.deepseek.com/v1/chat/completions", requestData);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<DeepSeekResponse>(jsonResponse);

                // 3. Extraer y formatear respuesta
                var components = JsonSerializer.Deserialize<UIComponent[]>(
                    result.Choices[0].Message.Content
                );

                return Ok(new { components });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al procesar el boceto: {ex.Message}");
            }
        }

    }

    public class DeepSeekResponse
    {
        public List<DeepSeekChoice> Choices { get; set; }
    }

    public class DeepSeekChoice
    {
        public DeepSeekMessage Message { get; set; }
    }

    public class DeepSeekMessage
    {
        public string Content { get; set; }
    }

    public class UIComponent
    {
        public string Type { get; set; }
        public string Content { get; set; }
        public int Width { get; set; }
        public Position Position { get; set; }
        public ComponentProperties Properties { get; set; }
    }

    public class Position
    {
        public int Top { get; set; }
        public int Left { get; set; }
    }

    public class ComponentProperties
    {
        public bool IsPassword { get; set; }
    }
}
