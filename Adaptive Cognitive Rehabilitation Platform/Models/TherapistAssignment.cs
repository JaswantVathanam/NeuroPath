using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeuroPath.Models
{
    /// <summary>
    /// Represents the assignment relationship between a Therapist and a Patient (Learner).
    /// This replaces the ParentChildRelation model for the therapist-patient management system.
    /// </summary>
    [Table("TherapistAssignments")]
    public class TherapistAssignment
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the Therapist (User with UserType = "Therapist")
        /// </summary>
        [Required]
        public int TherapistId { get; set; }

        [ForeignKey("TherapistId")]
        public User? Therapist { get; set; }

        /// <summary>
        /// Foreign key to the Patient/Learner (User with UserType = "User")
        /// </summary>
        [Required]
        public int PatientUserId { get; set; }

        [ForeignKey("PatientUserId")]
        public User? Patient { get; set; }

        /// <summary>
        /// Date when the patient was assigned to this therapist
        /// </summary>
        [Required]
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Therapist's notes about the patient (therapy goals, observations, progress notes)
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Indicates if this assignment is currently active
        /// </summary>
        [Required]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Date when the assignment was deactivated (if inactive)
        /// </summary>
        public DateTime? DeactivatedDate { get; set; }

        /// <summary>
        /// Last updated timestamp for autosave functionality
        /// </summary>
        [Required]
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Therapy category/focus area for this patient
        /// </summary>
        public string? TherapyFocus { get; set; }

        /// <summary>
        /// Patient's current difficulty level as assigned by therapist
        /// </summary>
        public string? AssignedDifficultyLevel { get; set; }
    }
}
