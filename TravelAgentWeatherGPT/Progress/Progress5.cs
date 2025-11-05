using System;
using System.Text;
using System.Threading.Tasks;
using AgentsExamples.Common;



public static class Progress5
{
    public static async Task<int> Run(string openAiKey)
    {
        // Lesson 5 often demonstrates chaining tools and more advanced orchestration
        var modelSettings = new ModelSettings
        {
            Reasoning = new() { ["effort"] = "high" },
            ExtraBody = new() { ["text"] = new Dictionary<string, string> { ["verbosity"] = "high" } }
        };

        var tripAgent = new AgentDefinition(
            "Trip Coach",
            "You coordinate weather, web search, and packing. For any ambiguous place, ask clarifying questions. Provide step-by-step plan and prioritized packing list.",
            "GPT‑4o Mini",
            modelSettings
        );

        var city = "Chandigarh";
        var userPrompt = $@"I'm traveling to {city} next week for two days. Provide a day-by-day itinerary, suggest clothes and gear to pack, and list local events if any.";

        // 1. Get weather
        var weatherReport = await WeatherClient.GetWeatherForecastAsync(city);
        Console.WriteLine("Tool output (weather):");
        Console.WriteLine(weatherReport);

        // 2. Search for events
        var searchResult = await SearchClient.WebSearchAsync($"{city} events this week");
        Console.WriteLine("Tool output (search) raw:");
        Console.WriteLine(searchResult);

        // 3. Build prompt with outputs
        var fullUserPrompt = new StringBuilder();
        fullUserPrompt.AppendLine(userPrompt);
        fullUserPrompt.AppendLine();
        fullUserPrompt.AppendLine("-- Tool output: get_weather_forecast --");
        fullUserPrompt.AppendLine(weatherReport);
        fullUserPrompt.AppendLine();
        fullUserPrompt.AppendLine("-- Tool output: web_search(events) --");
        fullUserPrompt.AppendLine(searchResult);

        var systemPrompt = tripAgent.Instructions + " Reasoning.Effort: high. Response.Verbosity: high.";

        var assistantReply = await OpenAiClient.SendChatAsync(systemPrompt, fullUserPrompt.ToString(), openAiKey, tripAgent.Model, tripAgent.ModelSettings);
        Console.WriteLine("=== Final Output ===");
        Console.WriteLine(assistantReply);
        return 0;
    }
}
