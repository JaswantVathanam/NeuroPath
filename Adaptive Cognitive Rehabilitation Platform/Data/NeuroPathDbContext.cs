using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NeuroPath.Models;

namespace NeuroPath.Data
{
    public class NeuroPathDbContext : DbContext
    {
        public NeuroPathDbContext(DbContextOptions<NeuroPathDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<GameSession> GameSessions { get; set; }
        public DbSet<AIAnalysis> AIAnalyses { get; set; }
        public DbSet<PerformanceSummary> PerformanceSummaries { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<ParentChildRelation> ParentChildRelations { get; set; }
        public DbSet<TherapistAssignment> TherapistAssignments { get; set; }
        public DbSet<LinkCode> LinkCodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.UserId);
                entity.HasMany(u => u.GameSessions).WithOne(s => s.User).HasForeignKey(s => s.UserId);
            });

            modelBuilder.Entity<GameSession>(entity =>
            {
                entity.HasKey(s => s.SessionId);
            });

            modelBuilder.Entity<AIAnalysis>(entity =>
            {
                entity.HasKey(a => a.AnalysisId);
                entity.HasOne<GameSession>().WithMany().HasForeignKey(a => a.SessionId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PerformanceSummary>(entity =>
            {
                entity.HasKey(p => p.Id);
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(a => a.AuditLogId);
            });

            modelBuilder.Entity<ParentChildRelation>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.HasOne(r => r.Parent)
                    .WithMany()
                    .HasForeignKey(r => r.ParentUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(r => r.Student)
                    .WithMany()
                    .HasForeignKey(r => r.StudentUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                // Unique constraint to prevent duplicate parent-child relationships
                entity.HasIndex(r => new { r.ParentUserId, r.StudentUserId }).IsUnique();
            });

            modelBuilder.Entity<LinkCode>(entity =>
            {
                entity.HasKey(l => l.Id);
                entity.HasOne(l => l.User)
                    .WithOne(u => u.LinkCode)
                    .HasForeignKey<LinkCode>(l => l.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                // Unique constraint on Code column
                entity.HasIndex(l => l.Code).IsUnique();
            });

            modelBuilder.Entity<TherapistAssignment>(entity =>
            {
                entity.HasKey(ta => ta.Id);
                entity.HasOne(ta => ta.Therapist)
                    .WithMany()
                    .HasForeignKey(ta => ta.TherapistId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(ta => ta.Patient)
                    .WithMany()
                    .HasForeignKey(ta => ta.PatientUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                // Unique constraint to prevent duplicate therapist-patient assignments
                entity.HasIndex(ta => new { ta.TherapistId, ta.PatientUserId, ta.IsActive }).IsUnique(false);
            });
        }
    }
}
