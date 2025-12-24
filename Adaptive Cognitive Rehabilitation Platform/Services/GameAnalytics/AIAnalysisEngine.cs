using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NeuroPath.Models;

namespace AdaptiveCognitiveRehabilitationPlatform.Services.GameAnalytics
{
    /// <summary>
    /// AI-powered analysis engine for game performance data
    /// Uses LM Studio Phi-4-mini to generate personalized insights
    /// </summary>
    public class AIAnalysisEngine
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AIAnalysisEngine> _logger;
        private readonly PerformanceMetricsCalculator _metricsCalculator;

        private const string LM_STUDIO_ENDPOINT = "http://localhost:1234/v1/chat/completions";
        private const string MODEL_NAME = "local-model";
        private const int ANALYSIS_TIMEOUT_SECONDS = 30;

        public AIAnalysisEngine(
            HttpClient httpClient,
            ILogger<AIAnalysisEngine> logger,
            PerformanceMetricsCalculator metricsCalculator)
        {
            _httpClient = httpClient;
            _logger = logger;
            _metricsCalculator = metricsCalculator;
            _httpClient.Timeout = TimeSpan.FromSeconds(ANALYSIS_TIMEOUT_SECONDS);
        }

        /// <summary>
        /// Analyze game session and generate AI feedback
        /// </summary>
        public async Task<AISessionAnalysis> AnalyzeGameSessionAsync(GameSession session, List<GameSession> userHistory)
        {
            var analysis = new AISessionAnalysis
            {
                SessionId = session.SessionId,
                AnalyzedAt = DateTime.UtcNow
            };

            try
            {
                // Generate performance summary
                var summary = GeneratePerformanceSummary(session);
                analysis.PerformanceSummary = summary;

                // Generate AI feedback
                var feedback = await GenerateAIFeedbackAsync(session, userHistory, summary);
                analysis.AIFeedback = feedback;

                // Generate recommendations
                var recommendations = GenerateRecommendations(session, userHistory);
                analysis.Recommendations = recommendations;

                // Generate encouragement
                var encouragement = await GenerateEncouragementAsync(session, userHistory, feedback);
                analysis.Encouragement = encouragement;

                analysis.Success = true;
                _logger.LogInformation($"Successfully analyzed session {session.SessionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error analyzing session {session.SessionId}: {ex.Message}");
                analysis.Success = false;
                analysis.ErrorMessage = ex.Message;
            }

            return analysis;
        }

        /// <summary>
        /// Generate comprehensive profile analysis
        /// </summary>
        public async Task<AIProfileAnalysis> AnalyzeUserProfileAsync(
            UserProfile profile,
            List<GameSession> allSessions)
        {
            var analysis = new AIProfileAnalysis
            {
                ProfileId = profile.ProfileId,
                ProfileName = profile.ProfileName,
                AnalyzedAt = DateTime.UtcNow
            };

            try
            {
                if (!allSessions.Any())
                {
                    analysis.Message = "No sessions completed yet. Start playing games to get personalized analysis!";
                    analysis.Success = true;
                    return analysis;
                }

                // Calculate overall metrics
                var metrics = _metricsCalculator.CalculateAggregatedMetrics(allSessions.ToList());
                analysis.OverallMetrics = metrics;

                // Analyze by game type
                var memoryGameSessions = allSessions.Where(s => s.GameType == "MemoryMatch").ToList();
                var reactionGameSessions = allSessions.Where(s => s.GameType == "ReactionTrainer").ToList();
                var sortingGameSessions = allSessions.Where(s => s.GameType == "SortingTask").ToList();

                if (memoryGameSessions.Any())
                    analysis.MemoryGameMetrics = _metricsCalculator.CalculateGameTypeMetrics(memoryGameSessions, "MemoryMatch");
                if (reactionGameSessions.Any())
                    analysis.ReactionGameMetrics = _metricsCalculator.CalculateGameTypeMetrics(reactionGameSessions, "ReactionTrainer");
                if (sortingGameSessions.Any())
                    analysis.SortingGameMetrics = _metricsCalculator.CalculateGameTypeMetrics(sortingGameSessions, "SortingTask");

                // Identify strengths and weaknesses
                var strengths = IdentifyStrengths(analysis.MemoryGameMetrics, analysis.ReactionGameMetrics, analysis.SortingGameMetrics);
                var weaknesses = IdentifyWeaknesses(analysis.MemoryGameMetrics, analysis.ReactionGameMetrics, analysis.SortingGameMetrics);

                analysis.CognitiveStrengths = strengths;
                analysis.AreasForImprovement = weaknesses;

                // Generate AI-powered insights
                var aiInsights = await GenerateProfileInsightsAsync(profile, analysis, allSessions);
                analysis.AIInsights = aiInsights;

                // Calculate recommended next difficulty
                analysis.RecommendedNextDifficulty = CalculateRecommendedDifficulty(metrics, profile.CurrentDifficultyLevel);

                // Calculate improvement velocity
                analysis.ImprovementVelocity = _metricsCalculator.CalculateImprovementVelocity(allSessions);

                analysis.Success = true;
                _logger.LogInformation($"Successfully analyzed profile {profile.ProfileId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error analyzing profile {profile.ProfileId}: {ex.Message}");
                analysis.Success = false;
                analysis.ErrorMessage = ex.Message;
            }

            return analysis;
        }

