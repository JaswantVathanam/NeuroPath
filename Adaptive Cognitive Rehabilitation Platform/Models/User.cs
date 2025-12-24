using System;
using System.Collections.Generic;

namespace NeuroPath.Models
{
    /// <summary>
    /// Represents a user in the system (learner or parent/guardian)
    /// </summary>
    public class User
    {
        public int UserId { get; set; }
        
        // Authentication
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? PasswordHash { get; set; }
        
        // Type: "User" (learner) or "Parent" (guardian)
        public string? UserType { get; set; }
        
        // For User (learner/child)
        public int? Age { get; set; }
        public string? CognitiveLevel { get; set; } // "Mild", "Moderate", "Severe"
        
        // Account Status
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        public LinkCode? LinkCode { get; set; }
        public ICollection<ParentChildRelation> AsParent { get; set; } = new List<ParentChildRelation>();
        public ICollection<ParentChildRelation> AsChild { get; set; } = new List<ParentChildRelation>();
        public ICollection<GameSession> GameSessions { get; set; } = new List<GameSession>();
    }
}
