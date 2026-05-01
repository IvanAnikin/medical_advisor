using System.Text.Json.Serialization;

namespace MedicalAdvisor.Web.Models;

public class ConversationSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public string AdvisorId { get; set; } = "diabetes";
    public string AdvisorName { get; set; } = "";
    public List<SessionMessage> Messages { get; set; } = [];
}

public class SessionMessage
{
    public ChatRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool HasDoseGuidanceWarning { get; set; }
    public bool ShowEmergencyCallButton { get; set; }
    public string? DetectedLanguage { get; set; }
}
