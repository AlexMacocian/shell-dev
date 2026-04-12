namespace ThemeEngine.Generators;

public class NvimColorschemeGenerator : IGenerator
{
    public string Name => "Neovim Colorscheme";
    public string OutputPath => ".config/nvim/lua/plugins/colorscheme.lua";

    public string Generate(Theme theme, string wallpapersDir)
    {
        var c = theme.Colors;

        var sat = c.SaturationBoost;
        const double minContrast = 4.5; // WCAG AA for normal text

        // --- Derive distinct syntax colors from the 13-color palette ---
        // Hue separations are deliberately wide so each category is visually distinct.

        // Keywords (mauve): rotate blue ~90° towards purple
        var syntaxKeyword = ColorHelper.AdjustSaturation(ColorHelper.ShiftHue(c.Blue, 90), sat + 0.05);

        // Types/classes (teal): blend blue towards green with stronger green pull
        var syntaxType = ColorHelper.AdjustSaturation(ColorHelper.MixColors(c.Blue, c.Green, 0.60), sat + 0.03);

        // Methods/functions (yellow): rotate blue ~150° towards warm gold
        var syntaxMethod = ColorHelper.AdjustSaturation(ColorHelper.ShiftHue(c.Blue, -150), sat + 0.08);

        // Interfaces/operators (sky): shift blue towards cyan, further from teal
        var syntaxCyan = ColorHelper.AdjustSaturation(ColorHelper.ShiftHue(c.Blue, -30), sat + 0.03);

        // Constructors (sapphire): hue-shifted blue variant, further from sky
        var syntaxSapphire = ColorHelper.AdjustSaturation(ColorHelper.ShiftHue(c.Blue, 40), sat + 0.02);

        // Properties (lavender): blend accent1 towards blue
        var syntaxProperty = ColorHelper.AdjustSaturation(ColorHelper.MixColors(c.Accent1, c.Blue, 0.35), sat);

        // Enum members/tags (flamingo): warm red-accent mix
        var syntaxFlamingo = ColorHelper.AdjustSaturation(ColorHelper.MixColors(c.Red, c.Accent1, 0.40), sat + 0.03);

        // Special/macros (pink): rotate blue far towards magenta
        var syntaxPink = ColorHelper.AdjustSaturation(ColorHelper.ShiftHue(c.Blue, 120), sat + 0.04);

        // Punctuation (rosewater): slightly warm text
        var syntaxRosewater = ColorHelper.MixColors(c.Text, c.Accent1, 0.15);

        // Parameters (maroon): text tinted towards red
        var syntaxParam = ColorHelper.MixColors(c.Red, c.Text, 0.40);

        // --- Ensure every syntax color meets WCAG AA contrast (4.5:1) against bg ---
        syntaxKeyword = ColorHelper.EnsureContrast(syntaxKeyword, c.Bg0, minContrast);
        syntaxType = ColorHelper.EnsureContrast(syntaxType, c.Bg0, minContrast);
        syntaxMethod = ColorHelper.EnsureContrast(syntaxMethod, c.Bg0, minContrast);
        syntaxCyan = ColorHelper.EnsureContrast(syntaxCyan, c.Bg0, minContrast);
        syntaxSapphire = ColorHelper.EnsureContrast(syntaxSapphire, c.Bg0, minContrast);
        syntaxProperty = ColorHelper.EnsureContrast(syntaxProperty, c.Bg0, minContrast);
        syntaxFlamingo = ColorHelper.EnsureContrast(syntaxFlamingo, c.Bg0, minContrast);
        syntaxPink = ColorHelper.EnsureContrast(syntaxPink, c.Bg0, minContrast);
        syntaxRosewater = ColorHelper.EnsureContrast(syntaxRosewater, c.Bg0, minContrast);
        syntaxParam = ColorHelper.EnsureContrast(syntaxParam, c.Bg0, minContrast);

        // --- Ensure colors close in hue are spread apart in lightness ---
        var distinctColors = ColorHelper.EnsureDistinct(
            [syntaxKeyword, syntaxType, syntaxMethod, syntaxCyan, syntaxSapphire,
             syntaxProperty, syntaxFlamingo, syntaxPink, syntaxRosewater, syntaxParam],
            c.Bg0);
        syntaxKeyword = distinctColors[0];
        syntaxType = distinctColors[1];
        syntaxMethod = distinctColors[2];
        syntaxCyan = distinctColors[3];
        syntaxSapphire = distinctColors[4];
        syntaxProperty = distinctColors[5];
        syntaxFlamingo = distinctColors[6];
        syntaxPink = distinctColors[7];
        syntaxRosewater = distinctColors[8];
        syntaxParam = distinctColors[9];

        // Re-verify contrast after distinctness nudging
        syntaxKeyword = ColorHelper.EnsureContrast(syntaxKeyword, c.Bg0, minContrast);
        syntaxType = ColorHelper.EnsureContrast(syntaxType, c.Bg0, minContrast);
        syntaxMethod = ColorHelper.EnsureContrast(syntaxMethod, c.Bg0, minContrast);
        syntaxCyan = ColorHelper.EnsureContrast(syntaxCyan, c.Bg0, minContrast);
        syntaxSapphire = ColorHelper.EnsureContrast(syntaxSapphire, c.Bg0, minContrast);
        syntaxProperty = ColorHelper.EnsureContrast(syntaxProperty, c.Bg0, minContrast);
        syntaxFlamingo = ColorHelper.EnsureContrast(syntaxFlamingo, c.Bg0, minContrast);
        syntaxPink = ColorHelper.EnsureContrast(syntaxPink, c.Bg0, minContrast);
        syntaxRosewater = ColorHelper.EnsureContrast(syntaxRosewater, c.Bg0, minContrast);
        syntaxParam = ColorHelper.EnsureContrast(syntaxParam, c.Bg0, minContrast);

        // Direct palette colors also need guaranteed contrast
        var slotBlue = ColorHelper.EnsureContrast(c.Blue, c.Bg0, minContrast);
        var slotGreen = ColorHelper.EnsureContrast(c.Green, c.Bg0, minContrast);
        var slotRed = ColorHelper.EnsureContrast(c.Red, c.Bg0, minContrast);
        var slotAccent2 = ColorHelper.EnsureContrast(c.Accent2, c.Bg0, minContrast);

        // Overlay/subtext shades (prevent duplicate dim tones)
        var overlay1 = ColorHelper.MixColors(c.TextDim, c.Text, 0.2);
        var subtext1 = ColorHelper.MixColors(c.TextDim, c.Text, 0.4);

        return $$"""
            -- Auto-generated by ThemeEngine — do not edit manually
            -- Theme: {{theme.Name}}
            return {
              {
                "catppuccin/nvim",
                name = "catppuccin",
                lazy = true,
                opts = {
                  flavour = "mocha",
                  integrations = { lsp = true, treesitter = true },
                  color_overrides = {
                    mocha = {
                      -- Chrome
                      base = "{{c.Bg0}}",
                      mantle = "{{c.Bg1}}",
                      crust = "{{c.Bg0}}",
                      surface0 = "{{c.Bg2}}",
                      surface1 = "{{c.Bg3}}",
                      surface2 = "{{c.Inactive}}",
                      overlay0 = "{{c.TextDim}}",
                      overlay1 = "{{overlay1}}",
                      overlay2 = "{{c.Border}}",
                      text = "{{c.Text}}",
                      subtext0 = "{{c.TextDim}}",
                      subtext1 = "{{subtext1}}",

                      -- Syntax (each slot unique)
                      lavender = "{{syntaxProperty}}",
                      blue = "{{slotBlue}}",
                      sapphire = "{{syntaxSapphire}}",
                      sky = "{{syntaxCyan}}",
                      teal = "{{syntaxType}}",
                      green = "{{slotGreen}}",
                      yellow = "{{syntaxMethod}}",
                      peach = "{{slotAccent2}}",
                      maroon = "{{syntaxParam}}",
                      red = "{{slotRed}}",
                      mauve = "{{syntaxKeyword}}",
                      pink = "{{syntaxPink}}",
                      flamingo = "{{syntaxFlamingo}}",
                      rosewater = "{{syntaxRosewater}}",
                    },
                  },
                  custom_highlights = function(colors)
                    return {
                      -- C# LSP semantic tokens
                      ["@lsp.type.keyword.cs"] = { fg = colors.mauve },
                      ["@lsp.type.class.cs"] = { fg = colors.teal },
                      ["@lsp.type.struct.cs"] = { fg = colors.teal, bold = true },
                      ["@lsp.type.interface.cs"] = { fg = colors.sky, bold = true, italic = true },
                      ["@lsp.type.enum.cs"] = { fg = colors.sapphire },
                      ["@lsp.type.enumMember.cs"] = { fg = colors.flamingo },
                      ["@lsp.type.typeParameter.cs"] = { fg = colors.teal, italic = true },
                      ["@lsp.type.namespace.cs"] = { fg = colors.subtext0, italic = true },
                      ["@lsp.type.method.cs"] = { fg = colors.yellow },
                      ["@lsp.type.extensionMethodName.cs"] = { fg = colors.yellow, bold = true },
                      ["@lsp.type.property.cs"] = { fg = colors.lavender },
                      ["@lsp.type.field.cs"] = { fg = colors.lavender, italic = true },
                      ["@lsp.type.staticField.cs"] = { fg = colors.peach, bold = true },
                      ["@lsp.type.parameter.cs"] = { fg = colors.maroon, italic = true },
                      ["@lsp.type.variable.cs"] = { fg = colors.text },
                      ["@lsp.type.local.cs"] = { fg = colors.text },
                      ["@lsp.type.delegate.cs"] = { fg = colors.flamingo, italic = true },
                      ["@lsp.type.event.cs"] = { fg = colors.flamingo, bold = true },
                      ["@lsp.type.string.cs"] = { fg = colors.green },
                      ["@lsp.type.number.cs"] = { fg = colors.peach },
                      ["@lsp.type.operator.cs"] = { fg = colors.sky },

                      -- General treesitter (benefits all languages)
                      ["@keyword"] = { fg = colors.mauve },
                      ["@type"] = { fg = colors.teal },
                      ["@type.builtin"] = { fg = colors.teal, bold = true },
                      ["@function"] = { fg = colors.yellow },
                      ["@function.method"] = { fg = colors.yellow },
                      ["@function.builtin"] = { fg = colors.yellow, italic = true },
                      ["@constructor"] = { fg = colors.sapphire },
                      ["@string"] = { fg = colors.green },
                      ["@number"] = { fg = colors.peach },
                      ["@variable"] = { fg = colors.text },
                      ["@variable.parameter"] = { fg = colors.maroon, italic = true },
                      ["@property"] = { fg = colors.lavender },
                      ["@operator"] = { fg = colors.sky },
                      ["@punctuation"] = { fg = colors.rosewater },
                      ["@comment"] = { fg = colors.overlay0, italic = true },
                      ["@constant"] = { fg = colors.peach },
                      ["@constant.builtin"] = { fg = colors.peach, bold = true },
                      ["@tag"] = { fg = colors.flamingo },
                      ["@tag.attribute"] = { fg = colors.yellow },
                      ["@namespace"] = { fg = colors.subtext0, italic = true },
                    }
                  end,
                },
              },
              {
                "LazyVim/LazyVim",
                opts = { colorscheme = "catppuccin" },
              },
            }
            """;
    }
}
