using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TaskDockr.Models;
using TaskDockr.Services;

namespace TaskDockr
{
    public partial class App : Application
    {
        [DllImport("user32.dll")] static extern bool GetCursorPos(out CursorPoint lp);
        [DllImport("user32.dll")] static extern uint GetDpiForSystem();
        struct CursorPoint { public int X; public int Y; }

        private static Mutex? _mutex;

        /// <summary>DPI scale factor (e.g. 1.25 for 125%).</summary>
        private static double GetDpiScale()
        {
            try { uint dpi = GetDpiForSystem(); if (dpi > 0) return dpi / 96.0; } catch { }
            return 1.0;
        }

        /// <summary>
        /// Dynamic popup height: header + shortcut grid rows + padding.
        /// Matches PopoutService.CalculateWindowHeight logic.
        /// </summary>
        private static double CalculatePopupHeight(int shortcutCount)
        {
            const double headerHeight = 36;
            const double gridPadding = 16;
            const double tileHeight = 86;   // 78 + 8 margin
            const int tilesPerRow = 3;
            const double minContentHeight = 86;
            const double maxWindowHeight = 500;

            int rows = shortcutCount > 0
                ? (int)Math.Ceiling((double)shortcutCount / tilesPerRow)
                : 0;
            double contentHeight = Math.Max(rows * tileHeight, minContentHeight);
            return Math.Min(headerHeight + contentHeight + gridPadding, maxWindowHeight);
        }
        private IServiceProvider? _serviceProvider;

        internal static void LogCrash(string context, Exception ex)
        {
            try
            {
                var logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TaskDockr");
                Directory.CreateDirectory(logDir);
                var logPath = Path.Combine(logDir, "crash.log");
                var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] CRASH in {context}\n" +
                            $"  Type: {ex.GetType().FullName}\n" +
                            $"  Message: {ex.Message}\n" +
                            $"  InnerException: {ex.InnerException?.Message}\n" +
                            $"  StackTrace:\n{ex.StackTrace}\n" +
                            new string('-', 80) + "\n";
                File.AppendAllText(logPath, entry);
            }
            catch { /* logging must never throw */ }
        }

