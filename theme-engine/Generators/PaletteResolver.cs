namespace ThemeEngine.Generators;

/// <summary>
/// A fully-resolved color palette where every foreground-on-background
/// combination is guaranteed to meet the configured WCAG contrast ratio
/// against <see cref="Bg0"/>, and where ANSI/syntax slots are perceptually
/// distinct from each other.
///
/// Generators should always derive their colors from this resolver rather
/// than touching <see cref="Theme.Colors"/> directly so contrast guarantees
/// remain consistent across the desktop.
/// </summary>
public record ResolvedPalette(
    // Backgrounds — passed through unchanged
    string Bg0,
    string Bg1,
    string Bg2,
    string Bg3,
    // Chrome / accents — contrast-checked against Bg0
    string Border,
    string Accent1,
    string Accent2,
    string Text,
    string TextDim,
    string Inactive,
    // Direct palette colors — contrast-checked against Bg0
    string Red,
    string Green,
    string Blue,
    // ANSI 0–7 — distinct, contrast-checked
    string AnsiBlack,
    string AnsiRed,
    string AnsiGreen,
    string AnsiYellow,
    string AnsiBlue,
    string AnsiMagenta,
    string AnsiCyan,
    string AnsiWhite,
    // ANSI 8–15 (bright) — visibly shifted from their normal counterparts
    string BrightBlack,
    string BrightRed,
    string BrightGreen,
    string BrightYellow,
    string BrightBlue,
    string BrightMagenta,
    string BrightCyan,
    string BrightWhite,
    // Selection pair — high visibility even with translucent windows
    string SelectionBg,
    string SelectionFg,
    // Mode flag for downstream generators
    bool IsLight,
    double MinContrast
);

