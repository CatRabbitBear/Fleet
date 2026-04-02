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
    private static readonly string[] RequiredCredentialTargets =
    {
        "FLEET_AZURE_ENDPOINT",
        "FLEET_AZURE_MODEL_ID",
        "FLEET_AZURE_MODEL_KEY"
    };

    private NotifyIcon? _trayIcon;
    private IHost? _webHost;
    private MainWindow? _mainWindow;
    private INotificationService? _notificationService;
    public IHost WebHost => _webHost ?? throw new InvalidOperationException("Web host not initialized");

    private async void App_Startup(object sender, StartupEventArgs e)
    {
        StartupDiagnostics.InitializeGlobalExceptionLogging();
        StartupDiagnostics.Info("Fleet.Tray startup initiated.");

        this.Exit += async (_, __) =>
        {
            await PerformShutdownCleanupAsync();
        };

        Dictionary<string, string?> managedKeys;
        try
        {
            // Try and read credentials here, showing a pop_up to the user otherwise.
            managedKeys = GetManagedCredentials();
            StartupDiagnostics.Info($"Credential load completed. Endpoint set: {!string.IsNullOrWhiteSpace(managedKeys.GetValueOrDefault("FLEET_AZURE_ENDPOINT"))}, Model set: {!string.IsNullOrWhiteSpace(managedKeys.GetValueOrDefault("FLEET_AZURE_MODEL_ID"))}, Key set: {!string.IsNullOrWhiteSpace(managedKeys.GetValueOrDefault("FLEET_AZURE_MODEL_KEY"))}");
        }
        catch (Exception ex)
        {
            StartupDiagnostics.Error("Credential load failed.", ex);
            System.Windows.MessageBox.Show(
                "Fleet failed to load credentials. Please check the startup log for details.",
                "Fleet startup error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            return;
        }

        try
        {
            // From .Shared, inject into .Blazor.
            _notificationService = new NotificationService();
            StartupDiagnostics.Info("Building Fleet.Blazor host.");

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

            StartupDiagnostics.Info("Starting Fleet.Blazor host on https://localhost:5001.");
            await _webHost.StartAsync();
            StartupDiagnostics.Info("Fleet.Blazor host started successfully.");
        }
        catch (Exception ex)
        {
            StartupDiagnostics.Error("Fleet.Blazor host failed to start.", ex);
            System.Windows.MessageBox.Show(
                "Fleet web host failed to start. This can happen due to HTTPS certificate trust, port conflicts, or invalid credentials. Check startup logs for full exception details.",
                "Fleet startup error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
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
            StartupDiagnostics.Info("Tray icon initialized successfully.");
        }
        catch (Exception ex)
        {
            StartupDiagnostics.Error("Failed to create tray icon.", ex);
            await PerformShutdownCleanupAsync();
            Shutdown();
            return;
        }

        // Create (but don’t show) the dashboard window
        _mainWindow = new MainWindow();
        _mainWindow.Hide();  // ensure it starts hidden
        StartupDiagnostics.Info("Main window initialized and hidden.");

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

        StartupDiagnostics.Info($"Permission '{request.Description}' granted: {granted}");
    }

    private Dictionary<string, string?> GetManagedCredentials()
    {
        // 1) Try to load all required secrets.
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
        if (RequiredCredentialTargets.Any(target => string.IsNullOrWhiteSpace(keys.GetValueOrDefault(target))))
        {
            StartupDiagnostics.Info("One or more credentials missing. Showing bulk credentials dialog.");
            var dlg = new BulkCredentialsWindow(keys);
            var result = dlg.ShowDialog();
            if (result != true)
            {
                // user cancelled → bail out
                StartupDiagnostics.Info("Credential entry canceled by user.");
                Shutdown();
                return keys;
            }

            var credentialErrors = new List<string>();

            // user clicked OK → pull back their entries and save them
            foreach (var kv in dlg.ResultingKeys)
            {
                StartupDiagnostics.Info($"Attempting to persist credential '{kv.Key}'. Value provided: {!string.IsNullOrWhiteSpace(kv.Value)}");
                if (kv.Key == "FLEET_CORS_EXCEMPTION")
                {
                    if (string.IsNullOrWhiteSpace(kv.Value))
                    {
                        if (!CredentialManagerHelper.TryDeleteCredential(kv.Key, out var deleteException))
                        {
                            StartupDiagnostics.Error($"Failed to delete credential '{kv.Key}'.", deleteException!);
                            credentialErrors.Add(kv.Key);
                            continue;
                        }

                        keys[kv.Key] = null;
                        StartupDiagnostics.Info($"Credential '{kv.Key}' removed because no value was provided.");
                        continue;
                    }
                }
                else if (string.IsNullOrWhiteSpace(kv.Value))
                {
                    continue;
                }

                try
                {
                    CredentialManagerHelper.SaveCredential(
                        target: kv.Key,
                        userName: string.Empty,
                        secret: kv.Value!,
                        useLocalMachine: true);
                    keys[kv.Key] = kv.Value;
                    StartupDiagnostics.Info($"Credential '{kv.Key}' saved successfully.");
                }
                catch (Exception ex)
                {
                    StartupDiagnostics.Error($"Failed to save credential '{kv.Key}'.", ex);
                    credentialErrors.Add(kv.Key);
                }
            }

            if (credentialErrors.Count != 0)
            {
                System.Windows.MessageBox.Show(
                    $"Fleet could not update one or more credentials: {string.Join(", ", credentialErrors)}. Please check startup.log for details.",
                    "Credential update error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
                return keys;
            }

            if (!Uri.TryCreate(keys["FLEET_AZURE_ENDPOINT"], UriKind.Absolute, out _))
            {
                StartupDiagnostics.Info("Credential validation failed: FLEET_AZURE_ENDPOINT is not a valid absolute URI.");
                System.Windows.MessageBox.Show(
                    "The Azure endpoint must be a valid absolute URI.",
                    "Invalid Azure endpoint",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
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
        StartupDiagnostics.Info("Shutting down app and cleaning up resources.");
        ToastNotificationManagerCompat.History.Clear();
        ToastNotificationManagerCompat.Uninstall();
        _trayIcon?.Dispose();

        if (_webHost is not null)
            await _webHost.StopAsync();

        _mainWindow?.Close();
    }
}
