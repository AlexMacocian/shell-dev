return {
  "folke/snacks.nvim",
  opts = {
    picker = {
      sources = {
        projects = {
          confirm = function(picker, item)
            if item then
              picker:close()
              vim.cmd("tcd " .. vim.fn.fnameescape(item.file))
              vim.schedule(function()
                Snacks.explorer.open()
                Snacks.terminal.open()
                --vim.cmd("CopilotChatToggle")
              end)
            end
          end,
        },
      },
    },
  },
}
