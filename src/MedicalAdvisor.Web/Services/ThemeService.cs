using MedicalAdvisor.Web.Models;
using MudBlazor;

namespace MedicalAdvisor.Web.Services;

public class ThemeService
{
    private static readonly MudTheme ClinicalTheme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#5B7FD6",
            Secondary = "#8DA4E2",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#1A1D26",
            Background = "#F8F9FC",
            Surface = "#FFFFFF",
            TextPrimary = "#1A1D26",
            TextSecondary = "#6B7280",
            DrawerBackground = "#FFFFFF",
            LinesDefault = "#E5E7EB",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#7C9BF0",
            Secondary = "#8DA4E2",
            AppbarBackground = "#1E1F25",
            AppbarText = "#E4E6EB",
            Background = "#16171C",
            Surface = "#1E1F25",
            TextPrimary = "#E4E6EB",
            TextSecondary = "#9CA3AF",
            DrawerBackground = "#1E1F25",
            LinesDefault = "#2E3039",
        },
    };

    private static readonly MudTheme FriendlyTheme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#5BA88A",
            Secondary = "#82C4A8",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#1A1D26",
            Background = "#F7FAF8",
            Surface = "#FFFFFF",
            TextPrimary = "#1A1D26",
            TextSecondary = "#6B7280",
            DrawerBackground = "#FFFFFF",
            LinesDefault = "#E5E7EB",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#6BC4A0",
            Secondary = "#82C4A8",
            AppbarBackground = "#1E1F25",
            AppbarText = "#E4E6EB",
            Background = "#16171C",
            Surface = "#1E1F25",
            TextPrimary = "#E4E6EB",
            TextSecondary = "#9CA3AF",
            DrawerBackground = "#1E1F25",
            LinesDefault = "#2E3039",
        },
    };

    public AppTheme CurrentTheme { get; private set; } = AppTheme.Clinical;

    public bool IsDarkMode { get; private set; }

    public MudTheme CurrentMudTheme => CurrentTheme switch
    {
        AppTheme.Friendly => FriendlyTheme,
        _ => ClinicalTheme,
    };

    /// <summary>
    /// CSS classes applied to the theme root to switch CSS custom property sets.
    /// Returns e.g. "theme-clinical" or "theme-friendly dark-mode".
    /// </summary>
    public string ThemeCssClass
    {
        get
        {
            var theme = CurrentTheme == AppTheme.Friendly ? "theme-friendly" : "theme-clinical";
            return IsDarkMode ? $"{theme} dark-mode" : theme;
        }
    }

    public event Action? OnThemeChanged;

    public void SetTheme(AppTheme theme)
    {
        if (CurrentTheme == theme) return;
        CurrentTheme = theme;
        OnThemeChanged?.Invoke();
    }

    public void SetDarkMode(bool isDark)
    {
        if (IsDarkMode == isDark) return;
        IsDarkMode = isDark;
        OnThemeChanged?.Invoke();
    }
}