        /// <summary>
        /// Generate performance summary for a session
        /// </summary>
        private string GeneratePerformanceSummary(GameSession session)
        {
            var summary = new StringBuilder();
            summary.AppendLine($"Game: {session.GameType}");
            summary.AppendLine($"Difficulty: {session.Difficulty}/5");
            summary.AppendLine($"Accuracy: {session.Accuracy:F1}%");
            summary.AppendLine($"Efficiency: {session.EfficiencyScore:F1}%");
            summary.AppendLine($"Duration: {session.TotalSeconds} seconds");
            summary.AppendLine($"Performance Score: {session.PerformanceScore}/100");
            summary.AppendLine($"Status: {session.Status}");

            return summary.ToString();
        }

        /// <summary>
        /// Generate AI feedback using LM Studio
        /// </summary>
        private async Task<string> GenerateAIFeedbackAsync(
            GameSession session,
            List<GameSession> userHistory,
            string performanceSummary)
        {
            try
            {
                var recentAverage = userHistory
                    .Where(s => s.Status == "Completed")
                    .OrderByDescending(s => s.TimeStarted)
                    .Take(5)
                    .Average(s => s.PerformanceScore);

                var prompt = $@"Analyze this cognitive game performance and provide brief constructive feedback:

Performance Data:
{performanceSummary}

Recent Average Score: {recentAverage:F1}/100
Games Played: {userHistory.Count}

Provide 1-2 sentences of specific, encouraging feedback about this session. Focus on what was done well and one actionable improvement.";

                return await CallLMStudioAsync(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error generating AI feedback: {ex.Message}");
                return "Great effort! Keep practicing to improve your score.";
            }
        }

        /// <summary>
        /// Generate recommendations for next session
        /// </summary>
        private List<string> GenerateRecommendations(GameSession session, List<GameSession> userHistory)
        {
            var recommendations = new List<string>();

            // Recommendation 1: Difficulty adjustment
            if (session.PerformanceScore >= 85)
            {
                recommendations.Add("Consider increasing difficulty level - you're performing very well!");
            }
            else if (session.PerformanceScore < 60)
            {
                recommendations.Add("Try reducing difficulty to build confidence and accuracy.");
            }

            // Recommendation 2: Practice focus
            if (session.Accuracy < 70)
            {
                recommendations.Add("Focus on accuracy rather than speed - slow down and be deliberate.");
            }

            if (session.EfficiencyScore < 60)
            {
                recommendations.Add("Practice more efficient strategies - minimize unnecessary moves.");
            }

            // Recommendation 3: Game variety
            var gameTypes = userHistory.Select(s => s.GameType).Distinct().ToList();
            if (gameTypes.Count < 3)
            {
                var missingGames = GetMissingGameTypes(gameTypes);
                recommendations.Add($"Try playing {string.Join(" and ", missingGames)} to work on different cognitive skills.");
            }

            // Recommendation 4: Consistency
            if (userHistory.Count < 5)
            {
                recommendations.Add("Build consistency by playing regularly - aim for daily sessions!");
            }

            return recommendations.Any() ? recommendations : new List<string> { "Keep up the excellent work!" };
        }

        /// <summary>
        /// Generate personalized encouragement message
        /// </summary>
        private async Task<string> GenerateEncouragementAsync(
            GameSession session,
            List<GameSession> userHistory,
            string feedback)
        {
            try
            {
                var progressIndicator = "";
                if (userHistory.Count >= 2)
                {
                    var previousScore = userHistory
                        .Where(s => s.SessionId != session.SessionId && s.Status == "Completed")
                        .OrderByDescending(s => s.TimeStarted)
                        .First()
                        .PerformanceScore;

                    if (session.PerformanceScore > previousScore)
                        progressIndicator = " You're improving!";
                    else if (session.PerformanceScore == previousScore)
                        progressIndicator = " You're maintaining consistency!";
                    else
                        progressIndicator = " Keep trying - progress isn't always linear!";
                }

                var prompt = $@"Generate a single encouraging message for a cognitive rehabilitation user. Be warm, personal, and motivating.

Context:
- Session Score: {session.PerformanceScore}/100
- Game Type: {session.GameType}
- Difficulty: {session.Difficulty}/5
- Total Sessions Completed: {userHistory.Count}
{progressIndicator}

Create ONE sentence that is warm, specific to their effort, and motivating. Maximum 15 words.";

                return await CallLMStudioAsync(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error generating encouragement: {ex.Message}");
                return "You're doing great! Keep up the hard work.";
            }
        }

        /// <summary>
        /// Generate AI insights for profile analysis
        /// </summary>
        private async Task<string> GenerateProfileInsightsAsync(
            UserProfile profile,
            AIProfileAnalysis analysis,
            List<GameSession> allSessions)
        {
            try
            {
                var topGame = GetTopPerformingGame(analysis);
                var improvementText = analysis.OverallMetrics.ImprovementTrend > 0
                    ? $"showing {Math.Abs(analysis.OverallMetrics.ImprovementPercentage):F1}% improvement"
                    : "with recent slight decline";

                var prompt = $@"Analyze this user's cognitive rehabilitation progress and provide a brief insight summary:

Profile: {profile.ProfileName}
Age: {profile.Age}
Condition: {profile.DiagnosedCondition}
Sessions Completed: {analysis.OverallMetrics.TotalSessions}
Average Score: {analysis.OverallMetrics.AverageScore:F1}/100
Best Performance: {analysis.OverallMetrics.BestScore}/100
Accuracy: {analysis.OverallMetrics.AverageAccuracy:F1}%
Top Game: {topGame}
Improvement Trend: {improvementText}

Provide 2-3 sentences of professional insight about their cognitive rehabilitation progress and overall trajectory.";

                return await CallLMStudioAsync(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error generating profile insights: {ex.Message}");
                return "Keep maintaining your practice routine for best results.";
            }
        }

        /// <summary>
        /// Call LM Studio for AI analysis
        /// </summary>
        private async Task<string> CallLMStudioAsync(string prompt)
        {
            try
            {
                var request = new
                {
                    model = MODEL_NAME,
                    messages = new[] {
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.7,
                    max_tokens = 150,
                    stream = false
                };

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(LM_STUDIO_ENDPOINT, jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var jsonDoc = JsonDocument.Parse(responseBody);

                    if (jsonDoc.RootElement.TryGetProperty("choices", out var choices) &&
                        choices.GetArrayLength() > 0 &&
                        choices[0].TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var content))
                    {
                        return content.GetString()?.Trim() ?? "Analysis complete.";
                    }
                }

                _logger.LogWarning($"LM Studio returned status {response.StatusCode}");
                return "Analysis complete.";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calling LM Studio: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Identify cognitive strengths
        /// </summary>
        private List<string> IdentifyStrengths(
            GameTypeMetrics memoryMetrics,
            GameTypeMetrics reactionMetrics,
            GameTypeMetrics sortingMetrics)
        {
            var strengths = new List<string>();
            const int STRENGTH_THRESHOLD = 80;

            if (memoryMetrics != null && memoryMetrics.AverageScore >= STRENGTH_THRESHOLD)
                strengths.Add($"Strong memory skills ({memoryMetrics.AverageScore:F1}/100)");

            if (reactionMetrics != null && reactionMetrics.AverageScore >= STRENGTH_THRESHOLD)
                strengths.Add($"Fast reaction time ({reactionMetrics.AverageScore:F1}/100)");

            if (sortingMetrics != null && sortingMetrics.AverageScore >= STRENGTH_THRESHOLD)
                strengths.Add($"Excellent categorization skills ({sortingMetrics.AverageScore:F1}/100)");

            return strengths.Any() ? strengths : new List<string> { "Building cognitive foundation" };
        }

        /// <summary>
        /// Identify areas for improvement
        /// </summary>
        private List<string> IdentifyWeaknesses(
            GameTypeMetrics memoryMetrics,
            GameTypeMetrics reactionMetrics,
            GameTypeMetrics sortingMetrics)
        {
            var weaknesses = new List<string>();
            const int IMPROVEMENT_THRESHOLD = 70;

            if (memoryMetrics != null && memoryMetrics.AverageScore < IMPROVEMENT_THRESHOLD)
                weaknesses.Add($"Memory recall - focus on pattern recognition");

            if (reactionMetrics != null && reactionMetrics.AverageScore < IMPROVEMENT_THRESHOLD)
                weaknesses.Add($"Reaction speed - practice rapid decision-making");

            if (sortingMetrics != null && sortingMetrics.AverageScore < IMPROVEMENT_THRESHOLD)
                weaknesses.Add($"Categorization - work on logical grouping");

            return weaknesses.Any() ? weaknesses : new List<string> { "All areas performing well!" };
        }

        /// <summary>
        /// Calculate recommended difficulty level
        /// </summary>
        private int CalculateRecommendedDifficulty(AggregatedMetrics metrics, int currentDifficulty)
        {
            if (metrics.AverageScore >= 85 && currentDifficulty < 5)
                return currentDifficulty + 1;

            if (metrics.AverageScore < 60 && currentDifficulty > 1)
                return currentDifficulty - 1;

            return currentDifficulty;
        }

        /// <summary>
        /// Get top performing game
        /// </summary>
        private string GetTopPerformingGame(AIProfileAnalysis analysis)
        {
            var scores = new Dictionary<string, double>();

            if (analysis.MemoryGameMetrics?.SessionCount > 0)
                scores["Memory Match"] = analysis.MemoryGameMetrics.AverageScore;
            if (analysis.ReactionGameMetrics?.SessionCount > 0)
                scores["Reaction Trainer"] = analysis.ReactionGameMetrics.AverageScore;
            if (analysis.SortingGameMetrics?.SessionCount > 0)
                scores["Sorting Task"] = analysis.SortingGameMetrics.AverageScore;

            return scores.Any() ? scores.MaxBy(x => x.Value).Key : "All Games";
        }

        /// <summary>
        /// Get missing game types
        /// </summary>
        private List<string> GetMissingGameTypes(List<string> playedGames)
        {
            var allGames = new[] { "Memory Match", "Reaction Trainer", "Sorting Task" };
            return allGames.Where(g => !playedGames.Contains(g, StringComparer.OrdinalIgnoreCase)).ToList();
        }
    }

    /// <summary>
    /// Container for session analysis results
    /// </summary>
    public class AISessionAnalysis
    {
        public int SessionId { get; set; }
        public DateTime AnalyzedAt { get; set; }
        public string PerformanceSummary { get; set; }
        public string AIFeedback { get; set; }
        public List<string> Recommendations { get; set; }
        public string Encouragement { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Container for profile analysis results
    /// </summary>
    public class AIProfileAnalysis
    {
        public int ProfileId { get; set; }
        public string ProfileName { get; set; }
        public DateTime AnalyzedAt { get; set; }
        public AggregatedMetrics OverallMetrics { get; set; }
        public GameTypeMetrics MemoryGameMetrics { get; set; }
        public GameTypeMetrics ReactionGameMetrics { get; set; }
        public GameTypeMetrics SortingGameMetrics { get; set; }
        public List<string> CognitiveStrengths { get; set; }
        public List<string> AreasForImprovement { get; set; }
        public string AIInsights { get; set; }
        public int RecommendedNextDifficulty { get; set; }
        public double ImprovementVelocity { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}
