using Microsoft.AspNetCore.Mvc;
using AdaptiveCognitiveRehabilitationPlatform.Services;
using System.Text.Json;
using System.Net.Http.Json;

namespace AdaptiveCognitiveRehabilitationPlatform.Controllers;

/// <summary>
/// API Controller for JSON-based Game Statistics
/// All data is stored in JSON files for easy portability and presentation
/// </summary>
[ApiController]
[Route("api/json-stats")]
public class JsonGameStatsController : ControllerBase
{
    private readonly IJsonGameStatsService _statsService;
    private readonly ILogger<JsonGameStatsController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private const string AI_ENDPOINT = "http://localhost:1234/v1/chat/completions";

    public JsonGameStatsController(
        IJsonGameStatsService statsService, 
        ILogger<JsonGameStatsController> logger,
        IHttpClientFactory httpClientFactory)
    {
        _statsService = statsService;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Save a new game session
    /// </summary>
    [HttpPost("save")]
    public async Task<IActionResult> SaveGameSession([FromBody] JsonSaveGameSessionRequest request)
    {
        try
        {
            _logger.LogInformation($"[JSON-STATS] Saving game session: {request.GameType} for user {request.UserId}");

            var entry = new GameStatsEntry
            {
                UserId = request.UserId,
                Username = request.Username ?? $"User{request.UserId}",
                GameType = request.GameType,
                GameMode = request.GameMode ?? "Practice",
                Difficulty = request.Difficulty,
                DifficultyName = request.DifficultyName ?? GetDifficultyName(request.Difficulty),
                Score = request.Score,
                Accuracy = request.Accuracy,
                TotalMoves = request.TotalMoves,
                CorrectMoves = request.CorrectMoves,
                ErrorCount = request.ErrorCount,
                TimeTakenSeconds = request.TimeTakenSeconds,
                MatchedPairs = request.MatchedPairs,
                TotalPairs = request.TotalPairs,
                AverageReactionTimeMs = request.AverageReactionTimeMs,
                ItemsSorted = request.ItemsSorted,
                TotalItems = request.TotalItems,
                AiEncouragement = request.AiEncouragement,
                AiFunMessage = request.AiFunMessage,
                AiEffortNote = request.AiEffortNote,
                RecommendedDifficulty = request.RecommendedDifficulty,
                AiConfidence = request.AiConfidence,
                StartTime = request.StartTime ?? DateTime.UtcNow.AddSeconds(-request.TimeTakenSeconds),
                EndTime = request.EndTime ?? DateTime.UtcNow,
                Notes = request.Notes,
                Status = "Completed"
            };

            await _statsService.SaveGameSessionAsync(entry);

            return Ok(new
            {
                success = true,
                message = "Game session saved successfully",
                sessionId = entry.Id,
                score = entry.Score,
                savedAt = entry.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"[JSON-STATS] Error saving session: {ex.Message}");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get all game sessions
    /// </summary>
    [HttpGet("sessions")]
    public async Task<IActionResult> GetAllSessions()
    {
        try
        {
            var sessions = await _statsService.GetAllSessionsAsync();
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[JSON-STATS] Error getting all sessions: {ex.Message}");
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
            var sessions = await _statsService.GetSessionsByUserIdAsync(userId);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[JSON-STATS] Error getting user sessions: {ex.Message}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get user summary/statistics
    /// </summary>
    [HttpGet("summary/{userId}")]
    public async Task<IActionResult> GetUserSummary(int userId)
    {
        try
        {
            var summary = await _statsService.GetUserSummaryAsync(userId);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[JSON-STATS] Error getting user summary: {ex.Message}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all user summaries (for therapist view)
    /// </summary>
    [HttpGet("summaries")]
    public async Task<IActionResult> GetAllUserSummaries()
    {
        try
        {
            var summaries = await _statsService.GetAllUserSummariesAsync();
            return Ok(summaries);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[JSON-STATS] Error getting all summaries: {ex.Message}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get therapist analytics data
    /// </summary>
    [HttpGet("therapist/analytics")]
    public async Task<IActionResult> GetTherapistAnalytics()
    {
        try
        {
            var analytics = await _statsService.GetTherapistAnalyticsAsync();
            var allSessions = await _statsService.GetAllSessionsAsync();
            
            // Transform to format expected by TherapistDashboard
            var result = new
            {
                totalUsers = analytics.TotalPatients,
                totalSessions = allSessions.Count,
                overallAccuracy = analytics.PatientSummaries.Any() 
                    ? analytics.PatientSummaries.Average(p => p.AverageAccuracy) : 0,
                averageScore = analytics.AveragePatientScore,
                
                gameTypeStats = analytics.GameDistribution.Select(g => {
                    var gameSessions = allSessions.Where(s => s.GameType == g.GameType).ToList();
                    return new
                    {
                        gameType = g.GameType,
                        gameName = GetGameDisplayName(g.GameType),
                        totalSessions = g.SessionCount,
                        averageScore = gameSessions.Any() ? (decimal)gameSessions.Average(s => s.Score) : 0,
                        averageAccuracy = gameSessions.Any() ? (decimal)gameSessions.Average(s => s.Accuracy) : 0,
                        bestScore = gameSessions.Any() ? gameSessions.Max(s => s.Score) : 0,
                        averageMoves = gameSessions.Any() ? (decimal)gameSessions.Average(s => s.TotalMoves) : 0,
                        averageReactionTime = gameSessions.Where(s => s.AverageReactionTimeMs.HasValue).Any() 
                            ? gameSessions.Where(s => s.AverageReactionTimeMs.HasValue).Average(s => s.AverageReactionTimeMs!.Value) : 0
                    };
                }).ToList(),
                
                userSummaries = analytics.PatientSummaries.Select(p => new
                {
                    userId = p.UserId,
                    username = p.Username ?? $"User_{p.UserId}",
                    totalSessions = p.TotalSessions,
                    averageScore = (double)p.AverageScore,
                    averageAccuracy = (double)p.AverageAccuracy,
                    bestScore = (int)p.BestScore,
                    totalTimePlayed = (double)allSessions.Where(s => s.UserId == p.UserId).Sum(s => s.TimeTakenSeconds),
                    lastPlayed = p.LastSessionDate,
                    progressTrend = p.ProgressTrend ?? "Stable"
                }).OrderByDescending(u => u.averageScore).ToList(),
                
                recentSessions = allSessions.OrderByDescending(s => s.CreatedAt).Take(10).Select(s => new
                {
                    userId = s.UserId,
                    username = s.Username ?? $"User_{s.UserId}",
                    gameType = s.GameType,
                    score = s.Score,
                    accuracy = (double)s.Accuracy,
                    difficulty = s.Difficulty,
                    timeTaken = (double)s.TimeTakenSeconds,
                    playedAt = s.CreatedAt
                }).ToList(),
                
                weeklyActivity = GetWeeklyActivityWithPercent(
                    Enumerable.Range(0, 7)
                        .Select(i => DateTime.UtcNow.Date.AddDays(-6 + i))
                        .Select(date => new DailyActivity
                        {
                            Day = date.ToString("ddd"),
                            Date = date,
                            GamesPlayed = allSessions.Count(s => s.CreatedAt.Date == date)
                        }).ToList()
                )
            };
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[JSON-STATS] Error getting therapist analytics: {ex.Message}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get detailed analysis for a specific user (for therapist per-user analysis)
    /// </summary>
    [HttpGet("therapist/user/{userId}/analysis")]
    public async Task<IActionResult> GetUserAnalysis(int userId)
    {
        try
        {
            var allSessions = await _statsService.GetAllSessionsAsync();
            var userSessions = allSessions.Where(s => s.UserId == userId).OrderBy(s => s.CreatedAt).ToList();
            
            if (!userSessions.Any())
            {
                return Ok(new { message = "No sessions found for this user", hasData = false });
            }
            
            var firstSession = userSessions.First();
            var username = firstSession.Username ?? $"User_{userId}";
            
            // Calculate per-game breakdown
            var gameBreakdown = userSessions
                .GroupBy(s => s.GameType)
                .Select(g => {
                    var sessions = g.OrderBy(s => s.CreatedAt).ToList();
                    var firstThree = sessions.Take(3).ToList();
                    var lastThree = sessions.TakeLast(3).ToList();
                    var improvement = lastThree.Any() && firstThree.Any() 
                        ? lastThree.Average(s => s.Score) - firstThree.Average(s => s.Score) 
                        : 0;
                    
                    return new
                    {
                        gameType = g.Key,
                        gameName = GetGameDisplayName(g.Key),
                        totalSessions = g.Count(),
                        averageScore = g.Average(s => s.Score),
                        averageAccuracy = g.Average(s => s.Accuracy),
                        bestScore = g.Max(s => s.Score),
                        worstScore = g.Min(s => s.Score),
                        totalTimePlayed = g.Sum(s => s.TimeTakenSeconds),
                        averageTimeTaken = g.Average(s => s.TimeTakenSeconds),
                        averageMoves = g.Average(s => s.TotalMoves),
                        averageReactionTime = g.Where(s => s.AverageReactionTimeMs.HasValue).Any() 
                            ? g.Where(s => s.AverageReactionTimeMs.HasValue).Average(s => s.AverageReactionTimeMs!.Value) : 0,
                        maxDifficulty = g.Max(s => s.Difficulty),
                        improvement = improvement,
                        trend = improvement > 5 ? "Improving" : improvement < -5 ? "Declining" : "Stable",
                        scoreHistory = sessions.Select(s => new { date = s.CreatedAt, score = s.Score, accuracy = s.Accuracy }).ToList()
                    };
                }).ToList();
            
            // Calculate overall trends
            var sortedSessions = userSessions.OrderBy(s => s.CreatedAt).ToList();
            var firstQuarter = sortedSessions.Take(sortedSessions.Count / 4 + 1).ToList();
            var lastQuarter = sortedSessions.TakeLast(sortedSessions.Count / 4 + 1).ToList();
            var overallImprovement = lastQuarter.Any() && firstQuarter.Any() 
                ? lastQuarter.Average(s => s.Score) - firstQuarter.Average(s => s.Score) 
                : 0;
            
            // Weekly performance over time
            var weeklyPerformance = userSessions
                .GroupBy(s => new { Year = s.CreatedAt.Year, Week = System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(s.CreatedAt, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday) })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Week)
                .Select(g => new
                {
                    weekLabel = $"W{g.Key.Week}",
                    averageScore = g.Average(s => s.Score),
                    averageAccuracy = g.Average(s => s.Accuracy),
                    sessionsPlayed = g.Count()
                }).ToList();
            
            // Daily activity pattern
            var dailyPattern = userSessions
                .GroupBy(s => s.CreatedAt.DayOfWeek)
                .Select(g => new
                {
                    day = g.Key.ToString(),
                    dayIndex = (int)g.Key,
                    sessionsPlayed = g.Count(),
                    averageScore = g.Average(s => s.Score)
                })
                .OrderBy(d => d.dayIndex)
                .ToList();
            
            // Difficulty progression
            var difficultyProgression = userSessions
                .GroupBy(s => s.Difficulty)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    difficulty = g.Key,
                    difficultyName = g.Key switch { 1 => "Easy", 2 => "Medium", 3 => "Hard", _ => $"Level {g.Key}" },
                    sessionsPlayed = g.Count(),
                    averageScore = (double)g.Average(s => s.Score),
                    averageAccuracy = (double)g.Average(s => s.Accuracy),
                    successRate = (double)g.Average(s => s.Accuracy) >= 50 ? 100.0 : (double)g.Average(s => s.Accuracy) * 2
                }).ToList();
            
            // Cognitive strengths analysis
            var strengths = new List<object>();
            var areasForImprovement = new List<object>();
            
            foreach (var game in gameBreakdown)
            {
                if (game.averageAccuracy >= 70)
                {
                    strengths.Add(new { area = game.gameName, score = game.averageAccuracy, description = $"Strong performance in {game.gameName}" });
                }
                else if (game.averageAccuracy < 50)
                {
                    areasForImprovement.Add(new { area = game.gameName, score = game.averageAccuracy, suggestion = $"Practice more {game.gameName} sessions" });
                }
            }
            
            var result = new
            {
                hasData = true,
                userId = userId,
                username = username,
                summary = new
                {
                    totalSessions = userSessions.Count,
                    totalGamesTypes = gameBreakdown.Count,
                    totalTimePlayed = userSessions.Sum(s => s.TimeTakenSeconds),
                    averageScore = userSessions.Average(s => s.Score),
                    averageAccuracy = userSessions.Average(s => s.Accuracy),
                    bestScore = userSessions.Max(s => s.Score),
                    firstSession = userSessions.First().CreatedAt,
                    lastSession = userSessions.Last().CreatedAt,
                    overallImprovement = overallImprovement,
                    overallTrend = overallImprovement > 5 ? "Improving" : overallImprovement < -5 ? "Declining" : "Stable"
                },
                gameBreakdown = gameBreakdown,
                weeklyPerformance = weeklyPerformance,
                dailyPattern = dailyPattern,
                difficultyProgression = difficultyProgression,
                strengths = strengths,
                areasForImprovement = areasForImprovement,
                recentSessions = userSessions.OrderByDescending(s => s.CreatedAt).Take(10).Select(s => new
                {
                    gameType = s.GameType,
                    gameName = GetGameDisplayName(s.GameType),
                    score = s.Score,
                    accuracy = s.Accuracy,
                    difficulty = s.Difficulty,
                    timeTaken = s.TimeTakenSeconds,
                    playedAt = s.CreatedAt
                }).ToList(),
                recommendations = GenerateTherapistRecommendations(gameBreakdown, overallImprovement, userSessions.Count),
                aiAnalysis = GenerateAITherapistAnalysis(username, gameBreakdown, overallImprovement, userSessions, strengths, areasForImprovement)
            };
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[JSON-STATS] Error getting user analysis: {ex.Message}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// UNIFIED AI Analysis Endpoint - Works for both Users and Therapists
    /// Uses a single JSON schema in LM Studio, returns different data based on requester type
    /// </summary>
    /// <param name="userId">The user/patient ID to analyze</param>
    /// <param name="requesterType">Either "user" (child-friendly) or "therapist" (clinical)</param>
    /// <param name="gameType">Optional: specific game type to analyze</param>
    [HttpGet("ai-analysis/{userId}")]
    public async Task<IActionResult> GetUnifiedAIAnalysis(int userId, [FromQuery] string requesterType = "user", [FromQuery] string? gameType = null)
    {
        try
        {
            var isTherapist = requesterType.ToLower() == "therapist";
            _logger.LogInformation($"[AI-UNIFIED] Starting AI analysis for user {userId}, requester: {requesterType}, game: {gameType ?? "all"}");
            
            var allSessions = await _statsService.GetAllSessionsAsync();
            var userSessions = allSessions.Where(s => s.UserId == userId).OrderBy(s => s.CreatedAt).ToList();
            
            if (!string.IsNullOrEmpty(gameType))
            {
                userSessions = userSessions.Where(s => s.GameType?.ToLower() == gameType.ToLower()).ToList();
            }
            
            if (!userSessions.Any())
            {
                return Ok(new {
                    success = false,
                    message = "No sessions found for this user",
                    targetAudience = requesterType,
                    analysis = GetDefaultUnifiedAnalysis(isTherapist, gameType ?? "MemoryMatch")
                });
            }
            
            var firstSession = userSessions.First();
            var username = firstSession.Username ?? $"Player_{userId}";
            var lastSession = userSessions.Last();
            
            // Build comprehensive data for AI
            var analysisData = BuildUnifiedAnalysisData(username, userSessions, isTherapist, gameType);
            
            // Call Phi-4-mini with unified schema
            var aiResponse = await CallPhiForUnifiedAnalysis(analysisData, isTherapist, gameType ?? lastSession.GameType ?? "MemoryMatch");
            
            return Ok(new {
                success = true,
                userId = userId,
                username = username,
                targetAudience = requesterType,
                gameType = gameType ?? lastSession.GameType,
                totalSessions = userSessions.Count,
                analysis = aiResponse
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"[AI-UNIFIED] Error: {ex.Message}");
            var isTherapist = requesterType.ToLower() == "therapist";
            return Ok(new { 
                success = false, 
                error = ex.Message,
                targetAudience = requesterType,
                analysis = GetDefaultUnifiedAnalysis(isTherapist, gameType ?? "MemoryMatch")
            });
        }
    }

    /// <summary>
    /// Legacy endpoint for user feedback - redirects to unified endpoint
    /// </summary>
    [HttpGet("user/{userId}/ai-feedback")]
    public async Task<IActionResult> GetUserAIFeedback(int userId, [FromQuery] string? gameType = null)
    {
        return await GetUnifiedAIAnalysis(userId, "user", gameType);
    }

    private string BuildUnifiedAnalysisData(string username, List<GameStatsEntry> sessions, bool isTherapist, string? gameType)
    {
        var lastSession = sessions.Last();
        var avgScore = sessions.Average(s => s.Score);
        var avgAccuracy = sessions.Average(s => s.Accuracy);
        var bestScore = sessions.Max(s => s.Score);
        var worstScore = sessions.Min(s => s.Score);
        var currentDifficulty = lastSession.Difficulty;
        var totalSessions = sessions.Count;
        
        // Calculate improvement
        var firstThree = sessions.Take(3).ToList();
        var lastThree = sessions.TakeLast(3).ToList();
        var improvement = lastThree.Any() && firstThree.Any() 
            ? lastThree.Average(s => s.Score) - firstThree.Average(s => s.Score) 
            : 0;
        
        var data = new System.Text.StringBuilder();
        
        // Include target audience in the prompt
        data.AppendLine($"TARGET AUDIENCE: {(isTherapist ? "THERAPIST (clinical, professional analysis)" : "USER/CHILD (encouraging, fun, simple language)")}");
        data.AppendLine();
        data.AppendLine($"=== PLAYER DATA: {username} ===");
        data.AppendLine($"Total Sessions: {totalSessions}");
        data.AppendLine($"Game Focus: {GetGameDisplayName(gameType ?? lastSession.GameType)}");
        data.AppendLine();
        
        data.AppendLine("--- PERFORMANCE METRICS ---");
        data.AppendLine($"Last Session Score: {lastSession.Score}");
        data.AppendLine($"Last Session Accuracy: {lastSession.Accuracy:F1}%");
        data.AppendLine($"Average Score: {avgScore:F0}");
        data.AppendLine($"Average Accuracy: {avgAccuracy:F1}%");
        data.AppendLine($"Best Score: {bestScore}");
        data.AppendLine($"Worst Score: {worstScore}");
        data.AppendLine($"Current Difficulty: {currentDifficulty}");
        data.AppendLine($"Score Trend: {(improvement >= 0 ? "+" : "")}{improvement:F0} points");
        data.AppendLine();
        
        // Game-specific metrics
        if (lastSession.GameType?.ToLower() == "memorymatch")
        {
            data.AppendLine("--- MEMORY MATCH SPECIFICS ---");
            data.AppendLine($"Average Moves: {sessions.Average(s => s.TotalMoves):F1}");
            data.AppendLine($"Average Time: {sessions.Average(s => s.TimeTakenSeconds):F1} seconds");
            data.AppendLine($"Last Session Moves: {lastSession.TotalMoves}");
            data.AppendLine($"Last Session Time: {lastSession.TimeTakenSeconds} seconds");
        }
        else if (lastSession.GameType?.ToLower() == "reactiontrainer")
        {
            data.AppendLine("--- REACTION TRAINER SPECIFICS ---");
            var reactionTimes = sessions.Where(s => s.AverageReactionTimeMs.HasValue).ToList();
            if (reactionTimes.Any())
            {
                data.AppendLine($"Average Reaction Time: {reactionTimes.Average(s => s.AverageReactionTimeMs!.Value):F0}ms");
                data.AppendLine($"Best Reaction Time: {reactionTimes.Min(s => s.AverageReactionTimeMs!.Value):F0}ms");
            }
            data.AppendLine($"Correct Responses: {lastSession.CorrectMoves}");
        }
        else if (lastSession.GameType?.ToLower() == "sortingtask")
        {
            data.AppendLine("--- SORTING TASK SPECIFICS ---");
            var sortingData = sessions.Where(s => s.ItemsSorted.HasValue).ToList();
            if (sortingData.Any())
            {
                data.AppendLine($"Average Items Sorted: {sortingData.Average(s => s.ItemsSorted!.Value):F1}");
            }
        }
        else if (lastSession.GameType?.ToLower() == "patterncopy")
        {
            data.AppendLine("--- PATTERN COPY SPECIFICS ---");
            var patternData = sessions.Where(s => s.PatternSize.HasValue).ToList();
            if (patternData.Any())
            {
                data.AppendLine($"Average Grid Size: {patternData.Average(s => s.GridSize ?? 3):F1}x{patternData.Average(s => s.GridSize ?? 3):F1}");
                data.AppendLine($"Average Pattern Size: {patternData.Average(s => s.PatternSize ?? 0):F1} cells");
                data.AppendLine($"Average Correct Patterns: {patternData.Average(s => s.CorrectPatterns ?? 0):F1}/{patternData.Average(s => s.TotalRounds ?? 0):F1}");
                data.AppendLine($"Average Time: {patternData.Average(s => s.TimeTakenSeconds):F1} seconds");
            }
        }
        
        // For therapist, add more clinical context
        if (isTherapist)
        {
            data.AppendLine();
            data.AppendLine("--- CLINICAL CONTEXT (FOR THERAPIST) ---");
            data.AppendLine($"Session Frequency: {totalSessions} sessions over {(sessions.Last().CreatedAt - sessions.First().CreatedAt).TotalDays:F0} days");
            data.AppendLine($"Difficulty Progression: Started at {sessions.First().Difficulty}, now at {currentDifficulty}");
            data.AppendLine($"Consistency: Score variance = {CalculateVariance(sessions.Select(s => (double)s.Score).ToList()):F1}");
            
            // Trend analysis
            var trend = improvement > 5 ? "IMPROVING" : improvement < -5 ? "DECLINING" : "STABLE";
            data.AppendLine($"Overall Trend: {trend}");
            
            // Group by game type for comprehensive view
            var gameGroups = sessions.GroupBy(s => s.GameType).ToList();
            if (gameGroups.Count > 1)
            {
                data.AppendLine();
                data.AppendLine("--- MULTI-GAME BREAKDOWN ---");
                foreach (var group in gameGroups)
                {
                    var gameSessions = group.ToList();
                    data.AppendLine($"{GetGameDisplayName(group.Key)}: {gameSessions.Count} sessions, Avg Score: {gameSessions.Average(s => s.Score):F0}, Avg Accuracy: {gameSessions.Average(s => s.Accuracy):F1}%");
                }
            }
        }
        
        return data.ToString();
    }

    private double CalculateVariance(List<double> values)
    {
        if (values.Count < 2) return 0;
        var avg = values.Average();
        return Math.Sqrt(values.Sum(v => Math.Pow(v - avg, 2)) / values.Count);
    }

    private async Task<object> CallPhiForUnifiedAnalysis(string analysisData, bool isTherapist, string gameType)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(180);  // Increased timeout for longer AI responses

            // Simplified prompt for faster responses
            var prompt = isTherapist 
                ? $@"Clinical analysis for therapist. Data: {analysisData}

Return JSON with: targetAudience=""therapist"", overallAssessment, memoryAnalysis, reactionAnalysis, sortingAnalysis, patternAnalysis, cognitiveStrengths (array), areasOfConcern (array), progressSummary, therapyRecommendations (array), nextSessionFocus, riskLevel (Low/Moderate/High), performanceScore (0-100), nextDifficultyLevel (1-10), confidence (0.5-1.0), gameAdjustments object, encouragingNote (positive message for patient)."
                : $@"Fun feedback for child. Data: {analysisData}

Return JSON with: targetAudience=""user"", encouragement (positive message), effortNote, funMessage (with emoji), performanceScore (0-100), nextDifficultyLevel (1-10), confidence (0.5-1.0), gameAdjustments object for {gameType}.";

            var request = new
            {
                model = "Phi-4-mini",
                messages = new[]
                {
                    new { role = "system", content = "Output only valid JSON. Be concise." },
                    new { role = "user", content = prompt }
                },
                temperature = isTherapist ? 0.3 : 0.7,
                max_tokens = 3000,  // Increased for complete responses
                response_format = new { type = "json_object" }
            };

            _logger.LogInformation($"[AI-UNIFIED] Sending to Phi-4-mini (audience: {(isTherapist ? "therapist" : "user")})...");
            var response = await httpClient.PostAsJsonAsync(AI_ENDPOINT, request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"[AI-UNIFIED] AI server returned {response.StatusCode}");
                return GetDefaultUnifiedAnalysis(isTherapist, gameType);
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"[AI-UNIFIED] Got response ({responseBody.Length} bytes)");

            // Extract and parse JSON
            var jsonContent = ExtractJsonFromAIResponse(responseBody);
            var aiResult = JsonSerializer.Deserialize<JsonElement>(jsonContent);
            
            // Return unified response with all fields
            return new {
                targetAudience = GetJsonString(aiResult, "targetAudience") ?? (isTherapist ? "therapist" : "user"),
                
                // User/Child fields
                encouragement = GetJsonString(aiResult, "encouragement") ?? "Great job playing today!",
                effortNote = GetJsonString(aiResult, "effortNote") ?? "You're putting in great effort!",
                funMessage = GetJsonString(aiResult, "funMessage") ?? "Keep up the awesome work!",
                performanceScore = GetJsonInt(aiResult, "performanceScore", 50),
                nextDifficultyLevel = GetJsonInt(aiResult, "nextDifficultyLevel", 1),
                confidence = GetJsonDouble(aiResult, "confidence", 0.7),
                gameAdjustments = ExtractGameAdjustments(aiResult, gameType),
                
                // Therapist fields
                overallAssessment = GetJsonString(aiResult, "overallAssessment") ?? "Review session data for assessment.",
                memoryAnalysis = GetJsonString(aiResult, "memoryAnalysis") ?? "No Memory Match data available.",
                reactionAnalysis = GetJsonString(aiResult, "reactionAnalysis") ?? "No Reaction data available.",
                sortingAnalysis = GetJsonString(aiResult, "sortingAnalysis") ?? "No Sorting data available.",
                patternAnalysis = GetJsonString(aiResult, "patternAnalysis") ?? "No Pattern Copy data available.",
                cognitiveStrengths = GetJsonStringArray(aiResult, "cognitiveStrengths"),
                areasOfConcern = GetJsonStringArray(aiResult, "areasOfConcern"),
                progressSummary = GetJsonString(aiResult, "progressSummary") ?? "Insufficient data for trend analysis.",
                therapyRecommendations = GetJsonStringArray(aiResult, "therapyRecommendations"),
                nextSessionFocus = GetJsonString(aiResult, "nextSessionFocus") ?? "Continue current exercises.",
                riskLevel = GetJsonString(aiResult, "riskLevel") ?? "Low",
                encouragingNote = GetJsonString(aiResult, "encouragingNote") ?? "Keep up the great work! Every session brings progress.",
                
                // Metadata
                generatedAt = DateTime.UtcNow,
                source = "Phi-4-mini AI"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"[AI-UNIFIED] Error: {ex.Message}");
            return GetDefaultUnifiedAnalysis(isTherapist, gameType);
        }
    }

    private object GetDefaultUnifiedAnalysis(bool isTherapist, string gameType)
    {
        return new
        {
            targetAudience = isTherapist ? "therapist" : "user",
            
            // User fields
            encouragement = "Great job playing today! Every game helps your brain grow stronger! 🌟",
            effortNote = "You're putting in great effort! Keep practicing!",
            funMessage = "Ready for your next adventure? Your brain is a superhero! 🦸‍♂️",
            performanceScore = 50,
            nextDifficultyLevel = 1,
            confidence = 0.7,
            gameAdjustments = GetDefaultGameAdjustments(gameType),
            
            // Therapist fields
            overallAssessment = "AI analysis unavailable. Please review statistical data manually.",
            memoryAnalysis = "Check Memory Match statistics in the data panel.",
            reactionAnalysis = "Check Reaction Trainer statistics in the data panel.",
            sortingAnalysis = "Check Sorting Task statistics in the data panel.",
            patternAnalysis = "Check Pattern Copy statistics in the data panel.",
            cognitiveStrengths = new List<string> { "Data available in statistics panel" },
            areasOfConcern = new List<string> { "Review accuracy metrics manually" },
            progressSummary = "Review improvement percentages in each game category.",
            therapyRecommendations = new List<string> { "Continue regular cognitive exercises", "Monitor progress trends" },
            nextSessionFocus = "Review recent performance and adjust accordingly.",
            riskLevel = "Unknown",
            encouragingNote = "Every session is progress! Keep encouraging consistent practice.",
            
            generatedAt = DateTime.UtcNow,
            source = "Default (AI unavailable)"
        };
    }

    private string BuildUserFeedbackData(string username, List<GameStatsEntry> sessions, string? gameType)
    {
        var lastSession = sessions.Last();
        var avgScore = sessions.Average(s => s.Score);
        var avgAccuracy = sessions.Average(s => s.Accuracy);
        var bestScore = sessions.Max(s => s.Score);
        var currentDifficulty = lastSession.Difficulty;
        var totalSessions = sessions.Count;
        
        // Calculate improvement
        var firstThree = sessions.Take(3).ToList();
        var lastThree = sessions.TakeLast(3).ToList();
        var improvement = lastThree.Average(s => s.Score) - firstThree.Average(s => s.Score);
        
        var data = new System.Text.StringBuilder();
        data.AppendLine($"Player: {username}");
        data.AppendLine($"Game: {GetGameDisplayName(gameType ?? lastSession.GameType)}");
        data.AppendLine($"Total Sessions: {totalSessions}");
        data.AppendLine($"Last Session Score: {lastSession.Score}");
        data.AppendLine($"Last Session Accuracy: {lastSession.Accuracy:F1}%");
        data.AppendLine($"Average Score: {avgScore:F0}");
        data.AppendLine($"Average Accuracy: {avgAccuracy:F1}%");
        data.AppendLine($"Best Score Ever: {bestScore}");
        data.AppendLine($"Current Difficulty: {currentDifficulty}");
        data.AppendLine($"Score Improvement: {(improvement >= 0 ? "+" : "")}{improvement:F0}");
        
        if (lastSession.GameType?.ToLower() == "memorymatch")
        {
            data.AppendLine($"Moves Used: {lastSession.TotalMoves}");
            data.AppendLine($"Time Taken: {lastSession.TimeTakenSeconds} seconds");
        }
        else if (lastSession.GameType?.ToLower() == "reactiontrainer")
        {
            data.AppendLine($"Reaction Time: {lastSession.AverageReactionTimeMs ?? 0}ms");
            data.AppendLine($"Correct Responses: {lastSession.CorrectMoves}");
        }
        else if (lastSession.GameType?.ToLower() == "patterncopy")
        {
            data.AppendLine($"Grid Size: {lastSession.GridSize ?? 3}x{lastSession.GridSize ?? 3}");
            data.AppendLine($"Pattern Size: {lastSession.PatternSize ?? 0} cells");
            data.AppendLine($"Correct Patterns: {lastSession.CorrectPatterns ?? 0}/{lastSession.TotalRounds ?? 0}");
            data.AppendLine($"Time Taken: {lastSession.TimeTakenSeconds} seconds");
        }
        
        return data.ToString();
    }

    private async Task<object> CallPhiForUserFeedback(string userData, string gameType)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(180);

            // LM Studio has the JSON schema configured in its UI (Structured Output section)
            // We just need to request JSON output format
            var prompt = $@"You are a friendly, encouraging game coach for children doing cognitive rehabilitation exercises. 
Based on this player's performance data, provide encouraging feedback and game adjustments.

{userData}

Provide your response as a JSON object with these fields:
- encouragement: A child-friendly encouragement message
- effortNote: A note about the child's effort  
- funMessage: A fun, engaging message for the child
- performanceScore: Performance score from 0-100
- nextDifficultyLevel: Recommended next difficulty level (integer, minimum 1)
- confidence: Your confidence in this recommendation (0.5 to 1.0)
- gameAdjustments: An object with gridColumns, gridRows, totalPairs, flipAnimationMs, visualComplexity, timePressure, cardDesign

Be positive, fun, and child-friendly! Adjust the game settings based on their performance.";

            var request = new
            {
                model = "microsoft/phi-4-mini-reasoning",
                messages = new[]
                {
                    new { role = "system", content = "You are a friendly game coach for children doing cognitive rehabilitation. Always be encouraging and positive. Output only valid JSON matching the requested structure." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.8,
                max_tokens = 1000,
                response_format = new { type = "json_object" }
            };

            _logger.LogInformation("[AI-USER-FEEDBACK] Sending to Phi-4-mini (LM Studio structured output enabled)...");
            var response = await httpClient.PostAsJsonAsync(AI_ENDPOINT, request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"[AI-USER-FEEDBACK] AI server returned {response.StatusCode}");
                return GetDefaultUserFeedback(gameType);
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"[AI-USER-FEEDBACK] Got response ({responseBody.Length} bytes)");

            // Extract JSON from response
            var jsonContent = ExtractJsonFromAIResponse(responseBody);
            var aiResult = JsonSerializer.Deserialize<JsonElement>(jsonContent);
            
            return new {
                encouragement = GetJsonString(aiResult, "encouragement"),
                effortNote = GetJsonString(aiResult, "effortNote"),
                funMessage = GetJsonString(aiResult, "funMessage"),
                performanceScore = GetJsonInt(aiResult, "performanceScore", 50),
                nextDifficultyLevel = GetJsonInt(aiResult, "nextDifficultyLevel", 1),
                confidence = GetJsonDouble(aiResult, "confidence", 0.7),
                gameAdjustments = ExtractGameAdjustments(aiResult, gameType),
                generatedAt = DateTime.UtcNow,
                source = "Phi-4-mini AI"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"[AI-USER-FEEDBACK] Error: {ex.Message}");
            return GetDefaultUserFeedback(gameType);
        }
    }

    private object GetUserFeedbackJsonSchema(string gameType)
    {
        // Base schema for all games
        var baseSchema = new
        {
            type = "object",
            properties = new Dictionary<string, object>
            {
                ["encouragement"] = new { type = "string", description = "Child-friendly encouragement message" },
                ["effortNote"] = new { type = "string", description = "Note about the child's effort" },
                ["funMessage"] = new { type = "string", description = "Fun, engaging message for the child" },
                ["performanceScore"] = new { type = "number", minimum = 0, maximum = 100, description = "Performance score 0-100" },
                ["nextDifficultyLevel"] = new { type = "integer", minimum = 1, maximum = 10, description = "Recommended next difficulty level" },
                ["confidence"] = new { type = "number", minimum = 0.5, maximum = 1, description = "Confidence in recommendation (0.5-1.0)" },
                ["gameAdjustments"] = GetGameAdjustmentsSchema(gameType)
            },
            required = new[] { "encouragement", "effortNote", "funMessage", "performanceScore", "nextDifficultyLevel", "confidence", "gameAdjustments" }
        };

        return baseSchema;
    }

    private object GetGameAdjustmentsSchema(string gameType)
    {
        return gameType?.ToLower() switch
        {
            "memorymatch" => new
            {
                type = "object",
                description = "Memory Match game adjustments",
                properties = new Dictionary<string, object>
                {
                    ["gridColumns"] = new { type = "integer", minimum = 2, maximum = 6, description = "Number of columns in the game grid" },
                    ["gridRows"] = new { type = "integer", minimum = 2, maximum = 6, description = "Number of rows in the game grid" },
                    ["totalPairs"] = new { type = "integer", minimum = 4, maximum = 18, description = "Total number of card pairs" },
                    ["flipAnimationMs"] = new { type = "integer", minimum = 200, maximum = 2000, description = "Card flip animation duration in milliseconds" },
                    ["visualComplexity"] = new { type = "string", @enum = new[] { "simple", "moderate", "complex" }, description = "Visual complexity of card designs" },
                    ["timePressure"] = new { type = "string", @enum = new[] { "relaxed", "moderate", "challenging" }, description = "Time pressure level" },
                    ["cardDesign"] = new { type = "string", @enum = new[] { "basic", "colorful", "themed" }, description = "Card design style" }
                },
                required = new[] { "gridColumns", "gridRows", "totalPairs", "flipAnimationMs", "visualComplexity", "timePressure", "cardDesign" }
            },
            "reactiontrainer" => new
            {
                type = "object",
                description = "Reaction Trainer game adjustments",
                properties = new Dictionary<string, object>
                {
                    ["targetCount"] = new { type = "integer", minimum = 5, maximum = 20, description = "Number of targets per round" },
                    ["targetDisplayMs"] = new { type = "integer", minimum = 500, maximum = 3000, description = "How long targets are shown in milliseconds" },
                    ["targetSize"] = new { type = "string", @enum = new[] { "small", "medium", "large" }, description = "Target size" },
                    ["distractors"] = new { type = "boolean", description = "Whether to include distractor targets" },
                    ["speedProgression"] = new { type = "string", @enum = new[] { "steady", "increasing", "random" }, description = "How speed changes during game" },
                    ["feedbackIntensity"] = new { type = "string", @enum = new[] { "subtle", "normal", "vibrant" }, description = "Visual feedback intensity" }
                },
                required = new[] { "targetCount", "targetDisplayMs", "targetSize", "distractors", "speedProgression", "feedbackIntensity" }
            },
            "sortingtask" => new
            {
                type = "object",
                description = "Sorting Task game adjustments",
                properties = new Dictionary<string, object>
                {
                    ["itemCount"] = new { type = "integer", minimum = 4, maximum = 15, description = "Number of items to sort" },
                    ["categoryCount"] = new { type = "integer", minimum = 2, maximum = 5, description = "Number of categories" },
                    ["timeLimit"] = new { type = "integer", minimum = 30, maximum = 300, description = "Time limit in seconds" },
                    ["hintLevel"] = new { type = "string", @enum = new[] { "none", "subtle", "helpful" }, description = "Hint availability" },
                    ["dragSensitivity"] = new { type = "string", @enum = new[] { "precise", "normal", "forgiving" }, description = "Drag and drop sensitivity" },
                    ["categoryTheme"] = new { type = "string", @enum = new[] { "colors", "shapes", "animals", "mixed" }, description = "Category theme" }
                },
                required = new[] { "itemCount", "categoryCount", "timeLimit", "hintLevel", "dragSensitivity", "categoryTheme" }
            },
            _ => new
            {
                type = "object",
                description = "Generic game adjustments",
                properties = new Dictionary<string, object>
                {
                    ["difficultyMultiplier"] = new { type = "number", minimum = 0.5, maximum = 2.0, description = "Difficulty multiplier" },
                    ["timeMultiplier"] = new { type = "number", minimum = 0.5, maximum = 2.0, description = "Time multiplier" },
                    ["hintEnabled"] = new { type = "boolean", description = "Enable hints" }
                },
                required = new[] { "difficultyMultiplier", "timeMultiplier", "hintEnabled" }
            }
        };
    }

    private object ExtractGameAdjustments(JsonElement aiResult, string gameType)
    {
        try
        {
            if (aiResult.TryGetProperty("gameAdjustments", out var adjustments))
            {
                return gameType?.ToLower() switch
                {
                    "memorymatch" => new
                    {
                        gridColumns = GetJsonInt(adjustments, "gridColumns", 4),
                        gridRows = GetJsonInt(adjustments, "gridRows", 4),
                        totalPairs = GetJsonInt(adjustments, "totalPairs", 8),
                        flipAnimationMs = GetJsonInt(adjustments, "flipAnimationMs", 600),
                        visualComplexity = GetJsonString(adjustments, "visualComplexity") ?? "moderate",
                        timePressure = GetJsonString(adjustments, "timePressure") ?? "moderate",
                        cardDesign = GetJsonString(adjustments, "cardDesign") ?? "colorful"
                    },
                    "reactiontrainer" => new
                    {
                        targetCount = GetJsonInt(adjustments, "targetCount", 10),
                        targetDisplayMs = GetJsonInt(adjustments, "targetDisplayMs", 1000),
                        targetSize = GetJsonString(adjustments, "targetSize") ?? "medium",
                        distractors = GetJsonBool(adjustments, "distractors", false),
                        speedProgression = GetJsonString(adjustments, "speedProgression") ?? "steady",
                        feedbackIntensity = GetJsonString(adjustments, "feedbackIntensity") ?? "normal"
                    },
                    "sortingtask" => new
                    {
                        itemCount = GetJsonInt(adjustments, "itemCount", 8),
                        categoryCount = GetJsonInt(adjustments, "categoryCount", 3),
                        timeLimit = GetJsonInt(adjustments, "timeLimit", 120),
                        hintLevel = GetJsonString(adjustments, "hintLevel") ?? "subtle",
                        dragSensitivity = GetJsonString(adjustments, "dragSensitivity") ?? "normal",
                        categoryTheme = GetJsonString(adjustments, "categoryTheme") ?? "colors"
                    },
                    _ => new
                    {
                        difficultyMultiplier = GetJsonDouble(adjustments, "difficultyMultiplier", 1.0),
                        timeMultiplier = GetJsonDouble(adjustments, "timeMultiplier", 1.0),
                        hintEnabled = GetJsonBool(adjustments, "hintEnabled", true)
                    }
                };
            }
        }
        catch { }
        
        return GetDefaultGameAdjustments(gameType);
    }

    private object GetDefaultGameAdjustments(string gameType)
    {
        return gameType?.ToLower() switch
        {
            "memorymatch" => new
            {
                gridColumns = 4,
                gridRows = 4,
                totalPairs = 8,
                flipAnimationMs = 600,
                visualComplexity = "moderate",
                timePressure = "moderate",
                cardDesign = "colorful"
            },
            "reactiontrainer" => new
            {
                targetCount = 10,
                targetDisplayMs = 1000,
                targetSize = "medium",
                distractors = false,
                speedProgression = "steady",
                feedbackIntensity = "normal"
            },
            "sortingtask" => new
            {
                itemCount = 8,
                categoryCount = 3,
                timeLimit = 120,
                hintLevel = "subtle",
                dragSensitivity = "normal",
                categoryTheme = "colors"
            },
            _ => new
            {
                difficultyMultiplier = 1.0,
                timeMultiplier = 1.0,
                hintEnabled = true
            }
        };
    }

    private object GetDefaultUserFeedback(string gameType)
    {
        return new
        {
            encouragement = "Great job playing today! Every game helps your brain grow stronger! 🌟",
            effortNote = "You're putting in great effort! Keep practicing and you'll see amazing progress!",
            funMessage = "Ready for your next adventure? Your brain is a superhero getting stronger every day! 🦸‍♂️",
            performanceScore = 50,
            nextDifficultyLevel = 1,
            confidence = 0.7,
            gameAdjustments = GetDefaultGameAdjustments(gameType),
            generatedAt = DateTime.UtcNow,
            source = "Default (AI unavailable)"
        };
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

    private double GetJsonDouble(JsonElement element, string property, double defaultValue)
    {
        try
        {
            if (element.TryGetProperty(property, out var prop))
            {
                return prop.ValueKind == JsonValueKind.Number ? prop.GetDouble() : defaultValue;
            }
        }
        catch { }
        return defaultValue;
    }

    private bool GetJsonBool(JsonElement element, string property, bool defaultValue)
    {
        try
        {
            if (element.TryGetProperty(property, out var prop))
            {
                return prop.ValueKind == JsonValueKind.True || prop.ValueKind == JsonValueKind.False 
                    ? prop.GetBoolean() : defaultValue;
            }
        }
        catch { }
        return defaultValue;
    }

    /// <summary>
    /// Get AI-Powered analysis for THERAPIST (clinical analysis and recommendations)
    /// Sends user data to the AI model and gets detailed analysis
    /// </summary>
    [HttpGet("therapist/user/{userId}/ai-analysis")]
    public async Task<IActionResult> GetUserAIAnalysis(int userId)
    {
        try
        {
            _logger.LogInformation($"[AI-ANALYSIS] Starting AI analysis for user {userId}");
            
            var allSessions = await _statsService.GetAllSessionsAsync();
            var userSessions = allSessions.Where(s => s.UserId == userId).OrderBy(s => s.CreatedAt).ToList();
            
            if (!userSessions.Any())
            {
                return Ok(new { 
                    success = false, 
                    message = "No sessions found for this user",
                    aiAnalysis = (string?)null
                });
            }
            
            var firstSession = userSessions.First();
            var username = firstSession.Username ?? $"User_{userId}";
            
            // Build comprehensive data summary for AI
            var dataSummary = BuildUserDataSummary(username, userSessions);
            
            // Call Phi-4-mini for analysis
            var aiResponse = await CallPhiForTherapistAnalysis(dataSummary);
            
            return Ok(new {
                success = true,
                userId = userId,
                username = username,
                totalSessions = userSessions.Count,
                aiAnalysis = aiResponse
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"[AI-ANALYSIS] Error: {ex.Message}");
            return Ok(new { 
                success = false, 
                error = ex.Message,
                aiAnalysis = GetFallbackAnalysis()
            });
        }
    }

    private string BuildUserDataSummary(string username, List<GameStatsEntry> sessions)
    {
        var summary = new System.Text.StringBuilder();
        summary.AppendLine($"=== PATIENT: {username} ===");
        summary.AppendLine($"Total Sessions: {sessions.Count}");
        summary.AppendLine($"Date Range: {sessions.First().CreatedAt:MMM d, yyyy} to {sessions.Last().CreatedAt:MMM d, yyyy}");
        summary.AppendLine();
        
        // Group by game type
        var gameGroups = sessions.GroupBy(s => s.GameType).ToList();
        
        foreach (var group in gameGroups)
        {
            var gameSessions = group.OrderBy(s => s.CreatedAt).ToList();
            var gameName = GetGameDisplayName(group.Key ?? "Unknown");
            
            summary.AppendLine($"--- {gameName} ({gameSessions.Count} sessions) ---");
            summary.AppendLine($"  Average Score: {gameSessions.Average(s => s.Score):F0}");
            summary.AppendLine($"  Average Accuracy: {gameSessions.Average(s => s.Accuracy):F1}%");
            summary.AppendLine($"  Best Score: {gameSessions.Max(s => s.Score)}");
            summary.AppendLine($"  Worst Score: {gameSessions.Min(s => s.Score)}");
            summary.AppendLine($"  Max Difficulty Reached: {gameSessions.Max(s => s.Difficulty)}");
            
            // Calculate improvement
            var firstThree = gameSessions.Take(3).ToList();
            var lastThree = gameSessions.TakeLast(3).ToList();
            if (firstThree.Any() && lastThree.Any())
            {
                var improvement = lastThree.Average(s => s.Score) - firstThree.Average(s => s.Score);
                summary.AppendLine($"  Score Improvement: {(improvement >= 0 ? "+" : "")}{improvement:F0} points");
            }
            
            // Game-specific metrics
            if (group.Key?.ToLower() == "memorymatch")
            {
                var avgMoves = gameSessions.Average(s => s.TotalMoves);
                var avgTime = gameSessions.Average(s => s.TimeTakenSeconds);
                summary.AppendLine($"  Average Moves: {avgMoves:F1}");
                summary.AppendLine($"  Average Time: {avgTime:F1} seconds");
            }
            else if (group.Key?.ToLower() == "reactiontrainer")
            {
                var reactionTimes = gameSessions.Where(s => s.AverageReactionTimeMs.HasValue).ToList();
                if (reactionTimes.Any())
                {
                    summary.AppendLine($"  Average Reaction Time: {reactionTimes.Average(s => s.AverageReactionTimeMs!.Value):F0}ms");
                }
            }
            else if (group.Key?.ToLower() == "sortingtask")
            {
                var avgItems = gameSessions.Where(s => s.ItemsSorted.HasValue).Any() 
                    ? gameSessions.Where(s => s.ItemsSorted.HasValue).Average(s => s.ItemsSorted!.Value) : 0;
                summary.AppendLine($"  Average Items Sorted: {avgItems:F1}");
            }
            
            // Recent performance trend
            if (gameSessions.Count >= 3)
            {
                var recent = gameSessions.TakeLast(3).Average(s => s.Accuracy);
                var older = gameSessions.Take(Math.Max(1, gameSessions.Count - 3)).Average(s => s.Accuracy);
                var trend = recent > older + 5 ? "IMPROVING" : recent < older - 5 ? "DECLINING" : "STABLE";
                summary.AppendLine($"  Recent Trend: {trend}");
            }
            
            summary.AppendLine();
        }
        
        // Overall metrics
        summary.AppendLine("--- OVERALL PERFORMANCE ---");
        summary.AppendLine($"  Total Time Played: {sessions.Sum(s => s.TimeTakenSeconds) / 60:F1} minutes");
        summary.AppendLine($"  Overall Accuracy: {sessions.Average(s => s.Accuracy):F1}%");
        summary.AppendLine($"  Overall Average Score: {sessions.Average(s => s.Score):F0}");
        
        return summary.ToString();
    }

    private async Task<object> CallPhiForTherapistAnalysis(string dataSummary)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(180);

            var prompt = $@"You are a cognitive rehabilitation therapist AI assistant. Analyze this patient's game performance data and provide a professional assessment.

{dataSummary}

Based on this data, provide a DETAILED analysis in the following JSON format. Be specific and reference actual numbers from the data:

{{
  ""overallAssessment"": ""A 2-3 sentence summary of the patient's overall cognitive performance"",
  ""memoryAnalysis"": ""Specific analysis of Memory Match performance if played, otherwise say 'No Memory Match data'"",
  ""reactionAnalysis"": ""Specific analysis of Reaction Trainer performance if played, otherwise say 'No Reaction data'"",
  ""sortingAnalysis"": ""Specific analysis of Sorting Task performance if played, otherwise say 'No Sorting data'"",
  ""cognitiveStrengths"": [""List"", ""of"", ""identified"", ""strengths""],
  ""areasOfConcern"": [""List"", ""of"", ""areas"", ""needing"", ""attention""],
  ""progressSummary"": ""Summary of improvement or decline trends observed"",
  ""therapyRecommendations"": [""Specific"", ""actionable"", ""therapy"", ""recommendations""],
  ""nextSessionFocus"": ""What to focus on in the next therapy session"",
  ""riskLevel"": ""Low/Moderate/High based on performance patterns"",
  ""encouragingNote"": ""A positive, encouraging note for the therapist to share with the patient""
}}

Output ONLY the JSON object. No explanations, no markdown, just valid JSON.";

            var request = new
            {
                model = "Phi-4-mini",
                messages = new[]
                {
                    new { role = "system", content = "You are an expert cognitive rehabilitation therapist AI. Analyze patient data and provide professional assessments in JSON format only." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.3,
                max_tokens = 2000
            };

            _logger.LogInformation("[AI-ANALYSIS] Sending data to Phi-4-mini...");
            var response = await httpClient.PostAsJsonAsync(AI_ENDPOINT, request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"[AI-ANALYSIS] AI server returned {response.StatusCode}");
                return GetFallbackAnalysis();
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"[AI-ANALYSIS] Got response ({responseBody.Length} bytes)");

            // Extract JSON from response
            var jsonContent = ExtractJsonFromAIResponse(responseBody);
            
            // Parse the AI response
            var aiResult = JsonSerializer.Deserialize<JsonElement>(jsonContent);
            
            return new {
                overallAssessment = GetJsonString(aiResult, "overallAssessment") ?? "Review session data for assessment.",
                memoryAnalysis = GetJsonString(aiResult, "memoryAnalysis") ?? "No Memory Match data available.",
                reactionAnalysis = GetJsonString(aiResult, "reactionAnalysis") ?? "No Reaction data available.",
                sortingAnalysis = GetJsonString(aiResult, "sortingAnalysis") ?? "No Sorting data available.",
                cognitiveStrengths = GetJsonStringArray(aiResult, "cognitiveStrengths"),
                areasOfConcern = GetJsonStringArray(aiResult, "areasOfConcern"),
                progressSummary = GetJsonString(aiResult, "progressSummary") ?? "Insufficient data for trend analysis.",
                therapyRecommendations = GetJsonStringArray(aiResult, "therapyRecommendations"),
                nextSessionFocus = GetJsonString(aiResult, "nextSessionFocus") ?? "Continue current exercises.",
                riskLevel = GetJsonString(aiResult, "riskLevel") ?? "Low",
                encouragingNote = GetJsonString(aiResult, "encouragingNote") ?? "Great progress! Keep up the consistent effort - every session matters! 🌟",
                generatedAt = DateTime.UtcNow,
                source = "Phi-4-mini AI"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"[AI-ANALYSIS] Error calling AI: {ex.Message}");
            return GetFallbackAnalysis();
        }
    }

    private string ExtractJsonFromAIResponse(string response)
    {
        try
        {
            // First try to parse the whole response as the chat completion format
            var chatResponse = JsonSerializer.Deserialize<JsonElement>(response);
            if (chatResponse.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var content = choices[0].GetProperty("message").GetProperty("content").GetString();
                if (!string.IsNullOrEmpty(content))
                {
                    // Find JSON in the content
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
        
        // Fallback: try to find JSON directly
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

    private object GetFallbackAnalysis()
    {
        return new {
            overallAssessment = "AI analysis is currently unavailable. Please review the statistical data above for performance insights.",
            memoryAnalysis = "Check Memory Match statistics in the game breakdown section.",
            reactionAnalysis = "Check Reaction Trainer statistics in the game breakdown section.",
            sortingAnalysis = "Check Sorting Task statistics in the game breakdown section.",
            cognitiveStrengths = new List<string> { "Data available in statistics panel" },
            areasOfConcern = new List<string> { "Review accuracy metrics for areas needing attention" },
            progressSummary = "Review the improvement percentages in each game category.",
            therapyRecommendations = new List<string> { "Continue regular cognitive exercises", "Focus on games with lower accuracy scores" },
            nextSessionFocus = "Review recent session performance and adjust difficulty accordingly.",
            riskLevel = "Unable to assess - review data manually",
            encouragingNote = "Every session is progress! Keep encouraging consistent practice.",
            generatedAt = DateTime.UtcNow,
            source = "Fallback (AI unavailable)"
        };
    }

    private object GenerateAITherapistAnalysis(string username, dynamic gameBreakdown, double overallImprovement, 
        List<GameStatsEntry> sessions, List<object> strengths, List<object> areasForImprovement)
    {
        var avgAccuracy = sessions.Any() ? (double)sessions.Average(s => s.Accuracy) : 0;
        var avgScore = sessions.Any() ? sessions.Average(s => s.Score) : 0;
        var totalSessions = sessions.Count;
        var daysSinceFirst = sessions.Any() ? (DateTime.UtcNow - sessions.Min(s => s.CreatedAt)).TotalDays : 0;
        var sessionsPerWeek = daysSinceFirst > 0 ? totalSessions / (daysSinceFirst / 7) : totalSessions;
        
        // Determine cognitive profile
        var cognitiveProfile = avgAccuracy >= 70 ? "High Performer" : 
                              avgAccuracy >= 50 ? "Developing" : 
                              avgAccuracy >= 30 ? "Needs Support" : "Requires Intervention";
        
        // Generate performance summary
        var performanceSummary = GeneratePerformanceSummary(username, avgAccuracy, avgScore, overallImprovement, totalSessions);
        
        // Generate cognitive assessment
        var cognitiveAssessment = GenerateCognitiveAssessment(gameBreakdown, avgAccuracy, strengths.Count, areasForImprovement.Count);
        
        // Generate progress analysis
        var progressAnalysis = GenerateProgressAnalysis(overallImprovement, sessionsPerWeek, totalSessions);
        
        // Generate therapy recommendations
        var therapyRecommendations = GenerateTherapyRecommendations(avgAccuracy, overallImprovement, gameBreakdown, sessionsPerWeek);
        
        // Generate next steps
        var nextSteps = GenerateNextSteps(avgAccuracy, overallImprovement, totalSessions, gameBreakdown);
        
        // Generate risk assessment
        var riskLevel = avgAccuracy < 30 || overallImprovement < -10 ? "High" :
                       avgAccuracy < 50 || overallImprovement < -5 ? "Moderate" : "Low";
        
        var riskAssessment = GenerateRiskAssessment(riskLevel, avgAccuracy, overallImprovement);

        return new
        {
            cognitiveProfile = cognitiveProfile,
            performanceSummary = performanceSummary,
            cognitiveAssessment = cognitiveAssessment,
            progressAnalysis = progressAnalysis,
            therapyRecommendations = therapyRecommendations,
            nextSteps = nextSteps,
            riskLevel = riskLevel,
            riskAssessment = riskAssessment,
            engagementScore = Math.Min(100, (int)(sessionsPerWeek * 20 + (avgAccuracy / 2))),
            motivationIndex = overallImprovement > 0 ? "Positive" : overallImprovement < -5 ? "Declining" : "Stable",
            generatedAt = DateTime.UtcNow
        };
    }

    private string GeneratePerformanceSummary(string username, double avgAccuracy, double avgScore, double improvement, int totalSessions)
    {
        var trend = improvement > 5 ? "showing positive improvement" : 
                   improvement < -5 ? "experiencing some challenges" : "maintaining steady performance";
        
        var accuracyLevel = avgAccuracy >= 70 ? "excellent" : 
                          avgAccuracy >= 50 ? "good" : 
                          avgAccuracy >= 30 ? "developing" : "needs significant support";
        
        return $"{username} has completed {totalSessions} cognitive training sessions with an average accuracy of {avgAccuracy:F1}% " +
               $"({accuracyLevel}). The patient is {trend} with an average score of {avgScore:F0} points. " +
               $"Overall performance trend indicates a {Math.Abs(improvement):F1} point " +
               $"{(improvement >= 0 ? "improvement" : "decline")} compared to initial sessions.";
    }

    private string GenerateCognitiveAssessment(dynamic gameBreakdown, double avgAccuracy, int strengthCount, int improvementCount)
    {
        var assessment = new System.Text.StringBuilder();
        
        if (avgAccuracy >= 70)
        {
            assessment.Append("Patient demonstrates strong cognitive processing abilities across multiple domains. ");
            assessment.Append("Neural pathway engagement appears healthy with consistent performance. ");
        }
        else if (avgAccuracy >= 50)
        {
            assessment.Append("Patient shows developing cognitive skills with room for improvement. ");
            assessment.Append("Some areas of strength identified alongside areas requiring focused attention. ");
        }
        else
        {
            assessment.Append("Patient requires additional cognitive support and intervention strategies. ");
            assessment.Append("Recommend comprehensive assessment to identify specific learning barriers. ");
        }
        
        if (strengthCount > 0)
            assessment.Append($"Identified {strengthCount} area(s) of cognitive strength. ");
        if (improvementCount > 0)
            assessment.Append($"Flagged {improvementCount} area(s) requiring targeted intervention. ");
        
        return assessment.ToString();
    }

    private string GenerateProgressAnalysis(double improvement, double sessionsPerWeek, int totalSessions)
    {
        var analysis = new System.Text.StringBuilder();
        
        if (totalSessions < 5)
        {
            analysis.Append("Insufficient data for comprehensive trend analysis. ");
            analysis.Append("Recommend continued sessions to establish baseline metrics. ");
        }
        else
        {
            if (improvement > 10)
            {
                analysis.Append("Outstanding progress trajectory! Patient shows significant cognitive gains. ");
                analysis.Append("Consider advancing to more challenging difficulty levels. ");
            }
            else if (improvement > 5)
            {
                analysis.Append("Positive progress detected. Patient is responding well to cognitive training. ");
                analysis.Append("Current approach is effective - maintain consistency. ");
            }
            else if (improvement > -5)
            {
                analysis.Append("Performance remains stable. Consider introducing variety or adjusting challenge level ");
                analysis.Append("to stimulate further progress. ");
            }
            else
            {
                analysis.Append("Declining performance trend identified. Recommend reviewing current approach ");
                analysis.Append("and potentially reducing difficulty to rebuild confidence. ");
            }
        }
        
        // Session frequency analysis
        if (sessionsPerWeek >= 5)
            analysis.Append("Excellent engagement frequency supporting consistent cognitive development. ");
        else if (sessionsPerWeek >= 3)
            analysis.Append("Good session frequency. Consider increasing to 5+ sessions per week for optimal results. ");
        else
            analysis.Append("Low session frequency may limit progress. Encourage more regular practice. ");
        
        return analysis.ToString();
    }

    private List<string> GenerateTherapyRecommendations(double avgAccuracy, double improvement, dynamic gameBreakdown, double sessionsPerWeek)
    {
        var recommendations = new List<string>();
        
        // Accuracy-based recommendations
        if (avgAccuracy < 40)
        {
            recommendations.Add("🎯 Consider reducing game difficulty to build foundational skills and confidence.");
            recommendations.Add("📝 Implement structured practice sessions focusing on one game type at a time.");
        }
        else if (avgAccuracy < 60)
        {
            recommendations.Add("📈 Patient is ready for moderate challenges. Gradually increase complexity.");
            recommendations.Add("🔄 Introduce varied exercises to strengthen neural pathway diversity.");
        }
        else
        {
            recommendations.Add("⭐ Patient excelling! Challenge with advanced difficulty levels and timed exercises.");
            recommendations.Add("🎓 Consider introducing new game types to expand cognitive skill set.");
        }
        
        // Progress-based recommendations
        if (improvement < -5)
        {
            recommendations.Add("⚠️ Address declining performance through one-on-one review session.");
            recommendations.Add("💬 Discuss any external factors that may be affecting cognitive performance.");
        }
        else if (improvement > 10)
        {
            recommendations.Add("🏆 Celebrate achievements! Positive reinforcement will maintain motivation.");
        }
        
        // Engagement recommendations
        if (sessionsPerWeek < 3)
        {
            recommendations.Add("📅 Establish routine schedule - aim for at least 3-4 sessions per week.");
            recommendations.Add("🏠 Consider home practice assignments between therapy sessions.");
        }
        
        // Memory-specific
        recommendations.Add("🧠 Memory exercises strengthen hippocampal function - maintain regular Memory Match practice.");
        recommendations.Add("⚡ Reaction training improves processing speed - balance with strategic games.");
        
        return recommendations;
    }

    private List<string> GenerateNextSteps(double avgAccuracy, double improvement, int totalSessions, dynamic gameBreakdown)
    {
        var steps = new List<string>();
        
        steps.Add($"1. Review current session with patient - discuss {(improvement >= 0 ? "achievements" : "challenges")}");
        
        if (avgAccuracy < 50)
            steps.Add("2. Adjust difficulty settings to appropriate level for patient's current abilities");
        else
            steps.Add("2. Consider incrementing difficulty level to maintain engagement");
        
        if (totalSessions < 10)
            steps.Add("3. Continue baseline data collection - minimum 10 sessions recommended");
        else
            steps.Add("3. Generate comprehensive progress report for care team review");
        
        steps.Add("4. Schedule follow-up assessment in 2 weeks to measure continued progress");
        steps.Add("5. Document observations and update treatment plan as needed");
        
        return steps;
    }

    private string GenerateRiskAssessment(string riskLevel, double avgAccuracy, double improvement)
    {
        return riskLevel switch
        {
            "High" => $"⚠️ HIGH RISK: Patient accuracy ({avgAccuracy:F1}%) and trend ({improvement:F1}) indicate significant " +
                      "cognitive challenges. Recommend immediate care team review and potential referral for " +
                      "comprehensive neuropsychological evaluation. Consider environmental and emotional factors.",
            
            "Moderate" => $"⚡ MODERATE RISK: Performance metrics suggest patient may benefit from adjusted approach. " +
                         $"Current accuracy of {avgAccuracy:F1}% with {improvement:F1} point trend change warrants " +
                         "close monitoring. Review in next 1-2 weeks recommended.",
            
            _ => $"✅ LOW RISK: Patient performing within expected parameters. Accuracy of {avgAccuracy:F1}% " +
                 $"and positive/stable trend indicate appropriate engagement with cognitive rehabilitation program. " +
                 "Continue current approach with regular monitoring."
        };
    }

    private List<string> GenerateTherapistRecommendations(dynamic gameBreakdown, double improvement, int totalSessions)
    {
        var recommendations = new List<string>();
        
        if (totalSessions < 5)
        {
            recommendations.Add("Patient needs more sessions to establish a baseline. Recommend at least 5 sessions per game type.");
        }
        
        if (improvement > 10)
        {
            recommendations.Add("Excellent progress! Patient shows significant improvement. Consider increasing difficulty levels.");
        }
        else if (improvement < -5)
        {
            recommendations.Add("Performance declining. Review with patient - may need additional support or reduced difficulty.");
        }
        
        recommendations.Add("Continue regular monitoring and encourage consistent practice.");
        
        return recommendations;
    }

    /// <summary>
    /// Get progress data for a user (compatible with existing Progress page)
    /// </summary>
    [HttpGet("progress/{userId}")]
    public async Task<IActionResult> GetProgressData(int userId)
    {
        try
        {
            var summary = await _statsService.GetUserSummaryAsync(userId);
            
            // Transform to format expected by Progress page
            var progressData = new
            {
                totalGamesPlayed = summary.TotalGamesPlayed,
                currentStreak = summary.CurrentStreak,
                averageAccuracy = summary.AverageAccuracy,
                currentLevel = summary.GameTypeBreakdown.Any() ? summary.GameTypeBreakdown.Max(g => g.CurrentLevel) : 1,
                bestScore = summary.BestScore,
                averageScore = summary.AverageScore,
                totalTimePlayed = summary.TotalTimePlayed,
                longestStreak = summary.LongestStreak,
                gameProgress = summary.GameTypeBreakdown.Select(g => new
                {
                    gameType = g.GameType,
                    gameName = GetGameDisplayName(g.GameType),
                    icon = g.GameIcon,
                    color = g.GameColor,
                    gamesPlayed = g.GamesPlayed,
                    bestScore = g.BestScore,
                    averageScore = g.AverageScore,
                    skillProgress = g.SkillProgress
                }).ToList(),
                recentActivity = summary.RecentSessions.Take(5).Select(s => new
                {
                    gameType = s.GameType,
                    gameName = GetGameDisplayName(s.GameType),
                    difficulty = s.Difficulty,
                    difficultyName = s.DifficultyName,
                    score = s.Score,
                    accuracy = s.Accuracy,
                    timeTaken = s.TimeTakenSeconds,
                    playedAt = s.CreatedAt,
                    moves = s.TotalMoves
                }).ToList(),
                weeklyActivity = summary.WeeklyActivity,
                recommendations = GenerateRecommendations(summary)
            };

            return Ok(progressData);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[JSON-STATS] Error getting progress data: {ex.Message}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get statistics data for a user (compatible with existing Statistics page)
    /// </summary>
    [HttpGet("statistics/{userId}")]
    public async Task<IActionResult> GetStatisticsData(int userId)
    {
        try
        {
            var summary = await _statsService.GetUserSummaryAsync(userId);
            var avgSessionDuration = summary.TotalGamesPlayed > 0 
                ? (decimal)summary.TotalTimePlayed / 60 / summary.TotalGamesPlayed 
                : 0;
            
            // Transform to format expected by Statistics page
            var statsData = new
            {
                totalSessions = summary.TotalGamesPlayed,
                averageSessionDuration = Math.Round(avgSessionDuration, 1),
                overallAccuracy = summary.AverageAccuracy,
                totalPoints = summary.TotalGamesPlayed > 0 ? summary.RecentSessions.Sum(s => s.Score) : 0,
                totalTimePlayed = summary.TotalTimePlayed,
                gameStats = summary.GameTypeBreakdown.Select(g => new
                {
                    gameType = g.GameType,
                    gameName = GetGameDisplayName(g.GameType),
                    icon = g.GameIcon,
                    color = g.GameColor,
                    gamesPlayed = g.GamesPlayed,
                    averageScore = g.AverageScore,
                    bestScore = g.BestScore,
                    averageAccuracy = g.AverageAccuracy,
                    extraMetric = GetExtraMetric(g),
                    extraMetricLabel = GetExtraMetricLabel(g.GameType)
                }).ToList(),
                weeklyActivity = GetWeeklyActivityWithPercent(summary.WeeklyActivity),
                difficultyProgression = GetDifficultyProgressionDto(summary),
                timeInsights = GenerateTimeInsights(summary),
                achievements = GenerateAchievementsDto(summary)
            };

            return Ok(statsData);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[JSON-STATS] Error getting statistics data: {ex.Message}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // Helper methods
    private string GetDifficultyName(int difficulty) => difficulty switch
    {
        1 => "Easy",
        2 => "Medium",
        3 => "Hard",
        _ => "Unknown"
    };

    private string GetGameDisplayName(string gameType) => gameType switch
    {
        "MemoryMatch" => "Memory Match",
        "ReactionTrainer" => "Reaction Trainer",
        "SortingTask" => "Sorting Task",
        "PatternCopy" => "Pattern Copy",
        "TrailMaking" => "Trail Making",
        "DualTask" => "Dual Task Training",
        "StroopTest" => "Stroop Test",
        _ => gameType
    };

    private List<object> GenerateRecommendations(UserGameSummary summary)
    {
        var recommendations = new List<object>();

        if (summary.CurrentStreak >= 3)
        {
            recommendations.Add(new
            {
                type = "streak",
                icon = "bi-check-circle-fill",
                title = "Keep it up!",
                message = $"Your streak of {summary.CurrentStreak} days is excellent. Consistency is key to cognitive improvement."
            });
        }

        var lowestGame = summary.GameTypeBreakdown.OrderBy(g => g.GamesPlayed).FirstOrDefault();
        if (lowestGame != null && summary.GameTypeBreakdown.Count > 1)
        {
            recommendations.Add(new
            {
                type = "try",
                icon = "bi-graph-up",
                title = "Try Something New",
                message = $"You haven't played much {GetGameDisplayName(lowestGame.GameType)}. Give it a try!"
            });
        }

        recommendations.Add(new
        {
            type = "goal",
            icon = "bi-target",
            title = "Weekly Goal",
            message = $"Aim for {Math.Max(10, summary.TotalGamesPlayed + 5)} games this week to keep improving!"
        });

        return recommendations;
    }

    private string GetExtraMetric(GameTypeStats g)
    {
        return g.GameType switch
        {
            "MemoryMatch" => $"{g.AverageMoves:0.0}",
            "ReactionTrainer" => $"{g.AverageReactionTime:0}ms",
            "SortingTask" => $"{g.AverageAccuracy:0}%",
            _ => ""
        };
    }

    private string GetExtraMetricLabel(string gameType)
    {
        return gameType switch
        {
            "MemoryMatch" => "Avg. Moves",
            "ReactionTrainer" => "Avg. Reaction",
            "SortingTask" => "Accuracy Rate",
            _ => ""
        };
    }

    private List<object> GetWeeklyActivityWithPercent(List<DailyActivity> activity)
    {
        if (activity == null || !activity.Any())
        {
            return new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" }
                .Select(day => new { day, gamesPlayed = 0, heightPercent = 10.0 } as object)
                .ToList();
        }

        var maxGames = activity.Max(a => a.GamesPlayed);
        if (maxGames == 0) maxGames = 1;

        return activity.Select(a => new
        {
            day = a.Day,
            gamesPlayed = a.GamesPlayed,
            heightPercent = Math.Max(10, (double)a.GamesPlayed / maxGames * 100)
        } as object).ToList();
    }

    private object GetDifficultyProgressionDto(UserGameSummary summary)
    {
        var maxLevel = summary.GameTypeBreakdown.Any() ? summary.GameTypeBreakdown.Max(g => g.CurrentLevel) : 1;
        var avgProgress = summary.GameTypeBreakdown.Any() ? summary.GameTypeBreakdown.Average(g => (double)g.SkillProgress) : 0;

        var levels = new List<object>
        {
            new { level = 1, name = "Beginner", status = maxLevel > 1 ? "completed" : (maxLevel == 1 ? "active" : "locked") },
            new { level = 2, name = "Intermediate", status = maxLevel > 2 ? "completed" : (maxLevel == 2 ? "active" : "locked") },
            new { level = 3, name = "Advanced", status = maxLevel > 3 ? "completed" : (maxLevel == 3 ? "active" : "locked") },
            new { level = 4, name = "Expert", status = maxLevel >= 4 ? "active" : "locked" }
        };

        return new
        {
            currentLevel = maxLevel,
            currentLevelName = GetDifficultyName(maxLevel),
            progressToNext = avgProgress,
            levels = levels
        };
    }

    private string GetMostActiveDay(List<DailyActivity> activity)
    {
        if (activity == null || !activity.Any()) return "Wednesday";
        return activity.OrderByDescending(a => a.GamesPlayed).First().Day;
    }

    private List<object> GenerateTimeInsights(UserGameSummary summary)
    {
        var avgDuration = summary.TotalGamesPlayed > 0 ? summary.TotalTimePlayed / summary.TotalGamesPlayed : 0;
        var totalHours = summary.TotalTimePlayed / 3600;
        var totalMins = (summary.TotalTimePlayed % 3600) / 60;
        var totalSecs = summary.TotalTimePlayed % 60;
        var hasData = summary.TotalGamesPlayed > 0;

        // Determine best performance time based on when sessions were played
        var bestTimeValue = "Play more to discover!";
        var bestTimeDetail = "We'll analyze when you perform best.";
        if (hasData && summary.RecentSessions.Any())
        {
            var morningGames = summary.RecentSessions.Count(s => s.CreatedAt.Hour >= 6 && s.CreatedAt.Hour < 12);
            var afternoonGames = summary.RecentSessions.Count(s => s.CreatedAt.Hour >= 12 && s.CreatedAt.Hour < 18);
            var eveningGames = summary.RecentSessions.Count(s => s.CreatedAt.Hour >= 18 && s.CreatedAt.Hour < 22);
            var nightGames = summary.RecentSessions.Count(s => s.CreatedAt.Hour >= 22 || s.CreatedAt.Hour < 6);

            var timeCounts = new[] { 
                (name: "Morning (6AM-12PM)", count: morningGames),
                (name: "Afternoon (12PM-6PM)", count: afternoonGames),
                (name: "Evening (6PM-10PM)", count: eveningGames),
                (name: "Night (10PM-6AM)", count: nightGames)
            };
            var bestTime = timeCounts.OrderByDescending(t => t.count).First();
            if (bestTime.count > 0)
            {
                bestTimeValue = bestTime.name;
                bestTimeDetail = $"You've played {bestTime.count} game(s) during this time.";
            }
        }

        // Determine most active day
        var mostActiveDay = "Not enough data";
        var mostActiveDayDetail = "Keep playing to see patterns.";
        if (hasData && summary.WeeklyActivity.Any())
        {
            var topDay = summary.WeeklyActivity.OrderByDescending(a => a.GamesPlayed).First();
            if (topDay.GamesPlayed > 0)
            {
                mostActiveDay = topDay.Day;
                mostActiveDayDetail = $"{topDay.Day} with {topDay.GamesPlayed} game(s) this week.";
            }
        }

        // Format average duration
        var avgMinutes = avgDuration / 60.0;
        var avgDurationValue = avgMinutes >= 1 
            ? $"{avgMinutes:0.0} Minutes" 
            : $"{avgDuration} Seconds";

        // Format total time played
        string totalTimeValue;
        if (totalHours > 0)
            totalTimeValue = $"{totalHours}h {totalMins}min";
        else if (totalMins > 0)
            totalTimeValue = $"{totalMins}min {totalSecs}s";
        else
            totalTimeValue = $"{totalSecs}s";

        return new List<object>
        {
            new
            {
                icon = "bi-brightness-high",
                iconClass = "morning",
                title = "Best Performance Time",
                value = bestTimeValue,
                detail = bestTimeDetail
            },
            new
            {
                icon = "bi-calendar3-week",
                iconClass = "calendar",
                title = "Most Active Day",
                value = mostActiveDay,
                detail = mostActiveDayDetail
            },
            new
            {
                icon = "bi-hourglass-split",
                iconClass = "hourglass",
                title = "Avg. Session Duration",
                value = avgDurationValue,
                detail = hasData ? $"Your sessions last on average {avgDuration} seconds." : "Play games to track session times."
            },
            new
            {
                icon = "bi-play-fill",
                iconClass = "total",
                title = "Total Time Played",
                value = totalTimeValue,
                detail = "Total time invested in cognitive training."
            }
        };
    }

    private List<object> GenerateAchievementsDto(UserGameSummary summary)
    {
        var achievements = new List<object>();

        achievements.Add(new
        {
            icon = "bi-fire",
            iconClass = "fire",
            name = summary.CurrentStreak >= 7 ? "7-Day Streak" : (summary.CurrentStreak >= 3 ? "3-Day Streak" : "First Streak"),
            description = summary.CurrentStreak >= 3 ? "Keep it up!" : "Play 3 days in a row",
            unlocked = summary.CurrentStreak >= 3
        });

        achievements.Add(new
        {
            icon = "bi-lightning-fill",
            iconClass = "lightning",
            name = "Quick Start",
            description = summary.TotalGamesPlayed >= 5 ? "Fast starter!" : "Complete 5 games",
            unlocked = summary.TotalGamesPlayed >= 5
        });

        achievements.Add(new
        {
            icon = "bi-bullseye",
            iconClass = "bullseye",
            name = "Sharpshooter",
            description = summary.AverageAccuracy >= 90 ? "Amazing accuracy!" : "Score 90%+ accuracy",
            unlocked = summary.AverageAccuracy >= 90
        });

        achievements.Add(new
        {
            icon = "bi-graph-up",
            iconClass = "growth",
            name = "Steady Progress",
            description = summary.TotalGamesPlayed >= 10 ? "Always improving!" : "Play 10+ games",
            unlocked = summary.TotalGamesPlayed >= 10
        });

        return achievements;
    }
}

/// <summary>
/// Request model for saving game session to JSON storage
/// </summary>
public class JsonSaveGameSessionRequest
{
    public int UserId { get; set; }
    public string? Username { get; set; }
    public string GameType { get; set; } = "";
    public string? GameMode { get; set; }
    public int Difficulty { get; set; }
    public string? DifficultyName { get; set; }
    public int Score { get; set; }
    public decimal Accuracy { get; set; }
    public int TotalMoves { get; set; }
    public int CorrectMoves { get; set; }
    public int ErrorCount { get; set; }
    public int TimeTakenSeconds { get; set; }
    public int? MatchedPairs { get; set; }
    public int? TotalPairs { get; set; }
    public double? AverageReactionTimeMs { get; set; }
    public int? ItemsSorted { get; set; }
    public int? TotalItems { get; set; }
    public string? AiEncouragement { get; set; }
    public string? AiFunMessage { get; set; }
    public string? AiEffortNote { get; set; }
    public string? RecommendedDifficulty { get; set; }
    public double? AiConfidence { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Notes { get; set; }
}
