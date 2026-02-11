-- lua/plugins/roslyn-lsp.lua
return {
  {
    "mason-org/mason.nvim",
    opts = function(_, opts)
      opts.registries = opts.registries or {}
      table.insert(opts.registries, "github:mason-org/mason-registry")
      table.insert(opts.registries, "github:Crashdummyy/mason-registry")
    end,
  },

  {
    "seblyng/roslyn.nvim",
    ft = { "cs", "razor" },
    opts = {
      filewatching = "auto",
    },
  },
}
