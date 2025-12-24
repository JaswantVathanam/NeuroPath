using System;

namespace NeuroPath.Models
{
    public class AuditLog
    {
        public int AuditLogId { get; set; }
        public string? EntityName { get; set; }
        public string? EntityId { get; set; }
        public string? Action { get; set; }
        public int? PerformedByUserId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? Details { get; set; }
    }
}
