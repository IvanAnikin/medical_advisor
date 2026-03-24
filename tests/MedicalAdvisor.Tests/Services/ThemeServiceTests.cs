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
    public void DefaultDarkMode_IsFalse()
    {
        var service = new ThemeService();

        Assert.False(service.IsDarkMode);
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

        service.SetTheme(AppTheme.Clinical);

        Assert.False(eventFired);
    }

    [Fact]
    public void SetDarkMode_ChangesIsDarkMode()
    {
        var service = new ThemeService();

        service.SetDarkMode(true);

        Assert.True(service.IsDarkMode);
    }

    [Fact]
    public void SetDarkMode_FiresOnThemeChangedEvent()
    {
        var service = new ThemeService();
        var eventFired = false;
        service.OnThemeChanged += () => eventFired = true;

        service.SetDarkMode(true);

        Assert.True(eventFired);
    }

    [Fact]
    public void SetDarkMode_DoesNotFireEvent_WhenSameValue()
    {
        var service = new ThemeService();
        var eventFired = false;
        service.OnThemeChanged += () => eventFired = true;

        service.SetDarkMode(false);

        Assert.False(eventFired);
    }

    [Fact]
    public void SetDarkMode_CanToggleBackAndForth()
    {
        var service = new ThemeService();

        service.SetDarkMode(true);
        Assert.True(service.IsDarkMode);

        service.SetDarkMode(false);
        Assert.False(service.IsDarkMode);
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
    public void ClinicalTheme_HasDarkPalette()
    {
        var service = new ThemeService();

        var theme = service.CurrentMudTheme;

        Assert.StartsWith("#7c9bf0", theme.PaletteDark.Primary.Value);
    }

    [Fact]
    public void FriendlyTheme_HasDarkPalette()
    {
        var service = new ThemeService();
        service.SetTheme(AppTheme.Friendly);

        var theme = service.CurrentMudTheme;

        Assert.StartsWith("#6bc4a0", theme.PaletteDark.Primary.Value);
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

    [Fact]
    public void ThemeCssClass_ReturnsDarkMode_WhenDarkEnabled()
    {
        var service = new ThemeService();
        service.SetDarkMode(true);

        Assert.Equal("theme-clinical dark-mode", service.ThemeCssClass);
    }

    [Fact]
    public void ThemeCssClass_ReturnsFriendlyDarkMode_WhenBothSet()
    {
        var service = new ThemeService();
        service.SetTheme(AppTheme.Friendly);
        service.SetDarkMode(true);

        Assert.Equal("theme-friendly dark-mode", service.ThemeCssClass);
    }

    [Fact]
    public void ThemeAndDarkMode_AreIndependent()
    {
        var service = new ThemeService();

        service.SetDarkMode(true);
        service.SetTheme(AppTheme.Friendly);

        Assert.Equal(AppTheme.Friendly, service.CurrentTheme);
        Assert.True(service.IsDarkMode);

        service.SetDarkMode(false);
        Assert.Equal(AppTheme.Friendly, service.CurrentTheme);
        Assert.False(service.IsDarkMode);
    }

    [Fact]
    public void EventFires_ForBothThemeAndDarkModeChanges()
    {
        var service = new ThemeService();
        var fireCount = 0;
        service.OnThemeChanged += () => fireCount++;

        service.SetTheme(AppTheme.Friendly);  // fires
        service.SetDarkMode(true);             // fires
        service.SetTheme(AppTheme.Clinical);   // fires
        service.SetDarkMode(false);            // fires

        Assert.Equal(4, fireCount);
    }
}
