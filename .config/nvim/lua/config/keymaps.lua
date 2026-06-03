-- Keymaps are automatically loaded on the VeryLazy event
-- Default keymaps that are always set: https://github.com/LazyVim/LazyVim/blob/main/lua/lazyvim/config/keymaps.lua
-- Add any additional keymaps here

-- Make Ctrl-\ Ctrl-n behave like <Esc> everywhere
vim.keymap.set("t", "<C-\\><C-n>", "<C-\\><C-n>", { desc = "Terminal Normal Mode" })
vim.keymap.set({ "i", "n", "v" }, "<C-\\><C-n>", "<Esc>", { noremap = true, silent = true })

-- map <leader>cx to copy current file path
vim.keymap.set("n", "<leader>af", function()
  local file_path = "#file:" .. vim.fn.expand("%:p")
  vim.fn.setreg("+", file_path)
  vim.notify("Copied " .. file_path, vim.log.levels.INFO)
end, { desc = "Copy absolute file path to clipboard" })
-- map <leader>cX to copy the entire repo glob to clipboard
vim.keymap.set("n", "<leader>aF", function()
  vim.fn.setreg("+", "#files://glob/**/*")
  vim.notify("Copied: #files://glob/**/*", vim.log.levels.INFO)
end, { desc = "Copy repo glob command to clipboard" })

-- Code actions (fix suggestions) for C#, etc.
vim.keymap.set({ "n", "v" }, "<leader>ca", vim.lsp.buf.code_action, {
  desc = "Code Action (fix suggestion)",
})
vim.keymap.set({ "n", "v" }, "<A-CR>", vim.lsp.buf.code_action, {
  desc = "Code Action (Alt+Enter)",
})

-- Git specific commands
vim.keymap.set("n", "<leader>ga", function()
  local confirm = vim.fn.input("Add ALL files (y/n): ")
  if confirm:lower() == "y" then
    vim.cmd("!git add .")
  else
    print("Aborted git add.")
  end
end, { desc = "Git add all (with confirmation)" })

vim.keymap.set("n", "<leader>gA", function()
  local confirm = vim.fn.input("Add ALL files and commit? (y/n): ")
  if confirm:lower() == "y" then
    vim.cmd("!git add .")
    local commit_msg = vim.fn.input("Commit message: ")
    if commit_msg ~= "" then
      vim.cmd('!git commit -m "' .. commit_msg .. '"')
    end
  else
    print("Aborted git add/commit.")
  end
end, { desc = "Git add all and commit (with confirmation, Alt)" })

local function markdown_url_under_cursor()
  local line = vim.api.nvim_get_current_line()
  local cursor_col = vim.api.nvim_win_get_cursor(0)[2] + 1

  for link_start, label, url in line:gmatch("()%[([^%]]+)%]%(([^%)%s]+)%)") do
    local label_start = link_start + 1
    local label_end = link_start + #label
    local url_start = link_start + #label + 3
    local url_end = url_start + #url - 1

    if
      (url:match("^https://") or url:match("^http://"))
      and ((label_start <= cursor_col and cursor_col <= label_end) or (url_start <= cursor_col and cursor_col <= url_end))
    then
      return url
    end
  end

  for _, pattern in ipairs({
    "()(https://[%w%-%._~:/%?#%[%]@!%$&'%(%)%*%+,;=%%]+)",
    "()(http://[%w%-%._~:/%?#%[%]@!%$&'%(%)%*%+,;=%%]+)",
  }) do
    for start_col, url in line:gmatch(pattern) do
      local end_col = start_col + #url - 1
      if start_col <= cursor_col and cursor_col <= end_col then
        return (url:gsub("[%)%]}>.,;:]+$", ""))
      end
    end
  end
end

function _G.open_markdown_url_under_cursor(notify_on_missing)
  local url = markdown_url_under_cursor()
  if not url then
    if notify_on_missing then
      vim.notify("No HTTP(S) URL under cursor", vim.log.levels.WARN)
    end
    return
  end

  vim.ui.open(url)
end

vim.api.nvim_create_autocmd("FileType", {
  pattern = { "markdown", "markdown.mdx" },
  callback = function(event)
    vim.keymap.set("n", "gd", function()
      _G.open_markdown_url_under_cursor(true)
    end, { buffer = event.buf, desc = "Open URL in browser" })

    vim.keymap.set(
      "n",
      "<LeftMouse>",
      "<LeftMouse><Cmd>lua _G.open_markdown_url_under_cursor(false)<CR>",
      { buffer = event.buf, silent = true }
    )
  end,
})
