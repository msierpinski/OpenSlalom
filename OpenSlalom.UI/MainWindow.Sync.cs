using Microsoft.EntityFrameworkCore;
using NLog;
using OpenSlalom.Data;
using OpenSlalom.Data.Entities;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Data;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;
using Microsoft.Win32;

namespace OpenSlalom.UI;

public partial class MainWindow
{
    private void UpdateConnectionStatus()
    {
        var localStatus = _databaseRuntimeInfo.LocalSqliteConnected ? "Verbunden" : "Nicht verbunden";
        var remoteStatus = _databaseRuntimeInfo.RemoteMySqlConnected ? "Verbunden" : "Nicht verbunden";

        FooterDbStatusTextBlock.Text = $"Local DB: {localStatus} | Remote DB: {remoteStatus}";

        if (!_databaseRuntimeInfo.LocalSqliteConnected && !string.IsNullOrWhiteSpace(_databaseRuntimeInfo.LocalSqliteError))
        {
            FooterDbStatusTextBlock.Text += $" | SQLite-Fehler: {_databaseRuntimeInfo.LocalSqliteError}";
            FooterDbStatusTextBlock.ToolTip = _databaseRuntimeInfo.LocalSqliteError;
        }

        if (!_databaseRuntimeInfo.RemoteMySqlConnected && !string.IsNullOrWhiteSpace(_databaseRuntimeInfo.RemoteMySqlError))
        {
            FooterDbStatusTextBlock.ToolTip = $"Local DB: {_databaseRuntimeInfo.LocalSqliteError}\nRemote DB: {_databaseRuntimeInfo.RemoteMySqlError}";
        }
    }

    private async Task RefreshSyncStatusAsync()
    {
        try
        {
            var status = await _dataSyncService.GetSyncStatusAsync();
            _syncNeeded = status.IsSyncNeeded;

            FooterSyncStateTextBlock.Text = status.Message;
            FooterSyncStateTextBlock.ToolTip = status.Message;
            UpdateSyncButtonVisualState();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Pruefen des Synchronisierungsstatus.");
            _syncNeeded = false;
            FooterSyncStateTextBlock.Text = "Sync-Status konnte nicht geladen werden.";
            FooterSyncStateTextBlock.ToolTip = ex.Message;
            UpdateSyncButtonVisualState();
        }
    }

    private void UpdateSyncButtonVisualState()
    {
        if (_syncInProgress)
        {
            FooterSyncButton.IsEnabled = false;
            FooterSyncButton.Content = "Synchronisiert...";
            FooterSyncButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
            FooterSyncButton.Foreground = Brushes.White;
            return;
        }

        FooterSyncButton.IsEnabled = true;
        FooterSyncButton.Content = _syncNeeded ? "Synchronisieren" : "Synchronisiert";

        if (_syncNeeded)
        {
            FooterSyncButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
            FooterSyncButton.Foreground = Brushes.Black;
        }
        else
        {
            FooterSyncButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"));
            FooterSyncButton.Foreground = Brushes.White;
        }
    }

    internal async void ReconnectRemote_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            await using var migrationDbContext = await _remoteMigrationDbContextFactory.CreateDbContextAsync();
            if (!await migrationDbContext.Database.CanConnectAsync())
            {
                _databaseRuntimeInfo.Set(
                    _databaseRuntimeInfo.LocalSqliteConnected,
                    false,
                    _databaseRuntimeInfo.LocalSqliteError,
                    "Remote-MySQL ist nicht erreichbar.");

                UpdateConnectionStatus();
                await RefreshSyncStatusAsync();
                EinstellungenView.SettingsFeedbackTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
                EinstellungenView.SettingsFeedbackTextBlock.Text = "Remote-Verbindung nicht verfuegbar.";
                return;
            }

            await migrationDbContext.Database.MigrateAsync();

            await using var remoteDbContext = await _remoteDbContextFactory.CreateDbContextAsync();
            var remoteConnected = await remoteDbContext.Database.CanConnectAsync();

            _databaseRuntimeInfo.Set(
                _databaseRuntimeInfo.LocalSqliteConnected,
                remoteConnected,
                _databaseRuntimeInfo.LocalSqliteError,
                remoteConnected ? null : "Remote-MySQL ist nach Migration nicht erreichbar.");

            UpdateConnectionStatus();
            await RefreshSyncStatusAsync();
            EinstellungenView.SettingsFeedbackTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(remoteConnected ? "#166534" : "#B91C1C"));
            EinstellungenView.SettingsFeedbackTextBlock.Text = remoteConnected
                ? "Remote-Verbindung erfolgreich aufgebaut."
                : "Remote-Verbindung fehlgeschlagen.";
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Remote-Verbindung konnte nicht neu aufgebaut werden.");

            _databaseRuntimeInfo.Set(
                _databaseRuntimeInfo.LocalSqliteConnected,
                false,
                _databaseRuntimeInfo.LocalSqliteError,
                ex.Message);

            UpdateConnectionStatus();
            await RefreshSyncStatusAsync();
            EinstellungenView.SettingsFeedbackTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
            EinstellungenView.SettingsFeedbackTextBlock.Text = "Remote-Verbindung fehlgeschlagen. Details stehen im Log.";
        }
    }

    private async void SyncNow_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_syncNeeded)
        {
            return;
        }

        _syncInProgress = true;
        UpdateSyncButtonVisualState();

        try
        {
            var result = await _dataSyncService.SyncBidirectionalAsync();
            FooterSyncStateTextBlock.Text = result.Message;
            FooterSyncStateTextBlock.ToolTip = result.Message;

            await LoadDisziplinenAsync();
            await LoadVereineAsync();
            await LoadTrainingsAsync();
            await LoadFahrerAsync();
            await LoadKartsAsync();
            await LoadWetterAsync();
            await LoadLookupDataAsync();

            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Synchronisierung fehlgeschlagen.");
            FooterSyncStateTextBlock.Text = "Synchronisierung fehlgeschlagen.";
            FooterSyncStateTextBlock.ToolTip = ex.Message;
            _syncNeeded = true;
            UpdateSyncButtonVisualState();
        }
        finally
        {
            _syncInProgress = false;
            UpdateSyncButtonVisualState();
        }
    }
}
