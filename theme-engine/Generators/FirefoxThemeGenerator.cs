namespace ThemeEngine.Generators;

public class FirefoxThemeGenerator : IGenerator
{
    public string Name => "Firefox Theme";
    public string OutputPath => ".config/firefox-theme/theme-colors.json";

    public string Generate(Theme theme, string wallpapersDir)
    {
        var c = theme.Colors;
        var colorScheme = theme.Gtk.ColorScheme.Contains("light") ? "light" : "dark";

        return $$"""
        {
            "colors": {
                "frame": "{{c.Bg0}}",
                "frame_inactive": "{{c.Bg0}}",
                "tab_background_text": "{{c.Text}}",
                "tab_selected": "{{c.Bg1}}",
                "tab_line": "{{c.Border}}",
                "tab_loading": "{{c.Border}}",
                "toolbar": "{{c.Bg0}}",
                "toolbar_text": "{{c.Text}}",
                "toolbar_field": "{{c.Bg1}}",
                "toolbar_field_text": "{{c.Text}}",
                "toolbar_field_border": "{{c.Inactive}}",
                "toolbar_field_border_focus": "{{c.Border}}",
                "toolbar_field_highlight": "{{c.Border}}",
                "toolbar_field_highlight_text": "{{c.Bg0}}",
                "toolbar_bottom_separator": "{{c.Bg2}}",
                "toolbar_top_separator": "{{c.Bg2}}",
                "toolbar_vertical_separator": "{{c.Bg2}}",
                "popup": "{{c.Bg1}}",
                "popup_text": "{{c.Text}}",
                "popup_border": "{{c.Bg2}}",
                "popup_highlight": "{{c.Bg2}}",
                "popup_highlight_text": "{{c.Text}}",
                "sidebar": "{{c.Bg0}}",
                "sidebar_text": "{{c.Text}}",
                "sidebar_border": "{{c.Bg2}}",
                "sidebar_highlight": "{{c.Bg2}}",
                "sidebar_highlight_text": "{{c.Text}}",
                "ntp_background": "{{c.Bg0}}",
                "ntp_card_background": "{{c.Bg1}}",
                "ntp_text": "{{c.Text}}",
                "button_background_hover": "{{c.Bg1}}",
                "button_background_active": "{{c.Bg2}}",
                "icons": "{{c.TextDim}}",
                "icons_attention": "{{c.Border}}"
            },
            "properties": {
                "color_scheme": "{{colorScheme}}",
                "content_color_scheme": "{{colorScheme}}"
            }
        }
        """;
    }
}
