using System.Text.Json;
using System.Text.Json.Serialization;

namespace AdaptiveCognitiveRehabilitationPlatform.Services;

/// <summary>
/// JSON-based Game Statistics Service
/// Stores all game sessions in JSON files for easy analysis and portability
/// </summary>
public interface IJsonGameStatsService
{
    Task SaveGameSessionAsync(GameStatsEntry entry);
    Task<List<GameStatsEntry>> GetAllSessionsAsync();
    Task<List<GameStatsEntry>> GetSessionsByUserIdAsync(int userId);
    Task<List<GameStatsEntry>> GetSessionsByGameTypeAsync(string gameType);
    Task<UserGameSummary> GetUserSummaryAsync(int userId);
    Task<List<UserGameSummary>> GetAllUserSummariesAsync();
    Task<TherapistAnalyticsData> GetTherapistAnalyticsAsync();
}

/// <summary>
/// Represents a single game session entry stored in JSON
/// </summary>
public class GameStatsEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string GameType { get; set; } = "";
    public string GameMode { get; set; } = "Practice";
    public int Difficulty { get; set; }
    public string DifficultyName { get; set; } = "";
    
    // Performance metrics
    public int Score { get; set; }
    public decimal Accuracy { get; set; }
    public int TotalMoves { get; set; }
    public int CorrectMoves { get; set; }
    public int ErrorCount { get; set; }
    public int TimeTakenSeconds { get; set; }
    
    // Game-specific data
    public int? MatchedPairs { get; set; }
    public int? TotalPairs { get; set; }
    public double? AverageReactionTimeMs { get; set; }
    public int? ItemsSorted { get; set; }
    public int? TotalItems { get; set; }
    
    // Pattern Copy specific
    public int? PatternSize { get; set; }
    public int? GridSize { get; set; }
    public int? TotalRounds { get; set; }
    public int? CorrectPatterns { get; set; }
    
    // AI Analysis
    public string? AiEncouragement { get; set; }
    public string? AiFunMessage { get; set; }
    public string? AiEffortNote { get; set; }
    public string? RecommendedDifficulty { get; set; }
    public double? AiConfidence { get; set; }
    
    // Timestamps
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Metadata
    public string? Notes { get; set; }
    public string Status { get; set; } = "Completed";
}

