using System.Text.Json;
using AdaptiveCognitiveRehabilitationPlatform.Models;

namespace AdaptiveCognitiveRehabilitationPlatform.Services;

/// <summary>
/// JSON-based Activity Statistics Service
/// Stores all activity sessions in JSON files for easy analysis and portability
/// Similar to JsonGameStatsService but for wellness activities
/// </summary>
public interface IJsonActivityStatsService
{
    Task<List<ActivitySession>> GetAllSessionsAsync();
    Task<List<ActivitySession>> GetSessionsByUserIdAsync(int userId);
    Task<List<ActivityTypeStats>> GetActivityProgressByUserIdAsync(int userId);
    Task<List<ActivityTypeStats>> GetTherapistActivityStatsAsync();
}

public class JsonActivityStatsService : IJsonActivityStatsService
{
    private readonly string _activityDataPath;
    private readonly ILogger<JsonActivityStatsService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonActivityStatsService(ILogger<JsonActivityStatsService> logger)
    {
        _logger = logger;
        _activityDataPath = Path.Combine(Directory.GetCurrentDirectory(), "GameData", "activity_sessions.json");
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
    }

    public async Task<List<ActivitySession>> GetAllSessionsAsync()
    {
        try
        {
            if (!File.Exists(_activityDataPath))
            {
                _logger.LogInformation("[ACTIVITY-SERVICE] No activity sessions file found");
                return new List<ActivitySession>();
            }

            var json = await File.ReadAllTextAsync(_activityDataPath);
            var sessions = JsonSerializer.Deserialize<List<ActivitySession>>(json, _jsonOptions);
            _logger.LogInformation($"[ACTIVITY-SERVICE] Loaded {sessions?.Count ?? 0} activity sessions");
            return sessions ?? new List<ActivitySession>();
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ACTIVITY-SERVICE] Error loading sessions: {ex.Message}");
            return new List<ActivitySession>();
        }
    }

    public async Task<List<ActivitySession>> GetSessionsByUserIdAsync(int userId)
    {
        var allSessions = await GetAllSessionsAsync();
        var userSessions = allSessions.Where(s => s.UserId == userId).ToList();
        _logger.LogInformation($"[ACTIVITY-SERVICE] Found {userSessions.Count} sessions for user {userId}");
        return userSessions;
    }

    public async Task<List<ActivityTypeStats>> GetActivityProgressByUserIdAsync(int userId)
    {
        try
        {
            var sessions = await GetSessionsByUserIdAsync(userId);
            
            if (!sessions.Any())
            {
                _logger.LogInformation($"[ACTIVITY-SERVICE] No sessions found for user {userId}");
                return new List<ActivityTypeStats>();
            }

            var stats = sessions.GroupBy(s => s.ActivityType)
                .Select(g => new ActivityTypeStats
                {
                    ActivityType = g.Key,
                    ActivityName = GetActivityDisplayName(g.Key),
                    Icon = GetActivityIcon(g.Key),
                    Color = GetActivityColor(g.Key),
                    TotalSessions = g.Count(),
                    TotalTimeMinutes = (int)(g.Sum(s => s.DurationSeconds) / 60),
                    AverageScore = g.Where(s => s.Score.HasValue).Any() 
                        ? g.Where(s => s.Score.HasValue).Average(s => s.Score!.Value) 
                        : 0,
                    AverageAccuracy = g.Where(s => s.Accuracy.HasValue).Any() 
                        ? (double)g.Where(s => s.Accuracy.HasValue).Average(s => s.Accuracy!.Value) 
                        : 0,
                    CompletionRate = g.Any() 
                        ? (int)(g.Count(s => s.Status == "Completed") * 100.0 / g.Count()) 
                        : 0,
                    LastSession = g.Max(s => s.EndTime)
                })
                .OrderByDescending(a => a.TotalSessions)
                .ToList();

            _logger.LogInformation($"[ACTIVITY-SERVICE] Generated {stats.Count} activity stats for user {userId}");
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ACTIVITY-SERVICE] Error getting progress: {ex.Message}");
            return new List<ActivityTypeStats>();
        }
    }

    public async Task<List<ActivityTypeStats>> GetTherapistActivityStatsAsync()
    {
        try
        {
            var sessions = await GetAllSessionsAsync();
            
            if (!sessions.Any())
            {
                return new List<ActivityTypeStats>();
            }

            var stats = sessions.GroupBy(s => s.ActivityType)
                .Select(g => new ActivityTypeStats
                {
                    ActivityType = g.Key,
                    ActivityName = GetActivityDisplayName(g.Key),
                    Icon = GetActivityIcon(g.Key),
                    Color = GetActivityColor(g.Key),
                    TotalSessions = g.Count(),
                    TotalTimeMinutes = (int)(g.Sum(s => s.DurationSeconds) / 60),
                    AverageScore = g.Where(s => s.Score.HasValue).Any() 
                        ? g.Where(s => s.Score.HasValue).Average(s => s.Score!.Value) 
                        : 0,
                    AverageAccuracy = g.Where(s => s.Accuracy.HasValue).Any() 
                        ? (double)g.Where(s => s.Accuracy.HasValue).Average(s => s.Accuracy!.Value) 
                        : 0,
                    CompletionRate = g.Any() 
                        ? (int)(g.Count(s => s.Status == "Completed") * 100.0 / g.Count()) 
                        : 0,
                    LastSession = g.Max(s => s.EndTime)
                })
                .OrderByDescending(a => a.TotalSessions)
                .ToList();

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ACTIVITY-SERVICE] Error getting therapist stats: {ex.Message}");
            return new List<ActivityTypeStats>();
        }
    }

    private string GetActivityDisplayName(string activityType) => activityType switch
    {
        "DailyJournal" => "Daily Journal",
        "WordAssociation" => "Word Association",
        "BreathingExercise" => "Breathing Exercise",
        "StoryRecall" => "Story Recall",
        "MentalMath" => "Mental Math",
        "FocusTracker" => "Focus Tracker",
        "WordPuzzles" => "Word Puzzles",
        "NumberSequence" => "Number Sequence",
        _ => activityType
    };

    private string GetActivityIcon(string activityType) => activityType switch
    {
        "DailyJournal" => "bi-journal-text",
        "WordAssociation" => "bi-link-45deg",
        "BreathingExercise" => "bi-wind",
        "StoryRecall" => "bi-book",
        "MentalMath" => "bi-calculator",
        "FocusTracker" => "bi-eye",
        "WordPuzzles" => "bi-puzzle",
        "NumberSequence" => "bi-123",
        _ => "bi-activity"
    };

    private string GetActivityColor(string activityType) => activityType switch
    {
        "DailyJournal" => "#9b59b6",
        "WordAssociation" => "#3498db",
        "BreathingExercise" => "#1abc9c",
        "StoryRecall" => "#e67e22",
        "MentalMath" => "#e74c3c",
        "FocusTracker" => "#2ecc71",
        "WordPuzzles" => "#f39c12",
        "NumberSequence" => "#9b59b6",
        _ => "#95a5a6"
    };
}
