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

        foreach (var entry in dialog.ResultingKeys)
        {
            if (entry.Key == "FLEET_CORS_EXCEMPTION" && string.IsNullOrWhiteSpace(entry.Value))
            {
                CredentialManagerHelper.DeleteCredential(entry.Key);
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.Value))
            {
                continue;
            }

            CredentialManagerHelper.SaveCredential(
                target: entry.Key,
                userName: string.Empty,
                secret: entry.Value,
                useLocalMachine: true);
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
