namespace MedicalAdvisor.Web.Models;

public class AdvisorConfig
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "Chat";
    public string DocsFolder { get; set; } = "";
    public string SystemPromptFile { get; set; } = "";
    public bool IsDefault { get; set; }
    public string WelcomeMessage { get; set; } = "";
    public bool UseRag { get; set; } = false;
    public string RagCacheFile { get; set; } = "";
    public string RagSourceFile { get; set; } = "";
    public int RagChunkSize { get; set; } = 2000;
    public int RagChunkOverlap { get; set; } = 200;
    public float RagSimilarityThreshold { get; set; } = 0.65f;
    public int RagTopK { get; set; } = 5;
}
