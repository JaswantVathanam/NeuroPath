namespace NeuroPath.DTOs
{
    /// <summary>
    /// Summary data for a Student/User, used in dashboards for Parents and Instructors
    /// </summary>
    public class StudentSummaryDto
    {
        public int StudentId { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public DateTime? LastSessionDate { get; set; }
        public int TotalSessions { get; set; }
        public double AverageAccuracy { get; set; }
        public int ActiveAssignments { get; set; }
    }
}
