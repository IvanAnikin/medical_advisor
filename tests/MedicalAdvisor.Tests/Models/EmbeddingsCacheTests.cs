using MedicalAdvisor.Web.Models;

namespace MedicalAdvisor.Tests.Models;

public class EmbeddingsCacheTests
{
    [Fact]
    public void EmbeddingsCache_RoundTrips_ViaJson()
    {
        var cache = new EmbeddingsCache
        {
            ModelName = "text-embedding-3-small",
            SourceFileHash = "sha256:abc123",
            CreatedAt = new DateTime(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc),
            Chunks =
            [
                new EmbeddingChunk
                {
                    Index = 0,
                    Text = "Test chunk",
                    Source = "Section 1",
                    Embedding = [0.1f, 0.2f, 0.3f]
                }
            ]
        };

        var json = System.Text.Json.JsonSerializer.Serialize(cache);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<EmbeddingsCache>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(cache.ModelName, deserialized.ModelName);
        Assert.Equal(cache.SourceFileHash, deserialized.SourceFileHash);
        Assert.Single(deserialized.Chunks);
        Assert.Equal("Test chunk", deserialized.Chunks[0].Text);
        Assert.Equal(3, deserialized.Chunks[0].Embedding.Length);
    }
}
