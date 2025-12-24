using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace NeuroPath.Services
{
    /// <summary>
    /// Centralized service for managing JWT authentication and user context.
    /// Handles token storage, retrieval, and automatic Bearer header attachment.
    /// </summary>
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;
        private string? _currentToken;
        private CurrentUser? _currentUser;

        public AuthService(HttpClient httpClient, IJSRuntime jsRuntime)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
        }

        /// <summary>
        /// Represents the currently authenticated user
        /// </summary>
        public class CurrentUser
        {
            public int UserId { get; set; }
            public string? Email { get; set; }
            public string? UserType { get; set; } // "User", "Parent"
            public bool IsAuthenticated => !string.IsNullOrEmpty(UserType);
        }

        /// <summary>
        /// Get the current authenticated user (if any)
        /// </summary>
        public async Task<CurrentUser> GetCurrentUserAsync()
        {
            if (_currentUser != null && _currentUser.IsAuthenticated)
                return _currentUser;

            try
            {
                _currentToken = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "authToken");
                
                if (string.IsNullOrEmpty(_currentToken))
                {
                    _currentUser = new CurrentUser();
                    return _currentUser;
                }

                // Decode JWT (basic parsing - in production, consider using a JWT library)
                _currentUser = DecodeToken(_currentToken);
                
                // Ensure Bearer header is set for subsequent requests
                AttachTokenToClient(_currentToken);
                
                return _currentUser;
            }
            catch
            {
                _currentUser = new CurrentUser();
                return _currentUser;
            }
        }

        /// <summary>
        /// Initialize auth service and attach token to HttpClient
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                _currentToken = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "authToken");
                
                if (!string.IsNullOrEmpty(_currentToken))
                {
                    AttachTokenToClient(_currentToken);
                    _currentUser = DecodeToken(_currentToken);
                }
            }
            catch
            {
                // Token not available or JSInterop not ready
            }
        }

        /// <summary>
        /// Store token in localStorage and update HttpClient headers
        /// </summary>
        public async Task SetTokenAsync(string token)
        {
            try
            {
                _currentToken = token;
                AttachTokenToClient(token);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", token);
                _currentUser = DecodeToken(token);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting token: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear token from localStorage and HttpClient headers
        /// </summary>
        public async Task LogoutAsync()
        {
            try
            {
                _currentToken = null;
                _currentUser = new CurrentUser();
                _httpClient.DefaultRequestHeaders.Authorization = null;
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
            }
            catch
            {
                // Ignore errors during logout
            }
        }

        /// <summary>
        /// Attach Bearer token to HttpClient default headers
        /// </summary>
        private void AttachTokenToClient(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        /// <summary>
        /// Basic JWT payload decode (reads the middle part only - no signature validation)
        /// In production, consider using System.IdentityModel.Tokens.Jwt
        /// </summary>
        private CurrentUser DecodeToken(string token)
        {
            try
            {
                var parts = token.Split('.');
                if (parts.Length != 3)
                    return new CurrentUser();

                // Decode the payload (second part)
                var payload = parts[1];
                // Add padding if needed
                var padded = payload + new string('=', (4 - payload.Length % 4) % 4);
                var decoded = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(padded));

                // Simple JSON parsing (could use JsonDocument or Newtonsoft.Json for robustness)
                var user = new CurrentUser();

                // Extract "sub" (UserId)
                var subMatch = System.Text.RegularExpressions.Regex.Match(decoded, @"""sub"":\s*""?(\d+)""?");
                if (subMatch.Success && int.TryParse(subMatch.Groups[1].Value, out var userId))
                    user.UserId = userId;

                // Extract "email"
                var emailMatch = System.Text.RegularExpressions.Regex.Match(decoded, @"""email"":\s*""([^""]+)""");
                if (emailMatch.Success)
                    user.Email = emailMatch.Groups[1].Value;

                // Extract "UserType" (User or Parent)
                var typeMatch = System.Text.RegularExpressions.Regex.Match(decoded, @"""UserType"":\s*""([^""]+)""");
                if (typeMatch.Success)
                    user.UserType = typeMatch.Groups[1].Value;

                return user;
            }
            catch
            {
                return new CurrentUser();
            }
        }
    }
}
