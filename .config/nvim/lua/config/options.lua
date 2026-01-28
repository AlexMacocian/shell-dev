-- Options are automatically loaded before lazy.nvim startup
-- Default options that are always set: https://github.com/LazyVim/LazyVim/blob/main/lua/lazyvim/config/options.lua
-- Add any additional options here

if vim.fn.has("win32") == 1 then
  -- Prefer PowerShell 7 if available, otherwise fall back to Windows PowerShell
  local pwsh = vim.fn.executable("pwsh") == 1 and "pwsh"
    or (vim.fn.executable("powershell") == 1 and "powershell" or nil)

  if pwsh then
    vim.opt.shell = pwsh
    vim.opt.shellcmdflag = "-NoLogo -NoProfile -ExecutionPolicy RemoteSigned -Command "
      .. "[Console]::InputEncoding=[Console]::OutputEncoding=[System.Text.UTF8Encoding]::new();"
      .. "$PSDefaultParameterValues['Out-File:Encoding']='utf8';"

    -- Recommended for PowerShell shell integration
    vim.opt.shellredir = "2>&1 | Out-File -Encoding utf8 %s; exit $LastExitCode"
    vim.opt.shellpipe = "2>&1 | Out-File -Encoding utf8 %s; exit $LastExitCode"
    vim.opt.shellquote = ""
    vim.opt.shellxquote = ""
  end
end
