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
using MySqlConnector;

namespace OpenSlalom.UI;

public partial class MainWindow
{
    private async Task LoadLocalUiSettingsAsync()
    {
        try
        {
            if (!File.Exists(_uiSettingsFilePath))
            {
                _localUiSettings = new LocalUiSettings();
                ApplySettingsToUi();
                return;
            }

            var json = await File.ReadAllTextAsync(_uiSettingsFilePath);
            _localUiSettings = JsonSerializer.Deserialize<LocalUiSettings>(json) ?? new LocalUiSettings();
            if (_localUiSettings.DefaultRundenanzahlProStint <= 0)
            {
                _localUiSettings.DefaultRundenanzahlProStint = 10;
            }

            _localUiSettings.TrainingRundenanzahlOverrides ??= new Dictionary<int, int>();
            _localUiSettings.TrainingSollrundenUeberschreitenOverrides ??= new Dictionary<int, bool>();
            _localUiSettings.TrainingZweiteZeitnahmeOverrides ??= new Dictionary<int, bool>();
            _localUiSettings.TrainingAutomatischZurRemoteDbSynchronisierenOverrides ??= new Dictionary<int, bool>();

            ApplySettingsToUi();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Lokale UI-Einstellungen konnten nicht geladen werden.");
            _localUiSettings = new LocalUiSettings();
            ApplySettingsToUi();
        }
    }

    private void ApplySettingsToUi()
    {
        if (EinstellungenView?.DefaultStintRoundsTextBox is null)
        {
            return;
        }

        EinstellungenView.DefaultStintRoundsTextBox.Text = _localUiSettings.DefaultRundenanzahlProStint.ToString();
        EinstellungenView.DefaultAllowExtraRoundsCheckBox.IsChecked = _localUiSettings.DefaultSollrundenUeberschreiten;
        EinstellungenView.DefaultSecondTimingCheckBox.IsChecked = _localUiSettings.DefaultZweiteZeitnahme;
        EinstellungenView.DefaultAutoSyncRemoteDbCheckBox.IsChecked = _localUiSettings.DefaultAutomatischZurRemoteDbSynchronisieren;
        ApplyRemoteConnectionSettingsToUi();
        EinstellungenView.SettingsFeedbackTextBlock.Text = string.Empty;

        ApplyTrainingRoundsToUi();
    }

    private int GetRoundsTargetForTraining(int trainingId)
    {
        if (_localUiSettings.TrainingRundenanzahlOverrides.TryGetValue(trainingId, out var rounds) && rounds > 0)
        {
            return rounds;
        }

        return _localUiSettings.DefaultRundenanzahlProStint;
    }

    private bool CanExceedRoundsTargetForTraining(int trainingId)
    {
        return _localUiSettings.TrainingSollrundenUeberschreitenOverrides.TryGetValue(trainingId, out var canExceed)
            ? canExceed
            : _localUiSettings.DefaultSollrundenUeberschreiten;
    }

    private bool IsSecondTrainingTimingEnabled(int trainingId)
    {
        return _localUiSettings.TrainingZweiteZeitnahmeOverrides.TryGetValue(trainingId, out var enabled)
            ? enabled
            : _localUiSettings.DefaultZweiteZeitnahme;
    }

    private bool IsAutomaticRemoteSyncEnabled(int trainingId)
    {
        return _localUiSettings.TrainingAutomatischZurRemoteDbSynchronisierenOverrides.TryGetValue(trainingId, out var enabled)
            ? enabled
            : _localUiSettings.DefaultAutomatischZurRemoteDbSynchronisieren;
    }

