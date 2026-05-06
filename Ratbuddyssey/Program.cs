using Avalonia;
using System;
using System.Diagnostics;

namespace Ratbuddyssey
{
    internal static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            LoggingService.Initialize();
            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Fatal exception escaped Main: {0}", ex);
                Trace.Flush();
                throw;
            }
        }

        public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
