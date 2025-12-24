using System;

namespace NeuroPath.Models
{
    /// <summary>
    /// Represents a game assigned by a trainer to a user
    /// </summary>
    public class GameAssignment
    {
        public int AssignmentId { get; set; }
        public int TrainerId { get; set; }
        public int UserId { get; set; }
        public int? ProfileId { get; set; }
        
        // Assignment Details
        public string GameType { get; set; } // "MemoryMatch", etc.
        public string GameMode { get; set; } // "Practice", "Test"
        public int Difficulty { get; set; } // 1-5
        public string Description { get; set; } // Why this game was assigned
        
        // Timeline
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
        public DateTime DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        
        // Tracking
        public string Status { get; set; } // "Active", "Completed", "Pending", "Overdue"
        public bool IsCompleted { get; set; } = false;
        public int? SessionIdUsed { get; set; } // Link to actual game session when played
        
        // Trainer Notes
        public string Notes { get; set; }
        public string Reason { get; set; } // Why assigned
        
        // Required Score
        public decimal? TargetScore { get; set; } // Trainer can set minimum expected score
        public decimal? ActualScore { get; set; } // Populated when completed
        
        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        public User Trainer { get; set; }
        public User User { get; set; }
        public UserProfile Profile { get; set; }
    }
}
