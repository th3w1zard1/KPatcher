// Copyright (c) 2021-2025 NCSDecomp contributors / KPatcher
// DeNCS-derived portions: MIT License (see NOTICE and licenses/DeNCS-MIT.txt).

using System;
using Avalonia;
using KCompiler.Diagnostics;
using Microsoft.Extensions.Logging;

namespace NCSDecomp.UI
{
    internal static class Program
    {
        internal static ILoggerFactory ToolLogFactory { get; private set; }

        [STAThread]
        public static void Main(string[] args)
        {
            ToolLogFactory = LoggerFactory.Create(b =>
            {
                b.SetMinimumLevel(ToolLogLevel.DefaultMinimumFromEnvironment());
                ToolHostLogging.AddFileSinkIfConfigured(b);
                ToolHostLogging.AddSimpleConsoleToStderr(b);
            });
            ToolHostLogging.LogHostStartupDebug(ToolLogFactory.CreateLogger("NCSDecomp.UI.boot"), "NCSDecomp.UI", args);

            try
            {
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
            finally
            {
                try
                {
                    if (ToolLogFactory != null)
                    {
                        ILogger shutdownLog = ToolLogFactory.CreateLogger("NCSDecomp.UI.shutdown");
                        if (shutdownLog.IsEnabled(LogLevel.Debug))
                        {
                            shutdownLog.LogDebug(
                                "Tool=NCSDecomp.UI Phase=host.shutdown CorrelationId={CorrelationId} Message=disposing log factory",
                                ToolCorrelation.ReadOptional() ?? string.Empty);
                        }
                    }
                }
                catch
                {
                    // Best-effort diagnostic; never block process exit.
                }

                ToolLogFactory?.Dispose();
                ToolLogFactory = null;
            }
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
        }
    }
}
