using System;
using System.Security.Cryptography;
using System.Text;
using NeuroPath.Models;
using NeuroPath.Data;
using Microsoft.EntityFrameworkCore;

namespace AdaptiveCognitiveRehabilitationPlatform.Services.Authentication
{
    /// <summary>
    /// Service for user identity management (registration, login, password handling)
    /// </summary>
    public class IdentityService
    {
        private readonly NeuroPathDbContext _dbContext;
        private readonly ILogger<IdentityService> _logger;

        public IdentityService(NeuroPathDbContext dbContext, ILogger<IdentityService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        public async Task<(bool Success, string Message, User? User)> RegisterUserAsync(
            string username, 
            string email, 
            string password, 
            string userType = "User")
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
                    return (false, "Username must be at least 3 characters long", null);

                if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                    return (false, "Invalid email address", null);

                if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                    return (false, "Password must be at least 6 characters long", null);

                // Validate UserType
                if (userType != "User" && userType != "Parent")
                    userType = "User";

                // Check if user already exists
                var existingUser = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Username == username || u.Email == email);

                if (existingUser != null)
                    return (false, "Username or email already exists", null);

                // Create new user
                var user = new User
                {
                    Username = username,
                    Email = email,
                    PasswordHash = HashPassword(password),
                    UserType = userType,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"User registered successfully: {username} (Type: {userType})");
                return (true, "User registered successfully", user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error registering user: {ex.Message}");
                return (false, $"Error registering user: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Authenticate user with credentials
        /// </summary>
        public async Task<(bool Success, string Message, User? User)> AuthenticateAsync(
            string username, 
            string password)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                    return (false, "Username and password are required", null);

                // Find user by username or email
                var user = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Username == username || u.Email == username);

                if (user == null)
                    return (false, "Invalid username or password", null);

                if (!user.IsActive)
                    return (false, "User account is inactive", null);

                // Verify password
                if (!VerifyPassword(password, user.PasswordHash))
                    return (false, "Invalid username or password", null);

                // Update last login
                user.LastLogin = DateTime.UtcNow;
                _dbContext.Users.Update(user);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"User authenticated successfully: {username}");
                return (true, "Authentication successful", user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error authenticating user: {ex.Message}");
                return (false, $"Error authenticating user: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            try
            {
                return await _dbContext.Users.FindAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user by ID: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get user by username
        /// </summary>
        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            try
            {
                return await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Username == username);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user by username: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Change user password
        /// </summary>
        public async Task<(bool Success, string Message)> ChangePasswordAsync(
            int userId, 
            string currentPassword, 
            string newPassword)
        {
            try
            {
                var user = await _dbContext.Users.FindAsync(userId);
                if (user == null)
                    return (false, "User not found");

                // Verify current password
                if (!VerifyPassword(currentPassword, user.PasswordHash))
                    return (false, "Current password is incorrect");

                // Validate new password
                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                    return (false, "New password must be at least 6 characters long");

                // Update password
                user.PasswordHash = HashPassword(newPassword);
                user.UpdatedAt = DateTime.UtcNow;

                _dbContext.Users.Update(user);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"Password changed for user: {user.Username}");
                return (true, "Password changed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error changing password: {ex.Message}");
                return (false, $"Error changing password: {ex.Message}");
            }
        }

        /// <summary>
        /// Hash password using PBKDF2
        /// </summary>
        private string HashPassword(string password)
        {
            // PBKDF2 implementation with modern RandomNumberGenerator
            byte[] salt = new byte[16];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, System.Security.Cryptography.HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(20);
                byte[] hashWithSalt = new byte[36];
                System.Array.Copy(salt, 0, hashWithSalt, 0, 16);
                System.Array.Copy(hash, 0, hashWithSalt, 16, 20);

                return Convert.ToBase64String(hashWithSalt);
            }
        }

        /// <summary>
        /// Verify password against hash
        /// </summary>
        private bool VerifyPassword(string password, string hash)
        {
            try
            {
                byte[] hashWithSalt = Convert.FromBase64String(hash);
                byte[] salt = new byte[16];
                System.Array.Copy(hashWithSalt, 0, salt, 0, 16);

                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, System.Security.Cryptography.HashAlgorithmName.SHA256))
                {
                    byte[] hash2 = pbkdf2.GetBytes(20);
                    for (int i = 0; i < 20; i++)
                    {
                        if (hashWithSalt[i + 16] != hash2[i])
                            return false;
                    }
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
