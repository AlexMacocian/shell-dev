using System.Text.Json;

namespace ThemeEngine.Generators;

public class VscodeSettingsGenerator : IGenerator
{
    public string Name => "VS Code Settings";
    public string OutputPath => ".config/Code/User/settings.json";

    public string Generate(Theme theme, string wallpapersDir)
    {
        var c = theme.Colors;

        var settings = new Dictionary<string, object>
        {
            ["github.copilot.nextEditSuggestions.enabled"] = true,
            ["git.enableSmartCommit"] = true,
            ["chat.viewSessions.orientation"] = "stacked",
            ["workbench.colorTheme"] = "Monokai",
            ["workbench.colorCustomizations"] = new Dictionary<string, string>
            {
                // Title bar
                ["titleBar.activeBackground"] = c.Bg0,
                ["titleBar.activeForeground"] = c.Text,
                ["titleBar.inactiveBackground"] = c.Bg0,
                ["titleBar.inactiveForeground"] = c.Inactive,

                // Activity bar
                ["activityBar.background"] = c.Bg0,
                ["activityBar.foreground"] = c.Border,
                ["activityBar.inactiveForeground"] = c.Inactive,
                ["activityBarBadge.background"] = c.Accent2,
                ["activityBarBadge.foreground"] = c.Text,

                // Sidebar
                ["sideBar.background"] = c.Bg1,
                ["sideBar.foreground"] = c.Text,
                ["sideBar.border"] = c.Bg2,
                ["sideBarTitle.foreground"] = c.Border,
                ["sideBarSectionHeader.background"] = c.Bg2,
                ["sideBarSectionHeader.foreground"] = c.Accent1,

                // Editor
                ["editor.background"] = c.Bg0,
                ["editor.foreground"] = c.Text,
                ["editor.lineHighlightBackground"] = c.Bg1 + "80",
                ["editor.selectionBackground"] = c.Border + "30",
                ["editor.selectionHighlightBackground"] = c.Border + "18",
                ["editor.wordHighlightBackground"] = c.Accent2 + "25",
                ["editor.findMatchBackground"] = c.Border + "40",
                ["editor.findMatchHighlightBackground"] = c.Border + "20",

                // Line numbers
                ["editorLineNumber.foreground"] = c.Inactive,
                ["editorLineNumber.activeForeground"] = c.Border,
                ["editorGutter.background"] = c.Bg0,

                // Tabs
                ["editorGroupHeader.tabsBackground"] = c.Bg0,
                ["tab.activeBackground"] = c.Bg1,
                ["tab.activeForeground"] = c.Accent1,
                ["tab.inactiveBackground"] = c.Bg0,
                ["tab.inactiveForeground"] = c.TextDim,
                ["tab.border"] = c.Bg2,
                ["tab.activeBorderTop"] = c.Border,

                // Status bar
                ["statusBar.background"] = c.Bg0,
                ["statusBar.foreground"] = c.TextDim,
                ["statusBar.border"] = c.Bg2,
                ["statusBar.debuggingBackground"] = c.Red,
                ["statusBar.debuggingForeground"] = c.Text,
                ["statusBar.noFolderBackground"] = c.Bg1,

                // Terminal
                ["terminal.background"] = c.Bg0,
                ["terminal.foreground"] = c.Text,
                ["terminal.ansiBlack"] = c.Bg0,
                ["terminal.ansiRed"] = c.Red,
                ["terminal.ansiGreen"] = c.Green,
                ["terminal.ansiYellow"] = c.Accent1,
                ["terminal.ansiBlue"] = c.Blue,
                ["terminal.ansiMagenta"] = c.Accent2,
                ["terminal.ansiCyan"] = "#6A8A7A",
                ["terminal.ansiWhite"] = c.Text,
                ["terminal.ansiBrightBlack"] = c.Inactive,
                ["terminal.ansiBrightRed"] = "#A83000",
                ["terminal.ansiBrightGreen"] = "#6A8A57",
                ["terminal.ansiBrightYellow"] = c.Border,
                ["terminal.ansiBrightBlue"] = "#5A7A8A",
                ["terminal.ansiBrightMagenta"] = "#C8843A",
                ["terminal.ansiBrightCyan"] = "#7A9A8A",
                ["terminal.ansiBrightWhite"] = "#F0E0CC",

                // Panels
                ["panel.background"] = c.Bg0,
                ["panel.border"] = c.Bg2,
                ["panelTitle.activeForeground"] = c.Border,
                ["panelTitle.inactiveForeground"] = c.Inactive,
                ["panelTitle.activeBorder"] = c.Border,

                // Lists
                ["list.activeSelectionBackground"] = c.Border + "20",
                ["list.activeSelectionForeground"] = c.Accent1,
                ["list.inactiveSelectionBackground"] = c.Bg2,
                ["list.hoverBackground"] = c.Bg1,
                ["list.highlightForeground"] = c.Accent1,

                // Input
                ["input.background"] = c.Bg1,
                ["input.foreground"] = c.Text,
                ["input.border"] = c.Inactive,
                ["input.placeholderForeground"] = c.Inactive,
                ["focusBorder"] = c.Border,

                // Dropdown
                ["dropdown.background"] = c.Bg1,
                ["dropdown.foreground"] = c.Text,
                ["dropdown.border"] = c.Inactive,

                // Buttons
                ["button.background"] = c.Border,
                ["button.foreground"] = c.Bg0,
                ["button.hoverBackground"] = c.Accent1,

                // Scrollbar
                ["scrollbarSlider.background"] = c.Inactive + "40",
                ["scrollbarSlider.hoverBackground"] = c.Inactive + "80",
                ["scrollbarSlider.activeBackground"] = c.Border + "60",

                // Breadcrumbs
                ["breadcrumb.foreground"] = c.TextDim,
                ["breadcrumb.focusForeground"] = c.Accent1,
                ["breadcrumb.activeSelectionForeground"] = c.Border,

                // Widgets
                ["editorWidget.background"] = c.Bg1,
                ["editorWidget.border"] = c.Inactive,

                // Minimap
                ["minimap.background"] = c.Bg0,

                // Peek view
                ["peekView.border"] = c.Border,
                ["peekViewEditor.background"] = c.Bg1,
                ["peekViewResult.background"] = c.Bg0,
                ["peekViewTitle.background"] = c.Bg1,
                ["peekViewTitleLabel.foreground"] = c.Accent1,

                // Git
                ["gitDecoration.modifiedResourceForeground"] = c.Border,
                ["gitDecoration.untrackedResourceForeground"] = c.Green,
                ["gitDecoration.deletedResourceForeground"] = c.Red,
                ["gitDecoration.conflictingResourceForeground"] = c.Accent2,
                ["gitDecoration.ignoredResourceForeground"] = c.Inactive,
            }
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        return JsonSerializer.Serialize(settings, options);
    }
}
