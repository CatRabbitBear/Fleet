using Fleet.Tray.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Net.Http;
using System.Windows;
using Application = System.Windows.Application;

namespace Fleet.Tray;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly string[] CredentialTargets =
    {
        "FLEET_AZURE_ENDPOINT",
        "FLEET_AZURE_MODEL_ID",
        "FLEET_AZURE_MODEL_KEY",
        "FLEET_CORS_EXCEMPTION"
    };

    public MainWindow()
    {
        InitializeComponent();

        // Resolve the factory from the DI container.
        _httpClientFactory = ((App)Application.Current)
            .WebHost.Services
            .GetRequiredService<IHttpClientFactory>();

        Closing += OnClosing;
    }

    private async Task RefreshStatus()
    {
        var status = new { message = "Hello from Fleet!" };
        StatusTextBlock.Text = $"Message: {status?.message}";
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshStatus();
    }

    private void ConfigureCredentialsButton_Click(object sender, RoutedEventArgs e)
    {
        var existing = CredentialTargets.ToDictionary(
            key => key,
            key => CredentialManagerHelper.LoadCredential(key).Secret);

        var dialog = new BulkCredentialsWindow(existing);
        var result = dialog.ShowDialog();

        if (result != true)
        {
            return;
        }

        var failedTargets = new List<string>();
        foreach (var entry in dialog.ResultingKeys)
        {
            if (entry.Key == "FLEET_CORS_EXCEMPTION" && string.IsNullOrWhiteSpace(entry.Value))
            {
                if (!CredentialManagerHelper.TryDeleteCredential(entry.Key, out var deleteException))
                {
                    StartupDiagnostics.Error($"Failed to delete credential '{entry.Key}'.", deleteException!);
                    failedTargets.Add(entry.Key);
                }
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.Value))
            {
                continue;
            }

            try
            {
                CredentialManagerHelper.SaveCredential(
                    target: entry.Key,
                    userName: string.Empty,
                    secret: entry.Value,
                    useLocalMachine: true);
            }
            catch (Exception ex)
            {
                StartupDiagnostics.Error($"Failed to save credential '{entry.Key}'.", ex);
                failedTargets.Add(entry.Key);
            }
        }

        if (failedTargets.Count != 0)
        {
            StatusTextBlock.Text = $"Some credentials failed to save: {string.Join(", ", failedTargets)}. See startup.log.";
            return;
        }

        StatusTextBlock.Text = "Azure credentials saved. Restart Fleet to reload runtime settings.";
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        // Cancel the close and just hide instead.
        e.Cancel = true;
        Hide();
    }
}
