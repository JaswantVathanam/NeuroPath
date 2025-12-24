using Microsoft.AspNetCore.Mvc;
using AdaptiveCognitiveRehabilitationPlatform.Services.Authentication;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AdaptiveCognitiveRehabilitationPlatform.Controllers
{
    /// <summary>
    /// JSON-based authentication controller for demo/presentation purposes
    /// Uses users.json file instead of database
    /// </summary>
    [ApiController]
    [Route("api/json-auth")]
    public class JsonAuthController : ControllerBase
    {
        private readonly IJsonUserService _userService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<JsonAuthController> _logger;

        public JsonAuthController(
            IJsonUserService userService,
            IConfiguration configuration,
            ILogger<JsonAuthController> logger)
        {
            _userService = userService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Login with JSON user credentials
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] JsonLoginRequest request)
        {
            try
            {
                _logger.LogInformation($"JSON Login attempt for: {request.Username}");

                var result = await _userService.AuthenticateAsync(request.Username, request.Password);

                if (!result.Success || result.User == null)
                {
                    return Unauthorized(new JsonLoginResponse
                    {
                        Success = false,
                        Message = result.Message
                    });
                }

                // Generate JWT token
                var token = GenerateJwtToken(result.User);

                _logger.LogInformation($"✅ JSON Login successful for: {result.User.FullName}");

                return Ok(new JsonLoginResponse
                {
                    Success = true,
                    Message = "Login successful",
                    Token = token,
                    User = new JsonUserResponse
                    {
                        UserId = result.User.UserId,
                        Username = result.User.Username,
                        FullName = result.User.FullName,
                        Email = result.User.Email,
                        UserType = result.User.UserType
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"JSON Login error: {ex.Message}");
                return StatusCode(500, new JsonLoginResponse
                {
                    Success = false,
                    Message = "An error occurred during login"
                });
            }
        }

        /// <summary>
        /// Get all users (for demo display)
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                var response = users.Select(u => new JsonUserResponse
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    FullName = u.FullName,
                    Email = u.Email,
                    UserType = u.UserType
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting users: {ex.Message}");
                return StatusCode(500, new { message = "Error retrieving users" });
            }
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpGet("users/{userId}")]
        public async Task<IActionResult> GetUserById(int userId)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(new JsonUserResponse
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    FullName = user.FullName,
                    Email = user.Email,
                    UserType = user.UserType
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user {userId}: {ex.Message}");
                return StatusCode(500, new { message = "Error retrieving user" });
            }
        }

        private string GenerateJwtToken(JsonUserEntry user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = jwtSettings["Secret"] ?? "your-super-secret-key-here-change-in-production";
            var issuer = jwtSettings["Issuer"] ?? "NeuroPathApp";
            var audience = jwtSettings["Audience"] ?? "NeuroPathAppUsers";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.UserType),
                new Claim("FullName", user.FullName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    // Request/Response DTOs
    public class JsonLoginRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class JsonLoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string? Token { get; set; }
        public JsonUserResponse? User { get; set; }
    }

    public class JsonUserResponse
    {
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string UserType { get; set; } = "";
    }
}
