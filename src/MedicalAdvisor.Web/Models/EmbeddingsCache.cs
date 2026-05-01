namespace MedicalAdvisor.Web.Models;

public class EmbeddingsCache
{
    public string ModelName { get; set; } = "";
    public string SourceFileHash { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public List<EmbeddingChunk> Chunks { get; set; } = [];
}
