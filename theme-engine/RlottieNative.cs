using System.Runtime.InteropServices;

/// <summary>
/// Thin P/Invoke wrapper around <c>librlottie.so</c>'s C ABI. Used by
/// <see cref="LottieConverter"/> to render Lottie animations frame-by-frame
/// into raw BGRA buffers which are then piped into ffmpeg for h264 encoding.
/// </summary>
/// <remarks>
/// rlottie renders into ARGB32-premultiplied buffers. On little-endian x86
/// the in-memory byte order is B, G, R, A — i.e. ffmpeg's <c>bgra</c>
/// pixel format. Because the theme engine injects an opaque background
/// shape layer before rendering, every pixel ends up with alpha=255 so
/// premultiplied vs. straight alpha is a no-op and the buffer can be fed
/// to ffmpeg as-is.
/// </remarks>
internal static class RlottieNative
{
  private const string Lib = "rlottie";

  [DllImport(Lib, EntryPoint = "lottie_animation_from_file", CharSet = CharSet.Ansi)]
  public static extern IntPtr FromFile(string path);

  [DllImport(Lib, EntryPoint = "lottie_animation_destroy")]
  public static extern void Destroy(IntPtr animation);

  [DllImport(Lib, EntryPoint = "lottie_animation_get_size")]
  public static extern void GetSize(IntPtr animation, out UIntPtr width, out UIntPtr height);

  [DllImport(Lib, EntryPoint = "lottie_animation_get_totalframe")]
  public static extern UIntPtr GetTotalFrame(IntPtr animation);

  [DllImport(Lib, EntryPoint = "lottie_animation_get_framerate")]
  public static extern double GetFrameRate(IntPtr animation);

  [DllImport(Lib, EntryPoint = "lottie_animation_render")]
  public static extern void Render(
      IntPtr animation,
      UIntPtr frameNum,
      IntPtr buffer,
      UIntPtr width,
      UIntPtr height,
      UIntPtr bytesPerLine);
}