/// <summary>
/// Summary of a user's game statistics
/// </summary>
public class UserGameSummary
{
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public int TotalGamesPlayed { get; set; }
    public int TotalTimePlayed { get; set; } // in seconds
    public decimal AverageScore { get; set; }
    public int BestScore { get; set; }
    public decimal AverageAccuracy { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateTime? LastPlayedDate { get; set; }
    
    public List<GameTypeStats> GameTypeBreakdown { get; set; } = new();
    public List<DailyActivity> WeeklyActivity { get; set; } = new();
    public List<GameStatsEntry> RecentSessions { get; set; } = new();
}

/// <summary>
/// Statistics for a specific game type
/// </summary>
public class GameTypeStats
{
    public string GameType { get; set; } = "";
    public string GameIcon { get; set; } = "";
    public string GameColor { get; set; } = "";
    public int GamesPlayed { get; set; }
    public decimal AverageScore { get; set; }
    public int BestScore { get; set; }
    public decimal AverageAccuracy { get; set; }
    public int TotalTimePlayed { get; set; }
    public int CurrentLevel { get; set; }
    public decimal SkillProgress { get; set; } // 0-100
    public decimal AverageMoves { get; set; } // For MemoryMatch
    public double AverageReactionTime { get; set; } // For ReactionTrainer (ms)
}

/// <summary>
/// Daily activity data for charts
/// </summary>
public class DailyActivity
{
    public string Day { get; set; } = "";
    public DateTime Date { get; set; }
    public int GamesPlayed { get; set; }
    public decimal AverageScore { get; set; }
}

/// <summary>
/// Analytics data for therapists
/// </summary>
public class TherapistAnalyticsData
{
    public int TotalPatients { get; set; }
    public int TotalSessionsThisWeek { get; set; }
    public decimal AveragePatientScore { get; set; }
    public List<PatientSummary> PatientSummaries { get; set; } = new();
    public List<GameTypeDistribution> GameDistribution { get; set; } = new();
}

/// <summary>
/// Patient summary for therapist view
/// </summary>
public class PatientSummary
{
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public int TotalSessions { get; set; }
    public decimal AverageScore { get; set; }
    public int BestScore { get; set; }
    public decimal AverageAccuracy { get; set; }
    public DateTime? LastSessionDate { get; set; }
    public string ProgressTrend { get; set; } = "Stable"; // Improving, Stable, Needs Attention
    public Dictionary<string, int> GameTypeBreakdown { get; set; } = new();
}

/// <summary>
/// Game type distribution for analytics
/// </summary>
public class GameTypeDistribution
{
    public string GameType { get; set; } = "";
    public int SessionCount { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// Implementation of JSON-based game stats service
/// </summary>
public class JsonGameStatsService : IJsonGameStatsService
{
    private readonly string _dataDirectory;
    private readonly string _statsFilePath;
    private readonly ILogger<JsonGameStatsService> _logger;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public JsonGameStatsService(ILogger<JsonGameStatsService> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _dataDirectory = Path.Combine(env.ContentRootPath, "GameData");
        _statsFilePath = Path.Combine(_dataDirectory, "game_sessions.json");
        
        // Ensure directory exists
        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
            _logger.LogInformation($"Created game data directory: {_dataDirectory}");
        }
        
        // Initialize empty file if it doesn't exist
        if (!File.Exists(_statsFilePath))
        {
            File.WriteAllText(_statsFilePath, "[]");
            _logger.LogInformation($"Created game sessions file: {_statsFilePath}");
        }
    }

    public async Task SaveGameSessionAsync(GameStatsEntry entry)
    {
        await _fileLock.WaitAsync();
        try
        {
            var sessions = await LoadSessionsFromFileAsync();
            entry.Id = Guid.NewGuid().ToString();
            entry.CreatedAt = DateTime.UtcNow;
            sessions.Add(entry);
            
            await SaveSessionsToFileAsync(sessions);
            _logger.LogInformation($"✅ Saved game session: {entry.GameType} for user {entry.UserId} - Score: {entry.Score}");
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<List<GameStatsEntry>> GetAllSessionsAsync()
    {
        return await LoadSessionsFromFileAsync();
    }

    public async Task<List<GameStatsEntry>> GetSessionsByUserIdAsync(int userId)
    {
        var sessions = await LoadSessionsFromFileAsync();
        return sessions.Where(s => s.UserId == userId)
                      .OrderByDescending(s => s.CreatedAt)
                      .ToList();
    }

    public async Task<List<GameStatsEntry>> GetSessionsByGameTypeAsync(string gameType)
    {
        var sessions = await LoadSessionsFromFileAsync();
        return sessions.Where(s => s.GameType.Equals(gameType, StringComparison.OrdinalIgnoreCase))
                      .OrderByDescending(s => s.CreatedAt)
                      .ToList();
    }

    public async Task<UserGameSummary> GetUserSummaryAsync(int userId)
    {
        var sessions = await GetSessionsByUserIdAsync(userId);
        
        if (sessions.Count == 0)
        {
            return new UserGameSummary
            {
                UserId = userId,
                TotalGamesPlayed = 0
            };
        }

        var summary = new UserGameSummary
        {
            UserId = userId,
            Username = sessions.First().Username,
            TotalGamesPlayed = sessions.Count,
            TotalTimePlayed = sessions.Sum(s => s.TimeTakenSeconds),
            AverageScore = (decimal)sessions.Average(s => s.Score),
            BestScore = sessions.Max(s => s.Score),
            AverageAccuracy = sessions.Average(s => s.Accuracy),
            LastPlayedDate = sessions.Max(s => s.CreatedAt),
            RecentSessions = sessions.Take(10).ToList()
        };

        // Calculate streaks
        var (currentStreak, longestStreak) = CalculateStreaks(sessions);
        summary.CurrentStreak = currentStreak;
        summary.LongestStreak = longestStreak;

        // Game type breakdown
        summary.GameTypeBreakdown = sessions
            .GroupBy(s => s.GameType)
            .Select(g => new GameTypeStats
            {
                GameType = g.Key,
                GameIcon = GetGameIcon(g.Key),
                GameColor = GetGameColor(g.Key),
                GamesPlayed = g.Count(),
                AverageScore = (decimal)g.Average(s => s.Score),
                BestScore = g.Max(s => s.Score),
                AverageAccuracy = g.Average(s => s.Accuracy),
                TotalTimePlayed = g.Sum(s => s.TimeTakenSeconds),
                CurrentLevel = g.Max(s => s.Difficulty),
                SkillProgress = CalculateSkillProgress(g.ToList()),
                AverageMoves = (decimal)g.Average(s => s.TotalMoves),
                AverageReactionTime = g.Where(s => s.AverageReactionTimeMs.HasValue)
                    .Select(s => s.AverageReactionTimeMs!.Value)
                    .DefaultIfEmpty(0)
                    .Average()
            })
            .ToList();

        // Weekly activity (last 7 days)
        var today = DateTime.UtcNow.Date;
        summary.WeeklyActivity = Enumerable.Range(0, 7)
            .Select(i => today.AddDays(-6 + i))
            .Select(date => new DailyActivity
            {
                Day = date.ToString("ddd"),
                Date = date,
                GamesPlayed = sessions.Count(s => s.CreatedAt.Date == date),
                AverageScore = sessions.Where(s => s.CreatedAt.Date == date).Any() 
                    ? (decimal)sessions.Where(s => s.CreatedAt.Date == date).Average(s => s.Score) 
                    : 0
            })
            .ToList();

        return summary;
    }

    public async Task<List<UserGameSummary>> GetAllUserSummariesAsync()
    {
        var sessions = await LoadSessionsFromFileAsync();
        var userIds = sessions.Select(s => s.UserId).Distinct();
        
        var summaries = new List<UserGameSummary>();
        foreach (var userId in userIds)
        {
            summaries.Add(await GetUserSummaryAsync(userId));
        }
        
        return summaries.OrderByDescending(s => s.LastPlayedDate).ToList();
    }

    public async Task<TherapistAnalyticsData> GetTherapistAnalyticsAsync()
    {
        var sessions = await LoadSessionsFromFileAsync();
        var weekAgo = DateTime.UtcNow.AddDays(-7);
        
        var analytics = new TherapistAnalyticsData
        {
            TotalPatients = sessions.Select(s => s.UserId).Distinct().Count(),
            TotalSessionsThisWeek = sessions.Count(s => s.CreatedAt >= weekAgo),
            AveragePatientScore = sessions.Any() ? (decimal)sessions.Average(s => s.Score) : 0
        };

        // Patient summaries
        var userGroups = sessions.GroupBy(s => s.UserId);
        foreach (var group in userGroups)
        {
            var userSessions = group.OrderByDescending(s => s.CreatedAt).ToList();
            var latestSession = userSessions.First();
            
            analytics.PatientSummaries.Add(new PatientSummary
            {
                UserId = group.Key,
                Username = latestSession.Username,
                FirstName = latestSession.Username.Split(' ').FirstOrDefault() ?? "",
                LastName = latestSession.Username.Split(' ').LastOrDefault() ?? "",
                TotalSessions = userSessions.Count,
                AverageScore = (decimal)userSessions.Average(s => s.Score),
                BestScore = userSessions.Max(s => s.Score),
                AverageAccuracy = userSessions.Average(s => s.Accuracy),
                LastSessionDate = latestSession.CreatedAt,
                ProgressTrend = CalculateProgressTrend(userSessions),
                GameTypeBreakdown = userSessions
                    .GroupBy(s => s.GameType)
                    .ToDictionary(g => g.Key, g => g.Count())
            });
        }

        // Game distribution
        var totalSessions = sessions.Count;
        analytics.GameDistribution = sessions
            .GroupBy(s => s.GameType)
            .Select(g => new GameTypeDistribution
            {
                GameType = g.Key,
                SessionCount = g.Count(),
                Percentage = totalSessions > 0 ? (decimal)g.Count() / totalSessions * 100 : 0
            })
            .OrderByDescending(g => g.SessionCount)
            .ToList();

        return analytics;
    }

    // Helper methods
    private async Task<List<GameStatsEntry>> LoadSessionsFromFileAsync()
    {
        try
        {
            if (!File.Exists(_statsFilePath))
            {
                return new List<GameStatsEntry>();
            }

            var json = await File.ReadAllTextAsync(_statsFilePath);
            return JsonSerializer.Deserialize<List<GameStatsEntry>>(json, _jsonOptions) ?? new List<GameStatsEntry>();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading sessions from file: {ex.Message}");
            return new List<GameStatsEntry>();
        }
    }

    private async Task SaveSessionsToFileAsync(List<GameStatsEntry> sessions)
    {
        var json = JsonSerializer.Serialize(sessions, _jsonOptions);
        await File.WriteAllTextAsync(_statsFilePath, json);
    }

    private (int current, int longest) CalculateStreaks(List<GameStatsEntry> sessions)
    {
        if (sessions.Count == 0) return (0, 0);

        var dates = sessions
            .Select(s => s.CreatedAt.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        int currentStreak = 0;
        int longestStreak = 0;
        int tempStreak = 1;
        var today = DateTime.UtcNow.Date;

        // Current streak
        if (dates.Contains(today) || dates.Contains(today.AddDays(-1)))
        {
            currentStreak = 1;
            var checkDate = dates.Contains(today) ? today.AddDays(-1) : today.AddDays(-2);
            
            while (dates.Contains(checkDate))
            {
                currentStreak++;
                checkDate = checkDate.AddDays(-1);
            }
        }

        // Longest streak
        for (int i = 1; i < dates.Count; i++)
        {
            if ((dates[i - 1] - dates[i]).Days == 1)
            {
                tempStreak++;
            }
            else
            {
                longestStreak = Math.Max(longestStreak, tempStreak);
                tempStreak = 1;
            }
        }
        longestStreak = Math.Max(longestStreak, tempStreak);

        return (currentStreak, longestStreak);
    }

    private decimal CalculateSkillProgress(List<GameStatsEntry> sessions)
    {
        if (sessions.Count == 0) return 0;

        // Factor in games played, accuracy, and difficulty level achieved
        var maxDifficulty = sessions.Max(s => s.Difficulty);
        var avgAccuracy = sessions.Average(s => s.Accuracy);
        var gamesPlayed = Math.Min(sessions.Count, 20); // Cap at 20 for calculation

        // Weight: 40% difficulty progress, 40% accuracy, 20% consistency
        var difficultyProgress = (maxDifficulty / 3.0m) * 40;
        var accuracyProgress = avgAccuracy * 0.4m;
        var consistencyProgress = (gamesPlayed / 20.0m) * 20;

        return Math.Min(100, difficultyProgress + accuracyProgress + consistencyProgress);
    }

    private string CalculateProgressTrend(List<GameStatsEntry> sessions)
    {
        if (sessions.Count < 3) return "Stable";

        var recentAvg = sessions.Take(3).Average(s => s.Score);
        var previousAvg = sessions.Skip(3).Take(3).Average(s => s.Score);

        if (recentAvg > previousAvg * 1.1) return "Improving";
        if (recentAvg < previousAvg * 0.9) return "Needs Attention";
        return "Stable";
    }

    private string GetGameIcon(string gameType) => gameType switch
    {
        "MemoryMatch" => "bi-memory",
        "ReactionTrainer" => "bi-lightning-fill",
        "SortingTask" => "bi-sort-down",
        "TrailMaking" => "bi-bezier2",
        "DualTask" => "bi-diagram-3",
        "StroopTest" => "bi-palette",
        _ => "bi-controller"
    };

    private string GetGameColor(string gameType) => gameType switch
    {
        "MemoryMatch" => "#6366f1",
        "ReactionTrainer" => "#f59e0b",
        "SortingTask" => "#10b981",
        "TrailMaking" => "#0ea5e9",
        "DualTask" => "#ec4899",
        "StroopTest" => "#a855f7",
        _ => "#6b7280"
    };
}
