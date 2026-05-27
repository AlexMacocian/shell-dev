using System.Diagnostics;
using System.Globalization;

namespace ThemeEngine;

/// <summary>
/// Renders GLSL fragment shaders to MP4 videos via headless
/// <c>glslViewer</c>, then feeds the resulting files into the existing
/// mpvpaper-based wallpaper cycler.
/// </summary>
/// <remarks>
/// glslViewer is launched with <c>--headless</c> so no X/Wayland surface
/// is required. Before rendering, the shader source has placeholders
/// substituted: <c>${BG_R}</c>/<c>${BG_G}</c>/<c>${BG_B}</c> for the
/// background, <c>${ACCENT_R}</c>/<c>${ACCENT_G}</c>/<c>${ACCENT_B}</c>
/// for the accent (plus convenience tokens <c>${BG}</c> / <c>${ACCENT}</c>
/// that expand to <c>vec3(r, g, b)</c>), and <c>${LOOP_SECONDS}</c> with
/// the recording duration so shaders can wrap motion to a seamless loop.
/// Shaders without placeholders pass through unchanged. The substituted
/// source is written to a temp file next to the cached MP4 so glslViewer
/// renders the themed variant. Outputs are cached under
/// <c>$XDG_CACHE_HOME/shell-dev/glsl-cache/&lt;stem&gt;-&lt;bg&gt;-&lt;accent&gt;-&lt;w&gt;x&lt;h&gt;-&lt;fps&gt;-&lt;dur&gt;s.mp4</c>
/// so repeated applies are instant.
/// </remarks>
public static class GlslConverter
{
  private const string Tool = "glslViewer";

  /// <summary>Tokens recognised in shader sources before render.</summary>
  private static readonly string[] Placeholders =
      ["${BG_R}", "${BG_G}", "${BG_B}", "${ACCENT_R}", "${ACCENT_G}", "${ACCENT_B}", "${BG}", "${ACCENT}", "${LOOP_SECONDS}"];

