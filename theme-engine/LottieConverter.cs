using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ThemeEngine;

/// <summary>
/// Converts Lottie JSON animations into GIFs that mpvpaper can play.
/// Outputs are cached under <c>&lt;wallpapersDir&gt;/.lottie-cache/&lt;hash&gt;.gif</c>
/// so unchanged source files are not re-rendered on every theme apply.
/// </summary>
public static class LottieConverter
{
    private const string CacheSubdir = ".lottie-cache";

    /// <summary>
    /// Renders each Lottie source under <paramref name="wallpapersDir"/> into
    /// a cached GIF and returns the cache-relative GIF paths. Paths are
    /// suitable for appending to <see cref="WallpaperSettings.Videos"/> since
    /// they're relative to the wallpapers root that the cycler script uses.
    /// </summary>
    /// <remarks>
    /// Missing <c>lottie2gif</c> is non-fatal: a warning is printed and the
    /// returned list is empty so the rest of the theme apply still succeeds.
    /// </remarks>
    public static string[] Convert(string[] lottiePaths, string wallpapersDir, string bgHex, string lineHex)
    {
        if (lottiePaths.Length == 0) return [];

        var bg = NormalizeHex(bgHex);
        var line = NormalizeHex(lineHex);

        if (!IsToolAvailable("lottie2gif"))
        {
            Console.WriteLine(
                "  [Lottie] WARNING: 'lottie2gif' not found on PATH. Install with " +
                "'paru -S rlottie lottieconv' to enable Lottie wallpapers.");
            return [];
        }

        var cacheDir = Path.Combine(wallpapersDir, CacheSubdir);
        Directory.CreateDirectory(cacheDir);

        var outputs = new List<string>();
        foreach (var rel in lottiePaths)
        {
            var abs = Path.Combine(wallpapersDir, rel);
            if (!File.Exists(abs))
            {
                Console.WriteLine($"  [Lottie] WARNING: source not found: {rel}");
                continue;
            }

            // Name the output after the source filename. Theme switches
            // always re-render and overwrite, so there's no need to key
            // on the source bytes or tint colors.
            var stem = Path.GetFileNameWithoutExtension(rel);
            var gifRel = Path.Combine(CacheSubdir, $"{stem}.gif");
            var gifAbs = Path.Combine(wallpapersDir, gifRel);

            Console.WriteLine($"  [Lottie] rendering {rel} -> {gifRel}");

            // `.lottie` files are dotLottie ZIP bundles — lottie2gif only
            // accepts the raw inner JSON, so extract first.
            string sourceJsonPath = abs;
            string? extracted = null;
            if (Path.GetExtension(abs).Equals(".lottie", StringComparison.OrdinalIgnoreCase))
            {
                extracted = ExtractDotLottieJson(abs);
                if (extracted is null)
                {
                    Console.WriteLine($"  [Lottie] WARNING: could not extract animation JSON from {rel}");
                    continue;
                }
                sourceJsonPath = extracted;
            }

            // Recolor strokes/fills to the theme line color so the
            // generic asset matches the active palette.
            var tinted = TintLottieJson(sourceJsonPath, line);

            try
            {
                if (!RunLottie2Gif(tinted, gifAbs, bg))
                {
                    Console.WriteLine($"  [Lottie] WARNING: failed to render {rel}");
                    continue;
                }
            }
            finally
            {
                if (extracted is not null && File.Exists(extracted))
                    File.Delete(extracted);
                if (File.Exists(tinted))
                    File.Delete(tinted);
            }

            outputs.Add(gifRel);
        }
        return [.. outputs];
    }

    /// <summary>
    /// Extracts the first <c>animations/*.json</c> entry from a dotLottie ZIP
    /// to a temp file and returns its path. Returns <c>null</c> if no
    /// animation entry is found.
    /// </summary>
    private static string? ExtractDotLottieJson(string dotLottiePath)
    {
        try
        {
            using var zip = ZipFile.OpenRead(dotLottiePath);
            var entry = zip.Entries.FirstOrDefault(e =>
                e.FullName.StartsWith("animations/", StringComparison.OrdinalIgnoreCase)
                && e.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
            if (entry is null) return null;

            var tempPath = Path.Combine(Path.GetTempPath(), $"lottie-{Guid.NewGuid():N}.json");
            entry.ExtractToFile(tempPath, overwrite: true);
            return tempPath;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Loads a Lottie JSON, walks every stroke (<c>ty:"st"</c>) and fill
    /// (<c>ty:"fl"</c>) shape and overrides its color <c>c.k</c> array with the
    /// supplied <paramref name="lineHex"/>. Writes the result to a temp file
    /// and returns its path. If parsing fails the original file is returned
    /// untouched.
    /// </summary>
    private static string TintLottieJson(string jsonPath, string lineHex)
    {
        try
        {
            var json = File.ReadAllText(jsonPath);
            var root = JsonNode.Parse(json);
            if (root is null) return jsonPath;

            var (r, g, b) = HexToFloatRgb(lineHex);
            Recolor(root, r, g, b);

            var outPath = Path.Combine(Path.GetTempPath(), $"lottie-tinted-{Guid.NewGuid():N}.json");
            File.WriteAllText(outPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = false }));
            return outPath;
        }
        catch
        {
            return jsonPath;
        }
    }

    private static void Recolor(JsonNode? node, double r, double g, double b)
    {
        switch (node)
        {
            case JsonObject obj:
                if (obj["ty"] is JsonValue tyVal
                    && tyVal.TryGetValue<string>(out var ty)
                    && (ty == "st" || ty == "fl")
                    && obj["c"] is JsonObject cObj
                    && cObj["k"] is JsonArray k)
                {
                    // Preserve existing alpha (defaults to 1.0 if missing) so
                    // partial-opacity strokes stay partial.
                    double a = k.Count > 3 && k[3] is JsonValue av && av.TryGetValue<double>(out var alpha) ? alpha : 1.0;
                    cObj["k"] = new JsonArray(r, g, b, a);
                }
                foreach (var kv in obj.ToList())
                    Recolor(kv.Value, r, g, b);
                break;
            case JsonArray arr:
                foreach (var item in arr.ToList())
                    Recolor(item, r, g, b);
                break;
        }
    }

    private static (double R, double G, double B) HexToFloatRgb(string hex)
    {
        var h = NormalizeHex(hex);
        var r = System.Convert.ToInt32(h.Substring(0, 2), 16) / 255.0;
        var g = System.Convert.ToInt32(h.Substring(2, 2), 16) / 255.0;
        var b = System.Convert.ToInt32(h.Substring(4, 2), 16) / 255.0;
        return (r, g, b);
    }

    private static string NormalizeHex(string hex)
    {
        var h = hex.TrimStart('#');
        if (h.Length == 8) h = h.Substring(0, 6);
        return h.ToLowerInvariant();
    }

    private static bool RunLottie2Gif(string input, string output, string bgHex)
    {
        try
        {
            // lottie2gif expects a bare 6-char hex bgColor (no `#` / `0x`).
            // Pass --non-transparent so the supplied color actually paints
            // the canvas instead of falling back to alpha.
            var psi = new ProcessStartInfo(
                "lottie2gif",
                $"--out \"{output}\" --non-transparent \"{input}\" {bgHex}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using var proc = Process.Start(psi);
            if (proc is null) return false;
            proc.WaitForExit(60_000);
            return proc.ExitCode == 0 && File.Exists(output);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsToolAvailable(string name)
    {
        try
        {
            var psi = new ProcessStartInfo("which", name)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using var proc = Process.Start(psi);
            if (proc is null) return false;
            proc.WaitForExit(2_000);
            return proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