    private void ApplyTrainingRoundsToUi()
    {
        if (TrainingsViewControl.TrainingRoundsTextBox is null || TrainingsViewControl.TrainingLapCounterTextBlock is null)
        {
            return;
        }

        _applyingTrainingSettingsToUi = true;
        try
        {
            if (_selectedTrainingDetailId is null)
            {
                TrainingsViewControl.TrainingRoundsTextBox.Text = _localUiSettings.DefaultRundenanzahlProStint.ToString();
                TrainingsViewControl.TrainingAllowExtraRoundsCheckBox.IsChecked = _localUiSettings.DefaultSollrundenUeberschreiten;
                TrainingsViewControl.TrainingSecondTimingCheckBox.IsChecked = _localUiSettings.DefaultZweiteZeitnahme;
                TrainingsViewControl.TrainingAutoSyncRemoteDbCheckBox.IsChecked = _localUiSettings.DefaultAutomatischZurRemoteDbSynchronisieren;
                TrainingsViewControl.TrainingSecondStopwatchPanel.Visibility = _localUiSettings.DefaultZweiteZeitnahme ? Visibility.Visible : Visibility.Collapsed;
                if (TrainingsViewControl.TrainingRoundsFeedbackTextBlock is not null)
                {
                    TrainingsViewControl.TrainingRoundsFeedbackTextBlock.Text = string.Empty;
                }

                UpdateTrainingLapProgressDisplay();
                UpdateTrainingStopwatchButtonsState();

                return;
            }

            var rounds = GetRoundsTargetForTraining(_selectedTrainingDetailId.Value);
            TrainingsViewControl.TrainingRoundsTextBox.Text = rounds.ToString();
            TrainingsViewControl.TrainingAllowExtraRoundsCheckBox.IsChecked = CanExceedRoundsTargetForTraining(_selectedTrainingDetailId.Value);
            var secondTimingEnabled = IsSecondTrainingTimingEnabled(_selectedTrainingDetailId.Value);
            TrainingsViewControl.TrainingSecondTimingCheckBox.IsChecked = secondTimingEnabled;
            TrainingsViewControl.TrainingAutoSyncRemoteDbCheckBox.IsChecked = IsAutomaticRemoteSyncEnabled(_selectedTrainingDetailId.Value);
            TrainingsViewControl.TrainingSecondStopwatchPanel.Visibility = secondTimingEnabled ? Visibility.Visible : Visibility.Collapsed;
            if (TrainingsViewControl.TrainingRoundsFeedbackTextBlock is not null)
            {
                TrainingsViewControl.TrainingRoundsFeedbackTextBlock.Text = string.Empty;
            }

            UpdateTrainingLapProgressDisplay();
            UpdateTrainingStopwatchButtonsState();
            UpdateSecondTrainingStopwatchButtonsState();
        }
        finally
        {
            _applyingTrainingSettingsToUi = false;
        }
    }

    private async Task SaveLocalUiSettingsAsync()
    {
        var settingsDirectory = Path.GetDirectoryName(_uiSettingsFilePath);
        if (!string.IsNullOrWhiteSpace(settingsDirectory))
        {
            Directory.CreateDirectory(settingsDirectory);
        }

        var json = JsonSerializer.Serialize(_localUiSettings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_uiSettingsFilePath, json);
    }

    internal async void SaveSettings_OnClick(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(EinstellungenView.DefaultStintRoundsTextBox.Text.Trim(), out var defaultRounds) || defaultRounds <= 0)
        {
            EinstellungenView.SettingsFeedbackTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
            EinstellungenView.SettingsFeedbackTextBlock.Text = "Bitte eine gueltige, positive Rundenanzahl eingeben.";
            return;
        }

        if (!TryApplyRemoteConnectionSettingsFromUi(out var remoteConnectionError))
        {
            EinstellungenView.SettingsFeedbackTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
            EinstellungenView.SettingsFeedbackTextBlock.Text = remoteConnectionError;
            return;
        }

