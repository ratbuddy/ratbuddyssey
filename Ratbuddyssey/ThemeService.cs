using Avalonia;
using Avalonia.Styling;

namespace Ratbuddyssey;

/// <summary>
/// Centralizes light/dark/system theme switching for the app.
/// </summary>
public static class ThemeService
{
    public static ThemeVariant Current
    {
        get => Application.Current?.RequestedThemeVariant ?? ThemeVariant.Default;
        set
        {
            if (Application.Current != null)
            {
                Application.Current.RequestedThemeVariant = value;
            }
        }
    }

    public static bool IsDark
    {
        get
        {
            var v = Current;
            if (v == ThemeVariant.Dark) return true;
            if (v == ThemeVariant.Light) return false;
            // Default = follow OS. Probe the actual variant resolved against the platform.
            var actual = Application.Current?.ActualThemeVariant;
            return actual == ThemeVariant.Dark;
        }
    }

    public static void SetLight() => Current = ThemeVariant.Light;
    public static void SetDark() => Current = ThemeVariant.Dark;
    public static void SetSystem() => Current = ThemeVariant.Default;

    /// <summary>Maps a string ("Light"/"Dark"/"System") onto the matching ThemeVariant.</summary>
    public static ThemeVariant Parse(string value) => value switch
    {
        "Light" => ThemeVariant.Light,
        "Dark" => ThemeVariant.Dark,
        _ => ThemeVariant.Default,
    };

    /// <summary>Inverse of <see cref="Parse"/>.</summary>
    public static string ToSettingValue(ThemeVariant v)
    {
        if (v == ThemeVariant.Light) return "Light";
        if (v == ThemeVariant.Dark) return "Dark";
        return "System";
    }
}
