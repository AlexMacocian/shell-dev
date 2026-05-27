using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ThemeEngine;

/// <summary>
/// Converts Lottie JSON animations into MP4 videos that mpvpaper can play.
/// Outputs are cached under
/// <c>$XDG_CACHE_HOME/shell-dev/lottie-cache/&lt;stem&gt;-&lt;bg&gt;-&lt;line&gt;.mp4</c>
/// (falling back to <c>~/.cache/shell-dev/lottie-cache/</c>) keyed by the
/// source filename plus the bg/line colors used to tint it, so unchanged
/// palettes are not re-rendered on subsequent theme applies. The cache
/// lives outside the repo so generated artifacts never pollute the
/// themes/ tree even when it's symlinked into ~/.config/hypr/wallpapers.
/// </summary>
/// <remarks>
/// Renders frame-by-frame via P/Invoke into <c>librlottie</c> (see
/// <see cref="RlottieNative"/>) and pipes the raw BGRA buffers into
/// <c>ffmpeg</c> for h264 encoding. This is dramatically faster than the
/// old <c>lottie2gif</c> path and produces files ~10× smaller. A
/// background fill is injected as a shape layer so rlottie's transparent
/// background gets replaced by the theme's bg color.
/// </remarks>
public static class LottieConverter
{
  /// <summary>
  /// Absolute path to the persistent Lottie render cache. Lives outside
  /// the repo (<c>$XDG_CACHE_HOME/shell-dev/lottie-cache</c>) so the
  /// large generated MP4s aren't accidentally committed when
  /// <c>themes/</c> is symlinked into the wallpapers dir.
  /// </summary>
  private static string CacheDir
  {
    get
    {
      var xdg = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
      if (string.IsNullOrEmpty(xdg))
        xdg = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".cache");
      return Path.Combine(xdg, "shell-dev", "lottie-cache");
    }
  }

  /// <summary>
  /// Renders each Lottie source under <paramref name="wallpapersDir"/> into
  /// a cached MP4 and returns the absolute MP4 paths. Absolute paths are
  /// used (rather than wallpaper-relative) because the cache lives outside
  /// the wallpapers tree; the wallpaper-cycler script detects absolute
  /// entries and uses them as-is.
  /// </summary>
  /// <remarks>
  /// Missing <c>ffmpeg</c> or <c>librlottie</c> is non-fatal: a warning is
  /// printed and the returned list is empty so the rest of the theme apply
  /// still succeeds.
  /// </remarks>
  public static string[] Convert(string[] lottiePaths, string wallpapersDir, string bgHex, string lineHex)
  {
    if (lottiePaths.Length == 0) return [];

    var bg = NormalizeHex(bgHex);
    var line = NormalizeHex(lineHex);

    if (!IsToolAvailable("ffmpeg"))
    {
      Console.WriteLine(
          "  [Lottie] WARNING: 'ffmpeg' not found on PATH. Install with " +
          "'sudo pacman -S ffmpeg' to enable Lottie wallpapers.");
      return [];
    }

    var cacheDir = CacheDir;
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

      // Key the cached video by source stem + bg/line color so different
      // themes that share the same source asset don't clobber each
      // other and so the cache hits across runs as long as the
      // palette is unchanged.
      var stem = Path.GetFileNameWithoutExtension(rel);
      var mp4Abs = Path.Combine(cacheDir, $"{stem}-{bg}-{line}.mp4");

      if (File.Exists(mp4Abs))
      {
        Console.WriteLine($"  [Lottie] cache hit {rel} -> {mp4Abs}");
        outputs.Add(mp4Abs);
        continue;
      }

      Console.WriteLine($"  [Lottie] rendering {rel} -> {mp4Abs}");
      using var progress = ProgressNotification.Start(
          "Theme Engine",
          $"Generating Lottie video for {stem} (this may take some time)...");

      // `.lottie` files are dotLottie ZIP bundles — rlottie's from_file
      // also accepts them, but extracting first lets us tint/inject bg
      // on the raw JSON before rendering.
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

      // Recolor strokes/fills to the theme line color and inject a
      // full-canvas background shape so the rendered MP4 doesn't show
      // through to black.
      var prepared = PrepareLottieJson(sourceJsonPath, line, bg);

      try
      {
        if (!RunLottieConvert(prepared, mp4Abs))
        {
          // Clean up any partial output so the next run re-renders
          // instead of treating a truncated file as a valid cache entry.
          if (File.Exists(mp4Abs))
          {
            try { File.Delete(mp4Abs); } catch { /* best effort */ }
          }
          Console.WriteLine($"  [Lottie] WARNING: failed to render {rel}");
          continue;
        }
      }
      finally
      {
        if (extracted is not null && File.Exists(extracted))
          File.Delete(extracted);
        if (File.Exists(prepared))
          File.Delete(prepared);
      }

      outputs.Add(mp4Abs);
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
  /// Loads a Lottie JSON, recolors strokes/fills with <paramref name="lineHex"/>,
  /// and injects a full-canvas background shape filled with <paramref name="bgHex"/>
  /// so the rendered MP4 has a solid theme background instead of black.
  /// Writes the result to a temp file and returns its path. If parsing
  /// fails the original file path is returned untouched.
  /// </summary>
  private static string PrepareLottieJson(string jsonPath, string lineHex, string bgHex)
  {
    try
    {
      var json = File.ReadAllText(jsonPath);
      if (JsonNode.Parse(json) is not JsonObject root) return jsonPath;

      var (lr, lg, lb) = HexToFloatRgb(lineHex);
      Recolor(root, lr, lg, lb);

      InjectBackgroundLayer(root, bgHex);

      var outPath = Path.Combine(Path.GetTempPath(), $"lottie-prep-{Guid.NewGuid():N}.json");
      File.WriteAllText(outPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = false }));
      return outPath;
    }
    catch
    {
      return jsonPath;
    }
  }

  /// <summary>
  /// Appends a full-canvas filled rectangle shape layer to <paramref name="root"/>'s
  /// <c>layers</c> array so it sits behind all existing content. Lottie renders
  /// the layers array top-down, so appending puts the background at the back.
  /// </summary>
  private static void InjectBackgroundLayer(JsonObject root, string bgHex)
  {
    if (root["layers"] is not JsonArray layers) return;
    if (root["w"] is not JsonValue wv || !wv.TryGetValue<double>(out var w)) return;
    if (root["h"] is not JsonValue hv || !hv.TryGetValue<double>(out var h)) return;
    if (root["op"] is not JsonValue opv || !opv.TryGetValue<double>(out var op)) return;

    var (r, g, b) = HexToFloatRgb(bgHex);

    var bgLayer = new JsonObject
    {
      ["ddd"] = 0,
      ["ind"] = 9999,
      ["ty"] = 4, // shape layer
      ["nm"] = "ThemeEngineBackground",
      ["sr"] = 1,
      ["ks"] = new JsonObject
      {
        ["o"] = new JsonObject { ["a"] = 0, ["k"] = 100 },
        ["r"] = new JsonObject { ["a"] = 0, ["k"] = 0 },
        ["p"] = new JsonObject { ["a"] = 0, ["k"] = new JsonArray(w / 2.0, h / 2.0, 0) },
        ["a"] = new JsonObject { ["a"] = 0, ["k"] = new JsonArray(0, 0, 0) },
        ["s"] = new JsonObject { ["a"] = 0, ["k"] = new JsonArray(100, 100, 100) },
      },
      ["ao"] = 0,
      ["shapes"] = new JsonArray
      {
        new JsonObject
        {
          ["ty"] = "gr",
          ["it"] = new JsonArray
          {
            new JsonObject
            {
              ["ty"] = "rc",
              ["d"] = 1,
              ["s"] = new JsonObject { ["a"] = 0, ["k"] = new JsonArray(w, h) },
              ["p"] = new JsonObject { ["a"] = 0, ["k"] = new JsonArray(0, 0) },
              ["r"] = new JsonObject { ["a"] = 0, ["k"] = 0 },
            },
            new JsonObject
            {
              ["ty"] = "fl",
              ["c"] = new JsonObject { ["a"] = 0, ["k"] = new JsonArray(r, g, b, 1) },
              ["o"] = new JsonObject { ["a"] = 0, ["k"] = 100 },
              ["r"] = 1,
              ["bm"] = 0,
            },
            new JsonObject
            {
              ["ty"] = "tr",
              ["p"] = new JsonObject { ["a"] = 0, ["k"] = new JsonArray(0, 0) },
              ["a"] = new JsonObject { ["a"] = 0, ["k"] = new JsonArray(0, 0) },
              ["s"] = new JsonObject { ["a"] = 0, ["k"] = new JsonArray(100, 100) },
              ["r"] = new JsonObject { ["a"] = 0, ["k"] = 0 },
              ["o"] = new JsonObject { ["a"] = 0, ["k"] = 100 },
              ["sk"] = new JsonObject { ["a"] = 0, ["k"] = 0 },
              ["sa"] = new JsonObject { ["a"] = 0, ["k"] = 0 },
            },
          },
        },
      },
      ["ip"] = 0,
      ["op"] = op,
      ["st"] = 0,
      ["bm"] = 0,
    };

    layers.Add(bgLayer);
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

  /// <summary>
  /// Renders <paramref name="input"/> (a Lottie JSON file) into an MP4 at
  /// <paramref name="output"/> by piping rlottie's raw BGRA frame buffers
  /// into ffmpeg's h264 encoder. Returns false on any failure so the
  /// caller can clean up a partial output.
  /// </summary>
  private static bool RunLottieConvert(string input, string output)
  {
    IntPtr anim = IntPtr.Zero;
    Process? proc = null;
    try
    {
      anim = RlottieNative.FromFile(input);
      if (anim == IntPtr.Zero) return false;

      RlottieNative.GetSize(anim, out var wU, out var hU);
      var width = (int)wU;
      var height = (int)hU;
      var frames = (int)RlottieNative.GetTotalFrame(anim);
      var fps = RlottieNative.GetFrameRate(anim);
      if (width <= 0 || height <= 0 || frames <= 0 || fps <= 0) return false;

      // Build ffmpeg args explicitly via ArgumentList so paths with spaces
      // don't need shell-quoting. yuv420p output is what most players
      // (and mpvpaper) expect; -crf 23 is a sensible visually-lossless
      // default that keeps file sizes small.
      var psi = new ProcessStartInfo("ffmpeg")
      {
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
      };
      foreach (var a in new[]
      {
        "-y", "-hide_banner", "-loglevel", "error",
        "-f", "rawvideo",
        "-pix_fmt", "bgra",
        "-s", $"{width}x{height}",
        "-r", fps.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
        "-i", "-",
        "-c:v", "libx264",
        "-pix_fmt", "yuv420p",
        "-preset", "veryfast",
        "-crf", "23",
        "-movflags", "+faststart",
        output,
      })
      {
        psi.ArgumentList.Add(a);
      }

      proc = Process.Start(psi);
      if (proc is null) return false;

      // Drain ffmpeg's stderr asynchronously so it can't deadlock on a
      // full pipe while we're busy writing frames to stdin.
      var stderrBuf = new System.Text.StringBuilder();
      proc.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderrBuf.AppendLine(e.Data); };
      proc.BeginErrorReadLine();

      // rlottie wants a uint32-per-pixel buffer; we keep one heap-pinned
      // buffer across all frames to avoid per-frame allocation churn.
      var bytesPerLine = (UIntPtr)(width * 4);
      var frameBytes = width * height * 4;
      var managed = new byte[frameBytes];
      var handle = GCHandle.Alloc(managed, GCHandleType.Pinned);
      try
      {
        var bufPtr = handle.AddrOfPinnedObject();
        var widthU = (UIntPtr)width;
        var heightU = (UIntPtr)height;
        var stdin = proc.StandardInput.BaseStream;
        for (var f = 0; f < frames; f++)
        {
          RlottieNative.Render(anim, (UIntPtr)f, bufPtr, widthU, heightU, bytesPerLine);
          stdin.Write(managed, 0, frameBytes);
        }
        stdin.Flush();
        stdin.Close();
      }
      finally
      {
        handle.Free();
      }

      proc.WaitForExit();
      if (proc.ExitCode != 0)
      {
        var err = stderrBuf.ToString().Trim();
        if (err.Length > 0)
          Console.WriteLine($"  [Lottie] ffmpeg: {err}");
        return false;
      }
      return File.Exists(output);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"  [Lottie] render error: {ex.Message}");
      return false;
    }
    finally
    {
      if (anim != IntPtr.Zero) RlottieNative.Destroy(anim);
      proc?.Dispose();
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
