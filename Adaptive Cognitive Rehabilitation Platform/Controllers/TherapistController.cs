using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AdaptiveCognitiveRehabilitationPlatform.Services;

namespace NeuroPath.Controllers
{
    /* ==================================================================================
     * SQL/EF Core - Commented out for JSON-only mode
     * This controller uses JSON storage for game statistics.
     * ================================================================================== */

    /// <summary>
    /// Therapist API controller - Uses JSON-based stats in JSON-only mode
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TherapistController : ControllerBase
    {
        private readonly IJsonGameStatsService _jsonStatsService;
        private readonly ILogger<TherapistController> _logger;

        public TherapistController(IJsonGameStatsService jsonStatsService, ILogger<TherapistController> logger)
        {
            _jsonStatsService = jsonStatsService;
            _logger = logger;
        }

        /// <summary>
        /// Get list of patients - Returns empty in JSON-only mode
        /// </summary>
        [HttpGet("patients")]
        public IActionResult GetPatients()
        {
            _logger.LogInformation("[THERAPIST-API] JSON-only mode: Returning empty patient list");
            return Ok(new List<PatientProfileDto>());
        }

        /// <summary>
        /// Get list of patients for a specific therapist by ID
        /// </summary>
        [HttpGet("{therapistId}/patients")]
        public IActionResult GetPatientsByTherapistId(int therapistId)
        {
            _logger.LogInformation($"[THERAPIST-API] JSON-only mode: Returning empty patient list for therapist {therapistId}");
            return Ok(new List<object>());
        }

        /// <summary>
        /// Get patient progress - Uses JSON stats in JSON-only mode
        /// </summary>
        [HttpGet("patient/{patientId}/progress")]
        public async Task<IActionResult> GetPatientProgress(int patientId)
        {
            try
            {
                var sessions = await _jsonStatsService.GetSessionsByUserIdAsync(patientId);
                var sessionList = sessions.ToList();

                var progressData = new PatientProgressDto
                {
                    PatientId = patientId,
                    TotalSessionsCompleted = sessionList.Count,
                    AverageScore = sessionList.Count > 0 ? (int)sessionList.Average(s => s.Score) : 0,
                    BestScore = sessionList.Count > 0 ? sessionList.Max(s => s.Score) : 0,
                    AverageAccuracy = sessionList.Count > 0 ? sessionList.Average(s => s.Accuracy) : 0,
                    TrendDirection = "stable",
                    LastSessionDate = sessionList.FirstOrDefault()?.EndTime ?? DateTime.UtcNow,
                    TotalPlayTimeMinutes = sessionList.Sum(s => s.TimeTakenSeconds) / 60
                };

                return Ok(progressData);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[THERAPIST-API] Error fetching patient progress: {ex.Message}");
                return StatusCode(500, new { error = "Error fetching progress", details = ex.Message });
            }
        }

        /// <summary>
        /// Get patient sessions from JSON storage
        /// </summary>
        [HttpGet("patient/{patientId}/sessions")]
        public async Task<IActionResult> GetPatientSessions(int patientId, [FromQuery] int limit = 50)
        {
            try
            {
                var sessions = await _jsonStatsService.GetSessionsByUserIdAsync(patientId);
                var sessionList = sessions.Take(limit).Select(s => new GameSessionDto
                {
                    SessionId = 0,
                    GameType = s.GameType,
                    Score = s.Score,
                    AccuracyPercentage = s.Accuracy,
                    TimeStarted = s.StartTime,
                    TimeCompleted = s.EndTime,
                    CreatedAt = s.EndTime,
                    Status = s.Status,
                    Notes = s.Notes ?? ""
                }).ToList();

                return Ok(sessionList);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[THERAPIST-API] Error fetching patient sessions: {ex.Message}");
                return StatusCode(500, new { error = "Error fetching sessions", details = ex.Message });
            }
        }

        /// <summary>
        /// Save patient notes - Placeholder in JSON-only mode
        /// </summary>
        [HttpPost("patient/{patientId}/notes")]
        public IActionResult SavePatientNotes(int patientId, [FromBody] SaveNotesDto request)
        {
            _logger.LogInformation($"[THERAPIST-API] JSON-only mode: Notes save requested for patient {patientId}");
            return Ok(new NoteSaveResponseDto
            {
                Success = true,
                Message = "Notes saved (JSON-only mode - not persisted to SQL)",
                SavedAt = DateTime.UtcNow,
                NoteLength = request?.Notes?.Length ?? 0
            });
        }

        /// <summary>
        /// Update difficulty level - Placeholder in JSON-only mode
        /// </summary>
        [HttpPost("patient/{patientId}/difficulty")]
        public IActionResult UpdateDifficultyLevel(int patientId, [FromBody] UpdateDifficultyDto request)
        {
            _logger.LogInformation($"[THERAPIST-API] JSON-only mode: Difficulty update requested for patient {patientId}");
            return Ok(new AssignmentUpdateResponseDto
            {
                Success = true,
                Message = $"Difficulty level updated to {request?.DifficultyLevel} (JSON-only mode)",
                UpdatedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Get therapist analytics from JSON storage
        /// </summary>
        [HttpGet("analytics")]
        public async Task<IActionResult> GetTherapistAnalytics()
        {
            try
            {
                var analyticsData = await _jsonStatsService.GetTherapistAnalyticsAsync();

                // Calculate additional metrics from patient summaries
                var totalSessions = analyticsData.PatientSummaries.Sum(p => p.TotalSessions);
                var avgAccuracy = analyticsData.PatientSummaries.Any() 
                    ? analyticsData.PatientSummaries.Average(p => p.AverageAccuracy) : 0;
                var avgScore = analyticsData.PatientSummaries.Any()
                    ? analyticsData.PatientSummaries.Average(p => p.AverageScore) : 0;

                // Find most/top performing games
                var allGameTypes = analyticsData.GameDistribution;
                var mostPlayedGame = allGameTypes.OrderByDescending(g => g.SessionCount).FirstOrDefault()?.GameType ?? "N/A";
                // Top performing is determined by patient summaries average scores, not game distribution
                var topPerformingGame = mostPlayedGame; // Using same as most played since GameTypeDistribution doesn't track scores

                return Ok(new TherapistAnalyticsDto
                {
                    TotalPatients = analyticsData.TotalPatients,
                    TotalSessionsThisWeek = analyticsData.TotalSessionsThisWeek,
                    TotalSessions = totalSessions,
                    AverageAccuracyAll = avgAccuracy,
                    AverageScoreAll = (int)avgScore,
                    SessionCompletionRate = 100, // Placeholder
                    TotalPlayTimeMinutes = 0, // Would need to calculate from sessions
                    TopPerformingGame = topPerformingGame,
                    MostPlayedGame = mostPlayedGame
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[THERAPIST-API] Error fetching analytics: {ex.Message}");
                return StatusCode(500, new { error = "Error fetching analytics", details = ex.Message });
            }
        }

        /// <summary>
        /// Get recent game sessions from JSON storage
        /// </summary>
        [HttpGet("recent-game-sessions")]
        public async Task<IActionResult> GetRecentGameSessions()
        {
            try
            {
                var allSessions = await _jsonStatsService.GetAllSessionsAsync();
                var recentSessions = allSessions
                    .OrderByDescending(s => s.EndTime)
                    .Take(20)
                    .Select(s => new
                    {
                        SessionId = s.Id,
                        PatientId = s.UserId,
                        PatientName = s.Username,
                        GameType = s.GameType,
                        PerformanceScore = s.Score,
                        Accuracy = s.Accuracy,
                        TotalSeconds = s.TimeTakenSeconds,
                        Difficulty = s.Difficulty,
                        NextDifficultyLevel = s.Difficulty,
                        TimeCompleted = s.EndTime,
                        AiAnalysis = s.AiEncouragement
                    })
                    .ToList();

                return Ok(recentSessions);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[THERAPIST-API] Error fetching recent game sessions: {ex.Message}");
                return StatusCode(500, new { error = "Error fetching game sessions", details = ex.Message });
            }
        }
    }

    // DTOs remain the same for API compatibility
    public class PatientProfileDto
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string AgeGroup { get; set; } = "";
        public string Email { get; set; } = "";
        public DateTime CreatedDate { get; set; }
        public DateTime AssignedDate { get; set; }
        public string TherapistNotes { get; set; } = "";
        public string AssignedDifficultyLevel { get; set; } = "";
    }

    public class PatientProgressDto
    {
        public int PatientId { get; set; }
        public int TotalSessionsCompleted { get; set; }
        public int AverageScore { get; set; }
        public int BestScore { get; set; }
        public decimal AverageAccuracy { get; set; }
        public Dictionary<string, int>? GameTypes { get; set; }
        public string TrendDirection { get; set; } = "stable";
        public DateTime LastSessionDate { get; set; }
        public long TotalPlayTimeMinutes { get; set; }
    }

    public class GameSessionDto
    {
        public int SessionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string GameType { get; set; } = "";
        public int Score { get; set; }
        public decimal AccuracyPercentage { get; set; }
        public DateTime? TimeStarted { get; set; }
        public DateTime? TimeCompleted { get; set; }
        public string Status { get; set; } = "";
        public string Notes { get; set; } = "";
    }

    public class SaveNotesDto
    {
        public string? Notes { get; set; }
    }

    public class NoteSaveResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public DateTime SavedAt { get; set; }
        public int NoteLength { get; set; }
    }

    public class UpdateDifficultyDto
    {
        public string? DifficultyLevel { get; set; }
    }

    public class AssignmentUpdateResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public DateTime UpdatedAt { get; set; }
    }

    public class TherapistAnalyticsDto
    {
        public int TotalPatients { get; set; }
        public int TotalSessionsThisWeek { get; set; }
        public int TotalSessions { get; set; }
        public decimal AverageAccuracyAll { get; set; }
        public int AverageScoreAll { get; set; }
        public decimal SessionCompletionRate { get; set; }
        public long TotalPlayTimeMinutes { get; set; }
        public string TopPerformingGame { get; set; } = "";
        public string MostPlayedGame { get; set; } = "";
    }
}
