using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace MedicalAdvisor.Web.Services;

public class DocumentService
{
    private static readonly (string FileName, string Title)[] Documents =
    [
        ("zaciname_s_inzulinem.txt", "Začínáme s inzulínem"),
        ("cgm_kontinualni_monitorace.txt", "CGM — Kontinuální monitorace glukózy"),
        ("pece_o_nohy.txt", "Péče o nohy při diabetu"),
        ("doporuceni_fyzicka_aktivita.txt", "Doporučení — Fyzická aktivita"),
    ];

    public string AllDocumentsContent { get; }

    public DocumentService(IWebHostEnvironment env, ILogger<DocumentService> logger)
    {
        var docsPath = Path.Combine(env.ContentRootPath, "docs");
        var parts = new List<string>();

        foreach (var (fileName, title) in Documents)
        {
            var filePath = Path.Combine(docsPath, fileName);

            try
            {
                var text = File.ReadAllText(filePath);
                logger.LogInformation(
                    "Loaded document '{Title}' ({FileName}): {Size} characters",
                    title, fileName, text.Length);

                parts.Add($"\n=== DOKUMENT: {title} ===\n\n{text}");
            }
            catch (FileNotFoundException)
            {
                logger.LogWarning(
                    "Document file not found: {FilePath}. Skipping.", filePath);
            }
        }

        AllDocumentsContent = string.Join("\n", parts);

        logger.LogInformation(
            "DocumentService initialized — {Count} document(s) loaded, total {Size} characters",
            parts.Count, AllDocumentsContent.Length);
    }
}
