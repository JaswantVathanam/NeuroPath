using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AdaptiveCognitiveRehabilitationPlatform.Services
{
    /// <summary>
    /// AI Service using Phi-4 SLM via Ollama for intelligent difficulty scaling
    /// Phi-4 is Microsoft's latest efficient language model optimized for reasoning tasks
    /// </summary>
    public class OllamaAIDifficultyService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OllamaAIDifficultyService> _logger;
        private const string OLLAMA_API_URL = "http://localhost:11434/api/generate";
        private const string MODEL_NAME = "phi4"; // or "phi", "mistral", "neural-chat" as fallbacks

        public OllamaAIDifficultyService(HttpClient httpClient, ILogger<OllamaAIDifficultyService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public class PerformanceData
        {
            public int Accuracy { get; set; } // 0-100
            public int ReactionTime { get; set; } // milliseconds
            public int CompletionTime { get; set; } // seconds
            public int ConsecutiveCorrect { get; set; }
            public int TotalAttempts { get; set; }
            public int CurrentDifficulty { get; set; }
        }

        public class AIDifficultyRecommendation
        {
            public int NewDifficulty { get; set; }
            public string? Recommendation { get; set; }
            public double ConfidenceScore { get; set; } // 0-1
            public bool ShouldIncrease { get; set; }
            public bool ShouldDecrease { get; set; }
            public string? Analysis { get; set; }
            public string? PersonalizedMessage { get; set; }
        }

        /// <summary>
        /// Get AI-driven difficulty recommendation using Phi-4
        /// </summary>
        public async Task<AIDifficultyRecommendation> GetAIDifficultyRecommendationAsync(PerformanceData performance)
        {
            try
            {
                // Build prompt for Phi-4
                var prompt = BuildPerformanceAnalysisPrompt(performance);
                
                _logger.LogInformation($"Sending performance analysis to Phi-4...\nPrompt: {prompt}");

                // Call Ollama API with Phi-4
                var llmResponse = await CallOllamaPhiAsync(prompt);
                
                _logger.LogInformation($"Phi-4 Response: {llmResponse}");

                // Parse LLM response and extract recommendation
                var recommendation = ParseLLMResponse(llmResponse, performance);
                
                return recommendation;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calling Phi-4: {ex.Message}. Falling back to rule-based system.");
                // Fallback to rule-based difficulty scaling
                return GetFallbackRecommendation(performance);
            }
        }

        /// <summary>
        /// Build a detailed prompt for Phi-4 to analyze player performance
        /// </summary>
        private string BuildPerformanceAnalysisPrompt(PerformanceData performance)
        {
            return $@"You are an AI-powered cognitive rehabilitation assistant. Analyze the player's performance and recommend difficulty adjustment.

PERFORMANCE METRICS:
- Accuracy: {performance.Accuracy}%
- Reaction Time: {performance.ReactionTime}ms average
- Completion Time: {performance.CompletionTime} seconds
- Consecutive Correct: {performance.ConsecutiveCorrect}
- Total Attempts: {performance.TotalAttempts}
- Current Difficulty: {performance.CurrentDifficulty}/5 (1=Beginner, 5=Expert)

ANALYSIS REQUIRED:
1. Is the player performing well (accuracy > 85% AND reaction time < 2000ms)?
2. Is the player struggling (accuracy < 50% OR reaction time > 3000ms)?
3. What is the player's consistency level?
4. Should difficulty be INCREASED, MAINTAINED, or DECREASED?

RESPOND IN THIS EXACT JSON FORMAT (no markdown, pure JSON):
{{
  ""difficulty"": <number 1-5>,
  ""increase"": <true/false>,
  ""decrease"": <true/false>,
  ""confidence"": <0.0-1.0>,
  ""analysis"": ""<brief technical analysis>"",
  ""message"": ""<encouraging personalized message for the player>""
}}

Respond ONLY with valid JSON, no other text.";
        }

        /// <summary>
        /// Call Ollama API with Phi-4 model
        /// </summary>
        private async Task<string> CallOllamaPhiAsync(string prompt)
        {
            try
            {
                var request = new
                {
                    model = MODEL_NAME,
                    prompt = prompt,
                    stream = false,
                    temperature = 0.3, // Lower temperature for consistent decisions
                    top_p = 0.9,
                    top_k = 40
                };

                var jsonContent = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                // Set timeout for Ollama call
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30));
                var response = await _httpClient.PostAsync(OLLAMA_API_URL, content, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Ollama API returned {response.StatusCode}: {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Parse Ollama response
                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;
                var generatedText = root.GetProperty("response").GetString() ?? "";

                return generatedText;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error connecting to Ollama: {ex.Message}. Is Ollama running on localhost:11434?");
                throw;
            }
        }

        /// <summary>
        /// Parse LLM response and extract difficulty recommendation
        /// </summary>
        private AIDifficultyRecommendation ParseLLMResponse(string llmResponse, PerformanceData performance)
        {
            try
            {
                // Extract JSON from response (in case there's extra text)
                var jsonStart = llmResponse.IndexOf("{");
                var jsonEnd = llmResponse.LastIndexOf("}");

                if (jsonStart < 0 || jsonEnd < 0)
                {
                    _logger.LogWarning("Could not find JSON in Phi-4 response. Using fallback.");
                    return GetFallbackRecommendation(performance);
                }

                var jsonString = llmResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                int newDifficulty = root.TryGetProperty("difficulty", out var diffElem) 
                    ? diffElem.GetInt32() 
                    : performance.CurrentDifficulty;

                bool shouldIncrease = root.TryGetProperty("increase", out var incElem) 
                    ? incElem.GetBoolean() 
                    : false;

                bool shouldDecrease = root.TryGetProperty("decrease", out var decElem) 
                    ? decElem.GetBoolean() 
                    : false;

                double confidence = root.TryGetProperty("confidence", out var confElem) 
                    ? confElem.GetDouble() 
                    : 0.5;

                string analysis = root.TryGetProperty("analysis", out var analysisElem) 
                    ? analysisElem.GetString() ?? "Performance analysis by Phi-4" 
                    : "Performance analysis by Phi-4";

                string personalizedMessage = root.TryGetProperty("message", out var msgElem) 
                    ? msgElem.GetString() ?? "Keep up the great work!" 
                    : "Keep up the great work!";

                // Validate difficulty is in range
                newDifficulty = Math.Max(1, Math.Min(5, newDifficulty));
                confidence = Math.Max(0, Math.Min(1, confidence));

                return new AIDifficultyRecommendation
                {
                    NewDifficulty = newDifficulty,
                    ShouldIncrease = shouldIncrease,
                    ShouldDecrease = shouldDecrease,
                    ConfidenceScore = confidence,
                    Analysis = analysis,
                    PersonalizedMessage = personalizedMessage,
                    Recommendation = shouldIncrease 
                        ? $"Ready for Level {newDifficulty}! {personalizedMessage}"
                        : shouldDecrease 
                        ? $"Let's practice at Level {newDifficulty}. {personalizedMessage}"
                        : $"Keep challenging yourself at Level {newDifficulty}. {personalizedMessage}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error parsing Phi-4 response: {ex.Message}. Using fallback.");
                return GetFallbackRecommendation(performance);
            }
        }

        /// <summary>
        /// Fallback rule-based recommendation when Ollama is unavailable
        /// </summary>
        private AIDifficultyRecommendation GetFallbackRecommendation(PerformanceData performance)
        {
            var recommendation = new AIDifficultyRecommendation();

            // Rule-based logic
            double performanceScore = (performance.Accuracy * 0.6) + 
                                     ((Math.Max(0, 3000 - performance.ReactionTime) / 3000) * 100 * 0.4);

            if (performanceScore > 75 && performance.CurrentDifficulty < 5)
            {
                recommendation.NewDifficulty = performance.CurrentDifficulty + 1;
                recommendation.ShouldIncrease = true;
                recommendation.PersonalizedMessage = $"Excellent work! You're ready to advance to Level {recommendation.NewDifficulty}.";
            }
            else if (performanceScore < 40 && performance.CurrentDifficulty > 1)
            {
                recommendation.NewDifficulty = performance.CurrentDifficulty - 1;
                recommendation.ShouldDecrease = true;
                recommendation.PersonalizedMessage = $"Let's practice at Level {recommendation.NewDifficulty} to build confidence.";
            }
            else
            {
                recommendation.NewDifficulty = performance.CurrentDifficulty;
                recommendation.PersonalizedMessage = $"You're doing well at Level {recommendation.NewDifficulty}. Keep practicing!";
            }

            recommendation.ConfidenceScore = Math.Min(performanceScore / 100, 1.0);
            recommendation.Analysis = "Using rule-based fallback (Ollama unavailable)";
            recommendation.Recommendation = recommendation.PersonalizedMessage;

            return recommendation;
        }

        /// <summary>
        /// Check if Ollama is available and Phi-4 is loaded
        /// </summary>
        public async Task<bool> IsOllamaAvailableAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("http://localhost:11434/api/tags");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get list of available models in Ollama
        /// </summary>
        public async Task<List<string>> GetAvailableModelsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("http://localhost:11434/api/tags");
                if (!response.IsSuccessStatusCode)
                    return new List<string>();

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                var models = new List<string>();
                if (root.TryGetProperty("models", out var modelsArray))
                {
                    foreach (var model in modelsArray.EnumerateArray())
                    {
                        if (model.TryGetProperty("name", out var nameElem))
                        {
                            models.Add(nameElem.GetString() ?? "");
                        }
                    }
                }

                return models;
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
