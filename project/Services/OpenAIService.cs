using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;

namespace proje.Services
{
    public class OpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _openAiApiKey;
        private readonly string? _replicateApiKey;
        private readonly IConfiguration _configuration;
        private readonly string _apiUrl = "https://api.openai.com/v1/chat/completions";

        public OpenAIService(IConfiguration configuration, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _openAiApiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key not found in configuration.");
            _replicateApiKey = configuration["Replicate:ApiKey"];
            _configuration = configuration;
        }

        /// <summary>
        /// Fotoğrafı analiz edip kişiye özel detaylı açıklama oluşturur (GPT-4 Vision)
        /// </summary>
        private async Task<string> AnalyzePhotoAndCreateDescriptionAsync(string photoBase64)
        {
            try
            {
                var requestBody = new
                {
                    model = "gpt-4o",
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = new object[]
                            {
                                new
                                {
                                    type = "text",
                                    text = "Bu fotoğraftaki kişiyi ÇOK DETAYLI bir şekilde analiz et. Aşağıdaki TÜM özellikleri MUTLAKA belirt:\n\n" +
                                           "1. YÜZ ÖZELLİKLERİ: Yüz şekli (oval, yuvarlak, kare, uzun), göz rengi, göz şekli, kaş şekli ve rengi, burun şekli ve boyutu, dudak şekli, çene yapısı, yüz hatları, yüz ifadesi\n" +
                                           "2. SAÇ: Rengi (siyah, kahverengi, sarı, kızıl vb.), uzunluk, stil (kısa, orta, uzun, dalgalı, düz, kıvırcık), saç çizgisi, sakal/bıyık durumu\n" +
                                           "3. TEN RENGİ: Açık, orta, koyu, ton detayları\n" +
                                           "4. VÜCUT YAPISI: Boy tahmini, kilo tahmini, vücut tipi (zayıf, normal, kilolu, kaslı), omuz genişliği, bel genişliği, kol uzunluğu, bacak uzunluğu\n" +
                                           "5. DURUŞ: Pozisyon, omuz duruşu, kambur/doğru duruş\n" +
                                           "6. KIYAFET: Renk, stil, kesim\n" +
                                           "7. DİĞER: Yaş tahmini, etnik köken ipuçları, benzersiz özellikler (dövmeler, yara izleri vb.)\n\n" +
                                           "Bu bilgileri kullanarak AYNI KİŞİYİ egzersiz sonrası dönüştürülmüş halini oluşturmak için kullanacağız. Her detayı yaz, hiçbir şeyi atlama."
                                },
                                new
                                {
                                    type = "image_url",
                                    image_url = new
                                    {
                                        url = $"data:image/jpeg;base64,{photoBase64}"
                                    }
                                }
                            }
                        }
                    },
                    max_tokens = 1000
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_openAiApiKey}");

                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"GPT-4 Vision API hatası: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var responseJson = JsonDocument.Parse(responseContent);

