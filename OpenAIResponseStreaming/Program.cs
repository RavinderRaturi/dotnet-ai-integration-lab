
using OpenAI.Chat;
using System.Text;

var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
Console.WriteLine($"OPENAI_API_KEY set: {!string.IsNullOrEmpty(openAiKey)}");

var gptModel = "gpt-4o-mini";
var client = new ChatClient(gptModel, openAiKey);


Console.WriteLine("Enter your message (empty line to quit):");
Console.WriteLine("----------------------------------------");


var messages = new List<ChatMessage>();

//Initial system prompt to set the behavior of the assistant. This is an example without streaming. Uncomment the while loop to use it without streaming.
//while (true)
//{
//    Console.WriteLine("You: ");
//    var input = Console.ReadLine();
//    if (string.IsNullOrWhiteSpace(input))
//        break;

//    messages.Add(new UserChatMessage(input));
//    Console.WriteLine();

//    //To work without context, uncomment below line and comment the next input creation line
//    //var response = await client.CompleteChatAsync(input);

//    //To work with context, use the messages list
//    var response = await client.CompleteChatAsync(messages);

//    var aiRespone = response.Value.Content[0].Text;

//    Console.WriteLine(aiRespone);
//    Console.WriteLine();
//    messages.Add(new AssistantChatMessage(aiRespone));


//    //Token usage display
//    Console.WriteLine($"Total tokens: {response.Value.Usage.TotalTokenCount}");

//}



while (true)
{
    var sb = new StringBuilder();

    Console.WriteLine("You: ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
        break;
    messages.Add(new UserChatMessage(input));
    Console.WriteLine();
    Console.Write("AI: ");
    await foreach (var chunk in client.CompleteChatStreamingAsync(messages))
    {
        foreach (var c in chunk.ContentUpdate)
        {
            Console.Write(c.Text);
            sb.Append(c.Text);
        }

    }

    messages.Add(new AssistantChatMessage(sb.ToString()));
    Console.WriteLine();
    //Cant implement token usage display with streaming yet. look for third party libraries or wait for official support.
}




