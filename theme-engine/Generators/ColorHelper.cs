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

    /// <summary>
    /// Lightens a color by mixing with white. Amount 0.0 = original, 1.0 = white.
    /// </summary>
    public static string Lighten(string hex, double amount) => MixColors(hex, "#FFFFFF", amount);

    /// <summary>
    /// Darkens a color by mixing with black. Amount 0.0 = original, 1.0 = black.
    /// </summary>
    public static string Darken(string hex, double amount) => MixColors(hex, "#000000", amount);

    /// <summary>
    /// Shifts the hue of a "#RRGGBB" color by the given degrees.
    /// </summary>
    public static string ShiftHue(string hex, double degrees)
    {
        var (h, s, l) = HexToHsl(hex);
        h = ((h + degrees) % 360 + 360) % 360;
        return HslToHex(h, s, l);
    }

    /// <summary>
    /// Adjusts saturation of a "#RRGGBB" color. Positive increases, negative decreases.
    /// </summary>
    public static string AdjustSaturation(string hex, double amount)
    {
        var (h, s, l) = HexToHsl(hex);
        s = Math.Clamp(s + amount, 0, 1);
        return HslToHex(h, s, l);
    }

    /// <summary>
    /// Converts "#RRGGBB" to HSL (H: 0–360, S: 0–1, L: 0–1).
    /// </summary>
    public static (double H, double S, double L) HexToHsl(string hex)
    {
        var clean = hex.TrimStart('#');
        var r = int.Parse(clean[..2], NumberStyles.HexNumber) / 255.0;
        var g = int.Parse(clean[2..4], NumberStyles.HexNumber) / 255.0;
        var b = int.Parse(clean[4..6], NumberStyles.HexNumber) / 255.0;

        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var l = (max + min) / 2.0;

        if (max == min)
            return (0, 0, l);

        var d = max - min;
        var s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);

        double h;
        if (max == r)
            h = ((g - b) / d + (g < b ? 6 : 0)) * 60;
        else if (max == g)
            h = ((b - r) / d + 2) * 60;
        else
            h = ((r - g) / d + 4) * 60;

        return (h, s, l);
    }

    /// <summary>
    /// Converts HSL (H: 0–360, S: 0–1, L: 0–1) to "#RRGGBB".
    /// </summary>
    public static string HslToHex(double h, double s, double l)
    {
        if (s == 0)
        {
            var v = (int)Math.Round(l * 255);
            return $"#{v:X2}{v:X2}{v:X2}";
        }

        var q = l < 0.5 ? l * (1 + s) : l + s - l * s;
        var p = 2 * l - q;

        var r = HueToRgb(p, q, h / 360.0 + 1.0 / 3.0);
        var g = HueToRgb(p, q, h / 360.0);
        var b = HueToRgb(p, q, h / 360.0 - 1.0 / 3.0);

        return $"#{(int)Math.Round(r * 255):X2}{(int)Math.Round(g * 255):X2}{(int)Math.Round(b * 255):X2}";
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1.0 / 6.0) return p + (q - p) * 6 * t;
        if (t < 1.0 / 2.0) return q;
        if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6;
        return p;
    }

    /// <summary>
    /// Computes WCAG 2.x relative luminance for a "#RRGGBB" color.
    /// Returns a value between 0 (black) and 1 (white).
    /// </summary>
    public static double RelativeLuminance(string hex)
    {
        var clean = hex.TrimStart('#');
        var rs = int.Parse(clean[..2], NumberStyles.HexNumber) / 255.0;
        var gs = int.Parse(clean[2..4], NumberStyles.HexNumber) / 255.0;
        var bs = int.Parse(clean[4..6], NumberStyles.HexNumber) / 255.0;

        var r = rs <= 0.03928 ? rs / 12.92 : Math.Pow((rs + 0.055) / 1.055, 2.4);
        var g = gs <= 0.03928 ? gs / 12.92 : Math.Pow((gs + 0.055) / 1.055, 2.4);
        var b = bs <= 0.03928 ? bs / 12.92 : Math.Pow((bs + 0.055) / 1.055, 2.4);

        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }

    /// <summary>
    /// Computes the WCAG contrast ratio between two "#RRGGBB" colors.
    /// Returns a value between 1 (identical) and 21 (black on white).
    /// </summary>
    public static double ContrastRatio(string hex1, string hex2)
    {
        var l1 = RelativeLuminance(hex1);
        var l2 = RelativeLuminance(hex2);
        var lighter = Math.Max(l1, l2);
        var darker = Math.Min(l1, l2);
        return (lighter + 0.05) / (darker + 0.05);
    }

    /// <summary>
    /// Adjusts the lightness (and, if necessary, the saturation) of a
    /// foreground color until it meets the minimum WCAG contrast ratio
    /// against the given background. Darkens on light backgrounds and
    /// lightens on dark backgrounds. Saturation is only reduced as a last
    /// resort, so on-theme hues are preserved when possible.
    /// </summary>
    public static string EnsureContrast(string fg, string bg, double minRatio = 4.5)
    {
        if (ContrastRatio(fg, bg) >= minRatio)
            return fg;

        var bgLum = RelativeLuminance(bg);
        var fgLum = RelativeLuminance(fg);
        var (h, s, l) = HexToHsl(fg);

        // Step 1: walk lightness in whichever direction widens the gap.
        // Choosing direction by fg-vs-bg (rather than bg vs midpoint) handles
        // mid-luminance backgrounds (e.g. selection rectangles) correctly.
        var step = fgLum >= bgLum ? 0.02 : -0.02;
        // If fg ≈ bg, prefer the direction with more headroom.
        if (Math.Abs(fgLum - bgLum) < 0.01)
            step = bgLum > 0.5 ? -0.02 : 0.02;

        for (var i = 0; i < 80; i++)
        {
            l = Math.Clamp(l + step, 0.02, 0.98);
            var candidate = HslToHex(h, s, l);
            if (ContrastRatio(candidate, bg) >= minRatio)
                return candidate;
        }

        // Step 2: lightness alone wasn't enough (very saturated hue versus a
        // very dark/light bg). Bleed saturation while continuing to push
        // lightness; this is required for hues like deep red on near-black.
        for (var i = 0; i < 40; i++)
        {
            s = Math.Max(0, s - 0.04);
            l = Math.Clamp(l + step, 0.02, 0.98);
            var candidate = HslToHex(h, s, l);
            if (ContrastRatio(candidate, bg) >= minRatio)
                return candidate;
        }

        return HslToHex(h, s, l);
    }

    /// <summary>
    /// Ensures a set of foreground colors are perceptually distinct from each other
    /// against a given background. Colors that are too close in perceived lightness
    /// or hue are nudged apart. Returns adjusted colors in the same order.
    /// </summary>
    public static string[] EnsureDistinct(string[] colors, string bg, double minHueDelta = 25, double minLightnessDelta = 0.08)
    {
        var result = new (double H, double S, double L)[colors.Length];
        for (var i = 0; i < colors.Length; i++)
            result[i] = HexToHsl(colors[i]);

        // Sort by hue to find close neighbors, then nudge apart
        for (var pass = 0; pass < 3; pass++)
        {
            for (var i = 0; i < result.Length; i++)
            {
                for (var j = i + 1; j < result.Length; j++)
                {
                    var hueDiff = Math.Abs(result[i].H - result[j].H);
                    if (hueDiff > 180) hueDiff = 360 - hueDiff;

                    var lDiff = Math.Abs(result[i].L - result[j].L);

                    // If hues are close AND lightness is close, nudge lightness apart
                    if (hueDiff < minHueDelta && lDiff < minLightnessDelta)
                    {
                        var bgLum = RelativeLuminance(bg);
                        if (bgLum > 0.5)
                        {
                            // Light bg: make one darker, one slightly less dark
                            result[i].L = Math.Clamp(result[i].L - minLightnessDelta / 2, 0.10, 0.85);
                            result[j].L = Math.Clamp(result[j].L + minLightnessDelta / 2, 0.10, 0.85);
                        }
                        else
                        {
                            // Dark bg: make one lighter, one slightly less light
                            result[i].L = Math.Clamp(result[i].L + minLightnessDelta / 2, 0.15, 0.90);
                            result[j].L = Math.Clamp(result[j].L - minLightnessDelta / 2, 0.15, 0.90);
                        }
                    }
                }
            }
        }

        var output = new string[colors.Length];
        for (var i = 0; i < result.Length; i++)
            output[i] = HslToHex(result[i].H, result[i].S, result[i].L);

        return output;
    }
}
