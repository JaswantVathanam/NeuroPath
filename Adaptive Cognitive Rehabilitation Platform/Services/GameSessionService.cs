using NeuroPath.Data;
using NeuroPath.Models;
using NeuroPath.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AdaptiveCognitiveRehabilitationPlatform.Services
{
    /// <summary>
    /// Service layer for game session management
    /// Separates business logic from API controllers
    /// Implements validation, security checks, and error handling
    /// </summary>
    public interface IGameSessionService
    {
        /// <summary>
        /// Save a game session with complete validation and security checks
        /// </summary>
        Task<SaveGameSessionResponseDto> SaveGameSessionAsync(SaveGameSessionRequestDto request, int authenticatedUserId, string userRole);

        /// <summary>
        /// Get game sessions for a user with pagination
        /// </summary>
        Task<List<GameSessionResponseDto>> GetUserGameSessionsAsync(int userId, int limit = 50, int offset = 0);

        /// <summary>
        /// Get user statistics
        /// </summary>
        Task<UserStatisticsDto> GetUserStatisticsAsync(int userId);
    }

    public class GameSessionService : IGameSessionService
    {
        private readonly NeuroPathDbContext _dbContext;
        private readonly ILogger<GameSessionService> _logger;
        private const int MAX_SESSION_LIMIT = 500;
        private const decimal MAX_SCORE = 10000;
        private const decimal MAX_ACCURACY = 100;

        public GameSessionService(NeuroPathDbContext dbContext, ILogger<GameSessionService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Save game session with comprehensive validation and security checks
        /// </summary>
        public async Task<SaveGameSessionResponseDto> SaveGameSessionAsync(SaveGameSessionRequestDto request, int authenticatedUserId, string userRole)
        {
            _logger.LogInformation("[GAME-SERVICE] SaveGameSessionAsync called. UserId={UserId}, GameType={GameType}, AuthenticatedUserId={AuthenticatedUserId}", 
                request.UserId, request.GameType, authenticatedUserId);

            // ============ SECURITY: Backend must verify access ============
            // Rule: Never trust frontend - validate on backend
            if (request.UserId != authenticatedUserId && userRole != "Admin")
            {
                _logger.LogWarning("[GAME-SERVICE] SECURITY: User {AuthenticatedUserId} attempted to save session for user {RequestUserId}", 
                    authenticatedUserId, request.UserId);
                throw new UnauthorizedAccessException("You can only save sessions for yourself");
            }

            // ============ VALIDATION: Validate all inputs ============
            ValidateGameSessionRequest(request);

            // ============ DATABASE: Verify user exists ============
            var user = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == request.UserId);

            if (user == null)
            {
                _logger.LogWarning("[GAME-SERVICE] User not found: UserId={UserId}", request.UserId);
                throw new KeyNotFoundException($"User with ID {request.UserId} not found");
            }

            _logger.LogInformation("[GAME-SERVICE] User verified: {Username} (UserId={UserId})", user.Username, user.UserId);

            // ============ DATA MAPPING: Create entity from DTO ============
            var gameSession = new GameSession
            {
                UserId = request.UserId,
                GameType = request.GameType,
                GameMode = request.GameMode ?? "Practice",
                GridSize = request.GridSize ?? "4x4",
                Difficulty = request.Difficulty,
                TotalPairs = request.TotalPairs ?? 0,
                CorrectMatches = request.CorrectMatches ?? 0,
                TotalMoves = request.TotalMoves ?? 0,
                Accuracy = SanitizeAccuracy(request.Accuracy),
                PerformanceScore = SanitizeScore(request.PerformanceScore),
                EfficiencyScore = SanitizeScore(request.EfficiencyScore),
                TimeStarted = request.TimeStarted ?? DateTime.UtcNow,
                TimeCompleted = request.TimeCompleted ?? DateTime.UtcNow,
                TotalSeconds = request.TotalSeconds ?? 0,
                Status = request.Status ?? "Completed",
                IsTestMode = request.IsTestMode ?? false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // ============ DATABASE: Save to database ============
            try
            {
                _dbContext.GameSessions.Add(gameSession);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("[GAME-SERVICE] ✅ Session saved successfully: SessionId={SessionId}, UserId={UserId}, GameType={GameType}, Score={Score}",
                    gameSession.SessionId, gameSession.UserId, gameSession.GameType, gameSession.PerformanceScore);

                // ============ RESPONSE: Return minimal DTO (never expose internals) ============
                return new SaveGameSessionResponseDto
                {
                    Success = true,
                    Message = "Game session saved successfully",
                    SessionId = gameSession.SessionId,
                    Score = (int)gameSession.PerformanceScore,
                    Accuracy = gameSession.Accuracy,
                    SavedAt = gameSession.CreatedAt
                };
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError("[GAME-SERVICE] Database error while saving session: {Message}", dbEx.Message);
                throw new InvalidOperationException("Failed to save game session to database", dbEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GAME-SERVICE] Unexpected error while saving session");
                throw;
            }
        }

        /// <summary>
        /// Get user game sessions with pagination
        /// </summary>
        public async Task<List<GameSessionResponseDto>> GetUserGameSessionsAsync(int userId, int limit = 50, int offset = 0)
        {
            _logger.LogInformation("[GAME-SERVICE] GetUserGameSessionsAsync: UserId={UserId}, Limit={Limit}, Offset={Offset}", 
                userId, limit, offset);

            // ============ VALIDATION: Limit pagination values ============
            limit = Math.Min(Math.Max(limit, 1), MAX_SESSION_LIMIT);
            offset = Math.Max(offset, 0);

            try
            {
                var sessions = await _dbContext.GameSessions
                    .AsNoTracking()
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.CreatedAt)
                    .Skip(offset)
                    .Take(limit)
                    .Select(s => new GameSessionResponseDto
                    {
                        SessionId = s.SessionId,
                        GameType = s.GameType ?? "Unknown",
                        GameMode = s.GameMode ?? "Practice",
                        Difficulty = s.Difficulty,
                        Score = (int)s.PerformanceScore,
                        Accuracy = s.Accuracy,
                        TotalSeconds = s.TotalSeconds,
                        TimeCompleted = s.TimeCompleted ?? DateTime.UtcNow,
                        Status = s.Status ?? "Unknown"
                    })
                    .ToListAsync();

                _logger.LogInformation("[GAME-SERVICE] ✅ Retrieved {Count} sessions for user {UserId}", 
                    sessions.Count, userId);

                return sessions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GAME-SERVICE] Error retrieving sessions for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Get user statistics
        /// </summary>
        public async Task<UserStatisticsDto> GetUserStatisticsAsync(int userId)
        {
            _logger.LogInformation("[GAME-SERVICE] GetUserStatisticsAsync: UserId={UserId}", userId);

            try
            {
                var sessions = await _dbContext.GameSessions
                    .AsNoTracking()
                    .Where(s => s.UserId == userId)
                    .ToListAsync();

                if (sessions.Count == 0)
                {
                    return new UserStatisticsDto { TotalSessionsPlayed = 0 };
                }

                return new UserStatisticsDto
                {
                    TotalSessionsPlayed = sessions.Count,
                    AverageScore = (int)sessions.Average(s => s.PerformanceScore),
                    BestScore = (int)sessions.Max(s => s.PerformanceScore),
                    AverageAccuracy = sessions.Average(s => s.Accuracy),
                    TotalPlayTime = sessions.Sum(s => s.TotalSeconds),
                    LastSessionDate = sessions.Max(s => s.TimeCompleted) ?? DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GAME-SERVICE] Error getting statistics for user {UserId}", userId);
                throw;
            }
        }

        // ============ HELPER METHODS: Data sanitization ============

        /// <summary>
        /// Validate game session request data
        /// </summary>
        private void ValidateGameSessionRequest(SaveGameSessionRequestDto request)
        {
            if (request == null)
                throw new ArgumentException("Request cannot be null");

            if (string.IsNullOrWhiteSpace(request.GameType))
                throw new ArgumentException("Game type is required");

            if (request.TotalSeconds < 0 || request.TotalSeconds > 36000)
                throw new ArgumentException("Total seconds must be between 0 and 36000");

            // Additional business logic validation
            if (request.Accuracy < 0 || request.Accuracy > 100)
                throw new ArgumentException("Accuracy must be between 0 and 100");

            _logger.LogInformation("[GAME-SERVICE] Validation passed for game session");
        }

        /// <summary>
        /// Sanitize and cap accuracy values
        /// </summary>
        private decimal SanitizeAccuracy(decimal? accuracy)
        {
            if (accuracy == null) return 0;
            return Math.Min(Math.Max(accuracy.Value, 0), MAX_ACCURACY);
        }

        /// <summary>
        /// Sanitize and cap score values
        /// </summary>
        private int SanitizeScore(decimal? score)
        {
            if (score == null) return 0;
            return (int)Math.Min(Math.Max(score.Value, 0), MAX_SCORE);
        }
    }

    /// <summary>
    /// User statistics DTO
    /// </summary>
    public class UserStatisticsDto
    {
        public int TotalSessionsPlayed { get; set; }
        public int AverageScore { get; set; }
        public int BestScore { get; set; }
        public decimal AverageAccuracy { get; set; }
        public long TotalPlayTime { get; set; }
        public DateTime LastSessionDate { get; set; }
    }
}
