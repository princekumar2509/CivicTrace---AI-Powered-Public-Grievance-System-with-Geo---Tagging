using System.Text;
using System.Text.Json;

namespace PublicGrievanceSystem.Services
{
    public class AIImageAnalyzer
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public AIImageAnalyzer(
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<ImageAnalysisResult> AnalyzeImageAsync(
            byte[] imageBytes,
            string mimeType,
            CancellationToken cancellationToken = default)
        {
            string? apiKey =
                _configuration["Gemini:ApiKey"];

            string primaryModel =
                _configuration["Gemini:Model"]
                ?? "gemini-3.6-flash";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new Exception(
                    "Gemini API key is not configured.");
            }

            string base64Image =
                Convert.ToBase64String(imageBytes);

            string prompt = """
                Analyze this image for a public grievance reporting system.

                Identify the main civic or public issue shown in the image.

                Choose ONLY ONE category from:

                Road
                Garbage
                Streetlight
                Electricity
                Water
                Drainage
                General

                Return ONLY valid JSON in this exact format:

                {
                  "category": "Road",
                  "confidence": 0.95,
                  "description": "A damaged road with a visible pothole."
                }

                Rules:
                - category must be one of the listed categories.
                - confidence must be between 0 and 1.
                - description should briefly explain what is visible.
                - Do not include markdown.
                - Do not include extra text.
                """;

            // =====================================================
            // FIRST MODEL
            // =====================================================

            try
            {
                return await AnalyzeWithModelAsync(
                    primaryModel,
                    apiKey,
                    base64Image,
                    mimeType,
                    prompt,
                    cancellationToken);
            }
            catch (GeminiTransientException)
            {
                // Primary model was temporarily unavailable.
                // Try the fallback model.
            }


            // =====================================================
            // FALLBACK MODEL
            // =====================================================

            const string fallbackModel =
                "gemini-3.5-flash";

            return await AnalyzeWithModelAsync(
                fallbackModel,
                apiKey,
                base64Image,
                mimeType,
                prompt,
                cancellationToken);
        }


        // =========================================================
        // SEND REQUEST TO A SPECIFIC GEMINI MODEL
        // =========================================================

        private async Task<ImageAnalysisResult> AnalyzeWithModelAsync(
            string model,
            string apiKey,
            string base64Image,
            string mimeType,
            string prompt,
            CancellationToken cancellationToken)
        {
            var requestBody = new
{
    contents = new[]
    {
        new
        {
            parts = new object[]
            {
                new
                {
                    text = prompt
                },
                new
                {
                    inline_data = new
                    {
                        mime_type = mimeType,
                        data = base64Image
                    }
                }
            }
        }
    },

    generationConfig = new
    {
        responseMimeType = "application/json"
    }
};

            string json =
                JsonSerializer.Serialize(requestBody);


            // =====================================================
            // RETRY UP TO 3 TIMES
            // =====================================================

            int maxAttempts = 3;

            for (int attempt = 1;
                 attempt <= maxAttempts;
                 attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var request =
                    new HttpRequestMessage(
                        HttpMethod.Post,
                        $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent");

                request.Headers.Add(
                    "x-goog-api-key",
                    apiKey);

                request.Content =
                    new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json");


                using HttpResponseMessage response =
                    await _httpClient.SendAsync(
                        request,
                        cancellationToken);


                string responseText =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);


                // =================================================
                // SUCCESS
                // =================================================

                if (response.IsSuccessStatusCode)
                {
                    return ParseGeminiResponse(
                        responseText);
                }


                // =================================================
                // TEMPORARY ERROR
                // =================================================

                bool shouldRetry =
                    response.StatusCode == System.Net.HttpStatusCode.RequestTimeout ||
                    response.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                    response.StatusCode == System.Net.HttpStatusCode.InternalServerError ||
                    response.StatusCode == System.Net.HttpStatusCode.BadGateway ||
                    response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                    response.StatusCode == System.Net.HttpStatusCode.GatewayTimeout;


                if (!shouldRetry)
                {
                    throw new Exception(
                        $"Gemini API error: {responseText}");
                }


                // =================================================
                // LAST ATTEMPT
                // =================================================

                if (attempt == maxAttempts)
                {
                    throw new GeminiTransientException(
                        $"Gemini model '{model}' is temporarily unavailable. " +
                        $"Last response: {responseText}");
                }


                // =================================================
                // EXPONENTIAL BACKOFF
                //
                // Attempt 1 → wait 2 seconds
                // Attempt 2 → wait 4 seconds
                // =================================================

                int delaySeconds =
                    (int)Math.Pow(2, attempt);

                await Task.Delay(
                    TimeSpan.FromSeconds(delaySeconds),
                    cancellationToken);
            }


            throw new GeminiTransientException(
                $"Gemini model '{model}' could not process the image.");
        }


        // =========================================================
        // PARSE GEMINI RESPONSE
        // =========================================================

        private ImageAnalysisResult ParseGeminiResponse(
    string responseText)
{
    using JsonDocument document =
        JsonDocument.Parse(responseText);

    string resultText =
        document.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString()
            ?? "";

    resultText = ExtractJsonObject(resultText);

    var result =
        JsonSerializer.Deserialize<ImageAnalysisResult>(
            resultText,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

    if (result == null)
    {
        throw new Exception(
            "Gemini returned an empty analysis result.");
    }

    return result;
}


        // =========================================================
        // REMOVE MARKDOWN CODE FENCES
        // =========================================================

        private string ExtractJsonObject(string text)
{
    text = text.Trim();

    // Remove markdown code fences if Gemini adds them
    if (text.StartsWith("```"))
    {
        int firstNewLine =
            text.IndexOf('\n');

        if (firstNewLine >= 0)
        {
            text =
                text[(firstNewLine + 1)..];
        }

        int lastFence =
            text.LastIndexOf("```");

        if (lastFence >= 0)
        {
            text =
                text[..lastFence];
        }

        text = text.Trim();
    }

    // Find the JSON object
    int firstBrace =
        text.IndexOf('{');

    int lastBrace =
        text.LastIndexOf('}');

    if (firstBrace < 0 || lastBrace < 0)
    {
        throw new Exception(
            "Gemini response did not contain a valid JSON object.");
    }

    // Extract only the JSON object
    return text[
        firstBrace..(lastBrace + 1)
    ].Trim();
}
    }


    // =============================================================
    // GEMINI RESULT MODEL
    // =============================================================

    public class ImageAnalysisResult
    {
        public string Category { get; set; } = "General";

        public double Confidence { get; set; }

        public string Description { get; set; } = "";
    }


    // =============================================================
    // TEMPORARY GEMINI SERVICE-UNAVAILABLE EXCEPTION
    // =============================================================

    public class GeminiTransientException : Exception
    {
        public GeminiTransientException(string message)
            : base(message)
        {
        }
    }
}