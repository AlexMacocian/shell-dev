using System.Reflection;
using System.Text.Json;
using ThemeEngine;
using ThemeEngine.Generators;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: dotnet run -- <theme.json> [--output-dir <dir>] [--restart]");
    Console.Error.WriteLine("       dotnet run -- wallpapers/elden-ring.json");
    Console.Error.WriteLine("       dotnet run -- wallpapers/elden-ring.json --output-dir ~/.config --restart");
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
var restart = args.Contains("--restart");

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

Console.WriteLine();

if (restart)
{
    Console.WriteLine("Restarting services...");

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

    Run("killall hyprpaper");
    Run("killall waybar");
    Run("killall dunst");
    Thread.Sleep(500);
    Run("hyprpaper");
    Run("waybar");
    Run("dunst");

    // Apply GTK theme to running session
    Run($"gsettings set org.gnome.desktop.interface color-scheme '{theme.Gtk.ColorScheme}'");
    Run($"gsettings set org.gnome.desktop.interface gtk-theme '{theme.Gtk.Theme}'");

    // Wait for dunst to be ready before sending notification
    Thread.Sleep(1000);
}

if (errors.Count > 0)
{
    NotificationService.Error("Theme Engine", $"Theme '{theme.Name}' applied with {errors.Count} error(s).");
}
else
{
    NotificationService.Success("Theme Engine", $"Theme '{theme.Name}' applied successfully.");
}

return 0;
