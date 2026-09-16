return {
  "folke/snacks.nvim",
  keys = {
    {
      "<leader>ac",
      function()
        local root = LazyVim.root()
        local cmd = { "copilot" }

        if vim.fn.filereadable(root .. "/agency.toml") == 1 then
          cmd = { "agency", "copilot" }
        elseif vim.fn.filereadable(root .. "/sherlock.toml") == 1 then
          cmd = { "sherlock", "copilot" }
        end

        if vim.fn.executable(cmd[1]) ~= 1 then
          vim.notify(cmd[1] .. " is not installed or not in PATH", vim.log.levels.ERROR)
          return
        end

        Snacks.terminal.toggle(cmd, {
          cwd = root,
          win = {
            position = "right",
            width = 0.45,
            title = " GitHub Copilot ",
          },
        })
      end,
      desc = "Copilot CLI",
    },
  },
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
                --Snacks.terminal.open()
              end)
            end
          end,
        },
      },
    },
  },
}
