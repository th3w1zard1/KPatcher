// Copyright 2021-2025 NCSDecomp / KPatcher

using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using KCompiler.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace NCSDecomp.UI
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            ILogger log = Program.ToolLogFactory?.CreateLogger("NCSDecomp.UI.App") ?? NullLogger.Instance;
            if (log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug(
                    "Tool=NCSDecomp.UI Phase=avalonia.framework_init CorrelationId={CorrelationId} LifetimeType={Lifetime}",
                    ToolCorrelation.ReadOptional() ?? string.Empty,
                    ApplicationLifetime?.GetType().Name ?? "(null)");
            }

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var main = new MainWindow();
                desktop.MainWindow = main;
                if (desktop.Args != null && desktop.Args.Length > 0)
                {
                    for (int i = 0; i < desktop.Args.Length; i++)
                    {
                        string arg = desktop.Args[i];
                        if (string.IsNullOrWhiteSpace(arg))
                        {
                            continue;
                        }

                        if (string.Equals(arg, "-i", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(arg, "--input", StringComparison.OrdinalIgnoreCase))
                        {
                            if (i + 1 < desktop.Args.Length)
                            {
                                main.LoadNcsFromPath(desktop.Args[i + 1], true);
                            }

                            break;
                        }

                        if (arg.Length > 0 && arg[0] != '-')
                        {
                            main.LoadNcsFromPath(arg, true);
                            break;
                        }
                    }
                }
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
