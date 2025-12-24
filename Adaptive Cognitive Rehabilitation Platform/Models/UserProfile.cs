using System;
using System.Collections.Generic;

namespace NeuroPath.Models
{
    /// <summary>
    /// Represents a user profile - additional details for client users
    /// Users can have multiple profiles (e.g., "Emma's Profile", "Home Practice")
    /// </summary>
    public class UserProfile
    {
        public int ProfileId { get; set; }
        public int UserId { get; set; }
        
        // Profile Details
        public string ProfileName { get; set; } // "Emma", "My Profile", "Home", etc.
        public string AvatarUrl { get; set; }
        public string FavoriteColor { get; set; }
        
        // Cognitive Profile
        public int Age { get; set; }
        public string CognitiveLevel { get; set; } // "Mild", "Moderate", "Severe"
        public string DiagnosedCondition { get; set; } // "ADHD", "Memory Loss", "Stroke Recovery", etc.
        
        // Therapy Details
        public string TherapyGoal { get; set; } // "Improve working memory", "Motor skill recovery", etc.
        public DateTime TherapyStartDate { get; set; }
        public int? AssignedTrainerId { get; set; }
        
        // Game Preferences
        public string PreferredGameMode { get; set; } // "Practice", "Test", "Therapy"
        public int CurrentDifficultyLevel { get; set; } = 1; // 1-5
        public string PreferredVisualStyle { get; set; } // "Colorful", "Minimal", "Dark"
        
        // Progress Tracking
        public int TotalSessionsCompleted { get; set; } = 0;
        public decimal AverageAccuracy { get; set; } = 0;
        public DateTime LastSessionDate { get; set; }
        public DateTime? ProfileUnlockedDate { get; set; } // When advanced features unlocked
        
        // Status
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        public User User { get; set; }
        public User AssignedTrainer { get; set; }
        public ICollection<GameSession> GameSessions { get; set; } = new List<GameSession>();
    }
}
