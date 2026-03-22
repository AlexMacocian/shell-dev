using System.Text.Json.Serialization;

namespace ThemeEngine;

public record Theme(
    string Name,
    ThemeColors Colors,
    HyprlandSettings Hyprland,
    FontSettings Font,
    GtkSettings Gtk,
    WaybarSettings Waybar,
    WallpaperSettings Wallpapers
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
    string Inactive
);

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
    string[] Fallback
);

public record WaybarSettings(
    int Height,
    [property: JsonPropertyName("font_size")] int FontSize,
    string Separator,
    double Opacity,
    [property: JsonPropertyName("border_width")] int BorderWidth,
    [property: JsonPropertyName("workspace_labels")] string[] WorkspaceLabels
);

public record WallpaperSettings(
    [property: JsonPropertyName("fit_mode")] string FitMode,
    string[] Images,
    string[] Videos
);

public record GtkSettings(
    [property: JsonPropertyName("color_scheme")] string ColorScheme,
    string Theme
);
