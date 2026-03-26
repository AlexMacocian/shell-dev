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

    /// <summary>
    /// Mixes two "#RRGGBB" colors. Amount 0.0 = all color1, 1.0 = all color2.
    /// </summary>
    public static string MixColors(string hex1, string hex2, double amount)
    {
        var c1 = hex1.TrimStart('#');
        var c2 = hex2.TrimStart('#');
        var r = (int)(int.Parse(c1[..2], NumberStyles.HexNumber) * (1 - amount) + int.Parse(c2[..2], NumberStyles.HexNumber) * amount);
        var g = (int)(int.Parse(c1[2..4], NumberStyles.HexNumber) * (1 - amount) + int.Parse(c2[2..4], NumberStyles.HexNumber) * amount);
        var b = (int)(int.Parse(c1[4..6], NumberStyles.HexNumber) * (1 - amount) + int.Parse(c2[4..6], NumberStyles.HexNumber) * amount);
        return $"#{r:X2}{g:X2}{b:X2}";
    }
}
