using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenAI;
using OpenAI.Chat;

class Program
{
    // Example fake function that returns the same weather information
    // In a production environment, this could be your backend API or an external API
    private static string GetCurrentWeather(string location, string unit = "fahrenheit")
    {
        var weatherInfo = new JsonObject
        {
            ["location"] = location,
            ["unit"] = unit,
            ["celsius"] = 22.5,
            ["temperature"] = 72
        };

        if (unit == "celsius")
        {
            weatherInfo["unit"] = "celsius";
        }
        else
        {
            weatherInfo["unit"] = "temperature";
        }

        return weatherInfo.ToString();
    }

    private static async Task Main(string[] args)
    {
        // Configure OpenAI Key
        var api = new OpenAIClient("sk-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx");

        // Create function specifications to interface with a hypothetical weather API
        var functions = new List<Function>
        {
            new Function(
                nameof(GetCurrentWeather),
                "Get the current weather in a given location",
                new JsonObject
                {
                    ["type"] = JsonNode.Parse("\"object\""),
                    ["properties"] = new JsonObject
                    {
                        ["location"] = new JsonObject
                        {
                            ["type"] = JsonNode.Parse("\"string\""),
                            ["description"] = JsonNode.Parse("\"The city and state, e.g. San Francisco, CA\"")
                        },
                        ["unit"] = new JsonObject
                        {
                            ["type"] = JsonNode.Parse("\"string\""),
                            ["description"] =
                                JsonNode.Parse(
                                    "\"The temperature unit to use. Infer this from the users location.\""),
                            ["enum"] = new JsonArray
                                {JsonNode.Parse("\"fahrenheit\""), JsonNode.Parse("\"celsius\"")}
                        }
                    },
                    ["required"] = new JsonArray {JsonNode.Parse("\"location\""), JsonNode.Parse("\"unit\"")}
                }
            )
        };

        // Generate the initial prompt
        var messages = new List<Message>
        {
            new Message(Role.System, "You are a helpful weather assistant."),
            new Message(Role.User, "What's the weather like today?"),
        };
        Console.WriteLine(
            $"{messages[0].Role}: {messages[0].Content}"); // Print immediately for better understanding. The same applies to print statements below.
        Console.WriteLine($"{messages[1].Role}: {messages[1].Content}");

        // Send the information to GPT and get a response. It's likely that GPT will ask for the location here.
        // When sending, pass the function specifications we created earlier to GPT, so it knows which functions it can call.
        var chatRequest = new ChatRequest(messages, functions: functions, functionCall: "auto",
            model: "gpt-3.5-turbo-0613");
        var result = await api.ChatEndpoint.GetCompletionAsync(chatRequest);
        messages.Add(result.FirstChoice.Message);
        Console.WriteLine(
            $"{result.FirstChoice.Message.Role}: {result.FirstChoice.Message.Content} | Finish Reason: {result.FirstChoice.FinishReason}");

        // Respond to GPT with a location, using the example location from the official example
        var locationMessage = new Message(Role.User, "I'm in Glasgow, Scotland");
        messages.Add(locationMessage);
        Console.WriteLine($"{locationMessage.Role}: {locationMessage.Content}");

        // Send the information to GPT and get a response. It's likely that the response content will be empty, and GPT will determine that it should call the function. In this case, the Finish Reason will be function_call.
        // Note: If the Finish Reason is not function_call, the assistant might also ask you which units you want the temperature in. It's best to add a check for the Finish Reason, but it's not demonstrated in this example.
        chatRequest = new ChatRequest(messages, functions: functions, functionCall: "auto",
            model: "gpt-3.5-turbo-0613");
        result = await api.ChatEndpoint.GetCompletionAsync(chatRequest);
        Console.WriteLine(
            $"{result.FirstChoice.Message.Role}: {result.FirstChoice.Message.Content} | Finish Reason: {result.FirstChoice.FinishReason}");

        // Important: When GPT's Finish Reason is function_call, the content must be null. If you don't handle this and add it directly to messages before submitting to GPT, it will trigger an error: 'content' is a required property - 'messages.4'"
        // So we need to create a new Message, keeping all the content from result.FirstChoice.Message except the content, which should be left as an empty string or replaced with other text such as 'Okay, please wait a moment.'
        var tmpMessage = new Message(result.FirstChoice.Message.Role, string.Empty, result.FirstChoice.Message.Name,
            result.FirstChoice.Message.Function);
        messages.Add(tmpMessage);

        // When the Finish Reason of GPT is function_call, we can determine what function GPT wants to call through result.FirstChoice.Message.Function.Name. In this example, since we only have one function, we don't need to add another check.
        // At the same time, the arguments necessary for calling the corresponding function will be stored in result.FirstChoice.Message.Function.Arguments.
        Console.WriteLine(
            $"| Arguments {result.FirstChoice.Message.Function.Arguments}"); // Print it out and take a look at the parameters.

        // Next, we extract result.FirstChoice.Message.Function.Arguments and call the Function to retrieve weather information.
        var functionArgs =
            JsonConvert.DeserializeObject<dynamic>(result.FirstChoice.Message.Function.Arguments.ToString());
        var location = (string) functionArgs.location;
        var unit = (string) functionArgs.unit;
        var functionResult = GetCurrentWeather(location, unit);

        // Then, we package the returned result (functionResult) into information and pass it back to GPT.
        // Role represents the Function, Content represents the functionResult, and don't forget to include the Name of the Function at the end.
        var functionMessage = new Message(Role.Function, functionResult, result.FirstChoice.Message.Function.Name);
        messages.Add(functionMessage);
        Console.WriteLine($"{functionMessage.Role}: {functionMessage.Content}");

        // Finally, we pass the information to GPT, which replies based on the Json content, and we've successfully completed the task.
        chatRequest = new ChatRequest(messages, functions: functions, functionCall: "auto",
            model: "gpt-3.5-turbo-0613");
        result = await api.ChatEndpoint.GetCompletionAsync(chatRequest);
        Console.WriteLine(
            $"{result.FirstChoice.Message.Role}: {result.FirstChoice.Message.Content} | Finish Reason: {result.FirstChoice.FinishReason}");
    }
}