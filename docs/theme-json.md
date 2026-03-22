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

- **`colors`** — 13-color palette used across all generators
- **`gtk.color_scheme`** — `prefer-dark` or `prefer-light`,
drives dark/light mode globally
- **`wallpapers.images`** / **`videos`** — paths relative to `themes/`
- **`waybar.separator`** — glyph between status bar modules
- **`hyprland`** — border size, rounding, gaps, blur, animation speeds
- **`font`** — family, fallback list, base size