        private static void WriteBreadcrumb(string message)
        {
            try
            {
                var path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TaskDockr", "startup.log");
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.AppendAllText(path, $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
            catch { }
        }

        public App()
        {
            WriteBreadcrumb("App() constructor entered");
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    LogCrash("AppDomain.UnhandledException", ex);
            };

            // Setup DI
            try
            {
                var services = new ServiceCollection();
                services.AddTaskDockrServices();
                _serviceProvider = services.BuildServiceProvider();
            }
            catch (Exception ex)
            {
                LogCrash("App DI setup", ex);
                throw;
            }
        }

        public static T GetService<T>() where T : class
        {
            if ((Current as App)?._serviceProvider == null)
                throw new InvalidOperationException("Service provider is not available");
            return (Current as App)!._serviceProvider!.GetRequiredService<T>();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                await OnStartupCoreAsync(e);
            }
            catch (Exception ex)
            {
                LogCrash("App.OnStartup", ex);
                MessageBox.Show($"TaskDockr failed to start:\n{ex.Message}", "Startup Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        private async Task OnStartupCoreAsync(StartupEventArgs e)
        {
            // Initialize FontAwesome6 fonts for icon rendering
            FontAwesome6.Fonts.FontAwesomeFonts.LoadAllStyles(
                new Uri("pack://application:,,,/FontAwesome6.Fonts.Net;component/Fonts/"));

            var configService = _serviceProvider!.GetRequiredService<IConfigurationService>();
            var config        = await configService.LoadConfigAsync();

            // Always dark
            Wpf.Ui.Appearance.ApplicationThemeManager.Apply(Wpf.Ui.Appearance.ApplicationTheme.Dark);

            // Check if launched from a pinned group shortcut: --group {id}
            string? groupId = null;
            var args = e.Args;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--group") { groupId = args[i + 1]; break; }
            }

            if (groupId != null)
            {
                var group = config.Groups?.FirstOrDefault(g => g.Id == groupId);
                if (group != null)
                {
                    var popup = new Views.PopoutWindow(group);
                    popup.Closed += (_, _) => Shutdown(0);

                    const double W = 310, Gap = 8;
                    // Dynamic height based on shortcut count
                    int shortcutCount = group.Shortcuts?.Count ?? 0;
                    double H = CalculatePopupHeight(shortcutCount);

                    var work   = System.Windows.SystemParameters.WorkArea;
                    var screen = new System.Windows.Size(
                        System.Windows.SystemParameters.PrimaryScreenWidth,
                        System.Windows.SystemParameters.PrimaryScreenHeight);

                    // Cursor position (physical pixels) — convert to DIPs
                    GetCursorPos(out var cur);
                    double dpiScale = GetDpiScale();
                    double cursorX = cur.X / dpiScale;
                    double cursorY = cur.Y / dpiScale;

                    // Which edge is the taskbar on?
                    double left, top;
                    if (work.Bottom < screen.Height)        // taskbar at bottom
                    {
                        top  = work.Bottom - H - Gap;
                        left = Math.Max(work.Left + Gap,
                               Math.Min(cursorX - W / 2, work.Right - W - Gap));
                    }
                    else if (work.Top > 0)                  // taskbar at top
                    {
                        top  = work.Top + Gap;
                        left = Math.Max(work.Left + Gap,
                               Math.Min(cursorX - W / 2, work.Right - W - Gap));
                    }
                    else if (work.Left > 0)                 // taskbar on left
                    {
                        left = work.Left + Gap;
                        top  = Math.Max(work.Top + Gap,
                               Math.Min(cursorY - H / 2, work.Bottom - H - Gap));
                    }
                    else                                    // taskbar on right
                    {
                        left = work.Right - W - Gap;
                        top  = Math.Max(work.Top + Gap,
                               Math.Min(cursorY - H / 2, work.Bottom - H - Gap));
                    }

                    popup.Width  = W;
                    popup.Height = H;
                    popup.Left   = left;
                    popup.Top    = top;
                    popup.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;
                    popup.Show();
                }
                else
                {
                    Shutdown(0);
                }
                return;
            }

            // Normal startup — single-instance enforcement
            if (config.Startup.SingleInstanceEnforced)
            {
                const string mutexName = "TaskDockr_SingleInstance_Mutex";
                _mutex = new Mutex(true, mutexName, out bool createdNew);
                if (!createdNew) { Shutdown(0); return; }
            }

            await InitializeStartupServicesAsync(config);

            var mainWindow    = new MainWindow();
            var windowManager = _serviceProvider.GetRequiredService<IWindowManagerService>();
            windowManager.ApplyWindowSettings(mainWindow, config.WindowSettings);
            mainWindow.Closed += async (_, _) => await OnMainWindowClosedAsync(mainWindow);
            mainWindow.Show();
            WriteBreadcrumb("MainWindow shown");
        }

        private void ApplyTheme(AppConfig config)
        {
            var theme = config.Theme switch
            {
                ThemePreference.Light => Wpf.Ui.Appearance.ApplicationTheme.Light,
                ThemePreference.Dark => Wpf.Ui.Appearance.ApplicationTheme.Dark,
                ThemePreference.Auto => Wpf.Ui.Appearance.ApplicationThemeManager.GetSystemTheme() switch
                {
                    Wpf.Ui.Appearance.SystemTheme.Light => Wpf.Ui.Appearance.ApplicationTheme.Light,
                    Wpf.Ui.Appearance.SystemTheme.Glow => Wpf.Ui.Appearance.ApplicationTheme.Light,
                    _ => Wpf.Ui.Appearance.ApplicationTheme.Dark
                },
                _ => Wpf.Ui.Appearance.ApplicationTheme.Dark
            };
            
            // Ensure theme is applied on UI thread
            Dispatcher.Invoke(() =>
            {
                Wpf.Ui.Appearance.ApplicationThemeManager.Apply(theme);
            });
        }

        private async Task InitializeStartupServicesAsync(AppConfig config)
        {
            try
            {
                var startupService            = _serviceProvider!.GetRequiredService<IStartupService>();
                var taskbarIntegrationService = _serviceProvider.GetRequiredService<ITaskbarIntegrationService>();

                if (config.Startup.BackgroundOperationsEnabled)
                    await startupService.InitializeBackgroundOperationsAsync();

                if (config.Startup.MinimizeToTray)
                    await taskbarIntegrationService.InitializeTrayIconAsync();

                if (config.Startup.PerformanceMonitoringEnabled)
                    await startupService.StartPerformanceMonitoringAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize startup services: {ex.Message}");
            }
        }

        private async Task OnMainWindowClosedAsync(MainWindow window)
        {
            try
            {
                var configService  = _serviceProvider!.GetRequiredService<IConfigurationService>();
                var windowManager  = _serviceProvider.GetRequiredService<IWindowManagerService>();
                var startupService = _serviceProvider.GetRequiredService<IStartupService>();
                var trayService    = _serviceProvider.GetRequiredService<ITaskbarIntegrationService>();

                var windowSettings = windowManager.GetWindowSettings(window);
                var config         = configService.CurrentConfig;
                config.WindowSettings = windowSettings;
                await configService.SaveConfigAsync(config);

                await startupService.ShutdownAsync();
                await trayService.CleanupTrayIconAsync();
            }
            catch (Exception ex)
            {
                LogCrash("App.OnMainWindowClosed", ex);
            }

            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            _mutex = null;
        }
    }
}
