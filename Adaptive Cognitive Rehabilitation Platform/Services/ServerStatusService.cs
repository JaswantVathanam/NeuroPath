using System.Diagnostics;

namespace AdaptiveCognitiveRehabilitationPlatform.Services
{
    /// <summary>
    /// Service to check and report AI server status on startup
    /// </summary>
    public class ServerStatusService : IHostedService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ServerStatusService> _logger;
        private const string LocalLMStudioEndpoint = "http://localhost:1234/v1/chat/completions";

        public ServerStatusService(HttpClient httpClient, ILogger<ServerStatusService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await CheckAIServerStatus();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task CheckAIServerStatus()
        {
            Console.WriteLine("\n" + new string('=', 70));
            Console.WriteLine("🔍 CHECKING AI SERVER STATUS ON STARTUP...");
            Console.WriteLine(new string('=', 70));

            var sw = Stopwatch.StartNew();
            try
            {
                // Create a minimal health check request
                var request = new
                {
                    model = "Phi-4-mini",
                    messages = new[] 
                    { 
                        new { role = "user", content = "Hello" }
                    },
                    max_tokens = 1
                };

                _logger.LogInformation("Attempting to connect to Phi-4-mini on {Endpoint}...", LocalLMStudioEndpoint);
                Console.WriteLine($"[STARTUP] 📡 Connecting to Phi-4-mini on {LocalLMStudioEndpoint}...");

                // Set a short timeout for health check
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await _httpClient.PostAsJsonAsync(LocalLMStudioEndpoint, request, cts.Token);

                sw.Stop();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[STARTUP] ✅ SUCCESS! Phi-4-mini is ONLINE and responding!");
                    Console.WriteLine($"[STARTUP] ⏱️  Response time: {sw.ElapsedMilliseconds}ms");
                    Console.WriteLine($"[STARTUP] 📍 Endpoint: {LocalLMStudioEndpoint}");
                    Console.WriteLine($"[STARTUP] 🤖 Model: Phi-4-mini");
                    Console.WriteLine("[STARTUP] 💚 AI auto-difficulty system is READY!");
                    _logger.LogInformation("✅ Phi-4-mini server is ONLINE (response time: {ResponseTimeMs}ms)", sw.ElapsedMilliseconds);
                }
                else
                {
                    sw.Stop();
                    Console.WriteLine($"[STARTUP] ⚠️  Server responded but with error status: {response.StatusCode}");
                    Console.WriteLine($"[STARTUP] ⏱️  Response time: {sw.ElapsedMilliseconds}ms");
                    Console.WriteLine("[STARTUP] 🔄 Will use BACKUP responses for AI encouragement");
                    _logger.LogWarning("[STARTUP] Phi-4-mini returned status {StatusCode}", response.StatusCode);
                }
            }
            catch (OperationCanceledException)
            {
                sw.Stop();
                Console.WriteLine($"[STARTUP] ⏰ Connection TIMEOUT after 5 seconds");
                Console.WriteLine($"[STARTUP] 🔴 Phi-4-mini server is OFFLINE or not responding");
                Console.WriteLine($"[STARTUP] 📍 Expected endpoint: {LocalLMStudioEndpoint}");
                Console.WriteLine("[STARTUP] 💡 Make sure LM Studio is running and has Phi-4-mini loaded");
                Console.WriteLine("[STARTUP] 🔄 Will use BACKUP responses - all games will still work!");
                _logger.LogWarning("[STARTUP] Phi-4-mini server OFFLINE - timeout after 5 seconds. Backup mode active.");
            }
            catch (HttpRequestException ex)
            {
                sw.Stop();
                Console.WriteLine($"[STARTUP] ❌ Connection FAILED");
                Console.WriteLine($"[STARTUP] 🔴 Phi-4-mini server is OFFLINE");
                Console.WriteLine($"[STARTUP] 📍 Expected endpoint: {LocalLMStudioEndpoint}");
                Console.WriteLine($"[STARTUP] 📝 Error: {ex.Message}");
                Console.WriteLine("[STARTUP] 💡 Make sure LM Studio is running with Phi-4-mini loaded");
                Console.WriteLine("[STARTUP] 🔄 Will use BACKUP responses - all games will still work!");
                _logger.LogError(ex, "[STARTUP] Phi-4-mini server connection failed. Backup mode active.");
            }
            catch (Exception ex)
            {
                sw.Stop();
                Console.WriteLine($"[STARTUP] ❌ Unexpected error: {ex.Message}");
                Console.WriteLine("[STARTUP] 🔄 Will use BACKUP responses - games will still function");
                _logger.LogError(ex, "[STARTUP] Unexpected error during server status check");
            }

            Console.WriteLine(new string('=', 70) + "\n");
        }
    }
}
