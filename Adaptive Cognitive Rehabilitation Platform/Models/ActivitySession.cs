namespace AdaptiveCognitiveRehabilitationPlatform.Models;

/// <summary>
/// Represents a completed activity session for cognitive rehabilitation activities
/// Activities are distinct from games - they focus on wellness, journaling, breathing, etc.
/// </summary>
public class ActivitySession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int UserId { get; set; }
    public string? Username { get; set; }
    public string ActivityType { get; set; } = "";
    public string? ActivityName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int DurationSeconds { get; set; }
    
    // Common metrics
    public int? Score { get; set; }
    public decimal? Accuracy { get; set; }
    public int? CompletionPercentage { get; set; }
    public string? Difficulty { get; set; }
    public string? Status { get; set; } = "Completed";
    
    // Activity-specific data stored as JSON
    public string? ActivityData { get; set; }
    
    // Mood/Wellness tracking
    public string? MoodBefore { get; set; }
    public string? MoodAfter { get; set; }
    
    // AI Analysis
    public string? AiEncouragement { get; set; }
    public string? AiFeedback { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Daily Journal specific data
/// </summary>
public class JournalActivityData
{
    public string? Title { get; set; }
    public int WordCount { get; set; }
    public string? Mood { get; set; }
    public string? PromptUsed { get; set; }
    public int EntryLength { get; set; }
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Word Association specific data
/// </summary>
public class WordAssociationData
{
    public int RoundsCompleted { get; set; }
    public int TotalRounds { get; set; }
    public int CorrectAssociations { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public int HintsUsed { get; set; }
    public List<string>? WordsUsed { get; set; }
}

/// <summary>
/// Breathing Exercise specific data
/// </summary>
public class BreathingExerciseData
{
    public string? ExerciseType { get; set; } // "4-7-8", "Box", "Calm"
    public int CyclesCompleted { get; set; }
    public int TotalCycles { get; set; }
    public int TotalBreathingTimeSeconds { get; set; }
    public bool CompletedFully { get; set; }
}

/// <summary>
/// Story Recall specific data
/// </summary>
public class StoryRecallData
{
    public string? StoryTitle { get; set; }
    public int QuestionsAnswered { get; set; }
    public int CorrectAnswers { get; set; }
    public int DetailsCaptured { get; set; }
    public int TotalDetails { get; set; }
    public double RecallAccuracy { get; set; }
}

/// <summary>
/// Mental Math specific data
/// </summary>
public class MentalMathData
{
    public int ProblemsAttempted { get; set; }
    public int ProblemsCorrect { get; set; }
    public string? OperationType { get; set; } // "Addition", "Subtraction", "Mixed"
    public double AverageTimePerProblemMs { get; set; }
    public int CurrentStreak { get; set; }
    public int MaxStreak { get; set; }
    public int DifficultyLevel { get; set; }
}

/// <summary>
/// Focus Tracker specific data
/// </summary>
public class FocusTrackerData
{
    public int FocusSessionMinutes { get; set; }
    public int BreaksTaken { get; set; }
    public int DistractionsLogged { get; set; }
    public string? FocusGoal { get; set; }
    public bool GoalAchieved { get; set; }
    public int ProductivityRating { get; set; } // 1-10
}

/// <summary>
/// Word Puzzles specific data
/// </summary>
public class WordPuzzlesData
{
    public string? PuzzleType { get; set; } // "Crossword", "WordSearch", "Anagram"
    public int PuzzlesCompleted { get; set; }
    public int TotalPuzzles { get; set; }
    public int WordsFound { get; set; }
    public int HintsUsed { get; set; }
    public double AverageTimePerPuzzleSeconds { get; set; }
}

/// <summary>
/// Number Sequence specific data
/// </summary>
public class NumberSequenceData
{
    public int SequencesCompleted { get; set; }
    public int TotalSequences { get; set; }
    public int CorrectPatterns { get; set; }
    public string? SequenceTypes { get; set; } // "Arithmetic", "Geometric", "Fibonacci", "Mixed"
    public int MaxSequenceLength { get; set; }
    public double AverageTimePerSequenceSeconds { get; set; }
}

/// <summary>
/// Request model for saving activity session
/// </summary>
public class SaveActivitySessionRequest
{
    public int UserId { get; set; }
    public string? Username { get; set; }
    public string ActivityType { get; set; } = "";
    public string? ActivityName { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int DurationSeconds { get; set; }
    public int? Score { get; set; }
    public decimal? Accuracy { get; set; }
    public int? CompletionPercentage { get; set; }
    public string? Difficulty { get; set; }
    public string? ActivityData { get; set; }
    public string? MoodBefore { get; set; }
    public string? MoodAfter { get; set; }
}

/// <summary>
/// Summary of a user's activity performance
/// </summary>
public class UserActivitySummary
{
    public int UserId { get; set; }
    public string? Username { get; set; }
    public int TotalActivities { get; set; }
    public int TotalTimeSpentMinutes { get; set; }
    public DateTime? LastActivityDate { get; set; }
    public int ActivitiesThisWeek { get; set; }
    public int CurrentStreak { get; set; }
    public List<ActivityTypeStats> ActivityBreakdown { get; set; } = new();
    public List<ActivitySession> RecentActivities { get; set; } = new();
    public MoodTrendData? MoodTrends { get; set; }
}

/// <summary>
/// Statistics for a specific activity type
/// </summary>
public class ActivityTypeStats
{
    public string ActivityType { get; set; } = "";
    public string ActivityName { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Color { get; set; } = "";
    public int TotalSessions { get; set; }
    public int TotalTimeMinutes { get; set; }
    public double AverageScore { get; set; }
    public double AverageAccuracy { get; set; }
    public int CompletionRate { get; set; }
    public DateTime? LastSession { get; set; }
}

/// <summary>
/// Mood trend analysis data
/// </summary>
public class MoodTrendData
{
    public string MostCommonMoodBefore { get; set; } = "";
    public string MostCommonMoodAfter { get; set; } = "";
    public double MoodImprovementRate { get; set; }
    public List<MoodEntry> RecentMoods { get; set; } = new();
}

public class MoodEntry
{
    public DateTime Date { get; set; }
    public string MoodBefore { get; set; } = "";
    public string MoodAfter { get; set; } = "";
}

/// <summary>
/// Therapist analytics for activities
/// </summary>
public class ActivityTherapistAnalytics
{
    public int TotalPatients { get; set; }
    public int TotalActivitySessions { get; set; }
    public int TotalTimeSpentMinutes { get; set; }
    public double OverallCompletionRate { get; set; }
    public List<ActivityDistribution> ActivityDistribution { get; set; } = new();
    public List<UserActivitySummary> PatientSummaries { get; set; } = new();
    public Dictionary<string, double> MoodImprovementByActivity { get; set; } = new();
}

public class ActivityDistribution
{
    public string ActivityType { get; set; } = "";
    public string ActivityName { get; set; } = "";
    public int SessionCount { get; set; }
    public double Percentage { get; set; }
}
