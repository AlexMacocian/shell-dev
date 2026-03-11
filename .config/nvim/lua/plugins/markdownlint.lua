-- Override markdownlint config to use our bundled config file
-- This disables line-length warnings for tables (MD013)

return {
  "mfussenegger/nvim-lint",
  opts = function(_, opts)
    local config_path = vim.fn.stdpath("config") .. "/linter-configs/.markdownlint.json"

    opts.linters = opts.linters or {}
    opts.linters.markdownlint = {
      args = { "--config", config_path, "--" },
    }
  end,
}
