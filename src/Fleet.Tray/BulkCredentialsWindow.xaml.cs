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


namespace Fleet.Tray;
/// <summary>
/// Interaction logic for BulkCredentialsWindow.xaml
/// </summary>
public partial class BulkCredentialsWindow : Window
{
    public Dictionary<string, string> ResultingKeys { get; } = new();
    public BulkCredentialsWindow(Dictionary<string, string?> existingKeys)
    {
        InitializeComponent();

        // Pre-populate fields if existing
        if (existingKeys.TryGetValue("FLEET_AZURE_ENDPOINT", out var endpoint) && endpoint != null)
            EndpointTextBox.Text = endpoint;
        if (existingKeys.TryGetValue("FLEET_AZURE_MODEL_ID", out var model) && model != null)
            ModelTextBox.Text = model;
        if (existingKeys.TryGetValue("FLEET_AZURE_MODEL_KEY", out var apiKey) && apiKey != null)
            ApiKeyPasswordBox.Password = apiKey;
        if (existingKeys.TryGetValue("FLEET_CORS_EXCEMPTION", out var cors) && cors != null)
            CorsExceptionPasswordBox.Password = cors;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(EndpointTextBox.Text) ||
            string.IsNullOrWhiteSpace(ModelTextBox.Text) ||
            string.IsNullOrWhiteSpace(ApiKeyPasswordBox.Password))
        {
            System.Windows.MessageBox.Show("Please fill in all required fields (Endpoint, Model, API Key).", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Gather results
        ResultingKeys["FLEET_AZURE_ENDPOINT"] = EndpointTextBox.Text;
        ResultingKeys["FLEET_AZURE_MODEL_ID"] = ModelTextBox.Text;
        ResultingKeys["FLEET_AZURE_MODEL_KEY"] = ApiKeyPasswordBox.Password;
        // Optional
        ResultingKeys["FLEET_CORS_EXCEMPTION"] = CorsExceptionPasswordBox.Password;

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
