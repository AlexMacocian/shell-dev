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
      local dap = require("dap")

      -- Prevent .vscode/launch.json from polluting the dap picker
      dap.providers.configs["dap.launch.json"] = nil

      -- Ensure dotnet is in PATH for netcoredbg
      vim.env.PATH = vim.env.PATH .. ":/usr/bin"

      vim.api.nvim_create_autocmd("FileType", {
        pattern = "razor",
        callback = function()
          if dap.configurations.cs and #dap.configurations.cs > 0 then
            dap.configurations.razor = dap.configurations.cs
          end
        end,
      })
    end,
  },
}
