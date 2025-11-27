return {
  {
    "mfussenegger/nvim-dap",
    optional = true,
    opts = function(_, _)
      local dap = require("dap")

      ---------------------------------------------------------------------------
      -- Adapter: netcoredbg
      ---------------------------------------------------------------------------
      if not dap.adapters.coreclr then
        dap.adapters.coreclr = {
          type = "executable",
          command = "netcoredbg", -- ensure in PATH
          args = { "--interpreter=vscode" },
        }
      end

      ---------------------------------------------------------------------------
      -- Cached DLL so program() and cwd() always match
      ---------------------------------------------------------------------------
      local last_dll = nil

      ---------------------------------------------------------------------------
      -- Pick a project (.csproj) from workspace
      ---------------------------------------------------------------------------
      local function pick_csproj()
        local cwd = vim.fn.getcwd()
        local csprojs = vim.fn.globpath(cwd, "**/*.csproj", false, true)

        if #csprojs == 0 then
          return nil
        elseif #csprojs == 1 then
          return csprojs[1]
        end

        -- Simple terminal picker (works inside DAP sessions)
        local items = { "Select project:" }
        for i, file in ipairs(csprojs) do
          items[#items + 1] = string.format("%d: %s", i, vim.fn.fnamemodify(file, ":."))
        end

        local choice = vim.fn.inputlist(items)
        if choice < 1 or choice > #csprojs then
          return nil
        end

        return csprojs[choice]
      end

      ---------------------------------------------------------------------------
      -- Find DLL via wildcard scanning (bin/Debug/net*/Name.dll)
      ---------------------------------------------------------------------------
      local function find_dll_wildcard(project_dir, project_name)
        local pattern = string.format("%s/bin/Debug/net*/%s.dll", project_dir, project_name)
        local matches = vim.fn.glob(pattern, false, true)

        if matches and #matches > 0 then
          table.sort(matches) -- lexical sort = highest TFM last
          return matches[#matches]
        end

        return nil
      end

      ---------------------------------------------------------------------------
      -- Resolve DLL for the given project
      ---------------------------------------------------------------------------
      local function resolve_dll()
        if last_dll and vim.fn.filereadable(last_dll) == 1 then
          return last_dll
        end

        local csproj = pick_csproj()
        if not csproj then
          return vim.fn.input("Path to DLL: ", vim.fn.getcwd() .. "/", "file")
        end

        local project_dir = vim.fn.fnamemodify(csproj, ":h")
        local project_name = vim.fn.fnamemodify(csproj, ":t:r")

        -- Try wildcard scanning
        local dll = find_dll_wildcard(project_dir, project_name)
        if dll then
          last_dll = dll
          return dll
        end

        -- Fallback manual entry
        local manual = vim.fn.input(
          string.format("DLL not found for %s, enter path manually: ", project_name),
          project_dir .. "/bin/",
          "file"
        )
        if manual ~= "" then
          last_dll = manual
        end
        return last_dll
      end

      ---------------------------------------------------------------------------
      -- DAP Launch Config
      ---------------------------------------------------------------------------
      local config = {
        type = "coreclr",
        name = "Launch .NET (auto)",
        request = "launch",
        program = function()
          return resolve_dll()
        end,
        cwd = function()
          local dll = resolve_dll()
          if not dll then
            return vim.fn.getcwd()
          end

          -- DLL path: <proj>/bin/Debug/net*/Name.dll
          return vim.fn.fnamemodify(dll, ":h")
        end,
      }

      local config_build = {
        type = "coreclr",
        name = "Build & Launch .NET (auto)",
        request = "launch",
        preLaunchTask = function()
          local csproj = pick_csproj()
          if not csproj then
            return nil
          end

          last_dll = nil -- Clear cache to force re-resolve after build

          local cmd = string.format("dotnet build %s", vim.fn.shellescape(csproj))

          vim.notify("Building: " .. csproj, vim.log.levels.INFO)
          local result = vim.fn.system(cmd)

          if vim.v.shell_error ~= 0 then
            vim.notify("Build failed:\n" .. result, vim.log.levels.ERROR)
            return false
          end

          vim.notify("Build succeeded", vim.log.levels.INFO)
          return true
        end,
        program = function()
          return resolve_dll()
        end,
        cwd = function()
          local dll = resolve_dll()
          if not dll then
            return vim.fn.getcwd()
          end
          return vim.fn.fnamemodify(dll, ":h")
        end,
      }

      dap.configurations.cs = dap.configurations.cs or {}
      table.insert(dap.configurations.cs, config)
      table.insert(dap.configurations.cs, config_build)

      dap.configurations.fsharp = dap.configurations.fsharp or {}
      table.insert(dap.configurations.fsharp, config)
      table.insert(dap.configurations.fsharp, config_build)

      dap.configurations.vb = dap.configurations.vb or {}
      table.insert(dap.configurations.vb, config)
      table.insert(dap.configurations.vb, config_build)
    end,
  },
}
