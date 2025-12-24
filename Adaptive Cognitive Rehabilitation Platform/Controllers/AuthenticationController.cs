using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using NeuroPath.Models;
using AdaptiveCognitiveRehabilitationPlatform.Services.Authentication;
using AdaptiveCognitiveRehabilitationPlatform.Services;

namespace AdaptiveCognitiveRehabilitationPlatform.Controllers
{
    /// <summary>
    /// Authentication API controller for login and registration
    /// NOW USES JSON-BASED AUTHENTICATION (SQL commented out)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        // SQL-dependent service - commented out for JSON-only mode
        // private readonly IdentityService _identityService;
        private readonly IJsonUserService _jsonUserService;
        private readonly JwtTokenService _jwtTokenService;
        private readonly ILogger<AuthenticationController> _logger;

        public AuthenticationController(
            // IdentityService identityService, // SQL-dependent - commented out
            IJsonUserService jsonUserService,
            JwtTokenService jwtTokenService,
            ILogger<AuthenticationController> logger)
        {
            // _identityService = identityService; // SQL-dependent - commented out
            _jsonUserService = jsonUserService;
            _jwtTokenService = jwtTokenService;
            _logger = logger;
        }

        /// <summary>
        /// User login endpoint - NOW USES JSON AUTHENTICATION
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                    return BadRequest(new ErrorResponse { Error = "Username and password are required" });

                // JSON-based authentication
                var authResult = await _jsonUserService.AuthenticateAsync(request.Username, request.Password);

                if (!authResult.Success || authResult.User == null)
                {
                    _logger.LogWarning($"Failed login attempt for username: {request.Username}");
                    return Unauthorized(new ErrorResponse { Error = authResult.Message ?? "Invalid credentials" });
                }

                // Create a User object for JWT token generation
                var user = new User
                {
                    UserId = authResult.User.UserId,
                    Username = authResult.User.Username,
                    Email = authResult.User.Email,
                    UserType = authResult.User.UserType,
                    IsActive = authResult.User.IsActive
                };

                var token = _jwtTokenService.GenerateToken(user);

                return Ok(new LoginResponse
                {
                    Success = true,
                    Message = "Login successful",
                    Token = token,
                    User = new UserDto
                    {
                        UserId = authResult.User.UserId,
                        Username = authResult.User.Username,
                        Email = authResult.User.Email,
                        UserType = authResult.User.UserType,
                        IsActive = authResult.User.IsActive,
                        FullName = authResult.User.FullName
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in login endpoint: {ex.Message}");
                return StatusCode(500, new ErrorResponse { Error = "An error occurred during login" });
            }
        }

        /// <summary>
        /// User registration endpoint - JSON-based (simplified)
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                // For JSON-only mode, registration is disabled (use pre-defined users)
                _logger.LogWarning("Registration attempted but JSON-only mode is active. Use pre-defined users.");
                return BadRequest(new ErrorResponse { 
                    Error = "Registration is disabled in demo mode. Please use one of the demo accounts." 
                });

                /* SQL-based registration - commented out
                if (string.IsNullOrWhiteSpace(request.Username) || 
                    string.IsNullOrWhiteSpace(request.Email) || 
                    string.IsNullOrWhiteSpace(request.Password))
                    return BadRequest(new ErrorResponse { Error = "Username, email, and password are required" });

                var userType = "User";
                if (!string.IsNullOrEmpty(request.UserType) && (request.UserType == "User" || request.UserType == "Parent"))
                    userType = request.UserType;

                var (success, message, user) = await _identityService.RegisterUserAsync(
                    request.Username, 
                    request.Email, 
                    request.Password, 
                    userType);

                if (!success || user == null)
                {
                    _logger.LogWarning($"Failed registration attempt for username: {request.Username}");
                    return BadRequest(new ErrorResponse { Error = message });
                }

                _logger.LogInformation($"New user registered: {user.Username}");

                return CreatedAtAction(nameof(Login), new RegisterResponse
                {
                    Success = true,
                    Message = message,
                    User = new UserDto
                    {
                        UserId = user.UserId,
                        Username = user.Username,
                        Email = user.Email,
                        UserType = user.UserType ?? "User",
                        IsActive = user.IsActive
                    }
                });
                */
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in register endpoint: {ex.Message}");
                return StatusCode(500, new ErrorResponse { Error = "An error occurred during registration" });
            }
        }

        /// <summary>
        /// Get current user profile - JSON-based
        /// </summary>
        [HttpGet("me")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                    return Unauthorized(new ErrorResponse { Error = "User not authenticated" });

                var user = await _jsonUserService.GetUserByIdAsync(userId);
                if (user == null)
                    return NotFound(new ErrorResponse { Error = "User not found" });

                return Ok(new UserDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    UserType = user.UserType,
                    IsActive = user.IsActive,
                    FullName = user.FullName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting current user: {ex.Message}");
                return StatusCode(500, new ErrorResponse { Error = "An error occurred" });
            }
        }

        /// <summary>
        /// Get all available demo users (for testing)
        /// </summary>
        [HttpGet("demo-users")]
        [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDemoUsers()
        {
            try
            {
                var users = await _jsonUserService.GetAllUsersAsync();
                var demoUsers = users.Select(u => new UserDto
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    UserType = u.UserType,
                    IsActive = u.IsActive,
                    FullName = u.FullName
                }).ToList();

                return Ok(demoUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting demo users: {ex.Message}");
                return StatusCode(500, new ErrorResponse { Error = "An error occurred" });
            }
        }

        /// <summary>
        /// Verify token validity
        /// </summary>
        [HttpPost("verify")]
        [ProducesResponseType(typeof(VerifyTokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public IActionResult VerifyToken([FromBody] VerifyTokenRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Token))
                    return BadRequest(new ErrorResponse { Error = "Token is required" });

                var principal = _jwtTokenService.ValidateToken(request.Token);
                if (principal == null)
                    return Unauthorized(new ErrorResponse { Error = "Invalid or expired token" });

                var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                var userTypeClaim = principal.FindFirst("UserType");

                return Ok(new VerifyTokenResponse
                {
                    Valid = true,
                    UserId = int.TryParse(userIdClaim?.Value, out var uid) ? uid : 0,
                    UserType = userTypeClaim?.Value ?? "User"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error verifying token: {ex.Message}");
                return BadRequest(new ErrorResponse { Error = "Error verifying token" });
            }
        }
    }

    // Request DTOs
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public string UserType { get; set; } = "User";
    }

    public class VerifyTokenRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }

    // Response DTOs
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public UserDto? User { get; set; }
    }

    public class RegisterResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public UserDto? User { get; set; }
    }

    public class VerifyTokenResponse
    {
        public bool Valid { get; set; }
        public int UserId { get; set; }
        public string UserType { get; set; } = string.Empty;
    }

    public class UserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string FullName { get; set; } = string.Empty;
    }

    public class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
    }
}

