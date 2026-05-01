using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MedicalAdvisor.Web.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

namespace MedicalAdvisor.Web.Services;

public class PumpRagService
{
    private readonly ITextEmbeddingGenerationService _embeddingService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<PumpRagService> _logger;
    private readonly AdvisorRegistry _advisorRegistry;

    private readonly Dictionary<string, List<EmbeddingChunk>> _chunksByAdvisor = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AdvisorConfig> _ragAdvisors = new(StringComparer.OrdinalIgnoreCase);

    public PumpRagService(
        ITextEmbeddingGenerationService embeddingService,
        AdvisorRegistry advisorRegistry,
        IWebHostEnvironment env,
        ILogger<PumpRagService> logger)
    {
        _embeddingService = embeddingService;
        _advisorRegistry = advisorRegistry;
        _env = env;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        var ragAdvisors = _advisorRegistry.GetAll().Where(a => a.UseRag).ToList();

        if (ragAdvisors.Count == 0)
        {
            _logger.LogWarning("No RAG-enabled advisors found in configuration.");
            return;
        }

        foreach (var advisor in ragAdvisors)
        {
            _ragAdvisors[advisor.Id] = advisor;
            await InitializeAdvisorAsync(advisor);
        }
    }

    private async Task InitializeAdvisorAsync(AdvisorConfig advisor)
    {
        var sourceFilePath = Path.Combine(_env.ContentRootPath, advisor.DocsFolder, advisor.RagSourceFile);
        var cacheFilePath = Path.Combine(_env.ContentRootPath, advisor.RagCacheFile);

        if (!File.Exists(sourceFilePath))
        {
            _logger.LogWarning("RAG source file not found: {Path} for advisor '{AdvisorId}'. Skipping.",
                sourceFilePath, advisor.Id);
            _chunksByAdvisor[advisor.Id] = [];
            return;
        }

        var sourceHash = ComputeFileHash(sourceFilePath);

        if (File.Exists(cacheFilePath))
        {
            try
            {
                var cacheJson = await File.ReadAllTextAsync(cacheFilePath);
                var cache = JsonSerializer.Deserialize<EmbeddingsCache>(cacheJson);

                if (cache != null && cache.SourceFileHash == sourceHash && cache.Chunks.Count > 0)
                {
                    _chunksByAdvisor[advisor.Id] = cache.Chunks;
                    _logger.LogInformation("Loaded {Count} chunks from cache for advisor '{AdvisorId}' ({Path})",
                        cache.Chunks.Count, advisor.Id, cacheFilePath);
                    return;
                }

                _logger.LogInformation("Cache hash mismatch or empty for advisor '{AdvisorId}' — rebuilding index.", advisor.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load cache for advisor '{AdvisorId}' from {Path} — rebuilding index.",
                    advisor.Id, cacheFilePath);
            }
        }

        await BuildIndexAsync(advisor, sourceFilePath, cacheFilePath, sourceHash);
    }

    public async Task<List<string>> RetrieveAsync(string query, string advisorId = "insulin-pump")
    {
        if (!_chunksByAdvisor.TryGetValue(advisorId, out var chunks) || chunks.Count == 0)
            return [];

        if (!_ragAdvisors.TryGetValue(advisorId, out var advisor))
            return [];

        var queryEmbeddings = await _embeddingService.GenerateEmbeddingsAsync([query]);
        var queryEmbedding = queryEmbeddings[0].ToArray();

        var threshold = advisor.RagSimilarityThreshold;
        var topK = advisor.RagTopK;

        var scored = chunks
            .Select(c => (Chunk: c, Score: CosineSimilarity(queryEmbedding, c.Embedding)))
            .Where(x => x.Score >= threshold)
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .ToList();

        // Expand results: for each retrieved chunk, also include its immediate neighbors
        // to preserve surrounding context that may contain relevant details.
        var expandedIndices = new HashSet<int>();
        foreach (var (chunk, _) in scored)
        {
            expandedIndices.Add(chunk.Index);
            if (chunk.Index > 0) expandedIndices.Add(chunk.Index - 1);
            if (chunk.Index < chunks.Count - 1) expandedIndices.Add(chunk.Index + 1);
        }

        // Build final result ordered by original index (preserves document reading order)
        var result = chunks
            .Where(c => expandedIndices.Contains(c.Index))
            .OrderBy(c => c.Index)
            .Select(c => c.Text)
            .Distinct()
            .ToList();

        _logger.LogDebug(
            "RAG retrieval for advisor '{AdvisorId}' query ({QueryLength} chars): {MatchCount} direct matches (threshold={Threshold}), " +
            "{ExpandedCount} chunks after neighbor expansion, top score = {TopScore:F4}",
            advisorId, query.Length, scored.Count, threshold, result.Count,
            scored.Count > 0 ? scored[0].Score : 0f);

        return result;
    }

