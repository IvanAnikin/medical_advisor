using MedicalAdvisor.Web.Services;

namespace MedicalAdvisor.Tests.Services;

public class PumpRagServiceTests
{
    // ── Chunking tests ──

    [Fact]
    public void ChunkText_SplitsByPageSeparator()
    {
        var text = "Page one content.\n---\nPage two content.";
        var chunks = PumpRagService.ChunkText(text);
        Assert.True(chunks.Count >= 1);
        Assert.Contains(chunks, c => c.Text.Contains("Page one"));
        Assert.Contains(chunks, c => c.Text.Contains("Page two"));
    }

    [Fact]
    public void ChunkText_DetectsUppercaseHeadings_AsNewSections()
    {
        var text = "Intro text here.\n---\nBEZPEČNOSTNÍ INFORMACE\nSome safety info.\n---\nMore safety details.";
        var chunks = PumpRagService.ChunkText(text);
        Assert.Contains(chunks, c => c.Source.Contains("BEZPEČNOSTNÍ"));
    }

    [Fact]
    public void ChunkText_DetectsNumberedHeadings_AsNewSections()
    {
        var text = "Intro.\n---\n1. ÚVOD\nIntroduction text.\n---\n2. INSTALACE\nInstallation info.";
        var chunks = PumpRagService.ChunkText(text);
        Assert.Contains(chunks, c => c.Source.Contains("ÚVOD") || c.Source.Contains("1."));
        Assert.Contains(chunks, c => c.Source.Contains("INSTALACE") || c.Source.Contains("2."));
    }

    [Fact]
    public void ChunkText_SplitsLargeSections_IntoWindows()
    {
        // Create a section larger than 2000 chars
        var largeSection = "VELKÁ SEKCE\n" + string.Join(". ", Enumerable.Range(1, 300).Select(i => $"Věta číslo {i} obsahující nějaký text"));
        var text = $"Intro.\n---\n{largeSection}";
        var chunks = PumpRagService.ChunkText(text);

        // Should produce multiple chunks for this section
        var sectionChunks = chunks.Where(c => c.Source.Contains("VELKÁ SEKCE")).ToList();
        Assert.True(sectionChunks.Count > 1, $"Expected multiple chunks but got {sectionChunks.Count}");

        // Parts should be labeled
        Assert.Contains(sectionChunks, c => c.Source.Contains("(part"));
    }

    [Fact]
    public void ChunkText_ReturnsEmpty_ForEmptyInput()
    {
        var chunks = PumpRagService.ChunkText("");
        Assert.Empty(chunks);
    }

    [Fact]
    public void ChunkText_HandlesNoSeparators()
    {
        var text = "Just some plain text without any page separators.";
        var chunks = PumpRagService.ChunkText(text);
        Assert.NotEmpty(chunks);
        Assert.Contains(chunks, c => c.Text.Contains("plain text"));
    }

    // ── Cosine similarity tests ──

    [Fact]
    public void CosineSimilarity_IdenticalVectors_ReturnsOne()
    {
        float[] a = [1f, 2f, 3f];
        var similarity = PumpRagService.CosineSimilarity(a, a);
        Assert.Equal(1f, similarity, precision: 5);
    }

    [Fact]
    public void CosineSimilarity_OrthogonalVectors_ReturnsZero()
    {
        float[] a = [1f, 0f, 0f];
        float[] b = [0f, 1f, 0f];
        var similarity = PumpRagService.CosineSimilarity(a, b);
        Assert.Equal(0f, similarity, precision: 5);
    }

    [Fact]
    public void CosineSimilarity_OppositeVectors_ReturnsNegativeOne()
    {
        float[] a = [1f, 0f];
        float[] b = [-1f, 0f];
        var similarity = PumpRagService.CosineSimilarity(a, b);
        Assert.Equal(-1f, similarity, precision: 5);
    }

    [Fact]
    public void CosineSimilarity_SimilarVectors_ReturnsHighScore()
    {
        float[] a = [1f, 2f, 3f];
        float[] b = [1.1f, 2.1f, 3.1f];
        var similarity = PumpRagService.CosineSimilarity(a, b);
        Assert.True(similarity > 0.99f);
    }
}
