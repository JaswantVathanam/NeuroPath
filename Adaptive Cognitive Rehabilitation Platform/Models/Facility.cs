using System;
using System.Collections.Generic;

namespace NeuroPath.Models
{
    /// <summary>
    /// Represents a facility (hospital, clinic, school, home) where trainers work
    /// </summary>
    public class Facility
    {
        public int FacilityId { get; set; }
        
        // Basic Info
        public string FacilityName { get; set; }
        public string FacilityType { get; set; } // "Hospital", "Clinic", "School", "Community Center", "Home"
        
        // Location
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }
        
        // Contact
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        
        // Details
        public string Description { get; set; }
        public string DirectorName { get; set; }
        public string LicenseNumber { get; set; }
        
        // Capacity
        public int MaxTrainers { get; set; } = 100;
        public int MaxUsers { get; set; } = 1000;
        
        // Status
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        public ICollection<User> Trainers { get; set; } = new List<User>();
    }
}
