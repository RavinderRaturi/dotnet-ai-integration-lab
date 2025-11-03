using LLama;
using LLama.Common;

var directory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.ToString();
var modelPath = Path.Combine(directory, "phi-2.Q2_K.gguf");


var parameters = new ModelParams(modelPath)
{
    ContextSize = 4096,
    GpuLayerCount = 5,
};



using var model = LLamaWeights.LoadFromFile(parameters);

using var context = model.CreateContext(parameters);

var executor = new InteractiveExecutor(context);

var chatHistory = new ChatHistory();

chatHistory.AddMessage(AuthorRole.System, @"Transcript of a dialog, where the User interacts with an Assistant named Jeff.
Jeff's role is to be helpful, provide answers straight to the point, and maintain a kind tone in all interactions.
Users may ask Jeff to draft emails or messages, so please ensure that your responses are clear, professional, and considerate.
Stick strictly to the information provided and do not add any additional commentary or details beyond the task at hand.
When asked to make a list, please provide only response with the list no additional information.
When given specific instructions, such as providing a list or a certain number of items, ensure you follow those instructions exactly.
Remember, your goal is to assist in the best way possible while making communication effective and pleasant.
Always keep the answer to the point with no additional information. Users only want the answers to their questions, nothing more.");

chatHistory.AddMessage(AuthorRole.User, "Hello, Ibex.");

chatHistory.AddMessage(AuthorRole.Assistant, "Hello. How may I help you today?");


var session = new ChatSession(executor, chatHistory);

var inferenceParams = new InferenceParams()
{
    MaxTokens = 1024,
    AntiPrompts = new List<string> { "User:" }
};


Console.ForegroundColor = ConsoleColor.Yellow;
Console.Write($"The chat is ready. {Environment.NewLine}User:");
Console.ForegroundColor = ConsoleColor.Green;
var userinput = Console.ReadLine() ?? string.Empty;

while (userinput != "stop")
{
    await foreach (var text in session.ChatAsync(
        new ChatHistory.Message(AuthorRole.User, userinput), inferenceParams))
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(text);
    }

    Console.ForegroundColor = ConsoleColor.Green;
    userinput = Console.ReadLine() ?? "";
}

