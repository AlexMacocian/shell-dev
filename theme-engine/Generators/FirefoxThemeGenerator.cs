namespace ThemeEngine.Generators;

public class FirefoxThemeGenerator : IGenerator
{
    public string Name => "Firefox Theme";
    public string OutputPath => ".config/firefox-theme/theme-colors.json";

    public string Generate(Theme theme, string wallpapersDir)
    {
        var c = theme.Colors;
        var p = PaletteResolver.Resolve(theme);
        var colorScheme = p.IsLight ? "light" : "dark";

        return $$"""
        {
            "colors": {
                "frame": "{{c.Bg0}}",
                "frame_inactive": "{{c.Bg0}}",
                "tab_background_text": "{{p.Text}}",
                "tab_selected": "{{c.Bg1}}",
                "tab_line": "{{p.Border}}",
                "tab_loading": "{{p.Border}}",
                "toolbar": "{{c.Bg0}}",
                "toolbar_text": "{{p.Text}}",
                "toolbar_field": "{{c.Bg1}}",
                "toolbar_field_text": "{{p.Text}}",
                "toolbar_field_border": "{{p.Inactive}}",
                "toolbar_field_border_focus": "{{p.Border}}",
                "toolbar_field_highlight": "{{p.SelectionBg}}",
                "toolbar_field_highlight_text": "{{p.SelectionFg}}",
                "toolbar_bottom_separator": "{{c.Bg2}}",
                "toolbar_top_separator": "{{c.Bg2}}",
                "toolbar_vertical_separator": "{{c.Bg2}}",
                "popup": "{{c.Bg1}}",
                "popup_text": "{{p.Text}}",
                "popup_border": "{{c.Bg2}}",
                "popup_highlight": "{{p.SelectionBg}}",
                "popup_highlight_text": "{{p.SelectionFg}}",
                "sidebar": "{{c.Bg0}}",
                "sidebar_text": "{{p.Text}}",
                "sidebar_border": "{{c.Bg2}}",
                "sidebar_highlight": "{{p.SelectionBg}}",
                "sidebar_highlight_text": "{{p.SelectionFg}}",
                "ntp_background": "{{c.Bg0}}",
                "ntp_card_background": "{{c.Bg1}}",
                "ntp_text": "{{p.Text}}",
                "button_background_hover": "{{c.Bg1}}",
                "button_background_active": "{{c.Bg2}}",
                "icons": "{{p.TextDim}}",
                "icons_attention": "{{p.Border}}"
            },
            "properties": {
                "color_scheme": "{{colorScheme}}",
                "content_color_scheme": "{{colorScheme}}"
            }
        }
        """;
    }
}
