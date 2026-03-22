using MedicalAdvisor.Web.Models;
using MedicalAdvisor.Web.Services;

namespace MedicalAdvisor.Tests.Services;

public class ConversationStateTests
{
    [Fact]
    public void InitialState_HasExactlyOneMessage()
    {
        var state = new ConversationState();

        Assert.Single(state.Messages);
    }

    [Fact]
    public void InitialState_WelcomeMessageHasAssistantRole()
    {
        var state = new ConversationState();

        Assert.Equal(ChatRole.Assistant, state.Messages[0].Role);
    }

    [Fact]
    public void InitialState_WelcomeMessageContentIsNotEmpty()
    {
        var state = new ConversationState();

        Assert.False(string.IsNullOrWhiteSpace(state.Messages[0].Content));
    }

    [Fact]
    public void InitialState_ChatHistoryIsEmpty()
    {
        // Welcome message is NOT added to ChatHistory per the implementation
        var state = new ConversationState();

        Assert.Empty(state.ChatHistory);
    }

    [Fact]
    public void AddUserMessage_AddsMessageWithUserRole()
    {
        var state = new ConversationState();

        state.AddUserMessage("Test question");

        Assert.Equal(2, state.Messages.Count);
        Assert.Equal(ChatRole.User, state.Messages[1].Role);
        Assert.Equal("Test question", state.Messages[1].Content);
    }

    [Fact]
    public void AddUserMessage_SetsTimestamp()
    {
        var state = new ConversationState();
        var before = DateTime.Now;

        state.AddUserMessage("Hello");

        var after = DateTime.Now;
        var msg = state.Messages[1];
        Assert.InRange(msg.Timestamp, before, after);
    }

    [Fact]
    public void AddAssistantMessage_AddsMessageWithAssistantRole()
    {
        var state = new ConversationState();

        state.AddAssistantMessage("Test answer");

        Assert.Equal(2, state.Messages.Count);
        Assert.Equal(ChatRole.Assistant, state.Messages[1].Role);
        Assert.Equal("Test answer", state.Messages[1].Content);
    }

    [Fact]
    public void AddAssistantMessage_SetsTimestamp()
    {
        var state = new ConversationState();
        var before = DateTime.Now;

        state.AddAssistantMessage("Response");

        var after = DateTime.Now;
        var msg = state.Messages[1];
        Assert.InRange(msg.Timestamp, before, after);
    }

    [Fact]
    public void MultipleAdds_GrowMessageCountCorrectly()
    {
        var state = new ConversationState();

        state.AddUserMessage("Q1");
        state.AddAssistantMessage("A1");
        state.AddUserMessage("Q2");
        state.AddAssistantMessage("A2");

        // 1 welcome + 4 additions
        Assert.Equal(5, state.Messages.Count);
    }

    [Fact]
    public void ChatHistory_MirrorsUserAndAssistantMessages()
    {
        var state = new ConversationState();

        state.AddUserMessage("Q1");
        state.AddAssistantMessage("A1");
        state.AddUserMessage("Q2");

        // ChatHistory should have 3 entries (welcome is excluded)
        Assert.Equal(3, state.ChatHistory.Count);
    }

    [Fact]
    public void ChatHistory_ContainsCorrectRoles()
    {
        var state = new ConversationState();

        state.AddUserMessage("Q1");
        state.AddAssistantMessage("A1");

        Assert.Equal(Microsoft.SemanticKernel.ChatCompletion.AuthorRole.User, state.ChatHistory[0].Role);
        Assert.Equal(Microsoft.SemanticKernel.ChatCompletion.AuthorRole.Assistant, state.ChatHistory[1].Role);
    }

    [Fact]
    public void Reset_ClearsMessagesAndAddsWelcomeBack()
    {
        var state = new ConversationState();

        state.AddUserMessage("Q1");
        state.AddAssistantMessage("A1");
        Assert.Equal(3, state.Messages.Count);

        state.Reset();

        Assert.Single(state.Messages);
        Assert.Equal(ChatRole.Assistant, state.Messages[0].Role);
    }

    [Fact]
    public void Reset_ClearsChatHistory()
    {
        var state = new ConversationState();

        state.AddUserMessage("Q1");
        state.AddAssistantMessage("A1");
        Assert.Equal(2, state.ChatHistory.Count);

        state.Reset();

        Assert.Empty(state.ChatHistory);
    }

    [Fact]
    public void Reset_AllowsNewMessagesAfterReset()
    {
        var state = new ConversationState();

        state.AddUserMessage("Before reset");
        state.Reset();
        state.AddUserMessage("After reset");

        Assert.Equal(2, state.Messages.Count);
        Assert.Equal("After reset", state.Messages[1].Content);
    }
}
