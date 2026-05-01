using MedicalAdvisor.Web.Components.Shared;

namespace MedicalAdvisor.Tests.Components;

public class DoseWarningHelpersTests
{
    [Fact]
    public void GetDiabetes2DisclaimerText_ReturnsVerbatimCzechText()
    {
        var text = DoseWarningHelpers.GetDiabetes2DisclaimerText();

        // Verify verbatim text is returned
        Assert.NotNull(text);
        Assert.NotEmpty(text);
        
        // Check for key phrases to ensure verbatim reproduction
        Assert.Contains("Důležité upozornění:", text);
        Assert.Contains("edukační nástroj", text);
        Assert.Contains("na rozdíl o vašeho lékaře", text); // Note: preserve the specific phrasing, not "od"
        Assert.Contains("orientační", text);
        Assert.Contains("konzultaci lékaře", text);
        Assert.Contains("hypoglykemie", text);
    }

    [Fact]
    public void GetDiabetes2DisclaimerText_ContainsCorrectPhrasingNotCorrected()
    {
        var text = DoseWarningHelpers.GetDiabetes2DisclaimerText();
        
        // Ensure the specific phrasing "na rozdíl o vašeho lékaře" is preserved verbatim
        // (not "corrected" to "na rozdíl od vašeho lékaře")
        Assert.Contains("na rozdíl o vašeho lékaře", text);
        Assert.DoesNotContain("na rozdíl od vašeho lékaře", text);
    }

    [Fact]
    public void GetShortDoseWarningText_ReturnsCzechWhenLanguageIsCz()
    {
        var text = DoseWarningHelpers.GetShortDoseWarningText("cs");

        Assert.Equal("Toto je edukační nástroj a není certifikovaným zdravotnickým prostředkem.", text);
    }

    [Fact]
    public void GetShortDoseWarningText_ReturnsCzechWhenLanguageStartsWithCz()
    {
        var text = DoseWarningHelpers.GetShortDoseWarningText("cs-CZ");

        Assert.Equal("Toto je edukační nástroj a není certifikovaným zdravotnickým prostředkem.", text);
    }

    [Fact]
    public void GetShortDoseWarningText_ReturnsEnglishWhenLanguageIsEnglish()
    {
        var text = DoseWarningHelpers.GetShortDoseWarningText("en");

        Assert.Equal("This is an educational tool and not a certified medical tool.", text);
    }

    [Fact]
    public void GetShortDoseWarningText_ReturnsEnglishWhenLanguageIsUnknown()
    {
        var text = DoseWarningHelpers.GetShortDoseWarningText("sk");

        Assert.Equal("This is an educational tool and not a certified medical tool.", text);
    }

    [Fact]
    public void GetShortDoseWarningText_ReturnsEnglishWhenLanguageIsNull()
    {
        var text = DoseWarningHelpers.GetShortDoseWarningText(null);

        Assert.Equal("This is an educational tool and not a certified medical tool.", text);
    }

    [Fact]
    public void GetShortDoseWarningText_ReturnsEnglishWhenLanguageIsEmpty()
    {
        var text = DoseWarningHelpers.GetShortDoseWarningText(string.Empty);

        Assert.Equal("This is an educational tool and not a certified medical tool.", text);
    }
}