                var description = responseJson.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return description ?? "Kişi analiz edilemedi.";
            }
            catch (Exception ex)
            {
                throw new Exception($"Fotoğraf analizi hatası: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// DALL-E ile egzersiz sonrası vücut transformasyon görüntüsü oluşturur (fotoğraf referanslı)
        /// </summary>
        /// <param name="exerciseProgram">Egzersiz programı açıklaması</param>
        /// <param name="currentBodyDescription">Mevcut vücut durumu açıklaması</param>
        /// <param name="duration">Program süresi (ay cinsinden)</param>
        /// <param name="photoBase64">Kullanıcının fotoğrafı (base64)</param>
        /// <returns>Oluşturulan görüntü URL'i</returns>
        public async Task<string> GenerateBodyTransformationImageAsync(string exerciseProgram, string currentBodyDescription, int duration = 3, string? photoBase64 = null)
        {
            try
            {
                string personDescription = currentBodyDescription;
                bool photoAnalyzed = false;

                // Eğer fotoğraf varsa, önce analiz et
                if (!string.IsNullOrEmpty(photoBase64))
                {
                    try
                    {
                        personDescription = await AnalyzePhotoAndCreateDescriptionAsync(photoBase64);
                        photoAnalyzed = true;
                    }
                    catch (Exception ex)
                    {
                        // Fotoğraf analizi başarısız olursa, mevcut açıklamayı kullan
                        System.Diagnostics.Debug.WriteLine($"Fotoğraf analizi hatası: {ex.Message}");
                    }
                }

                // Fotoğraf analiz edildiyse çok daha detaylı ve spesifik prompt kullan
                var prompt = photoAnalyzed
                    ? $"Create a realistic before and after body transformation comparison image. " +
                      $"\n\nLEFT PANEL (BEFORE - CURRENT STATE):\n" +
                      $"Show THIS SPECIFIC PERSON exactly as described: {personDescription}\n" +
                      $"This person is about to start the exercise program: {exerciseProgram}\n" +
                      $"\n\nRIGHT PANEL (AFTER - {duration} MONTHS LATER):\n" +
                      $"Show the EXACT SAME IDENTICAL PERSON after {duration} months of consistent training with: {exerciseProgram}\n" +
                      $"\n\nCRITICAL REQUIREMENTS - The person MUST be IDENTICAL in both panels:\n" +
                      $"- SAME EXACT FACE: All facial features from the description must match exactly - same face shape, same eye color and shape, same nose, same mouth, same jawline, same facial structure\n" +
                      $"- SAME EXACT HAIR: Exact same hair color, exact same hair style, exact same length, exact same texture as described\n" +
                      $"- SAME EXACT SKIN TONE: Exact same skin color and complexion\n" +
                      $"- SAME EXACT HEIGHT: Same height, same body proportions, same frame\n" +
                      $"- SAME EXACT AGE: Same age appearance\n" +
                      $"- SAME EXACT PERSONALITY FEATURES: Any unique features, birthmarks, or distinctive characteristics must be identical\n" +
                      $"\nONLY DIFFERENCE between left and right:\n" +
                      $"- Body composition: More muscle mass, better muscle definition, less body fat\n" +
                      $"- Posture: Improved, more confident stance\n" +
                      $"- Overall fitness: More athletic appearance\n" +
                      $"\nThe transformation must be realistic and show the SAME PERSON with improved fitness. " +
                      $"Style: Professional fitness photography, side-by-side comparison, natural lighting, realistic proportions, IDENTICAL person in both panels."
                    : $"Create a realistic before and after body transformation image showing a person who has been doing the following exercise program for {duration} months: {exerciseProgram}. " +
                      $"Current body description: {currentBodyDescription}. " +
                      $"Show a side-by-side comparison with the person on the left showing their current physique and on the right showing their transformed, fitter, more muscular and toned physique after {duration} months of consistent training. " +
                      $"The transformation should be realistic and motivational, showing visible muscle definition, improved posture, and overall fitness improvement. " +
                      $"Style: professional fitness photography, natural lighting, realistic proportions.";

                var requestBody = new
                {
                    model = "dall-e-3",
                    prompt = prompt,
                    n = 1,
                    size = "1024x1024",
                    quality = "standard"
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_openAiApiKey}");

                var response = await _httpClient.PostAsync("https://api.openai.com/v1/images/generations", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"DALL-E API hatası: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var responseJson = JsonDocument.Parse(responseContent);

                var imageUrl = responseJson.RootElement
                    .GetProperty("data")[0]
                    .GetProperty("url")
                    .GetString();

                return imageUrl ?? throw new Exception("Görüntü URL'i alınamadı.");
            }
            catch (Exception ex)
            {
                throw new Exception($"DALL-E görüntü oluşturma hatası: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Stable Diffusion ile image-to-image transformasyon görüntüsü oluşturur (fotoğraf referanslı)
        /// Replicate API kullanır - fotoğrafı doğrudan referans olarak kullanabilir
        /// </summary>
        /// <param name="photoBase64">Kullanıcının fotoğrafı (base64)</param>
        /// <param name="exerciseProgram">Egzersiz programı açıklaması</param>
        /// <param name="duration">Program süresi (ay cinsinden)</param>
        /// <returns>Oluşturulan görüntü URL'i</returns>
        public async Task<string> GenerateBodyTransformationWithStableDiffusionAsync(string photoBase64, string exerciseProgram, int duration = 3)
        {
            if (string.IsNullOrEmpty(_replicateApiKey))
            {
                throw new InvalidOperationException("Replicate API key not found. Please add 'Replicate:ApiKey' to appsettings.json");
            }

            try
            {
                // Prompt oluştur - before/after karşılaştırması için
                var prompt = $"A realistic before and after body transformation comparison, side by side. " +
                            $"LEFT SIDE: The exact person from the uploaded photo with their current physique, same face, same hair, same appearance. " +
                            $"RIGHT SIDE: The EXACT SAME IDENTICAL PERSON after {duration} months of consistent training with this exercise program: {exerciseProgram}. " +
                            $"The person must be IDENTICAL in both sides - same exact face features, same hair color and style, same skin tone, same height, same body frame. " +
                            $"ONLY the body composition changes: more muscle definition, less body fat, improved posture, better fitness, more athletic build. " +
                            $"Professional fitness photography, natural lighting, realistic transformation, same person identity maintained.";

                var negativePrompt = "blurry, distorted, different person, wrong face, unrealistic proportions, cartoon style, anime style, painting, artwork, deformed, ugly, bad anatomy";

                // Replicate API - Stable Diffusion img2img model
                // Model: stability-ai/sdxl veya fofr/stable-diffusion-xl-img2img
                var predictionRequest = new
                {
                    version = "39ed52f2a78e934b3ba6e2a89f5b1c712de7dfea535525255b1aa35c5565e08b", // fofr/stable-diffusion-xl-img2img
                    input = new Dictionary<string, object>
                    {
                        { "image", $"data:image/jpeg;base64,{photoBase64}" },
                        { "prompt", prompt },
                        { "negative_prompt", negativePrompt },
                        { "num_outputs", 1 },
                        { "guidance_scale", 7.5 },
                        { "num_inference_steps", 50 },
                        { "strength", 0.75 }, // 0.75 = %75 transformasyon, %25 orijinal korunur
                        { "scheduler", "K_EULER" }
                    }
                };

                var json = JsonSerializer.Serialize(predictionRequest, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {_replicateApiKey}");
                _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");

                // Prediction oluştur
                var createResponse = await _httpClient.PostAsync("https://api.replicate.com/v1/predictions", content);
                
                if (!createResponse.IsSuccessStatusCode)
                {
                    var errorContent = await createResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Replicate API hatası: {createResponse.StatusCode} - {errorContent}");
                }

                var createResponseContent = await createResponse.Content.ReadAsStringAsync();
                var createResponseJson = JsonDocument.Parse(createResponseContent);
                
                var predictionId = createResponseJson.RootElement.GetProperty("id").GetString();
                var statusUrl = createResponseJson.RootElement.GetProperty("urls").GetProperty("get").GetString();

                if (string.IsNullOrEmpty(predictionId) || string.IsNullOrEmpty(statusUrl))
                {
                    throw new Exception("Prediction ID veya status URL alınamadı.");
                }

                // Prediction'ın tamamlanmasını bekle (polling)
                string? imageUrl = null;
                int maxAttempts = 90; // Maksimum 90 deneme (yaklaşık 3 dakika)
                int attempt = 0;

                while (attempt < maxAttempts)
                {
                    await Task.Delay(2000); // 2 saniye bekle

                    var statusResponse = await _httpClient.GetAsync(statusUrl);
                    if (!statusResponse.IsSuccessStatusCode)
                    {
                        throw new Exception($"Status kontrolü hatası: {statusResponse.StatusCode}");
                    }

                    var statusContent = await statusResponse.Content.ReadAsStringAsync();
                    var statusJson = JsonDocument.Parse(statusContent);
                    
                    var status = statusJson.RootElement.GetProperty("status").GetString();

                    if (status == "succeeded")
                    {
                        var output = statusJson.RootElement.GetProperty("output");
                        if (output.ValueKind == JsonValueKind.Array && output.GetArrayLength() > 0)
                        {
                            imageUrl = output[0].GetString();
                        }
                        else if (output.ValueKind == JsonValueKind.String)
                        {
                            imageUrl = output.GetString();
                        }
                        break;
                    }
                    else if (status == "failed" || status == "canceled")
                    {
                        var error = statusJson.RootElement.TryGetProperty("error", out var errorElement) 
                            ? errorElement.GetString() 
                            : "Bilinmeyen hata";
                        throw new Exception($"Prediction başarısız: {error}");
                    }
                    // "starting" veya "processing" durumlarında devam et

                    attempt++;
                }

                if (string.IsNullOrEmpty(imageUrl))
                {
                    throw new Exception("Görüntü oluşturma zaman aşımına uğradı.");
                }

                return imageUrl;
            }
            catch (Exception ex)
            {
                throw new Exception($"Stable Diffusion görüntü oluşturma hatası: {ex.Message}", ex);
            }
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
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_openAiApiKey}");

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

