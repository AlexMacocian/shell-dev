return {
  "saghen/blink.cmp",
  opts = function(_, opts)
    -- Make sure the sources table exists
    opts.sources = opts.sources or {}
    opts.sources.default = opts.sources.default or {}
    opts.sources.providers = opts.sources.providers or {}

    -- Add easy-dotnet to the default source list (avoid duplicates)
    if not vim.tbl_contains(opts.sources.default, "easy-dotnet") then
      table.insert(opts.sources.default, "easy-dotnet")
    end

    -- Register the provider if it isn't already defined
    opts.sources.providers["easy-dotnet"] = vim.tbl_deep_extend("force", opts.sources.providers["easy-dotnet"] or {}, {
      name = "easy-dotnet",
      enabled = true,
      module = "easy-dotnet.completion.blink",
      score_offset = 10000,
      async = true,
    })

    return opts
  end,
}
