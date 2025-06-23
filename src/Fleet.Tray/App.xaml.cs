using Fleet.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace Fleet.Tray
{
    public partial class App : Application
    {
        private NotifyIcon? _trayIcon;
        private IHost? _webHost;
        private MainWindow? _mainWindow;
        public IHost WebHost => _webHost ?? throw new InvalidOperationException("Web host not initialized");

        private async void App_Startup(object sender, StartupEventArgs e)
        {
            // 1) Spin up the web server
            _webHost = WebHostBuilder
                            .MyCreateHostBuilder(Array.Empty<string>())
                            .ConfigureServices(services =>
                            {
                                // notification service, etc.

                                // register a named HttpClient pre-configured for your own server
                                services.AddHttpClient("SleeprApi", client =>
                                {
                                    client.BaseAddress = new Uri("https://localhost:5001/");
                                    client.DefaultRequestVersion = new Version(2, 0); // optional
                                });
                            })
                            .Build();
            await _webHost.StartAsync();

            // 2) Show the tray icon
            _trayIcon = new NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Text = "Fleet",
                Visible = true,
                ContextMenuStrip = new ContextMenuStrip
                {
                    Items =
                    {
                        new ToolStripMenuItem("Open Web UI", null, (_,__) =>
                            Process.Start(new ProcessStartInfo("https://localhost:5001/api/status")
                            { UseShellExecute = true })),
                                    // new item to show your MainWindow
                        new ToolStripMenuItem("Show Dashboard", null, (_,__) =>
                        {
                            // Ensure we marshal back to the WPF UI thread
                            Dispatcher.Invoke(() =>
                            {
                                // If window was minimized, reset state
                                if (_mainWindow == null) return;
                                _mainWindow.WindowState = WindowState.Normal;
                                _mainWindow.Show();
                                _mainWindow.Activate();
                            });
                        }),
                        new ToolStripMenuItem("Exit", null, (_,__) => ShutdownApp())
                    }
                }
            };

            // Create (but don’t show) the dashboard window
            _mainWindow = new MainWindow();
            _mainWindow.Hide();  // ensure it starts hidden
        }

        private async void ShutdownApp()
        {
            _trayIcon?.Dispose();
            if (_webHost is not null)
                await _webHost.StopAsync();
            _mainWindow?.Close();
            Shutdown();  // now the app will exit
        }
    }
}