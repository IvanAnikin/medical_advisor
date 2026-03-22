using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace MedicalAdvisor.Web.Services;

public class MedicalAdvisorService
{
    private readonly IChatCompletionService _chatCompletion;
    private readonly string _systemPrompt;
    private readonly ILogger<MedicalAdvisorService> _logger;

    public MedicalAdvisorService(
        IChatCompletionService chatCompletion,
        DocumentService documentService,
        IWebHostEnvironment env,
        ILogger<MedicalAdvisorService> logger)
    {
        _chatCompletion = chatCompletion;
        _logger = logger;

        var promptPath = Path.Combine(env.ContentRootPath, "Prompts", "SystemPrompt.txt");
        var promptTemplate = File.ReadAllText(promptPath);

        _systemPrompt = promptTemplate + documentService.AllDocumentsContent;

        _logger.LogInformation(
            "MedicalAdvisorService initialized — system prompt length: {Length} characters",
            _systemPrompt.Length);
    }

    public async IAsyncEnumerable<string> StreamResponseAsync(
        string userMessage,
        ConversationState state,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Add user message to ChatHistory only (UI manages Messages separately).
        state.ChatHistory.AddUserMessage(userMessage);

        // Build a fresh ChatHistory with the system prompt + full conversation so far.
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(_systemPrompt);

        foreach (var message in state.ChatHistory)
        {
            chatHistory.Add(message);
        }

        var executionSettings = new OpenAIPromptExecutionSettings
        {
            Temperature = 0.3,
            MaxTokens = 1024,
            TopP = 0.9,
        };

        var fullResponse = new StringBuilder();

        await foreach (var chunk in _chatCompletion.GetStreamingChatMessageContentsAsync(
            chatHistory, executionSettings, kernel: null, cancellationToken: ct))
        {
            if (chunk.Content is { Length: > 0 } content)
            {
                fullResponse.Append(content);
                yield return content;
            }
        }

        // Add the full assistant response to ChatHistory (UI manages Messages separately).
        state.ChatHistory.AddAssistantMessage(fullResponse.ToString());

        _logger.LogDebug(
            "Completed streaming response — {Length} characters",
            fullResponse.Length);
    }
}
