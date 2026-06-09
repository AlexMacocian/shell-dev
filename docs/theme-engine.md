# Theme Engine

A Go console app that reads a theme JSON and generates configs for Hyprland,
waybar, kitty, neovim, VS Code, Firefox, dunst, wofi, omni-launcher,
quick-visor, GTK, and hyprlock.

## Usage

```bash
bash linux/apply-theme.sh "Elden Ring"
```

Or directly:

```bash
rainbeau themes/elden-ring.json
```

## Wallpapers

Themes can declare three wallpaper sources under `wallpapers`:

- `images` — static images, played by `hyprpaper`.

- `videos` — videos and GIFs, played by `mpvpaper`.

- `lotties` — Lottie JSON animations. Rendered to GIF on theme apply via
  `lottie2gif` (Samsung's `rlottie` + the `lottieconv` CLI) and cached under
  `themes/.lottie-cache/<hash>.gif`. Cached outputs are appended to `videos`
  internally so they flow through the existing `mpvpaper` cycler unchanged.

  Install once on Arch:

  ```bash
  paru -S rlottie lottieconv
  ```

  If `lottie2gif` is missing, the theme apply prints a warning and skips the
  Lottie sources rather than failing.

## Firefox Live Theming

Firefox themes update at runtime without restarting the browser, powered by a
signed WebExtension and a native messaging host.

### Setup

Run once after installing Firefox and creating a profile:

```bash
bash linux/init-firefox.sh
```

This sets up:

- Chrome directory symlink (`userChrome.css`, `userContent.css`)
- `user.js` symlink for Firefox preferences
- The signed theme extension (`.xpi`) in the profile
- The native messaging host manifest

Restart Firefox after running the script and approve the extension when
prompted.

### Extension Source

The extension lives in `theme-engine/firefox-extension/`. If you modify it,
re-sign with `web-ext sign` and re-run `init-firefox.sh`.

### Known Limitation

Switching between light and dark themes requires a Firefox restart for hover
states and tooltips to update. Firefox's design system uses `light-dark()` CSS
functions tied to `color-scheme`, which only re-evaluates on startup. Switching
between themes of the same mode (e.g. dark → dark) works fully at runtime.
