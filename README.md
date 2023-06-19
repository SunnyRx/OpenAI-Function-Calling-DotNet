# OpenaAI-Function-Calling-DotNet
This is a simple example of using DotNet to use OpenAI Function Calling.

Please refer to the comments in [Program.cs](https://github.com/SunnyRx/OpenaAI-Function-Calling-DotNet/blob/main/Program.cs) for an explanation of this example.

# Running the Example
To run this example, follow these steps:

1. Sign up for an account on OpenAI and get an API key
2. Configure the OpenAIClient with your API key
3. Run the example code
4. View the console output to see the conversation and function calls

After execution, you should see output similar to the following in the console:
```
System: You are a helpful weather assistant.
User: What's the weather like today?
Assistant: Sure, I can help you with that. May I know your current location? | Finish Reason: stop
User: I'm in Glasgow, Scotland
Assistant:  | Finish Reason: function_call
| Arguments {
  "location": "Glasgow, Scotland",
  "unit": "celsius"
}
Function: {
  "location": "Glasgow, Scotland",
  "unit": "celsius",
  "celsius": 22.5,
  "temperature": 72
}
Assistant: The current weather in Glasgow, Scotland is 22.5°C (72°F). The weather is quite pleasant today. | Finish Reason: stop
```
