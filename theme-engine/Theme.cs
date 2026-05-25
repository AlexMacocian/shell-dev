using System.Text.Json.Serialization;

namespace ThemeEngine;

public record Theme(
    string Name,
    ThemeColors Colors,
    HyprlandSettings Hyprland,
    FontSettings Font,
    GtkSettings Gtk,
    WaybarSettings Waybar,
    WallpaperSettings Wallpapers,
    TerminalSettings? Terminal = null,
    NvimSettings? Nvim = null
);

public record ThemeColors(
    string Bg0,
    string Bg1,
    string Bg2,
    string Bg3,
    string Border,
    string Accent1,
    string Accent2,
    string Text,
    [property: JsonPropertyName("text_dim")] string TextDim,
    string Red,
    string Green,
    string Blue,
    string Inactive,
    double? Saturation = null
)
{
    /// <summary>
    /// Saturation boost applied to derived syntax/terminal colors.
    /// Defaults to 0.10 if not specified in the theme JSON.
    /// </summary>
    public double SaturationBoost => Saturation ?? 0.10;
}

public record HyprlandSettings(
    [property: JsonPropertyName("border_size")] int BorderSize,
    int Rounding,
    [property: JsonPropertyName("gaps_in")] int GapsIn,
    [property: JsonPropertyName("gaps_out")] int GapsOut,
    [property: JsonPropertyName("shadow_range")] int ShadowRange,
    [property: JsonPropertyName("shadow_render_power")] int ShadowRenderPower,
    [property: JsonPropertyName("blur_size")] int BlurSize,
    [property: JsonPropertyName("blur_passes")] int BlurPasses,
    [property: JsonPropertyName("blur_vibrancy")] double BlurVibrancy,
    [property: JsonPropertyName("active_opacity")] double ActiveOpacity,
    [property: JsonPropertyName("inactive_opacity")] double InactiveOpacity,
    [property: JsonPropertyName("animation_speed_global")] double AnimationSpeedGlobal,
    [property: JsonPropertyName("animation_speed_border")] double AnimationSpeedBorder,
    [property: JsonPropertyName("animation_speed_windows")] double AnimationSpeedWindows,
    [property: JsonPropertyName("animation_speed_windows_in")] double AnimationSpeedWindowsIn,
    [property: JsonPropertyName("animation_speed_windows_out")] double AnimationSpeedWindowsOut,
    [property: JsonPropertyName("animation_speed_fade_in")] double AnimationSpeedFadeIn,
    [property: JsonPropertyName("animation_speed_fade_out")] double AnimationSpeedFadeOut,
    [property: JsonPropertyName("animation_speed_workspaces")] double AnimationSpeedWorkspaces
);

public record FontSettings(
    string Family,
    string[] Fallback,
    int Size
);

public record WaybarSettings(
    int Height,
    string Separator,
    double Opacity,
    [property: JsonPropertyName("border_width")] int BorderWidth,
    [property: JsonPropertyName("workspace_labels")] string[] WorkspaceLabels
);

public record WallpaperSettings(
    [property: JsonPropertyName("fit_mode")] string FitMode,
    string[] Images,
    string[] Videos,
    [property: JsonPropertyName("cycle_interval")] int CycleInterval,
    // Lottie source files (JSON). Converted to GIF at theme-apply time and
    // fed into the existing video pipeline via mpvpaper. Cached on-disk by
    // source-content hash so unchanged files are not re-rendered.
    string[]? Lotties = null
);

public record GtkSettings(
    [property: JsonPropertyName("color_scheme")] string ColorScheme,
    string Theme
);

/// <summary>
/// Terminal-specific tuning. All fields optional; sensible defaults are used
/// when omitted so existing themes don't have to declare this section.
/// </summary>
public record TerminalSettings(
    double? Opacity = null,
    [property: JsonPropertyName("min_contrast")] double? MinContrast = null
)
{
    /// <summary>
    /// Window background opacity for the terminal. Defaults to 1.0 (fully
    /// opaque) so colors and selection highlights are not washed out by the
    /// wallpaper bleeding through.
    /// </summary>
    public double EffectiveOpacity => Opacity ?? 1.0;

    /// <summary>
    /// Minimum WCAG contrast ratio enforced for foreground colors against the
    /// terminal background. Defaults to 7.0 (WCAG AAA).
    /// </summary>
    public double EffectiveMinContrast => MinContrast ?? 7.0;
}

/// <summary>
/// Per-theme Neovim colorscheme selection. Each theme picks a real, hand-tuned
/// nvim colorscheme rather than trying to dynamically rewrite catppuccin's
/// palette — dynamic palette overrides interact poorly with treesitter/LSP
/// highlight groups and looked off in practice.
///
/// <para><c>Colorscheme</c>: name passed to <c>:colorscheme</c>, e.g. <c>"catppuccin-mocha"</c>,
/// <c>"slate"</c>, or <c>"modus_vivendi"</c>.</para>
/// <para><c>Plugin</c>: optional lazy.nvim spec (owner/repo). Omit for nvim's
/// built-in colorschemes (slate, koehler, peachpuff, modus_operandi, etc.).</para>
/// <para><c>Name</c>: optional override for lazy.nvim's <c>name</c> field
/// (e.g. <c>"catppuccin"</c> for <c>"catppuccin/nvim"</c>).</para>
/// </summary>
public record NvimSettings(
    string Colorscheme,
    string? Plugin = null,
    string? Name = null
);
