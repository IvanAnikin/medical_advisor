using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using MedicalAdvisor.Web.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ChatTokenUsage = OpenAI.Chat.ChatTokenUsage;

namespace MedicalAdvisor.Web.Services;

public class MedicalAdvisorService
{
    internal sealed record ParsedAssistantResponse(
        string Content,
        List<string> QuickReplies,
        bool HasDoseGuidanceWarning,
        bool ShowEmergencyCallButton,
        string? DetectedLanguage);

    private readonly IChatCompletionService _chatCompletion;
    private readonly DocumentService _documentService;
    private readonly AdvisorRegistry _advisorRegistry;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<MedicalAdvisorService> _logger;
    private readonly PumpRagService? _ragService;
    private readonly ConcurrentDictionary<string, string> _systemPromptCache = new();
    private static readonly Regex QuickRepliesRegex = new(@"\[QUICK_REPLIES\]\s*(.*?)\s*\[/QUICK_REPLIES\]", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ResponseMetaRegex = new(@"\[RESPONSE_META\]\s*(.*?)\s*\[/RESPONSE_META\]", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public MedicalAdvisorService(
        IChatCompletionService chatCompletion,
        DocumentService documentService,
        AdvisorRegistry advisorRegistry,
        IWebHostEnvironment env,
        ILogger<MedicalAdvisorService> logger,
        PumpRagService? ragService = null)
    {
        _chatCompletion = chatCompletion;
        _documentService = documentService;
        _advisorRegistry = advisorRegistry;
        _env = env;
        _logger = logger;
        _ragService = ragService;
    }

    private string GetSystemPrompt(string advisorId)
    {
        return _systemPromptCache.GetOrAdd(advisorId, id =>
        {
            var advisor = _advisorRegistry.GetById(id)
                ?? throw new InvalidOperationException($"Advisor '{id}' not found.");

            var promptPath = Path.Combine(_env.ContentRootPath, advisor.SystemPromptFile);
            var promptTemplate = File.ReadAllText(promptPath);
            var docs = _documentService.GetDocumentsForAdvisor(id);
            var fullPrompt = promptTemplate + docs;

            _logger.LogInformation(
                "Loaded system prompt for advisor '{AdvisorId}' — {Length} characters",
                id, fullPrompt.Length);

            return fullPrompt;
        });
    }

    internal string GetSystemPromptForAdvisor(string advisorId)
    {
        return GetSystemPrompt(advisorId);
    }

    /// <summary>
    /// Streams a response for the specified advisor. Defaults to "diabetes" for backward compatibility.
    /// </summary>
    public async IAsyncEnumerable<string> StreamResponseAsync(
        string userMessage,
        ConversationState state,
        ChatMessage streamingMessage,
        [EnumeratorCancellation] CancellationToken ct = default,
        string advisorId = "diabetes")
    {
        var systemPrompt = GetSystemPrompt(advisorId);

        // For RAG advisors, retrieve relevant chunks (injected after system prompt to keep prefix stable for caching)
        string? ragContext = null;
        var advisor = _advisorRegistry.GetById(advisorId);
        if (advisor?.UseRag == true && _ragService != null)
        {
            var chunks = await _ragService.RetrieveAsync(userMessage, advisorId);
            if (chunks.Count > 0)
            {
                ragContext = "=== RELEVANTNÍ ČÁSTI MANUÁLU ===\n\n" + string.Join("\n\n---\n\n", chunks);
            }
        }

        // Add user message to ChatHistory only (UI manages Messages separately).
        state.ChatHistory.AddUserMessage(userMessage);

        // Build a fresh ChatHistory with the system prompt + full conversation so far.
        // System prompt is always first and identical per advisor — this maximizes prompt cache hits.
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        // RAG context goes as a separate system message after the stable prefix.
        // This keeps the main system prompt cacheable while still grounding the response.
        if (ragContext != null)
        {
            chatHistory.AddSystemMessage(ragContext);
        }

        foreach (var message in state.ChatHistory)
        {
            chatHistory.Add(message);
        }

        var executionSettings = new OpenAIPromptExecutionSettings
        {
            Temperature = 0.3,
            TopP = 0.9,
        };

        var fullResponse = new StringBuilder();

        var channel = Channel.CreateUnbounded<string>();
        var producerTask = ProduceStreamWithRetryAsync(chatHistory, executionSettings, channel.Writer, ct);

        await foreach (var content in channel.Reader.ReadAllAsync(ct))
        {
            fullResponse.Append(content);
            yield return content;
        }

        // Await the producer to propagate any non-429 exceptions.
        await producerTask;

        // Parse machine-readable metadata and quick replies from the full response.
        var parsedResponse = ParseAssistantResponse(fullResponse.ToString());

        // Store clean content (without quick-reply block) in ChatHistory.
        state.ChatHistory.AddAssistantMessage(parsedResponse.Content);

        // Set quick replies on the streaming message so the UI can render them.
        streamingMessage.Content = parsedResponse.Content;
        streamingMessage.QuickReplies = parsedResponse.QuickReplies;
        streamingMessage.HasDoseGuidanceWarning = parsedResponse.HasDoseGuidanceWarning;
        streamingMessage.ShowEmergencyCallButton = parsedResponse.ShowEmergencyCallButton;
        streamingMessage.DetectedLanguage = parsedResponse.DetectedLanguage;
        streamingMessage.AdvisorId = advisorId;

        _logger.LogDebug(
            "Completed streaming response — {Length} characters, {QuickReplyCount} quick replies, doseWarning={DoseWarning}, emergency={Emergency}",
            parsedResponse.Content.Length,
            parsedResponse.QuickReplies.Count,
            parsedResponse.HasDoseGuidanceWarning,
            parsedResponse.ShowEmergencyCallButton);
    }

    internal static (string Content, List<string> QuickReplies) ParseQuickReplies(string response)
    {
        var match = QuickRepliesRegex.Match(response);
        if (!match.Success)
            return (response, []);

        var content = response[..match.Index].TrimEnd();
        var replies = match.Groups[1].Value
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(4)
            .ToList();

        return (content, replies);
    }

    internal static ParsedAssistantResponse ParseAssistantResponse(string response)
    {
        var cleanedResponse = response;
        var hasDoseGuidanceWarning = false;
        var showEmergencyCallButton = false;
        string? detectedLanguage = null;

        var metaMatch = ResponseMetaRegex.Match(cleanedResponse);
        if (metaMatch.Success)
        {
            foreach (var rawLine in metaMatch.Groups[1].Value.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var separatorIndex = rawLine.IndexOf('=');
                if (separatorIndex <= 0)
                    continue;

                var key = rawLine[..separatorIndex].Trim();
                var value = rawLine[(separatorIndex + 1)..].Trim();

                if (key.Equals("DOSE_GUIDANCE", StringComparison.OrdinalIgnoreCase))
                {
                    hasDoseGuidanceWarning = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                }
                else if (key.Equals("EMERGENCY", StringComparison.OrdinalIgnoreCase))
                {
                    showEmergencyCallButton = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                }
                else if (key.Equals("LANGUAGE", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(value))
                {
                    detectedLanguage = value;
                }
            }

            cleanedResponse = ResponseMetaRegex.Replace(cleanedResponse, string.Empty).Trim();
        }

        var (content, quickReplies) = ParseQuickReplies(cleanedResponse);

        return new ParsedAssistantResponse(
            content.Trim(),
            quickReplies,
            hasDoseGuidanceWarning,
            showEmergencyCallButton,
            detectedLanguage);
    }

    private async Task ProduceStreamWithRetryAsync(
        ChatHistory chatHistory,
        OpenAIPromptExecutionSettings settings,
        ChannelWriter<string> writer,
        CancellationToken ct)
    {
        const int maxRetries = 3;
        try
        {
            for (var attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    StreamingChatMessageContent? lastChunk = null;
                    await foreach (var chunk in _chatCompletion.GetStreamingChatMessageContentsAsync(
                        chatHistory, settings, kernel: null, cancellationToken: ct))
                    {
                        lastChunk = chunk;
                        if (chunk.Content is { Length: > 0 } content)
                        {
                            await writer.WriteAsync(content, ct);
                        }
                    }
                    LogPromptCacheMetrics(lastChunk);
                    return; // Success
                }
                catch (HttpOperationException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests && attempt < maxRetries)
                {
                    var delay = TimeSpan.FromSeconds(15 * Math.Pow(2, attempt));
                    _logger.LogWarning("Rate limited (429), retrying in {Delay}s (attempt {Attempt}/{Max})",
                        delay.TotalSeconds, attempt + 1, maxRetries);
                    await Task.Delay(delay, ct);
                }
            }
        }
        finally
        {
            writer.Complete();
        }
    }

    private void LogPromptCacheMetrics(StreamingChatMessageContent? lastChunk)
    {
        if (lastChunk?.Metadata is not { } metadata)
            return;

        // SK OpenAI connector may use "Usage" or the inner update object
        ChatTokenUsage? usage = null;
        if (metadata.TryGetValue("Usage", out var obj) && obj is ChatTokenUsage u1)
            usage = u1;
        else if (metadata.TryGetValue("usage", out obj) && obj is ChatTokenUsage u2)
            usage = u2;

        if (usage == null)
        {
            // Log available keys to help diagnose
            _logger.LogDebug("Streaming metadata keys: {Keys}", string.Join(", ", metadata.Keys));
            return;
        }

        var cached = usage.InputTokenDetails?.CachedTokenCount ?? 0;
        var input = usage.InputTokenCount;
        var output = usage.OutputTokenCount;
        var cachePercent = input > 0 ? (double)cached / input * 100 : 0;

        _logger.LogInformation(
            "Token usage — input: {Input}, cached: {Cached} ({CachePercent:F0}%), output: {Output}, total: {Total}",
            input, cached, cachePercent, output, usage.TotalTokenCount);
    }
}
