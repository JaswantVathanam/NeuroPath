using NeuroPath.Data;
using NeuroPath.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace NeuroPath.Services
{
    /// <summary>
    /// Service for seeding initial test data into the database
    /// </summary>
    public class DataSeedingService
    {
        private readonly NeuroPathDbContext _dbContext;
        private readonly ILogger<DataSeedingService> _logger;

        public DataSeedingService(NeuroPathDbContext dbContext, ILogger<DataSeedingService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Seed test users and parents into the database
        /// </summary>
        public async Task SeedDataAsync()
        {
            try
            {
                _logger.LogInformation("Starting data seeding...");

                // Check if users already exist
                var existingUsers = await _dbContext.Users.CountAsync();
                if (existingUsers > 0)
                {
                    _logger.LogInformation($"Database already has {existingUsers} users. Skipping seed.");
                    return;
                }

                // Create Users (Learners)
                var user1 = new User
                {
                    Username = "jaswantb",
                    Email = "jaswant.b@outlook.com",
                    PasswordHash = HashPassword("JaswantB@123"),
                    UserType = "User",
                    Age = 12,
                    CognitiveLevel = "Moderate",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var user2 = new User
                {
                    Username = "rishikap",
                    Email = "rishika.p@outlook.com",
                    PasswordHash = HashPassword("RishikaP@123"),
                    UserType = "User",
                    Age = 10,
                    CognitiveLevel = "Mild",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Create Parents (Guardians)
                var parent1 = new User
                {
                    Username = "baskar",
                    Email = "baskar@outlook.com",
                    PasswordHash = HashPassword("Baskar@123"),
                    UserType = "Parent",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var parent2 = new User
                {
                    Username = "ponnalagappan",
                    Email = "ponnalagappan@outlook.com",
                    PasswordHash = HashPassword("Ponnalagappan@123"),
                    UserType = "Parent",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Add users to context
                _dbContext.Users.Add(user1);
                _dbContext.Users.Add(user2);
                _dbContext.Users.Add(parent1);
                _dbContext.Users.Add(parent2);

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Users created successfully");

                // Create Parent-Child Relationships
                var relation1 = new ParentChildRelation
                {
                    ParentUserId = parent1.UserId,
                    StudentUserId = user1.UserId,
                    Relationship = "Parent",
                    IsPrimary = true,
                    CreatedAt = DateTime.UtcNow
                };

                var relation2 = new ParentChildRelation
                {
                    ParentUserId = parent2.UserId,
                    StudentUserId = user2.UserId,
                    Relationship = "Parent",
                    IsPrimary = true,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.ParentChildRelations.Add(relation1);
                _dbContext.ParentChildRelations.Add(relation2);

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Parent-Child relationships created successfully");

                // Create some sample game sessions for testing
                var session1 = new GameSession
                {
                    UserId = user1.UserId,
                    GameType = "MemoryMatch",
                    GameMode = "Practice",
                    GridSize = "4x4",
                    Difficulty = 2,
                    TotalPairs = 8,
                    CorrectMatches = 7,
                    TotalMoves = 12,
                    Accuracy = 87.5m,
                    PerformanceScore = 85,
                    EfficiencyScore = 0.92m,
                    TimeStarted = DateTime.UtcNow.AddMinutes(-5),
                    TimeCompleted = DateTime.UtcNow,
                    TotalSeconds = 300,
                    Status = "Completed",
                    IsTestMode = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var session2 = new GameSession
                {
                    UserId = user2.UserId,
                    GameType = "ReactionTrainer",
                    GameMode = "Practice",
                    GridSize = "1x1",
                    Difficulty = 1,
                    TotalPairs = 10,
                    CorrectMatches = 9,
                    TotalMoves = 10,
                    Accuracy = 90m,
                    PerformanceScore = 88,
                    EfficiencyScore = 0.95m,
                    TimeStarted = DateTime.UtcNow.AddMinutes(-3),
                    TimeCompleted = DateTime.UtcNow,
                    TotalSeconds = 180,
                    Status = "Completed",
                    IsTestMode = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _dbContext.GameSessions.Add(session1);
                _dbContext.GameSessions.Add(session2);

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Sample game sessions created successfully");

                _logger.LogInformation("✅ Data seeding completed successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error during data seeding: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Hash password using PBKDF2
        /// </summary>
        private string HashPassword(string password)
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(20);
                byte[] hashWithSalt = new byte[36];
                Array.Copy(salt, 0, hashWithSalt, 0, 16);
                Array.Copy(hash, 0, hashWithSalt, 16, 20);

                return Convert.ToBase64String(hashWithSalt);
            }
        }
    }
}
