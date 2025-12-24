using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace AdaptiveCognitiveRehabilitationPlatform.Services;

/// <summary>
/// Model for Game Session Result
/// </summary>
public class GameSessionResult
{
    public string GameType { get; set; } = "";
    public int DifficultyLevel { get; set; }
    public int TotalMoves { get; set; }
    public int TotalMatches { get; set; }
    public int CorrectMatches { get; set; }
    public int ErrorCount { get; set; }
    public int ElapsedSeconds { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    
    // Computed statistics
    public double AccuracyPercentage { get; set; }
    public double SpeedScore { get; set; } // matches per second
    public double ConsistencyScore { get; set; }
    public double OverallScore { get; set; }
}

/// <summary>
/// Service for calculating game statistics
/// </summary>
public class GameStatisticsService
{
    /// <summary>
    /// Calculate comprehensive statistics for a game session
    /// </summary>
    public GameSessionResult CalculateStatistics(
        string gameType,
        int difficultyLevel,
        int totalMoves,
        int totalMatches,
        int correctMatches,
        int errorCount,
        int elapsedSeconds,
        DateTime startTime,
        DateTime endTime)
    {
        var result = new GameSessionResult
        {
            GameType = gameType,
            DifficultyLevel = difficultyLevel,
            TotalMoves = totalMoves,
            TotalMatches = totalMatches,
            CorrectMatches = correctMatches,
            ErrorCount = errorCount,
            ElapsedSeconds = elapsedSeconds,
            StartTime = startTime,
            EndTime = endTime
        };

        // Calculate accuracy
        result.AccuracyPercentage = totalMatches > 0 
            ? (double)correctMatches / totalMatches * 100 
            : 0;

        // Calculate speed (matches per second)
        result.SpeedScore = elapsedSeconds > 0 
            ? (double)correctMatches / elapsedSeconds * 100
            : 0;

        // Calculate consistency (lower error rate = higher consistency)
        double errorRate = totalMoves > 0 ? (double)errorCount / totalMoves : 0;
        result.ConsistencyScore = Math.Max(0, 100 - (errorRate * 100));

        // Calculate overall score (weighted average)
        // 50% accuracy, 30% speed, 20% consistency
        result.OverallScore = 
            (result.AccuracyPercentage * 0.5) +
            (Math.Min(result.SpeedScore, 100) * 0.3) +
            (result.ConsistencyScore * 0.2);

        return result;
    }

    /// <summary>
    /// Get performance level description
    /// </summary>
    public string GetPerformanceLevel(double score)
    {
        return score switch
        {
            >= 90 => "Excellent! 🌟",
            >= 80 => "Very Good! ⭐",
            >= 70 => "Good! 👍",
            >= 60 => "Keep Going! 💪",
            _ => "Practice More! 📚"
        };
    }

    /// <summary>
    /// Get improvement suggestions
    /// </summary>
    public List<string> GetImprovementSuggestions(GameSessionResult result)
    {
        var suggestions = new List<string>();

        if (result.AccuracyPercentage < 70)
            suggestions.Add("📍 Try to focus more on card positions to improve accuracy");

        if (result.SpeedScore < 50)
            suggestions.Add("⚡ Work on recognizing patterns faster");

        if (result.ConsistencyScore < 70)
            suggestions.Add("🎯 Take your time and stay focused - avoid rushing");

        if (result.ErrorCount > result.TotalMatches * 0.3)
            suggestions.Add("🔍 Pay closer attention to details before making moves");

        if (suggestions.Count == 0)
            suggestions.Add("🚀 Fantastic performance! Ready for the next challenge?");

        return suggestions;
    }

    /// <summary>
    /// Determine recommended difficulty adjustment
    /// </summary>
    public (int recommendedLevel, string reason) GetDifficultyRecommendation(
        GameSessionResult result,
        int currentLevel,
        int maxLevel = 3)
    {
        double score = result.OverallScore;

        if (score >= 85 && currentLevel < maxLevel)
        {
            return (currentLevel + 1, "You're doing great! Ready for more challenge?");
        }
        else if (score < 60 && currentLevel > 1)
        {
            return (currentLevel - 1, "Let's practice at an easier level first.");
        }
        else
        {
            return (currentLevel, "Great job! You're at the right level.");
        }
    }
}
