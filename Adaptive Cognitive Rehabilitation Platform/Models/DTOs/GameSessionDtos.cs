using System.ComponentModel.DataAnnotations;

namespace NeuroPath.Models.DTOs
{
    /// <summary>
    /// DTO for saving game session - Frontend sends this, backend validates it
    /// NEVER exposes internal database entities directly to clients
    /// </summary>
    public class SaveGameSessionRequestDto
    {
        /// <summary>
        /// The user ID of the player (therapist/patient) - Will be validated against JWT token
        /// </summary>
        [Required(ErrorMessage = "User ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "User ID must be a positive integer")]
        public int UserId { get; set; }

        /// <summary>
        /// Type of game played (e.g., MemoryMatch, ReactionTrainer, SortingTask)
        /// </summary>
        [Required(ErrorMessage = "Game type is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Game type must be between 3 and 50 characters")]
        [RegularExpression(@"^[a-zA-Z0-9\-]+$", ErrorMessage = "Game type contains invalid characters")]
        public string GameType { get; set; } = "";

        /// <summary>
        /// Game mode (Practice, Challenge, Test, etc.)
        /// </summary>
        [StringLength(50, ErrorMessage = "Game mode must not exceed 50 characters")]
        public string? GameMode { get; set; }

        /// <summary>
        /// Grid size for games like Memory Match (e.g., "4x4", "6x6")
        /// </summary>
        [StringLength(20, ErrorMessage = "Grid size must not exceed 20 characters")]
        public string? GridSize { get; set; }

        /// <summary>
        /// Difficulty level (1-10)
        /// </summary>
        [Range(1, 10, ErrorMessage = "Difficulty must be between 1 and 10")]
        public int Difficulty { get; set; } = 1;

        /// <summary>
        /// Total pairs for memory-based games
        /// </summary>
        [Range(0, 1000, ErrorMessage = "Total pairs must be between 0 and 1000")]
        public int? TotalPairs { get; set; }

        /// <summary>
        /// Correct matches achieved
        /// </summary>
        [Range(0, 1000, ErrorMessage = "Correct matches must be between 0 and 1000")]
        public int? CorrectMatches { get; set; }

        /// <summary>
        /// Total moves/attempts made
        /// </summary>
        [Range(0, 10000, ErrorMessage = "Total moves must be between 0 and 10000")]
        public int? TotalMoves { get; set; }

        /// <summary>
        /// Accuracy percentage (0-100)
        /// </summary>
        [Range(0, 100, ErrorMessage = "Accuracy must be between 0 and 100")]
        public decimal? Accuracy { get; set; }

        /// <summary>
        /// Performance score (typically 0-100)
        /// </summary>
        [Range(0, 10000, ErrorMessage = "Performance score must be a positive number")]
        public decimal? PerformanceScore { get; set; }

        /// <summary>
        /// Efficiency score based on time and accuracy
        /// </summary>
        [Range(0, 10000, ErrorMessage = "Efficiency score must be a positive number")]
        public decimal? EfficiencyScore { get; set; }

        /// <summary>
        /// When the game started
        /// </summary>
        public DateTime? TimeStarted { get; set; }

        /// <summary>
        /// When the game was completed
        /// </summary>
        public DateTime? TimeCompleted { get; set; }

        /// <summary>
        /// Total seconds played
        /// </summary>
        [Range(0, 36000, ErrorMessage = "Total seconds must be between 0 and 36000 (10 hours)")]
        public int? TotalSeconds { get; set; }

        /// <summary>
        /// Game status (Completed, InProgress, Abandoned, Failed)
        /// </summary>
        [StringLength(50, ErrorMessage = "Status must not exceed 50 characters")]
        public string? Status { get; set; }

        /// <summary>
        /// Whether this was a test mode session
        /// </summary>
        public bool? IsTestMode { get; set; }
    }

    /// <summary>
    /// Response DTO after successful game session save
    /// NEVER exposes unnecessary data - only what client needs
    /// </summary>
    public class SaveGameSessionResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int SessionId { get; set; }
        public decimal Score { get; set; }
        public decimal Accuracy { get; set; }
        public DateTime SavedAt { get; set; }
    }

