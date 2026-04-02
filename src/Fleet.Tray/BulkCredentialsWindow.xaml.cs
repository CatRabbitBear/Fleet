using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Fleet.Tray.Utils;


namespace Fleet.Tray;
/// <summary>
/// Interaction logic for BulkCredentialsWindow.xaml
/// </summary>
public partial class BulkCredentialsWindow : Window
{
    public Dictionary<string, string> ResultingKeys { get; } = new();
    private readonly Dictionary<string, string?> _existingKeys;

    public BulkCredentialsWindow(Dictionary<string, string?> existingKeys)
    {
        InitializeComponent();
        _existingKeys = existingKeys;

        // Pre-populate non-secret fields.
        if (existingKeys.TryGetValue("FLEET_AZURE_ENDPOINT", out var endpoint) && endpoint != null)
            EndpointTextBox.Text = endpoint;
        if (existingKeys.TryGetValue("FLEET_AZURE_MODEL_ID", out var model) && model != null)
            ModelTextBox.Text = model;
        if (existingKeys.TryGetValue("FLEET_CORS_EXCEMPTION", out var cors) && cors != null)
            CorsExceptionTextBox.Text = cors;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        StartupDiagnostics.Info("Bulk credentials dialog OK clicked.");
        // Validate required fields
        var endpoint = EndpointTextBox.Text.Trim();
        var model = ModelTextBox.Text.Trim();
        var apiKey = ApiKeyPasswordBox.Password;

        if (string.IsNullOrWhiteSpace(endpoint) ||
            string.IsNullOrWhiteSpace(model) ||
            (string.IsNullOrWhiteSpace(apiKey) && string.IsNullOrWhiteSpace(_existingKeys.GetValueOrDefault("FLEET_AZURE_MODEL_KEY"))))
        {
            System.Windows.MessageBox.Show("Please fill in all required fields (OpenAI Endpoint, Model/Deployment, API Key).", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Gather results
        ResultingKeys["FLEET_AZURE_ENDPOINT"] = endpoint;
        ResultingKeys["FLEET_AZURE_MODEL_ID"] = model;
        ResultingKeys["FLEET_AZURE_MODEL_KEY"] = string.IsNullOrWhiteSpace(apiKey)
            ? _existingKeys["FLEET_AZURE_MODEL_KEY"]!
            : apiKey;
        // Optional
        ResultingKeys["FLEET_CORS_EXCEMPTION"] = CorsExceptionTextBox.Text.Trim();

        StartupDiagnostics.Info($"Bulk credentials collected. EndpointProvided={!string.IsNullOrWhiteSpace(endpoint)}, ModelProvided={!string.IsNullOrWhiteSpace(model)}, ApiKeyProvided={!string.IsNullOrWhiteSpace(apiKey)}, CorsProvided={!string.IsNullOrWhiteSpace(ResultingKeys["FLEET_CORS_EXCEMPTION"])}");

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
