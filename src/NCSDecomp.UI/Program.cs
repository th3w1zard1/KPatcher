// Copyright (c) 2021-2025 DeNCS contributors / KPatcher
// DeNCS-derived portions: MIT License (see NOTICE and licenses/DeNCS-MIT.txt).

using System;
using Avalonia;

namespace NCSDecomp.UI
{
    internal static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
        }
    }
}
