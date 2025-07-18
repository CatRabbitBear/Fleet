using Fleet.Blazor;
using Fleet.Shared;
using Fleet.Shared.Interfaces;
using Fleet.Tray.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Windows;
using Application = System.Windows.Application;

namespace Fleet.Tray;

[SupportedOSPlatform("windows")]
public partial class App : Application
{
    private NotifyIcon? _trayIcon;
    private IHost? _webHost;
    private MainWindow? _mainWindow;
    private INotificationService? _notificationService;
    public IHost WebHost => _webHost ?? throw new InvalidOperationException("Web host not initialized");

    private async void App_Startup(object sender, StartupEventArgs e)
    {

        this.Exit += async (_, __) =>
        {
            await PerformShutdownCleanupAsync();
        };

        // Try and read credentials here, showing a pop_up to the user otherwise.
        Dictionary<string, string?> managedKeys = GetManagedCredentials();

        try
        {
            // From .Shared, inject into .Blazor.
            _notificationService = new NotificationService();

            // 1) Spin up the web server
            _webHost = BlazorHostBuilder
                            .CreateHostBuilder(e.Args)
                            .ConfigureAppConfiguration((ctx, cfg) =>
                            {
                                // this merges your secrets into IConfiguration
                                cfg.AddInMemoryCollection(managedKeys);
                            })
                            .ConfigureServices(services =>
                            {
                                // notification service, etc.
                                services.AddSingleton<INotificationService>(_notificationService);

                                // register a named HttpClient pre-configured for your own server
                                services.AddHttpClient("FleetApi", client =>
                                {
                                    client.BaseAddress = new Uri("https://localhost:5001/");
                                    client.DefaultRequestVersion = new Version(2, 0); // optional
                                });
                            })
                            .Build();
            await _webHost.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            Shutdown();
            return;
        }

        ToastNotificationManagerCompat.OnActivated += toastArgs =>
        {
            var args = ToastArguments.Parse(toastArgs.Argument);

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (args["action"] == "allow")
                {
                    _notificationService?.ResolvePermission(true);
                }
                else if (args["action"] == "deny")
                {
                    _notificationService?.ResolvePermission(false);
                }
            });
        };

        try
        {
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
                        Process.Start(new ProcessStartInfo("https://localhost:5001/")
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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to create tray icon: {ex.Message}");
            await PerformShutdownCleanupAsync();
            Shutdown();
            return;
        }



        // Create (but don’t show) the dashboard window
        _mainWindow = new MainWindow();
        _mainWindow.Hide();  // ensure it starts hidden

        // Handle notifications from .Blazor
        _notificationService.OnPermissionRequested += HandlePermissionRequest;
    }

    private async void HandlePermissionRequest(PermissionRequest request)
    {
        new ToastContentBuilder()
            .AddText("Fleet - Permission Request")
            .AddText(request.Description)
            .AddButton(new ToastButton("Allow", "action=allow").SetBackgroundActivation())
            .AddButton(new ToastButton("Deny", "action=deny").SetBackgroundActivation())
            .Show();

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        bool granted;

        try
        {
            granted = await request.UserDecision.Task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            granted = false;  // Default to deny after timeout
        }

        Console.WriteLine($"Permission '{request.Description}' granted: {granted}");
    }

    private Dictionary<string, string?> GetManagedCredentials()
    {
        // 1) Try to load all 5 secrets
        var keys = new Dictionary<string, string?>();
            foreach (var target in new[] {
            "FLEET_AZURE_ENDPOINT",
            "FLEET_AZURE_MODEL_ID",
            "FLEET_AZURE_MODEL_KEY",
            "FLEET_CORS_EXCEMPTION" // Needed to allow a browser extension to work - not bundled with app so should be ignored.
        })
        {
            var (_, secret) = CredentialManagerHelper.LoadCredential(target);
            keys[target] = secret;
        }

        // 2) If any missing, block here with a single dialog
        if (keys.Values.Any(v => string.IsNullOrEmpty(v)))
        {
            var dlg = new BulkCredentialsWindow(keys);
            // BulkCredentialsWindow should:
            //  - Show 5 PasswordBoxes/TextBoxes labeled for each key
            //  - Pre‑fill any existing secrets as "***" or leave blank
            //  - Return a bool? from ShowDialog (true = OK/Save, false = cancel)
            var result = dlg.ShowDialog();
            if (result != true)
            {
                // user cancelled → bail out
                Shutdown();
                return keys;
            }

            // user clicked OK → pull back their entries and save them
            foreach (var kv in dlg.ResultingKeys)
            {
                CredentialManagerHelper.SaveCredential(
                    target: kv.Key,
                    userName: string.Empty,
                    secret: kv.Value!,
                    useLocalMachine: true);
                keys[kv.Key] = kv.Value;
            }
        }
        return keys;
    }

    private void ShutdownApp()
    {
        Shutdown();  // now the app will exit
    }

    private async Task PerformShutdownCleanupAsync()
    {
        Console.WriteLine("Shutting Down App!");
        ToastNotificationManagerCompat.History.Clear();
        ToastNotificationManagerCompat.Uninstall();
        _trayIcon?.Dispose();

        if (_webHost is not null)
            await _webHost.StopAsync();

        _mainWindow?.Close();
    }
}