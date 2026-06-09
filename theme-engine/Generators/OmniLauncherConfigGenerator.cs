using System.Text.Json;

namespace ThemeEngine.Generators;

public class OmniLauncherConfigGenerator : IGenerator
{
    public string Name => "Omni Launcher Config";
    public string OutputPath => ".config/omni-launcher/config.json";

    public string Generate(Theme theme, string wallpapersDir)
    {
        var c = theme.Colors;
        var p = PaletteResolver.Resolve(theme);
        var f = theme.Font;

        var config = new
        {
            fontFamily = f.Family,
            fontSize = Math.Max(14, f.Size + 4),
            giphyApiKey = "",
            panelWidth = 720,
            panelHeight = 520,
            padding = 5,
            spacing = 5,
            radius = Math.Max(6, theme.Hyprland.Rounding * 2),
            colors = new
            {
                background = c.Bg0,
                foreground = p.Text,
                idle = p.TextDim,
                accent = p.Accent1,
                overlayStrong = p.SelectionBg,
                overlayWeak = c.Bg2,
                border = p.Border,
            },
        };

        return JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        }) + Environment.NewLine;
    }
}
