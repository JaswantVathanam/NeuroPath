using System;

namespace NeuroPath.Models
{
    public class PerformanceSummary
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? ProfileId { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal AverageAccuracy { get; set; }
        public decimal AverageScore { get; set; }
        public int TotalSessions { get; set; }
    }
}
