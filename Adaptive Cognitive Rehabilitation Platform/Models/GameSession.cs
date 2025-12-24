using System;
using System.Collections.Generic;

namespace NeuroPath.Models
{
    /// <summary>
    /// Represents a single game session - tracks all performance data
    /// </summary>
    public class GameSession
    {
        public int SessionId { get; set; }
        public int UserId { get; set; }
        public int? ProfileId { get; set; }
        public int? TrainerId { get; set; } // Who assigned this game (if any)
        public int? GameId { get; set; } // NEW: Reference to Game catalog (for parent reports)
        
        // Game Identification
        public string? GameType { get; set; } // "MemoryMatch", "ReactionTrainer", "SortingTask"
        public string? GameMode { get; set; } // "Practice", "Test", "Therapy"
        
        // Game Configuration
        public string? GridSize { get; set; } // "3x4", "4x4", etc.
        public int Difficulty { get; set; } // 1-5
        public string? VisualComplexity { get; set; } // "Simple", "Animals", "Patterns", "Geometric"
        public int? AnimationSpeedMs { get; set; } // Milliseconds for card flip speed
        
        // Game Performance Data
        public int TotalPairs { get; set; }
        public int CorrectMatches { get; set; }
        public int TotalMoves { get; set; }
        public decimal Accuracy { get; set; } // Percentage 0-100
        public int PerformanceScore { get; set; } // AI-calculated score 0-100
        public decimal EfficiencyScore { get; set; } // Optimal moves vs actual
        
        // Timing Data
        public DateTime TimeStarted { get; set; }
        public DateTime? TimeCompleted { get; set; }
        public int TotalSeconds { get; set; } // Duration in seconds
        public int? TimeLimit { get; set; } // For test mode
        
        // AI Analysis & Recommendations
        public string? AiAnalysis { get; set; } // JSON string with AI feedback
        public string? AiRecommendation { get; set; } // JSON string with next grid config
        public string? AiEncouragement { get; set; } // Personalized encouragement message
        
        // Session Status
        public string? Status { get; set; } // "InProgress", "Completed", "Paused", "Abandoned"
        public bool IsTestMode { get; set; } // True if formal assessment
        public int? NextDifficultyLevel { get; set; } // AI recommendation
        
        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
        
        // Navigation Properties
        public User? User { get; set; }
    }

    /// <summary>
    /// Helper class for streaks calculation
    /// </summary>
    public class UserStreak
    {
        public int UserId { get; set; }
        public int CurrentStreak { get; set; }
        public DateTime LastSessionDate { get; set; }
        public int LongestStreak { get; set; }
    }
}
