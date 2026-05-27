using System.Diagnostics;

namespace ThemeEngine;

public static class NotificationService
{
    public static void Info(string title, string message)
        => Send(title, message, "dialog-information");

    public static void Success(string title, string message)
        => Send(title, message, "dialog-positive");

    public static void Error(string title, string message)
        => Send(title, message, "dialog-error");

    private static void Send(string title, string message, string icon)
    {
        try
        {
            var psi = new ProcessStartInfo("notify-send")
            {
                ArgumentList = { "-i", icon, title, message },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            Process.Start(psi)?.WaitForExit(3000);
        }
        catch
        {
            // notify-send not available — fall through silently
        }

        // Always log to console as well
        Console.WriteLine($"[{icon}] {title}: {message}");
    }
}

/// <summary>
/// A long-lived desktop notification that keeps itself open while a slow
/// operation is running and animates a 6-frame braille spinner so the user
/// can tell it's still alive. Dispose to close.
/// </summary>
public sealed class ProgressNotification : IDisposable
{
    // 6-dot braille cell with one dot missing; the "hole" rotates
    // clockwise around the 2x3 cell (TL, TR, MR, BR, BL, ML).
    private static readonly string[] Frames =
    [
        "\u283E", // ⠾ hole at top-left
        "\u2837", // ⠷ hole at top-right
        "\u282F", // ⠯ hole at middle-right
        "\u281F", // ⠟ hole at bottom-right
        "\u283B", // ⠻ hole at bottom-left
        "\u283D", // ⠽ hole at middle-left
    ];

    private const int FrameIntervalMs = 150;

    private readonly string title;
    private string baseMessage;
    private readonly string icon;
    private readonly CancellationTokenSource cts = new();
    private readonly Task loop;
    private uint id;

    private ProgressNotification(string title, string baseMessage, string icon)
    {
        this.title = title;
        this.baseMessage = baseMessage;
        this.icon = icon;

        // Send the initial notification synchronously so we capture its id
        // before kicking off the animation loop.
        id = SendFrame(Frames[0], replace: false);
        Console.WriteLine($"[{icon}] {title}: {baseMessage}");

        loop = Task.Run(AnimateAsync);
    }

    public static ProgressNotification Start(string title, string message, string icon = "dialog-information")
        => new(title, message, icon);

    /// <summary>
    /// Replace the message body shown next to the spinner. Safe to call from
    /// any thread; the next animation tick (within FrameIntervalMs) will pick
    /// up the new text. Useful for streaming progress (e.g. "Rendering ... 42%").
    /// </summary>
    public void UpdateMessage(string newMessage)
    {
        Interlocked.Exchange(ref baseMessage, newMessage);
    }

    private async Task AnimateAsync()
    {
        var i = 0;
        try
        {
            while (!cts.IsCancellationRequested)
            {
                await Task.Delay(FrameIntervalMs, cts.Token);
                i = (i + 1) % Frames.Length;
                SendFrame(Frames[i], replace: true);
            }
        }
        catch (OperationCanceledException)
        {
            // expected on Dispose
        }
        catch
        {
            // never let the animation task crash the program
        }
    }

    /// <summary>
    /// Sends (or replaces) the notification with the supplied spinner frame.
    /// Returns the notification id assigned by the server.
    /// </summary>
    private uint SendFrame(string frame, bool replace)
    {
        try
        {
            var psi = new ProcessStartInfo("notify-send")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            psi.ArgumentList.Add("-i");
            psi.ArgumentList.Add(icon);
            // -t 0 = never expire while we keep refreshing it
            psi.ArgumentList.Add("-t");
            psi.ArgumentList.Add("0");
            if (replace && id != 0)
            {
                psi.ArgumentList.Add("-r");
                psi.ArgumentList.Add(id.ToString());
            }
            else
            {
                // Only -p on the initial send so we get the id back
                psi.ArgumentList.Add("-p");
            }
            psi.ArgumentList.Add(title);
            psi.ArgumentList.Add($"{frame} {baseMessage}");

            using var p = Process.Start(psi);
            if (p is null) return id;
            // Read stdout before WaitForExit to avoid pipe-fill deadlocks.
            var stdout = p.StandardOutput.ReadToEnd();
            p.WaitForExit(2000);
            if (!replace && uint.TryParse(stdout.Trim(), out var parsed))
                return parsed;
            return id;
        }
        catch
        {
            return id;
        }
    }

    public void Dispose()
    {
        try { cts.Cancel(); } catch { /* ignore */ }
        try { loop.Wait(500); } catch { /* ignore */ }
        // Close the notification by sending one final replace with a very
        // short timeout. Without this dunst would keep the spinner frame
        // visible forever.
        try
        {
            if (id != 0)
            {
                var psi = new ProcessStartInfo("notify-send")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };
                psi.ArgumentList.Add("-i");
                psi.ArgumentList.Add(icon);
                psi.ArgumentList.Add("-r");
                psi.ArgumentList.Add(id.ToString());
                psi.ArgumentList.Add("-t");
                psi.ArgumentList.Add("1");
                psi.ArgumentList.Add(title);
                psi.ArgumentList.Add(baseMessage);
                using var p = Process.Start(psi);
                p?.WaitForExit(1000);
            }
        }
        catch
        {
            // best effort
        }
        cts.Dispose();
    }
}
