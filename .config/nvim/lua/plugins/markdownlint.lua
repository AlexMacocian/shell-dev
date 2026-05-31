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
      add_unique(opts.ensure_installed, "prettier")
    end,
  },
  {
    "stevearc/conform.nvim",
    opts = function(_, opts)
      opts.formatters = opts.formatters or {}

      -- markdownlint can fix rules like heading spacing, but MD013 is not fixable;
      -- Prettier wraps Markdown prose before markdownlint handles its fixable rules.
      opts.formatters.prettier = vim.tbl_deep_extend("force", opts.formatters.prettier or {}, {
        append_args = function(_, ctx)
          if vim.tbl_contains({ "markdown", "markdown.mdx" }, vim.bo[ctx.buf].filetype) then
            return { "--prose-wrap", "always", "--print-width", "80" }
          end
          return {}
        end,
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