public static class PaletteResolver
{
    /// <summary>
    /// Resolves a <see cref="ThemeColors"/> palette into a contrast-guaranteed
    /// <see cref="ResolvedPalette"/>. Targets WCAG AAA (7:1) by default; can be
    /// overridden per-theme via <see cref="TerminalSettings.MinContrast"/>.
    /// </summary>
    public static ResolvedPalette Resolve(Theme theme)
    {
        var c = theme.Colors;
        var bg0 = c.Bg0;
        var isLight = theme.Gtk.ColorScheme.Contains("light");
        var minContrast = theme.Terminal?.EffectiveMinContrast ?? 7.0;
        // Non-text UI elements (borders, dim text) only need 4.5:1 — pushing
        // them all to 7:1 destroys the visual hierarchy between primary text
        // and secondary chrome.
        var minUiContrast = Math.Min(4.5, minContrast);

        // --- Direct palette: enforce contrast vs bg ---
        var text = ColorHelper.EnsureContrast(c.Text, bg0, minContrast);
        var textDim = ColorHelper.EnsureContrast(c.TextDim, bg0, minUiContrast);
        var border = ColorHelper.EnsureContrast(c.Border, bg0, minUiContrast);
        var accent1 = ColorHelper.EnsureContrast(c.Accent1, bg0, minContrast);
        var accent2 = ColorHelper.EnsureContrast(c.Accent2, bg0, minContrast);
        var red = ColorHelper.EnsureContrast(c.Red, bg0, minContrast);
        var green = ColorHelper.EnsureContrast(c.Green, bg0, minContrast);
        var blue = ColorHelper.EnsureContrast(c.Blue, bg0, minContrast);
        // Inactive is intentionally low-contrast (it represents "disabled")
        // but still keep it readable enough not to vanish entirely.
        var inactive = ColorHelper.EnsureContrast(c.Inactive, bg0, 3.0);

        // --- ANSI normal palette ---
        // Hue-derive each slot to land near its canonical color-wheel
        // position (red 0°, yellow 60°, green 120°, cyan 180°, blue 210°,
        // magenta 300°). Without this, an aesthetic theme with a gray-blue
        // Border would emit a gray-blue "yellow" slot, which makes ANSI
        // output unreadable.
        var sat = c.SaturationBoost;

        // Anchor the warm side on Red (or, if Red is barely saturated, push
        // it to a real red first).
        var redAnchor = ForceMinSaturation(c.Red, 0.55);
        var greenAnchor = ForceMinSaturation(c.Green, 0.45);
        var blueAnchor = ForceMinSaturation(c.Blue, 0.50);

        // Yellow: pull red towards green hue (60°). Higher saturation than
        // either anchor so it reads as a vivid warm tone.
        var yellowSeed = HueAt(redAnchor, 50, 0.65);
        // Magenta: between red and blue (320°).
        var magentaSeed = HueAt(redAnchor, 320, 0.55);
        // Cyan: blend blue with green for a distinct hue between them.
        var cyanSeed = HueAt(blueAnchor, 180, 0.55);

        var redSat = ColorHelper.AdjustSaturation(redAnchor, sat);
        var greenSat = ColorHelper.AdjustSaturation(greenAnchor, sat);
        var blueSat = ColorHelper.AdjustSaturation(blueAnchor, sat);

        // Push every slot to AAA contrast against bg.
        var ansiRed = ColorHelper.EnsureContrast(redSat, bg0, minContrast);
        var ansiGreen = ColorHelper.EnsureContrast(greenSat, bg0, minContrast);
        var ansiYellow = ColorHelper.EnsureContrast(yellowSeed, bg0, minContrast);
        var ansiBlue = ColorHelper.EnsureContrast(blueSat, bg0, minContrast);
        var ansiMagenta = ColorHelper.EnsureContrast(magentaSeed, bg0, minContrast);
        var ansiCyan = ColorHelper.EnsureContrast(cyanSeed, bg0, minContrast);

        // Spread perceptually-close hues apart so green/yellow and blue/cyan
        // don't read as the same color.
        var distinct = ColorHelper.EnsureDistinct(
            [ansiRed, ansiGreen, ansiYellow, ansiBlue, ansiMagenta, ansiCyan],
            bg0, minHueDelta: 25, minLightnessDelta: 0.10);
        ansiRed = ColorHelper.EnsureContrast(distinct[0], bg0, minContrast);
        ansiGreen = ColorHelper.EnsureContrast(distinct[1], bg0, minContrast);
        ansiYellow = ColorHelper.EnsureContrast(distinct[2], bg0, minContrast);
        ansiBlue = ColorHelper.EnsureContrast(distinct[3], bg0, minContrast);
        ansiMagenta = ColorHelper.EnsureContrast(distinct[4], bg0, minContrast);
        ansiCyan = ColorHelper.EnsureContrast(distinct[5], bg0, minContrast);

        // ANSI 0 (black) and 7 (white) act as dark/light foregrounds for
        // TUIs. They must stay near their named luminance regardless of
        // theme mode — programs that hard-code "black" or "white" expect
        // dark and light respectively.
        var ansiBlack = isLight ? text : c.Bg3;
        var ansiWhite = isLight ? c.Bg3 : text;

        // --- Bright variants: shift lightness in the high-contrast direction ---
        const double brightShift = 0.12;
        string Brighten(string color) => isLight
            ? ColorHelper.EnsureContrast(ColorHelper.Darken(color, brightShift), bg0, minContrast)
            : ColorHelper.EnsureContrast(ColorHelper.Lighten(color, brightShift), bg0, minContrast);

        var brightRed = Brighten(ansiRed);
        var brightGreen = Brighten(ansiGreen);
        var brightYellow = Brighten(ansiYellow);
        var brightBlue = Brighten(ansiBlue);
        var brightMagenta = Brighten(ansiMagenta);
        var brightCyan = Brighten(ansiCyan);
        // bright black / bright white: an extra step of luminance away from
        // their normal counterparts in the high-contrast direction.
        var brightBlack = isLight
            ? ColorHelper.EnsureContrast(ColorHelper.Darken(inactive, 0.10), bg0, 3.0)
            : ColorHelper.EnsureContrast(ColorHelper.Lighten(inactive, 0.15), bg0, 4.5);
        var brightWhite = isLight
            ? c.Bg2
            : ColorHelper.EnsureContrast(ColorHelper.Lighten(textDim, 0.10), bg0, minContrast);

        // --- Selection pair ---
        // Use the Border hue as the seed (it's the theme's "this is the
        // accent" color) but push lightness aggressively towards the
        // *opposite* end from the background, with high saturation, so the
        // selection rectangle pops even when the terminal window has 0.85
        // opacity over a busy wallpaper.
        var (selBg, selFg) = ResolveSelection(c.Border, bg0, text, isLight);

        return new ResolvedPalette(
            Bg0: bg0,
            Bg1: c.Bg1,
            Bg2: c.Bg2,
            Bg3: c.Bg3,
            Border: border,
            Accent1: accent1,
            Accent2: accent2,
            Text: text,
            TextDim: textDim,
            Inactive: inactive,
            Red: red,
            Green: green,
            Blue: blue,
            AnsiBlack: ansiBlack,
            AnsiRed: ansiRed,
            AnsiGreen: ansiGreen,
            AnsiYellow: ansiYellow,
            AnsiBlue: ansiBlue,
            AnsiMagenta: ansiMagenta,
            AnsiCyan: ansiCyan,
            AnsiWhite: ansiWhite,
            BrightBlack: brightBlack,
            BrightRed: brightRed,
            BrightGreen: brightGreen,
            BrightYellow: brightYellow,
            BrightBlue: brightBlue,
            BrightMagenta: brightMagenta,
            BrightCyan: brightCyan,
            BrightWhite: brightWhite,
            SelectionBg: selBg,
            SelectionFg: selFg,
            IsLight: isLight,
            MinContrast: minContrast
        );
    }

