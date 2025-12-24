using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NeuroPath.Models;

namespace AdaptiveCognitiveRehabilitationPlatform.Services.GameAnalytics
{
    /// <summary>
    /// Calculates various performance metrics from game sessions
    /// </summary>
    public class PerformanceMetricsCalculator
    {
        private readonly ILogger<PerformanceMetricsCalculator> _logger;

        public PerformanceMetricsCalculator(ILogger<PerformanceMetricsCalculator> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Calculate metrics for a completed session
        /// </summary>
        public void CalculateSessionMetrics(GameSession session)
        {
            try
            {
                // Calculate Accuracy (only for games with correct/total moves)
                if (session.TotalMoves > 0)
                {
                    session.Accuracy = (decimal)(session.CorrectMatches / (double)session.TotalMoves) * 100;
                    session.Accuracy = Math.Min(100, Math.Max(0, session.Accuracy)); // Clamp 0-100
                }

                // Calculate Efficiency Score (optimal vs actual moves)
                if (session.CorrectMatches > 0)
                {
                    int optimalMoves = session.CorrectMatches;
                    decimal efficiency = (decimal)(optimalMoves / (double)session.TotalMoves) * 100;
                    session.EfficiencyScore = Math.Min(100, Math.Max(0, efficiency)); // Clamp 0-100
                }

                // Calculate Overall Performance Score (weighted combination)
                session.PerformanceScore = CalculatePerformanceScore(
                    session.Accuracy,
                    session.EfficiencyScore,
                    session.TotalSeconds,
                    session.Difficulty
                );

                _logger.LogInformation($"Calculated metrics for session {session.SessionId}: Accuracy={session.Accuracy:F2}%, Performance={session.PerformanceScore}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calculating metrics for session {session.SessionId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Calculate overall performance score (0-100) based on multiple factors
        /// </summary>
        private int CalculatePerformanceScore(decimal accuracy, decimal efficiency, int timeTaken, int difficulty)
        {
            // Base score from accuracy (40% weight)
            decimal accuracyScore = accuracy * 0.40m;

            // Efficiency bonus (40% weight)
            decimal efficiencyScore = efficiency * 0.40m;

            // Difficulty bonus (20% weight) - harder difficulty = higher score for same accuracy
            decimal difficultyBonus = (difficulty * 20) * 0.20m;

            // Time penalty (only penalize if significantly over expected time)
            // Expected time = 30 seconds + 10 seconds per difficulty level
            int expectedTime = 30 + (difficulty * 10);
            decimal timeBonus = 0;
            if (timeTaken <= expectedTime)
            {
                // Faster = better score
                timeBonus = ((decimal)(expectedTime - timeTaken) / expectedTime) * 10;
            }
            else
            {
                // Slower = small penalty
                decimal penalty = Math.Min(10, ((decimal)(timeTaken - expectedTime) / expectedTime) * 5);
                timeBonus = -penalty;
            }

            decimal totalScore = accuracyScore + efficiencyScore + difficultyBonus + timeBonus;
            return (int)Math.Min(100, Math.Max(0, totalScore));
        }

        /// <summary>
        /// Calculate average metrics from a collection of sessions
        /// </summary>
        public AggregatedMetrics CalculateAggregatedMetrics(List<GameSession> sessions)
        {
            if (!sessions.Any())
            {
                return new AggregatedMetrics();
            }

            var completedSessions = sessions.Where(s => s.Status == "Completed").ToList();
            if (!completedSessions.Any())
            {
                return new AggregatedMetrics();
            }

            var metrics = new AggregatedMetrics
            {
                TotalSessions = completedSessions.Count,
                AverageAccuracy = completedSessions.Average(s => (double)s.Accuracy),
                BestScore = completedSessions.Max(s => s.PerformanceScore),
                LowestScore = completedSessions.Min(s => s.PerformanceScore),
                AverageScore = completedSessions.Average(s => s.PerformanceScore),
                AverageDuration = (int)completedSessions.Average(s => s.TotalSeconds),
                AverageEfficiency = completedSessions.Average(s => (double)s.EfficiencyScore),
                TotalTime = completedSessions.Sum(s => s.TotalSeconds),
            };

            // Calculate improvement trend (latest 5 vs earliest 5)
            if (completedSessions.Count >= 10)
            {
                var earliest5 = completedSessions.OrderBy(s => s.TimeStarted).Take(5).ToList();
                var latest5 = completedSessions.OrderByDescending(s => s.TimeStarted).Take(5).ToList();

                double earlyAvg = earliest5.Average(s => s.PerformanceScore);
                double lateAvg = latest5.Average(s => s.PerformanceScore);
                metrics.ImprovementTrend = lateAvg - earlyAvg;
                metrics.ImprovementPercentage = earlyAvg > 0 ? (metrics.ImprovementTrend / earlyAvg) * 100 : 0;
            }

            return metrics;
        }

        /// <summary>
        /// Calculate improvement velocity (score per week)
        /// </summary>
        public double CalculateImprovementVelocity(List<GameSession> sessions)
        {
            if (sessions.Count < 2)
                return 0;

            var completedSessions = sessions.Where(s => s.Status == "Completed").OrderBy(s => s.TimeStarted).ToList();
            if (completedSessions.Count < 2)
                return 0;

            var firstSession = completedSessions.First();
            var lastSession = completedSessions.Last();

            var timeSpan = lastSession.TimeStarted - firstSession.TimeStarted;
            if (timeSpan.TotalDays < 1)
                return 0;

            var scoreImprovement = lastSession.PerformanceScore - firstSession.PerformanceScore;
            var weeksElapsed = timeSpan.TotalDays / 7;

            return scoreImprovement / weeksElapsed;
        }

        /// <summary>
        /// Get game-specific metrics
        /// </summary>
        public GameTypeMetrics CalculateGameTypeMetrics(List<GameSession> sessions, string gameType)
        {
            var gameSessions = sessions.Where(s => s.GameType == gameType && s.Status == "Completed").ToList();

            if (!gameSessions.Any())
            {
                return new GameTypeMetrics { GameType = gameType };
            }

            return new GameTypeMetrics
            {
                GameType = gameType,
                SessionCount = gameSessions.Count,
                AverageScore = gameSessions.Average(s => s.PerformanceScore),
                BestScore = gameSessions.Max(s => s.PerformanceScore),
                AverageAccuracy = gameSessions.Average(s => (double)s.Accuracy),
                MostUsedDifficulty = gameSessions.GroupBy(s => s.Difficulty)
                    .OrderByDescending(g => g.Count())
                    .First()
                    .Key,
                AverageTime = (int)gameSessions.Average(s => s.TotalSeconds)
            };
        }
    }

    /// <summary>
    /// Container for aggregated metrics
    /// </summary>
    public class AggregatedMetrics
    {
        public int TotalSessions { get; set; }
        public double AverageAccuracy { get; set; }
        public int BestScore { get; set; }
        public int LowestScore { get; set; }
        public double AverageScore { get; set; }
        public int AverageDuration { get; set; }
        public double AverageEfficiency { get; set; }
        public int TotalTime { get; set; }
        public double ImprovementTrend { get; set; }
        public double ImprovementPercentage { get; set; }
    }

    /// <summary>
    /// Container for game-type specific metrics
    /// </summary>
    public class GameTypeMetrics
    {
        public string GameType { get; set; }
        public int SessionCount { get; set; }
        public double AverageScore { get; set; }
        public int BestScore { get; set; }
        public double AverageAccuracy { get; set; }
        public int MostUsedDifficulty { get; set; }
        public int AverageTime { get; set; }
    }
}
