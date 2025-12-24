using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AdaptiveCognitiveRehabilitationPlatform.Services;

/// <summary>
/// Service to generate dynamic sorting game content using LM Studio AI
/// Creates different items and categories each game session
/// </summary>
public class DynamicSortingContentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DynamicSortingContentService> _logger;
    private readonly string _lmStudioUrl;

    public DynamicSortingContentService(HttpClient httpClient, ILogger<DynamicSortingContentService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _lmStudioUrl = "http://localhost:1234/v1/chat/completions";
    }

    /// <summary>
    /// Generate dynamic sorting content based on difficulty level
    /// </summary>
    public async Task<DynamicSortingContent?> GenerateDynamicContentAsync(int difficultyLevel)
    {
        try
        {
            _logger.LogInformation($"🤖 Generating dynamic sorting content for level {difficultyLevel}...");

            var prompt = BuildContentGenerationPrompt(difficultyLevel);
            var request = BuildChatCompletionRequest(prompt);

            var jsonContent = JsonSerializer.Serialize(request);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogDebug($"Sending request to LM Studio: {_lmStudioUrl}");

            var response = await _httpClient.PostAsync(_lmStudioUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"❌ LM Studio error: {response.StatusCode}");
                return GetDefaultContent(difficultyLevel);
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug($"Raw response: {responseContent}");

            var chatResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (chatResponse?.Choices?.Length == 0)
            {
                _logger.LogError("No choices in AI response");
                return GetDefaultContent(difficultyLevel);
            }

            var messageContent = chatResponse.Choices[0].Message?.Content ?? "";
            _logger.LogInformation($"✅ Dynamic Content Generated");

            return ParseContentResponse(messageContent);
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Exception during content generation: {ex.Message}");
            return GetDefaultContent(difficultyLevel);
        }
    }

    private string BuildContentGenerationPrompt(int difficultyLevel)
    {
        var itemCount = difficultyLevel switch
        {
            1 => 6,
            2 => 12,
            3 => 18,
            _ => 6
        };

        var categoryCount = difficultyLevel switch
        {
            1 => 2,
            2 => 3,
            3 => 4,
            _ => 2
        };

        return $@"Generate unique and fun sorting game content for a cognitive rehabilitation game.

Difficulty Level: {difficultyLevel}
Required Item Count: {itemCount}
Required Category Count: {categoryCount}

Please create NEW and DIFFERENT items and categories (NOT the same as previous sessions).
Make the items age-appropriate and educational.

Respond with ONLY valid JSON (no markdown, no extra text) following this exact structure:
{{
  ""categories"": [
    {{ ""name"": ""Category Name"" }},
    // ... {categoryCount} total categories
  ],
  ""items"": [
    {{ ""name"": ""Item Name"", ""category"": ""Category Name"" }},
    // ... {itemCount} total items
  ]
}}

Examples of varied content:
- Level 1: Mix of: Clothing/Furniture, Vehicles/Animals, Colors/Shapes
- Level 2: Mix of: Weather/Seasons, Sports/Music, Tools/Kitchen
- Level 3: Mix of: Emotions/Actions, Places/Buildings, Nature/Science

Generate fresh and different combinations!";
    }

    private ChatCompletionRequest BuildChatCompletionRequest(string prompt)
    {
        return new ChatCompletionRequest
        {
            Model = "phi-4",
            Messages = new[]
            {
                new Message
                {
                    Role = "system",
                    Content = "You are a creative game designer for educational cognitive rehabilitation games. Always respond with valid JSON only, no additional text."
                },
                new Message
                {
                    Role = "user",
                    Content = prompt
                }
            },
            Temperature = 0.9,  // Higher temperature for more variety
            MaxTokens = 1000,
            Stream = false
        };
    }

    private DynamicSortingContent? ParseContentResponse(string jsonContent)
    {
        try
        {
            // Clean up the response if it contains markdown code blocks
            var cleanJson = jsonContent
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            var result = JsonSerializer.Deserialize<DynamicSortingContent>(
                cleanJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result?.Categories != null && result.Items != null)
            {
                _logger.LogInformation($"✅ Successfully parsed dynamic content: {result.Categories.Count} categories, {result.Items.Count} items");
                return result;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Failed to parse content response: {ex.Message}");
            return null;
        }
    }

    private DynamicSortingContent GetDefaultContent(int difficultyLevel)
    {
        return difficultyLevel switch
        {
            1 => new DynamicSortingContent
            {
                Categories = new List<SortCategory>
                {
                    new SortCategory { Name = "Clothing" },
                    new SortCategory { Name = "Furniture" }
                },
                Items = new List<SortingItem>
                {
                    new SortingItem { Name = "Shirt", Category = "Clothing" },
                    new SortingItem { Name = "Pants", Category = "Clothing" },
                    new SortingItem { Name = "Hat", Category = "Clothing" },
                    new SortingItem { Name = "Chair", Category = "Furniture" },
                    new SortingItem { Name = "Table", Category = "Furniture" },
                    new SortingItem { Name = "Bed", Category = "Furniture" }
                }
            },
            2 => new DynamicSortingContent
            {
                Categories = new List<SortCategory>
                {
                    new SortCategory { Name = "Animals" },
                    new SortCategory { Name = "Vehicles" },
                    new SortCategory { Name = "Toys" }
                },
                Items = new List<SortingItem>
                {
                    new SortingItem { Name = "Dog", Category = "Animals" },
                    new SortingItem { Name = "Cat", Category = "Animals" },
                    new SortingItem { Name = "Lion", Category = "Animals" },
                    new SortingItem { Name = "Elephant", Category = "Animals" },
                    new SortingItem { Name = "Car", Category = "Vehicles" },
                    new SortingItem { Name = "Bicycle", Category = "Vehicles" },
                    new SortingItem { Name = "Airplane", Category = "Vehicles" },
                    new SortingItem { Name = "Train", Category = "Vehicles" },
                    new SortingItem { Name = "Ball", Category = "Toys" },
                    new SortingItem { Name = "Doll", Category = "Toys" },
                    new SortingItem { Name = "Puzzle", Category = "Toys" },
                    new SortingItem { Name = "Kite", Category = "Toys" }
                }
            },
            3 => new DynamicSortingContent
            {
                Categories = new List<SortCategory>
                {
                    new SortCategory { Name = "Sports" },
                    new SortCategory { Name = "Weather" },
                    new SortCategory { Name = "Emotions" },
                    new SortCategory { Name = "Colors" }
                },
                Items = new List<SortingItem>
                {
                    new SortingItem { Name = "Soccer", Category = "Sports" },
                    new SortingItem { Name = "Basketball", Category = "Sports" },
                    new SortingItem { Name = "Tennis", Category = "Sports" },
                    new SortingItem { Name = "Swimming", Category = "Sports" },
                    new SortingItem { Name = "Rain", Category = "Weather" },
                    new SortingItem { Name = "Snow", Category = "Weather" },
                    new SortingItem { Name = "Sunny", Category = "Weather" },
                    new SortingItem { Name = "Windy", Category = "Weather" },
                    new SortingItem { Name = "Happy", Category = "Emotions" },
                    new SortingItem { Name = "Sad", Category = "Emotions" },
                    new SortingItem { Name = "Excited", Category = "Emotions" },
                    new SortingItem { Name = "Calm", Category = "Emotions" },
                    new SortingItem { Name = "Red", Category = "Colors" },
                    new SortingItem { Name = "Blue", Category = "Colors" },
                    new SortingItem { Name = "Green", Category = "Colors" },
                    new SortingItem { Name = "Yellow", Category = "Colors" },
                    new SortingItem { Name = "Purple", Category = "Colors" },
                    new SortingItem { Name = "Orange", Category = "Colors" }
                }
            },
            _ => new DynamicSortingContent()
        };
    }

    // API Models
    public class ChatCompletionRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "phi-4";

        [JsonPropertyName("messages")]
        public Message[] Messages { get; set; } = Array.Empty<Message>();

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.9;

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 1000;

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;
    }

    public class Message
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
    }

    public class ChatCompletionResponse
    {
        [JsonPropertyName("choices")]
        public Choice[]? Choices { get; set; }
    }

    public class Choice
    {
        [JsonPropertyName("message")]
        public Message? Message { get; set; }
    }
}

/// <summary>
/// Dynamic sorting game content
/// </summary>
public class DynamicSortingContent
{
    [JsonPropertyName("categories")]
    public List<SortCategory> Categories { get; set; } = new();

    [JsonPropertyName("items")]
    public List<SortingItem> Items { get; set; } = new();
}

public class SortCategory
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}

public class SortingItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";
}
