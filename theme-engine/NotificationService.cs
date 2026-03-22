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
