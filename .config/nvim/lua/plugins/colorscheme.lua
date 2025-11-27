return {
  {
    "catppuccin/nvim",
    name = "catppuccin",
    lazy = true,
    opts = {
      flavour = "mocha",
      integrations = { lsp = true, treesitter = true },
      custom_highlights = function(colors)
        return {
          ["@lsp.type.interface.cs"] = {
            fg = colors.sky,
            style = { "bold", "italic" },
          },

          ["@lsp.type.class.cs"] = {
            fg = colors.lavender,
            style = {},
          },

          ["@lsp.type.extensionMethodName.cs"] = {
            fg = colors.mauve,
            style = { "bold" },
          },

	  ["@lsp.type.variable.cs"] = {
            fg = colors.text,
            style = { "italic" },
          },

	  ["@lsp.type.staticField.cs"] = {
            fg = colors.peach,
            style = { "bold" },
          },
        }
      end,
    },
  },
  {
    "LazyVim/LazyVim",
    opts = { colorscheme = "catppuccin" },
  },
}
