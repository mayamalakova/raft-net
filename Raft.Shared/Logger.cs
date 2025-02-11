using System.Reflection;
using Microsoft.Extensions.Configuration;
using Serilog.Extensions.Logging;
using Serilog;

namespace Raft.Shared;

public static class Logger
{
    public static void ConfigureLogger()
    {
        var settingsLocation = GetLogSettingsLocation("appsettings.json");
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(settingsLocation)
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();
    }
    
    private static string GetLogSettingsLocation(string settingsName)
    {
        var projectLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        return Path.Combine(projectLocation!, settingsName);
    }

    public static void Close()
    {
        Log.CloseAndFlush();
    }
}