using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AdaptiveCognitiveRehabilitationPlatform.Services;

/// <summary>
/// Service for AI-powered game analysis using LM Studio
/// </summary>
public class GameAiAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GameAiAnalysisService> _logger;
    private readonly string _lmStudioUrl;

    public GameAiAnalysisService(HttpClient httpClient, ILogger<GameAiAnalysisService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        // LM Studio runs on localhost:1234 by default
        _lmStudioUrl = "http://localhost:1234/v1/chat/completions";
    }

    /// <summary>
    /// Analyze game performance and get AI recommendations
    /// </summary>
    public async Task<AiAnalysisResponse?> AnalyzeGamePerformanceAsync(
        GameSessionResult gameResult,
        string playerName = "Player",
        int playerAge = 0)
    {
        try
        {
            _logger.LogInformation($"🤖 Analyzing game performance for {playerName}...");

            var prompt = BuildAnalysisPrompt(gameResult, playerName, playerAge);
            var request = BuildChatCompletionRequest(prompt);

            var jsonContent = JsonSerializer.Serialize(request);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogDebug($"Sending request to LM Studio: {_lmStudioUrl}");

            var response = await _httpClient.PostAsync(_lmStudioUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"❌ LM Studio error: {response.StatusCode}");
                return GetDefaultResponse(gameResult);
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug($"Raw response: {responseContent}");

            var chatResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (chatResponse?.Choices?.Length == 0)
            {
                _logger.LogError("No choices in AI response");
                return GetDefaultResponse(gameResult);
            }

            var messageContent = chatResponse.Choices[0].Message?.Content ?? "";
            _logger.LogInformation($"✅ AI Analysis: {messageContent}");

            return ParseAiResponse(messageContent, gameResult);
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Exception during AI analysis: {ex.Message}");
            return GetDefaultResponse(gameResult);
        }
    }

    private string BuildAnalysisPrompt(GameSessionResult result, string playerName, int playerAge)
    {
        var ageInfo = playerAge > 0 ? $" The child is {playerAge} years old." : "";

        return $@"Analyze this game performance and provide encouragement and feedback:

Player: {playerName}{ageInfo}
Game: {result.GameType}
Difficulty: Level {result.DifficultyLevel}
Performance Score: {result.OverallScore:F1}/100
Accuracy: {result.AccuracyPercentage:F1}%
Speed Score: {result.SpeedScore:F1}
Consistency: {result.ConsistencyScore:F1}%
Moves: {result.TotalMoves}
Correct Matches: {result.CorrectMatches}
Errors: {result.ErrorCount}
Time: {result.ElapsedSeconds} seconds

Please respond with ONLY valid JSON (no markdown, no extra text) following this exact structure:
{{
  ""encouragement"": ""A child-friendly encouragement message"",
  ""effortNote"": ""A note about their effort"",
  ""funMessage"": ""A fun, engaging message for the child"",
  ""performanceScore"": {result.OverallScore:F1},
  ""nextDifficultyLevel"": {GetRecommendedLevel(result)},
  ""confidence"": 0.85,
  ""gameAdjustments"": {{
    ""gridColumns"": {GetRecommendedGridColumns(result)},
    ""gridRows"": {GetRecommendedGridRows(result)},
    ""totalPairs"": {GetRecommendedPairs(result)},
    ""flipAnimationMs"": {GetRecommendedFlipSpeed(result)},
    ""visualComplexity"": ""{GetRecommendedComplexity(result)}"",
    ""timePressure"": ""{GetRecommendedTimePressure(result)}"",
    ""cardDesign"": ""colorful""
  }}
}}";
    }

    private ChatCompletionRequest BuildChatCompletionRequest(string prompt)
    {
        return new ChatCompletionRequest
        {
            Model = "phi-4",
            Messages = new[]
            {
                new Message
                {
                    Role = "system",
                    Content = "You are an encouraging educational AI assistant for cognitive rehabilitation games. Always respond with valid JSON only, no additional text."
                },
                new Message
                {
                    Role = "user",
                    Content = prompt
                }
            },
            Temperature = 0.7,
            MaxTokens = 500,
            Stream = false
        };
    }

    private AiAnalysisResponse? ParseAiResponse(string jsonContent, GameSessionResult result)
    {
        try
        {
            // Clean up the response if it contains markdown code blocks
            var cleanJson = jsonContent
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            var response = JsonSerializer.Deserialize<AiAnalysisResponse>(
                cleanJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (response != null)
            {
                _logger.LogInformation("✅ Successfully parsed AI response");
                return response;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError($"Failed to parse AI JSON response: {ex.Message}");
        }

        return GetDefaultResponse(result);
    }

    private AiAnalysisResponse GetDefaultResponse(GameSessionResult result)
    {
        var (recommendedLevel, reason) = GetDifficultyRecommendation(result);

        var messages = result.OverallScore switch
        {
            >= 90 => new[] 
            { 
                "🌟 Fantastic! You're a superstar!",
                "You showed amazing skill and focus today.",
                "Keep shining like a bright star!" 
            },
            >= 80 => new[] 
            { 
                "⭐ Excellent work!",
                "You're doing really well!",
                "You're on your way to becoming a champion!" 
            },
            >= 70 => new[] 
            { 
                "👍 Great job!",
                "You're making good progress.",
                "Keep practicing and you'll do even better!" 
            },
            >= 60 => new[] 
            { 
                "💪 Good try!",
                "Every game helps you improve.",
                "You're building strong skills!" 
            },
            _ => new[] 
            { 
                "📚 Nice effort!",
                "Practice makes perfect!",
                "Let's try again and do even better!" 
            }
        };

        return new AiAnalysisResponse
        {
            Encouragement = messages[0],
            EffortNote = messages[1],
            FunMessage = messages[2],
            PerformanceScore = result.OverallScore,
            NextDifficultyLevel = recommendedLevel,
            Confidence = 0.85,
            GameAdjustments = new GameAdjustments
            {
                GridColumns = GetRecommendedGridColumns(result),
                GridRows = GetRecommendedGridRows(result),
                TotalPairs = GetRecommendedPairs(result),
                FlipAnimationMs = GetRecommendedFlipSpeed(result),
                VisualComplexity = GetRecommendedComplexity(result),
                TimePressure = GetRecommendedTimePressure(result),
                CardDesign = "colorful"
            }
        };
    }

    private int GetRecommendedLevel(GameSessionResult result)
    {
        return result.OverallScore >= 85 ? result.DifficultyLevel + 1 : 
               result.OverallScore < 60 && result.DifficultyLevel > 1 ? result.DifficultyLevel - 1 :
               result.DifficultyLevel;
    }

    private (int level, string reason) GetDifficultyRecommendation(GameSessionResult result)
    {
        if (result.OverallScore >= 85 && result.DifficultyLevel < 3)
            return (result.DifficultyLevel + 1, "You're ready for more challenge!");
        if (result.OverallScore < 60 && result.DifficultyLevel > 1)
            return (result.DifficultyLevel - 1, "Let's practice at an easier level.");
        return (result.DifficultyLevel, "You're at the perfect level!");
    }

    private int GetRecommendedGridColumns(GameSessionResult result) =>
        result.DifficultyLevel switch
        {
            1 => 4,
            2 => 5,
            3 => 6,
            _ => 4
        };

    private int GetRecommendedGridRows(GameSessionResult result) =>
        result.DifficultyLevel switch
        {
            1 => 4,
            2 => 4,
            3 => 4,
            _ => 4
        };

    private int GetRecommendedPairs(GameSessionResult result) =>
        result.DifficultyLevel switch
        {
            1 => 8,
            2 => 10,
            3 => 12,
            _ => 8
        };

    private int GetRecommendedFlipSpeed(GameSessionResult result) =>
        result.OverallScore > 80 ? 400 : 700;

    private string GetRecommendedComplexity(GameSessionResult result) =>
        result.OverallScore >= 85 ? "complex" :
        result.OverallScore >= 70 ? "moderate" :
        "simple";

    private string GetRecommendedTimePressure(GameSessionResult result) =>
        result.OverallScore >= 85 ? "challenging" :
        result.OverallScore >= 70 ? "moderate" :
        "relaxed";

    // LM Studio API Models
    public class ChatCompletionRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "phi-4";

        [JsonPropertyName("messages")]
        public Message[] Messages { get; set; } = Array.Empty<Message>();

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 500;

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;
    }

    public class Message
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
    }

    public class ChatCompletionResponse
    {
        [JsonPropertyName("choices")]
        public Choice[]? Choices { get; set; }
    }

    public class Choice
    {
        [JsonPropertyName("message")]
        public Message? Message { get; set; }
    }

    /// <summary>
    /// AI Analysis response with recommendations and game adjustments
    /// </summary>
    public class AiAnalysisResponse
    {
        [JsonPropertyName("encouragement")]
        public string Encouragement { get; set; } = "Great job!";

        [JsonPropertyName("effortNote")]
        public string EffortNote { get; set; } = "Keep practicing!";

        [JsonPropertyName("funMessage")]
        public string FunMessage { get; set; } = "You're doing great!";

        [JsonPropertyName("performanceScore")]
        public double PerformanceScore { get; set; }

        [JsonPropertyName("nextDifficultyLevel")]
        public int NextDifficultyLevel { get; set; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("gameAdjustments")]
        public GameAdjustments? GameAdjustments { get; set; }
    }

    /// <summary>
    /// Recommended game adjustments based on performance
    /// </summary>
    public class GameAdjustments
    {
        [JsonPropertyName("gridColumns")]
        public int GridColumns { get; set; }

        [JsonPropertyName("gridRows")]
        public int GridRows { get; set; }

        [JsonPropertyName("totalPairs")]
        public int TotalPairs { get; set; }

        [JsonPropertyName("flipAnimationMs")]
        public int FlipAnimationMs { get; set; }

        [JsonPropertyName("visualComplexity")]
        public string VisualComplexity { get; set; } = "moderate";

        [JsonPropertyName("timePressure")]
        public string TimePressure { get; set; } = "moderate";

        [JsonPropertyName("cardDesign")]
        public string CardDesign { get; set; } = "colorful";
    }
}
