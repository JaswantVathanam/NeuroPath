using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AdaptiveCognitiveRehabilitationPlatform.Services;

namespace NeuroPath.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private readonly IJsonGameStatsService _jsonStatsService;
        private readonly ILogger<GameController> _logger;

        public GameController(
            IJsonGameStatsService jsonStatsService,
            ILogger<GameController> logger)
        {
            _jsonStatsService = jsonStatsService;
            _logger = logger;
        }

        [HttpPost("save-session")]
        public async Task<IActionResult> SaveGameSession([FromBody] GameSessionRequest request)
        {
            try
            {
                var entry = new GameStatsEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = request.UserId,
                    Username = request.Username ?? "",
                    GameType = request.GameType,
                    GameMode = request.GameMode ?? "Practice",
                    Difficulty = request.DifficultyLevel,
                    Score = request.Score,
                    Accuracy = (decimal)(request.Accuracy ?? 0),
                    TotalMoves = request.TotalMoves ?? 0,
                    CorrectMoves = request.CorrectMoves ?? 0,
                    ErrorCount = request.ErrorCount ?? 0,
                    TimeTakenSeconds = request.Duration,
                    StartTime = request.StartTime ?? DateTime.UtcNow.AddSeconds(-request.Duration),
                    EndTime = request.EndTime ?? DateTime.UtcNow,
                    Status = request.CompletedSuccessfully ? "Completed" : "Incomplete"
                };

                await _jsonStatsService.SaveGameSessionAsync(entry);
                _logger.LogInformation("Game session saved for user {UserId}, game type: {GameType}", request.UserId, request.GameType);
                return Ok(new { success = true, message = "Game session saved successfully", sessionId = entry.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving game session");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("sessions/{userId:int}")]
        public async Task<IActionResult> GetUserSessions(int userId)
        {
            try
            {
                var sessions = await _jsonStatsService.GetSessionsByUserIdAsync(userId);
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sessions for user {UserId}", userId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("sessions/{userId:int}/{gameType}")]
        public async Task<IActionResult> GetUserSessionsByGameType(int userId, string gameType)
        {
            try
            {
                var allSessions = await _jsonStatsService.GetSessionsByUserIdAsync(userId);
                var filteredSessions = allSessions.Where(s => s.GameType.Equals(gameType, StringComparison.OrdinalIgnoreCase)).ToList();
                return Ok(filteredSessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving {GameType} sessions for user {UserId}", gameType, userId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("patient/autosave")]
        public async Task<IActionResult> AutoSavePatientSession([FromBody] PatientAutoSaveRequest request)
        {
            try
            {
                var entry = new GameStatsEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = request.UserId ?? 0,
                    Username = request.Username ?? "anonymous",
                    GameType = request.GameType,
                    GameMode = request.GameMode ?? "Practice",
                    Difficulty = request.DifficultyLevel,
                    Score = request.Score,
                    Accuracy = (decimal)(request.Accuracy ?? 0),
                    TotalMoves = request.TotalAttempts ?? 0,
                    CorrectMoves = request.CorrectAttempts ?? 0,
                    ErrorCount = (request.TotalAttempts ?? 0) - (request.CorrectAttempts ?? 0),
                    TimeTakenSeconds = request.Duration,
                    AverageReactionTimeMs = request.ResponseTime,
                    StartTime = request.StartTime ?? DateTime.UtcNow.AddSeconds(-request.Duration),
                    EndTime = DateTime.UtcNow,
                    Status = request.CompletedSuccessfully ? "Completed" : "Incomplete"
                };

                await _jsonStatsService.SaveGameSessionAsync(entry);
                _logger.LogInformation("Patient session autosaved for user {UserId}, game: {GameType}", request.UserId, request.GameType);
                return Ok(new { success = true, message = "Session autosaved", sessionId = entry.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error autosaving patient session");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("statistics/{userId:int}")]
        public async Task<IActionResult> GetUserStatistics(int userId)
        {
            try
            {
                var summary = await _jsonStatsService.GetUserSummaryAsync(userId);
                return Ok(new
                {
                    totalGames = summary.TotalGamesPlayed,
                    totalScore = summary.BestScore,
                    averageScore = summary.AverageScore,
                    totalDuration = summary.TotalTimePlayed,
                    averageAccuracy = summary.AverageAccuracy,
                    currentStreak = summary.CurrentStreak,
                    gameTypeBreakdown = summary.GameTypeBreakdown,
                    recentSessions = summary.RecentSessions.Take(5)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving statistics for user {UserId}", userId);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class GameSessionRequest
    {
        public int UserId { get; set; }
        public string? Username { get; set; }
        public string GameType { get; set; } = string.Empty;
        public string? GameMode { get; set; }
        public int Score { get; set; }
        public int Duration { get; set; }
        public int DifficultyLevel { get; set; } = 1;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool CompletedSuccessfully { get; set; }
        public double? Accuracy { get; set; }
        public int? TotalMoves { get; set; }
        public int? CorrectMoves { get; set; }
        public int? ErrorCount { get; set; }
    }

    public class PatientAutoSaveRequest
    {
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public string GameType { get; set; } = string.Empty;
        public string? GameMode { get; set; }
        public int Score { get; set; }
        public int Duration { get; set; }
        public int DifficultyLevel { get; set; } = 1;
        public DateTime? StartTime { get; set; }
        public bool CompletedSuccessfully { get; set; }
        public double? Accuracy { get; set; }
        public double? ResponseTime { get; set; }
        public int? TotalAttempts { get; set; }
        public int? CorrectAttempts { get; set; }
    }
}
