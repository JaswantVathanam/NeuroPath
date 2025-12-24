using System;
using System.Collections.Generic;

namespace AdaptiveCognitiveRehabilitationPlatform.Models
{
    /// <summary>
    /// Represents a patient/user in the cognitive rehabilitation platform
    /// </summary>
    public class Patient
    {
        public int Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        
        // Health information
        public string? ConditionType { get; set; } // e.g., "Stroke Recovery", "MCI", "TBI", "Depression", "Anxiety"
        public string? ConditionDescription { get; set; }
        public DateTime? ConditionDiagnosisDate { get; set; }
        
        // Account
        public string? PasswordHash { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? LastLoginDate { get; set; }
        
        // Accessibility preferences
        public string TextSize { get; set; } = "Normal"; // Normal, Large, ExtraLarge
        public bool HighContrastMode { get; set; } = false;
        public bool ReduceAnimations { get; set; } = false;
        public bool ScreenReaderOptimized { get; set; } = false;
        
        // Assigned caretakers
        public List<CaretakerAssignment> CaretakerAssignments { get; set; } = new();
        
        // Game sessions
        public List<GameSession> GameSessions { get; set; } = new();
    }

    /// <summary>
    /// Represents a caretaker/therapist who monitors patients
    /// </summary>
    public class Caretaker
    {
        public int Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Specialization { get; set; } // e.g., "Occupational Therapist", "Speech Therapist"
        public string? LicenseNumber { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        
        // Account
        public string? PasswordHash { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? LastLoginDate { get; set; }
        
        // Organization
        public string? Organization { get; set; }
        
        // Assigned patients
        public List<CaretakerAssignment> PatientAssignments { get; set; } = new();
        
        // Notifications
        public List<Notification> Notifications { get; set; } = new();
    }

    /// <summary>
    /// Links patients and caretakers
    /// </summary>
    public class CaretakerAssignment
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public Patient? Patient { get; set; }
        public int CaretakerId { get; set; }
        public Caretaker? Caretaker { get; set; }
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Represents a game session/game play
    /// </summary>
    public class GameSession
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public Patient? Patient { get; set; }
        public string? GameName { get; set; } // Memory Match, Reaction Trainer, Sorting Task
        public int StartingDifficulty { get; set; }
        public int EndingDifficulty { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool IsCompleted { get; set; } = false;
        
        // Performance metrics
        public int Accuracy { get; set; } // 0-100%
        public int Score { get; set; }
        public int ReactionTime { get; set; } // milliseconds
        public int CompletionTime { get; set; } // seconds
        public int MoveCount { get; set; } // for card games
        
        // AI Analysis
        public bool DifficultyIncreased { get; set; } = false;
        public string? AIAnalysis { get; set; }
        public DateTime? CaretakerNotificationSent { get; set; }
    }

    /// <summary>
    /// Notifications for caretakers about patient progress
    /// </summary>
    public class Notification
    {
        public int Id { get; set; }
        public int CaretakerId { get; set; }
        public Caretaker? Caretaker { get; set; }
        public int? PatientId { get; set; }
        public Patient? Patient { get; set; }
        public int? GameSessionId { get; set; }
        public GameSession? GameSession { get; set; }
        
        public string? Title { get; set; }
        public string? Message { get; set; }
        public NotificationType Type { get; set; } // Difficulty Increase, Achievement, Alert, etc.
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
        public DateTime? ReadDate { get; set; }
        
        // Action tracking
        public string? ActionUrl { get; set; }
    }

    public enum NotificationType
    {
        DifficultyIncrease,
        DifficultyDecrease,
        Achievement,
        Alert,
        PatientProgress,
        SessionCompleted,
        PerformanceWarning,
        MilestoneReached
    }

    /// <summary>
    /// Patient achievements/milestones
    /// </summary>
    public class Achievement
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public Patient? Patient { get; set; }
        
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Icon { get; set; } // Bootstrap icon name
        public DateTime UnlockedDate { get; set; } = DateTime.UtcNow;
        
        public AchievementType Type { get; set; }
    }

    public enum AchievementType
    {
        Milestone,
        GameMastery,
        Consistency,
        PerformanceBased,
        DifficultyBased,
        StreakBased
    }
}
