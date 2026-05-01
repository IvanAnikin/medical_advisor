using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace MedicalAdvisor.Web.Services;

public class DocumentService
{
    private readonly Dictionary<string, string> _documentsByAdvisor = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Backward-compatible property — returns the default (diabetes) advisor's documents.
    /// </summary>
    public string AllDocumentsContent => GetDocumentsForAdvisor("diabetes");

    public DocumentService(IWebHostEnvironment env, AdvisorRegistry registry, ILogger<DocumentService> logger)
    {
        foreach (var advisor in registry.GetAll())
        {
            var docsPath = Path.Combine(env.ContentRootPath, advisor.DocsFolder);

            if (!Directory.Exists(docsPath))
            {
                logger.LogWarning(
                    "Docs folder not found for advisor '{AdvisorId}': {Path}. No documents loaded.",
                    advisor.Id, docsPath);
                _documentsByAdvisor[advisor.Id] = "";
                continue;
            }

            var parts = new List<string>();

            var supportedFiles = Directory.EnumerateFiles(docsPath, "*.*")
                .Where(f =>
                {
                    var ext = Path.GetExtension(f).ToLowerInvariant();
                    return ext is ".txt" or ".docx";
                })
                .Where(f =>
                {
                    // For RAG advisors, skip the RAG source file (consumed only by RAG)
                    if (advisor.UseRag && !string.IsNullOrEmpty(advisor.RagSourceFile))
                    {
                        var name = Path.GetFileName(f);
                        if (name.Equals(advisor.RagSourceFile, StringComparison.OrdinalIgnoreCase))
                            return false;
                    }
                    return true;
                })
                .OrderBy(f => f)
                .ToList();

            foreach (var filePath in supportedFiles)
            {
                var fileName = Path.GetFileName(filePath);
                var title = Path.GetFileNameWithoutExtension(filePath);

                try
                {
                    var ext = Path.GetExtension(filePath).ToLowerInvariant();
                    var text = ext == ".docx"
                        ? ExtractTextFromDocx(filePath)
                        : File.ReadAllText(filePath);

                    logger.LogInformation(
                        "Loaded document '{Title}' ({FileName}) for advisor '{AdvisorId}': {Size} characters",
                        title, fileName, advisor.Id, text.Length);

                    parts.Add($"\n=== DOKUMENT: {title} ===\n\n{text}");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex,
                        "Failed to load document '{FileName}' for advisor '{AdvisorId}'. Skipping.",
                        fileName, advisor.Id);
                }
            }

            var combined = string.Join("\n", parts);
            _documentsByAdvisor[advisor.Id] = combined;

            logger.LogInformation(
                "DocumentService initialized for advisor '{AdvisorId}' — {Count} document(s) loaded, total {Size} characters",
                advisor.Id, parts.Count, combined.Length);
        }
    }

    public string GetDocumentsForAdvisor(string advisorId)
    {
        return _documentsByAdvisor.TryGetValue(advisorId, out var docs) ? docs : "";
    }

    private static string ExtractTextFromDocx(string filePath)
    {
        using var doc = WordprocessingDocument.Open(filePath, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return "";

        var sb = new StringBuilder();
        foreach (var paragraph in body.Elements<Paragraph>())
        {
            sb.AppendLine(paragraph.InnerText);
        }
        return sb.ToString();
    }
}
