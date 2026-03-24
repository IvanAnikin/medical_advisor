using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using MedicalAdvisor.Web.Models;
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
        ChatMessage streamingMessage,
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

        // Parse quick replies from the full response.
        var (cleanContent, quickReplies) = ParseQuickReplies(fullResponse.ToString());

        // Store clean content (without quick-reply block) in ChatHistory.
        state.ChatHistory.AddAssistantMessage(cleanContent);

        // Set quick replies on the streaming message so the UI can render them.
        streamingMessage.Content = cleanContent;
        streamingMessage.QuickReplies = quickReplies;

        _logger.LogDebug(
            "Completed streaming response — {Length} characters, {QuickReplyCount} quick replies",
            cleanContent.Length, quickReplies.Count);
    }

    internal static (string Content, List<string> QuickReplies) ParseQuickReplies(string response)
    {
        var match = Regex.Match(response, @"\[QUICK_REPLIES\]\s*(.*?)\s*\[/QUICK_REPLIES\]", RegexOptions.Singleline);
        if (!match.Success)
            return (response, []);

        var content = response[..match.Index].TrimEnd();
        var replies = match.Groups[1].Value
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(4)
            .ToList();

        return (content, replies);
    }
}