        try
        {
            _localUiSettings.DefaultRundenanzahlProStint = defaultRounds;
            _localUiSettings.DefaultSollrundenUeberschreiten = EinstellungenView.DefaultAllowExtraRoundsCheckBox.IsChecked == true;
            _localUiSettings.DefaultZweiteZeitnahme = EinstellungenView.DefaultSecondTimingCheckBox.IsChecked == true;
            _localUiSettings.DefaultAutomatischZurRemoteDbSynchronisieren = EinstellungenView.DefaultAutoSyncRemoteDbCheckBox.IsChecked == true;
            await SaveLocalUiSettingsAsync();
            ApplyTrainingRoundsToUi();

            EinstellungenView.SettingsFeedbackTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#166534"));
            EinstellungenView.SettingsFeedbackTextBlock.Text = "Einstellungen wurden lokal gespeichert.";
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Lokale UI-Einstellungen konnten nicht gespeichert werden.");
            EinstellungenView.SettingsFeedbackTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
            EinstellungenView.SettingsFeedbackTextBlock.Text = "Speichern fehlgeschlagen. Details stehen im Log.";
        }
    }

    private void ApplyRemoteConnectionSettingsToUi()
    {
        try
        {
            var builder = new MySqlConnectionStringBuilder(_remoteDbConnectionSettings.ConnectionString);
            EinstellungenView.RemoteDbServerTextBox.Text = builder.Server;
            EinstellungenView.RemoteDbPortTextBox.Text = builder.Port.ToString(CultureInfo.InvariantCulture);
            EinstellungenView.RemoteDbDatabaseTextBox.Text = builder.Database;
            EinstellungenView.RemoteDbUserTextBox.Text = builder.UserID;
            EinstellungenView.RemoteDbPasswordBox.Password = builder.Password;

            _localUiSettings.RemoteDbServer = builder.Server;
            _localUiSettings.RemoteDbPort = checked((int)builder.Port);
            _localUiSettings.RemoteDbDatabase = builder.Database;
            _localUiSettings.RemoteDbUser = builder.UserID;
            _localUiSettings.RemoteDbPassword = builder.Password;
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Remote-DB-Verbindung konnte nicht in die Einstellungen übernommen werden.");
        }
    }

    private bool TryApplyRemoteConnectionSettingsFromUi(out string errorMessage)
    {
        var server = EinstellungenView.RemoteDbServerTextBox.Text.Trim();
        var portText = EinstellungenView.RemoteDbPortTextBox.Text.Trim();
        var database = EinstellungenView.RemoteDbDatabaseTextBox.Text.Trim();
        var user = EinstellungenView.RemoteDbUserTextBox.Text.Trim();
        var password = EinstellungenView.RemoteDbPasswordBox.Password;

        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database) || string.IsNullOrWhiteSpace(user))
        {
            errorMessage = "Server, Datenbank und Benutzer dürfen nicht leer sein.";
            return false;
        }

        if (!uint.TryParse(portText, NumberStyles.None, CultureInfo.InvariantCulture, out var port) || port == 0 || port > ushort.MaxValue)
        {
            errorMessage = "Bitte einen gültigen Port zwischen 1 und 65535 eingeben.";
            return false;
        }

        try
        {
            var builder = new MySqlConnectionStringBuilder(_remoteDbConnectionSettings.ConnectionString)
            {
                Server = server,
                Port = port,
                Database = database,
                UserID = user,
                Password = password
            };

            _localUiSettings.RemoteDbServer = server;
            _localUiSettings.RemoteDbPort = (int)port;
            _localUiSettings.RemoteDbDatabase = database;
            _localUiSettings.RemoteDbUser = user;
            _localUiSettings.RemoteDbPassword = password;
            _remoteDbConnectionSettings.Update(builder.ConnectionString);
            errorMessage = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Remote-DB-Verbindungsdaten sind ungültig.");
            errorMessage = "Die Remote-Verbindungsdaten sind ungültig.";
            return false;
        }
    }

    internal void TrainingRoundsTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_applyingTrainingSettingsToUi || _selectedTrainingDetailId is null)
        {
            return;
        }

        _trainingSettingsSaveTimer.Stop();
        _trainingSettingsSaveTimer.Start();
        TrainingsViewControl.TrainingRoundsFeedbackTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
        TrainingsViewControl.TrainingRoundsFeedbackTextBlock.Text = "Änderung wird automatisch gespeichert ...";
    }

    private async void TrainingSettingsSaveTimer_OnTick(object? sender, EventArgs e)
    {
        _trainingSettingsSaveTimer.Stop();
        await SaveTrainingRoundsAutomaticallyAsync();
    }

    private async Task SaveTrainingRoundsAutomaticallyAsync()
    {
        if (_selectedTrainingDetailId is null)
        {
            return;
        }

        if (!int.TryParse(TrainingsViewControl.TrainingRoundsTextBox.Text.Trim(), out var trainingRounds) || trainingRounds <= 0)
        {
            TrainingsViewControl.TrainingRoundsFeedbackTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
            TrainingsViewControl.TrainingRoundsFeedbackTextBlock.Text = "Bitte eine gueltige, positive Rundenanzahl eingeben.";
            return;
        }

        try
        {
            var trainingId = _selectedTrainingDetailId.Value;
            _localUiSettings.TrainingRundenanzahlOverrides[trainingId] = trainingRounds;
            await SaveLocalUiSettingsAsync();
            if (_selectedTrainingDetailId != trainingId)
            {
                return;
            }

            UpdateTrainingLapProgressDisplay();
            UpdateTrainingStopwatchButtonsState();
            UpdateTrainingDriverButtonsState();

            TrainingsViewControl.TrainingRoundsFeedbackTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#166534"));
            TrainingsViewControl.TrainingRoundsFeedbackTextBlock.Text = "Rundenanzahl automatisch gespeichert.";
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Trainingsspezifische Rundenanzahl konnte nicht gespeichert werden.");
            TrainingsViewControl.TrainingRoundsFeedbackTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
            TrainingsViewControl.TrainingRoundsFeedbackTextBlock.Text = "Speichern fehlgeschlagen. Details stehen im Log.";
        }
    }

    internal async void TrainingAllowExtraRoundsCheckBox_OnClick(object sender, RoutedEventArgs e)
    {
        if (_applyingTrainingSettingsToUi || _selectedTrainingDetailId is null || sender is not CheckBox checkBox)
        {
            return;
        }

        var trainingId = _selectedTrainingDetailId.Value;
        _localUiSettings.TrainingSollrundenUeberschreitenOverrides[trainingId] = checkBox.IsChecked == true;

        try
        {
            await SaveLocalUiSettingsAsync();
            if (_selectedTrainingDetailId != trainingId)
            {
                return;
            }

            UpdateTrainingStopwatchButtonsState();
            UpdateTrainingDriverButtonsState();
            TrainingsViewControl.TrainingRoundsFeedbackTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#166534"));
            TrainingsViewControl.TrainingRoundsFeedbackTextBlock.Text = "Einstellung automatisch gespeichert.";
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Einstellung zum Überschreiten der Sollrunden konnte nicht gespeichert werden.");
            TrainingsViewControl.TrainingRoundsFeedbackTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
            TrainingsViewControl.TrainingRoundsFeedbackTextBlock.Text = "Speichern fehlgeschlagen. Details stehen im Log.";
        }
    }

    internal async void TrainingSecondTimingCheckBox_OnClick(object sender, RoutedEventArgs e)
    {
        if (_applyingTrainingSettingsToUi || _selectedTrainingDetailId is null || sender is not CheckBox checkBox)
        {
            return;
        }

        var trainingId = _selectedTrainingDetailId.Value;
        var enabled = checkBox.IsChecked == true;
        if (!enabled && !CanDisableSecondTrainingTiming())
        {
            _applyingTrainingSettingsToUi = true;
            checkBox.IsChecked = true;
            _applyingTrainingSettingsToUi = false;
            MessageBox.Show("Die zweite Zeitnahme kann erst deaktiviert werden, wenn ihr Stint noch nicht begonnen wurde.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _localUiSettings.TrainingZweiteZeitnahmeOverrides[trainingId] = enabled;
        if (!enabled)
        {
            _trainingSecondActiveDriverByTrainingId.Remove(trainingId);
            _trainingSecondStopwatchContext = null;
            TrainingSecondLapTimeItems.Clear();
            foreach (var item in TrainingStarterListItems)
            {
                item.IsAktivZweiteZeitnahme = false;
            }

            TrainingsViewControl.TrainingStarterDataGrid.Items.Refresh();
        }

        TrainingsViewControl.TrainingSecondStopwatchPanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;

        try
        {
            await SaveLocalUiSettingsAsync();
            UpdateSecondTrainingStopwatchContextWithActiveDriver();
            UpdateTrainingDriverButtonsState();
            TrainingsViewControl.TrainingRoundsFeedbackTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#166534"));
            TrainingsViewControl.TrainingRoundsFeedbackTextBlock.Text = "Einstellung automatisch gespeichert.";
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Einstellung für die zweite Zeitnahme konnte nicht gespeichert werden.");
            TrainingsViewControl.TrainingRoundsFeedbackTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
            TrainingsViewControl.TrainingRoundsFeedbackTextBlock.Text = "Speichern fehlgeschlagen. Details stehen im Log.";
        }
    }

    internal async void TrainingAutoSyncRemoteDbCheckBox_OnClick(object sender, RoutedEventArgs e)
    {
        if (_applyingTrainingSettingsToUi || _selectedTrainingDetailId is null || sender is not CheckBox checkBox)
        {
            return;
        }

        var trainingId = _selectedTrainingDetailId.Value;
        _localUiSettings.TrainingAutomatischZurRemoteDbSynchronisierenOverrides[trainingId] = checkBox.IsChecked == true;

        try
        {
            await SaveLocalUiSettingsAsync();
            if (_selectedTrainingDetailId != trainingId)
            {
                return;
            }

            TrainingsViewControl.TrainingRoundsFeedbackTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#166534"));
            TrainingsViewControl.TrainingRoundsFeedbackTextBlock.Text = "Einstellung automatisch gespeichert.";
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Einstellung zur automatischen Remote-Synchronisierung konnte nicht gespeichert werden.");
            TrainingsViewControl.TrainingRoundsFeedbackTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
            TrainingsViewControl.TrainingRoundsFeedbackTextBlock.Text = "Speichern fehlgeschlagen. Details stehen im Log.";
        }
    }
}
