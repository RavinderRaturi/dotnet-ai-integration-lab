using System;
using System.Text;
using System.Threading.Tasks;
using AgentsExamples.Common;



public static class Progress4
{
    public static async Task<int> Run(string openAiKey)
    {
        // Lesson 4 in the repo focuses on more complex reasoning controls.
        var modelSettings = new ModelSettings
        {
            Reasoning = new() { ["effort"] = "medium" },
            ExtraBody = new() { ["text"] = new Dictionary<string, string> { ["verbosity"] = "medium" } }
        };

        var tripAgent = new AgentDefinition(
            "Trip Coach",
            "You are a trip planning assistant. Prioritize safety, timing, and solo traveler needs. Check live weather first, then find activities and estimate durations. Use web_search if local events are required.",
            "GPT‑4o Mini",
            modelSettings
        );

        var city = "Mussoorie";
        var userPrompt = $@"I'm visiting {city} this weekend. Give me a schedule of activities for a single traveler for one day, include time estimates, and tell me what to pack given the current weather.";

        // fetch weather
        var weatherReport = await WeatherClient.GetWeatherForecastAsync(city);
        Console.WriteLine("Tool output (weather):");
        Console.WriteLine(weatherReport);

        // optionally perform a web search for top attractions
        var searchResult = await SearchClient.WebSearchAsync($"{city} top attractions");
        Console.WriteLine("Tool output (search) raw:");
        Console.WriteLine(searchResult);

        var fullUserPrompt = new StringBuilder();
        fullUserPrompt.AppendLine(userPrompt);
        fullUserPrompt.AppendLine();
        fullUserPrompt.AppendLine("-- Tool output: get_weather_forecast --");
        fullUserPrompt.AppendLine(weatherReport);
        fullUserPrompt.AppendLine();
        fullUserPrompt.AppendLine("-- Tool output: web_search(top attractions) --");
        fullUserPrompt.AppendLine(searchResult);

        var systemPrompt = tripAgent.Instructions + " Reasoning.Effort: medium. Response.Verbosity: medium.";

        var assistantReply = await OpenAiClient.SendChatAsync(systemPrompt, fullUserPrompt.ToString(), openAiKey, tripAgent.Model, tripAgent.ModelSettings);
        Console.WriteLine("=== Final Output ===");
        Console.WriteLine(assistantReply);
        return 0;
    }
}
