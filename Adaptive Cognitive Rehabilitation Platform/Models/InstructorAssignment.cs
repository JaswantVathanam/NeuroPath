using System;

namespace NeuroPath.Models
{
    public class InstructorAssignment
    {
        public int AssignmentId { get; set; }
        public int TrainerId { get; set; }
        public int UserId { get; set; }
        public int? ProfileId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Notes { get; set; }
    }
}
