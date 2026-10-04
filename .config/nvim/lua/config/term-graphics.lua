-- Neovim's :terminal parses kitty graphics APC sequences but never renders them,
-- so a TUI child process (Copilot CLI, chafa, timg, ...) reserves the cells and
-- leaves them blank. Forward the graphics commands to the host terminal instead.
--
-- Only placement-independent commands are forwarded. Copilot CLI and most modern
-- TUIs use unicode placeholders (U=1), where the image is anchored to U+10EEEE
-- cells that Neovim already stores and redraws, so the host terminal positions,
-- scrolls and clips the image without any coordinate translation on our side.

local ST = "\27\\"
local PREFIX = "\27_G"

local M = {}

---@type uv.uv_tty_t?
local tty
local tty_failed = false

local function write(data)
  if tty_failed then
    return
  end
  if not tty then
    local ok, handle = pcall(vim.uv.new_tty, 1, false)
    if not ok or not handle then
      tty_failed = true
      return
    end
    tty = handle
  end
  if vim.env.TMUX then
    data = "\27Ptmux;" .. data:gsub("\27", "\27\27") .. ST
  end
  tty:write(data)
end

local function host_supports_graphics()
  if vim.env.KITTY_WINDOW_ID or vim.env.GHOSTTY_RESOURCES_DIR or vim.env.GHOSTTY_BIN_DIR then
    return true
  end
  if (vim.env.TERM_PROGRAM or ""):find("WezTerm", 1, true) then
    return true
  end
  return (vim.env.TERM or ""):find("kitty", 1, true) ~= nil
end

---@return table<string, string>?
local function parse_control(sequence)
  local control = sequence:match("^\27_G([^;]*)")
  if not control then
    return nil
  end
  local keys = {}
  for key, value in control:gmatch("([a-zA-Z])=([^,]*)") do
    keys[key] = value
  end
  return keys
end

-- `a` defaults to `t` (transmit) when omitted. Transmit, delete and animate never
-- move or read the cursor, and `U=1` placements are driven by placeholder cells,
-- so all of them stay correct when replayed outside the terminal buffer. A plain
-- `a=T`/`a=p` would be placed at the host cursor, which is not where the terminal
-- window is, so those are dropped.
local function is_placement_independent(keys)
  if keys.U == "1" then
    return true
  end
  local action = keys.a or "t"
  return action == "t" or action == "d" or action == "a"
end

---@type table<integer, "forward"|"skip">
local chunked = {}
---@type table<integer, table<string, true>>
local transmitted = {}

local function on_term_request(event)
  local sequence = event.data.sequence
  if sequence:sub(1, #PREFIX) ~= PREFIX then
    return
  end
  local keys = parse_control(sequence)
  if not keys then
    return
  end

  -- A chunked transmission only carries its control keys in the first chunk, so
  -- the decision made there has to apply to every follow-up chunk.
  local forward
  if chunked[event.buf] then
    forward = chunked[event.buf] == "forward"
    if keys.m ~= "1" then
      chunked[event.buf] = nil
    end
  else
    forward = is_placement_independent(keys)
    if keys.m == "1" then
      chunked[event.buf] = forward and "forward" or "skip"
    end
  end

  if not forward then
    return
  end

  if keys.i and keys.a ~= "d" then
    transmitted[event.buf] = transmitted[event.buf] or {}
    transmitted[event.buf][keys.i] = true
  end

  write(sequence .. (event.data.terminator or ST))
end

local function free_images(buf)
  local ids = transmitted[buf]
  if ids then
    for id in pairs(ids) do
      write(PREFIX .. "a=d,d=I,i=" .. id .. ",q=2" .. ST)
    end
  end
  transmitted[buf] = nil
  chunked[buf] = nil
end

function M.setup()
  if vim.g.term_graphics_passthrough == false then
    return
  end
  if vim.fn.has("gui_running") == 1 or #vim.api.nvim_list_uis() == 0 then
    return
  end
  if not host_supports_graphics() then
    return
  end

  -- Placeholder cells carry the image id in a 24-bit foreground colour.
  vim.o.termguicolors = true

  local group = vim.api.nvim_create_augroup("term_graphics_passthrough", { clear = true })
  vim.api.nvim_create_autocmd("TermRequest", {
    group = group,
    callback = on_term_request,
  })
  vim.api.nvim_create_autocmd({ "TermClose", "BufWipeout" }, {
    group = group,
    callback = function(event)
      free_images(event.buf)
    end,
  })
end

return M
