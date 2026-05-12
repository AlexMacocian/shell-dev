using System.Text.Json;

namespace ThemeEngine.Generators;

public class VscodeSettingsGenerator : IGenerator
{
    public string Name => "VS Code Settings";
    public string OutputPath => ".config/Code/User/settings.json";

    public string Generate(Theme theme, string wallpapersDir)
    {
        var c = theme.Colors;
        var p = PaletteResolver.Resolve(theme);
        var isLight = p.IsLight;
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
                ["foreground"] = p.Text,
                ["descriptionForeground"] = p.TextDim,
                ["disabledForeground"] = p.Inactive,
                ["icon.foreground"] = p.TextDim,
                ["textLink.foreground"] = p.Blue,
                ["textLink.activeForeground"] = p.Accent1,

                // Title bar
                ["titleBar.activeBackground"] = c.Bg0,
                ["titleBar.activeForeground"] = p.Text,
                ["titleBar.inactiveBackground"] = c.Bg0,
                ["titleBar.inactiveForeground"] = p.Inactive,

                // Activity bar
                ["activityBar.background"] = c.Bg0,
                ["activityBar.foreground"] = p.Border,
                ["activityBar.inactiveForeground"] = p.Inactive,
                ["activityBarBadge.background"] = p.Accent2,
                ["activityBarBadge.foreground"] = p.Text,

                // Sidebar
                ["sideBar.background"] = c.Bg1,
                ["sideBar.foreground"] = p.Text,
                ["sideBar.border"] = c.Bg2,
                ["sideBarTitle.foreground"] = p.Border,
                ["sideBarSectionHeader.background"] = c.Bg2,
                ["sideBarSectionHeader.foreground"] = p.Accent1,

                // Editor — selection uses the resolved high-contrast pair so
                // it stays visible regardless of palette desaturation.
                ["editor.background"] = c.Bg0,
                ["editor.foreground"] = p.Text,
                ["editor.lineHighlightBackground"] = c.Bg1 + "80",
                ["editor.selectionBackground"] = p.SelectionBg + "80",
                ["editor.selectionHighlightBackground"] = p.SelectionBg + "40",
                ["editor.wordHighlightBackground"] = p.Accent2 + "40",
                ["editor.findMatchBackground"] = p.SelectionBg + "60",
                ["editor.findMatchHighlightBackground"] = p.SelectionBg + "30",

                // Line numbers
                ["editorLineNumber.foreground"] = p.Inactive,
                ["editorLineNumber.activeForeground"] = p.Border,
                ["editorGutter.background"] = c.Bg0,

                // Tabs
                ["editorGroupHeader.tabsBackground"] = c.Bg0,
                ["tab.activeBackground"] = c.Bg1,
                ["tab.activeForeground"] = p.Accent1,
                ["tab.inactiveBackground"] = c.Bg0,
                ["tab.inactiveForeground"] = p.TextDim,
                ["tab.unfocusedActiveForeground"] = p.TextDim,
                ["tab.unfocusedInactiveForeground"] = p.Inactive,
                ["tab.border"] = c.Bg2,
                ["tab.activeBorderTop"] = p.Border,

                // Status bar
                ["statusBar.background"] = c.Bg0,
                ["statusBar.foreground"] = p.TextDim,
                ["statusBar.border"] = c.Bg2,
                ["statusBar.debuggingBackground"] = p.Red,
                ["statusBar.debuggingForeground"] = p.Text,
                ["statusBar.noFolderBackground"] = c.Bg1,

                // Terminal — same contrast-resolved palette as Kitty so the
                // VS Code integrated terminal looks consistent with the
                // standalone one.
                ["terminal.background"] = c.Bg0,
                ["terminal.foreground"] = p.Text,
                ["terminal.selectionBackground"] = p.SelectionBg,
                ["terminal.ansiBlack"] = p.AnsiBlack,
                ["terminal.ansiRed"] = p.AnsiRed,
                ["terminal.ansiGreen"] = p.AnsiGreen,
                ["terminal.ansiYellow"] = p.AnsiYellow,
                ["terminal.ansiBlue"] = p.AnsiBlue,
                ["terminal.ansiMagenta"] = p.AnsiMagenta,
                ["terminal.ansiCyan"] = p.AnsiCyan,
                ["terminal.ansiWhite"] = p.AnsiWhite,
                ["terminal.ansiBrightBlack"] = p.BrightBlack,
                ["terminal.ansiBrightRed"] = p.BrightRed,
                ["terminal.ansiBrightGreen"] = p.BrightGreen,
                ["terminal.ansiBrightYellow"] = p.BrightYellow,
                ["terminal.ansiBrightBlue"] = p.BrightBlue,
                ["terminal.ansiBrightMagenta"] = p.BrightMagenta,
                ["terminal.ansiBrightCyan"] = p.BrightCyan,
                ["terminal.ansiBrightWhite"] = p.BrightWhite,

                // Panels
                ["panel.background"] = c.Bg0,
                ["panel.border"] = c.Bg2,
                ["panelTitle.activeForeground"] = p.Border,
                ["panelTitle.inactiveForeground"] = p.Inactive,
                ["panelTitle.activeBorder"] = p.Border,

                // Lists
                ["list.activeSelectionBackground"] = p.SelectionBg + "60",
                ["list.activeSelectionForeground"] = p.SelectionFg,
                ["list.inactiveSelectionBackground"] = c.Bg2,
                ["list.inactiveSelectionForeground"] = p.Text,
                ["list.inactiveFocusOutline"] = p.Border + "40",
                ["list.hoverBackground"] = c.Bg1,
                ["list.highlightForeground"] = p.Accent1,

                // Tree
                ["tree.indentGuidesStroke"] = c.Bg3,

                // Input
                ["input.background"] = c.Bg1,
                ["input.foreground"] = p.Text,
                ["input.border"] = p.Inactive,
                ["input.placeholderForeground"] = p.Inactive,
                ["focusBorder"] = p.Border,

                // Dropdown
                ["dropdown.background"] = c.Bg1,
                ["dropdown.foreground"] = p.Text,
                ["dropdown.border"] = p.Inactive,

                // Buttons
                ["button.background"] = p.Border,
                ["button.foreground"] = c.Bg0,
                ["button.hoverBackground"] = p.Accent1,

                // Scrollbar
                ["scrollbarSlider.background"] = p.Inactive + "40",
                ["scrollbarSlider.hoverBackground"] = p.Inactive + "80",
                ["scrollbarSlider.activeBackground"] = p.Border + "60",

                // Breadcrumbs
                ["breadcrumb.foreground"] = p.TextDim,
                ["breadcrumb.focusForeground"] = p.Accent1,
                ["breadcrumb.activeSelectionForeground"] = p.Border,

                // Widgets
                ["editorWidget.background"] = c.Bg1,
                ["editorWidget.foreground"] = p.Text,
                ["editorWidget.border"] = p.Inactive,

                // Badges
                ["badge.background"] = p.Accent2,
                ["badge.foreground"] = c.Bg0,

                // Notifications
                ["notificationCenterHeader.foreground"] = p.Text,
                ["notificationCenterHeader.background"] = c.Bg1,
                ["notifications.foreground"] = p.Text,
                ["notifications.background"] = c.Bg1,
                ["notifications.border"] = c.Bg2,

                // Minimap
                ["minimap.background"] = c.Bg0,

                // Peek view
                ["peekView.border"] = p.Border,
                ["peekViewEditor.background"] = c.Bg1,
                ["peekViewResult.background"] = c.Bg0,
                ["peekViewTitle.background"] = c.Bg1,
                ["peekViewTitleLabel.foreground"] = p.Accent1,

                // Git
                ["gitDecoration.modifiedResourceForeground"] = p.Border,
                ["gitDecoration.untrackedResourceForeground"] = p.Green,
                ["gitDecoration.deletedResourceForeground"] = p.Red,
                ["gitDecoration.conflictingResourceForeground"] = p.Accent2,
                ["gitDecoration.ignoredResourceForeground"] = p.Inactive,
                ["gitDecoration.submoduleResourceForeground"] = p.Blue,

                // Command palette / quick input
                ["quickInput.background"] = c.Bg1,
                ["quickInput.foreground"] = p.Text,
                ["quickInputTitle.background"] = c.Bg2,
                ["quickInputList.focusBackground"] = p.SelectionBg + "60",
                ["quickInputList.focusForeground"] = p.SelectionFg,

                // Keybinding labels
                ["keybindingLabel.foreground"] = p.Text,
                ["keybindingLabel.background"] = c.Bg2,
                ["keybindingLabel.border"] = c.Bg3,

                // Settings
                ["settings.headerForeground"] = p.Accent1,
                ["settings.modifiedItemIndicator"] = p.Border,
                ["settings.focusedRowBackground"] = c.Bg1,

                // Context menus
                ["menu.background"] = c.Bg1,
                ["menu.foreground"] = p.Text,
                ["menu.selectionBackground"] = p.SelectionBg + "60",
                ["menu.selectionForeground"] = p.SelectionFg,
                ["menu.separatorBackground"] = c.Bg2,
                ["menu.border"] = c.Bg2,

                // Menu bar
                ["menubar.selectionBackground"] = p.SelectionBg + "60",
                ["menubar.selectionForeground"] = p.SelectionFg,

                // CodeLens
                ["editorCodeLens.foreground"] = p.TextDim,

                // Inlay hints (references, type hints, etc.)
                ["editorInlayHint.foreground"] = p.TextDim,
                ["editorInlayHint.background"] = c.Bg2 + "80",
                ["editorInlayHint.typeForeground"] = p.Blue,
                ["editorInlayHint.parameterForeground"] = p.TextDim,

                // Bracket match
                ["editorBracketMatch.background"] = p.Border + "20",
                ["editorBracketMatch.border"] = p.Border,

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
