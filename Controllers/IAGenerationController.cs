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

        private const string OpenAIApiKey = "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";

        private const string ChatGPTUrl = "https://api.openai.com/v1/chat/completions";

        private readonly IHttpClientFactory _httpClientFactory;

        public IAGenerationController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }


        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeSketch(IFormFile    file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No se envió ningún archivo.");

            try
            {
                // Convertir imagen a Base64
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var base64Image = Convert.ToBase64String(memoryStream.ToArray());

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", OpenAIApiKey);

                var requestData = new
                {
                    model = "gpt-4o",
                    messages = new object[]
                    {
                new {
                    role = "user",
                    content = new object[]
                    {
                        new {
                            type = "text",
                            text = "Analiza este boceto y devuelve un JSON con componentes UI. Formato: { type: 'text|input|button', content: string, width: number, position: { top: number, left: number }, properties?: { isPassword?: boolean } }"
                        },
                        new {
                            type = "image_url",
                            image_url = new {
                                url = $"data:image/jpeg;base64,{base64Image}"
                            }
                        }
                    }
                }
                    },
                    max_tokens = 2000
                };

                var response = await client.PostAsJsonAsync(ChatGPTUrl, requestData);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<OpenAIResponse>(jsonResponse, options);

                if (result?.Choices == null || result.Choices.Count == 0)
                {
                    return BadRequest("No se recibió una respuesta válida de la API.");
                }

                var jsonComponentsString = result.Choices[0].Message.Content
                   .Replace("```json", "")
                   .Replace("```", "")
                   .Trim();

                var components = JsonSerializer.Deserialize<UIComponent[]>(jsonComponentsString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return Ok(new { components });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al procesar el boceto: {ex.Message}");
            }
        }

        [HttpPost("generate-from-prompt")]
        public async Task<IActionResult> GenerateFromPrompt([FromBody] PromptRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
                return BadRequest("El prompt está vacío.");

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", OpenAIApiKey);

                var requestData = new
                {
                    model = "gpt-4o",
                    messages = new[]
                    {
                new
                {
                    role = "user",
                    content = request.Prompt +
                    "\nDevuelve solo un JSON con formato: " +
                    "[{ type: 'text|input|button', content: string, width: number, height: number, positionX: number, positionY: number }]"
                }
            },
                    max_tokens = 1500
                };

                var response = await client.PostAsJsonAsync(ChatGPTUrl, requestData);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<OpenAIResponse>(jsonResponse, options);

                if (result?.Choices == null || result.Choices.Count == 0)
                {
                    return BadRequest("La respuesta de la IA está vacía.");
                }

                var jsonComponentsString = result.Choices[0].Message.Content
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                int start = jsonComponentsString.IndexOf('[');
                int end = jsonComponentsString.LastIndexOf(']');
                if (start >= 0 && end > start)
                {
                    jsonComponentsString = jsonComponentsString.Substring(start, end - start + 1);
                }
                else
                {
                    return BadRequest("No se pudo encontrar un array JSON válido en la respuesta.");
                }

                var rawComponents = JsonSerializer.Deserialize<List<RawComponent>>(jsonComponentsString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var components = rawComponents.Select(rc => new UIComponent
                {
                    Type = rc.Type,
                    Content = rc.Content,
                    Width = rc.Width,
                    Position = new Position
                    {
                        Top = rc.PositionY,
                        Left = rc.PositionX
                    },
                    Properties = new ComponentProperties
                    {
                        IsPassword = rc.Content.ToLower().Contains("contraseña")
                    }
                }).ToArray();

                return Ok(new { components });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al generar componentes desde prompt: {ex.Message}");
            }
        }



        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", OpenAIApiKey); 

            try
            {
                var response = await client.GetAsync("https://api.openai.com/v1/models");
                var content = await response.Content.ReadAsStringAsync();
                return Ok(content);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error de conexión: {ex.Message}");
            }
        }



    }




    public class OpenAIResponse
    {
        public string Id { get; set; }
        public string Object { get; set; }
        public long Created { get; set; }
        public string Model { get; set; }
        public List<Choice> Choices { get; set; }
        public Usage Usage { get; set; }
        public string SystemFingerprint { get; set; }
    }

    public class Choice
    {
        public int Index { get; set; }
        public Message Message { get; set; }
        public object Logprobs { get; set; }
        public string FinishReason { get; set; }
    }

    public class Message
    {
        public string Role { get; set; }
        public string Content { get; set; }
        public object Refusal { get; set; }
        public List<object> Annotations { get; set; }
    }

    public class Usage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
        public PromptTokensDetails PromptTokensDetails { get; set; }
        public CompletionTokensDetails CompletionTokensDetails { get; set; }
    }

    public class PromptTokensDetails
    {
        public int CachedTokens { get; set; }
        public int AudioTokens { get; set; }
    }

    public class CompletionTokensDetails
    {
        public int ReasoningTokens { get; set; }
        public int AudioTokens { get; set; }
        public int AcceptedPredictionTokens { get; set; }
        public int RejectedPredictionTokens { get; set; }
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

    public class PromptRequest
    {
        public string Prompt { get; set; }
    }

    public class RawComponent
    {
        public string Type { get; set; }
        public string Content { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int PositionX { get; set; }
        public int PositionY { get; set; }
    }
}
