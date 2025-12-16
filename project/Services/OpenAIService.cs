using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace proje.Services
{
    public class OpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiUrl = "https://api.openai.com/v1/chat/completions";

        public OpenAIService(IConfiguration configuration, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key not found in configuration.");
        }

        /// <summary>
        /// OpenAI API'sine prompt gönderir ve yanıt alır
        /// </summary>
        public async Task<string> GenerateNutritionPlanAsync(string prompt)
        {
            try
            {
                var requestBody = new
                {
                    model = "gpt-4o-mini", // Daha ekonomik model
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = "Sen profesyonel bir beslenme uzmanısın. Kullanıcılara detaylı, kişiselleştirilmiş beslenme programları oluşturuyorsun. Türkçe yanıt veriyorsun ve Türk mutfağına uygun öneriler sunuyorsun."
                        },
                        new
                        {
                            role = "user",
                            content = prompt
                        }
                    },
                    temperature = 0.7,
                    max_tokens = 2000
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                var response = await _httpClient.PostAsync(_apiUrl, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"OpenAI API hatası: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var responseJson = JsonDocument.Parse(responseContent);

                var aiResponse = responseJson.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return aiResponse ?? "Yanıt alınamadı.";
            }
            catch (Exception ex)
            {
                throw new Exception($"OpenAI API hatası: {ex.Message}", ex);
            }
        }
    }
}

