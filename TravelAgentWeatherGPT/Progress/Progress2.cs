using System;
using System.Text;
using System.Threading.Tasks;
using AgentsExamples.Common;

 

public static class Progress2
{
    public static async Task<int> Run(string openAiKey)
    {
        var tripAgent = new AgentDefinition(
            name: "Trip Coach",
            instructions: string.Concat(
                "You help travelers plan by checking real-time weather.",
                " When asked about weather or packing, call the get_weather_forecast tool.",
                " Make sure you have access to real-time weather data to make your recommendations. "),
            model: "GPT‑4o Mini"
        );

        var city = "Dehradun";
        var userPrompt = $"Headed to {city} today. What weather should I expect and what is the exact temperature right now? Also, what types of clothes should I pack";

        // tool heuristic
        var needsWeather = userPrompt.Contains("weather", StringComparison.OrdinalIgnoreCase)
                        || userPrompt.Contains("temperature", StringComparison.OrdinalIgnoreCase)
                        || userPrompt.Contains("pack", StringComparison.OrdinalIgnoreCase);

        string weatherReport = string.Empty;
        if (needsWeather)
        {
            weatherReport = await WeatherClient.GetWeatherForecastAsync(city);
            Console.WriteLine("Tool output (weather):");
            Console.WriteLine(weatherReport);
        }

        var fullUserPrompt = new StringBuilder();
        fullUserPrompt.AppendLine(userPrompt);
        if (!string.IsNullOrEmpty(weatherReport))
        {
            fullUserPrompt.AppendLine();
            fullUserPrompt.AppendLine("-- Tool output: get_weather_forecast --");
            fullUserPrompt.AppendLine(weatherReport);
        }

        // system prompt fallback steering
        var systemPrompt = tripAgent.Instructions + " Reasoning.Effort: low. Response.Verbosity: medium.";

        var assistantReply = await OpenAiClient.SendChatAsync(systemPrompt, fullUserPrompt.ToString(), openAiKey, tripAgent.Model, tripAgent.ModelSettings);
        Console.WriteLine("=== Final Output ===");
        Console.WriteLine(assistantReply);
        return 0;
    }
}
