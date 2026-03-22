using MedicalAdvisor.Web.Models;
using MudBlazor;

namespace MedicalAdvisor.Web.Services;

public class ThemeService
{
    private static readonly MudTheme ClinicalTheme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#1565C0",
            Secondary = "#42A5F5",
            AppbarBackground = "#1565C0",
            Background = "#FAFAFA",
            Surface = "#FFFFFF",
        },
    };

    private static readonly MudTheme FriendlyTheme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#00897B",
            Secondary = "#4DB6AC",
            AppbarBackground = "#00897B",
            Background = "#FFF8F0",
            Surface = "#FFFFFF",
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "12px",
        },
    };

    public AppTheme CurrentTheme { get; private set; } = AppTheme.Clinical;

    public MudTheme CurrentMudTheme => CurrentTheme switch
    {
        AppTheme.Friendly => FriendlyTheme,
        _ => ClinicalTheme,
    };

    public event Action? OnThemeChanged;

    public void SetTheme(AppTheme theme)
    {
        if (CurrentTheme == theme) return;

        CurrentTheme = theme;
        OnThemeChanged?.Invoke();
    }
}
