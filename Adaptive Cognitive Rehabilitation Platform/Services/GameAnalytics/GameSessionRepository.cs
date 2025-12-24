using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NeuroPath.Models;

namespace AdaptiveCognitiveRehabilitationPlatform.Services.GameAnalytics
{
    /// <summary>
    /// In-memory implementation of game session repository
    /// TODO: Replace with Entity Framework implementation when DB is ready
    /// </summary>
    public class GameSessionRepository : IGameSessionRepository
    {
        private static List<GameSession> _sessions = new();
        private readonly ILogger<GameSessionRepository> _logger;

        public GameSessionRepository(ILogger<GameSessionRepository> logger)
        {
            _logger = logger;
        }

        // CRUD Operations
        public async Task<GameSession> CreateSessionAsync(GameSession session)
        {
            _sessions.Add(session);
            _logger.LogInformation($"Created session {session.SessionId}");
            return await Task.FromResult(session);
        }

        public async Task<GameSession> GetSessionByIdAsync(int sessionId)
        {
            return await Task.FromResult(_sessions.FirstOrDefault(s => s.SessionId == sessionId));
        }

        public async Task<GameSession> UpdateSessionAsync(GameSession session)
        {
            var existing = _sessions.FirstOrDefault(s => s.SessionId == session.SessionId);
            if (existing != null)
            {
                _sessions.Remove(existing);
                _sessions.Add(session);
            }
            return await Task.FromResult(session);
        }

        public async Task<bool> DeleteSessionAsync(int sessionId)
        {
            var session = _sessions.FirstOrDefault(s => s.SessionId == sessionId);
            if (session != null)
            {
                _sessions.Remove(session);
                return await Task.FromResult(true);
            }
            return await Task.FromResult(false);
        }

        // Query Operations
        public async Task<List<GameSession>> GetSessionsByUserIdAsync(int userId)
        {
            return await Task.FromResult(_sessions.Where(s => s.UserId == userId).ToList());
        }

        public async Task<List<GameSession>> GetSessionsByProfileIdAsync(int profileId)
        {
            return await Task.FromResult(_sessions.Where(s => s.ProfileId == profileId).ToList());
        }

        public async Task<List<GameSession>> GetSessionsByGameTypeAsync(string gameType)
        {
            return await Task.FromResult(_sessions.Where(s => s.GameType == gameType).ToList());
        }

        public async Task<List<GameSession>> GetSessionsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await Task.FromResult(_sessions
                .Where(s => s.TimeStarted >= startDate && s.TimeStarted <= endDate)
                .ToList());
        }

        public async Task<List<GameSession>> GetRecentSessionsAsync(int userId, int count = 10)
        {
            return await Task.FromResult(_sessions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.TimeStarted)
                .Take(count)
                .ToList());
        }

        public async Task<List<GameSession>> GetCompletedSessionsAsync(int userId)
        {
            return await Task.FromResult(_sessions
                .Where(s => s.UserId == userId && s.Status == "Completed")
                .ToList());
        }

        // Analytics Queries
        public async Task<Dictionary<string, int>> GetSessionCountByGameTypeAsync(int userId)
        {
            var result = _sessions
                .Where(s => s.UserId == userId && s.Status == "Completed")
                .GroupBy(s => s.GameType)
                .ToDictionary(g => g.Key, g => g.Count());

            return await Task.FromResult(result);
        }

        public async Task<double> GetAverageAccuracyAsync(int userId)
        {
            var sessions = _sessions.Where(s => s.UserId == userId && s.Status == "Completed").ToList();
            if (!sessions.Any()) return 0;

            return await Task.FromResult(sessions.Average(s => (double)s.Accuracy));
        }

        public async Task<double> GetAverageAccuracyByGameAsync(int userId, string gameType)
        {
            var sessions = _sessions
                .Where(s => s.UserId == userId && s.GameType == gameType && s.Status == "Completed")
                .ToList();

            if (!sessions.Any()) return 0;

            return await Task.FromResult(sessions.Average(s => (double)s.Accuracy));
        }

        public async Task<int> GetCurrentStreakAsync(int userId)
        {
            var sessions = _sessions
                .Where(s => s.UserId == userId && s.Status == "Completed")
                .OrderByDescending(s => s.TimeCompleted)
                .ToList();

            if (!sessions.Any()) return 0;

            int streak = 0;
            DateTime lastDate = DateTime.UtcNow.Date;

            foreach (var session in sessions)
            {
                var sessionDate = (session.TimeCompleted ?? DateTime.UtcNow).Date;
                if (sessionDate == lastDate || sessionDate == lastDate.AddDays(-1))
                {
                    streak++;
                    lastDate = sessionDate;
                }
                else
                {
                    break;
                }
            }

            return await Task.FromResult(streak);
        }

        public async Task<List<GameSession>> GetSessionsByDifficultyAsync(int userId, int difficulty)
        {
            return await Task.FromResult(_sessions
                .Where(s => s.UserId == userId && s.Difficulty == difficulty)
                .ToList());
        }

        // Statistics
        public async Task<int> GetTotalSessionsAsync(int userId)
        {
            return await Task.FromResult(_sessions.Count(s => s.UserId == userId && s.Status == "Completed"));
        }

        public async Task<int> GetSessionsThisWeekAsync(int userId)
        {
            var startOfWeek = DateTime.UtcNow.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
            return await Task.FromResult(_sessions
                .Count(s => s.UserId == userId && s.TimeStarted >= startOfWeek && s.Status == "Completed"));
        }

        public async Task<int> GetSessionsThisMonthAsync(int userId)
        {
            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            return await Task.FromResult(_sessions
                .Count(s => s.UserId == userId && s.TimeStarted >= startOfMonth && s.Status == "Completed"));
        }
    }
}
