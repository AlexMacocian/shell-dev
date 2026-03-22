using System.Text.Json;

namespace ThemeEngine.Generators;

public class VscodeSettingsGenerator : IGenerator
{
    public string Name => "VS Code Settings";
    public string OutputPath => ".config/Code/User/settings.json";

    public string Generate(Theme theme, string wallpapersDir)
    {
        var c = theme.Colors;
        var isLight = theme.Gtk.ColorScheme.Contains("light");
        var baseTheme = isLight ? "Default Light Modern" : "Monokai";

        var settings = new Dictionary<string, object>
        {
            ["github.copilot.nextEditSuggestions.enabled"] = true,
            ["git.enableSmartCommit"] = true,
            ["chat.viewSessions.orientation"] = "stacked",
            ["workbench.colorTheme"] = baseTheme,
            ["editor.fontFamily"] = $"'{theme.Font.Family}', monospace",
            ["editor.fontSize"] = theme.Font.Size + 2,
            ["terminal.integrated.fontFamily"] = $"'{theme.Font.Family}'",
            ["terminal.integrated.fontSize"] = theme.Font.Size + 2,
            ["workbench.colorCustomizations"] = new Dictionary<string, string>
            {
                // Global defaults
                ["foreground"] = c.Text,
                ["descriptionForeground"] = c.TextDim,
                ["disabledForeground"] = c.Inactive,
                ["icon.foreground"] = c.TextDim,
                ["textLink.foreground"] = c.Blue,
                ["textLink.activeForeground"] = c.Accent1,

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
                ["tab.unfocusedActiveForeground"] = c.TextDim,
                ["tab.unfocusedInactiveForeground"] = c.Inactive,
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
                ["terminal.ansiCyan"] = c.Blue,
                ["terminal.ansiWhite"] = c.Text,
                ["terminal.ansiBrightBlack"] = isLight ? c.TextDim : c.Inactive,
                ["terminal.ansiBrightRed"] = c.Red,
                ["terminal.ansiBrightGreen"] = c.Green,
                ["terminal.ansiBrightYellow"] = c.Border,
                ["terminal.ansiBrightBlue"] = c.Blue,
                ["terminal.ansiBrightMagenta"] = c.Accent2,
                ["terminal.ansiBrightCyan"] = c.Blue,
                ["terminal.ansiBrightWhite"] = isLight ? c.Bg3 : c.Bg1,

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
                ["list.inactiveSelectionForeground"] = c.Text,
                ["list.inactiveFocusOutline"] = c.Border + "40",
                ["list.hoverBackground"] = c.Bg1,
                ["list.highlightForeground"] = c.Accent1,

                // Tree
                ["tree.indentGuidesStroke"] = c.Bg3,

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
                ["editorWidget.foreground"] = c.Text,
                ["editorWidget.border"] = c.Inactive,

                // Badges
                ["badge.background"] = c.Accent2,
                ["badge.foreground"] = c.Bg0,

                // Notifications
                ["notificationCenterHeader.foreground"] = c.Text,
                ["notificationCenterHeader.background"] = c.Bg1,
                ["notifications.foreground"] = c.Text,
                ["notifications.background"] = c.Bg1,
                ["notifications.border"] = c.Bg2,

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
                ["gitDecoration.submoduleResourceForeground"] = c.Blue,

                // Command palette / quick input
                ["quickInput.background"] = c.Bg1,
                ["quickInput.foreground"] = c.Text,
                ["quickInputTitle.background"] = c.Bg2,
                ["quickInputList.focusBackground"] = c.Border + "20",
                ["quickInputList.focusForeground"] = c.Text,

                // Keybinding labels
                ["keybindingLabel.foreground"] = c.Text,
                ["keybindingLabel.background"] = c.Bg2,
                ["keybindingLabel.border"] = c.Bg3,

                // Settings
                ["settings.headerForeground"] = c.Accent1,
                ["settings.modifiedItemIndicator"] = c.Border,
                ["settings.focusedRowBackground"] = c.Bg1,

                // Context menus
                ["menu.background"] = c.Bg1,
                ["menu.foreground"] = c.Text,
                ["menu.selectionBackground"] = c.Border + "20",
                ["menu.selectionForeground"] = c.Text,
                ["menu.separatorBackground"] = c.Bg2,
                ["menu.border"] = c.Bg2,

                // Menu bar
                ["menubar.selectionBackground"] = c.Border + "20",
                ["menubar.selectionForeground"] = c.Text,

                // CodeLens
                ["editorCodeLens.foreground"] = c.TextDim,

                // Inlay hints (references, type hints, etc.)
                ["editorInlayHint.foreground"] = c.TextDim,
                ["editorInlayHint.background"] = c.Bg2 + "80",
                ["editorInlayHint.typeForeground"] = c.Blue,
                ["editorInlayHint.parameterForeground"] = c.TextDim,

                // Bracket match
                ["editorBracketMatch.background"] = c.Border + "20",
                ["editorBracketMatch.border"] = c.Border,

                // Widget shadows
                ["widget.shadow"] = c.Bg0 + "40",
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
