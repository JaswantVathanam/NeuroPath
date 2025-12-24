using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AdaptiveCognitiveRehabilitationPlatform.Services;

/// <summary>
/// AI-powered analysis service for cognitive rehabilitation activities.
/// Provides real-time feedback, pattern analysis, and recommendations during activities.
/// Uses Phi-4-mini via LM Studio for intelligent analysis.
/// </summary>
public class ActivityAIAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ActivityAIAnalysisService> _logger;
    private const string AI_ENDPOINT = "http://localhost:1234/v1/chat/completions";
    private const string MODEL_NAME = "Phi-4-mini";

    public ActivityAIAnalysisService(HttpClient httpClient, ILogger<ActivityAIAnalysisService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
    }

    #region Story Recall Analysis

    /// <summary>
    /// Analyze story recall performance and provide AI-powered feedback
    /// </summary>
    public async Task<StoryRecallAIResponse> AnalyzeStoryRecallAsync(StoryRecallAnalysisRequest request)
    {
        try
        {
            _logger.LogInformation($"[ACTIVITY-AI] Analyzing Story Recall for {request.Username}");

            var prompt = $@"You are a cognitive therapist analyzing a patient's story recall performance.

PATIENT: {request.Username}
STORY: ""{request.StoryTitle}""
DIFFICULTY: {request.Difficulty}
QUESTIONS ANSWERED: {request.QuestionsAnswered}/{request.TotalQuestions}
CORRECT ANSWERS: {request.CorrectAnswers}
ACCURACY: {request.Accuracy:F1}%
TIME SPENT READING: {request.ReadingTimeSeconds} seconds
DETAILS RECALLED: {string.Join(", ", request.DetailsRecalled ?? new List<string>())}
MISSED DETAILS: {string.Join(", ", request.MissedDetails ?? new List<string>())}

Analyze their memory recall performance and provide:
1. Overall assessment of their recall ability
2. Specific memory strengths observed
3. Areas needing improvement
4. Recommended story difficulty for next session
5. Memory techniques to suggest
6. Encouraging message

Output as JSON:
{{
    ""overallAssessment"": ""string"",
    ""memoryStrengths"": [""array of strengths""],
    ""areasToImprove"": [""areas needing work""],
    ""recommendedDifficulty"": ""easy/medium/hard"",
    ""memoryTechniques"": [""techniques to try""],
    ""encouragement"": ""warm supportive message"",
    ""recallScore"": 0-100,
    ""nextStoryRecommendation"": ""type of story to try next""
}}";

            var response = await CallAIAsync(prompt);
            return ParseStoryRecallResponse(response, request);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ACTIVITY-AI] Story Recall analysis error: {ex.Message}");
            return GetDefaultStoryRecallResponse(request);
        }
    }

    #endregion

    #region Mental Math Analysis

    /// <summary>
    /// Analyze mental math performance and suggest difficulty adjustments
    /// </summary>
    public async Task<MentalMathAIResponse> AnalyzeMentalMathAsync(MentalMathAnalysisRequest request)
    {
        try
        {
            _logger.LogInformation($"[ACTIVITY-AI] Analyzing Mental Math for {request.Username}");

            var prompt = $@"You are a cognitive therapist analyzing mental math performance.

PATIENT: {request.Username}
PROBLEMS ATTEMPTED: {request.ProblemsAttempted}
CORRECT ANSWERS: {request.CorrectAnswers}
ACCURACY: {request.Accuracy:F1}%
AVERAGE TIME PER PROBLEM: {request.AverageTimeMs:F0}ms
OPERATION TYPES: {request.OperationTypes}
CURRENT DIFFICULTY: {request.Difficulty}
CURRENT STREAK: {request.CurrentStreak}
MAX STREAK: {request.MaxStreak}
ERROR PATTERNS: {string.Join(", ", request.ErrorPatterns ?? new List<string>())}

Analyze their mathematical processing and provide:
1. Cognitive processing speed assessment
2. Pattern recognition in errors
3. Recommended difficulty adjustment
4. Specific operation types to practice
5. Mental calculation strategies

Output as JSON:
{{
    ""processingAssessment"": ""string"",
    ""errorPatternAnalysis"": ""string"",
    ""recommendedDifficulty"": ""easier/same/harder"",
    ""operationsToFocus"": [""operations to practice""],
    ""calculationStrategies"": [""mental math tips""],
    ""encouragement"": ""supportive message"",
    ""cognitiveLoadScore"": 0-100,
    ""suggestedProblemTypes"": [""types to try""]
}}";

            var response = await CallAIAsync(prompt);
            return ParseMentalMathResponse(response, request);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ACTIVITY-AI] Mental Math analysis error: {ex.Message}");
            return GetDefaultMentalMathResponse(request);
        }
    }

    #endregion

    #region Word Association Analysis

    /// <summary>
    /// Analyze word association patterns for semantic memory assessment
    /// </summary>
    public async Task<WordAssociationAIResponse> AnalyzeWordAssociationAsync(WordAssociationAnalysisRequest request)
    {
        try
        {
            _logger.LogInformation($"[ACTIVITY-AI] Analyzing Word Association for {request.Username}");

            var prompt = $@"You are a cognitive therapist analyzing word association performance.

PATIENT: {request.Username}
CHAIN LENGTH: {request.ChainLength}
TOTAL SCORE: {request.Score}
WORDS IN CHAIN: {string.Join(" → ", request.WordChain ?? new List<string>())}
LONGEST WORD: {request.LongestWord}
AVERAGE RESPONSE TIME: {request.AverageResponseTimeMs:F0}ms
INVALID ATTEMPTS: {request.InvalidAttempts}

Analyze their semantic memory and language processing:
1. Word association quality (creative vs common associations)
2. Semantic network strength
3. Language fluency indicators
4. Vocabulary depth assessment
5. Areas for vocabulary building

Output as JSON:
{{
    ""semanticAnalysis"": ""string"",
    ""associationQuality"": ""creative/standard/limited"",
    ""vocabularyAssessment"": ""string"",
    ""semanticStrengths"": [""observed strengths""],
    ""vocabularyBuilding"": [""areas to expand""],
    ""encouragement"": ""supportive message"",
    ""semanticScore"": 0-100,
    ""wordCategoriesToExplore"": [""categories to try""]
}}";

            var response = await CallAIAsync(prompt);
            return ParseWordAssociationResponse(response, request);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ACTIVITY-AI] Word Association analysis error: {ex.Message}");
            return GetDefaultWordAssociationResponse(request);
        }
    }

    #endregion

    #region Focus Tracker Analysis

    /// <summary>
    /// Analyze focus and attention patterns
    /// </summary>
    public async Task<FocusTrackerAIResponse> AnalyzeFocusTrackerAsync(FocusTrackerAnalysisRequest request)
    {
        try
        {
            _logger.LogInformation($"[ACTIVITY-AI] Analyzing Focus Tracker for {request.Username}");

            var prompt = $@"You are a cognitive therapist analyzing visual attention and focus.

PATIENT: {request.Username}
TOTAL ROUNDS: {request.TotalRounds}
CORRECT ROUNDS: {request.CorrectRounds}
ACCURACY: {request.Accuracy:F1}%
MAX LEVEL REACHED: {request.MaxLevel}
TARGETS TO TRACK: {request.TargetCount}
DIFFICULTY: {request.Difficulty}
AVERAGE RESPONSE TIME: {request.AverageResponseTimeMs:F0}ms

Analyze their visual attention and tracking ability:
1. Sustained attention assessment
2. Visual tracking capability
3. Working memory load handling
4. Attention span indicators
5. Recommended attention exercises

Output as JSON:
{{
    ""attentionAssessment"": ""string"",
    ""trackingCapability"": ""excellent/good/developing"",
    ""workingMemoryAnalysis"": ""string"",
    ""attentionStrengths"": [""observed strengths""],
    ""focusImprovements"": [""areas to develop""],
    ""encouragement"": ""supportive message"",
    ""attentionScore"": 0-100,
    ""recommendedExercises"": [""exercises to try""]
}}";

            var response = await CallAIAsync(prompt);
            return ParseFocusTrackerResponse(response, request);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ACTIVITY-AI] Focus Tracker analysis error: {ex.Message}");
            return GetDefaultFocusTrackerResponse(request);
        }
    }

    #endregion

    #region Word Puzzles Analysis

    /// <summary>
    /// Analyze word puzzle solving patterns
    /// </summary>
    public async Task<WordPuzzlesAIResponse> AnalyzeWordPuzzlesAsync(WordPuzzlesAnalysisRequest request)
    {
        try
        {
            _logger.LogInformation($"[ACTIVITY-AI] Analyzing Word Puzzles for {request.Username}");

            var prompt = $@"You are a cognitive therapist analyzing word puzzle performance.

PATIENT: {request.Username}
PUZZLE TYPE: {request.PuzzleType}
PUZZLES SOLVED: {request.PuzzlesSolved}/{request.TotalPuzzles}
ACCURACY: {request.Accuracy:F1}%
HINTS USED: {request.HintsUsed}
WORDS SOLVED: {string.Join(", ", request.SolvedWords ?? new List<string>())}
DIFFICULTY: {request.Difficulty}
AVERAGE TIME PER PUZZLE: {request.AverageTimeSeconds:F1}s

Analyze their language processing and problem-solving:
1. Pattern recognition in letter arrangements
2. Vocabulary utilization
3. Problem-solving approach
4. Hint dependency analysis
5. Recommended puzzle types

Output as JSON:
{{
    ""problemSolvingAnalysis"": ""string"",
    ""patternRecognition"": ""strong/moderate/developing"",
    ""vocabularyUtilization"": ""string"",
    ""solvingStrengths"": [""observed strengths""],
    ""areasToChallenge"": [""areas to push""],
    ""encouragement"": ""supportive message"",
    ""linguisticScore"": 0-100,
    ""recommendedPuzzleTypes"": [""puzzle types to try""]
}}";

            var response = await CallAIAsync(prompt);
            return ParseWordPuzzlesResponse(response, request);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ACTIVITY-AI] Word Puzzles analysis error: {ex.Message}");
            return GetDefaultWordPuzzlesResponse(request);
        }
    }

    #endregion

    #region Number Sequence Analysis

    /// <summary>
    /// Analyze number sequence pattern recognition
    /// </summary>
    public async Task<NumberSequenceAIResponse> AnalyzeNumberSequenceAsync(NumberSequenceAnalysisRequest request)
    {
        try
        {
            _logger.LogInformation($"[ACTIVITY-AI] Analyzing Number Sequence for {request.Username}");

            var prompt = $@"You are a cognitive therapist analyzing number sequence pattern recognition.

PATIENT: {request.Username}
SEQUENCES COMPLETED: {request.SequencesCompleted}/{request.TotalSequences}
ACCURACY: {request.Accuracy:F1}%
CURRENT STREAK: {request.CurrentStreak}
MAX STREAK: {request.MaxStreak}
DIFFICULTY: {request.Difficulty}
SEQUENCE TYPES ATTEMPTED: {request.SequenceTypes}
AVERAGE TIME: {request.AverageTimeSeconds:F1}s

Analyze their logical reasoning and pattern recognition:
1. Abstract reasoning capability
2. Mathematical pattern detection
3. Sequential processing ability
4. Logical thinking assessment
5. Recommended sequence complexity

Output as JSON:
{{
    ""reasoningAssessment"": ""string"",
    ""patternDetection"": ""excellent/good/developing"",
    ""sequentialProcessing"": ""string"",
    ""logicalStrengths"": [""observed strengths""],
    ""challengeAreas"": [""areas to develop""],
    ""encouragement"": ""supportive message"",
    ""reasoningScore"": 0-100,
    ""recommendedSequenceTypes"": [""sequence types to try""]
}}";

            var response = await CallAIAsync(prompt);
            return ParseNumberSequenceResponse(response, request);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ACTIVITY-AI] Number Sequence analysis error: {ex.Message}");
            return GetDefaultNumberSequenceResponse(request);
        }
    }

    #endregion

    #region Breathing Exercise Analysis

    /// <summary>
    /// Analyze breathing exercise completion and provide wellness feedback
    /// </summary>
    public async Task<BreathingAIResponse> AnalyzeBreathingExerciseAsync(BreathingAnalysisRequest request)
    {
        try
        {
            _logger.LogInformation($"[ACTIVITY-AI] Analyzing Breathing Exercise for {request.Username}");

            var prompt = $@"You are a wellness therapist analyzing breathing exercise performance.

PATIENT: {request.Username}
EXERCISE TYPE: {request.ExerciseType}
CYCLES COMPLETED: {request.CyclesCompleted}/{request.TotalCycles}
TOTAL BREATHING TIME: {request.TotalBreathingTimeSeconds} seconds
MOOD BEFORE: {request.MoodBefore}
MOOD AFTER: {request.MoodAfter}
COMPLETED FULLY: {request.CompletedFully}

Analyze their relaxation practice and provide wellness guidance:
1. Breathing pattern consistency
2. Relaxation effectiveness
3. Mood improvement observed
4. Recommended breathing techniques
5. Mindfulness suggestions

Output as JSON:
{{
    ""relaxationAssessment"": ""string"",
    ""breathingConsistency"": ""excellent/good/developing"",
    ""moodImpactAnalysis"": ""string"",
    ""wellnessStrengths"": [""observed benefits""],
    ""mindfulnessTips"": [""suggestions for practice""],
    ""encouragement"": ""calming supportive message"",
    ""relaxationScore"": 0-100,
    ""recommendedTechniques"": [""breathing techniques to try""]
}}";

            var response = await CallAIAsync(prompt);
            return ParseBreathingResponse(response, request);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ACTIVITY-AI] Breathing analysis error: {ex.Message}");
            return GetDefaultBreathingResponse(request);
        }
    }

    #endregion

    #region Daily Journal Analysis

    /// <summary>
    /// Analyze journal entry for emotional insights
    /// </summary>
    public async Task<JournalAIResponse> AnalyzeJournalEntryAsync(JournalAnalysisRequest request)
    {
        try
        {
            _logger.LogInformation($"[ACTIVITY-AI] Analyzing Journal Entry for {request.Username}");

            var prompt = $@"You are a supportive wellness therapist analyzing a journal entry.

PATIENT: {request.Username}
ENTRY TITLE: {request.Title}
WORD COUNT: {request.WordCount}
MOOD SELECTED: {request.Mood}
PROMPT USED: {request.PromptUsed ?? "Free writing"}
TIME SPENT: {request.TimeSpentMinutes} minutes

Note: Do not analyze the actual journal content for privacy. Focus on:
1. Journaling engagement level
2. Emotional expression through mood selection
3. Writing consistency indicators
4. Recommended journaling prompts
5. Emotional wellness suggestions

Output as JSON:
{{
    ""engagementAnalysis"": ""string"",
    ""emotionalExpression"": ""expressive/moderate/reserved"",
    ""writingPatterns"": ""string"",
    ""journalingStrengths"": [""positive observations""],
    ""growthSuggestions"": [""ideas for deeper reflection""],
    ""encouragement"": ""warm supportive message"",
    ""wellnessScore"": 0-100,
    ""recommendedPrompts"": [""journal prompts to try""]
}}";

            var response = await CallAIAsync(prompt);
            return ParseJournalResponse(response, request);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ACTIVITY-AI] Journal analysis error: {ex.Message}");
            return GetDefaultJournalResponse(request);
        }
    }

    #endregion

    #region Real-time Difficulty Recommendation

    /// <summary>
    /// Get AI recommendation for activity difficulty based on recent performance
    /// </summary>
    public async Task<DifficultyRecommendation> GetDifficultyRecommendationAsync(
        string activityType,
        string username,
        List<ActivityPerformanceData> recentPerformance)
    {
        try
        {
            var avgAccuracy = recentPerformance.Any() ? recentPerformance.Average(p => p.Accuracy) : 50;
            var avgScore = recentPerformance.Any() ? recentPerformance.Average(p => p.Score) : 0;
            var trend = CalculatePerformanceTrend(recentPerformance);

            var prompt = $@"You are a cognitive therapist recommending activity difficulty.

ACTIVITY: {activityType}
PATIENT: {username}
RECENT SESSIONS: {recentPerformance.Count}
AVERAGE ACCURACY: {avgAccuracy:F1}%
AVERAGE SCORE: {avgScore:F0}
PERFORMANCE TREND: {trend}
LAST 5 ACCURACIES: {string.Join(", ", recentPerformance.TakeLast(5).Select(p => $"{p.Accuracy:F0}%"))}

Recommend optimal difficulty:
- If accuracy > 85% and improving: increase difficulty
- If accuracy 60-85%: maintain current
- If accuracy < 60% or declining: decrease difficulty

Output as JSON:
{{
    ""recommendedDifficulty"": ""easy/medium/hard"",
    ""confidenceLevel"": 0.0-1.0,
    ""reasoning"": ""explanation"",
    ""adjustmentType"": ""increase/maintain/decrease"",
    ""encouragement"": ""brief supportive note""
}}";

            var response = await CallAIAsync(prompt);
            return ParseDifficultyRecommendation(response, avgAccuracy);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ACTIVITY-AI] Difficulty recommendation error: {ex.Message}");
            return GetDefaultDifficultyRecommendation(recentPerformance);
        }
    }

    /// <summary>
    /// Get AI recommendation for which story to read next
    /// </summary>
    public async Task<StoryRecommendation> GetStoryRecommendationAsync(int userId)
    {
        try
        {
            _logger.LogInformation($"[ACTIVITY-AI] Getting story recommendation for user {userId}");

            // Try to get recent story recall performance
            var prompt = @"You are a cognitive therapist recommending a story for a patient to read.

The patient is starting a Story Recall activity. Based on typical progression:
- New users should start with Easy stories
- After mastering Easy (>80% accuracy), move to Medium
- After mastering Medium (>80% accuracy), move to Hard

Since this is the start of a session, recommend based on general cognitive engagement principles.

Output as JSON:
{
    ""recommendedDifficulty"": ""easy"",
    ""message"": ""A friendly, encouraging recommendation message (2-3 sentences)"",
    ""reasoning"": ""Why this difficulty is recommended""
}";

            var response = await CallAIAsync(prompt);
            return ParseStoryRecommendation(response);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ACTIVITY-AI] Story recommendation error: {ex.Message}");
            return new StoryRecommendation
            {
                RecommendedDifficulty = "easy",
                Message = "Start with an easy story to warm up your memory! You can always try harder ones as you progress.",
                Reasoning = "Default recommendation for new session"
            };
        }
    }

    private StoryRecommendation ParseStoryRecommendation(string json)
    {
        try
        {
            var element = JsonSerializer.Deserialize<JsonElement>(json);
            return new StoryRecommendation
            {
                RecommendedDifficulty = GetJsonString(element, "recommendedDifficulty", "easy"),
                Message = GetJsonString(element, "message", "Start with an easy story to warm up your memory!"),
                Reasoning = GetJsonString(element, "reasoning", "Good starting point")
            };
        }
        catch
        {
            return new StoryRecommendation
            {
                RecommendedDifficulty = "easy",
                Message = "Start with an easy story to warm up your memory!",
                Reasoning = "Default recommendation"
            };
        }
    }

    /// <summary>
    /// Analyze activity performance and provide comprehensive feedback
    /// </summary>
    public async Task<ActivityAnalysisResult> AnalyzeActivityPerformanceAsync(
        AdaptiveCognitiveRehabilitationPlatform.Models.ActivitySession session,
        List<AdaptiveCognitiveRehabilitationPlatform.Models.ActivitySession> previousSessions)
    {
        try
        {
            _logger.LogInformation($"[ACTIVITY-AI] Analyzing {session.ActivityType} performance");

            var durationMinutes = session.DurationSeconds / 60;
            var prompt = $@"You are a cognitive therapist providing feedback on a patient's activity.

ACTIVITY: {session.ActivityType}
SCORE: {session.Score}
ACCURACY: {session.Accuracy:F1}%
DIFFICULTY: {session.Difficulty}
DURATION: {durationMinutes} minutes
SESSION DATA: {session.ActivityData}
PREVIOUS SESSIONS: {previousSessions.Count}

Provide comprehensive analysis:
1. Performance assessment
2. Pattern analysis
3. Encouragement
4. Recommended next difficulty

Output as JSON:
{{
    ""encouragement"": ""warm, supportive message (2-3 sentences)"",
    ""patternAnalysis"": ""observations about their performance patterns"",
    ""recommendedNextDifficulty"": ""easy/medium/hard"",
    ""cognitiveStrengths"": [""list of strengths observed""],
    ""areasForGrowth"": [""areas to work on""]
}}";

            var response = await CallAIAsync(prompt);
            return ParseActivityAnalysisResult(response, session);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ACTIVITY-AI] Activity analysis error: {ex.Message}");
            return GetDefaultActivityAnalysisResult(session);
        }
    }

    private ActivityAnalysisResult ParseActivityAnalysisResult(string json, AdaptiveCognitiveRehabilitationPlatform.Models.ActivitySession session)
    {
        try
        {
            var element = JsonSerializer.Deserialize<JsonElement>(json);
            return new ActivityAnalysisResult
            {
                Encouragement = GetJsonString(element, "encouragement", "Great job completing this activity!"),
                PatternAnalysis = GetJsonString(element, "patternAnalysis", $"You scored {session.Score} with {session.Accuracy:F0}% accuracy."),
                RecommendedNextDifficulty = GetJsonString(element, "recommendedNextDifficulty", session.Difficulty ?? "medium"),
                CognitiveStrengths = GetJsonStringArray(element, "cognitiveStrengths"),
                AreasForGrowth = GetJsonStringArray(element, "areasForGrowth")
            };
        }
        catch
        {
            return GetDefaultActivityAnalysisResult(session);
        }
    }

    private ActivityAnalysisResult GetDefaultActivityAnalysisResult(AdaptiveCognitiveRehabilitationPlatform.Models.ActivitySession session)
    {
        var accuracy = session.Accuracy ?? 0m;
        var encouragement = accuracy >= 70
            ? "Excellent work! Your performance shows great cognitive engagement!"
            : "Good effort! Every practice session strengthens your cognitive abilities.";

        return new ActivityAnalysisResult
        {
            Encouragement = encouragement,
            PatternAnalysis = $"You completed a {session.Difficulty ?? "medium"} level {session.ActivityType} activity with {accuracy:F0}% accuracy.",
            RecommendedNextDifficulty = accuracy >= 80 ? GetNextDifficultyLevel(session.Difficulty ?? "medium") : (session.Difficulty ?? "medium"),
            CognitiveStrengths = new List<string> { "Consistent engagement", "Good effort" },
            AreasForGrowth = new List<string> { "Continue regular practice" }
        };
    }

    private string GetNextDifficultyLevel(string? current)
    {
        return (current?.ToLower()) switch
        {
            "easy" => "medium",
            "medium" => "hard",
            _ => "hard"
        };
    }

    #endregion

    #region Helper Methods

    private async Task<string> CallAIAsync(string prompt)
    {
        var request = new
        {
            model = MODEL_NAME,
            messages = new[]
            {
                new { role = "system", content = "You are a cognitive rehabilitation therapist AI. Provide supportive, encouraging analysis. Output ONLY valid JSON." },
                new { role = "user", content = prompt }
            },
            temperature = 0.3,
            max_tokens = 1500
        };

        var response = await _httpClient.PostAsJsonAsync(AI_ENDPOINT, request);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"AI server returned {response.StatusCode}");
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        return ExtractJsonFromResponse(responseBody);
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
        return (start >= 0 && end > start) ? response.Substring(start, end - start + 1) : "{}";
    }

    private string GetJsonString(JsonElement element, string property, string defaultValue = "")
    {
        try { return element.TryGetProperty(property, out var p) ? p.GetString() ?? defaultValue : defaultValue; }
        catch { return defaultValue; }
    }

    private int GetJsonInt(JsonElement element, string property, int defaultValue = 0)
    {
        try { return element.TryGetProperty(property, out var p) && p.ValueKind == JsonValueKind.Number ? p.GetInt32() : defaultValue; }
        catch { return defaultValue; }
    }

    private List<string> GetJsonStringArray(JsonElement element, string property)
    {
        var result = new List<string>();
        try
        {
            if (element.TryGetProperty(property, out var p) && p.ValueKind == JsonValueKind.Array)
                foreach (var item in p.EnumerateArray())
                    if (item.GetString() is string s) result.Add(s);
        }
        catch { }
        return result.Any() ? result : new List<string> { "Keep practicing!", "You're making progress!" };
    }

    private string CalculatePerformanceTrend(List<ActivityPerformanceData> data)
    {
        if (data.Count < 2) return "stable";
        var recent = data.TakeLast(3).Average(p => p.Accuracy);
        var earlier = data.Take(Math.Max(1, data.Count - 3)).Average(p => p.Accuracy);
        if (recent > earlier + 5) return "improving";
        if (recent < earlier - 5) return "declining";
        return "stable";
    }

    #endregion

    #region Response Parsers

    private StoryRecallAIResponse ParseStoryRecallResponse(string json, StoryRecallAnalysisRequest request)
    {
        try
        {
            var element = JsonSerializer.Deserialize<JsonElement>(json);
            return new StoryRecallAIResponse
            {
                OverallAssessment = GetJsonString(element, "overallAssessment", "Good effort on story recall!"),
                MemoryStrengths = GetJsonStringArray(element, "memoryStrengths"),
                AreasToImprove = GetJsonStringArray(element, "areasToImprove"),
                RecommendedDifficulty = GetJsonString(element, "recommendedDifficulty", request.Difficulty),
                MemoryTechniques = GetJsonStringArray(element, "memoryTechniques"),
                Encouragement = GetJsonString(element, "encouragement", "Great job working on your memory! 🌟"),
                RecallScore = GetJsonInt(element, "recallScore", (int)request.Accuracy),
                NextStoryRecommendation = GetJsonString(element, "nextStoryRecommendation", "Try a similar difficulty story")
            };
        }
        catch { return GetDefaultStoryRecallResponse(request); }
    }

    private MentalMathAIResponse ParseMentalMathResponse(string json, MentalMathAnalysisRequest request)
    {
        try
        {
            var element = JsonSerializer.Deserialize<JsonElement>(json);
            return new MentalMathAIResponse
            {
                ProcessingAssessment = GetJsonString(element, "processingAssessment", "Good mental calculation ability!"),
                ErrorPatternAnalysis = GetJsonString(element, "errorPatternAnalysis", "Keep practicing for improvement"),
                RecommendedDifficulty = GetJsonString(element, "recommendedDifficulty", "same"),
                OperationsToFocus = GetJsonStringArray(element, "operationsToFocus"),
                CalculationStrategies = GetJsonStringArray(element, "calculationStrategies"),
                Encouragement = GetJsonString(element, "encouragement", "Your math skills are improving! 🧮"),
                CognitiveLoadScore = GetJsonInt(element, "cognitiveLoadScore", (int)request.Accuracy),
                SuggestedProblemTypes = GetJsonStringArray(element, "suggestedProblemTypes")
            };
        }
        catch { return GetDefaultMentalMathResponse(request); }
    }

    private WordAssociationAIResponse ParseWordAssociationResponse(string json, WordAssociationAnalysisRequest request)
    {
        try
        {
            var element = JsonSerializer.Deserialize<JsonElement>(json);
            return new WordAssociationAIResponse
            {
                SemanticAnalysis = GetJsonString(element, "semanticAnalysis", "Good word connections!"),
                AssociationQuality = GetJsonString(element, "associationQuality", "standard"),
                VocabularyAssessment = GetJsonString(element, "vocabularyAssessment", "Solid vocabulary usage"),
                SemanticStrengths = GetJsonStringArray(element, "semanticStrengths"),
                VocabularyBuilding = GetJsonStringArray(element, "vocabularyBuilding"),
                Encouragement = GetJsonString(element, "encouragement", "Great word associations! 📚"),
                SemanticScore = GetJsonInt(element, "semanticScore", Math.Min(100, request.Score)),
                WordCategoriesToExplore = GetJsonStringArray(element, "wordCategoriesToExplore")
            };
        }
        catch { return GetDefaultWordAssociationResponse(request); }
    }

    private FocusTrackerAIResponse ParseFocusTrackerResponse(string json, FocusTrackerAnalysisRequest request)
    {
        try
        {
            var element = JsonSerializer.Deserialize<JsonElement>(json);
            return new FocusTrackerAIResponse
            {
                AttentionAssessment = GetJsonString(element, "attentionAssessment", "Good focus maintained!"),
                TrackingCapability = GetJsonString(element, "trackingCapability", "good"),
                WorkingMemoryAnalysis = GetJsonString(element, "workingMemoryAnalysis", "Solid working memory"),
                AttentionStrengths = GetJsonStringArray(element, "attentionStrengths"),
                FocusImprovements = GetJsonStringArray(element, "focusImprovements"),
                Encouragement = GetJsonString(element, "encouragement", "Your focus is getting sharper! 👁️"),
                AttentionScore = GetJsonInt(element, "attentionScore", (int)request.Accuracy),
                RecommendedExercises = GetJsonStringArray(element, "recommendedExercises")
            };
        }
        catch { return GetDefaultFocusTrackerResponse(request); }
    }

    private WordPuzzlesAIResponse ParseWordPuzzlesResponse(string json, WordPuzzlesAnalysisRequest request)
    {
        try
        {
            var element = JsonSerializer.Deserialize<JsonElement>(json);
            return new WordPuzzlesAIResponse
            {
                ProblemSolvingAnalysis = GetJsonString(element, "problemSolvingAnalysis", "Good puzzle solving!"),
                PatternRecognition = GetJsonString(element, "patternRecognition", "moderate"),
                VocabularyUtilization = GetJsonString(element, "vocabularyUtilization", "Good word knowledge"),
                SolvingStrengths = GetJsonStringArray(element, "solvingStrengths"),
                AreasToChallenge = GetJsonStringArray(element, "areasToChallenge"),
                Encouragement = GetJsonString(element, "encouragement", "Great puzzle solving skills! 🧩"),
                LinguisticScore = GetJsonInt(element, "linguisticScore", (int)request.Accuracy),
                RecommendedPuzzleTypes = GetJsonStringArray(element, "recommendedPuzzleTypes")
            };
        }
        catch { return GetDefaultWordPuzzlesResponse(request); }
    }

    private NumberSequenceAIResponse ParseNumberSequenceResponse(string json, NumberSequenceAnalysisRequest request)
    {
        try
        {
            var element = JsonSerializer.Deserialize<JsonElement>(json);
            return new NumberSequenceAIResponse
            {
                ReasoningAssessment = GetJsonString(element, "reasoningAssessment", "Good pattern recognition!"),
                PatternDetection = GetJsonString(element, "patternDetection", "good"),
                SequentialProcessing = GetJsonString(element, "sequentialProcessing", "Solid sequential thinking"),
                LogicalStrengths = GetJsonStringArray(element, "logicalStrengths"),
                ChallengeAreas = GetJsonStringArray(element, "challengeAreas"),
                Encouragement = GetJsonString(element, "encouragement", "Your pattern skills are growing! 🔢"),
                ReasoningScore = GetJsonInt(element, "reasoningScore", (int)request.Accuracy),
                RecommendedSequenceTypes = GetJsonStringArray(element, "recommendedSequenceTypes")
            };
        }
        catch { return GetDefaultNumberSequenceResponse(request); }
    }

    private BreathingAIResponse ParseBreathingResponse(string json, BreathingAnalysisRequest request)
    {
        try
        {
            var element = JsonSerializer.Deserialize<JsonElement>(json);
            return new BreathingAIResponse
            {
                RelaxationAssessment = GetJsonString(element, "relaxationAssessment", "Good relaxation practice!"),
                BreathingConsistency = GetJsonString(element, "breathingConsistency", "good"),
                MoodImpactAnalysis = GetJsonString(element, "moodImpactAnalysis", "Positive mood shift observed"),
                WellnessStrengths = GetJsonStringArray(element, "wellnessStrengths"),
                MindfulnessTips = GetJsonStringArray(element, "mindfulnessTips"),
                Encouragement = GetJsonString(element, "encouragement", "Beautiful breathing practice! 🌬️"),
                RelaxationScore = GetJsonInt(element, "relaxationScore", request.CompletedFully ? 90 : 70),
                RecommendedTechniques = GetJsonStringArray(element, "recommendedTechniques")
            };
        }
        catch { return GetDefaultBreathingResponse(request); }
    }

    private JournalAIResponse ParseJournalResponse(string json, JournalAnalysisRequest request)
    {
        try
        {
            var element = JsonSerializer.Deserialize<JsonElement>(json);
            return new JournalAIResponse
            {
                EngagementAnalysis = GetJsonString(element, "engagementAnalysis", "Good journaling engagement!"),
                EmotionalExpression = GetJsonString(element, "emotionalExpression", "moderate"),
                WritingPatterns = GetJsonString(element, "writingPatterns", "Consistent writing practice"),
                JournalingStrengths = GetJsonStringArray(element, "journalingStrengths"),
                GrowthSuggestions = GetJsonStringArray(element, "growthSuggestions"),
                Encouragement = GetJsonString(element, "encouragement", "Thank you for sharing your thoughts! 📝"),
                WellnessScore = GetJsonInt(element, "wellnessScore", 80),
                RecommendedPrompts = GetJsonStringArray(element, "recommendedPrompts")
            };
        }
        catch { return GetDefaultJournalResponse(request); }
    }

    private DifficultyRecommendation ParseDifficultyRecommendation(string json, double avgAccuracy)
    {
        try
        {
            var element = JsonSerializer.Deserialize<JsonElement>(json);
            return new DifficultyRecommendation
            {
                RecommendedDifficulty = GetJsonString(element, "recommendedDifficulty", avgAccuracy > 80 ? "hard" : avgAccuracy > 50 ? "medium" : "easy"),
                ConfidenceLevel = avgAccuracy > 70 ? 0.85 : 0.7,
                Reasoning = GetJsonString(element, "reasoning", "Based on recent performance"),
                AdjustmentType = GetJsonString(element, "adjustmentType", "maintain"),
                Encouragement = GetJsonString(element, "encouragement", "Keep up the great work!")
            };
        }
        catch { return GetDefaultDifficultyRecommendation(new List<ActivityPerformanceData>()); }
    }

    #endregion

    #region Default Responses

    private StoryRecallAIResponse GetDefaultStoryRecallResponse(StoryRecallAnalysisRequest request) => new()
    {
        OverallAssessment = "Good effort on story recall! Keep practicing to strengthen your memory.",
        MemoryStrengths = new List<string> { "Engaging with stories", "Answering comprehension questions" },
        AreasToImprove = new List<string> { "Focus on key details", "Visualize the story as you read" },
        RecommendedDifficulty = request.Accuracy > 80 ? "hard" : request.Accuracy > 50 ? "medium" : "easy",
        MemoryTechniques = new List<string> { "Create mental images", "Connect details to personal experiences" },
        Encouragement = "Every story you read strengthens your memory! Keep exploring! 📖",
        RecallScore = (int)request.Accuracy,
        NextStoryRecommendation = "Try a story with similar complexity"
    };

    private MentalMathAIResponse GetDefaultMentalMathResponse(MentalMathAnalysisRequest request) => new()
    {
        ProcessingAssessment = "Good mental math practice! Your calculation skills are developing.",
        ErrorPatternAnalysis = "Focus on accuracy over speed initially",
        RecommendedDifficulty = request.Accuracy > 80 ? "harder" : request.Accuracy > 50 ? "same" : "easier",
        OperationsToFocus = new List<string> { "Addition", "Subtraction" },
        CalculationStrategies = new List<string> { "Break numbers into parts", "Use mental number lines" },
        Encouragement = "Your mental math is getting stronger with every problem! 🧮",
        CognitiveLoadScore = (int)request.Accuracy,
        SuggestedProblemTypes = new List<string> { "Simple addition", "Round number practice" }
    };

    private WordAssociationAIResponse GetDefaultWordAssociationResponse(WordAssociationAnalysisRequest request) => new()
    {
        SemanticAnalysis = "Good word connections! Your vocabulary network is active.",
        AssociationQuality = "standard",
        VocabularyAssessment = "Solid vocabulary foundation",
        SemanticStrengths = new List<string> { "Quick word recall", "Logical connections" },
        VocabularyBuilding = new List<string> { "Explore new word categories", "Read diverse materials" },
        Encouragement = "Your word power is growing! Keep making connections! 📚",
        SemanticScore = Math.Min(100, request.Score),
        WordCategoriesToExplore = new List<string> { "Nature words", "Action words", "Descriptive words" }
    };

    private FocusTrackerAIResponse GetDefaultFocusTrackerResponse(FocusTrackerAnalysisRequest request) => new()
    {
        AttentionAssessment = "Good focus practice! Your attention skills are developing.",
        TrackingCapability = request.Accuracy > 70 ? "good" : "developing",
        WorkingMemoryAnalysis = "Working memory engaged effectively",
        AttentionStrengths = new List<string> { "Visual tracking", "Sustained focus" },
        FocusImprovements = new List<string> { "Practice with more targets", "Increase tracking duration" },
        Encouragement = "Your focus is getting sharper every session! 👁️",
        AttentionScore = (int)request.Accuracy,
        RecommendedExercises = new List<string> { "Gradual difficulty increase", "Longer focus sessions" }
    };

    private WordPuzzlesAIResponse GetDefaultWordPuzzlesResponse(WordPuzzlesAnalysisRequest request) => new()
    {
        ProblemSolvingAnalysis = "Good puzzle solving approach! Keep challenging yourself.",
        PatternRecognition = request.Accuracy > 70 ? "strong" : "developing",
        VocabularyUtilization = "Good word knowledge demonstrated",
        SolvingStrengths = new List<string> { "Persistence", "Pattern recognition" },
        AreasToChallenge = new List<string> { "Try harder puzzles", "Reduce hint usage" },
        Encouragement = "Every puzzle solved makes you sharper! 🧩",
        LinguisticScore = (int)request.Accuracy,
        RecommendedPuzzleTypes = new List<string> { "Anagrams", "Word searches" }
    };

    private NumberSequenceAIResponse GetDefaultNumberSequenceResponse(NumberSequenceAnalysisRequest request) => new()
    {
        ReasoningAssessment = "Good pattern recognition! Your logical thinking is developing.",
        PatternDetection = request.Accuracy > 70 ? "good" : "developing",
        SequentialProcessing = "Sequential thinking engaged well",
        LogicalStrengths = new List<string> { "Number pattern awareness", "Logical thinking" },
        ChallengeAreas = new List<string> { "Complex sequences", "Faster recognition" },
        Encouragement = "Your pattern skills grow with each sequence! 🔢",
        ReasoningScore = (int)request.Accuracy,
        RecommendedSequenceTypes = new List<string> { "Arithmetic sequences", "Simple patterns" }
    };

    private BreathingAIResponse GetDefaultBreathingResponse(BreathingAnalysisRequest request) => new()
    {
        RelaxationAssessment = "Wonderful breathing practice! You're nurturing your calm.",
        BreathingConsistency = request.CompletedFully ? "excellent" : "good",
        MoodImpactAnalysis = "Breathing exercises help regulate emotions",
        WellnessStrengths = new List<string> { "Commitment to relaxation", "Mindful breathing" },
        MindfulnessTips = new List<string> { "Practice at the same time daily", "Create a calm environment" },
        Encouragement = "Each breath brings more peace. Beautiful practice! 🌬️",
        RelaxationScore = request.CompletedFully ? 90 : 75,
        RecommendedTechniques = new List<string> { "4-7-8 breathing", "Box breathing" }
    };

    private JournalAIResponse GetDefaultJournalResponse(JournalAnalysisRequest request) => new()
    {
        EngagementAnalysis = "Great journaling session! Writing helps process thoughts.",
        EmotionalExpression = "expressive",
        WritingPatterns = "Consistent reflective practice",
        JournalingStrengths = new List<string> { "Regular writing", "Honest reflection" },
        GrowthSuggestions = new List<string> { "Try gratitude entries", "Explore future goals" },
        Encouragement = "Thank you for taking time to reflect. Your thoughts matter! 📝",
        WellnessScore = 85,
        RecommendedPrompts = new List<string> { "What made you smile today?", "What are you grateful for?" }
    };

    private DifficultyRecommendation GetDefaultDifficultyRecommendation(List<ActivityPerformanceData> data) => new()
    {
        RecommendedDifficulty = "medium",
        ConfidenceLevel = 0.7,
        Reasoning = "Based on general performance patterns",
        AdjustmentType = "maintain",
        Encouragement = "Keep practicing at your current level!"
    };

    #endregion
}

