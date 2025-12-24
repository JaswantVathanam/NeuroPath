using System.Text.Json;
using System.Text.Json.Serialization;

namespace AdaptiveCognitiveRehabilitationPlatform.Services.Authentication
{
    /// <summary>
    /// JSON-based user authentication service for demo/presentation purposes
    /// </summary>
    public interface IJsonUserService
    {
        Task<JsonUserAuthResult> AuthenticateAsync(string username, string password);
        Task<List<JsonUserEntry>> GetAllUsersAsync();
        Task<JsonUserEntry?> GetUserByIdAsync(int userId);
        Task<JsonUserEntry?> GetUserByUsernameAsync(string username);
    }

    public class JsonUserService : IJsonUserService
    {
        private readonly string _usersFilePath;
        private readonly ILogger<JsonUserService> _logger;
        private List<JsonUserEntry>? _cachedUsers;

        public JsonUserService(ILogger<JsonUserService> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _usersFilePath = Path.Combine(environment.ContentRootPath, "GameData", "users.json");
            EnsureUsersFileExists();
        }

        private void EnsureUsersFileExists()
        {
            var directory = Path.GetDirectoryName(_usersFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
                _logger.LogInformation($"Created users directory: {directory}");
            }

            if (!File.Exists(_usersFilePath))
            {
                var defaultUsers = new JsonUsersFile
                {
                    Users = new List<JsonUserEntry>()
                };
                var json = JsonSerializer.Serialize(defaultUsers, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_usersFilePath, json);
                _logger.LogInformation($"Created users file: {_usersFilePath}");
            }
        }

        private async Task<List<JsonUserEntry>> LoadUsersAsync()
        {
            if (_cachedUsers != null)
                return _cachedUsers;

            try
            {
                var json = await File.ReadAllTextAsync(_usersFilePath);
                var usersFile = JsonSerializer.Deserialize<JsonUsersFile>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _cachedUsers = usersFile?.Users ?? new List<JsonUserEntry>();
                _logger.LogInformation($"Loaded {_cachedUsers.Count} users from JSON file");
                return _cachedUsers;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading users from JSON: {ex.Message}");
                return new List<JsonUserEntry>();
            }
        }

        public async Task<JsonUserAuthResult> AuthenticateAsync(string username, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    return new JsonUserAuthResult
                    {
                        Success = false,
                        Message = "Username and password are required"
                    };
                }

                var users = await LoadUsersAsync();
                var user = users.FirstOrDefault(u =>
                    (u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) ||
                     u.Email.Equals(username, StringComparison.OrdinalIgnoreCase)) &&
                    u.Password == password &&
                    u.IsActive);

                if (user == null)
                {
                    _logger.LogWarning($"Failed login attempt for username: {username}");
                    return new JsonUserAuthResult
                    {
                        Success = false,
                        Message = "Invalid username or password"
                    };
                }

                _logger.LogInformation($"✅ User authenticated successfully: {user.FullName} ({user.UserType})");
                return new JsonUserAuthResult
                {
                    Success = true,
                    Message = "Login successful",
                    User = user
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Authentication error: {ex.Message}");
                return new JsonUserAuthResult
                {
                    Success = false,
                    Message = $"Authentication error: {ex.Message}"
                };
            }
        }

        public async Task<List<JsonUserEntry>> GetAllUsersAsync()
        {
            return await LoadUsersAsync();
        }

        public async Task<JsonUserEntry?> GetUserByIdAsync(int userId)
        {
            var users = await LoadUsersAsync();
            return users.FirstOrDefault(u => u.UserId == userId);
        }

        public async Task<JsonUserEntry?> GetUserByUsernameAsync(string username)
        {
            var users = await LoadUsersAsync();
            return users.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) ||
                u.Email.Equals(username, StringComparison.OrdinalIgnoreCase));
        }
    }

    // DTOs
    public class JsonUsersFile
    {
        [JsonPropertyName("users")]
        public List<JsonUserEntry> Users { get; set; } = new();
    }

    public class JsonUserEntry
    {
        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; } = "";

        [JsonPropertyName("fullName")]
        public string FullName { get; set; } = "";

        [JsonPropertyName("email")]
        public string Email { get; set; } = "";

        [JsonPropertyName("password")]
        public string Password { get; set; } = "";

        [JsonPropertyName("userType")]
        public string UserType { get; set; } = "User";

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class JsonUserAuthResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public JsonUserEntry? User { get; set; }
    }
}
