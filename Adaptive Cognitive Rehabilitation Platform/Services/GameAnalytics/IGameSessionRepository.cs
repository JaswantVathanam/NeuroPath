using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeuroPath.Models;

namespace AdaptiveCognitiveRehabilitationPlatform.Services.GameAnalytics
{
    /// <summary>
    /// Repository interface for game session data persistence
    /// </summary>
    public interface IGameSessionRepository
    {
        // CRUD Operations
        Task<GameSession> CreateSessionAsync(GameSession session);
        Task<GameSession> GetSessionByIdAsync(int sessionId);
        Task<GameSession> UpdateSessionAsync(GameSession session);
        Task<bool> DeleteSessionAsync(int sessionId);

        // Query Operations
        Task<List<GameSession>> GetSessionsByUserIdAsync(int userId);
        Task<List<GameSession>> GetSessionsByProfileIdAsync(int profileId);
        Task<List<GameSession>> GetSessionsByGameTypeAsync(string gameType);
        Task<List<GameSession>> GetSessionsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<List<GameSession>> GetRecentSessionsAsync(int userId, int count = 10);
        Task<List<GameSession>> GetCompletedSessionsAsync(int userId);
        
        // Analytics Queries
        Task<Dictionary<string, int>> GetSessionCountByGameTypeAsync(int userId);
        Task<double> GetAverageAccuracyAsync(int userId);
        Task<double> GetAverageAccuracyByGameAsync(int userId, string gameType);
        Task<int> GetCurrentStreakAsync(int userId);
        Task<List<GameSession>> GetSessionsByDifficultyAsync(int userId, int difficulty);
        
        // Statistics
        Task<int> GetTotalSessionsAsync(int userId);
        Task<int> GetSessionsThisWeekAsync(int userId);
        Task<int> GetSessionsThisMonthAsync(int userId);
    }
}