    /// <summary>
    /// Error response DTO for consistent error handling
    /// </summary>
    public class ErrorResponseDto
    {
        public bool Success { get; set; } = false;
        public string Message { get; set; } = "";
        public string? ErrorCode { get; set; }
        public Dictionary<string, string[]>? ValidationErrors { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Get game sessions for a user - Response DTO
    /// </summary>
    public class GameSessionResponseDto
    {
        public int SessionId { get; set; }
        public string GameType { get; set; } = "";
        public string GameMode { get; set; } = "";
        public int Difficulty { get; set; }
        public int Score { get; set; }
        public decimal Accuracy { get; set; }
        public int TotalSeconds { get; set; }
        public DateTime TimeCompleted { get; set; }
        public string Status { get; set; } = "";
    }

    /// <summary>
    /// Therapist-Patient relationship DTO
    /// </summary>
    public class PatientAssignmentDto
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string AgeGroup { get; set; } = "";
        public DateTime AssignedDate { get; set; }
    }

    /// <summary>
    /// DTO for patient game autosave - Sent by game component after AI evaluation
    /// Contains all results that AI calculated
    /// </summary>
    public class PatientGameAutosaveDto
    {
        /// <summary>
        /// Type of game (MemoryMatch, PatternRecognition, SpeedChallenge, etc.)
        /// </summary>
        [Required(ErrorMessage = "Game type is required")]
        [StringLength(50, MinimumLength = 3)]
        public string GameType { get; set; } = "";

        /// <summary>
        /// Final score calculated by AI
        /// </summary>
        [Range(0, 100000, ErrorMessage = "Score must be a positive number")]
        public int Score { get; set; }

        /// <summary>
        /// Accuracy percentage (0-100)
        /// </summary>
        [Range(0, 100, ErrorMessage = "Accuracy must be between 0 and 100")]
        public double Accuracy { get; set; }

        /// <summary>
        /// Time taken in milliseconds
        /// </summary>
        [Range(0, 3600000, ErrorMessage = "Time must be between 0 and 1 hour")]
        public long TimeTaken { get; set; }

        /// <summary>
        /// Current difficulty level
        /// </summary>
        [StringLength(50)]
        public string? DifficultyCurrent { get; set; }

        /// <summary>
        /// Difficulty level recommended by AI (Easy, Medium, Hard, Expert)
        /// </summary>
        [StringLength(50)]
        public string? DifficultyRecommended { get; set; }

        /// <summary>
        /// AI analysis text/feedback about performance
        /// </summary>
        [StringLength(1000)]
        public string? AIAnalysis { get; set; }

        /// <summary>
        /// Additional metadata (JSON string) with game-specific data
        /// </summary>
        [StringLength(2000)]
        public string? ResultMetadata { get; set; }
    }

    /// <summary>
    /// Response DTO after successful patient game autosave
    /// </summary>
    public class PatientGameAutosaveResponseDto
    {
        public bool Success { get; set; }
        public DateTime SavedAt { get; set; }
        public string OperationId { get; set; } = "";
        public int GameSessionId { get; set; }
        public string GameType { get; set; } = "";
        public int Score { get; set; }
        public double Accuracy { get; set; }
        public string? DifficultyRecommended { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// DTO for displaying game sessions in therapist dashboard
    /// </summary>
    public class GameSessionDisplayDto
    {
        public int SessionId { get; set; }
        public string PatientName { get; set; } = "";
        public int PatientId { get; set; }
        public string GameType { get; set; } = "";
        public int PerformanceScore { get; set; }
        public decimal Accuracy { get; set; }
        public int TotalSeconds { get; set; }
        public int Difficulty { get; set; }
        public int NextDifficultyLevel { get; set; }
        public DateTime TimeCompleted { get; set; }
        public string? AiAnalysis { get; set; }
    }
}

