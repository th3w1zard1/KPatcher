// Copyright 2021-2025 NCSDecomp / KPatcher

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
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
