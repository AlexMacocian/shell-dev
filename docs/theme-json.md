# Theme JSON

Themes are JSON files in `themes/`. Each defines colors, fonts, wallpapers,
and per-app tuning for the entire desktop.

## Example

See [church-bud.json](../themes/church-bud.json) for a complete example.

## Creating a Theme

1. Copy an existing theme:

   ```bash
   cp themes/scarlet-rot.json themes/my-theme.json
   ```

2. Add wallpapers:

   ```bash
   mkdir themes/my-theme
   # Add images/videos
   ```

3. Edit the JSON — colors, wallpaper paths, separator glyph, etc.

4. Apply:

   ```bash
   bash linux/apply-theme.sh "My Theme" --restart
   ```

## Key Sections

- **`colors`** — 13-color palette used across all generators. The engine
  guarantees WCAG AAA (7:1) contrast against `bg0` for every derived
  foreground color (text, ANSI palette, selection), so themes can pick
  evocative source colors without worrying about readability — the engine
  will push lightness (and, as a last resort, saturation) until each color
  meets the target.
- **`gtk.color_scheme`** — `prefer-dark` or `prefer-light`,
drives dark/light mode globally
- **`wallpapers.images`** / **`videos`** — paths relative to `themes/`
- **`waybar.separator`** — glyph between status bar modules
- **`hyprland`** — border size, rounding, gaps, blur, animation speeds
- **`font`** — family, fallback list, base size
- **`terminal`** _(optional)_ — terminal-specific tuning. Fields:
  - `opacity` _(default `1.0`)_ — Kitty window background opacity. The
    default is fully opaque so colors and selection rectangles stay vivid;
    set lower if you want wallpaper bleed-through.
  - `min_contrast` _(default `7.0`)_ — WCAG ratio enforced for every
    foreground-on-background pair. Lower it (e.g. `4.5`) to allow more
    saturated source colors at the cost of readability.
