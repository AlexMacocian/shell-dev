-- Override markdownlint-cli2 to use our bundled config
-- Disables line-length warnings for tables (MD013)

return {
  "mfussenegger/nvim-lint",
  opts = {
    linters = {
      ["markdownlint-cli2"] = {
        args = {
          "--config",
          vim.fn.stdpath("config") .. "/linter-configs/.markdownlint.json",
          "-",
        },
      },
    },
  },
}
