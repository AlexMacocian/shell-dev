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

Console.WriteLine();

if (restart)
{
    Console.WriteLine("Restarting services...");

    // Show a progress popup via wofi (fixed size, no resize)
    var wofiPsi = new System.Diagnostics.ProcessStartInfo("bash",
        "-c \"echo '  Applying theme...' | wofi -d -j -W 600 -H 40 -k /dev/null -s ~/.config/wofi/style.css -D dynamic_lines=false 2>/dev/null\"")
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };
    var wofiProc = System.Diagnostics.Process.Start(wofiPsi);

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

    var firefoxWasRunning = System.Diagnostics.Process.GetProcessesByName("firefox").Length > 0;
    var firefoxWorkspace = "";
    if (firefoxWasRunning)
    {
        var psi = new System.Diagnostics.ProcessStartInfo("hyprctl", "clients -j")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        var proc = System.Diagnostics.Process.Start(psi);
        var output = proc?.StandardOutput.ReadToEnd() ?? "";
        proc?.WaitForExit(3000);
        try
        {
            var clients = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(output);
            foreach (var client in clients.EnumerateArray())
            {
                var cls = client.GetProperty("class").GetString() ?? "";
                if (cls.Contains("firefox", StringComparison.OrdinalIgnoreCase))
                {
                    firefoxWorkspace = client.GetProperty("workspace").GetProperty("id").ToString();
                    break;
                }
            }
        }
        catch { }
    }

    Run("killall hyprpaper");
    Run("killall waybar");
    Run("killall dunst");
    Run("killall hyprlauncher");
    if (firefoxWasRunning) Run("killall firefox");
    Thread.Sleep(500);

    Run("hyprpaper");
    Run("waybar");
    Run("dunst");

    if (firefoxWasRunning)
    {
        Run("firefox");
        if (!string.IsNullOrEmpty(firefoxWorkspace))
        {
            Thread.Sleep(500);
            Run($"hyprctl dispatch movetoworkspacesilent {firefoxWorkspace},class:firefox");
        }
    }

    // Apply GTK theme to running session
    Run($"gsettings set org.gnome.desktop.interface color-scheme '{theme.Gtk.ColorScheme}'");
    Run($"gsettings set org.gnome.desktop.interface gtk-theme '{theme.Gtk.Theme}'");

    // Close the progress popup
    try { wofiProc?.Kill(); } catch { }
    try { wofiProc?.WaitForExit(1000); } catch { }
    Run("killall wofi");
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
