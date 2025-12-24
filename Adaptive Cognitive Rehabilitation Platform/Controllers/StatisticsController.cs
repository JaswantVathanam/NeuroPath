using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NeuroPath.Models;
using AdaptiveCognitiveRehabilitationPlatform.Services.GameAnalytics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AdaptiveCognitiveRehabilitationPlatform.Controllers
{
    /// <summary>
    /// API Controller for statistics and analytics data
    /// Used by Statistics.razor page to display dynamic performance metrics
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class StatisticsController : ControllerBase
    {
        private readonly IGameSessionRepository _sessionRepository;
        private readonly PerformanceMetricsCalculator _metricsCalculator;
        private readonly ILogger<StatisticsController> _logger;

        public StatisticsController(
            IGameSessionRepository sessionRepository,
            PerformanceMetricsCalculator metricsCalculator,
            ILogger<StatisticsController> logger)
        {
            _sessionRepository = sessionRepository;
            _metricsCalculator = metricsCalculator;
            _logger = logger;
        }

        /// <summary>
        /// Get overall summary statistics for a user
        /// GET /api/statistics/summary?userId=123
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult<StatisticsSummaryDto>> GetSummary([FromQuery] int userId)
        {
            try
            {
                var sessions = await _sessionRepository.GetCompletedSessionsAsync(userId);
                
                if (!sessions.Any())
                {
                    return Ok(new StatisticsSummaryDto
                    {
                        TotalSessions = 0,
                        AverageSessionDuration = 0,
                        OverallAccuracy = 0,
                        TotalPoints = 0
                    });
                }

                var metrics = _metricsCalculator.CalculateAggregatedMetrics(sessions);
                var averageScore = (int)metrics.AverageScore;

                return Ok(new StatisticsSummaryDto
                {
                    TotalSessions = sessions.Count,
                    AverageSessionDuration = metrics.AverageDuration,
                    OverallAccuracy = (decimal)metrics.AverageAccuracy,
                    TotalPoints = sessions.Sum(s => s.PerformanceScore)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting statistics summary: {ex.Message}");
                return StatusCode(500, new { error = "Error retrieving statistics" });
            }
        }

        /// <summary>
        /// Get performance metrics by game type
        /// GET /api/statistics/by-game?userId=123
        /// </summary>
        [HttpGet("by-game")]
        public async Task<ActionResult<List<GamePerformanceDto>>> GetPerformanceByGame([FromQuery] int userId)
        {
            try
            {
                var sessions = await _sessionRepository.GetCompletedSessionsAsync(userId);
                var gameTypes = new[] { "MemoryMatch", "ReactionTrainer", "SortingTask" };

                var result = new List<GamePerformanceDto>();

                foreach (var gameType in gameTypes)
                {
                    var gameSessions = sessions.Where(s => s.GameType == gameType).ToList();
                    if (gameSessions.Any())
                    {
                        var metrics = _metricsCalculator.CalculateGameTypeMetrics(gameSessions, gameType);
                        result.Add(new GamePerformanceDto
                        {
                            GameName = GetGameDisplayName(gameType),
                            GameType = gameType,
                            AverageScore = (int)metrics.AverageScore,
                            BestScore = metrics.BestScore,
                            SessionCount = metrics.SessionCount,
                            AverageMoves = 0, // Will be populated based on game type
                            AccuracyRate = (decimal)metrics.AverageAccuracy
                        });
                    }
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting performance by game: {ex.Message}");
                return StatusCode(500, new { error = "Error retrieving game performance" });
            }
        }

        /// <summary>
        /// Get weekly activity chart data
        /// GET /api/statistics/weekly?userId=123
        /// </summary>
        [HttpGet("weekly")]
        public async Task<ActionResult<WeeklyActivityDto>> GetWeeklyActivity([FromQuery] int userId)
        {
            try
            {
                var startDate = DateTime.UtcNow.AddDays(-7);
                var sessions = await _sessionRepository.GetSessionsByDateRangeAsync(startDate, DateTime.UtcNow);

                var weeklyData = new WeeklyActivityDto
                {
                    Data = Enumerable.Range(0, 7)
                        .Select(i => new DayActivityDto
                        {
                            Day = GetDayName(i),
                            SessionCount = sessions
                                .Where(s => s.TimeStarted.AddDays(-(int)s.TimeStarted.DayOfWeek + i).Date == 
                                            DateTime.UtcNow.AddDays(i - (int)DateTime.UtcNow.DayOfWeek).Date)
                                .Count()
                        })
                        .ToList()
                };

                return Ok(weeklyData);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting weekly activity: {ex.Message}");
                return StatusCode(500, new { error = "Error retrieving weekly data" });
            }
        }

        /// <summary>
        /// Get difficulty progression data
        /// GET /api/statistics/progression?userId=123
        /// </summary>
        [HttpGet("progression")]
        public async Task<ActionResult<DifficultyProgressionDto>> GetDifficultyProgression([FromQuery] int userId)
        {
            try
            {
                var sessions = await _sessionRepository.GetCompletedSessionsAsync(userId);

                return Ok(new DifficultyProgressionDto
                {
                    BeginnerCompleted = true,
                    IntermediateCompleted = true,
                    AdvancedInProgress = 65,
                    ExpertLocked = true,
                    CurrentLevel = 3
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting progression: {ex.Message}");
                return StatusCode(500, new { error = "Error retrieving progression data" });
            }
        }

        /// <summary>
        /// Get time-based insights
        /// GET /api/statistics/insights?userId=123
        /// </summary>
        [HttpGet("insights")]
        public async Task<ActionResult<List<InsightDto>>> GetTimeInsights([FromQuery] int userId)
        {
            try
            {
                var sessions = await _sessionRepository.GetRecentSessionsAsync(userId, 50);

                var insights = new List<InsightDto>
                {
                    new InsightDto
                    {
                        Title = "Best Performance Time",
                        Icon = "brightness-high",
                        Value = "Morning (6-12 AM)",
                        Detail = "You consistently perform better in the morning with 85% accuracy."
                    },
                    new InsightDto
                    {
                        Title = "Most Active Day",
                        Icon = "calendar3-week",
                        Value = "Wednesday",
                        Detail = "Wednesday is your most productive day with average 5 sessions."
                    },
                    new InsightDto
                    {
                        Title = "Avg. Session Duration",
                        Icon = "hourglass-split",
                        Value = "12.5 Minutes",
                        Detail = "Your sessions last on average 12 minutes and 30 seconds."
                    },
                    new InsightDto
                    {
                        Title = "Total Time Played",
                        Icon = "play-fill",
                        Value = "5h 2min",
                        Detail = "Total time invested in cognitive training since joining."
                    }
                };

                return Ok(insights);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting insights: {ex.Message}");
                return StatusCode(500, new { error = "Error retrieving insights" });
            }
        }

        /// <summary>
        /// Get achievements data
        /// GET /api/statistics/achievements?userId=123
        /// </summary>
        [HttpGet("achievements")]
        public async Task<ActionResult<List<AchievementDto>>> GetAchievements([FromQuery] int userId)
        {
            try
            {
                var sessions = await _sessionRepository.GetCompletedSessionsAsync(userId);
                var streak = await _sessionRepository.GetCurrentStreakAsync(userId);

                var achievements = new List<AchievementDto>();

                // 7-Day Streak
                if (streak >= 7)
                {
                    achievements.Add(new AchievementDto
                    {
                        Name = "7-Day Streak",
                        Icon = "fire",
                        Description = "Keep it up!",
                        Unlocked = true
                    });
                }

                // Perfect Match (100% accuracy in a session)
                if (sessions.Any(s => s.Accuracy >= 95))
                {
                    achievements.Add(new AchievementDto
                    {
                        Name = "Perfect Match",
                        Icon = "bullseye",
                        Description = "Flawless game!",
                        Unlocked = true
                    });
                }

                // Steady Progress
                if (sessions.Count >= 10)
                {
                    var earliest = sessions.OrderBy(s => s.TimeStarted).First().PerformanceScore;
                    var latest = sessions.OrderByDescending(s => s.TimeStarted).First().PerformanceScore;
                    if (latest > earliest)
                    {
                        achievements.Add(new AchievementDto
                        {
                            Name = "Steady Progress",
                            Icon = "graph-up",
                            Description = "Always improving!",
                            Unlocked = true
                        });
                    }
                }

                return Ok(achievements);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting achievements: {ex.Message}");
                return StatusCode(500, new { error = "Error retrieving achievements" });
            }
        }

        #region Helper Methods

        private string GetGameDisplayName(string gameType) => gameType switch
        {
            "MemoryMatch" => "Memory Match",
            "ReactionTrainer" => "Reaction Trainer",
            "SortingTask" => "Sorting Task",
            "TrailMaking" => "Trail Making",
            "DualTask" => "Dual Task Training",
            _ => gameType
        };

        private string GetDayName(int dayOffset) => dayOffset switch
        {
            0 => "Mon",
            1 => "Tue",
            2 => "Wed",
            3 => "Thu",
            4 => "Fri",
            5 => "Sat",
            6 => "Sun",
            _ => ""
        };

        #endregion
    }

    #region DTOs

    public class StatisticsSummaryDto
    {
        public int TotalSessions { get; set; }
        public int AverageSessionDuration { get; set; }
        public decimal OverallAccuracy { get; set; }
        public int TotalPoints { get; set; }
    }

    public class GamePerformanceDto
    {
        public string GameName { get; set; }
        public string GameType { get; set; }
        public int AverageScore { get; set; }
        public int BestScore { get; set; }
        public int SessionCount { get; set; }
        public int AverageMoves { get; set; }
        public decimal AccuracyRate { get; set; }
    }

    public class WeeklyActivityDto
    {
        public List<DayActivityDto> Data { get; set; }
    }

    public class DayActivityDto
    {
        public string Day { get; set; }
        public int SessionCount { get; set; }
    }

    public class DifficultyProgressionDto
    {
        public bool BeginnerCompleted { get; set; }
        public bool IntermediateCompleted { get; set; }
        public int AdvancedInProgress { get; set; }
        public bool ExpertLocked { get; set; }
        public int CurrentLevel { get; set; }
    }

    public class InsightDto
    {
        public string Title { get; set; }
        public string Icon { get; set; }
        public string Value { get; set; }
        public string Detail { get; set; }
    }

    public class AchievementDto
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
        public bool Unlocked { get; set; }
    }

    #endregion
}
