using System.Reflection;
using System.Text.Json;
using ThemeEngine;
using ThemeEngine.Generators;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: dotnet run -- <theme.json> [--output-dir <dir>]");
    Console.Error.WriteLine("       dotnet run -- themes/elden-ring.json");
    Console.Error.WriteLine("       dotnet run -- themes/elden-ring.json --output-dir ~/.config");
    return 1;
}

var themeFile = Path.GetFullPath(args[0]);
if (!File.Exists(themeFile))
{
    NotificationService.Error("Theme Engine", $"Theme file not found: {themeFile}");
    return 1;
}

// Parse flags
var outputDir = Path.GetFullPath(
    args.SkipWhile(a => a != "--output-dir").Skip(1).FirstOrDefault()
    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
);

// Wallpapers dir is the directory containing the theme JSON
var wallpapersDir = Path.GetDirectoryName(themeFile)!;

Theme theme;
try
{
    var json = File.ReadAllText(themeFile);
    var options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };
    theme = JsonSerializer.Deserialize<Theme>(json, options)
        ?? throw new InvalidOperationException("Deserialization returned null.");
}
catch (Exception ex)
{
    NotificationService.Error("Theme Engine", $"Failed to parse theme: {ex.Message}");
    return 1;
}

Console.WriteLine($"Applying theme: {theme.Name}");
Console.WriteLine($"Output directory: {outputDir}");
Console.WriteLine();

// Discover all IGenerator implementations
var generators = Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(t => t is { IsClass: true, IsAbstract: false } && t.GetInterfaces().Contains(typeof(IGenerator)))
    .Select(t => (IGenerator)Activator.CreateInstance(t)!)
    .ToList();

var errors = new List<string>();

foreach (var gen in generators)
{
    try
    {
        var outPath = Path.Combine(outputDir, gen.OutputPath);
        var outDir = Path.GetDirectoryName(outPath)!;

        // Remove any files that block directory creation (e.g. a flat config file
        // at ~/.config/dunst where we need a ~/.config/dunst/ directory)
        var pathToCheck = outDir;
        while (!string.IsNullOrEmpty(pathToCheck) && pathToCheck.StartsWith(outputDir) && pathToCheck != outputDir)
        {
            if (!Directory.Exists(pathToCheck) && File.Exists(pathToCheck))
            {
                File.Delete(pathToCheck);
                break;
            }
            pathToCheck = Path.GetDirectoryName(pathToCheck)!;
        }

        Directory.CreateDirectory(outDir);

        var content = gen.Generate(theme, wallpapersDir);
        File.WriteAllText(outPath, content);

        Console.WriteLine($"  [{gen.Name}] -> {outPath}");
    }
    catch (Exception ex)
    {
        errors.Add($"{gen.Name}: {ex.Message}");
        NotificationService.Error("Theme Engine", $"Generator '{gen.Name}' failed: {ex.Message}");
    }
}

// Make scripts executable
var execMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
    | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute;
foreach (var script in Directory.GetFiles(Path.Combine(outputDir, ".config/hypr/scripts"), "*.sh"))
    File.SetUnixFileMode(script, execMode);

static void Run(string cmd)
{
    var parts = cmd.Split(' ', 2);
    var psi = new System.Diagnostics.ProcessStartInfo(parts[0], parts.Length > 1 ? parts[1] : "")
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };
    System.Diagnostics.Process.Start(psi)?.WaitForExit(5000);
}

static void RunDetached(string cmd)
{
    var psi = new System.Diagnostics.ProcessStartInfo("bash", $"-c \"{cmd}\"")
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };
    System.Diagnostics.Process.Start(psi);
}

// Always kill and restart the wallpaper cycler (picks up new image list)
{
    var killPsi = new System.Diagnostics.ProcessStartInfo("bash", "-c \"pkill -f wallpaper-cycler\"")
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };
    System.Diagnostics.Process.Start(killPsi)?.WaitForExit(3000);
    Thread.Sleep(300);
    RunDetached("~/.config/hypr/scripts/wallpaper-cycler.sh &");
}

// Live-reload kitty theme in all windows via socket
{
    var sockets = Directory.GetFiles("/tmp", "kitty-socket-*");
    foreach (var sock in sockets)
    {
        var kittyPsi = new System.Diagnostics.ProcessStartInfo("kitten",
            $"@ --to unix:{sock} set-colors --all --configured ~/.config/kitty/kitty.conf")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        try { System.Diagnostics.Process.Start(kittyPsi)?.WaitForExit(3000); } catch { }
    }
}

Console.WriteLine();

// Live-reload all services
Run("hyprctl reload");
Run("killall -SIGUSR2 waybar");
Run("killall dunst");
Run($"gsettings set org.gnome.desktop.interface color-scheme '{theme.Gtk.ColorScheme}'");
Run($"gsettings set org.gnome.desktop.interface gtk-theme '{theme.Gtk.Theme}'");

// Hyprpaper doesn't support full config reload; kill it and let hyprctl reload respawn it
Run("killall hyprpaper");
RunDetached("hyprpaper");

// Hyprlauncher needs reloading; kill it and let hyprctl reload it on demand
Run("killall hyprlauncher");

if (errors.Count > 0)
{
    NotificationService.Error("Theme Engine", $"Theme '{theme.Name}' applied with {errors.Count} error(s).");
}
else
{
    NotificationService.Success("Theme Engine", $"Theme '{theme.Name}' applied successfully.");
}

return 0;
