using MedicalAdvisor.Web.Models;
using Microsoft.SemanticKernel.ChatCompletion;

namespace MedicalAdvisor.Web.Services;

public class ConversationState
{
    internal const string WelcomeMessage =
        "Dobrý den! Jsem váš diabetologický poradce.";

    public List<ChatMessage> Messages { get; private set; } = [];
    public ChatHistory ChatHistory { get; private set; } = new();
    public string? CurrentSessionId { get; set; }

    public ConversationState()
    {
        AddWelcomeMessage();
    }

    public void AddUserMessage(string content)
    {
        Messages.Add(new ChatMessage
        {
            Role = Models.ChatRole.User,
            Content = content,
            Timestamp = DateTime.Now,
        });

        ChatHistory.AddUserMessage(content);
    }

    public void AddAssistantMessage(string content)
    {
        Messages.Add(new ChatMessage
        {
            Role = Models.ChatRole.Assistant,
            Content = content,
            Timestamp = DateTime.Now,
        });

        ChatHistory.AddAssistantMessage(content);
    }

    public void Reset()
    {
        Messages.Clear();
        ChatHistory = new ChatHistory();
        CurrentSessionId = null;
        AddWelcomeMessage();
    }

    /// <summary>
    /// Creates a snapshot of the current conversation for saving.
    /// </summary>
    public ConversationSession ToSession()
    {
        // Derive title from first user message or fallback.
        var firstUserMsg = Messages.FirstOrDefault(m => m.Role == Models.ChatRole.User);
        var title = firstUserMsg?.Content ?? "Nová konverzace";
        if (title.Length > 50)
            title = title[..47] + "...";

        return new ConversationSession
        {
            Id = CurrentSessionId ?? Guid.NewGuid().ToString("N")[..8],
            Title = title,
            CreatedAt = Messages.FirstOrDefault()?.Timestamp ?? DateTime.Now,
            UpdatedAt = DateTime.Now,
            Messages = Messages
                .Where(m => !m.IsStreaming)
                .Select(m => new SessionMessage
                {
                    Role = m.Role,
                    Content = m.Content,
                    Timestamp = m.Timestamp
                })
                .ToList()
        };
    }

    /// <summary>
    /// Restores conversation from a saved session.
    /// </summary>
    public void LoadFromSession(ConversationSession session)
    {
        Messages.Clear();
        ChatHistory = new ChatHistory();
        CurrentSessionId = session.Id;

        foreach (var msg in session.Messages)
        {
            Messages.Add(new ChatMessage
            {
                Role = msg.Role,
                Content = msg.Content,
                Timestamp = msg.Timestamp,
                ShowQuickReplies = false,
            });

            if (msg.Role == Models.ChatRole.User)
                ChatHistory.AddUserMessage(msg.Content);
            else
                ChatHistory.AddAssistantMessage(msg.Content);
        }
    }

    /// <summary>
    /// Returns true if the conversation has any user messages (worth saving).
    /// </summary>
    public bool HasUserMessages => Messages.Any(m => m.Role == Models.ChatRole.User);

    private void AddWelcomeMessage()
    {
        Messages.Add(new ChatMessage
        {
            Role = Models.ChatRole.Assistant,
            Content = WelcomeMessage,
            Timestamp = DateTime.Now,
        });

        ChatHistory.AddAssistantMessage(WelcomeMessage);
    }
}
