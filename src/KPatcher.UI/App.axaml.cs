using System;
using System.Globalization;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using KPatcher.Core.Resources;
using KPatcher.UI.Resources;
using KPatcher.UI.ViewModels;
using KPatcher.UI.Views;

namespace KPatcher.UI
{

    public partial class App : Application
    {
        private UpdateManager _updateManager;
        private bool _cleanupRegistered = false;
        private bool _cleanupExecuted = false;
        private readonly object _cleanupLock = new object();

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

            // Register process exit handler early to catch forced terminations
            // This provides a fallback if ShutdownRequested doesn't fire
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // Set UI and thread culture: saved preference first, then OS, then fallback to English
            string[] supported = LanguageSettings.GetSupportedCodes();
            CultureInfo culture;
            string saved = LanguageSettings.GetSavedLanguage();
            if (!string.IsNullOrEmpty(saved))
            {
                culture = new CultureInfo(saved);
            }
            else
            {
                CultureInfo ui = CultureInfo.CurrentUICulture;
                string twoLetter = ui.TwoLetterISOLanguageName;
                culture = Array.IndexOf(supported, twoLetter) >= 0 ? ui : new CultureInfo("en");
            }

            PatcherResources.Culture = culture;
            UIResources.Culture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                string title = string.Format(culture, UIResources.WindowTitleFormat, Core.VersionLabel);
                if (Core.IsAlphaVersionOrLowerThanV1_0_0(Core.VersionLabel))
                {
                    title += UIResources.WindowTitleAlphaSuffix;
                }
                desktop.MainWindow = new MainWindow
                {
                    Title = title,
                    DataContext = new MainWindowViewModel(),
                };

                // Initialize and start update manager on UI thread
                InitializeUpdateManager();

                // Register cleanup handler for normal shutdown
                // This is the primary cleanup path for graceful shutdown
                if (!_cleanupRegistered)
                {
                    desktop.ShutdownRequested += OnShutdownRequested;
                    _cleanupRegistered = true;
                }
            }

            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>
        /// Applies a new UI language: saves preference, sets resource/thread culture, and replaces the main window
        /// so all bindings re-evaluate with the new culture. Call from Language menu command.
        /// </summary>
        public static void RequestLanguageChange(string twoLetterCode)
        {
            if (string.IsNullOrEmpty(twoLetterCode))
            {
                return;
            }

            string[] supported = LanguageSettings.GetSupportedCodes();
            if (Array.IndexOf(supported, twoLetterCode) < 0)
            {
                return;
            }

            LanguageSettings.SaveLanguage(twoLetterCode);
            CultureInfo culture = new CultureInfo(twoLetterCode);

            PatcherResources.Culture = culture;
            UIResources.Culture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                Window oldWindow = desktop.MainWindow;
                if (oldWindow?.DataContext is MainWindowViewModel oldVm && !string.IsNullOrEmpty(oldVm.ModPath))
                {
                    Core.LastLoadedModPathForLanguageChange = oldVm.ModPath;
                }
                string title = string.Format(culture, UIResources.WindowTitleFormat, Core.VersionLabel);
                if (Core.IsAlphaVersionOrLowerThanV1_0_0(Core.VersionLabel))
                {
                    title += UIResources.WindowTitleAlphaSuffix;
                }
                desktop.MainWindow = new MainWindow
                {
                    Title = title,
                    DataContext = new MainWindowViewModel(),
                };
                oldWindow?.Close();
            }
        }

        private void OnShutdownRequested(object sender, ShutdownRequestedEventArgs e)
        {
            CleanupUpdateManager();
        }

        private void OnProcessExit(object sender, EventArgs e)
        {
            // Fallback cleanup for forced termination or crashes
            // This ensures cleanup even if ShutdownRequested doesn't fire
            CleanupUpdateManager();
        }

        private void InitializeUpdateManager()
        {
            try
            {
                _updateManager = new UpdateManager
                {
                    CheckOnStartup = true,
                    SilentCheck = true,
                    UseBetaChannel = false // Set to true to use beta channel
                };

                // Start update checking
                _updateManager.Start();
            }
            catch (System.Exception ex)
            {
                // Log error but don't crash the app if update system fails
                System.Diagnostics.Debug.WriteLine($"Failed to initialize update manager: {ex.Message}");
            }
        }

        /// <summary>
        /// Safely disposes the update manager. This method is idempotent and thread-safe.
        /// Called from multiple cleanup paths to ensure reliable resource disposal.
        /// Uses double-checked locking pattern to prevent race conditions.
        /// </summary>
        private void CleanupUpdateManager()
        {
            // Early exit if already cleaned up (fast path)
            if (_cleanupExecuted)
            {
                return;
            }

            lock (_cleanupLock)
            {
                // Double-check after acquiring lock
                if (_cleanupExecuted || _updateManager is null)
                {
                    return;
                }

                try
                {
                    _updateManager.Dispose();
                }
                catch (Exception ex)
                {
                    // Log but don't throw - cleanup should never crash the app
                    System.Diagnostics.Debug.WriteLine($"Error disposing update manager: {ex.Message}");
                }
                finally
                {
                    _updateManager = null;
                    _cleanupExecuted = true;
                }
            }
        }
    }
}

