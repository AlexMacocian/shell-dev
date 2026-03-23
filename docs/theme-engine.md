# Theme Engine

A .NET console app that reads a theme JSON and generates configs for Hyprland,
waybar, kitty, neovim, VS Code, Firefox, dunst, wofi, GTK, and hyprlock.

## Usage

```bash
bash linux/apply-theme.sh "Elden Ring"
```

Or directly:

```bash
dotnet run --project theme-engine -- themes/elden-ring.json
```

## How It Works

1. Deserializes the theme JSON into typed C# records (`Theme.cs`)
2. Auto-discovers all `IGenerator` implementations via reflection
3. Each generator produces one config file from the theme data
4. Writes files, sets script permissions

## Adding a Generator

Create a class implementing `IGenerator`:

```csharp
public class MyAppGenerator : IGenerator
{
    public string Name => "My App";
    public string OutputPath => ".config/myapp/config";

    public string Generate(Theme theme, string wallpapersDir)
    {
        var c = theme.Colors;
        return $$"""
        background = {{c.Bg0}}
        foreground = {{c.Text}}
        """;
    }
}
```

No registration needed — it's discovered automatically. Add the output path to `.gitignore`.

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

Restart Firefox after running the script and approve the extension when prompted.

### Extension Source

The extension lives in `theme-engine/firefox-extension/`. If you modify it, re-sign
with `web-ext sign` and re-run `init-firefox.sh`.

### Known Limitation

Switching between light and dark themes requires a Firefox restart for hover
states and tooltips to update. Firefox's design system uses `light-dark()` CSS
functions tied to `color-scheme`, which only re-evaluates on startup. Switching
between themes of the same mode (e.g. dark → dark) works fully at runtime.
