namespace Fleet.Blazor.Utilities;

public static class FleetPathHelper
{
    public static string LogsDir => Ensure("Logs");
    public static string BlazorOutputDir => Ensure("BlazorOutput");

    private static string Ensure(string folder)
    {
        var path = Path.Combine(AppContext.BaseDirectory, folder);
        Directory.CreateDirectory(path);
        return path;
    }
}