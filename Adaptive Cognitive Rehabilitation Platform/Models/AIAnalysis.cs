using System;

namespace NeuroPath.Models
{
    public class AIAnalysis
    {
        public int AnalysisId { get; set; }
        public int SessionId { get; set; }
        public string? Message { get; set; }
        public string? StrengthsJson { get; set; } // JSON array
        public string? WeaknessesJson { get; set; } // JSON array
        public string? RecommendationsJson { get; set; } // JSON array
        public decimal ConfidenceScore { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public int? ReviewedByInstructorId { get; set; }
        public string? ReviewNotes { get; set; }
    }
}
