using Fleet.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Application = System.Windows.Application;

namespace Fleet.Tray;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IHttpClientFactory _httpClientFactory;
    public MainWindow()
    {
        InitializeComponent();

        // Resolve the factory from the DI container
        _httpClientFactory = ((App)Application.Current)
            .WebHost.Services
            .GetRequiredService<IHttpClientFactory>();

        this.Closing += OnClosing;
    }

    private async Task RefreshStatus()
    {
        var client = _httpClientFactory.CreateClient("SleeprApi");
        // var status = await client.GetFromJsonAsync<Class1>("api/health/detail");
        var status = new { message = "Hello from Fleet!" };
        StatusTextBlock.Text = $"Message: {status?.message}";
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshStatus();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        // Cancel the close and just hide instead
        e.Cancel = true;
        this.Hide();
    }
}