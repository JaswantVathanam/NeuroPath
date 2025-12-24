using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AdaptiveCognitiveRehabilitationPlatform.Services
{
    public class AzureAIDifficultyService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureAIDifficultyService> _logger;

        private const string LocalLMStudioEndpoint = "http://localhost:1234/v1/chat/completions";
        private const string ModelName = "Phi-4-mini";

        public AzureAIDifficultyService(HttpClient httpClient, IConfiguration configuration, ILogger<AzureAIDifficultyService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _httpClient.Timeout = TimeSpan.FromSeconds(45);  // ← OPTIMIZED: 45 seconds for AI processing (plenty of time, prevents hanging)
        }

        public async Task<AIChildFriendlyAdjustment> AnalyzeMovePattersAndEncourageAsync(GameSessionData sessionData, int currentDifficulty, string childName = "Friend")
        {
            try
            {
                var prompt = BuildChildFriendlyPrompt(sessionData, currentDifficulty, childName);

                var request = new ChatCompletionRequest
                {
                    Model = ModelName,
                    Messages = new[]
                    {
                        new ChatMessage
                        {
                            Role = "system",
                            Content = "You are a friendly coach for children. Output ONLY a JSON object. No thinking, no explanations, no markdown. Just the JSON."
                        },
                        new ChatMessage
                        {
                            Role = "user",
                            Content = prompt
                        }
                    },
                    Temperature = 0.01,  // ← ULTRA-DETERMINISTIC: Forces JSON focus over thinking
                    MaxTokens = 2000,    // ← OPTIMIZED: 2000 tokens is plenty for JSON response (was 512K - way too much!)
                    TopP = 1.0f
                };

                Console.WriteLine($"[AI] 🎮 Analyzing {childName}'s moves with encouragement!");
                Console.WriteLine($"[AI] 📡 Connecting to Phi-4-mini on http://localhost:1234...");
                _logger.LogInformation("[AI] Analyzing moves for {ChildName}", childName);

                var response = await _httpClient.PostAsJsonAsync(LocalLMStudioEndpoint, request);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[AI] ⚠️ Server offline ({response.StatusCode}) - Using BACKUP RESPONSE");
                    _logger.LogWarning("[AI] LM Studio not responding ({StatusCode})", response.StatusCode);
                    return GetDefaultChildFriendlyAdjustment(sessionData, childName, isBackup: true);
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[AI] ✅ GOT AI RESPONSE ({jsonString.Length} bytes)");
                Console.WriteLine($"[RAW] Raw response (first 500 chars): {jsonString.Substring(0, Math.Min(500, jsonString.Length))}");

                // EXTRACT FIRST before logging
                var cleanedJson = ExtractJsonFromResponse(jsonString);
                Console.WriteLine($"[EXTRACT] 🔑 JSON LENGTH: {cleanedJson.Length} chars, STARTS: {cleanedJson.Substring(0, Math.Min(100, cleanedJson.Length))}");
                Console.WriteLine($"[EXTRACT] 🔑 JSON ENDS: ...{cleanedJson.Substring(Math.Max(0, cleanedJson.Length - 100))}");
                
                // Validate JSON is complete
                if (!cleanedJson.EndsWith("}"))
                {
                    Console.WriteLine($"[JSON] ⚠️ JSON doesn't end with }}, appears truncated!");
                    Console.WriteLine($"[JSON] Full extracted JSON: {cleanedJson}");
                }
                
                _logger.LogDebug("[AI] Cleaned JSON: {Json}", cleanedJson);
                
                var result = JsonSerializer.Deserialize<AIChildFriendlyAdjustment>(cleanedJson);
                if (result != null && !string.IsNullOrWhiteSpace(result.Encouragement))
                {
                    // ANTI-HALLUCINATION: Validate confidence is in valid range (0.0-1.0)
                    if (result.Confidence < 0.5 || result.Confidence > 1.0)
                    {
                        Console.WriteLine($"[AI] ⚠️ Confidence out of range ({result.Confidence:P0}) - resetting to calculated value");
                        // Recalculate from performance score
                        var perf = result.PerformanceScore;
                        if (perf >= 85) result.Confidence = 0.95;
                        else if (perf >= 75) result.Confidence = 0.85;
                        else if (perf >= 65) result.Confidence = 0.75;
                        else if (perf >= 55) result.Confidence = 0.70;
                        else if (perf >= 45) result.Confidence = 0.60;
                        else if (perf >= 35) result.Confidence = 0.55;
                        else result.Confidence = 0.50;
                    }

                    Console.WriteLine($"[AI] 🤖 AI SAYS: {result.Encouragement}");
                    Console.WriteLine($"[AI] 📊 Performance Score: {result.PerformanceScore}%");
                    Console.WriteLine($"[AI] 🔐 Confidence: {result.Confidence:P0}");
                    Console.WriteLine($"[AI] 📈 Next Difficulty: Level {result.NextDifficultyLevel}");
                    Console.WriteLine($"[AI] 🎮 AI ADJUSTS: {result.GameAdjustments?.TotalPairs} cards at {result.GameAdjustments?.FlipAnimationMs}ms speed");
                    Console.WriteLine($"[AI] 🧠 Complexity: {result.GameAdjustments?.VisualComplexity}");
                    return result;
                }

                Console.WriteLine($"[AI] ⚠️ AI response invalid - retrying...");
                _logger.LogWarning("[AI] Failed to deserialize, data was: {Json}", cleanedJson);
                // Retry once with a simpler request
                return await AnalyzeMovePattersAndEncourageAsync(sessionData, currentDifficulty, childName);
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"[AI] ⏱️ AI took too long (>45s) - using local analysis");
                _logger.LogWarning(ex, "[AI] Timeout after 45 seconds");
                // Don't use backup - generate response locally instead
                var accuracy = sessionData.TotalCorrectMoves > 0
                    ? (sessionData.TotalCorrectMoves * 100.0 / (sessionData.TotalCorrectMoves + sessionData.TotalWrongMoves))
                    : 0;
                var preciseAccuracy = Math.Round(accuracy, 2);
                var nextLevel = accuracy >= 75 ? Math.Min(5, currentDifficulty + 1) : 
                               accuracy < 50 ? Math.Max(1, currentDifficulty - 1) : 
                               currentDifficulty;
                
                Console.WriteLine($"[LOCAL] Using local analysis instead of AI");
                Console.WriteLine($"[LOCAL] 📊 Performance: {preciseAccuracy}%");
                Console.WriteLine($"[LOCAL] 📈 Next Level: {nextLevel}");
                return BuildLocalAdjustment(sessionData, currentDifficulty, childName);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[AI] ⚠️ Cannot connect to Phi-4 - using local analysis");
                Console.WriteLine($"[AI] 📌 Make sure LM Studio is running on port 1234");
                _logger.LogError(ex, "[AI] Connection error - Phi-4 may not be running");
                return BuildLocalAdjustment(sessionData, currentDifficulty, childName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AI] ⚠️ Error: {ex.Message}");
                Console.WriteLine($"[AI] Using local analysis");
                _logger.LogError(ex, "[AI] Analysis error");
                return BuildLocalAdjustment(sessionData, currentDifficulty, childName);
            }
        }

        private string ExtractJsonFromResponse(string response)
        {
            try
            {
                var responseLen = response.Length;
                Console.WriteLine($"[EXTRACT] 📦 Received {responseLen} bytes");

                // Step 1: Handle ChatCompletion wrapper from LM Studio
                if (response.Contains("\"choices\""))
                {
                    Console.WriteLine($"[EXTRACT] 🔍 Detected ChatCompletion wrapper");
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(response);
                        var element = jsonDoc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content");
                        response = element.GetString() ?? "";
                        Console.WriteLine($"[EXTRACT] ✅ Extracted from wrapper: {response.Length} bytes");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[EXTRACT] ⚠️ Wrapper parsing failed: {ex.Message}");
                    }
                }

                // Step 2: Check if response is PURE thinking block
                if (response.TrimStart().StartsWith("<think", System.StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[EXTRACT] 🧠 Response starts with <think> block");
                    
                    // Try to extract JSON from INSIDE the think block
                    var thinkEnd = response.IndexOf("</think>", System.StringComparison.OrdinalIgnoreCase);
                    if (thinkEnd >= 0)
                    {
                        var beforeThink = response.Substring(0, response.IndexOf("<think>", System.StringComparison.OrdinalIgnoreCase));
                        var afterThink = response.Substring(thinkEnd + 8);
                        var middle = response.Substring(response.IndexOf(">", System.StringComparison.OrdinalIgnoreCase) + 1, thinkEnd - response.IndexOf(">", System.StringComparison.OrdinalIgnoreCase) - 1);
                        
                        // Try JSON in this order: after, before, middle
                        foreach (var section in new[] { afterThink, beforeThink, middle })
                        {
                            var json = ExtractJsonFromText(section);
                            if (json != "{}")
                            {
                                Console.WriteLine($"[EXTRACT] ✨ Found JSON in think response!");
                                return json;
                            }
                        }
                    }
                    
                    Console.WriteLine($"[EXTRACT] ❌ Response is PURE thinking block with no JSON");
                    return "{}";
                }

                // Step 3: Extract JSON from normal response
                string jsonStr = ExtractJsonFromText(response);
                if (jsonStr != "{}")
                {
                    Console.WriteLine($"[EXTRACT] ✅ Extracted {jsonStr.Length} bytes of JSON");
                    return jsonStr;
                }
                
                Console.WriteLine($"[EXTRACT] ❌ No valid JSON found");
                return "{}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EXTRACT] ❌ ERROR: {ex.Message}");
                return "{}";
            }
        }

        private string ExtractJsonFromText(string text)
        {
            // Remove markdown code fence markers and common artifacts
            text = text.Replace("```json", "")
                      .Replace("```ts", "")
                      .Replace("```", "")
                      .Replace("\\r\\n", "\n")  // Handle escaped newlines
                      .Replace("\r\n", "\n")    // Handle actual CRLF
                      .Trim();
            
            var firstBrace = text.IndexOf('{');
            var lastCloseBrace = text.LastIndexOf('}');
            
            if (firstBrace >= 0 && lastCloseBrace > firstBrace)
            {
                var extracted = text.Substring(firstBrace, lastCloseBrace - firstBrace + 1).Trim();
                
                // Additional validation: ensure it starts with { and ends with }
                if (extracted.StartsWith("{") && extracted.EndsWith("}"))
                {
                    Console.WriteLine($"[EXTRACT] 📋 Cleaned JSON length: {extracted.Length}");
                    return extracted;
                }
            }
            
            return "{}";
        }

        private string BuildChildFriendlyPrompt(GameSessionData sessionData, int currentDifficulty, string childName)
        {
            var accuracy = sessionData.TotalCorrectMoves > 0
                ? (sessionData.TotalCorrectMoves * 100.0 / (sessionData.TotalCorrectMoves + sessionData.TotalWrongMoves))
                : 0;

            var preciseAccuracy = Math.Round(accuracy, 2);
            var nextLevel = accuracy >= 75 ? Math.Min(5, currentDifficulty + 1) : 
                           accuracy < 50 ? Math.Max(1, currentDifficulty - 1) : 
                           currentDifficulty;
            
            var gridSize = 4 + nextLevel;
            var pairs = 6 + (nextLevel * 2);
            var speed = accuracy >= 75 ? 300 : accuracy >= 50 ? 400 : 500;

            // Calculate confidence based on accuracy (0.0 to 1.0)
            double confidence = 0.5;
            if (preciseAccuracy >= 85) confidence = 0.95;
            else if (preciseAccuracy >= 75) confidence = 0.85;
            else if (preciseAccuracy >= 65) confidence = 0.75;
            else if (preciseAccuracy >= 55) confidence = 0.70;
            else if (preciseAccuracy >= 45) confidence = 0.60;
            else if (preciseAccuracy >= 35) confidence = 0.55;
            else confidence = 0.50;

            // AI should EVALUATE performance automatically with contextual thinking
            return $@"You are a warm, encouraging coach for a child doing memory therapy.

GAME RESULTS TO ANALYZE:
- Correct matches found: {sessionData.TotalCorrectMoves}
- Wrong attempts: {sessionData.TotalWrongMoves}
- Accuracy: {preciseAccuracy}%
- Total moves: {sessionData.TotalCorrectMoves + sessionData.TotalWrongMoves}
- Improvement trend: {Math.Round(sessionData.LearningCurve, 2)} (higher = improving)
- Average thinking time: {Math.Round(sessionData.AverageHesitation, 0)}ms

EVALUATE THEIR PERFORMANCE (0-100 scale):
- Below 40: Needs practice (score 20-40)
- 40-60: Learning well (score 45-65)
- 60-75: Good improvement (score 65-80)
- Above 75: Excellent! (score 80-95)

IMPORTANT: performanceScore should reflect your professional evaluation of their learning, NOT just echo their accuracy.

Create encouragement based on their ACTUAL results. Respond with ONLY this JSON:
{{
  ""encouragement"": ""[Create specific praise for what they accomplished - mention their actual numbers]"",
  ""effortNote"": ""[Describe what their performance reveals about their strategy - acknowledge both strengths and areas to work on]"",
  ""funMessage"": ""[Playful, motivating message about coming challenges]"",
  ""performanceScore"": [YOUR PROFESSIONAL EVALUATION: 0-100, based on accuracy, improvement, and consistency],
  ""nextDifficultyLevel"": {nextLevel},
  ""confidence"": {confidence},
  ""gameAdjustments"": {{
    ""gridColumns"": {gridSize},
    ""gridRows"": {gridSize},
    ""totalPairs"": {pairs},
    ""flipAnimationMs"": {speed},
    ""visualComplexity"": ""{(nextLevel >= 4 ? "colorful" : nextLevel >= 2 ? "medium" : "simple")}"",
    ""timePressure"": ""{(accuracy >= 75 ? "mild" : "none")}"",
    ""cardDesign"": ""animals""
  }}
}}";
        }

        // Fast encouragement generator - picks ONE pattern
        private string GetEncouragement(double accuracy)
        {
            if (accuracy >= 85)
            {
                var verbs = new[] { "crushed", "dominated", "destroyed", "rocked" };
                var adjectives = new[] { "incredible", "amazing", "superb", "fantastic" };
                return $"🌟 {verbs[new Random().Next(verbs.Length)]} it! Memory {adjectives[new Random().Next(adjectives.Length)]}!";
            }
            else if (accuracy >= 75)
            {
                var quality = new[] { "Good", "Great" }[new Random().Next(2)];
                var verb = new[] { "pushing", "going", "learning" }[new Random().Next(3)];
                return $"� {quality} job! Keep {verb}!";
            }
            else
            {
                var verb = new[] { "You", "Keep", "Let's" }[new Random().Next(3)];
                var action = new[] { "can do this", "got this", "try again" }[new Random().Next(3)];
                var msg = new[] { "Every game teaches", "Practice helps", "You're learning" }[new Random().Next(3)];
                return $"💪 {verb} {action}! {msg}!";
            }
        }

        // Fast effort note generator
        private string GetEffortNote(double accuracy, int correct, int wrong)
        {
            var total = correct + wrong;
            var messages = new[]
            {
                $"{correct} out of {total} correct - great consistency!",
                $"Getting {accuracy}% - you're improving!",
                $"Found {correct} matches - nice work!",
                $"{total} total attempts - keep practicing!"
            };
            return messages[new Random().Next(messages.Length)];
        }

        // Fast fun message generator
        private string GetFunMessage(double accuracy, int correct)
        {
            var emojis = new[] { "🚀", "🧠", "⭐" };
            var messages = new[]
            {
                $"You found {correct} matches - your brain is sharp! {emojis[new Random().Next(emojis.Length)]}",
                $"{Math.Round(accuracy)}% accuracy - you're a memory champion! {emojis[new Random().Next(emojis.Length)]}",
                $"Amazing performance with {correct} correct moves! {emojis[new Random().Next(emojis.Length)]}",
                $"Your memory skills are level {correct/2} strong! {emojis[new Random().Next(emojis.Length)]}"
            };
            return messages[new Random().Next(messages.Length)];
        }

        // Build response locally when AI is unavailable (fast fallback)
        private AIChildFriendlyAdjustment BuildLocalAdjustment(GameSessionData sessionData, int currentDifficulty, string childName)
        {
            var accuracy = sessionData.TotalCorrectMoves > 0
                ? (sessionData.TotalCorrectMoves * 100.0 / (sessionData.TotalCorrectMoves + sessionData.TotalWrongMoves))
                : 0;

            var preciseAccuracy = Math.Round(accuracy, 2);
            var nextLevel = accuracy >= 75 ? Math.Min(5, currentDifficulty + 1) : 
                           accuracy < 50 ? Math.Max(1, currentDifficulty - 1) : 
                           currentDifficulty;
            
            var gridSize = 4 + nextLevel;
            var pairs = 6 + (nextLevel * 2);
            var speed = accuracy >= 75 ? 300 : accuracy >= 50 ? 400 : 500;

            // Calculate confidence based on accuracy
            double confidence = 0.5;
            if (preciseAccuracy >= 85) confidence = 0.95;
            else if (preciseAccuracy >= 75) confidence = 0.85;
            else if (preciseAccuracy >= 65) confidence = 0.75;
            else if (preciseAccuracy >= 55) confidence = 0.70;
            else if (preciseAccuracy >= 45) confidence = 0.60;
            else if (preciseAccuracy >= 35) confidence = 0.55;
            else confidence = 0.50;

            return new AIChildFriendlyAdjustment
            {
                Encouragement = GetEncouragement(preciseAccuracy),
                EffortNote = GetEffortNote(preciseAccuracy, sessionData.TotalCorrectMoves, sessionData.TotalWrongMoves),
                FunMessage = GetFunMessage(preciseAccuracy, sessionData.TotalCorrectMoves),
                PerformanceScore = preciseAccuracy,
                NextDifficultyLevel = nextLevel,
                Confidence = confidence,
                GameAdjustments = new GameAdjustments
                {
                    GridColumns = gridSize,
                    GridRows = gridSize,
                    TotalPairs = pairs,
                    FlipAnimationMs = speed,
                    VisualComplexity = nextLevel >= 4 ? "colorful" : nextLevel >= 2 ? "medium" : "simple",
                    TimePressure = accuracy >= 75 ? "mild" : "none",
                    CardDesign = "animals"
                }
            };
        }

        private AIChildFriendlyAdjustment GetDefaultChildFriendlyAdjustment(GameSessionData sessionData, string childName, bool isBackup = false)
        {
            var accuracy = sessionData.TotalCorrectMoves > 0
                ? (sessionData.TotalCorrectMoves * 100.0 / (sessionData.TotalCorrectMoves + sessionData.TotalWrongMoves))
                : 0;

            var preciseAccuracy = Math.Round(accuracy, 2);

            var encouragement = accuracy >= 75 ? $"🌟 Amazing, {childName}! Superstar!"
                : accuracy >= 50 ? $"😊 Great job, {childName}! Getting it!"
                : $"💪 Keep going, {childName}! Learning!";

            var nextDifficultyLevel = accuracy >= 75 ? Math.Min(5, sessionData.CurrentPairs + 1) : 
                                     accuracy < 50 ? Math.Max(1, sessionData.CurrentPairs - 1) : 
                                     sessionData.CurrentPairs;

            var nextPairs = accuracy >= 75 ? Math.Min(15, sessionData.CurrentPairs + 2) : 
                           accuracy >= 50 ? sessionData.CurrentPairs : 
                           Math.Max(6, sessionData.CurrentPairs - 2);
            var nextSpeed = accuracy >= 75 ? 350 : accuracy >= 50 ? 400 : 500;

            // Calculate confidence based on accuracy (0.0 to 1.0) - PREVENTS HALLUCINATION
            double confidence = 0.5;
            if (preciseAccuracy >= 85) confidence = 0.95;
            else if (preciseAccuracy >= 75) confidence = 0.85;
            else if (preciseAccuracy >= 65) confidence = 0.75;
            else if (preciseAccuracy >= 55) confidence = 0.70;
            else if (preciseAccuracy >= 45) confidence = 0.60;
            else if (preciseAccuracy >= 35) confidence = 0.55;
            else confidence = 0.50;

            if (isBackup)
            {
                Console.WriteLine($"[BACKUP] 🔄 Auto-generated backup response");
                Console.WriteLine($"[BACKUP] 💬 SAYS: {encouragement}");
                Console.WriteLine($"[BACKUP] 📊 Performance: {preciseAccuracy}%");
                Console.WriteLine($"[BACKUP] 📈 Next Level: {nextDifficultyLevel}");
                Console.WriteLine($"[BACKUP] 🎮 ADJUSTS: {nextPairs} cards at {nextSpeed}ms speed");
                Console.WriteLine($"[BACKUP] 🔐 Confidence: {confidence:P0}");
                _logger.LogInformation("[BACKUP] Using backup for {ChildName}", childName);
            }

            return new AIChildFriendlyAdjustment
            {
                Encouragement = encouragement,
                EffortNote = accuracy >= 75 ? "You're super fast!" : accuracy >= 50 ? "You're thinking carefully!" : "Keep practicing!",
                FunMessage = "Every game makes your brain stronger! 🧠",
                PerformanceScore = preciseAccuracy,
                NextDifficultyLevel = nextDifficultyLevel,
                Confidence = confidence,  // NEW: Add confidence to backup
                GameAdjustments = new GameAdjustments
                {
                    GridColumns = 4,
                    GridRows = 4,
                    TotalPairs = nextPairs,
                    FlipAnimationMs = nextSpeed,
                    VisualComplexity = accuracy >= 75 ? "colorful" : accuracy >= 50 ? "medium" : "simple",
                    TimePressure = accuracy >= 75 ? "mild" : "none",
                    CardDesign = "animals"
                }
            };
        }
    }

    public class ChatCompletionRequest
    {
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("messages")]
        public ChatMessage[]? Messages { get; set; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("top_p")]
        public float TopP { get; set; }
    }


    public class ChatCompletionResponse
    {
        [JsonPropertyName("choices")]
        public Choice[]? Choices { get; set; }
    }

    public class Choice
    {
        [JsonPropertyName("message")]
        public ChatMessage? Message { get; set; }
    }

    public class ChatMessage
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    public class MovePattern
    {
        public int MoveNumber { get; set; }
        public DateTime Timestamp { get; set; }
        public int FirstCardId { get; set; }
        public int SecondCardId { get; set; }
        public int TimeBetweenFlips { get; set; }
        public bool WasCorrect { get; set; }
        public int HesitationTime { get; set; }
    }

    public class GameSessionData
    {
        public List<MovePattern> Moves { get; set; } = new();
        public int TotalCorrectMoves { get; set; }
        public int TotalWrongMoves { get; set; }
        public double AverageHesitation { get; set; }
        public double AverageTimeBetweenFlips { get; set; }
        public string? OverallStrategy { get; set; }
        public int CurrentPairs { get; set; }
        public double CompletionTime { get; set; }
        public double LearningCurve { get; set; }
    }

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
        public string? VisualComplexity { get; set; }

        [JsonPropertyName("timePressure")]
        public string? TimePressure { get; set; }

        [JsonPropertyName("cardDesign")]
        public string? CardDesign { get; set; }
    }

    public class AIChildFriendlyAdjustment
    {
        [JsonPropertyName("encouragement")]
        public string? Encouragement { get; set; }

        [JsonPropertyName("effortNote")]
        public string? EffortNote { get; set; }

        [JsonPropertyName("funMessage")]
        public string? FunMessage { get; set; }

        [JsonPropertyName("performanceScore")]
        public double PerformanceScore { get; set; }

        [JsonPropertyName("nextDifficultyLevel")]
        public int NextDifficultyLevel { get; set; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("gameAdjustments")]
        public GameAdjustments? GameAdjustments { get; set; }
    }
}
