using Microsoft.JSInterop;

namespace AdaptiveCognitiveRehabilitationPlatform.Services.Authentication
{
    /// <summary>
    /// HTTP message handler that automatically injects JWT tokens into outgoing API requests
    /// </summary>
    public class AuthenticationMessageHandler : DelegatingHandler
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<AuthenticationMessageHandler> _logger;

        public AuthenticationMessageHandler(IJSRuntime jsRuntime, ILogger<AuthenticationMessageHandler> logger)
        {
            _jsRuntime = jsRuntime;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                // Retrieve JWT token from localStorage (stored as "authToken" during login)
                var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");

                if (!string.IsNullOrEmpty(token))
                {
                    // Add authorization header with Bearer token
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    _logger.LogDebug($"JWT token added to request: {request.RequestUri}");
                }
                else
                {
                    _logger.LogWarning($"No JWT token found in localStorage for request: {request.RequestUri}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving JWT token: {ex.Message}");
            }

            // Send the request with the token
            var response = await base.SendAsync(request, cancellationToken);

            // Log response status
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"API response: {response.StatusCode} - {response.ReasonPhrase} for {request.RequestUri}");
            }

            return response;
        }
    }
}
