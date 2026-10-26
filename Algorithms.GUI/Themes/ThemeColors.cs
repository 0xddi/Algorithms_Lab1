using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace Algorithms.GUI.Themes;

/// <summary>
/// Colors for elements built in code (ScottPlot charts, 3D surface, legends).
/// XAML uses the same roles from Themes/Palette.axaml; keep both in sync.
/// </summary>
public static class ThemeColors
{
    public static bool IsLight => Application.Current?.ActualThemeVariant == ThemeVariant.Light;

    private static string Pick(string dark, string light) => IsLight ? light : dark;

    public static string PanelBackground => Pick("#252526", "#FFFFFF");
    public static string PlotDataBackground => Pick("#1E1E1E", "#FAFAFA");
    public static string Border => Pick("#3E3E42", "#D4D4D8");
    public static string Text => Pick("#D4D4D4", "#1F1F1F");
    public static string TextSecondary => Pick("#CCCCCC", "#3B3B3B");
    public static string Axes => Pick("#B4B4B4", "#5F5F5F");
    public static string GridLine => Pick("#333337", "#E6E6E9");
    public static string Accent => Pick("#3794FF", "#0066CC");
    public static string Theory => Pick("#FFB35C", "#B35400");

    /// <summary>
    /// Categorical colors for chart series. Light variants are darker so lines keep
    /// at least 3:1 contrast against the white plot background.
    /// </summary>
    public static string[] Series => IsLight ? LightSeries : DarkSeries;

    private static readonly string[] DarkSeries =
    {
        "#3794FF", "#FF7EB6", "#5EE0C0", "#B794F6", "#8BD450", "#FF8A65", "#FFD54F",
        "#4DD0E1", "#F06292", "#AED581", "#9575CD", "#E57373", "#4FC3F7", "#DCE775", "#BA68C8"
    };

    private static readonly string[] LightSeries =
    {
        "#0066CC", "#C2185B", "#00796B", "#6A3FB5", "#2E7D32", "#D84315", "#9A6700",
        "#00838F", "#AD1457", "#558B2F", "#4527A0", "#C62828", "#0277BD", "#7A6F00", "#8E24AA"
    };

    public static IBrush Brush(string hex) => SolidColorBrush.Parse(hex);

    public static ScottPlot.Color Plot(string hex) => ScottPlot.Color.FromHex(hex);
}
