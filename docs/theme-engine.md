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
