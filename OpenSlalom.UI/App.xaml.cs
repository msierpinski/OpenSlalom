using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Hosting;
using OpenSlalom.Data;
using MySqlConnector;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace OpenSlalom.UI;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var splashWindow = new SplashWindow();
        splashWindow.Show();

        try
        {
            var appConfiguration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var remoteMySqlConnectionString = appConfiguration.GetConnectionString("OpenSlalomRemote")
                ?? throw new InvalidOperationException("Connection string 'OpenSlalomRemote' fehlt.");

            remoteMySqlConnectionString = LoadSavedRemoteConnectionString(remoteMySqlConnectionString);

            var localSqliteConnectionString = appConfiguration.GetConnectionString("OpenSlalomLocal")
                ?? "Data Source=open_slalom_local.db";

            _host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    config.Sources.Clear();
                    config.AddConfiguration(appConfiguration);
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
                })
                .UseNLog()
                .ConfigureServices((_, services) =>
                {
                    services.AddOpenSlalomDualData(localSqliteConnectionString, remoteMySqlConnectionString);
                    services.AddSingleton<DatabaseRuntimeInfo>();
                    services.AddTransient<MainWindow>();
                })
                .Build();

            var initResult = await _host.InitializeOpenSlalomDualDatabasesAsync();
            _host.Services.GetRequiredService<DatabaseRuntimeInfo>().Set(
                initResult.LocalSqliteConnected,
                initResult.RemoteMySqlConnected,
                initResult.LocalSqliteError,
                initResult.RemoteMySqlError);

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Start fehlgeschlagen: {ex.Message}",
                "OpenSlalom",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
        finally
        {
            splashWindow.Close();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        LogManager.Shutdown();

        base.OnExit(e);
    }

    private static string LoadSavedRemoteConnectionString(string fallbackConnectionString)
    {
        var settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenSlalom",
            "ui-settings.json");

        try
        {
            if (!File.Exists(settingsPath))
            {
                return fallbackConnectionString;
            }

            var settings = JsonSerializer.Deserialize<MainWindow.LocalUiSettings>(File.ReadAllText(settingsPath));
            if (settings is null || string.IsNullOrWhiteSpace(settings.RemoteDbServer) ||
                string.IsNullOrWhiteSpace(settings.RemoteDbDatabase) || string.IsNullOrWhiteSpace(settings.RemoteDbUser) ||
                settings.RemoteDbPort <= 0 || settings.RemoteDbPort > ushort.MaxValue)
            {
                return fallbackConnectionString;
            }

            var builder = new MySqlConnectionStringBuilder(fallbackConnectionString)
            {
                Server = settings.RemoteDbServer.Trim(),
                Port = (uint)settings.RemoteDbPort,
                Database = settings.RemoteDbDatabase.Trim(),
                UserID = settings.RemoteDbUser.Trim(),
                Password = settings.RemoteDbPassword
            };

            return builder.ConnectionString;
        }
        catch
        {
            return fallbackConnectionString;
        }
    }
}

