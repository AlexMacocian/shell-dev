using System.Globalization;

namespace ThemeEngine.Generators;

public static class ColorHelper
{
    /// <summary>
    /// Converts "#RRGGBB" to "RRGGBBaa" for Hyprland rgba() format.
    /// </summary>
    public static string ToHyprRgba(string hex, string alpha = "ee")
    {
        var clean = hex.TrimStart('#');
        return $"{clean}{alpha}";
    }

    /// <summary>
    /// Converts "#RRGGBB" to "rgba(R, G, B, opacity)" for CSS.
    /// </summary>
    public static string ToCssRgba(string hex, double opacity)
    {
        var clean = hex.TrimStart('#');
        var r = int.Parse(clean[..2], NumberStyles.HexNumber);
        var g = int.Parse(clean[2..4], NumberStyles.HexNumber);
        var b = int.Parse(clean[4..6], NumberStyles.HexNumber);
        return $"rgba({r}, {g}, {b}, {opacity.ToString(CultureInfo.InvariantCulture)})";
    }
}
