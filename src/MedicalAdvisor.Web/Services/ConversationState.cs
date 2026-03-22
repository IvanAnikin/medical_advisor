using MedicalAdvisor.Web.Models;
using Microsoft.SemanticKernel.ChatCompletion;

namespace MedicalAdvisor.Web.Services;

public class ConversationState
{
    private const string WelcomeMessage =
        "Dobrý den! Jsem váš diabetologický poradce. Mohu vám pomoci s informacemi o:\n\n" +
        "• **Léčbě inzulínem** — zahájení, typy inzulínu, dávkování, aplikace\n" +
        "• **Kontinuální monitoraci glukózy (CGM)** — princip, systémy, interpretace\n" +
        "• **Péči o nohy při diabetu** — prevence, rizika, postupy\n" +
        "• **Fyzické aktivitě s diabetem** — doporučení, typy, rizika\n\n" +
        "S čím vám mohu pomoci?";

    public List<ChatMessage> Messages { get; private set; } = [];
    public ChatHistory ChatHistory { get; private set; } = new();

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
        AddWelcomeMessage();
    }

    private void AddWelcomeMessage()
    {
        Messages.Add(new ChatMessage
        {
            Role = Models.ChatRole.Assistant,
            Content = WelcomeMessage,
            Timestamp = DateTime.Now,
        });

        // The welcome message is not added to ChatHistory — it is part of the
        // system prompt context, not a prior assistant turn for the LLM.
    }
}
