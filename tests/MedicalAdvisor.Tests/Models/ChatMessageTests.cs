using MedicalAdvisor.Web.Models;

namespace MedicalAdvisor.Tests.Models;

public class ChatMessageTests
{
    [Fact]
    public void QuickReplies_DefaultsToEmptyList()
    {
        var message = new ChatMessage();

        Assert.NotNull(message.QuickReplies);
        Assert.Empty(message.QuickReplies);
    }

    [Fact]
    public void ShowQuickReplies_DefaultsToTrue()
    {
        var message = new ChatMessage();

        Assert.True(message.ShowQuickReplies);
    }

    [Fact]
    public void QuickReplies_CanBeSetAndRead()
    {
        var message = new ChatMessage
        {
            QuickReplies = ["Otázka 1", "Otázka 2"]
        };

        Assert.Equal(2, message.QuickReplies.Count);
        Assert.Equal("Otázka 1", message.QuickReplies[0]);
    }

    [Fact]
    public void ShowQuickReplies_CanBeSetToFalse()
    {
        var message = new ChatMessage { ShowQuickReplies = false };

        Assert.False(message.ShowQuickReplies);
    }
}
