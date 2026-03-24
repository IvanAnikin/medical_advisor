using MedicalAdvisor.Web.Models;
using MedicalAdvisor.Web.Services;
using MudBlazor;

namespace MedicalAdvisor.Tests.Services;

public class ThemeServiceTests
{
    [Fact]
    public void DefaultTheme_IsClinical()
    {
        var service = new ThemeService();

        Assert.Equal(AppTheme.Clinical, service.CurrentTheme);
    }

    [Fact]
    public void SetTheme_ChangesCurrentTheme()
    {
        var service = new ThemeService();

        service.SetTheme(AppTheme.Friendly);

        Assert.Equal(AppTheme.Friendly, service.CurrentTheme);
    }

    [Fact]
    public void SetTheme_FiresOnThemeChangedEvent()
    {
        var service = new ThemeService();
        var eventFired = false;
        service.OnThemeChanged += () => eventFired = true;

        service.SetTheme(AppTheme.Friendly);

        Assert.True(eventFired);
    }

    [Fact]
    public void SetTheme_DoesNotFireEvent_WhenThemeIsSame()
    {
        var service = new ThemeService();
        var eventFired = false;
        service.OnThemeChanged += () => eventFired = true;

        // Default is Clinical, setting it again should be a no-op
        service.SetTheme(AppTheme.Clinical);

        Assert.False(eventFired);
    }

    [Fact]
    public void CurrentMudTheme_ReturnsDifferentObjects_ForDifferentThemes()
    {
        var service = new ThemeService();

        var clinicalTheme = service.CurrentMudTheme;
        service.SetTheme(AppTheme.Friendly);
        var friendlyTheme = service.CurrentMudTheme;

        Assert.NotSame(clinicalTheme, friendlyTheme);
    }

    [Fact]
    public void ClinicalTheme_HasSoftBluePrimaryColor()
    {
        var service = new ThemeService();

        var theme = service.CurrentMudTheme;

        // MudColor.Value returns lowercase with alpha suffix (e.g. "#5b7fd6ff")
        Assert.StartsWith("#5b7fd6", theme.PaletteLight.Primary.Value);
    }

    [Fact]
    public void FriendlyTheme_HasSoftSagePrimaryColor()
    {
        var service = new ThemeService();
        service.SetTheme(AppTheme.Friendly);

        var theme = service.CurrentMudTheme;

        Assert.StartsWith("#5ba88a", theme.PaletteLight.Primary.Value);
    }

    [Fact]
    public void ClinicalTheme_HasCorrectSecondaryColor()
    {
        var service = new ThemeService();

        var theme = service.CurrentMudTheme;

        Assert.StartsWith("#8da4e2", theme.PaletteLight.Secondary.Value);
    }

    [Fact]
    public void FriendlyTheme_HasCorrectSecondaryColor()
    {
        var service = new ThemeService();
        service.SetTheme(AppTheme.Friendly);

        var theme = service.CurrentMudTheme;

        Assert.StartsWith("#82c4a8", theme.PaletteLight.Secondary.Value);
    }

    [Fact]
    public void ClinicalTheme_AppBarBackground_IsWhite()
    {
        var service = new ThemeService();

        var theme = service.CurrentMudTheme;

        Assert.StartsWith("#ffffff", theme.PaletteLight.AppbarBackground.Value);
    }

    [Fact]
    public void FriendlyTheme_AppBarBackground_IsWhite()
    {
        var service = new ThemeService();
        service.SetTheme(AppTheme.Friendly);

        var theme = service.CurrentMudTheme;

        Assert.StartsWith("#ffffff", theme.PaletteLight.AppbarBackground.Value);
    }

    [Fact]
    public void SetTheme_CanSwitchBackAndForth()
    {
        var service = new ThemeService();

        service.SetTheme(AppTheme.Friendly);
        Assert.Equal(AppTheme.Friendly, service.CurrentTheme);

        service.SetTheme(AppTheme.Clinical);
        Assert.Equal(AppTheme.Clinical, service.CurrentTheme);

        Assert.StartsWith("#5b7fd6", service.CurrentMudTheme.PaletteLight.Primary.Value);
    }

    [Fact]
    public void SetTheme_FiresEventOnEachDistinctChange()
    {
        var service = new ThemeService();
        var fireCount = 0;
        service.OnThemeChanged += () => fireCount++;

        service.SetTheme(AppTheme.Friendly);  // fires
        service.SetTheme(AppTheme.Friendly);  // same, no fire
        service.SetTheme(AppTheme.Clinical);  // fires

        Assert.Equal(2, fireCount);
    }

    [Fact]
    public void ThemeCssClass_ReturnsClinical_ByDefault()
    {
        var service = new ThemeService();

        Assert.Equal("theme-clinical", service.ThemeCssClass);
    }

    [Fact]
    public void ThemeCssClass_ReturnsFriendly_WhenFriendlyTheme()
    {
        var service = new ThemeService();
        service.SetTheme(AppTheme.Friendly);

        Assert.Equal("theme-friendly", service.ThemeCssClass);
    }
}
