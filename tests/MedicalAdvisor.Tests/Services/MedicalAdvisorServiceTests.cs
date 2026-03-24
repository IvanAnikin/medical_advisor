using MedicalAdvisor.Web.Services;

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
}
