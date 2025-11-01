using System;
using DotNetEnv;


class Program
{
    // Simple runner. Pass lesson number as arg: 2,3,4,5
    public static async Task<int> Main(string[] args)
    {
        // load .env if exists
        Env.Load();

        var lesson = args.Length > 0 ? args[0] : "5";
        Console.WriteLine($"Running Lesson {lesson} example");

        var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
        Console.WriteLine($"OPENAI_API_KEY set: {!string.IsNullOrEmpty(openAiKey)}");
        Console.WriteLine($"WEATHER_API_KEY set: {!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEATHER_API_KEY"))}");
  //      Console.WriteLine($"BING_API_KEY set: {!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BING_API_KEY"))}");

        return lesson switch
        {
            "2" => await Progress2.Run(openAiKey),
            "3" => await Progress3.Run(openAiKey),
            "4" => await Progress4.Run(openAiKey),
            "5" => await Progress5.Run(openAiKey),
            _ => await Progress2.Run(openAiKey)
        };
    }
}
