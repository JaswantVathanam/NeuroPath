using System;
using System.Collections.Generic;
using System.Linq;

namespace AdaptiveCognitiveRehabilitationPlatform.Services
{
    /// <summary>
    /// AI Service for calculating and adjusting game difficulty based on player performance
    /// </summary>
    public class AIDifficultyService
    {
        public class PerformanceMetrics
        {
            public int Accuracy { get; set; } // 0-100%
            public int ReactionTime { get; set; } // in milliseconds
            public int CompletionTime { get; set; } // in seconds
            public int ConsecutiveCorrect { get; set; } // consecutive correct answers
            public int TotalAttempts { get; set; }
            public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        }

        public class DifficultyAdjustment
        {
            public int NewDifficulty { get; set; }
            public string? Reason { get; set; }
            public double ConfidenceScore { get; set; } // 0-1
            public bool ShouldIncrease { get; set; }
            public bool ShouldDecrease { get; set; }
            public string? Recommendation { get; set; }
        }

        private const int MIN_DIFFICULTY = 1;
        private const int MAX_DIFFICULTY = 5;
        private const int SAMPLES_FOR_DECISION = 3; // How many sessions to analyze

        /// <summary>
        /// Calculate difficulty adjustment based on performance metrics
        /// </summary>
        public DifficultyAdjustment CalculateDifficultyAdjustment(
            int currentDifficulty,
            List<PerformanceMetrics> recentPerformance)
        {
            if (recentPerformance == null || recentPerformance.Count == 0)
            {
                return new DifficultyAdjustment
                {
                    NewDifficulty = currentDifficulty,
                    Reason = "Insufficient performance data",
                    ConfidenceScore = 0.0,
                    ShouldIncrease = false,
                    ShouldDecrease = false,
                    Recommendation = "Continue playing to build performance history"
                };
            }

            // Calculate performance scores
            var avgAccuracy = recentPerformance.Average(p => p.Accuracy);
            var avgReactionTime = recentPerformance.Average(p => p.ReactionTime);
            var avgCompletionTime = recentPerformance.Average(p => p.CompletionTime);
            var avgConsecutiveCorrect = recentPerformance.Average(p => p.ConsecutiveCorrect);

            // Calculate improvement trend (comparing recent vs older sessions)
            var recentScore = CalculatePerformanceScore(avgAccuracy, avgReactionTime, currentDifficulty);
            var improvementTrend = CalculateImprovementTrend(recentPerformance);

            // Decision logic
            bool shouldIncrease = recentScore > 75 && improvementTrend > 0;
            bool shouldDecrease = recentScore < 40 || improvementTrend < -10;

            int newDifficulty = currentDifficulty;
            double confidenceScore = 0.0;
            string reason = "";
            string recommendation = "";

            if (shouldIncrease && currentDifficulty < MAX_DIFFICULTY)
            {
                newDifficulty = Math.Min(currentDifficulty + 1, MAX_DIFFICULTY);
                confidenceScore = Math.Min(1.0, (recentScore - 75) / 25.0);
                reason = $"Excellent performance (Score: {recentScore:F1}, Improvement: {improvementTrend:F1}%)";
                recommendation = $"✓ You're doing great! Level increased to {GetDifficultyName(newDifficulty)}. Challenge yourself!";
            }
            else if (shouldDecrease && currentDifficulty > MIN_DIFFICULTY)
            {
                newDifficulty = Math.Max(currentDifficulty - 1, MIN_DIFFICULTY);
                confidenceScore = Math.Min(1.0, (40 - recentScore) / 40.0);
                reason = $"Performance needs improvement (Score: {recentScore:F1})";
                recommendation = $"Level adjusted to {GetDifficultyName(newDifficulty)} to help you improve steadily.";
            }
            else
            {
                confidenceScore = 0.7;
                reason = $"Consistent performance (Score: {recentScore:F1})";
                recommendation = $"Keep practicing at {GetDifficultyName(currentDifficulty)} level!";
            }

            return new DifficultyAdjustment
            {
                NewDifficulty = newDifficulty,
                Reason = reason,
                ConfidenceScore = confidenceScore,
                ShouldIncrease = shouldIncrease && currentDifficulty < MAX_DIFFICULTY,
                ShouldDecrease = shouldDecrease && currentDifficulty > MIN_DIFFICULTY,
                Recommendation = recommendation
            };
        }

        /// <summary>
        /// Calculate overall performance score (0-100)
        /// </summary>
        private double CalculatePerformanceScore(double accuracy, double avgReactionTime, int difficulty)
        {
            // Weight accuracy heavily (60%)
            var accuracyScore = accuracy * 0.6;

            // Reaction time component (40%)
            // Normalize based on difficulty
            var reactionWeight = GetReactionTimeWeight(difficulty);
            var reactionScore = Math.Max(0, 100 - (avgReactionTime / reactionWeight)) * 0.4;

            return Math.Min(100, accuracyScore + reactionScore);
        }

        /// <summary>
        /// Get baseline reaction time weight for difficulty
        /// </summary>
        private double GetReactionTimeWeight(int difficulty)
        {
            return difficulty switch
            {
                1 => 3000,  // Easy: more lenient on time
                2 => 2500,
                3 => 2000,
                4 => 1500,
                5 => 1000,  // Hard: stricter on time
                _ => 2000
            };
        }

        /// <summary>
        /// Calculate improvement trend as percentage change
        /// </summary>
        private double CalculateImprovementTrend(List<PerformanceMetrics> performances)
        {
            if (performances.Count < 2) return 0;

            var sorted = performances.OrderBy(p => p.Timestamp).ToList();
            var firstHalf = sorted.Take(sorted.Count / 2).ToList();
            var secondHalf = sorted.Skip(sorted.Count / 2).ToList();

            if (firstHalf.Count == 0 || secondHalf.Count == 0) return 0;

            var firstAvg = firstHalf.Average(p => p.Accuracy);
            var secondAvg = secondHalf.Average(p => p.Accuracy);

            return ((secondAvg - firstAvg) / firstAvg) * 100;
        }

        /// <summary>
        /// Get human-readable difficulty name
        /// </summary>
        public string GetDifficultyName(int difficulty)
        {
            return difficulty switch
            {
                1 => "Beginner",
                2 => "Easy",
                3 => "Medium",
                4 => "Hard",
                5 => "Expert",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Check if player is ready for next difficulty
        /// </summary>
        public bool IsReadyForNextDifficulty(int currentDifficulty, List<PerformanceMetrics> recentPerformance)
        {
            if (currentDifficulty >= MAX_DIFFICULTY || recentPerformance.Count < SAMPLES_FOR_DECISION)
                return false;

            var adjustment = CalculateDifficultyAdjustment(currentDifficulty, recentPerformance);
            return adjustment.ShouldIncrease && adjustment.ConfidenceScore > 0.7;
        }

        /// <summary>
        /// Generate performance summary for caretaker
        /// </summary>
        public string GeneratePerformanceSummary(int gameNumber, string gameName, PerformanceMetrics metrics, int difficulty)
        {
            return $"Game {gameNumber}: {gameName} (Difficulty: {GetDifficultyName(difficulty)})\n" +
                   $"  • Accuracy: {metrics.Accuracy}%\n" +
                   $"  • Time: {metrics.CompletionTime}s\n" +
                   $"  • Reaction Time: {metrics.ReactionTime}ms\n" +
                   $"  • Streak: {metrics.ConsecutiveCorrect} consecutive correct";
        }
    }
}
