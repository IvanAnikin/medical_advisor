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
    };

    private static readonly MudTheme DarkTheme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#7C9BF0",
            Secondary = "#5BA88A",
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

    public bool IsDarkMode => CurrentTheme == AppTheme.Dark;

    public MudTheme CurrentMudTheme => CurrentTheme switch
    {
        AppTheme.Friendly => FriendlyTheme,
        AppTheme.Dark => DarkTheme,
        _ => ClinicalTheme,
    };

    /// <summary>
    /// CSS class applied to the chat container to switch CSS custom property sets.
    /// </summary>
    public string ThemeCssClass => CurrentTheme switch
    {
        AppTheme.Friendly => "theme-friendly",
        AppTheme.Dark => "theme-dark",
        _ => "theme-clinical",
    };

    public event Action? OnThemeChanged;

    public void SetTheme(AppTheme theme)
    {
        if (CurrentTheme == theme) return;

        CurrentTheme = theme;
        OnThemeChanged?.Invoke();
    }
}
