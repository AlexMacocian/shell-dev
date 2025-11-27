-- Keymaps are automatically loaded on the VeryLazy event
-- Default keymaps that are always set: https://github.com/LazyVim/LazyVim/blob/main/lua/lazyvim/config/keymaps.lua
-- Add any additional keymaps here

-- Make Ctrl-\ Ctrl-n behave like <Esc> everywhere
vim.keymap.set("t", "<C-\\><C-n>", "<C-\\><C-n>", { desc = "Terminal Normal Mode" })
vim.keymap.set({ "i", "n", "v" }, "<C-\\><C-n>", "<Esc>", { noremap = true, silent = true })

-- map <leader>cx to copy current file path
vim.keymap.set("n", "<leader>cx", function()
  vim.fn.setreg("+", vim.fn.expand("%:p"))
end, { desc = "Copy absolute file path to clipboard" })

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
