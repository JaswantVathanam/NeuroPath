namespace NeuroPath.Models
{
    /// <summary>
    /// Overall statistics summary for a user
    /// </summary>
    public class StatisticsSummaryDto
    {
        public int TotalSessions { get; set; }
        public double OverallAccuracy { get; set; }
        public int AverageDuration { get; set; }
        public int TotalPoints { get; set; }
    }

    /// <summary>
    /// Performance metrics for a specific game
    /// </summary>
    public class GamePerformanceDto
    {
        public string? GameName { get; set; }
        public int AverageScore { get; set; }
        public int BestScore { get; set; }
        public int SessionCount { get; set; }
        public double AccuracyRate { get; set; }
    }

    /// <summary>
    /// Weekly activity data
    /// </summary>
    public class WeeklyActivityDto
    {
        public List<DayActivityDto>? DailyActivity { get; set; }
    }

    /// <summary>
    /// Daily activity for a specific day
    /// </summary>
    public class DayActivityDto
    {
        public string? Day { get; set; }
        public int SessionCount { get; set; }
    }

    /// <summary>
    /// Difficulty progression across games
    /// </summary>
    public class DifficultyProgressionDto
    {
        public int BeginnerCompleted { get; set; }
        public int IntermediateCompleted { get; set; }
        public int AdvancedInProgress { get; set; }
        public int ExpertLocked { get; set; }
        public int CurrentLevel { get; set; }
    }

    /// <summary>
    /// Time-based insights
    /// </summary>
    public class InsightDto
    {
        public string? Title { get; set; }
        public string? Icon { get; set; }
        public string? Value { get; set; }
        public string? Detail { get; set; }
    }

    /// <summary>
    /// User achievements
    /// </summary>
    public class AchievementDto
    {
        public string? Name { get; set; }
        public string? Icon { get; set; }
        public string? Description { get; set; }
        public bool Unlocked { get; set; }
    }
}
