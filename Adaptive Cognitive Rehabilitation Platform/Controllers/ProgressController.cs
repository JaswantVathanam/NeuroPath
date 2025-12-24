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
    /// API Controller for progress and personalized insights
    /// Used by Progress.razor page to display AI-generated analysis and recommendations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProgressController : ControllerBase
    {
        private readonly IGameSessionRepository _sessionRepository;
        private readonly AIAnalysisEngine _aiAnalysisEngine;
        private readonly PerformanceMetricsCalculator _metricsCalculator;
        private readonly ILogger<ProgressController> _logger;

        public ProgressController(
            IGameSessionRepository sessionRepository,
            AIAnalysisEngine aiAnalysisEngine,
            PerformanceMetricsCalculator metricsCalculator,
            ILogger<ProgressController> logger)
        {
            _sessionRepository = sessionRepository;
            _aiAnalysisEngine = aiAnalysisEngine;
            _metricsCalculator = metricsCalculator;
            _logger = logger;
        }

        /// <summary>
        /// Get overall progress summary for a user profile
        /// GET /api/progress/summary?profileId=123
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult<ProgressSummaryDto>> GetProgressSummary([FromQuery] int profileId)
        {
            try
            {
                var sessions = await _sessionRepository.GetSessionsByProfileIdAsync(profileId);
                
                if (!sessions.Any())
                {
                    return Ok(new ProgressSummaryDto
                    {
                        TotalGamesPlayed = 0,
                        CurrentStreak = 0,
                        AverageScore = 0,
                        CurrentLevel = 1,
                        Message = "Start playing games to track your progress!"
                    });
                }

                var completedSessions = sessions.Where(s => !string.IsNullOrEmpty(s.Status) && s.Status == "Completed").ToList();
                var metrics = _metricsCalculator.CalculateAggregatedMetrics(completedSessions);
                int userId = sessions.First().UserId;
                var streak = await _sessionRepository.GetCurrentStreakAsync(userId);

                return Ok(new ProgressSummaryDto
                {
                    TotalGamesPlayed = completedSessions.Count,
                    CurrentStreak = streak,
                    AverageScore = (int)metrics.AverageScore,
                    CurrentLevel = GetCurrentLevel((int)metrics.AverageScore),
                    Message = GenerateProgressMessage(metrics)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting progress summary: {ex.Message}");
                return StatusCode(500, new { error = "Error retrieving progress" });
            }
        }

        /// <summary>
        /// Get game-wise progress for all three games
        /// GET /api/progress/by-game?profileId=123
        /// </summary>
        [HttpGet("by-game")]
        public async Task<ActionResult<List<GameProgressDto>>> GetProgressByGame([FromQuery] int profileId)
        {
            try
            {
                var sessions = await _sessionRepository.GetSessionsByProfileIdAsync(profileId);
                var completedSessions = sessions.Where(s => s.Status == "Completed").ToList();

                var result = new List<GameProgressDto>();
                var gameTypes = new[] { "MemoryMatch", "ReactionTrainer", "SortingTask" };

                foreach (var gameType in gameTypes)
                {
                    var gameSessions = completedSessions.Where(s => s.GameType == gameType).ToList();

                    if (gameSessions.Any())
                    {
                        var metrics = _metricsCalculator.CalculateGameTypeMetrics(gameSessions, gameType);
                        var progressPercent = CalculateProgressPercentage(metrics.AverageScore);

                        result.Add(new GameProgressDto
                        {
                            GameName = GetGameDisplayName(gameType),
                            GameType = gameType,
                            GamesPlayed = metrics.SessionCount,
                            BestScore = metrics.BestScore,
                            AverageScore = (int)metrics.AverageScore,
                            Accuracy = (decimal)metrics.AverageAccuracy,
                            ProgressPercentage = progressPercent,
                            Color = GetGameColor(gameType)
                        });
                    }
                    else
                    {
                        result.Add(new GameProgressDto
                        {
                            GameName = GetGameDisplayName(gameType),
                            GameType = gameType,
                            GamesPlayed = 0,
                            BestScore = 0,
                            AverageScore = 0,
                            Accuracy = 0,
                            ProgressPercentage = 0,
                            Color = GetGameColor(gameType)
                        });
                    }
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting progress by game: {ex.Message}");
                return StatusCode(500, new { error = "Error retrieving game progress" });
            }
        }

        /// <summary>
        /// Get recent activity log
        /// GET /api/progress/recent-activity?profileId=123&limit=10
        /// </summary>
        [HttpGet("recent-activity")]
        public async Task<ActionResult<List<ActivityLogDto>>> GetRecentActivity([FromQuery] int profileId, [FromQuery] int limit = 10)
        {
            try
            {
                var sessions = await _sessionRepository.GetRecentSessionsAsync(
                    (await _sessionRepository.GetSessionsByProfileIdAsync(profileId)).First().UserId,
                    limit);

                var activities = sessions
                    .Where(s => s.Status == "Completed")
                    .Select(s => new ActivityLogDto
                    {
                        GameName = GetGameDisplayName(s.GameType),
                        GameType = s.GameType,
                        DifficultyLevel = s.Difficulty,
                        Score = s.PerformanceScore,
                        Duration = s.TotalSeconds,
                        Accuracy = (decimal)s.Accuracy,
                        TimestampAgo = GetTimeAgo(s.TimeCompleted ?? DateTime.UtcNow),
                        Badge = GetScoreBadge(s.PerformanceScore)
                    })
                    .ToList();

                return Ok(activities);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting recent activity: {ex.Message}");
                return StatusCode(500, new { error = "Error retrieving activity log" });
            }
        }

        /// <summary>
        /// Get personalized recommendations from AI analysis
        /// GET /api/progress/recommendations?profileId=123
        /// </summary>
        [HttpGet("recommendations")]
        public async Task<ActionResult<List<RecommendationDto>>> GetRecommendations([FromQuery] int profileId)
        {
            try
            {
                var sessions = await _sessionRepository.GetSessionsByProfileIdAsync(profileId);
                var completedSessions = sessions.Where(s => s.Status == "Completed").ToList();

                if (!completedSessions.Any())
                {
                    return Ok(new List<RecommendationDto>
                    {
                        new RecommendationDto
                        {
                            Icon = "check-circle-fill",
                            Title = "Get Started!",
                            Description = "Begin with an easy difficulty to build confidence.",
                            IconColor = "success"
                        }
                    });
                }

                var recommendations = new List<RecommendationDto>();

                // Recommendation 1: Performance-based
                var avgScore = completedSessions.Average(s => s.PerformanceScore);
                if (avgScore >= 85)
                {
                    recommendations.Add(new RecommendationDto
                    {
                        Icon = "graph-up",
                        Title = "Next Challenge",
                        Description = "Try increasing difficulty - you're performing very well!",
                        IconColor = "primary"
                    });
                }
                else if (avgScore < 60)
                {
                    recommendations.Add(new RecommendationDto
                    {
                        Icon = "target",
                        Title = "Focus on Accuracy",
                        Description = "Aim for accuracy over speed. Take your time and be deliberate.",
                        IconColor = "warning"
                    });
                }

                // Recommendation 2: Streak-based
                var streak = await _sessionRepository.GetCurrentStreakAsync(sessions.First().UserId);
                if (streak >= 7)
                {
                    recommendations.Add(new RecommendationDto
                    {
                        Icon = "fire",
                        Title = "Keep Your Streak!",
                        Description = "Amazing consistency! Keep playing daily to maintain momentum.",
                        IconColor = "danger"
                    });
                }
                else
                {
                    recommendations.Add(new RecommendationDto
                    {
                        Icon = "calendar3-week",
                        Title = "Build Consistency",
                        Description = "Aim for daily sessions to build a strong practice habit.",
                        IconColor = "info"
                    });
                }

                // Recommendation 3: Game variety
                var gameTypes = completedSessions.Select(s => s.GameType).Distinct().ToList();
                if (gameTypes.Count < 3)
                {
                    var missingGames = GetMissingGameNames(gameTypes);
                    recommendations.Add(new RecommendationDto
                    {
                        Icon = "joystick",
                        Title = "Try New Games",
                        Description = $"Explore {string.Join(" and ", missingGames)} to work on different skills.",
                        IconColor = "success"
                    });
                }

                return Ok(recommendations.Take(3).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting recommendations: {ex.Message}");
                return StatusCode(500, new { error = "Error retrieving recommendations" });
            }
        }

        /// <summary>
        /// Get detailed AI analysis for a profile
        /// GET /api/progress/ai-analysis?profileId=123
        /// </summary>
        [HttpGet("ai-analysis")]
        public async Task<ActionResult<AIAnalysisDto>> GetAIAnalysis([FromQuery] int profileId)
        {
            try
            {
                var sessions = await _sessionRepository.GetSessionsByProfileIdAsync(profileId);
                var completedSessions = sessions.Where(s => s.Status == "Completed").ToList();

                if (!completedSessions.Any())
                {
                    return Ok(new AIAnalysisDto
                    {
                        Message = "No sessions completed yet. Start playing to get AI analysis!",
                        Strengths = new List<string>(),
                        AreasForImprovement = new List<string>(),
                        RecommendedDifficulty = 1
                    });
                }

                var metrics = _metricsCalculator.CalculateAggregatedMetrics(completedSessions);
                var strengths = IdentifyStrengths(sessions);
                var improvements = IdentifyImprovements(sessions);
                var recommendedDifficulty = CalculateRecommendedDifficulty(metrics);

                var analysis = new AIAnalysisDto
                {
                    Message = GenerateAIMessage(metrics, strengths),
                    Strengths = strengths,
                    AreasForImprovement = improvements,
                    RecommendedDifficulty = recommendedDifficulty,
                    ImprovementTrendPercentage = (decimal)metrics.ImprovementPercentage,
                    AverageAccuracy = (decimal)metrics.AverageAccuracy,
                    SessionCount = metrics.TotalSessions,
                    BestScore = metrics.BestScore
                };

                return Ok(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting AI analysis: {ex.Message}");
                return StatusCode(500, new { error = "Error retrieving AI analysis" });
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

        private string GetGameColor(string gameType) => gameType switch
        {
            "MemoryMatch" => "#6366f1",
            "ReactionTrainer" => "#f59e0b",
            "SortingTask" => "#10b981",
            "TrailMaking" => "#0ea5e9",
            "DualTask" => "#ec4899",
            _ => "#6b7280"
        };

        private int GetCurrentLevel(int averageScore)
        {
            if (averageScore >= 90) return 5;
            if (averageScore >= 80) return 4;
            if (averageScore >= 70) return 3;
            if (averageScore >= 60) return 2;
            return 1;
        }

        private int CalculateProgressPercentage(double averageScore)
        {
            return Math.Min(100, Math.Max(0, (int)averageScore));
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var now = DateTime.UtcNow;
            var span = now - dateTime;

            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
            return dateTime.ToString("MMM d");
        }

        private string GetScoreBadge(int score)
        {
            if (score >= 90) return "Excellent";
            if (score >= 80) return "Good";
            if (score >= 70) return "Fair";
            if (score >= 60) return "Needs Work";
            return "Try Again";
        }

        private string GenerateProgressMessage(AggregatedMetrics metrics)
        {
            if (metrics.ImprovementPercentage > 10)
                return "Great improvement! Keep it up!";
            if (metrics.AverageAccuracy > 80)
                return "Excellent accuracy! You're doing great!";
            if (metrics.TotalSessions >= 10)
                return "Consistent practice is paying off!";
            return "Building your skills session by session!";
        }

        private List<string> GetMissingGameNames(List<string> playedGames)
        {
            var allGames = new Dictionary<string, string>
            {
                { "MemoryMatch", "Memory Match" },
                { "ReactionTrainer", "Reaction Trainer" },
                { "SortingTask", "Sorting Task" }
            };

            return allGames
                .Where(g => !playedGames.Contains(g.Key, StringComparer.OrdinalIgnoreCase))
                .Select(g => g.Value)
                .ToList();
        }

        private List<string> IdentifyStrengths(List<GameSession> sessions)
        {
            var strengths = new List<string>();
            var completedSessions = sessions.Where(s => s.Status == "Completed").ToList();

            var memoryAvg = completedSessions.Where(s => s.GameType == "MemoryMatch").Average(s => (double?)s.PerformanceScore) ?? 0;
            var reactionAvg = completedSessions.Where(s => s.GameType == "ReactionTrainer").Average(s => (double?)s.PerformanceScore) ?? 0;
            var sortingAvg = completedSessions.Where(s => s.GameType == "SortingTask").Average(s => (double?)s.PerformanceScore) ?? 0;

            if (memoryAvg >= 75) strengths.Add("Strong memory recall");
            if (reactionAvg >= 75) strengths.Add("Quick reaction time");
            if (sortingAvg >= 75) strengths.Add("Excellent categorization");

            return strengths.Any() ? strengths : new List<string> { "Developing cognitive skills" };
        }

        private List<string> IdentifyImprovements(List<GameSession> sessions)
        {
            var improvements = new List<string>();
            var completedSessions = sessions.Where(s => s.Status == "Completed").ToList();

            var memoryAvg = completedSessions.Where(s => s.GameType == "MemoryMatch").Average(s => (double?)s.PerformanceScore) ?? 0;
            var reactionAvg = completedSessions.Where(s => s.GameType == "ReactionTrainer").Average(s => (double?)s.PerformanceScore) ?? 0;
            var sortingAvg = completedSessions.Where(s => s.GameType == "SortingTask").Average(s => (double?)s.PerformanceScore) ?? 0;

            if (memoryAvg < 75 && memoryAvg > 0) improvements.Add("Focus on memory strategies");
            if (reactionAvg < 75 && reactionAvg > 0) improvements.Add("Practice reaction drills");
            if (sortingAvg < 75 && sortingAvg > 0) improvements.Add("Work on categorization skills");

            return improvements.Any() ? improvements : new List<string> { "All areas improving!" };
        }

        private int CalculateRecommendedDifficulty(AggregatedMetrics metrics)
        {
            if (metrics.AverageScore >= 85) return Math.Min(5, 3 + 1);
            if (metrics.AverageScore < 60) return Math.Max(1, 3 - 1);
            return 3;
        }

        private string GenerateAIMessage(AggregatedMetrics metrics, List<string> strengths)
        {
            var strengthStr = string.Join(" and ", strengths);
            if (metrics.ImprovementPercentage > 15)
                return $"Excellent progress! Your {strengthStr} are shining through. Keep pushing forward!";
            return $"You're building strong {strengthStr}. Consistent practice will yield great results!";
        }

        #endregion
    }

    #region DTOs

    public class ProgressSummaryDto
    {
        public int TotalGamesPlayed { get; set; }
        public int CurrentStreak { get; set; }
        public int AverageScore { get; set; }
        public int CurrentLevel { get; set; }
        public string? Message { get; set; }
    }

    public class GameProgressDto
    {
        public string? GameName { get; set; }
        public string? GameType { get; set; }
        public int GamesPlayed { get; set; }
        public int BestScore { get; set; }
        public int AverageScore { get; set; }
        public decimal Accuracy { get; set; }
        public int ProgressPercentage { get; set; }
        public string? Color { get; set; }
    }

    public class ActivityLogDto
    {
        public string? GameName { get; set; }
        public string? GameType { get; set; }
        public int DifficultyLevel { get; set; }
        public int Score { get; set; }
        public int Duration { get; set; }
        public decimal Accuracy { get; set; }
        public string? TimestampAgo { get; set; }
        public string? Badge { get; set; }
    }

    public class RecommendationDto
    {
        public string? Icon { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? IconColor { get; set; }
    }

    public class AIAnalysisDto
    {
        public string? Message { get; set; }
        public List<string>? Strengths { get; set; }
        public List<string>? AreasForImprovement { get; set; }
        public int RecommendedDifficulty { get; set; }
        public decimal ImprovementTrendPercentage { get; set; }
        public decimal AverageAccuracy { get; set; }
        public int SessionCount { get; set; }
        public int BestScore { get; set; }
    }

    #endregion
}
