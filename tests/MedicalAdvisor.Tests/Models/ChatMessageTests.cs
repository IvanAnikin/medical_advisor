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

    [Fact]
    public void DoseGuidanceWarning_DefaultsToFalse()
    {
        var message = new ChatMessage();

        Assert.False(message.HasDoseGuidanceWarning);
    }

    [Fact]
    public void EmergencyCallButton_DefaultsToFalse()
    {
        var message = new ChatMessage();

        Assert.False(message.ShowEmergencyCallButton);
    }

    [Fact]
    public void DetectedLanguage_DefaultsToNull()
    {
        var message = new ChatMessage();

        Assert.Null(message.DetectedLanguage);
    }

    [Fact]
    public void AdvisorId_DefaultsToNull()
    {
        var message = new ChatMessage();

        Assert.Null(message.AdvisorId);
    }

    [Fact]
    public void AdvisorId_CanBeSetAndRead()
    {
        var message = new ChatMessage { AdvisorId = "diabetes-2" };

        Assert.Equal("diabetes-2", message.AdvisorId);
    }
}
