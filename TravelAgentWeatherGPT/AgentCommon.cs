using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AgentsExamples.Common
{
    // small model types
    public record WeatherInfo(string City, string Country, double TempF, string Condition);

    public class ModelSettings
    {
        [JsonPropertyName("reasoning")]
        public Dictionary<string, string>? Reasoning { get; set; }

        [JsonPropertyName("extra_body")]
        public Dictionary<string, object>? ExtraBody { get; set; }
    }

    public class AgentDefinition
    {
        public string Name { get; set; }
        public string Instructions { get; set; }
        public string Model { get; set; }
        public ModelSettings? ModelSettings { get; set; }
        public AgentDefinition(string name, string instructions, string model, ModelSettings? ms = null)
        {
            Name = name;
            Instructions = instructions;
            Model = model;
            ModelSettings = ms;
        }
    }

    // Weather client
    public static class WeatherClient
    {
        private static readonly HttpClient _http = new();

        public static async Task<string> GetWeatherForecastAsync(string city)
        {
            var apiKey = Environment.GetEnvironmentVariable("WEATHER_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
                return "WEATHER_API_KEY not set in environment variables.";

            var baseUrl = "https://api.weatherapi.com/v1/current.json";
            var url = $"{baseUrl}?q={Uri.EscapeDataString(city)}&aqi=no&key={Uri.EscapeDataString(apiKey)}";

            try
            {
                using var resp = await _http.GetAsync(url).ConfigureAwait(false);
                resp.EnsureSuccessStatusCode();
                var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("location", out var location) || !root.TryGetProperty("current", out var current))
                    return $"Could not retrieve weather for '{city}'. Try a more specific place name.";

                var weather = new WeatherInfo(
                    location.GetProperty("name").GetString() ?? city,
                    location.GetProperty("country").GetString() ?? "",
                    current.GetProperty("temp_f").GetDouble(),
                    current.GetProperty("condition").GetProperty("text").GetString() ?? ""
                );

                var today = DateTime.Today.ToString("yyyy-MM-dd");
                var sb = new StringBuilder();
                sb.AppendLine($"Real-time weather report for {today}:");
                sb.AppendLine($"   - City: {weather.City}");
                sb.AppendLine($"   - Country: {weather.Country}");
                sb.AppendLine($"   - Temperature: {weather.TempF:F1} °F");
                sb.AppendLine($"   - Weather Conditions: {weather.Condition}");
                return sb.ToString();
            }
            catch (HttpRequestException e)
            {
                return $"Error fetching weather data: {e.Message}";
            }
        }
    }

    // Simple Bing Web Search client (requires BING_API_KEY)
    public static class SearchClient
    {
        private static readonly HttpClient _http = new();

        public static async Task<string> WebSearchAsync(string query)
        {
            var bingKey = Environment.GetEnvironmentVariable("BING_API_KEY");
            if (string.IsNullOrWhiteSpace(bingKey))
                return "BING_API_KEY not set in environment variables.";

            var endpoint = $"https://api.bing.microsoft.com/v7.0/search?q={Uri.EscapeDataString(query)}";
            _http.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");
            _http.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", bingKey);

            try
            {
                using var resp = await _http.GetAsync(endpoint).ConfigureAwait(false);
                resp.EnsureSuccessStatusCode();
                var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                // For brevity just return the top snippet. Real usage should parse and extract relevant fields.
                return json;
            }
            catch (HttpRequestException e)
            {
                return $"Error calling Bing Search: {e.Message}";
            }
        }
    }

    // OpenAI client via HTTP. Best-effort insertion of model_settings.
    public static class OpenAiClient
    {
        private static readonly HttpClient _http = new();

        public static async Task<string> SendChatAsync(
      string systemPrompt,
      string userPrompt,
      string apiKey,
      string model = "GPT‑4o Mini",
      ModelSettings? modelSettings = null)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return "OPENAI_API_KEY not set in environment variables.";

            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            // Build steering text from modelSettings (fallback approach).
            if (modelSettings is not null)
            {
                var sb = new StringBuilder();
                if (modelSettings.Reasoning is not null)
                {
                    foreach (var kv in modelSettings.Reasoning)
                        sb.AppendLine($"Reasoning.{kv.Key}: {kv.Value}.");
                }

                if (modelSettings.ExtraBody is not null)
                {
                    foreach (var kv in modelSettings.ExtraBody)
                    {
                        // Handle simple dictionary payloads (most common case used in lessons)
                        if (kv.Value is Dictionary<string, string> dict)
                        {
                            foreach (var d in dict)
                                sb.AppendLine($"{kv.Key}.{d.Key}: {d.Value}.");
                        }
                        else
                        {
                            // Fallback: stringify non-dictionary values
                            sb.AppendLine($"{kv.Key}: {kv.Value}");
                        }
                    }
                }

                var steering = sb.ToString().Trim();
                if (!string.IsNullOrEmpty(steering))
                {
                    systemPrompt = systemPrompt + "\nSteering instructions for model:\n" + steering;
                }
            }

            // Build standard OpenAI Chat Completions payload. Do NOT add unknown top-level fields.
            var payload = new Dictionary<string, object?>
            {
                ["model"] = model,
                ["messages"] = new object[]
                {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = userPrompt }
                }                
            };

            var opts = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            var json = JsonSerializer.Serialize(payload, opts);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await _http.PostAsync("https://api.openai.com/v1/chat/completions", content).ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                return $"OpenAI error {resp.StatusCode}: {err}";
            }

            var respJson = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var doc = JsonDocument.Parse(respJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var message = choices[0].GetProperty("message").GetProperty("content").GetString();
                return message ?? string.Empty;
            }

            return "No response from OpenAI.";
        }

    }
}