#region Request Models

public class StoryRecallAnalysisRequest
{
    public string Username { get; set; } = "";
    public string StoryTitle { get; set; } = "";
    public string Difficulty { get; set; } = "medium";
    public int QuestionsAnswered { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public double Accuracy { get; set; }
    public int ReadingTimeSeconds { get; set; }
    public List<string>? DetailsRecalled { get; set; }
    public List<string>? MissedDetails { get; set; }
}

public class MentalMathAnalysisRequest
{
    public string Username { get; set; } = "";
    public int ProblemsAttempted { get; set; }
    public int CorrectAnswers { get; set; }
    public double Accuracy { get; set; }
    public double AverageTimeMs { get; set; }
    public string OperationTypes { get; set; } = "";
    public string Difficulty { get; set; } = "medium";
    public int CurrentStreak { get; set; }
    public int MaxStreak { get; set; }
    public List<string>? ErrorPatterns { get; set; }
}

public class WordAssociationAnalysisRequest
{
    public string Username { get; set; } = "";
    public int ChainLength { get; set; }
    public int Score { get; set; }
    public List<string>? WordChain { get; set; }
    public string LongestWord { get; set; } = "";
    public double AverageResponseTimeMs { get; set; }
    public int InvalidAttempts { get; set; }
}

public class FocusTrackerAnalysisRequest
{
    public string Username { get; set; } = "";
    public int TotalRounds { get; set; }
    public int CorrectRounds { get; set; }
    public double Accuracy { get; set; }
    public int MaxLevel { get; set; }
    public int TargetCount { get; set; }
    public string Difficulty { get; set; } = "medium";
    public double AverageResponseTimeMs { get; set; }
}

public class WordPuzzlesAnalysisRequest
{
    public string Username { get; set; } = "";
    public string PuzzleType { get; set; } = "";
    public int PuzzlesSolved { get; set; }
    public int TotalPuzzles { get; set; }
    public double Accuracy { get; set; }
    public int HintsUsed { get; set; }
    public List<string>? SolvedWords { get; set; }
    public string Difficulty { get; set; } = "medium";
    public double AverageTimeSeconds { get; set; }
}

public class NumberSequenceAnalysisRequest
{
    public string Username { get; set; } = "";
    public int SequencesCompleted { get; set; }
    public int TotalSequences { get; set; }
    public double Accuracy { get; set; }
    public int CurrentStreak { get; set; }
    public int MaxStreak { get; set; }
    public string Difficulty { get; set; } = "medium";
    public string SequenceTypes { get; set; } = "";
    public double AverageTimeSeconds { get; set; }
}

public class BreathingAnalysisRequest
{
    public string Username { get; set; } = "";
    public string ExerciseType { get; set; } = "";
    public int CyclesCompleted { get; set; }
    public int TotalCycles { get; set; }
    public int TotalBreathingTimeSeconds { get; set; }
    public string? MoodBefore { get; set; }
    public string? MoodAfter { get; set; }
    public bool CompletedFully { get; set; }
}

public class JournalAnalysisRequest
{
    public string Username { get; set; } = "";
    public string Title { get; set; } = "";
    public int WordCount { get; set; }
    public string Mood { get; set; } = "";
    public string? PromptUsed { get; set; }
    public int TimeSpentMinutes { get; set; }
}

public class ActivityPerformanceData
{
    public string ActivityType { get; set; } = "";
    public double Score { get; set; }
    public double Accuracy { get; set; }
    public DateTime CompletedAt { get; set; }
}

#endregion

#region Response Models

public class StoryRecallAIResponse
{
    public string OverallAssessment { get; set; } = "";
    public List<string> MemoryStrengths { get; set; } = new();
    public List<string> AreasToImprove { get; set; } = new();
    public string RecommendedDifficulty { get; set; } = "medium";
    public List<string> MemoryTechniques { get; set; } = new();
    public string Encouragement { get; set; } = "";
    public int RecallScore { get; set; }
    public string NextStoryRecommendation { get; set; } = "";
}

public class MentalMathAIResponse
{
    public string ProcessingAssessment { get; set; } = "";
    public string ErrorPatternAnalysis { get; set; } = "";
    public string RecommendedDifficulty { get; set; } = "same";
    public List<string> OperationsToFocus { get; set; } = new();
    public List<string> CalculationStrategies { get; set; } = new();
    public string Encouragement { get; set; } = "";
    public int CognitiveLoadScore { get; set; }
    public List<string> SuggestedProblemTypes { get; set; } = new();
}

public class WordAssociationAIResponse
{
    public string SemanticAnalysis { get; set; } = "";
    public string AssociationQuality { get; set; } = "";
    public string VocabularyAssessment { get; set; } = "";
    public List<string> SemanticStrengths { get; set; } = new();
    public List<string> VocabularyBuilding { get; set; } = new();
    public string Encouragement { get; set; } = "";
    public int SemanticScore { get; set; }
    public List<string> WordCategoriesToExplore { get; set; } = new();
}

public class FocusTrackerAIResponse
{
    public string AttentionAssessment { get; set; } = "";
    public string TrackingCapability { get; set; } = "";
    public string WorkingMemoryAnalysis { get; set; } = "";
    public List<string> AttentionStrengths { get; set; } = new();
    public List<string> FocusImprovements { get; set; } = new();
    public string Encouragement { get; set; } = "";
    public int AttentionScore { get; set; }
    public List<string> RecommendedExercises { get; set; } = new();
}

public class WordPuzzlesAIResponse
{
    public string ProblemSolvingAnalysis { get; set; } = "";
    public string PatternRecognition { get; set; } = "";
    public string VocabularyUtilization { get; set; } = "";
    public List<string> SolvingStrengths { get; set; } = new();
    public List<string> AreasToChallenge { get; set; } = new();
    public string Encouragement { get; set; } = "";
    public int LinguisticScore { get; set; }
    public List<string> RecommendedPuzzleTypes { get; set; } = new();
}

public class NumberSequenceAIResponse
{
    public string ReasoningAssessment { get; set; } = "";
    public string PatternDetection { get; set; } = "";
    public string SequentialProcessing { get; set; } = "";
    public List<string> LogicalStrengths { get; set; } = new();
    public List<string> ChallengeAreas { get; set; } = new();
    public string Encouragement { get; set; } = "";
    public int ReasoningScore { get; set; }
    public List<string> RecommendedSequenceTypes { get; set; } = new();
}

public class BreathingAIResponse
{
    public string RelaxationAssessment { get; set; } = "";
    public string BreathingConsistency { get; set; } = "";
    public string MoodImpactAnalysis { get; set; } = "";
    public List<string> WellnessStrengths { get; set; } = new();
    public List<string> MindfulnessTips { get; set; } = new();
    public string Encouragement { get; set; } = "";
    public int RelaxationScore { get; set; }
    public List<string> RecommendedTechniques { get; set; } = new();
}

public class JournalAIResponse
{
    public string EngagementAnalysis { get; set; } = "";
    public string EmotionalExpression { get; set; } = "";
    public string WritingPatterns { get; set; } = "";
    public List<string> JournalingStrengths { get; set; } = new();
    public List<string> GrowthSuggestions { get; set; } = new();
    public string Encouragement { get; set; } = "";
    public int WellnessScore { get; set; }
    public List<string> RecommendedPrompts { get; set; } = new();
}

public class DifficultyRecommendation
{
    public string RecommendedDifficulty { get; set; } = "medium";
    public double ConfidenceLevel { get; set; }
    public string Reasoning { get; set; } = "";
    public string AdjustmentType { get; set; } = "maintain";
    public string Encouragement { get; set; } = "";
}

public class StoryRecommendation
{
    public string RecommendedDifficulty { get; set; } = "easy";
    public string Message { get; set; } = "";
    public string Reasoning { get; set; } = "";
}

public class ActivityAnalysisResult
{
    public string Encouragement { get; set; } = "";
    public string PatternAnalysis { get; set; } = "";
    public string RecommendedNextDifficulty { get; set; } = "medium";
    public List<string> CognitiveStrengths { get; set; } = new();
    public List<string> AreasForGrowth { get; set; } = new();
}

#endregion
