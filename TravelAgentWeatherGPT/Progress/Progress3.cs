using System;
using System.Text;
using System.Threading.Tasks;
using AgentsExamples.Common;



public static class Progress3
{
    public static async Task<int> Run(string openAiKey)
    {
        // map the model_settings example from Lesson 3
        var modelSettings = new ModelSettings
        {
            Reasoning = new() { ["effort"] = "low" },
            ExtraBody = new() { ["text"] = new Dictionary<string, string> { ["verbosity"] = "low" } }
        };

        var tripAgent = new AgentDefinition(
            name: "Trip Coach",
            instructions: string.Concat(
                "You plan vacation activities for a single traveler, checking real-time weather first,",
                " and you recommend clothes to pack and activities to do while visiting the city.",
                " If web search is needed to find activities, use the web_search tool.",
                " Be concise and prioritize solo-friendly options."),
            model: "GPT‑4o Mini",
            ms: modelSettings
        );

        var city = "Dehradun";
        var userPrompt = $@"Headed to {city} today. What weather should I expect and what is the exact temperature right now?
Also, what types of clothes should I pack?
What activities do you recommend for a solo traveler while visiting the city?
If you need, use web search to find events or attractions.";

        // call weather tool first
        var weatherReport = await WeatherClient.GetWeatherForecastAsync(city);
        Console.WriteLine("Tool output (weather):");
        Console.WriteLine(weatherReport);

        var fullUserPrompt = new StringBuilder();
        fullUserPrompt.AppendLine(userPrompt);
        fullUserPrompt.AppendLine();
        fullUserPrompt.AppendLine("-- Tool output: get_weather_forecast --");
        fullUserPrompt.AppendLine(weatherReport);

        // system prompt includes steering fallback
        var systemPrompt = tripAgent.Instructions + " Reasoning.Effort: low. Response.Verbosity: low.";

        var assistantReply = await OpenAiClient.SendChatAsync(
    systemPrompt,
    fullUserPrompt.ToString(),
    openAiKey,
    tripAgent.Model,
    tripAgent.ModelSettings
);

        Console.WriteLine("=== Final Output ===");
        Console.WriteLine(assistantReply);
        return 0;
    }
}
