namespace NeuroPath.Models
{
    /// <summary>
    /// Overall progress summary for a user profile
    /// </summary>
    public class ProgressSummaryDto
    {
        public int TotalGamesPlayed { get; set; }
        public int CurrentStreak { get; set; }
        public int AverageScore { get; set; }
        public int CurrentLevel { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// Progress details for a specific game
    /// </summary>
    public class GameProgressDto
    {
        public string? GameName { get; set; }
        public int GamesPlayed { get; set; }
        public int BestScore { get; set; }
        public int AverageScore { get; set; }
        public double Accuracy { get; set; }
        public double ProgressPercentage { get; set; }
        public string? Color { get; set; }
    }

    /// <summary>
    /// Activity log entry for recent games
    /// </summary>
    public class ActivityLogDto
    {
        public string? GameName { get; set; }
        public int DifficultyLevel { get; set; }
        public int Score { get; set; }
        public int Duration { get; set; }
        public double Accuracy { get; set; }
        public string? TimestampAgo { get; set; }
        public string? Badge { get; set; }
    }

    /// <summary>
    /// AI-generated recommendation for the user
    /// </summary>
    public class RecommendationDto
    {
        public string? Icon { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? IconColor { get; set; }
    }

    /// <summary>
    /// Complete AI analysis of user's performance
    /// </summary>
    public class AIAnalysisDto
    {
        public string? Message { get; set; }
        public string[]? Strengths { get; set; }
        public string[]? AreasForImprovement { get; set; }
        public int RecommendedDifficulty { get; set; }
        public double ImprovementTrendPercentage { get; set; }
        public double AverageAccuracy { get; set; }
        public int SessionCount { get; set; }
        public int BestScore { get; set; }
    }
}
