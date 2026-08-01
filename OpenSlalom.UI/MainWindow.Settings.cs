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

    private void ApplyTrainingRoundsToUi()
    {
        if (TrainingsViewControl.TrainingRoundsTextBox is null || TrainingsViewControl.TrainingLapCounterTextBlock is null)
        {
            return;
        }

        if (_selectedTrainingDetailId is null)
        {
            TrainingsViewControl.TrainingRoundsTextBox.Text = _localUiSettings.DefaultRundenanzahlProStint.ToString();
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
        if (TrainingsViewControl.TrainingRoundsFeedbackTextBlock is not null)
        {
            TrainingsViewControl.TrainingRoundsFeedbackTextBlock.Text = string.Empty;
        }

        UpdateTrainingLapProgressDisplay();
        UpdateTrainingStopwatchButtonsState();
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

        try
        {
            _localUiSettings.DefaultRundenanzahlProStint = defaultRounds;
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

    internal async void SaveTrainingRounds_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedTrainingDetailId is null)
        {
            TrainingsViewControl.TrainingRoundsFeedbackTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
            TrainingsViewControl.TrainingRoundsFeedbackTextBlock.Text = "Bitte zuerst ein Training auswaehlen.";
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
            _localUiSettings.TrainingRundenanzahlOverrides[_selectedTrainingDetailId.Value] = trainingRounds;
            await SaveLocalUiSettingsAsync();
            ApplyTrainingRoundsToUi();

            TrainingsViewControl.TrainingRoundsFeedbackTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#166534"));
            TrainingsViewControl.TrainingRoundsFeedbackTextBlock.Text = "Rundenanzahl fuer das Training gespeichert.";
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Trainingsspezifische Rundenanzahl konnte nicht gespeichert werden.");
            TrainingsViewControl.TrainingRoundsFeedbackTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
            TrainingsViewControl.TrainingRoundsFeedbackTextBlock.Text = "Speichern fehlgeschlagen. Details stehen im Log.";
        }
    }
}
