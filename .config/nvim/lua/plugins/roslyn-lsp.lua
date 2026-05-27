-- lua/plugins/roslyn-lsp.lua
return {
  {
    "mason-org/mason.nvim",
    opts = function(_, opts)
      opts.registries = opts.registries or {}
      table.insert(opts.registries, "github:mason-org/mason-registry")
      table.insert(opts.registries, "github:Crashdummyy/mason-registry")
      opts.ensure_installed = opts.ensure_installed or {}
      table.insert(opts.ensure_installed, "html-lsp")
      table.insert(opts.ensure_installed, "roslyn")
    end,
  },

  {
    "seblyng/roslyn.nvim",
    ft = { "cs", "razor" },
    opts = {
      filewatching = "auto",
    },
    init = function()
      vim.filetype.add({
        extension = {
          razor = "razor",
          cshtml = "razor",
        },
      })
    end,
    config = function(_, opts)
      require("roslyn").setup(opts)
      -- Mason installs the shim as `roslyn`, but roslyn.nvim searches for
      -- `roslyn-language-server`. Pin the cmd explicitly so discovery is
      -- not needed. See checkpoint 013.
      vim.lsp.config("roslyn", {
        cmd = {
          vim.fn.stdpath("data") .. "/mason/bin/roslyn",
          "--logLevel=Information",
          "--extensionLogDirectory=" .. vim.fs.dirname(vim.lsp.get_log_path()),
          "--stdio",
        },
      })
    end,
  },
}