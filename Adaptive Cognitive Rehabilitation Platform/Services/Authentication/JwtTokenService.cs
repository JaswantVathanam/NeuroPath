using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using NeuroPath.Models;

namespace AdaptiveCognitiveRehabilitationPlatform.Services.Authentication
{
    /// <summary>
    /// Service for generating and validating JWT tokens for API authentication
    /// </summary>
    public class JwtTokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtTokenService> _logger;
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly int _jwtExpirationMinutes;

        public JwtTokenService(IConfiguration configuration, ILogger<JwtTokenService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _jwtSecret = _configuration["Jwt:Secret"] ?? "your-secret-key-min-32-characters-long!!!";
            _jwtIssuer = _configuration["Jwt:Issuer"] ?? "NeuroPathApp";
            _jwtAudience = _configuration["Jwt:Audience"] ?? "NeuroPathUsers";
            _jwtExpirationMinutes = int.TryParse(_configuration["Jwt:ExpirationMinutes"], out var exp) ? exp : 60;

            // Validate secret length
            if (_jwtSecret.Length < 32)
            {
                _logger.LogWarning("JWT Secret is shorter than 32 characters. This is NOT recommended for production.");
            }
        }

        /// <summary>
        /// Generate JWT token for a user
        /// </summary>
        public string GenerateToken(User user)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtSecret);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim("UserType", user.UserType ?? "User"),
                    new Claim("IsActive", user.IsActive.ToString())
                };

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
                    Issuer = _jwtIssuer,
                    Audience = _jwtAudience,
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key), 
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                _logger.LogInformation($"JWT token generated for user: {user.Username} (Type: {user.UserType})");
                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating JWT token: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Validate JWT token and extract claims
        /// </summary>
        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtSecret);

                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Token validation failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Extract user ID from token
        /// </summary>
        public int? GetUserIdFromToken(string token)
        {
            var principal = ValidateToken(token);
            if (principal == null)
                return null;

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                return userId;

            return null;
        }

        /// <summary>
        /// Extract user type from token
        /// </summary>
        public string? GetUserTypeFromToken(string token)
        {
            var principal = ValidateToken(token);
            if (principal == null)
                return null;

            var userTypeClaim = principal.FindFirst("UserType");
            return userTypeClaim?.Value;
        }
    }
}