    private async Task BuildIndexAsync(AdvisorConfig advisor, string sourceFilePath, string cacheFilePath, string sourceHash)
    {
        _logger.LogInformation("Building RAG index for advisor '{AdvisorId}' from {Path}...", advisor.Id, sourceFilePath);

        var text = await File.ReadAllTextAsync(sourceFilePath);
        var rawChunks = ChunkText(text, advisor.RagChunkSize, advisor.RagChunkOverlap);

        _logger.LogInformation("Chunked source into {Count} chunks for advisor '{AdvisorId}'. Embedding...",
            rawChunks.Count, advisor.Id);

        var chunks = new List<EmbeddingChunk>();
        const int batchSize = 10;
        const int maxRetries = 5;
        int totalBatches = (rawChunks.Count + batchSize - 1) / batchSize;

        for (int i = 0; i < rawChunks.Count; i += batchSize)
        {
            var batch = rawChunks.Skip(i).Take(batchSize).ToList();
            // Embed with section heading prefix for better semantic matching
            var texts = batch.Select(c => $"[{c.Source}] {c.Text}").ToList();
            int batchNum = i / batchSize + 1;

            IList<ReadOnlyMemory<float>>? embeddings = null;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    embeddings = await _embeddingService.GenerateEmbeddingsAsync(texts);
                    break;
                }
                catch (HttpOperationException ex) when (ex.Message.Contains("429"))
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt) * 15);
                    _logger.LogWarning("Rate limited on batch {Batch}/{Total} for advisor '{AdvisorId}', retrying in {Delay}s (attempt {Attempt}/{Max})...",
                        batchNum, totalBatches, advisor.Id, delay.TotalSeconds, attempt + 1, maxRetries);
                    await Task.Delay(delay);
                }
            }

            if (embeddings is null)
                throw new InvalidOperationException($"Failed to embed batch starting at chunk {i} after {maxRetries} retries.");

            for (int j = 0; j < batch.Count; j++)
            {
                chunks.Add(new EmbeddingChunk
                {
                    Index = i + j,
                    Text = batch[j].Text,
                    Source = batch[j].Source,
                    Embedding = embeddings[j].ToArray()
                });
            }

            _logger.LogInformation("Embedded batch {Batch}/{Total} ({Count} chunks) for advisor '{AdvisorId}'",
                batchNum, totalBatches, batch.Count, advisor.Id);

            // Pace requests to avoid rate limits (120K TPM)
            if (i + batchSize < rawChunks.Count)
                await Task.Delay(TimeSpan.FromSeconds(2));
        }

        _chunksByAdvisor[advisor.Id] = chunks;

        var cache = new EmbeddingsCache
        {
            ModelName = "text-embedding-3-small",
            SourceFileHash = sourceHash,
            CreatedAt = DateTime.UtcNow,
            Chunks = chunks
        };

        var cacheDir = Path.GetDirectoryName(cacheFilePath);
        if (!string.IsNullOrEmpty(cacheDir) && !Directory.Exists(cacheDir))
            Directory.CreateDirectory(cacheDir);

        var json = JsonSerializer.Serialize(cache);
        await File.WriteAllTextAsync(cacheFilePath, json);

        _logger.LogInformation("Built index for advisor '{AdvisorId}': {Count} chunks, saved to cache ({Path})",
            advisor.Id, chunks.Count, cacheFilePath);
    }

    internal static List<(string Text, string Source)> ChunkText(string text, int chunkSize = 2000, int overlap = 200)
    {
        // Split by page separator (--- on its own line)
        var pages = text.Split("\n---\n", StringSplitOptions.None);
        if (pages.Length <= 1)
        {
            // Try with \r\n line endings
            pages = text.Split("\r\n---\r\n", StringSplitOptions.None);
        }

        var sections = new List<(string Heading, StringBuilder Content)>();
        string currentHeading = "Úvod";
        var currentContent = new StringBuilder();

        foreach (var page in pages)
        {
            var trimmedPage = page.Trim();
            if (string.IsNullOrEmpty(trimmedPage))
                continue;

            // Find the first non-empty line of this page
            var firstLine = trimmedPage.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()?.Trim() ?? "";

            // Check if first line looks like a heading
            if (firstLine.Length > 0 && firstLine.Length < 100 && IsHeading(firstLine))
            {
                // Save current section if it has content
                if (currentContent.Length > 0)
                {
                    sections.Add((currentHeading, currentContent));
                    currentContent = new StringBuilder();
                }
                currentHeading = firstLine;
            }

            currentContent.AppendLine(trimmedPage);
        }

        // Don't forget the last section
        if (currentContent.Length > 0)
        {
            sections.Add((currentHeading, currentContent));
        }

        // Now chunk each section
        var result = new List<(string Text, string Source)>();

        foreach (var (heading, content) in sections)
        {
            var sectionText = content.ToString().Trim();
            if (string.IsNullOrEmpty(sectionText))
                continue;

            if (sectionText.Length <= chunkSize)
            {
                result.Add((sectionText, heading));
            }
            else
            {
                var windows = SplitIntoWindows(sectionText, windowSize: chunkSize, overlap: overlap);
                for (int i = 0; i < windows.Count; i++)
                {
                    result.Add((windows[i], $"{heading} (part {i + 1})"));
                }
            }
        }

        return result;
    }

    private static bool IsHeading(string line)
    {
        // ALL UPPERCASE (at least 3 chars, allowing digits, spaces, punctuation)
        if (line.Length >= 3 && line == line.ToUpperInvariant() && line.Any(char.IsLetter))
            return true;

        // Starts with digit followed by dot and space (e.g., "1. ", "14.2 ")
        if (char.IsDigit(line[0]) && line.Contains(". "))
            return true;

        return false;
    }

    private static List<string> SplitIntoWindows(string text, int windowSize, int overlap)
    {
        var windows = new List<string>();

        // Split at sentence boundaries
        var sentences = SplitSentences(text);
        var current = new StringBuilder();
        var buffer = new List<(string Sentence, int Start, int End)>();
        int runningPos = 0;
        foreach (var sentence in sentences)
        {
            buffer.Add((sentence, runningPos, runningPos + sentence.Length));
            runningPos += sentence.Length;
        }

        int windowStart = 0;
        while (windowStart < buffer.Count)
        {
            current.Clear();
            int windowEnd = windowStart;

            // Accumulate sentences until we exceed windowSize
            while (windowEnd < buffer.Count && current.Length + buffer[windowEnd].Sentence.Length <= windowSize)
            {
                current.Append(buffer[windowEnd].Sentence);
                windowEnd++;
            }

            // If we couldn't fit even one sentence, take it anyway
            if (windowEnd == windowStart && windowStart < buffer.Count)
            {
                current.Append(buffer[windowStart].Sentence);
                windowEnd = windowStart + 1;
            }

            windows.Add(current.ToString().Trim());

            // Move forward, but back up by overlap amount
            int overlapChars = 0;
            int nextStart = windowEnd;
            while (nextStart > windowStart + 1 && overlapChars < overlap)
            {
                nextStart--;
                overlapChars += buffer[nextStart].Sentence.Length;
            }

            if (nextStart <= windowStart)
                nextStart = windowStart + 1;

            // Safety: if we're making no progress, force advance
            if (nextStart <= windowStart)
                nextStart = windowEnd;

            windowStart = nextStart;

            if (windowStart >= buffer.Count)
                break;
        }

        return windows;
    }

    private static List<string> SplitSentences(string text)
    {
        var sentences = new List<string>();
        var current = new StringBuilder();

        for (int i = 0; i < text.Length; i++)
        {
            current.Append(text[i]);

            if (text[i] == '.' || text[i] == '?' || text[i] == '!')
            {
                // Check if followed by space or newline (sentence boundary)
                if (i + 1 >= text.Length || text[i + 1] == ' ' || text[i + 1] == '\n' || text[i + 1] == '\r')
                {
                    // Include trailing whitespace in the sentence
                    while (i + 1 < text.Length && (text[i + 1] == ' ' || text[i + 1] == '\n' || text[i + 1] == '\r'))
                    {
                        i++;
                        current.Append(text[i]);
                    }
                    sentences.Add(current.ToString());
                    current.Clear();
                }
            }
        }

        // Remaining text
        if (current.Length > 0)
            sentences.Add(current.ToString());

        return sentences;
    }

    private static string ComputeFileHash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hashBytes = SHA256.HashData(stream);
        return "sha256:" + Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    internal static float CosineSimilarity(float[] a, float[] b)
    {
        float dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        return dot / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
    }
}
