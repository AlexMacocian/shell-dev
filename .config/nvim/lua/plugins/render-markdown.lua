-- Disable wrap when render-markdown is active so table pipes stay aligned
return {
  "MeanderingProgrammer/render-markdown.nvim",
  opts = {
    win_options = {
      wrap = { rendered = false },
    },
  },
}
