using MedicalAdvisor.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace MedicalAdvisor.Tests.Services;

public class DocumentServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _docsDir;
    private readonly Mock<IWebHostEnvironment> _envMock;
    private readonly Mock<ILogger<DocumentService>> _loggerMock;

    private static readonly (string FileName, string Content)[] SampleDocs =
    [
        ("zaciname_s_inzulinem.txt", "Informace o zahájení léčby inzulínem a dávkování."),
        ("cgm_kontinualni_monitorace.txt", "Kontinuální monitorace glukózy pomocí senzorů."),
        ("pece_o_nohy.txt", "Péče o nohy při diabetu a prevence komplikací."),
        ("doporuceni_fyzicka_aktivita.txt", "Doporučení pro fyzickou aktivitu s diabetem."),
    ];

    public DocumentServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"MedAdvisorTests_{Guid.NewGuid():N}");
        _docsDir = Path.Combine(_tempDir, "docs");

        _envMock = new Mock<IWebHostEnvironment>();
        _envMock.Setup(e => e.ContentRootPath).Returns(_tempDir);

        _loggerMock = new Mock<ILogger<DocumentService>>();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private void CreateAllSampleDocs()
    {
        Directory.CreateDirectory(_docsDir);

        foreach (var (fileName, content) in SampleDocs)
        {
            File.WriteAllText(Path.Combine(_docsDir, fileName), content);
        }
    }

    [Fact]
    public void AllDocumentsContent_ContainsDelimiters_WhenAllFilesExist()
    {
        // Arrange
        CreateAllSampleDocs();

        // Act
        var service = new DocumentService(_envMock.Object, _loggerMock.Object);

        // Assert
        Assert.Contains("=== DOKUMENT: Začínáme s inzulínem ===", service.AllDocumentsContent);
        Assert.Contains("=== DOKUMENT: CGM — Kontinuální monitorace glukózy ===", service.AllDocumentsContent);
        Assert.Contains("=== DOKUMENT: Péče o nohy při diabetu ===", service.AllDocumentsContent);
        Assert.Contains("=== DOKUMENT: Doporučení — Fyzická aktivita ===", service.AllDocumentsContent);
    }

    [Fact]
    public void AllDocumentsContent_ContainsFileContents_WhenAllFilesExist()
    {
        // Arrange
        CreateAllSampleDocs();

        // Act
        var service = new DocumentService(_envMock.Object, _loggerMock.Object);

        // Assert
        foreach (var (_, content) in SampleDocs)
        {
            Assert.Contains(content, service.AllDocumentsContent);
        }
    }

    [Fact]
    public void AllDocumentsContent_ContainsKeyTerms_WhenAllFilesExist()
    {
        // Arrange
        CreateAllSampleDocs();

        // Act
        var service = new DocumentService(_envMock.Object, _loggerMock.Object);

        // Assert
        Assert.Contains("inzulínem", service.AllDocumentsContent);
        Assert.Contains("monitorace", service.AllDocumentsContent);
        Assert.Contains("nohy", service.AllDocumentsContent);
        Assert.Contains("aktivitu", service.AllDocumentsContent);
    }

    [Fact]
    public void Constructor_DoesNotThrow_WhenSomeFilesAreMissing()
    {
        // Arrange — create docs dir but only one file
        Directory.CreateDirectory(_docsDir);
        File.WriteAllText(
            Path.Combine(_docsDir, "zaciname_s_inzulinem.txt"),
            "Partial content about insulin.");

        // Act — should not throw
        var service = new DocumentService(_envMock.Object, _loggerMock.Object);

        // Assert — contains the one loaded doc, but not the missing ones
        Assert.Contains("Partial content about insulin.", service.AllDocumentsContent);
        Assert.DoesNotContain("monitorace", service.AllDocumentsContent);
    }

    [Fact]
    public void Constructor_DoesNotThrow_WhenDocsDirIsEmpty()
    {
        // Arrange — docs dir exists but contains no files.
        // File.ReadAllText throws FileNotFoundException (caught by the service).
        Directory.CreateDirectory(_docsDir);

        // Act & Assert — should not throw
        var service = new DocumentService(_envMock.Object, _loggerMock.Object);

        Assert.NotNull(service.AllDocumentsContent);
    }

    [Fact]
    public void AllDocumentsContent_IsEmpty_WhenNoFilesExist()
    {
        // Arrange — empty docs directory
        Directory.CreateDirectory(_docsDir);

        // Act
        var service = new DocumentService(_envMock.Object, _loggerMock.Object);

        // Assert
        Assert.Empty(service.AllDocumentsContent);
    }

    [Fact]
    public void AllDocumentsContent_LoadsAllFourDocuments_WhenAllPresent()
    {
        // Arrange
        CreateAllSampleDocs();

        // Act
        var service = new DocumentService(_envMock.Object, _loggerMock.Object);

        // Assert — count the delimiter markers
        var delimiterCount = service.AllDocumentsContent
            .Split("=== DOKUMENT:")
            .Length - 1; // Split produces N+1 parts for N occurrences

        Assert.Equal(4, delimiterCount);
    }
}
