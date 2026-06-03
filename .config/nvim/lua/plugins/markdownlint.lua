local markdownlint_config = vim.fn.stdpath("config") .. "/linter-configs/.markdownlint.json"

local function add_unique(list, value)
  if not vim.tbl_contains(list, value) then
    table.insert(list, value)
  end
end

return {
  {
    "mason-org/mason.nvim",
    opts = function(_, opts)
      opts.ensure_installed = opts.ensure_installed or {}
      add_unique(opts.ensure_installed, "mdformat")
    end,
  },
  {
    "stevearc/conform.nvim",
    opts = function(_, opts)
      opts.formatters = opts.formatters or {}
      opts.formatters_by_ft = opts.formatters_by_ft or {}

      opts.formatters_by_ft.markdown = { "mdformat", "markdownlint-cli2", "markdown-toc" }

      -- markdownlint can report MD013, but cannot fix line length.
      -- mdformat wraps prose while leaving link-only TOCs, tables, and code blocks stable.
      opts.formatters.mdformat = vim.tbl_deep_extend("force", opts.formatters.mdformat or {}, {
        args = { "--wrap", "80", "--number", "--extensions", "gfm", "-" },
      })

      opts.formatters["markdownlint-cli2"] =
        vim.tbl_deep_extend("force", opts.formatters["markdownlint-cli2"] or {}, {
          args = {
            "--config",
            markdownlint_config,
            "--fix",
            "$FILENAME",
          },
        })
    end,
  },
  {
    "mfussenegger/nvim-lint",
    opts = {
      linters = {
        ["markdownlint-cli2"] = {
          args = {
            "--config",
            markdownlint_config,
            "-",
          },
        },
      },
    },
  },
}
