-- Override LazyVim's Go extra: guard against nil textDocument capabilities.
-- Fixes: lazyvim/plugins/extras/lang/go.lua:60
--   "attempt to index field 'textDocument' (a nil value)"
return {
  {
    "neovim/nvim-lspconfig",
    opts = {
      setup = {
        gopls = function(_, _)
          Snacks.util.lsp.on({ name = "gopls" }, function(_, client)
            if client.server_capabilities.semanticTokensProvider then
              return
            end
            local caps = client.config and client.config.capabilities
            local semantic = caps and caps.textDocument and caps.textDocument.semanticTokens
            if not semantic then
              return
            end
            client.server_capabilities.semanticTokensProvider = {
              full = true,
              legend = {
                tokenTypes = semantic.tokenTypes,
                tokenModifiers = semantic.tokenModifiers,
              },
              range = true,
            }
          end)
          return false
        end,
      },
    },
  },
}