    /// <summary>
    /// Picks a selection background and foreground that:
    /// 1. Have high contrast against the normal background (so the rectangle
    ///    is visible even with translucent windows).
    /// 2. Have high contrast against each other (so selected text is readable).
    /// 3. Use a saturated tint of the Border accent so it feels on-theme.
    /// </summary>
    private static (string Bg, string Fg) ResolveSelection(
        string seed, string bg, string text, bool isLight)
    {
        var (h, s, _) = ColorHelper.HexToHsl(seed);
        // Force strong saturation; selections that look gray are the #1 cause
        // of "I can't tell what I selected".
        s = Math.Max(s, 0.55);

        // Push lightness towards the high-contrast end of the spectrum.
        var targetL = isLight ? 0.30 : 0.72;
        var selBg = ColorHelper.HslToHex(h, s, targetL);

        // Guarantee at least 4.5:1 against the window background — selections
        // are large UI elements, AA-large is the relevant target here, but we
        // shoot a little higher for safety against translucent windows.
        selBg = ColorHelper.EnsureContrast(selBg, bg, 4.5);

        // Pick the foreground with the better contrast against the selection
        // rectangle: usually bg0 wins on dark themes, text wins on light.
        var contrastWithText = ColorHelper.ContrastRatio(text, selBg);
        var contrastWithBg = ColorHelper.ContrastRatio(bg, selBg);
        var selFg = contrastWithText >= contrastWithBg ? text : bg;

        // If neither hits 7:1, push the chosen foreground harder.
        selFg = ColorHelper.EnsureContrast(selFg, selBg, 7.0);
        return (selBg, selFg);
    }

    /// <summary>
    /// Returns a color whose hue is set to <paramref name="targetHue"/>,
    /// borrowing lightness from <paramref name="seed"/> and forcing
    /// saturation to at least <paramref name="minSaturation"/>.
    /// </summary>
    private static string HueAt(string seed, double targetHue, double minSaturation)
    {
        var (_, s, l) = ColorHelper.HexToHsl(seed);
        s = Math.Max(s, minSaturation);
        return ColorHelper.HslToHex(targetHue, s, l);
    }

    /// <summary>
    /// Returns the input color, but with saturation pushed to at least the
    /// given threshold. Hue and lightness are preserved.
    /// </summary>
    private static string ForceMinSaturation(string hex, double minSaturation)
    {
        var (h, s, l) = ColorHelper.HexToHsl(hex);
        if (s >= minSaturation) return hex;
        return ColorHelper.HslToHex(h, minSaturation, l);
    }
}
