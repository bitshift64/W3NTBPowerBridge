using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace W3NTBPowerBridge.Services;

/// <summary>
/// Applies the light or dark application color palette.
/// </summary>
public static class ThemeService
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaBorderColor = 34;
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;
    private const int DwmwaColorDefault = unchecked((int)0xFFFFFFFF);

    /// <summary>
    /// Applies the selected theme to application resources.
    /// </summary>
    /// <param name="darkModeEnabled">True to use the dark red operator theme.</param>
    public static void Apply(bool darkModeEnabled)
    {
        if (Application.Current is null)
        {
            return;
        }

        if (darkModeEnabled)
        {
            SetBrush("WindowBackgroundBrush", "#080808");
            SetBrush("PanelBackgroundBrush", "#121212");
            SetBrush("PanelBorderBrush", "#3A1A1A");
            SetBrush("PrimaryTextBrush", "#FF4D4D");
            SetBrush("SecondaryTextBrush", "#D93636");
            SetBrush("InputBackgroundBrush", "#1A1A1A");
            SetBrush("InputBorderBrush", "#6B2020");
            SetBrush("OnAirIdleBackgroundBrush", "#140808");
            SetBrush("OnAirIdleBorderBrush", "#3A1A1A");
            SetBrush("OnAirIdleTextBrush", "#7A2C2C");
            SetBrush("OnAirIdleLampBrush", "#4A1C1C");
        }
        else
        {
            SetBrush("WindowBackgroundBrush", "#F5F5F5");
            SetBrush("PanelBackgroundBrush", "#FFFFFF");
            SetBrush("PanelBorderBrush", "#DADADA");
            SetBrush("PrimaryTextBrush", "#111827");
            SetBrush("SecondaryTextBrush", "#444444");
            SetBrush("InputBackgroundBrush", "#FFFFFF");
            SetBrush("InputBorderBrush", "#ABADB3");
            SetBrush("OnAirIdleBackgroundBrush", "#F3F4F6");
            SetBrush("OnAirIdleBorderBrush", "#D1D5DB");
            SetBrush("OnAirIdleTextBrush", "#6B7280");
            SetBrush("OnAirIdleLampBrush", "#9CA3AF");
        }

        ApplyWindowFrameTheme(darkModeEnabled);
    }

    /// <summary>
    /// Applies the selected Windows frame theme to a specific window.
    /// </summary>
    /// <param name="window">Window to style.</param>
    /// <param name="darkModeEnabled">True to use the dark red operator frame.</param>
    public static void ApplyWindowFrame(Window window, bool darkModeEnabled)
    {
        ApplyWindowFrameToWindow(window, darkModeEnabled);
    }

    private static void SetBrush(string key, string color)
    {
        Application.Current.Resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    }

    private static void ApplyWindowFrameTheme(bool darkModeEnabled)
    {
        var window = Application.Current?.MainWindow;
        if (window is null)
        {
            return;
        }

        ApplyWindowFrameToWindow(window, darkModeEnabled);
    }

    private static void ApplyWindowFrameToWindow(Window window, bool darkModeEnabled)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var darkMode = darkModeEnabled ? 1 : 0;
        _ = DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref darkMode, sizeof(int));

        if (darkModeEnabled)
        {
            var borderColor = ToColorRef(0x3A, 0x1A, 0x1A);
            var captionColor = ToColorRef(0x08, 0x08, 0x08);
            var textColor = ToColorRef(0xFF, 0x4D, 0x4D);
            _ = DwmSetWindowAttribute(handle, DwmwaBorderColor, ref borderColor, sizeof(int));
            _ = DwmSetWindowAttribute(handle, DwmwaCaptionColor, ref captionColor, sizeof(int));
            _ = DwmSetWindowAttribute(handle, DwmwaTextColor, ref textColor, sizeof(int));
            return;
        }

        var defaultColor = DwmwaColorDefault;
        _ = DwmSetWindowAttribute(handle, DwmwaBorderColor, ref defaultColor, sizeof(int));
        _ = DwmSetWindowAttribute(handle, DwmwaCaptionColor, ref defaultColor, sizeof(int));
        _ = DwmSetWindowAttribute(handle, DwmwaTextColor, ref defaultColor, sizeof(int));
    }

    private static int ToColorRef(byte red, byte green, byte blue)
    {
        return red | (green << 8) | (blue << 16);
    }

    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int attributeValue, int attributeSize);
}
