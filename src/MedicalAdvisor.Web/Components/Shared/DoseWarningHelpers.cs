namespace MedicalAdvisor.Web.Components.Shared;

/// <summary>
/// Static helper methods for dose warning text generation.
/// Extracted for unit testability.
/// </summary>
public static class DoseWarningHelpers
{
    /// <summary>
    /// Returns the verbatim Czech diabetes-2 disclaimer text (multi-sentence regulatory).
    /// This is used when advisor is diabetes-2 and HasDoseGuidanceWarning is true.
    /// </summary>
    public static string GetDiabetes2DisclaimerText()
    {
        return @"Důležité upozornění: Toto je pouze edukační nástroj a není certifikovaným zdravotnickým prostředkem. Tento poradce, na rozdíl o vašeho lékaře, o vás nemá detailní informace a výpočet dávky inzulin, výběr typu inzulinu nebo jiných přípravků k léčbě diabetu a jejich dávkování je proto pouze orientační. Doporučujeme proto vždy konzultaci lékaře. Při aplikaci vyšší než potřebné dávky inzulinu či jiných přípravků k léčbě diabetu hrozí hypoglykemie.";
    }

    /// <summary>
    /// Returns the short legacy dose warning text (not diabetes-2 specific).
    /// </summary>
    public static string GetShortDoseWarningText(string? detectedLanguage)
    {
        var normalizedLanguage = GetNormalizedLanguage(detectedLanguage);
        return normalizedLanguage switch
        {
            "cs" => "Toto je edukační nástroj a není certifikovaným zdravotnickým prostředkem.",
            _ => "This is an educational tool and not a certified medical tool."
        };
    }

    private static string GetNormalizedLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return "en";

        return language.StartsWith("cs", StringComparison.OrdinalIgnoreCase)
            ? "cs"
            : "en";
    }
}
