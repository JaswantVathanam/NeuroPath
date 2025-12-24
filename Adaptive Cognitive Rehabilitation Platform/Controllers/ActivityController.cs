using Microsoft.AspNetCore.Mvc;
using AdaptiveCognitiveRehabilitationPlatform.Models;
using System.Text.Json;

namespace AdaptiveCognitiveRehabilitationPlatform.Controllers;

/// <summary>
/// API Controller for Activity Sessions
/// Handles saving, retrieving, and analyzing cognitive rehabilitation activities
/// </summary>
[ApiController]
[Route("api/activities")]
public class ActivityController : ControllerBase
{
    private readonly ILogger<ActivityController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _activityDataPath;
    private const string AI_ENDPOINT = "http://localhost:1234/v1/chat/completions";

    public ActivityController(
        ILogger<ActivityController> logger,
        IHttpClientFactory httpClientFactory,
        IWebHostEnvironment env)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _activityDataPath = Path.Combine(env.ContentRootPath, "GameData", "activity_sessions.json");
    }

    /// <summary>
    /// Save a new activity session
    /// </summary>
    [HttpPost("save")]
    public async Task<IActionResult> SaveActivitySession([FromBody] SaveActivitySessionRequest request)
    {
        try
        {
            _logger.LogInformation($"[ACTIVITIES] Saving activity session: {request.ActivityType} for user {request.UserId}");

            var session = new ActivitySession
            {
                UserId = request.UserId,
                Username = request.Username ?? $"User{request.UserId}",
                ActivityType = request.ActivityType,
                ActivityName = GetActivityDisplayName(request.ActivityType),
                StartTime = request.StartTime ?? DateTime.UtcNow.AddSeconds(-request.DurationSeconds),
                EndTime = request.EndTime ?? DateTime.UtcNow,
                DurationSeconds = request.DurationSeconds,
                Score = request.Score,
                Accuracy = request.Accuracy,
                CompletionPercentage = request.CompletionPercentage ?? 100,
                Difficulty = request.Difficulty,
                ActivityData = request.ActivityData,
                MoodBefore = request.MoodBefore,
                MoodAfter = request.MoodAfter,
                Status = "Completed",
                CreatedAt = DateTime.UtcNow
            };

            await SaveSessionToFileAsync(session);

            return Ok(new
            {
                success = true,
                message = "Activity session saved successfully",
                sessionId = session.Id,
                savedAt = session.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ACTIVITIES] Error saving session: {ex.Message}");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get all activity sessions
    /// </summary>
    [HttpGet("sessions")]
    public async Task<IActionResult> GetAllSessions()
    {
        try
        {
            var sessions = await LoadSessionsFromFileAsync();
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ACTIVITIES] Error getting all sessions: {ex.Message}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get sessions for a specific user
    /// </summary>
    [HttpGet("sessions/user/{userId}")]
    public async Task<IActionResult> GetUserSessions(int userId)
    {
        try
        {
            var sessions = await LoadSessionsFromFileAsync();
            var userSessions = sessions.Where(s => s.UserId == userId).OrderByDescending(s => s.CreatedAt).ToList();
            return Ok(userSessions);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ACTIVITIES] Error getting user sessions: {ex.Message}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get user activity summary
    /// </summary>
    [HttpGet("summary/{userId}")]
    public async Task<IActionResult> GetUserSummary(int userId)
    {
        try
        {
            var sessions = await LoadSessionsFromFileAsync();
            var userSessions = sessions.Where(s => s.UserId == userId).OrderByDescending(s => s.CreatedAt).ToList();

            if (!userSessions.Any())
            {
                return Ok(new UserActivitySummary
                {
                    UserId = userId,
                    TotalActivities = 0
                });
            }

            var weekAgo = DateTime.UtcNow.AddDays(-7);
            var summary = new UserActivitySummary
            {
                UserId = userId,
                Username = userSessions.First().Username,
                TotalActivities = userSessions.Count,
                TotalTimeSpentMinutes = userSessions.Sum(s => s.DurationSeconds) / 60,
                LastActivityDate = userSessions.First().CreatedAt,
                ActivitiesThisWeek = userSessions.Count(s => s.CreatedAt >= weekAgo),
                CurrentStreak = CalculateStreak(userSessions),
                ActivityBreakdown = GetActivityBreakdown(userSessions),
                RecentActivities = userSessions.Take(10).ToList(),
                MoodTrends = GetMoodTrends(userSessions)
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ACTIVITIES] Error getting user summary: {ex.Message}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get therapist analytics for activities
    /// </summary>
    [HttpGet("therapist/analytics")]
    public async Task<IActionResult> GetTherapistAnalytics()
    {
        try
        {
            var sessions = await LoadSessionsFromFileAsync();

            if (!sessions.Any())
            {
                return Ok(new ActivityTherapistAnalytics
                {
                    TotalPatients = 0,
                    TotalActivitySessions = 0
                });
            }

            var userGroups = sessions.GroupBy(s => s.UserId).ToList();
            var analytics = new ActivityTherapistAnalytics
            {
                TotalPatients = userGroups.Count,
                TotalActivitySessions = sessions.Count,
                TotalTimeSpentMinutes = sessions.Sum(s => s.DurationSeconds) / 60,
                OverallCompletionRate = sessions.Average(s => s.CompletionPercentage ?? 100),
                ActivityDistribution = sessions.GroupBy(s => s.ActivityType)
                    .Select(g => new ActivityDistribution
                    {
                        ActivityType = g.Key,
                        ActivityName = GetActivityDisplayName(g.Key),
                        SessionCount = g.Count(),
                        Percentage = (double)g.Count() / sessions.Count * 100
                    }).OrderByDescending(a => a.SessionCount).ToList(),
                PatientSummaries = userGroups.Select(g =>
                {
                    var userSessions = g.OrderByDescending(s => s.CreatedAt).ToList();
                    return new UserActivitySummary
                    {
                        UserId = g.Key,
                        Username = userSessions.First().Username,
                        TotalActivities = userSessions.Count,
                        TotalTimeSpentMinutes = userSessions.Sum(s => s.DurationSeconds) / 60,
                        LastActivityDate = userSessions.First().CreatedAt,
                        ActivityBreakdown = GetActivityBreakdown(userSessions),
                        MoodTrends = GetMoodTrends(userSessions)
                    };
                }).OrderByDescending(u => u.TotalActivities).ToList(),
                MoodImprovementByActivity = CalculateMoodImprovementByActivity(sessions)
            };

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ACTIVITIES] Error getting therapist analytics: {ex.Message}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get activity type statistics for therapist dashboard
    /// </summary>
    [HttpGet("therapist/activity-stats")]
    public async Task<IActionResult> GetActivityTypeStats()
    {
        try
        {
            var sessions = await LoadSessionsFromFileAsync();

            if (!sessions.Any())
            {
                return Ok(new List<ActivityTypeStats>());
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
                    AverageScore = g.Where(s => s.Score.HasValue).Any() ? g.Where(s => s.Score.HasValue).Average(s => s.Score!.Value) : 0,
                    AverageAccuracy = g.Where(s => s.Accuracy.HasValue).Any() ? (double)g.Where(s => s.Accuracy.HasValue).Average(s => s.Accuracy!.Value) : 0,
                    CompletionRate = g.Any() ? (int)(g.Count(s => s.Status == "Completed") * 100.0 / g.Count()) : 0,
                    LastSession = g.Max(s => s.EndTime)
                }).OrderByDescending(a => a.TotalSessions).ToList();

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ACTIVITIES] Error getting activity stats: {ex.Message}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get AI analysis for a user's activity performance
    /// </summary>
    [HttpGet("ai-analysis/{userId}")]
    public async Task<IActionResult> GetActivityAIAnalysis(int userId, [FromQuery] string? activityType = null)
    {
        try
        {
            _logger.LogInformation($"[ACTIVITY-AI] Starting AI analysis for user {userId}, activity: {activityType ?? "all"}");

            var sessions = await LoadSessionsFromFileAsync();
            var userSessions = sessions.Where(s => s.UserId == userId).OrderBy(s => s.CreatedAt).ToList();

            if (!string.IsNullOrEmpty(activityType))
            {
                userSessions = userSessions.Where(s => s.ActivityType?.ToLower() == activityType.ToLower()).ToList();
            }

            if (!userSessions.Any())
            {
                return Ok(new
                {
                    success = false,
                    message = "No activity sessions found for this user",
                    analysis = GetDefaultActivityAnalysis(activityType)
                });
            }

            var username = userSessions.First().Username ?? $"User_{userId}";
            var analysisData = BuildActivityAnalysisData(username, userSessions, activityType);
            var aiResponse = await CallPhiForActivityAnalysis(analysisData, activityType);

            return Ok(new
            {
                success = true,
                userId = userId,
                username = username,
                activityType = activityType,
                totalSessions = userSessions.Count,
                analysis = aiResponse
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ACTIVITY-AI] Error: {ex.Message}");
            return Ok(new
            {
                success = false,
                error = ex.Message,
                analysis = GetDefaultActivityAnalysis(activityType)
            });
        }
    }

    /// <summary>
    /// Get activity progress for user (used by Progress page)
    /// </summary>
    [HttpGet("progress/{userId}")]
    public async Task<IActionResult> GetActivityProgress(int userId)
    {
        try
        {
            _logger.LogInformation($"[ACTIVITIES] GetActivityProgress called for userId: {userId}");
            var sessions = await LoadSessionsFromFileAsync();
            _logger.LogInformation($"[ACTIVITIES] Total sessions loaded from file: {sessions.Count}");
            var userSessions = sessions.Where(s => s.UserId == userId).ToList();
            _logger.LogInformation($"[ACTIVITIES] Sessions for userId {userId}: {userSessions.Count}");

            if (!userSessions.Any())
            {
                _logger.LogWarning($"[ACTIVITIES] No sessions found for userId: {userId}");
                return Ok(new List<object>());
            }

            var stats = userSessions.GroupBy(s => s.ActivityType)
                .Select(g => new
                {
                    activityType = g.Key,
                    activityName = GetActivityDisplayName(g.Key),
                    icon = GetActivityIcon(g.Key),
                    color = GetActivityColor(g.Key),
                    totalSessions = g.Count(),
                    totalTimeMinutes = (int)(g.Sum(s => s.DurationSeconds) / 60),
                    averageScore = g.Where(s => s.Score.HasValue).Any() ? g.Where(s => s.Score.HasValue).Average(s => s.Score!.Value) : 0,
                    averageAccuracy = g.Where(s => s.Accuracy.HasValue).Any() ? (double)g.Where(s => s.Accuracy.HasValue).Average(s => s.Accuracy!.Value) : 0,
                    completionRate = g.Any() ? (int)(g.Count(s => s.Status == "Completed") * 100.0 / g.Count()) : 0,
                    lastSession = (DateTime?)g.Max(s => s.EndTime)
                }).OrderByDescending(a => a.totalSessions).ToList();

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ACTIVITIES] Error getting activity progress: {ex.Message}");
            return Ok(new List<object>());
        }
    }

    /// <summary>
    /// Get weekly activity data for charts
    /// </summary>
    [HttpGet("weekly/{userId}")]
    public async Task<IActionResult> GetWeeklyActivity(int userId)
    {
        try
        {
            var sessions = await LoadSessionsFromFileAsync();
            var userSessions = sessions.Where(s => s.UserId == userId).ToList();

            var weeklyData = Enumerable.Range(0, 7)
                .Select(i => DateTime.UtcNow.Date.AddDays(-6 + i))
                .Select(date => new
                {
                    day = date.ToString("ddd"),
                    date = date,
                    activitiesCompleted = userSessions.Count(s => s.CreatedAt.Date == date),
                    minutesSpent = userSessions.Where(s => s.CreatedAt.Date == date).Sum(s => s.DurationSeconds) / 60
                }).ToList();

            return Ok(weeklyData);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ACTIVITIES] Error getting weekly activity: {ex.Message}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    #region Private Helper Methods

    private async Task SaveSessionToFileAsync(ActivitySession session)
    {
        var sessions = await LoadSessionsFromFileAsync();
        sessions.Add(session);

        var directory = Path.GetDirectoryName(_activityDataPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(sessions, new JsonSerializerOptions { WriteIndented = true });
        await System.IO.File.WriteAllTextAsync(_activityDataPath, json);
    }

    private async Task<List<ActivitySession>> LoadSessionsFromFileAsync()
    {
        if (!System.IO.File.Exists(_activityDataPath))
        {
            return new List<ActivitySession>();
        }

        var json = await System.IO.File.ReadAllTextAsync(_activityDataPath);
        return JsonSerializer.Deserialize<List<ActivitySession>>(json) ?? new List<ActivitySession>();
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
        "FocusTracker" => "bi-bullseye",
        "WordPuzzles" => "bi-puzzle",
        "NumberSequence" => "bi-123",
        _ => "bi-activity"
    };

    private string GetActivityColor(string activityType) => activityType switch
    {
        "DailyJournal" => "#8b5cf6",
        "WordAssociation" => "#06b6d4",
        "BreathingExercise" => "#10b981",
        "StoryRecall" => "#f59e0b",
        "MentalMath" => "#ef4444",
        "FocusTracker" => "#3b82f6",
        "WordPuzzles" => "#ec4899",
        "NumberSequence" => "#6366f1",
        _ => "#6b7280"
    };

    private int CalculateStreak(List<ActivitySession> sessions)
    {
        if (!sessions.Any()) return 0;

        var dates = sessions.Select(s => s.CreatedAt.Date).Distinct().OrderByDescending(d => d).ToList();
        if (!dates.Any()) return 0;

        int streak = 0;
        var checkDate = DateTime.UtcNow.Date;

        // Check if there's activity today or yesterday to start the streak
        if (dates.First() < checkDate.AddDays(-1)) return 0;

        foreach (var date in dates)
        {
            if (date == checkDate || date == checkDate.AddDays(-1))
            {
                streak++;
                checkDate = date.AddDays(-1);
            }
            else if (date < checkDate)
            {
                break;
            }
        }

        return streak;
    }

    private List<ActivityTypeStats> GetActivityBreakdown(List<ActivitySession> sessions)
    {
        return sessions.GroupBy(s => s.ActivityType)
            .Select(g => new ActivityTypeStats
            {
                ActivityType = g.Key,
                ActivityName = GetActivityDisplayName(g.Key),
                Icon = GetActivityIcon(g.Key),
                Color = GetActivityColor(g.Key),
                TotalSessions = g.Count(),
                TotalTimeMinutes = g.Sum(s => s.DurationSeconds) / 60,
                AverageScore = g.Where(s => s.Score.HasValue).Any() ? g.Where(s => s.Score.HasValue).Average(s => s.Score!.Value) : 0,
                AverageAccuracy = g.Where(s => s.Accuracy.HasValue).Any() ? (double)g.Where(s => s.Accuracy.HasValue).Average(s => s.Accuracy!.Value) : 0,
                CompletionRate = (int)g.Average(s => s.CompletionPercentage ?? 100),
                LastSession = g.Max(s => s.CreatedAt)
            }).OrderByDescending(a => a.TotalSessions).ToList();
    }

    private MoodTrendData? GetMoodTrends(List<ActivitySession> sessions)
    {
        var sessionsWithMood = sessions.Where(s => !string.IsNullOrEmpty(s.MoodBefore) || !string.IsNullOrEmpty(s.MoodAfter)).ToList();
        if (!sessionsWithMood.Any()) return null;

        var moodsBefore = sessionsWithMood.Where(s => !string.IsNullOrEmpty(s.MoodBefore)).Select(s => s.MoodBefore!).ToList();
        var moodsAfter = sessionsWithMood.Where(s => !string.IsNullOrEmpty(s.MoodAfter)).Select(s => s.MoodAfter!).ToList();

        return new MoodTrendData
        {
            MostCommonMoodBefore = moodsBefore.GroupBy(m => m).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key ?? "",
            MostCommonMoodAfter = moodsAfter.GroupBy(m => m).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key ?? "",
            MoodImprovementRate = CalculateMoodImprovement(sessionsWithMood),
            RecentMoods = sessionsWithMood.Take(10).Select(s => new MoodEntry
            {
                Date = s.CreatedAt,
                MoodBefore = s.MoodBefore ?? "",
                MoodAfter = s.MoodAfter ?? ""
            }).ToList()
        };
    }

    private double CalculateMoodImprovement(List<ActivitySession> sessions)
    {
        var moodValues = new Dictionary<string, int>
        {
            { "Stressed", 1 }, { "Tired", 2 }, { "Sad", 2 }, { "Anxious", 2 },
            { "Neutral", 3 }, { "Thoughtful", 3 },
            { "Calm", 4 }, { "Hopeful", 4 }, { "Grateful", 4 },
            { "Happy", 5 }, { "Excited", 5 }, { "Peaceful", 5 }
        };

        var improvements = sessions
            .Where(s => !string.IsNullOrEmpty(s.MoodBefore) && !string.IsNullOrEmpty(s.MoodAfter))
            .Where(s => moodValues.ContainsKey(s.MoodBefore!) && moodValues.ContainsKey(s.MoodAfter!))
            .Select(s => moodValues[s.MoodAfter!] - moodValues[s.MoodBefore!])
            .ToList();

        return improvements.Any() ? improvements.Average() : 0;
    }

    private Dictionary<string, double> CalculateMoodImprovementByActivity(List<ActivitySession> sessions)
    {
        return sessions.GroupBy(s => s.ActivityType)
            .ToDictionary(
                g => g.Key,
                g => CalculateMoodImprovement(g.ToList())
            );
    }

    private string BuildActivityAnalysisData(string username, List<ActivitySession> sessions, string? activityType)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== PATIENT: {username} ===");
        sb.AppendLine($"Total Activity Sessions: {sessions.Count}");
        sb.AppendLine($"Date Range: {sessions.First().CreatedAt:MMM d, yyyy} to {sessions.Last().CreatedAt:MMM d, yyyy}");
        sb.AppendLine();

        var activityGroups = sessions.GroupBy(s => s.ActivityType).ToList();
        foreach (var group in activityGroups)
        {
            var activitySessions = group.OrderBy(s => s.CreatedAt).ToList();
            var name = GetActivityDisplayName(group.Key);

            sb.AppendLine($"--- {name} ({activitySessions.Count} sessions) ---");
            sb.AppendLine($"  Total Time: {activitySessions.Sum(s => s.DurationSeconds) / 60} minutes");
            
            if (activitySessions.Any(s => s.Score.HasValue))
                sb.AppendLine($"  Average Score: {activitySessions.Where(s => s.Score.HasValue).Average(s => s.Score!.Value):F0}");
            
            if (activitySessions.Any(s => s.Accuracy.HasValue))
                sb.AppendLine($"  Average Accuracy: {activitySessions.Where(s => s.Accuracy.HasValue).Average(s => s.Accuracy!.Value):F1}%");

            sb.AppendLine($"  Completion Rate: {activitySessions.Average(s => s.CompletionPercentage ?? 100):F0}%");

            // Mood analysis
            var moodSessions = activitySessions.Where(s => !string.IsNullOrEmpty(s.MoodBefore) && !string.IsNullOrEmpty(s.MoodAfter)).ToList();
            if (moodSessions.Any())
            {
                var improvement = CalculateMoodImprovement(moodSessions);
                sb.AppendLine($"  Mood Improvement: {(improvement >= 0 ? "+" : "")}{improvement:F1}");
            }

            sb.AppendLine();
        }

        // Overall wellness metrics
        sb.AppendLine("--- WELLNESS METRICS ---");
        var totalTime = sessions.Sum(s => s.DurationSeconds) / 60;
        sb.AppendLine($"  Total Time Invested: {totalTime} minutes");
        sb.AppendLine($"  Sessions per Week: {sessions.Count / Math.Max(1, (sessions.Last().CreatedAt - sessions.First().CreatedAt).TotalDays / 7):F1}");

        return sb.ToString();
    }

    private async Task<object> CallPhiForActivityAnalysis(string analysisData, string? activityType)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(120);

            var prompt = $@"You are a cognitive rehabilitation therapist AI assistant. Analyze this patient's ACTIVITY (not game) performance data and provide a wellness-focused assessment.

{analysisData}

These are WELLNESS ACTIVITIES, not games. Focus on:
- Emotional well-being and mood improvements
- Mindfulness and relaxation progress
- Cognitive exercise engagement
- Healthy habit formation

Provide your response as JSON with these fields:
- overallWellnessAssessment: Summary of the patient's wellness journey
- activityEngagement: How well they're engaging with activities
- moodAnalysis: Analysis of mood patterns and improvements
- strengthsIdentified: Array of wellness strengths
- areasForGrowth: Array of areas to focus on
- wellnessRecommendations: Array of specific recommendations
- encouragingMessage: A warm, supportive message for the patient
- therapistNotes: Clinical notes for the therapist
- engagementLevel: High/Medium/Low
- wellnessScore: 0-100 overall wellness score

Output ONLY valid JSON.";

            var request = new
            {
                model = "Phi-4-mini",
                messages = new[]
                {
                    new { role = "system", content = "You are a supportive cognitive rehabilitation therapist AI. Focus on wellness, emotional support, and positive reinforcement. Output only valid JSON." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.5,
                max_tokens = 2000,
                response_format = new { type = "json_object" }
            };

            _logger.LogInformation("[ACTIVITY-AI] Sending to Phi-4-mini...");
            var response = await httpClient.PostAsJsonAsync(AI_ENDPOINT, request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"[ACTIVITY-AI] AI server returned {response.StatusCode}");
                return GetDefaultActivityAnalysis(activityType);
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"[ACTIVITY-AI] Got response ({responseBody.Length} bytes)");

            var jsonContent = ExtractJsonFromResponse(responseBody);
            var aiResult = JsonSerializer.Deserialize<JsonElement>(jsonContent);

            return new
            {
                overallWellnessAssessment = GetJsonString(aiResult, "overallWellnessAssessment") ?? "Review activity data for wellness assessment.",
                activityEngagement = GetJsonString(aiResult, "activityEngagement") ?? "Good engagement with activities.",
                moodAnalysis = GetJsonString(aiResult, "moodAnalysis") ?? "Mood tracking shows consistent participation.",
                strengthsIdentified = GetJsonStringArray(aiResult, "strengthsIdentified"),
                areasForGrowth = GetJsonStringArray(aiResult, "areasForGrowth"),
                wellnessRecommendations = GetJsonStringArray(aiResult, "wellnessRecommendations"),
                encouragingMessage = GetJsonString(aiResult, "encouragingMessage") ?? "You're doing great! Keep up the wonderful work on your wellness journey! 🌟",
                therapistNotes = GetJsonString(aiResult, "therapistNotes") ?? "Patient showing consistent engagement with wellness activities.",
                engagementLevel = GetJsonString(aiResult, "engagementLevel") ?? "Medium",
                wellnessScore = GetJsonInt(aiResult, "wellnessScore", 70),
                generatedAt = DateTime.UtcNow,
                source = "Phi-4-mini AI"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ACTIVITY-AI] Error: {ex.Message}");
            return GetDefaultActivityAnalysis(activityType);
        }
    }

    private object GetDefaultActivityAnalysis(string? activityType)
    {
        return new
        {
            overallWellnessAssessment = "AI analysis unavailable. Please review activity statistics for wellness insights.",
            activityEngagement = "Check activity completion rates in the statistics panel.",
            moodAnalysis = "Review mood tracking data for emotional patterns.",
            strengthsIdentified = new List<string> { "Consistent participation", "Completing activities regularly" },
            areasForGrowth = new List<string> { "Try variety of activities", "Track mood before and after sessions" },
            wellnessRecommendations = new List<string> 
            { 
                "Continue daily journaling for emotional processing",
                "Practice breathing exercises for stress management",
                "Engage with word activities for cognitive stimulation"
            },
            encouragingMessage = "Every activity you complete is a step toward better wellness! Keep going! 🌟",
            therapistNotes = "Review statistical data for detailed activity analysis.",
            engagementLevel = "Medium",
            wellnessScore = 70,
            generatedAt = DateTime.UtcNow,
            source = "Default (AI unavailable)"
        };
    }

    private string ExtractJsonFromResponse(string response)
    {
        try
        {
            var chatResponse = JsonSerializer.Deserialize<JsonElement>(response);
            if (chatResponse.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var content = choices[0].GetProperty("message").GetProperty("content").GetString();
                if (!string.IsNullOrEmpty(content))
                {
                    var startIdx = content.IndexOf('{');
                    var endIdx = content.LastIndexOf('}');
                    if (startIdx >= 0 && endIdx > startIdx)
                    {
                        return content.Substring(startIdx, endIdx - startIdx + 1);
                    }
                }
            }
        }
        catch { }

        var start = response.IndexOf('{');
        var end = response.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            return response.Substring(start, end - start + 1);
        }

        return "{}";
    }

    private string GetJsonString(JsonElement element, string property)
    {
        try
        {
            if (element.TryGetProperty(property, out var prop))
            {
                return prop.GetString() ?? "";
            }
        }
        catch { }
        return "";
    }

    private int GetJsonInt(JsonElement element, string property, int defaultValue)
    {
        try
        {
            if (element.TryGetProperty(property, out var prop))
            {
                return prop.ValueKind == JsonValueKind.Number ? prop.GetInt32() : defaultValue;
            }
        }
        catch { }
        return defaultValue;
    }

    private List<string> GetJsonStringArray(JsonElement element, string property)
    {
        var result = new List<string>();
        try
        {
            if (element.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in prop.EnumerateArray())
                {
                    var str = item.GetString();
                    if (!string.IsNullOrEmpty(str))
                        result.Add(str);
                }
            }
        }
        catch { }
        return result;
    }

    #endregion

    #region AI Sound Curation

    /// <summary>
    /// AI-powered sound therapy curation using Phi-4 with LM Studio structured output
    /// </summary>
    [HttpPost("ai-curate-sounds")]
    public async Task<IActionResult> AICurateSounds([FromBody] AICurationRequest request)
    {
        try
        {
            _logger.LogInformation($"[AI-SOUND-CURATOR] User mood: {request.UserMood}");
            
            var availableSoundsText = string.Join("\n", request.AvailableSounds.Select(s => 
                $"- {s.Name} ({s.Category}): {s.Description}"));

            // Get previously recommended sounds to add variety
            var previousSoundsNote = "";
            if (request.PreviousSounds != null && request.PreviousSounds.Count > 0)
            {
                previousSoundsNote = $"\n\nIMPORTANT: The user recently used these sounds: {string.Join(", ", request.PreviousSounds)}. " +
                    "Sometimes recommend similar combinations if they fit the mood well, but also explore different sounds occasionally for variety. " +
                    "Use your judgment - if the mood matches previous sounds, feel free to recommend them again.";
            }

            var systemPrompt = @"You are a professional sound therapy curator AI. Your role is to create UNIQUE and personalized therapeutic sound mixes based on how users are feeling.

You have deep knowledge of:
- Sound therapy and its psychological benefits
- How different sounds affect mood and mental state
- Optimal sound combinations for relaxation, focus, sleep, stress relief
- Volume balancing for harmonious mixes

CRITICAL VARIETY RULES:
- DO NOT always recommend Rain or Fireplace - explore the FULL range of available sounds
- Each recommendation should feel fresh and tailored to the specific mood
- Consider less common but effective sounds like: Singing Bowls, Tibetan Bells, Wind Chimes, Forest Birds, Crickets, Bamboo Flute, Ambient Pads
- Mix categories creatively - combine nature with instruments, or white noise with ambient sounds
- Vary your volume recommendations (don't always use 70/60 pattern)

Guidelines:
- Choose 2-4 sounds that complement each other
- Set volumes creatively: try 55, 65, 75, 45, 85 - not just 60/70
- Be empathetic and understanding in your explanation
- Keep the mix name creative and unique each time
- Duration should be 5, 10, 15, 30, or 60 minutes based on their needs
- Surprise the user with thoughtful, unexpected combinations";

            var userPrompt = $@"User's current mood/situation: ""{request.UserMood}""

Available sounds to choose from:
{availableSoundsText}{previousSoundsNote}

Create a personalized sound therapy mix for this user.";

            // LM Studio structured output schema
            var responseSchema = new
            {
                type = "object",
                properties = new
                {
                    mixName = new
                    {
                        type = "string",
                        description = "A creative, descriptive name for this sound mix"
                    },
                    recommendedSounds = new
                    {
                        type = "array",
                        items = new { type = "string" },
                        minItems = 2,
                        maxItems = 4,
                        description = "List of 2-4 sound names from the available sounds"
                    },
                    volumeSettings = new
                    {
                        type = "object",
                        description = "Volume for each sound (0-100). Main sounds 60-80, accents 40-60",
                        additionalProperties = new
                        {
                            type = "integer",
                            minimum = 20,
                            maximum = 100
                        }
                    },
                    explanation = new
                    {
                        type = "string",
                        description = "A warm, personalized explanation (2-3 sentences) of why these sounds were chosen for the user's mood"
                    },
                    wellnessTip = new
                    {
                        type = "string",
                        description = "A helpful wellness tip related to their mood and the recommended sounds"
                    },
                    recommendedDuration = new
                    {
                        type = "integer",
                        @enum = new[] { 5, 10, 15, 30, 60 },
                        description = "Recommended session duration in minutes"
                    },
                    mixCategory = new
                    {
                        type = "string",
                        @enum = new[] { "relaxation", "focus", "sleep", "stress-relief", "meditation", "energy", "comfort" },
                        description = "The primary purpose of this mix"
                    },
                    isNewCombination = new
                    {
                        type = "boolean",
                        description = "True if this is a new/different combination from user's previous sounds, false if recommending similar"
                    }
                },
                required = new[] { "mixName", "recommendedSounds", "volumeSettings", "explanation", "wellnessTip", "recommendedDuration", "mixCategory", "isNewCombination" }
            };

            var aiRequest = new
            {
                model = "microsoft/phi-4-mini-reasoning",
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.9,  // Higher for more creative/varied recommendations
                max_tokens = 600,
                response_format = new
                {
                    type = "json_schema",
                    json_schema = new
                    {
                        name = "sound_therapy_curation",
                        strict = true,
                        schema = responseSchema
                    }
                }
            };

            _logger.LogInformation("[AI-SOUND-CURATOR] Sending request to Phi-4 with structured output...");
            
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var response = await httpClient.PostAsJsonAsync(AI_ENDPOINT, aiRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"[AI-SOUND-CURATOR] Phi-4 returned error: {response.StatusCode}");
                return Ok(new
                {
                    success = false,
                    error = "AI service temporarily unavailable. Please try again."
                });
            }

            var jsonContent = ExtractJsonFromResponse(responseContent);
            _logger.LogInformation($"[AI-SOUND-CURATOR] Extracted JSON: {jsonContent}");

            var parsed = JsonSerializer.Deserialize<JsonElement>(jsonContent);

            // Extract values
            var mixName = GetJsonString(parsed, "mixName");
            var recommendedSounds = GetJsonStringArray(parsed, "recommendedSounds");
            var explanation = GetJsonString(parsed, "explanation");
            var wellnessTip = GetJsonString(parsed, "wellnessTip");
            var recommendedDuration = GetJsonInt(parsed, "recommendedDuration", 15);
            var mixCategory = GetJsonString(parsed, "mixCategory");
            var isNewCombination = false;
            if (parsed.TryGetProperty("isNewCombination", out var newCombProp))
            {
                isNewCombination = newCombProp.ValueKind == JsonValueKind.True;
            }

            // Extract volume settings
            var volumeSettings = new Dictionary<string, int>();
            if (parsed.TryGetProperty("volumeSettings", out var volumeObj) && volumeObj.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in volumeObj.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Number)
                    {
                        volumeSettings[prop.Name] = prop.Value.GetInt32();
                    }
                }
            }

            // Validate that recommended sounds exist in available sounds
            var validSoundNames = request.AvailableSounds.Select(s => s.Name).ToHashSet();
            recommendedSounds = recommendedSounds.Where(s => validSoundNames.Contains(s)).ToList();

            if (recommendedSounds.Count == 0)
            {
                // Fallback: recommend varied sounds based on mood keywords with randomization
                var moodLower = request.UserMood.ToLower();
                var random = new Random();
                
                // Multiple options per mood for variety
                var sleepOptions = new[]
                {
                    (new List<string> { "Rain", "Pink Noise", "Piano" }, new Dictionary<string, int> { { "Rain", 65 }, { "Pink Noise", 45 }, { "Piano", 35 } }, "Peaceful Slumber"),
                    (new List<string> { "Ocean Waves", "Ambient Pads", "Wind Chimes" }, new Dictionary<string, int> { { "Ocean Waves", 70 }, { "Ambient Pads", 55 }, { "Wind Chimes", 30 } }, "Dreamy Tides"),
                    (new List<string> { "Crickets", "Stream", "Singing Bowls" }, new Dictionary<string, int> { { "Crickets", 50 }, { "Stream", 60 }, { "Singing Bowls", 40 } }, "Night Garden"),
                    (new List<string> { "Brown Noise", "Thunderstorm" }, new Dictionary<string, int> { { "Brown Noise", 55 }, { "Thunderstorm", 65 } }, "Deep Rest")
                };
                
                var focusOptions = new[]
                {
                    (new List<string> { "Brown Noise", "Coffee Shop" }, new Dictionary<string, int> { { "Brown Noise", 60 }, { "Coffee Shop", 50 } }, "Deep Focus Zone"),
                    (new List<string> { "White Noise", "Rain" }, new Dictionary<string, int> { { "White Noise", 55 }, { "Rain", 45 } }, "Concentration Station"),
                    (new List<string> { "Stream", "Forest Birds" }, new Dictionary<string, int> { { "Stream", 65 }, { "Forest Birds", 40 } }, "Nature's Office"),
                    (new List<string> { "Pink Noise", "Bamboo Flute" }, new Dictionary<string, int> { { "Pink Noise", 50 }, { "Bamboo Flute", 55 } }, "Zen Productivity")
                };
                
                var stressOptions = new[]
                {
                    (new List<string> { "Ocean Waves", "Singing Bowls" }, new Dictionary<string, int> { { "Ocean Waves", 70 }, { "Singing Bowls", 50 } }, "Calm Waters"),
                    (new List<string> { "Tibetan Bells", "Stream", "Wind" }, new Dictionary<string, int> { { "Tibetan Bells", 45 }, { "Stream", 60 }, { "Wind", 40 } }, "Mountain Serenity"),
                    (new List<string> { "Ambient Pads", "Rain", "Wind Chimes" }, new Dictionary<string, int> { { "Ambient Pads", 65 }, { "Rain", 50 }, { "Wind Chimes", 35 } }, "Gentle Embrace"),
                    (new List<string> { "Forest", "Piano" }, new Dictionary<string, int> { { "Forest", 60 }, { "Piano", 55 } }, "Peaceful Grove")
                };
                
                var defaultOptions = new[]
                {
                    (new List<string> { "Rain", "Fireplace" }, new Dictionary<string, int> { { "Rain", 65 }, { "Fireplace", 55 } }, "Cozy Retreat"),
                    (new List<string> { "Ocean Waves", "Wind Chimes" }, new Dictionary<string, int> { { "Ocean Waves", 70 }, { "Wind Chimes", 40 } }, "Seaside Calm"),
                    (new List<string> { "Forest", "Stream", "Forest Birds" }, new Dictionary<string, int> { { "Forest", 55 }, { "Stream", 60 }, { "Forest Birds", 45 } }, "Woodland Walk"),
                    (new List<string> { "Ambient Pads", "Singing Bowls" }, new Dictionary<string, int> { { "Ambient Pads", 65 }, { "Singing Bowls", 50 } }, "Ethereal Space"),
                    (new List<string> { "Thunderstorm", "Fireplace" }, new Dictionary<string, int> { { "Thunderstorm", 60 }, { "Fireplace", 55 } }, "Stormy Comfort"),
                    (new List<string> { "Crickets", "Campfire", "Wind" }, new Dictionary<string, int> { { "Crickets", 50 }, { "Campfire", 60 }, { "Wind", 35 } }, "Summer Night")
                };
                
                (List<string> sounds, Dictionary<string, int> volumes, string name) selected;
                
                if (moodLower.Contains("sleep") || moodLower.Contains("tired") || moodLower.Contains("insomnia"))
                {
                    selected = sleepOptions[random.Next(sleepOptions.Length)];
                }
                else if (moodLower.Contains("focus") || moodLower.Contains("work") || moodLower.Contains("study") || moodLower.Contains("concentrate"))
                {
                    selected = focusOptions[random.Next(focusOptions.Length)];
                }
                else if (moodLower.Contains("stress") || moodLower.Contains("anxious") || moodLower.Contains("anxiety") || moodLower.Contains("worried"))
                {
                    selected = stressOptions[random.Next(stressOptions.Length)];
                }
                else
                {
                    selected = defaultOptions[random.Next(defaultOptions.Length)];
                }
                
                recommendedSounds = selected.sounds;
                volumeSettings = selected.volumes;
                mixName = selected.name;
                explanation = "A carefully curated combination to help you feel better.";
            }

            _logger.LogInformation($"[AI-SOUND-CURATOR] Generated mix: {mixName} with {recommendedSounds.Count} sounds (new combination: {isNewCombination})");

            return Ok(new
            {
                success = true,
                mixName,
                recommendedSounds,
                volumeSettings,
                explanation,
                wellnessTip,
                recommendedDuration,
                mixCategory,
                isNewCombination,
                source = "Phi-4 AI"
            });
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("[AI-SOUND-CURATOR] Request timed out");
            return Ok(new
            {
                success = false,
                error = "AI request timed out. Please try again."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"[AI-SOUND-CURATOR] Error: {ex.Message}");
            return Ok(new
            {
                success = false,
                error = "Failed to generate AI recommendation. Please try again."
            });
        }
    }

    public class AICurationRequest
    {
        public string UserMood { get; set; } = "";
        public List<SoundInfo> AvailableSounds { get; set; } = new();
        public List<string>? PreviousSounds { get; set; }
        public string? Username { get; set; }
    }

    public class SoundInfo
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
    }

    #endregion
}
