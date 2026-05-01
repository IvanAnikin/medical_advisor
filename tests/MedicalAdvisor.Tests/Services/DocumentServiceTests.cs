using MedicalAdvisor.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
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

    private AdvisorRegistry CreateRegistry()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Advisors:0:Id"] = "diabetes",
                ["Advisors:0:Name"] = "Diabetologický poradce",
                ["Advisors:0:Slug"] = "diabetes",
                ["Advisors:0:DocsFolder"] = "docs",
                ["Advisors:0:SystemPromptFile"] = "Prompts/SystemPrompt.txt",
                ["Advisors:0:IsDefault"] = "true",
                ["Advisors:0:WelcomeMessage"] = "Dobrý den!",
            })
            .Build();

        return new AdvisorRegistry(config);
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
        var service = new DocumentService(_envMock.Object, CreateRegistry(), _loggerMock.Object);

        // Assert
        Assert.Contains("=== DOKUMENT: zaciname_s_inzulinem ===", service.AllDocumentsContent);
        Assert.Contains("=== DOKUMENT: cgm_kontinualni_monitorace ===", service.AllDocumentsContent);
        Assert.Contains("=== DOKUMENT: pece_o_nohy ===", service.AllDocumentsContent);
        Assert.Contains("=== DOKUMENT: doporuceni_fyzicka_aktivita ===", service.AllDocumentsContent);
    }

    [Fact]
    public void AllDocumentsContent_ContainsFileContents_WhenAllFilesExist()
    {
        // Arrange
        CreateAllSampleDocs();

        // Act
        var service = new DocumentService(_envMock.Object, CreateRegistry(), _loggerMock.Object);

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
        var service = new DocumentService(_envMock.Object, CreateRegistry(), _loggerMock.Object);

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
        var service = new DocumentService(_envMock.Object, CreateRegistry(), _loggerMock.Object);

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
        var service = new DocumentService(_envMock.Object, CreateRegistry(), _loggerMock.Object);

        Assert.NotNull(service.AllDocumentsContent);
    }

    [Fact]
    public void AllDocumentsContent_IsEmpty_WhenNoFilesExist()
    {
        // Arrange — empty docs directory
        Directory.CreateDirectory(_docsDir);

        // Act
        var service = new DocumentService(_envMock.Object, CreateRegistry(), _loggerMock.Object);

        // Assert
        Assert.Empty(service.AllDocumentsContent);
    }

    [Fact]
    public void AllDocumentsContent_LoadsAllFourDocuments_WhenAllPresent()
    {
        // Arrange
        CreateAllSampleDocs();

        // Act
        var service = new DocumentService(_envMock.Object, CreateRegistry(), _loggerMock.Object);

        // Assert — count the delimiter markers
        var delimiterCount = service.AllDocumentsContent
            .Split("=== DOKUMENT:")
            .Length - 1; // Split produces N+1 parts for N occurrences

        Assert.Equal(4, delimiterCount);
    }

    [Fact]
    public void GetDocumentsForAdvisor_ReturnsDiabetesDocs()
    {
        // Arrange
        CreateAllSampleDocs();

        // Act
        var service = new DocumentService(_envMock.Object, CreateRegistry(), _loggerMock.Object);

        // Assert
        var docs = service.GetDocumentsForAdvisor("diabetes");
        Assert.NotEmpty(docs);
        Assert.Contains("inzulínem", docs);
    }

    [Fact]
    public void GetDocumentsForAdvisor_ReturnsEmpty_ForUnknownAdvisor()
    {
        // Arrange
        CreateAllSampleDocs();

        // Act
        var service = new DocumentService(_envMock.Object, CreateRegistry(), _loggerMock.Object);

        // Assert
        var docs = service.GetDocumentsForAdvisor("nonexistent-advisor");
        Assert.Empty(docs);
    }

    [Fact]
    public void GetDocumentsForAdvisor_MultiAdvisor_LoadsDocsPerAdvisor()
    {
        // Arrange — set up two advisors with separate doc folders
        CreateAllSampleDocs();
        var gestationalDir = Path.Combine(_tempDir, "docs", "gestational");
        Directory.CreateDirectory(gestationalDir);
        File.WriteAllText(
            Path.Combine(gestationalDir, "gestdm_info.txt"),
            "Informace o gestačním diabetu v těhotenství.");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Advisors:0:Id"] = "diabetes",
                ["Advisors:0:Name"] = "Diabetologický poradce",
                ["Advisors:0:Slug"] = "diabetes",
                ["Advisors:0:DocsFolder"] = "docs",
                ["Advisors:0:SystemPromptFile"] = "Prompts/SystemPrompt.txt",
                ["Advisors:0:IsDefault"] = "true",
                ["Advisors:0:WelcomeMessage"] = "Dobrý den!",
                ["Advisors:1:Id"] = "gestational-diabetes",
                ["Advisors:1:Name"] = "Gestační diabetes",
                ["Advisors:1:Slug"] = "gestacni-diabetes",
                ["Advisors:1:DocsFolder"] = "docs/gestational",
                ["Advisors:1:SystemPromptFile"] = "Prompts/GestationalSystemPrompt.txt",
                ["Advisors:1:IsDefault"] = "false",
                ["Advisors:1:WelcomeMessage"] = "Dobrý den!",
            })
            .Build();
        var registry = new AdvisorRegistry(config);

        // Act
        var service = new DocumentService(_envMock.Object, registry, _loggerMock.Object);

        // Assert — each advisor gets only its own docs
        var diabetesDocs = service.GetDocumentsForAdvisor("diabetes");
        var gestationalDocs = service.GetDocumentsForAdvisor("gestational-diabetes");

        Assert.Contains("inzulínem", diabetesDocs);
        Assert.Contains("gestačním diabetu", gestationalDocs);
        Assert.DoesNotContain("gestačním diabetu", diabetesDocs);
    }

    [Fact]
    public void GetDocumentsForAdvisor_Diabetes2_LoadsDedicatedNormalizedDocument()
    {
        CreateAllSampleDocs();

        var diabetes2Dir = Path.Combine(_tempDir, "docs", "diabetes2");
        Directory.CreateDirectory(diabetes2Dir);
        File.WriteAllText(
            Path.Combine(diabetes2Dir, "diabetes2_runtime_guide.txt"),
            "Normalizovaný diabetes-2 dokument s korekčním faktorem a sportem.");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Advisors:0:Id"] = "diabetes",
                ["Advisors:0:Name"] = "Diabetologický poradce",
                ["Advisors:0:Slug"] = "diabetes",
                ["Advisors:0:DocsFolder"] = "docs",
                ["Advisors:0:SystemPromptFile"] = "Prompts/SystemPrompt.txt",
                ["Advisors:0:IsDefault"] = "true",
                ["Advisors:0:WelcomeMessage"] = "Dobrý den!",
                ["Advisors:1:Id"] = "diabetes-2",
                ["Advisors:1:Name"] = "Diabeticky poradce 2",
                ["Advisors:1:Slug"] = "diabeticky-poradce-2",
                ["Advisors:1:DocsFolder"] = "docs/diabetes2",
                ["Advisors:1:SystemPromptFile"] = "Prompts/Diabetes2SystemPrompt.txt",
                ["Advisors:1:IsDefault"] = "false",
                ["Advisors:1:WelcomeMessage"] = "Dobrý den.",
            })
            .Build();
        var registry = new AdvisorRegistry(config);

        var service = new DocumentService(_envMock.Object, registry, _loggerMock.Object);

        var diabetes2Docs = service.GetDocumentsForAdvisor("diabetes-2");

        Assert.Contains("Normalizovaný diabetes-2 dokument", diabetes2Docs);
        Assert.DoesNotContain("gestačním diabetu", diabetes2Docs);
    }
}
