return {
  -- Disable OmniSharp since we use Roslyn
  {
    "neovim/nvim-lspconfig",
    opts = {
      servers = {
        omnisharp = {
          enabled = false,
        },
      },
    },
    init = function()
      -- Override the default textDocument/definition handler to deduplicate results
      local original_def_handler = vim.lsp.handlers["textDocument/definition"]
      vim.lsp.handlers["textDocument/definition"] = function(err, result, ctx, config)
        if result == nil or vim.tbl_isempty(result) then
          return original_def_handler(err, result, ctx, config)
        end

        -- Normalize to array
        local results = vim.islist(result) and result or { result }

        -- Deduplicate based on URI and range
        local seen = {}
        local unique = {}
        for _, item in ipairs(results) do
          local uri = item.uri or item.targetUri
          local range = item.range or item.targetSelectionRange or {}
          local start = range.start or {}
          local key = (uri or "") .. ":" .. (start.line or 0) .. ":" .. (start.character or 0)
          if not seen[key] then
            seen[key] = true
            table.insert(unique, item)
          end
        end

        -- If only one unique result, pass single item (auto-jumps)
        if #unique == 1 then
          return original_def_handler(err, unique[1], ctx, config)
        end

        return original_def_handler(err, unique, ctx, config)
      end
    end,
  },

  -- Disable inlay hints for C# on Linux to avoid Roslyn out-of-range errors
  {
    "seblyng/roslyn.nvim",
    optional = true,
    cond = vim.fn.has("unix") == 1 and vim.fn.has("mac") == 0, -- Only on Linux
    opts = {
      config = {
        settings = {
          ["csharp|inlay_hints"] = {
            csharp_enable_inlay_hints_for_implicit_object_creation = false,
            csharp_enable_inlay_hints_for_implicit_variable_types = false,
            csharp_enable_inlay_hints_for_lambda_parameter_types = false,
            csharp_enable_inlay_hints_for_types = false,
            dotnet_enable_inlay_hints_for_indexer_parameters = false,
            dotnet_enable_inlay_hints_for_literal_parameters = false,
            dotnet_enable_inlay_hints_for_object_creation_parameters = false,
            dotnet_enable_inlay_hints_for_other_parameters = false,
            dotnet_enable_inlay_hints_for_parameters = false,
          },
        },
      },
    },
  },
}
