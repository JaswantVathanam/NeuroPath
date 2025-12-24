namespace NeuroPath.Models
{
    public class LinkCode
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? Code { get; set; } // 6-8 char alphanumeric, unique
        public DateTime ExpiresAt { get; set; } // 7 days from creation
        public bool IsUsed { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation
        public virtual User? User { get; set; }
    }
}
