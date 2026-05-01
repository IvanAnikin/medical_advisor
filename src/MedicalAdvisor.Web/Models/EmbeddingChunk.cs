namespace MedicalAdvisor.Web.Models;

public class EmbeddingChunk
{
    public int Index { get; set; }
    public string Text { get; set; } = "";
    public string Source { get; set; } = "";
    public float[] Embedding { get; set; } = [];
}
