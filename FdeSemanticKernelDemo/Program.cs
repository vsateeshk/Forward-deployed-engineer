using System.Text;
using FdeSemanticKernelDemo.Plugins;
using FdeSemanticKernelDemo.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

Console.WriteLine("==============================================");
Console.WriteLine("Enterprise Customer Support & Legacy Order Triage Agent");
Console.WriteLine("==============================================");

IConfigurationRoot config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

var azureEndpoint = config["AzureOpenAI:Endpoint"];
var azureApiKey = config["AzureOpenAI:ApiKey"];
var azureDeploymentName = config["AzureOpenAI:DeploymentName"];
var openAiApiKey = config["OpenAI:ApiKey"];
var openAiModelId = config["OpenAI:ModelId"];

var kernelBuilder = Kernel.CreateBuilder();

kernelBuilder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

var selectedApiKey = !string.IsNullOrWhiteSpace(azureApiKey) ? azureApiKey : openAiApiKey;

if (string.IsNullOrWhiteSpace(selectedApiKey))
{
    Console.WriteLine();
    Console.WriteLine("No API key was found.");
    Console.WriteLine("Please update appsettings.json with an Azure OpenAI or OpenAI key before running this demo.");
    Console.WriteLine();
    Console.WriteLine("Example Azure config:");
    Console.WriteLine("  \"AzureOpenAI\": { \"Endpoint\": \"https://YOUR-RESOURCE.openai.azure.com/\", \"ApiKey\": \"...\", \"DeploymentName\": \"gpt-4o-mini\" }");
    Console.WriteLine();
    return;
}

if (!string.IsNullOrWhiteSpace(azureEndpoint) && !string.IsNullOrWhiteSpace(azureApiKey) && !string.IsNullOrWhiteSpace(azureDeploymentName))
{
    // Azure OpenAI is often used in enterprise environments, so this is the most realistic option for client sites.
    kernelBuilder.AddAzureOpenAIChatCompletion(
        deploymentName: azureDeploymentName,
        endpoint: azureEndpoint,
        apiKey: azureApiKey);
}
else if (!string.IsNullOrWhiteSpace(openAiApiKey) && !string.IsNullOrWhiteSpace(openAiModelId))
{
    // OpenAI fallback for learners using a standard API key.
    kernelBuilder.AddOpenAIChatCompletion(
        modelId: openAiModelId,
        apiKey: openAiApiKey);
}
else
{
    Console.WriteLine("The appsettings file was found, but the configured model settings are incomplete.");
    Console.WriteLine("Please provide either AzureOpenAI or OpenAI settings.");
    return;
}

var kernel = kernelBuilder.Build();
var legacyOrderDatabase = new LegacyOrderDatabase();
var enterpriseOrderPlugin = new EnterpriseOrderPlugin(legacyOrderDatabase);

// Import the plugin so Semantic Kernel can auto-discover it and decide when to call the methods.
// The plugin name matters because it becomes part of the prompt contract the model reasons over.
kernel.ImportPluginFromObject(enterpriseOrderPlugin, "EnterpriseOrder");

var chatService = kernel.GetRequiredService<IChatCompletionService>();

var systemPrompt = @"
You are an Enterprise Triage Agent for a retail company.

Your job is to help customer support by:
1. Looking up the order information using the legacy order system before making decisions.
2. Reading the company return policy document to ground your answer in official policy.
3. Deciding whether the request is eligible for a return, refund, or manager escalation.
4. Writing a professional customer-facing reply.
5. Escalating to a human manager when required by policy, unavailable facts, or high-value orders.

Important rules:
- Before replying, verify the order details and the policy.
- Do not invent facts.
- If the order is missing, the return window is expired, or the amount exceeds the policy threshold, escalate with a clear reason.
- If the return request is eligible and policy allows it, propose a helpful customer response.
- Keep your answer concise but professional.
- Explain when policy requires manager approval.
";

var sampleEmails = new Dictionary<string, string>
{
    ["1"] = "Hi support team, I ordered a pair of wireless headphones on October 12, 2026. My order ID is ORD-1001. I am a VIP customer and the item is still unopened. I want to return it because I changed my mind. Can you help?",
    ["2"] = "Hello, I purchased an office chair on September 15, 2026 with order ID ORD-1002. It arrived on time, but I am trying to return it now. Please advise if this is eligible. I am not a VIP member.",
    ["3"] = "Customer support, I ordered a smartphone case on November 1, 2026 with order ID ORD-1003. The item arrived defective and I am a VIP member. I would like a replacement or refund. Please review and tell me the next step.",
    ["4"] = "I need help with order ORD-1004. It was a 4K monitor for $899. I want to return it because I changed my mind, but I am not sure if it is allowed. This is a large purchase and I would appreciate a quick answer."
};

Console.WriteLine();
Console.WriteLine("Choose a sample customer email:");
foreach (var item in sampleEmails)
{
    Console.WriteLine($"  {item.Key}. {item.Value.Substring(0, Math.Min(item.Value.Length, 80))}...");
}
Console.WriteLine("  custom. Type your own email message");
Console.WriteLine("  exit. Quit the app");

while (true)
{
    Console.WriteLine();
    Console.Write("Your choice: ");
    var choice = Console.ReadLine();

    if (string.Equals(choice, "exit", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Exiting the demo. Thanks for trying the FDE Semantic Kernel example.");
        break;
    }

    var userMessage = sampleEmails.TryGetValue(choice ?? string.Empty, out var email) ? email : choice;

    if (string.IsNullOrWhiteSpace(userMessage))
    {
        Console.WriteLine("Please choose a sample email or type a custom message.");
        continue;
    }

    Console.WriteLine();
    Console.WriteLine("--- Customer Email ---");
    Console.WriteLine(userMessage);
    Console.WriteLine("---------------------");

    var chatHistory = new ChatHistory();
    chatHistory.AddSystemMessage(systemPrompt);
    chatHistory.AddUserMessage(userMessage);

    var executionSettings = new OpenAIPromptExecutionSettings
    {
        Temperature = 0.2,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    };

    Console.WriteLine();
    Console.WriteLine("Calling Semantic Kernel with automatic function selection...");

    try
    {
        // The model can decide to call the plugin methods automatically when facts are needed.
        // This is the heart of the FDE pattern: AI is guided by real business tools, not freeform guessing.
        var response = await chatService.GetChatMessageContentAsync(
            chatHistory,
            executionSettings,
            kernel);

        Console.WriteLine();
        Console.WriteLine("=== Agent Response ===");
        Console.WriteLine(response.Content ?? "No response returned.");
    }
    catch (Exception ex)
    {
        Console.WriteLine();
        Console.WriteLine($"The AI request failed. Check your API key and model configuration. Details: {ex.Message}");
    }
}
