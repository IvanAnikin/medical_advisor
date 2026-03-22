namespace MedicalAdvisor.Web.Models;

public enum ChatRole { User, Assistant }

public class ChatMessage
{
    public ChatRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool IsStreaming { get; set; }
}
