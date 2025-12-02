-- lua/plugins/roslyn-lsp.lua
return {
  -- Add the custom mason registry
  {
    "williamboman/mason.nvim",
    opts = function(_, opts)
      opts.registries = opts.registries or {}
      table.insert(opts.registries, "github:mason-org/mason-registry")
      table.insert(opts.registries, "github:Crashdummyy/mason-registry")
    end,
  },

  -- Setup roslyn.nvim
  {
    "seblyng/roslyn.nvim",
    ft = "cs",
    opts = {
      exe = {
        "dotnet",
        vim.fs.joinpath(
          vim.fn.stdpath("data"),
          "mason",
          "packages",
          "roslyn",
          "Microsoft.CodeAnalysis.LanguageServer.dll"
        ),
      },
      filewatching = true,
      config = {
        settings = {
          ["csharp|background_analysis"] = {
            dotnet_analyzer_diagnostics_scope = "fullSolution",
            dotnet_compiler_diagnostics_scope = "fullSolution",
          },
          ["csharp|inlay_hints"] = {
            csharp_enable_inlay_hints_for_implicit_object_creation = true,
            csharp_enable_inlay_hints_for_implicit_variable_types = true,
            csharp_enable_inlay_hints_for_lambda_parameter_types = true,
            csharp_enable_inlay_hints_for_types = true,
            dotnet_enable_inlay_hints_for_indexer_parameters = true,
          },
          ["csharp|code_lens"] = {
            dotnet_enable_references_code_lens = true,
          },
        },
      },
    },
  },
}
