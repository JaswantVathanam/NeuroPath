using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AdaptiveCognitiveRehabilitationPlatform.Services;

namespace Adaptive_Cognitive_Rehabilitation_Platform.Controllers
{
    /* ==================================================================================
     * SQL/EF Core - Commented out for JSON-only mode
     * Note: ParentDashboard.razor has been removed, this controller is kept for API compatibility
     * ================================================================================== */

    /// <summary>
    /// Parent/Guardian API controller - Uses JSON-based stats in JSON-only mode
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ParentController : ControllerBase
    {
        private readonly IJsonGameStatsService _jsonStatsService;
        private readonly ILogger<ParentController> _logger;

        public ParentController(IJsonGameStatsService jsonStatsService, ILogger<ParentController> logger)
        {
            _jsonStatsService = jsonStatsService;
            _logger = logger;
        }

        /// <summary>
        /// Get list of children - Returns empty in JSON-only mode
        /// </summary>
        [HttpGet("children")]
        public IActionResult GetChildren()
        {
            _logger.LogInformation("[PARENT-API] JSON-only mode: Returning empty children list");
            return Ok(new
            {
                Children = new List<object>(),
                TotalCount = 0,
                Message = "JSON-only mode - Parent dashboard has been removed"
            });
        }

        /// <summary>
        /// Get list of children for a specific parent by ID
        /// </summary>
        [HttpGet("{parentId}/children")]
        public IActionResult GetChildrenByParentId(int parentId)
        {
            _logger.LogInformation($"[PARENT-API] JSON-only mode: Returning empty children list for parent {parentId}");
            return Ok(new List<object>());
        }

        /// <summary>
        /// Get child progress - Uses JSON stats
        /// </summary>
        [HttpGet("{parentId}/child/{childId}/progress")]
        public async Task<IActionResult> GetChildProgressByIds(int parentId, int childId)
        {
            try
            {
                var sessions = await _jsonStatsService.GetSessionsByUserIdAsync(childId);
                var sessionList = sessions.ToList();

                var stats = new
                {
                    TotalGamesPlayed = sessionList.Count,
                    AverageScore = sessionList.Count > 0 ? (int)sessionList.Average(s => s.Score) : 0,
                    BestScore = sessionList.Count > 0 ? sessionList.Max(s => s.Score) : 0,
                    AverageAccuracy = sessionList.Count > 0 ? (double)sessionList.Average(s => s.Accuracy) : 0,
                    TotalPlayTime = sessionList.Sum(s => s.TimeTakenSeconds),
                    GameTypes = sessionList
                        .GroupBy(s => s.GameType)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    RecentSessions = sessionList.OrderByDescending(s => s.EndTime).Take(10).Select(s => new
                    {
                        SessionId = s.Id,
                        GameType = s.GameType,
                        CreatedAt = s.EndTime,
                        Duration = s.TimeTakenSeconds,
                        Score = s.Score,
                        Accuracy = s.Accuracy
                    }).ToList()
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[PARENT-API] Error fetching child progress: {ex.Message}");
                return StatusCode(500, new { Error = "An error occurred while fetching progress" });
            }
        }

        /// <summary>
        /// Get child sessions from JSON storage
        /// </summary>
        [HttpGet("{parentId}/child/{childId}/sessions")]
        public async Task<IActionResult> GetChildSessionsByIds(int parentId, int childId, [FromQuery] int limit = 50)
        {
            try
            {
                var sessions = await _jsonStatsService.GetSessionsByUserIdAsync(childId);
                var sessionList = sessions.OrderByDescending(s => s.EndTime).Take(limit).Select(s => new
                {
                    SessionId = s.Id,
                    GameType = s.GameType,
                    GameMode = s.GameMode,
                    Difficulty = s.Difficulty,
                    Score = s.Score,
                    Accuracy = s.Accuracy,
                    TotalSeconds = s.TimeTakenSeconds,
                    TimeCompleted = s.EndTime,
                    Status = s.Status
                }).ToList();

                return Ok(sessionList);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[PARENT-API] Error fetching child sessions: {ex.Message}");
                return StatusCode(500, new { Error = "An error occurred while fetching sessions" });
            }
        }

        /// <summary>
        /// Get detailed reports for a specific child
        /// </summary>
        [HttpGet("child/{childId}/reports")]
        public async Task<IActionResult> GetChildReports(int childId)
        {
            try
            {
                var sessions = await _jsonStatsService.GetSessionsByUserIdAsync(childId);
                var sessionList = sessions.ToList();

                var gameTypeStats = sessionList
                    .GroupBy(s => s.GameType)
                    .Select(g => new
                    {
                        GameType = g.Key,
                        TotalSessions = g.Count(),
                        AverageScore = g.Average(s => s.Score),
                        AverageAccuracy = g.Average(s => s.Accuracy)
                    })
                    .ToList();

                return Ok(new
                {
                    Child = new { UserId = childId, Username = sessionList.FirstOrDefault()?.Username ?? childId.ToString(), Email = "", Age = 0, CognitiveLevel = "" },
                    TotalSessions = sessionList.Count,
                    GameTypeStats = gameTypeStats,
                    Sessions = sessionList.OrderByDescending(s => s.EndTime).Take(50).Select(s => new
                    {
                        SessionId = s.Id,
                        GameType = s.GameType,
                        CreatedAt = s.EndTime,
                        Duration = s.TimeTakenSeconds,
                        PerformanceScore = s.Score,
                        Accuracy = s.Accuracy
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[PARENT-API] Error fetching child reports: {ex.Message}");
                return StatusCode(500, new { Error = "An error occurred while fetching reports" });
            }
        }

        /// <summary>
        /// Get detailed view of a specific session
        /// </summary>
        [HttpGet("child/{childId}/session/{sessionId}")]
        public IActionResult GetSessionDetails(int childId, int sessionId)
        {
            _logger.LogInformation($"[PARENT-API] JSON-only mode: Session details for child {childId}, session {sessionId}");
            return Ok(new
            {
                Session = new
                {
                    SessionId = sessionId,
                    GameType = "Unknown",
                    CreatedAt = DateTime.UtcNow,
                    Duration = 0,
                    PerformanceScore = 0,
                    Accuracy = 0,
                    TotalMoves = 0,
                    CorrectMatches = 0
                },
                Analysis = (object?)null
            });
        }
    }
}
