using Microsoft.AspNetCore.Mvc;
using AdaptiveCognitiveRehabilitationPlatform.Services;
using System.Text.Json.Serialization;

namespace AdaptiveCognitiveRehabilitationPlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AIAnalysisController : ControllerBase
    {
        private readonly AzureAIDifficultyService _aiService;
        private readonly ILogger<AIAnalysisController> _logger;

        public AIAnalysisController(AzureAIDifficultyService aiService, ILogger<AIAnalysisController> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        /// <summary>
        /// Analyze game session and get AI encouragement + difficulty adjustments
        /// Backend processes the request (can take 10-60 seconds)
        /// Frontend doesn't need to wait
        /// </summary>
        [HttpPost("analyze-game")]
        public async Task<IActionResult> AnalyzeGame([FromBody] AnalyzeGameRequest request)
        {
            if (request == null)
            {
                return BadRequest("Invalid request");
            }

            try
            {
                _logger.LogInformation("[AI API] Analyzing game for {ChildName}: {GameType}", 
                    request.ChildName, request.GameType);

                // Convert request to service model
                var sessionData = new GameSessionData
                {
                    Moves = new List<MovePattern>(),
                    TotalCorrectMoves = request.TotalCorrect,
                    TotalWrongMoves = request.TotalWrong,
                    CurrentPairs = request.CurrentPairs,
                    CompletionTime = request.CompletionTimeSeconds,
                    AverageHesitation = 0,
                    AverageTimeBetweenFlips = 0
                };

                // Call AI service (this may take 10-60 seconds)
                var adjustment = await _aiService.AnalyzeMovePattersAndEncourageAsync(
                    sessionData,
                    request.CurrentDifficulty,
                    request.ChildName ?? "Player"
                );

                var response = new AnalyzeGameResponse
                {
                    Success = true,
                    Encouragement = adjustment.Encouragement,
                    EffortNote = adjustment.EffortNote,
                    FunMessage = adjustment.FunMessage,
                    GameAdjustments = new GameAdjustmentsDto
                    {
                        GridColumns = adjustment.GameAdjustments?.GridColumns ?? 4,
                        GridRows = adjustment.GameAdjustments?.GridRows ?? 4,
                        TotalPairs = adjustment.GameAdjustments?.TotalPairs ?? request.CurrentPairs,
                        FlipAnimationMs = adjustment.GameAdjustments?.FlipAnimationMs ?? 400,
                        VisualComplexity = adjustment.GameAdjustments?.VisualComplexity ?? "colorful",
                        TimePressure = adjustment.GameAdjustments?.TimePressure ?? "none",
                        CardDesign = adjustment.GameAdjustments?.CardDesign ?? "animals"
                    }
                };

                _logger.LogInformation("[AI API] Analysis complete for {ChildName}", request.ChildName);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AI API] Error analyzing game for {ChildName}", request.ChildName);
                return StatusCode(500, new { error = "AI analysis failed", message = ex.Message });
            }
        }
    }

    // Request/Response DTOs
    public class AnalyzeGameRequest
    {
        [JsonPropertyName("childName")]
        public string? ChildName { get; set; }

        [JsonPropertyName("gameType")]
        public string? GameType { get; set; } // "MemoryMatch", "ReactionTrainer", "SortingTask"

        [JsonPropertyName("currentDifficulty")]
        public int CurrentDifficulty { get; set; }

        [JsonPropertyName("totalCorrect")]
        public int TotalCorrect { get; set; }

        [JsonPropertyName("totalWrong")]
        public int TotalWrong { get; set; }

        [JsonPropertyName("currentPairs")]
        public int CurrentPairs { get; set; }

        [JsonPropertyName("completionTimeSeconds")]
        public double CompletionTimeSeconds { get; set; }
    }

    public class AnalyzeGameResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("encouragement")]
        public string? Encouragement { get; set; }

        [JsonPropertyName("effortNote")]
        public string? EffortNote { get; set; }

        [JsonPropertyName("funMessage")]
        public string? FunMessage { get; set; }

        [JsonPropertyName("gameAdjustments")]
        public GameAdjustmentsDto? GameAdjustments { get; set; }
    }

    public class GameAdjustmentsDto
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
}
