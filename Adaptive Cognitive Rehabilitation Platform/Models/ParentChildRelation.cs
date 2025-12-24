using System;

namespace NeuroPath.Models
{
    public class ParentChildRelation
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Foreign key to Parent/Guardian user
        /// </summary>
        public int ParentUserId { get; set; }
        
        /// <summary>
        /// Foreign key to Student/User
        /// </summary>
        public int StudentUserId { get; set; }
        
        /// <summary>
        /// Relationship description: "Mother", "Father", "Guardian", "Grandparent", etc.
        /// </summary>
        public string? Relationship { get; set; }
        
        /// <summary>
        /// Is this the primary contact for the student?
        /// </summary>
        public bool IsPrimary { get; set; } = false;
        
        /// <summary>
        /// When the relationship was established
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual User? Parent { get; set; }
        public virtual User? Student { get; set; }
    }
}
