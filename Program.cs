using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine("ERROR: Set OPENAI_API_KEY environment variable and re-run.");
            return 1;
        }

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var endpoint = "https://api.openai.com/v1/chat/completions";
        var payload = new
        {
            model = "gpt-4o-mini", // if unavailable, replace with a model you have access to
            messages = new[]
            {
                new { role = "system", content = "You are a concise assistant." },
                new { role = "user", content = "Write a 3-line plan to learn AI integration for a senior .NET dev." }
            },
            max_tokens = 350,
            temperature = 0.2
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        Console.WriteLine("Calling the LLM...");
        var resp = await http.PostAsync(endpoint, content);
        var respText = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            Console.WriteLine("API call failed:");
            Console.WriteLine(resp.StatusCode);
            Console.WriteLine(respText);
            return 2;
        }

        try
        {
            using var doc = JsonDocument.Parse(respText);
            var choice = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
            Console.WriteLine("\n--- Response ---\n");
            Console.WriteLine(choice.Trim());
        }
        catch (Exception)
        {
            Console.WriteLine("Could not parse response; raw:");
            Console.WriteLine(respText);
        }

        return 0;
    }
}
