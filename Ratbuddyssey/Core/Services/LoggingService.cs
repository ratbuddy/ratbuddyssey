using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Ratbuddyssey;

/// <summary>
/// Bootstraps a process-wide log file (<c>ratbuddyssey.log</c> in the app
/// directory) and hooks every global exception channel — AppDomain unhandled
/// exceptions, unobserved Task exceptions, and first-chance exceptions — so a
/// crash report is on disk before the process unwinds.
/// </summary>
internal static class LoggingService
{
    private const string LogFileName = "ratbuddyssey.log";
    private const long MaxLogBytes = 5L * 1024 * 1024; // 5 MiB; rotated on startup
    private static bool _initialized;
    private static string _logPath;

    public static string LogPath => _logPath;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        try
        {
            _logPath = Path.Combine(AppContext.BaseDirectory, LogFileName);
            RotateIfTooLarge(_logPath);

            var stream = new FileStream(_logPath, FileMode.Append, FileAccess.Write, FileShare.Read);
            var writer = new StreamWriter(stream) { AutoFlush = true };
            var listener = new TimestampedTraceListener(writer) { Name = "ratbuddyssey-file" };

            Trace.Listeners.Add(listener);
            Trace.AutoFlush = true;
            // Intentionally NOT logging Environment.CurrentDirectory or any other
            // path: cwd commonly resolves to the user profile, and the log file
            // travels with bug reports. Process id is enough to correlate runs.
            Trace.TraceInformation("=== Ratbuddyssey starting (pid {0}) ===", Environment.ProcessId);
        }
        catch (Exception ex)
        {
            // If we can't open the file we still want global handlers installed,
            // but there's no point throwing — fall back to whatever listeners
            // are already attached (Avalonia's LogToTrace etc.).
            Debug.WriteLine($"LoggingService.Initialize: file listener failed: {ex.Message}");
        }

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Trace.TraceError("AppDomain.UnhandledException (terminating={0}): {1}",
                e.IsTerminating, e.ExceptionObject);
            Trace.Flush();
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Trace.TraceError("TaskScheduler.UnobservedTaskException: {0}", e.Exception);
            Trace.Flush();
            e.SetObserved();
        };

        // First-chance is noisy in normal flow but crucial for diagnosing
        // crashes that get swallowed by Avalonia's dispatcher; cap at WARNING
        // level so it's visible without overwhelming the file.
        AppDomain.CurrentDomain.FirstChanceException += (_, e) =>
        {
            // Skip the framework's own benign exception types we know are noise.
            var type = e.Exception.GetType().FullName;
            if (type == "System.Threading.Tasks.TaskCanceledException"
                || type == "System.OperationCanceledException")
                return;
            // Log only the exception type at first-chance. Messages frequently
            // embed full file paths, URLs, or user-supplied strings; the full
            // detail is still captured if the exception escapes (UnhandledException
            // / UnobservedTaskException). For deeper diagnostics, opt in via
            // the RATBUDDYSSEY_TRACE_FIRSTCHANCE_DETAIL environment variable.
            if (Environment.GetEnvironmentVariable("RATBUDDYSSEY_TRACE_FIRSTCHANCE_DETAIL") == "1")
            {
                Trace.TraceWarning("FirstChance {0}: {1}", type, e.Exception.Message);
            }
            else
            {
                Trace.TraceWarning("FirstChance {0}", type);
            }
        };
    }

    private static void RotateIfTooLarge(string path)
    {
        try
        {
            var fi = new FileInfo(path);
            if (fi.Exists && fi.Length > MaxLogBytes)
            {
                string rotated = path + ".old";
                if (File.Exists(rotated)) File.Delete(rotated);
                File.Move(path, rotated);
            }
        }
        catch
        {
            // Best-effort; rotation failure shouldn't stop the app.
        }
    }

    /// <summary>
    /// <see cref="TextWriterTraceListener"/> with timestamps + log levels
    /// prepended via <c>TraceEvent</c>.
    /// </summary>
    private sealed class TimestampedTraceListener : TextWriterTraceListener
    {
        public TimestampedTraceListener(TextWriter writer) : base(writer) { }

        public override void TraceEvent(TraceEventCache eventCache, string source,
            TraceEventType eventType, int id, string message)
            => Write(FormatLine(eventType, message));

        public override void TraceEvent(TraceEventCache eventCache, string source,
            TraceEventType eventType, int id, string format, params object[] args)
            => Write(FormatLine(eventType, format == null ? string.Empty
                : string.Format(System.Globalization.CultureInfo.InvariantCulture, format, args)));

        public override void WriteLine(string message)
            => base.WriteLine(FormatLine(TraceEventType.Information, message).TrimEnd());

        private static string FormatLine(TraceEventType type, string message)
            => string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] {2}{3}",
                DateTime.Now, type, message, Environment.NewLine);
    }
}