  /// <summary>
  /// Absolute path to the persistent shader render cache. Lives outside
  /// the repo so the large generated MP4s aren't accidentally committed
  /// when <c>themes/</c> is symlinked into the wallpapers dir.
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
      return Path.Combine(xdg, "shell-dev", "glsl-cache");
    }
  }

  public static string[] Convert(ShaderEntry[] shaders, string wallpapersDir, string bgHex, string accentHex)
  {
    if (shaders.Length == 0) return [];

    if (!IsToolAvailable(Tool))
    {
      Console.WriteLine(
          $"  [Shader] WARNING: '{Tool}' not found on PATH. Install with " +
          "'paru -S glslviewer' to enable shader wallpapers.");
      return [];
    }

    var bg = NormalizeHex(bgHex);
    var accent = NormalizeHex(accentHex);
    var (br, bgGreen, bb) = HexToFloatRgb(bgHex);
    var (ar, ag, ab) = HexToFloatRgb(accentHex);

    var cacheDir = CacheDir;
    Directory.CreateDirectory(cacheDir);

    var outputs = new List<string>();
    foreach (var entry in shaders)
    {
      var abs = Path.Combine(wallpapersDir, entry.Path);
      if (!File.Exists(abs))
      {
        Console.WriteLine($"  [Shader] WARNING: source not found: {entry.Path}");
        continue;
      }

      var source = File.ReadAllText(abs);
      var hasPlaceholders = Placeholders.Any(source.Contains);

      var stem = Path.GetFileNameWithoutExtension(entry.Path);
      var cacheName = hasPlaceholders
          ? $"{stem}-{bg}-{accent}-{entry.Width}x{entry.Height}-{entry.Fps}-{entry.DurationSeconds}s.mp4"
          : $"{stem}-{entry.Width}x{entry.Height}-{entry.Fps}-{entry.DurationSeconds}s.mp4";
      var mp4Abs = Path.Combine(cacheDir, cacheName);

      if (File.Exists(mp4Abs))
      {
        Console.WriteLine($"  [Shader] cache hit {entry.Path} -> {mp4Abs}");
        outputs.Add(mp4Abs);
        continue;
      }

      // If the shader has placeholders, write a substituted copy next to
      // the target mp4 so glslViewer renders the themed variant. The
      // sibling .frag is kept around for debugging and re-renders.
      string shaderToRender;
      if (hasPlaceholders)
      {
        var substituted = SubstituteColors(source, br, bgGreen, bb, ar, ag, ab, entry.DurationSeconds);
        shaderToRender = Path.ChangeExtension(mp4Abs, ".frag");
        File.WriteAllText(shaderToRender, substituted);
      }
      else
      {
        shaderToRender = abs;
      }

      Console.WriteLine($"  [Shader] rendering {entry.Path} -> {mp4Abs}");
      using var progress = ProgressNotification.Start(
          "Theme Engine",
          $"Rendering shader {stem} (this may take some time)...");

      // glslViewer writes a near-lossless mp4 (huge: ~300MB for 1080p60s).
      // Render to a temp file, then re-encode with x264 CRF to shrink
      // by ~10x. If ffmpeg isn't available we fall back to the raw output.
      var rawPath = mp4Abs + ".raw.mp4";
      if (File.Exists(rawPath)) { try { File.Delete(rawPath); } catch { } }

      if (!RunGlslViewer(shaderToRender, rawPath, entry, progress))
      {
        if (File.Exists(rawPath)) { try { File.Delete(rawPath); } catch { } }
        Console.WriteLine($"  [Shader] WARNING: failed to render {entry.Path}");
        continue;
      }

      progress.UpdateMessage($"Compressing {stem}...");
      if (!CompressMp4(rawPath, mp4Abs, entry.DurationSeconds, progress, stem))
      {
        // Compression failed: keep the raw file under the cache name so
        // we still get a working wallpaper, just larger.
        try { File.Move(rawPath, mp4Abs, overwrite: true); } catch { }
      }
      else
      {
        try { File.Delete(rawPath); } catch { }
      }

      outputs.Add(mp4Abs);
    }
    return [.. outputs];
  }

  /// <summary>
  /// Expand <c>${BG_*}</c>/<c>${ACCENT_*}</c>/<c>${BG}</c>/<c>${ACCENT}</c>
  /// tokens in a shader source. Floats use invariant culture so the
  /// decimal separator is always <c>.</c>.
  /// </summary>
  private static string SubstituteColors(
      string source,
      float br, float bg, float bb,
      float ar, float ag, float ab,
      int loopSeconds)
  {
    // Always include a decimal point so GLSL treats the literal as a
    // float (otherwise `tile * 2.0 / ${LOOP_SECONDS}` becomes a mixed
    // float/int expression and fails to compile).
    string F(float v)
    {
      var s = v.ToString("0.######", CultureInfo.InvariantCulture);
      return s.Contains('.') ? s : s + ".0";
    }
    return source
        .Replace("${BG_R}", F(br))
        .Replace("${BG_G}", F(bg))
        .Replace("${BG_B}", F(bb))
        .Replace("${ACCENT_R}", F(ar))
        .Replace("${ACCENT_G}", F(ag))
        .Replace("${ACCENT_B}", F(ab))
        .Replace("${BG}", $"vec3({F(br)}, {F(bg)}, {F(bb)})")
        .Replace("${ACCENT}", $"vec3({F(ar)}, {F(ag)}, {F(ab)})")
        .Replace("${LOOP_SECONDS}", F(loopSeconds));
  }

  /// <summary>
  /// Drives glslViewer in headless mode: passes window size, fps and a
  /// queued <c>record</c> command followed by <c>q</c> so the process
  /// exits once recording completes. glslViewer's built-in recorder
  /// shells out to ffmpeg internally and writes h264 mp4.
  /// </summary>
  private static bool RunGlslViewer(
      string shaderPath,
      string outputPath,
      ShaderEntry entry,
      ProgressNotification? progress = null)
  {
    try
    {
      var psi = new ProcessStartInfo(Tool)
      {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
      };

      void Add(string s) => psi.ArgumentList.Add(s);
      Add("--headless");
      Add("--noncurses");
      Add("-w"); Add(entry.Width.ToString(CultureInfo.InvariantCulture));
      Add("-h"); Add(entry.Height.ToString(CultureInfo.InvariantCulture));
      Add("-r"); Add(entry.Fps.ToString(CultureInfo.InvariantCulture));

      // -E queues a console command that runs after the GL context is
      // ready. record,<file>,<from>,<to>,<fps> writes mp4 via the
      // bundled ffmpeg encoder; q quits glslViewer once recording is
      // done so the process actually exits.
      Add("-E");
      Add($"record,{outputPath},0,{entry.DurationSeconds.ToString(CultureInfo.InvariantCulture)},{entry.Fps.ToString(CultureInfo.InvariantCulture)}");
      Add("-E"); Add("q");

      Add(shaderPath);

      using var proc = Process.Start(psi);
      if (proc is null) return false;

      var stderrBuf = new System.Text.StringBuilder();
      proc.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderrBuf.AppendLine(e.Data); };
      proc.BeginErrorReadLine();
      // glslViewer streams recording progress on stdout as lines like
      //   // [ ##########........ ] 22%
      // Match ONLY that exact bar pattern — other lines may contain
      // unrelated "%" values (per-frame stats, ETA, etc) which would
      // otherwise interleave and make the percentage jump around.
      var stem = Path.GetFileNameWithoutExtension(shaderPath);
      var lastPct = -1;
      var progressRe = new System.Text.RegularExpressions.Regex(
          @"\[\s*[#.\s]+\]\s*(\d+)\s*%",
          System.Text.RegularExpressions.RegexOptions.Compiled);
      proc.OutputDataReceived += (_, e) =>
      {
        if (e.Data is null || progress is null) return;
        var m = progressRe.Match(e.Data);
        if (!m.Success) return;
        if (!int.TryParse(m.Groups[1].Value, out var pct)) return;
        // Drop out-of-order updates so the displayed % is monotonic.
        if (pct <= lastPct) return;
        lastPct = pct;
        progress.UpdateMessage($"Rendering {stem}... {pct}%");
      };
      proc.BeginOutputReadLine();

      proc.WaitForExit();
      if (proc.ExitCode != 0)
      {
        var err = stderrBuf.ToString().Trim();
        if (err.Length > 0)
          Console.WriteLine($"  [Shader] glslViewer: {err}");
        return false;
      }
      return File.Exists(outputPath);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"  [Shader] render error: {ex.Message}");
      return false;
    }
  }

  /// <summary>
  /// Re-encode the raw glslViewer mp4 with libx264 CRF to dramatically
  /// shrink file size (typically ~10x) while staying visually lossless
  /// for wallpaper use. Returns false if ffmpeg is missing or fails.
  /// </summary>
  private static bool CompressMp4(
      string srcPath,
      string dstPath,
      double totalSeconds = 0,
      ProgressNotification? progress = null,
      string? stem = null)
  {
    if (!IsToolAvailable("ffmpeg"))
    {
      Console.WriteLine("  [Shader] ffmpeg not found, keeping raw mp4");
      return false;
    }
    try
    {
      var psi = new ProcessStartInfo("ffmpeg")
      {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
      };
      void Add(string s) => psi.ArgumentList.Add(s);
      Add("-y");
      Add("-hide_banner");
      Add("-loglevel"); Add("error");
      // -progress writes key=value stats to stdout each second (e.g.
      // out_time_us=1234567, progress=continue|end). We parse it to drive
      // the live percent in the desktop notification.
      Add("-progress"); Add("pipe:1");
      Add("-nostats");
      Add("-i"); Add(srcPath);
      Add("-c:v"); Add("libx264");
      Add("-preset"); Add("slow");
      Add("-crf"); Add("28");
      Add("-pix_fmt"); Add("yuv420p");
      Add("-movflags"); Add("+faststart");
      Add("-an");
      Add(dstPath);

      using var proc = Process.Start(psi);
      if (proc is null) return false;
      var stderrBuf = new System.Text.StringBuilder();
      proc.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderrBuf.AppendLine(e.Data); };
      proc.BeginErrorReadLine();

      var lastPct = -1;
      var totalUs = totalSeconds > 0 ? totalSeconds * 1_000_000.0 : 0.0;
      proc.OutputDataReceived += (_, e) =>
      {
        if (e.Data is null || progress is null || totalUs <= 0) return;
        // Lines look like "out_time_us=12345678" or "out_time_ms=12345"
        // (older builds). Match either.
        var idx = e.Data.IndexOf('=');
        if (idx <= 0) return;
        var key = e.Data.AsSpan(0, idx);
        var val = e.Data.AsSpan(idx + 1);
        double us;
        if (key.SequenceEqual("out_time_us") || key.SequenceEqual("out_time_ms"))
        {
          if (!long.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)) return;
          // ffmpeg's "out_time_ms" is actually microseconds in modern builds.
          us = n;
        }
        else return;
        var pct = (int)Math.Clamp(us * 100.0 / totalUs, 0, 100);
        if (pct <= lastPct) return;
        lastPct = pct;
        progress.UpdateMessage($"Compressing {stem ?? "video"}... {pct}%");
      };
      proc.BeginOutputReadLine();
      proc.WaitForExit();
      if (proc.ExitCode != 0)
      {
        Console.WriteLine($"  [Shader] ffmpeg: {stderrBuf.ToString().Trim()}");
        return false;
      }
      return File.Exists(dstPath);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"  [Shader] compress error: {ex.Message}");
      return false;
    }
  }

  private static string NormalizeHex(string hex) =>
      (hex.StartsWith('#') ? hex[1..] : hex).ToLowerInvariant();

  private static (float r, float g, float b) HexToFloatRgb(string hex)
  {
    var h = hex.StartsWith('#') ? hex[1..] : hex;
    var r = int.Parse(h[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255f;
    var g = int.Parse(h.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255f;
    var b = int.Parse(h.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255f;
    return (r, g, b);
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
