using MedicalAdvisor.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;

namespace MedicalAdvisor.Tests.Services;

public class MedicalAdvisorServiceTests
{
    [Fact]
    public void ParseQuickReplies_ExtractsReplies()
    {
        var response = "Zde je odpověď.\n\n[QUICK_REPLIES]\nJak správně aplikovat inzulín?\nJaké jsou typy inzulínu?\nCo dělat při hypoglykémii?\n[/QUICK_REPLIES]";

        var (content, quickReplies) = MedicalAdvisorService.ParseQuickReplies(response);

        Assert.Equal("Zde je odpověď.", content);
        Assert.Equal(3, quickReplies.Count);
        Assert.Equal("Jak správně aplikovat inzulín?", quickReplies[0]);
        Assert.Equal("Jaké jsou typy inzulínu?", quickReplies[1]);
        Assert.Equal("Co dělat při hypoglykémii?", quickReplies[2]);
    }

    [Fact]
    public void ParseQuickReplies_ReturnsEmpty_WhenNoBlock()
    {
        var response = "Zde je odpověď bez návrhů.";

        var (content, quickReplies) = MedicalAdvisorService.ParseQuickReplies(response);

        Assert.Equal("Zde je odpověď bez návrhů.", content);
        Assert.Empty(quickReplies);
    }

    [Fact]
    public void ParseQuickReplies_LimitsTo4()
    {
        var response = "Odpověď.\n\n[QUICK_REPLIES]\nOtázka 1\nOtázka 2\nOtázka 3\nOtázka 4\nOtázka 5\nOtázka 6\n[/QUICK_REPLIES]";

        var (content, quickReplies) = MedicalAdvisorService.ParseQuickReplies(response);

        Assert.Equal("Odpověď.", content);
        Assert.Equal(4, quickReplies.Count);
        Assert.Equal("Otázka 4", quickReplies[3]);
    }

    [Fact]
    public void ParseQuickReplies_StripsBlockFromContent()
    {
        var response = "Toto je text.\n\n[QUICK_REPLIES]\nNávrh A\nNávrh B\n[/QUICK_REPLIES]";

        var (content, quickReplies) = MedicalAdvisorService.ParseQuickReplies(response);

        Assert.DoesNotContain("[QUICK_REPLIES]", content);
        Assert.DoesNotContain("[/QUICK_REPLIES]", content);
        Assert.DoesNotContain("Návrh A", content);
        Assert.Equal("Toto je text.", content);
    }

    [Fact]
    public void ParseQuickReplies_HandlesEmptyBlock()
    {
        var response = "Odpověď.\n\n[QUICK_REPLIES]\n[/QUICK_REPLIES]";

        var (content, quickReplies) = MedicalAdvisorService.ParseQuickReplies(response);

        Assert.Equal("Odpověď.", content);
        Assert.Empty(quickReplies);
    }

    [Fact]
    public void ParseQuickReplies_TrimsWhitespaceInReplies()
    {
        var response = "Odpověď.\n\n[QUICK_REPLIES]\n  Otázka s mezerami  \n  Další otázka  \n[/QUICK_REPLIES]";

        var (content, quickReplies) = MedicalAdvisorService.ParseQuickReplies(response);

        Assert.Equal(2, quickReplies.Count);
        Assert.Equal("Otázka s mezerami", quickReplies[0]);
        Assert.Equal("Další otázka", quickReplies[1]);
    }

    [Fact]
    public void ParseQuickReplies_HandlesBlockWithExtraNewlines()
    {
        var response = "Odpověď.\n\n[QUICK_REPLIES]\n\nOtázka 1\n\nOtázka 2\n\n[/QUICK_REPLIES]";

        var (content, quickReplies) = MedicalAdvisorService.ParseQuickReplies(response);

        Assert.Equal("Odpověď.", content);
        Assert.Equal(2, quickReplies.Count);
    }

    [Fact]
    public void ParseAssistantResponse_ExtractsMetadataAndPreservesQuickReplies()
    {
        var response = "Dávka vychází na 3 jednotky.\n\n[QUICK_REPLIES]\nCo dělat při vyšší glykémii po jídle?\nJak upravit dávku před sportem?\n[/QUICK_REPLIES]\n[RESPONSE_META]\nDOSE_GUIDANCE=true\nEMERGENCY=false\nLANGUAGE=cs\n[/RESPONSE_META]";

        var parsed = MedicalAdvisorService.ParseAssistantResponse(response);

        Assert.Equal("Dávka vychází na 3 jednotky.", parsed.Content);
        Assert.Equal(2, parsed.QuickReplies.Count);
        Assert.True(parsed.HasDoseGuidanceWarning);
        Assert.False(parsed.ShowEmergencyCallButton);
        Assert.Equal("cs", parsed.DetectedLanguage);
    }

    [Fact]
    public void ParseAssistantResponse_StripsMetadataFromVisibleContent()
    {
        var response = "Okamžitě podat glukagon a volat pomoc.\n[RESPONSE_META]\nDOSE_GUIDANCE=false\nEMERGENCY=true\nLANGUAGE=cs\n[/RESPONSE_META]";

        var parsed = MedicalAdvisorService.ParseAssistantResponse(response);

        Assert.Equal("Okamžitě podat glukagon a volat pomoc.", parsed.Content);
        Assert.DoesNotContain("RESPONSE_META", parsed.Content);
        Assert.True(parsed.ShowEmergencyCallButton);
        Assert.False(parsed.HasDoseGuidanceWarning);
    }

    [Fact]
    public void GetSystemPromptForAdvisor_UsesDedicatedPromptAndDocuments()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"MedicalAdvisorPromptTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            Directory.CreateDirectory(Path.Combine(tempDir, "Prompts"));
            Directory.CreateDirectory(Path.Combine(tempDir, "docs"));
            Directory.CreateDirectory(Path.Combine(tempDir, "docs", "diabetes2"));

            File.WriteAllText(Path.Combine(tempDir, "Prompts", "SystemPrompt.txt"), "DEFAULT PROMPT\n");
            File.WriteAllText(Path.Combine(tempDir, "Prompts", "Diabetes2SystemPrompt.txt"), "DIABETES2 PROMPT\n");
            File.WriteAllText(Path.Combine(tempDir, "docs", "default.txt"), "DEFAULT DOC");
            File.WriteAllText(Path.Combine(tempDir, "docs", "diabetes2", "runtime.txt"), "DIABETES2 DOC");

            var configuration = new ConfigurationBuilder()
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

            var registry = new AdvisorRegistry(configuration);
            var envMock = new Mock<IWebHostEnvironment>();
            envMock.SetupGet(e => e.ContentRootPath).Returns(tempDir);

            var documentService = new DocumentService(envMock.Object, registry, Mock.Of<ILogger<DocumentService>>());
            var service = new MedicalAdvisorService(
                Mock.Of<IChatCompletionService>(),
                documentService,
                registry,
                envMock.Object,
                Mock.Of<ILogger<MedicalAdvisorService>>());

            var prompt = service.GetSystemPromptForAdvisor("diabetes-2");

            Assert.StartsWith("DIABETES2 PROMPT", prompt);
            Assert.Contains("DIABETES2 DOC", prompt);
            Assert.DoesNotContain("DEFAULT DOC", prompt);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }
}
