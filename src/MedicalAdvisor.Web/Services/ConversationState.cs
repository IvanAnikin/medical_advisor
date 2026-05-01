using MedicalAdvisor.Web.Models;
using Microsoft.SemanticKernel.ChatCompletion;

namespace MedicalAdvisor.Web.Services;

/// <summary>
/// Per-advisor conversation state holder.
/// </summary>
public class AdvisorConversationState
{
    public List<ChatMessage> Messages { get; set; } = [];
    public ChatHistory ChatHistory { get; set; } = new();
    public string? CurrentSessionId { get; set; }
}

public class ConversationState
{
    internal const string WelcomeMessage =
        "Dobrý den! Jsem váš diabetologický poradce.";

    private const string DefaultAdvisorId = "diabetes";

    private readonly Dictionary<string, AdvisorConversationState> _states = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Backward-compatible: returns Messages for the default (diabetes) advisor.
    /// </summary>
    public List<ChatMessage> Messages => GetOrCreate(DefaultAdvisorId).Messages;

    /// <summary>
    /// Backward-compatible: returns ChatHistory for the default (diabetes) advisor.
    /// </summary>
    public ChatHistory ChatHistory => GetOrCreate(DefaultAdvisorId).ChatHistory;

    /// <summary>
    /// Backward-compatible: returns CurrentSessionId for the default (diabetes) advisor.
    /// </summary>
    public string? CurrentSessionId
    {
        get => GetOrCreate(DefaultAdvisorId).CurrentSessionId;
        set => GetOrCreate(DefaultAdvisorId).CurrentSessionId = value;
    }

    public ConversationState()
    {
        // Initialize the default advisor with the welcome message.
        var defaultState = GetOrCreate(DefaultAdvisorId);
        AddWelcomeMessage(defaultState, WelcomeMessage);
    }

    /// <summary>
    /// Gets or creates conversation state for a specific advisor.
    /// </summary>
    public AdvisorConversationState GetOrCreate(string advisorId)
    {
        if (_states.TryGetValue(advisorId, out var state))
            return state;

        state = new AdvisorConversationState();
        _states[advisorId] = state;
        return state;
    }

    /// <summary>
    /// Initializes an advisor's state with a welcome message if it has no messages yet.
    /// </summary>
    public void EnsureWelcomeMessage(string advisorId, string welcomeMessage)
    {
        var state = GetOrCreate(advisorId);
        if (state.Messages.Count == 0)
        {
            AddWelcomeMessage(state, welcomeMessage);
        }
    }

    public void AddUserMessage(string content)
    {
        var state = GetOrCreate(DefaultAdvisorId);
        state.Messages.Add(new ChatMessage
        {
            Role = Models.ChatRole.User,
            Content = content,
            Timestamp = DateTime.Now,
        });

        state.ChatHistory.AddUserMessage(content);
    }

    public void AddAssistantMessage(string content)
    {
        var state = GetOrCreate(DefaultAdvisorId);
        state.Messages.Add(new ChatMessage
        {
            Role = Models.ChatRole.Assistant,
            Content = content,
            Timestamp = DateTime.Now,
        });

        state.ChatHistory.AddAssistantMessage(content);
    }

    /// <summary>
    /// Resets the default advisor's state. Backward compatible.
    /// </summary>
    public void Reset()
    {
        Reset(DefaultAdvisorId);
    }

    /// <summary>
    /// Resets a specific advisor's state.
    /// </summary>
    public void Reset(string advisorId)
    {
        var state = GetOrCreate(advisorId);
        state.Messages.Clear();
        state.ChatHistory = new ChatHistory();
        state.CurrentSessionId = null;

        var welcome = advisorId.Equals(DefaultAdvisorId, StringComparison.OrdinalIgnoreCase)
            ? WelcomeMessage
            : "Dobrý den!";
        AddWelcomeMessage(state, welcome);
    }

    /// <summary>
    /// Creates a snapshot of a specific advisor's conversation for saving.
    /// </summary>
    public ConversationSession ToSession(string advisorId, string advisorName = "")
    {
        var state = GetOrCreate(advisorId);

        var firstUserMsg = state.Messages.FirstOrDefault(m => m.Role == Models.ChatRole.User);
        var title = firstUserMsg?.Content ?? "Nová konverzace";
        if (title.Length > 50)
            title = title[..47] + "...";

        return new ConversationSession
        {
            Id = state.CurrentSessionId ?? Guid.NewGuid().ToString("N")[..8],
            Title = title,
            CreatedAt = state.Messages.FirstOrDefault()?.Timestamp ?? DateTime.Now,
            UpdatedAt = DateTime.Now,
            AdvisorId = advisorId,
            AdvisorName = advisorName,
            Messages = state.Messages
                .Where(m => !m.IsStreaming)
                .Select(m => new SessionMessage
                {
                    Role = m.Role,
                    Content = m.Content,
                    Timestamp = m.Timestamp,
                    HasDoseGuidanceWarning = m.HasDoseGuidanceWarning,
                    ShowEmergencyCallButton = m.ShowEmergencyCallButton,
                    DetectedLanguage = m.DetectedLanguage,
                })
                .ToList()
        };
    }

    /// <summary>
    /// Backward-compatible overload — creates session for the default advisor.
    /// </summary>
    public ConversationSession ToSession()
    {
        return ToSession(DefaultAdvisorId);
    }

    /// <summary>
    /// Restores conversation from a saved session into the correct advisor's state.
    /// </summary>
    public void LoadFromSession(ConversationSession session)
    {
        var advisorId = string.IsNullOrEmpty(session.AdvisorId) ? DefaultAdvisorId : session.AdvisorId;
        var state = GetOrCreate(advisorId);

        state.Messages.Clear();
        state.ChatHistory = new ChatHistory();
        state.CurrentSessionId = session.Id;

        foreach (var msg in session.Messages)
        {
            state.Messages.Add(new ChatMessage
            {
                Role = msg.Role,
                Content = msg.Content,
                Timestamp = msg.Timestamp,
                ShowQuickReplies = false,
                HasDoseGuidanceWarning = msg.HasDoseGuidanceWarning,
                ShowEmergencyCallButton = msg.ShowEmergencyCallButton,
                DetectedLanguage = msg.DetectedLanguage,
            });

            if (msg.Role == Models.ChatRole.User)
                state.ChatHistory.AddUserMessage(msg.Content);
            else
                state.ChatHistory.AddAssistantMessage(msg.Content);
        }
    }

    /// <summary>
    /// Returns true if the default advisor has user messages.
    /// </summary>
    public bool HasUserMessages => HasUserMessages_ForAdvisor(DefaultAdvisorId);

    /// <summary>
    /// Returns true if the specified advisor has user messages.
    /// </summary>
    public bool HasUserMessages_ForAdvisor(string advisorId)
    {
        if (!_states.TryGetValue(advisorId, out var state))
            return false;
        return state.Messages.Any(m => m.Role == Models.ChatRole.User);
    }

    private static void AddWelcomeMessage(AdvisorConversationState state, string welcomeMessage)
    {
        state.Messages.Add(new ChatMessage
        {
            Role = Models.ChatRole.Assistant,
            Content = welcomeMessage,
            Timestamp = DateTime.Now,
        });

        state.ChatHistory.AddAssistantMessage(welcomeMessage);
    }
}
