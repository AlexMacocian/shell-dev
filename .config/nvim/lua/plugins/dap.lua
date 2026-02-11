return {
  {
    "jay-babu/mason-nvim-dap.nvim",
    opts = {
      ensure_installed = { "netcoredbg" },
      handlers = {
        coreclr = function() end,
      },
    },
  },
  {
    "mfussenegger/nvim-dap",
    opts = function()
      -- Prevent .vscode/launch.json from polluting the dap picker
      require("dap").providers.configs["dap.launch.json"] = nil

      vim.api.nvim_create_autocmd("FileType", {
        pattern = "razor",
        callback = function()
          local dap = require("dap")
          if dap.configurations.cs and #dap.configurations.cs > 0 then
            dap.configurations.razor = dap.configurations.cs
          end
        end,
      })
    end,
  },
}
