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
    private async Task LoadTrainingsAsync()
    {
        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();

            var trainings = await dbContext.Trainings
                .AsNoTracking()
                .Include(x => x.Verein)
                .Include(x => x.Disziplin)
                .Include(x => x.Wetter)
                .OrderByDescending(x => x.Zeitpunkt)
                .ThenBy(x => x.Name)
                .Select(x => new TrainingListItem
                {
                    Id = x.Id,
                    Uuid = x.Uuid,
                    VereinId = x.VereinId,
                    DisziplinId = x.DisziplinId,
                    WetterId = x.WetterId,
                    Name = x.Name,
                    Beschreibung = x.Beschreibung,
                    Zeitpunkt = x.Zeitpunkt,
                    ZeitpunktText = x.Zeitpunkt.ToString("dd.MM.yyyy"),
                    TrainingAbgeschlossen = x.TrainingAbgeschlossen,
                    TrainingAbgeschlossenText = x.TrainingAbgeschlossen ? "Ja" : "Nein",
                    IstVeroeffentlicht = x.IstVeroeffentlicht,
                    IstVeroeffentlichtText = x.IstVeroeffentlicht ? "Ja" : "Nein",
                    VereinName = x.Verein.Vereinsname,
                    DisziplinName = x.Disziplin.Name,
                    WetterName = x.Wetter.Bezeichnung
                })
                .ToListAsync();

            TrainingItems.Clear();
            foreach (var item in trainings)
            {
                TrainingItems.Add(item);
            }

            RefreshOpenTrainingMenuButtons();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Laden der Trainings aus SQLite.");
        }
    }

    internal async void OpenCreateTrainingPage_OnClick(object sender, RoutedEventArgs e)
    {
        await LoadLookupDataAsync();
        TrainingsViewControl.CreateTrainingNameTextBox.Text = string.Empty;
        TrainingsViewControl.CreateTrainingBeschreibungTextBox.Text = string.Empty;
        TrainingsViewControl.CreateTrainingZeitpunktPicker.SelectedDate = DateTime.Today;
        TrainingsViewControl.CreateTrainingAbgeschlossenCheckBox.IsChecked = false;
        TrainingsViewControl.CreateTrainingIstVeroeffentlichtCheckBox.IsChecked = false;
        TrainingsViewControl.CreateTrainingVereinComboBox.SelectedIndex = -1;
        TrainingsViewControl.CreateTrainingDisziplinComboBox.SelectedIndex = -1;
        TrainingsViewControl.CreateTrainingWetterComboBox.SelectedIndex = -1;
        ShowTrainingDialog(TrainingDialogMode.Create);
    }

    internal async void SaveCreateTraining_OnClick(object sender, RoutedEventArgs e)
    {
        var name = TrainingsViewControl.CreateTrainingNameTextBox.Text.Trim();
        var beschreibung = TrainingsViewControl.CreateTrainingBeschreibungTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(beschreibung) ||
            !TrainingsViewControl.CreateTrainingZeitpunktPicker.SelectedDate.HasValue ||
            TrainingsViewControl.CreateTrainingVereinComboBox.SelectedValue is not int vereinId ||
            TrainingsViewControl.CreateTrainingDisziplinComboBox.SelectedValue is not int disziplinId ||
            TrainingsViewControl.CreateTrainingWetterComboBox.SelectedValue is not int wetterId)
        {
            MessageBox.Show("Bitte alle Felder ausfuellen.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            dbContext.Trainings.Add(new Training
            {
                Uuid = Guid.NewGuid(),
                Name = name,
                Beschreibung = beschreibung,
                Zeitpunkt = DateOnly.FromDateTime(TrainingsViewControl.CreateTrainingZeitpunktPicker.SelectedDate.Value),
                TrainingAbgeschlossen = TrainingsViewControl.CreateTrainingAbgeschlossenCheckBox.IsChecked == true,
                IstVeroeffentlicht = TrainingsViewControl.CreateTrainingIstVeroeffentlichtCheckBox.IsChecked == true,
                VereinId = vereinId,
                DisziplinId = disziplinId,
                WetterId = wetterId
            });

            await dbContext.SaveChangesAsync();
            ShowTrainingDialog(TrainingDialogMode.None);
            await LoadTrainingsAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Anlegen eines Trainings.");
            MessageBox.Show("Training konnte nicht angelegt werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    internal void OpenDetailTrainingPage_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out var trainingId))
        {
            return;
        }

        NavigateTo($"{TrainingStatisticsTagPrefix}{trainingId}");
    }

    internal async void OpenEditTrainingPage_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out var trainingId))
        {
            return;
        }

        await LoadLookupDataAsync();
        await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
        var training = await dbContext.Trainings.FirstOrDefaultAsync(x => x.Id == trainingId);
        if (training is null)
        {
            return;
        }

        _editTrainingId = training.Id;
        TrainingsViewControl.EditTrainingNameTextBox.Text = training.Name;
        TrainingsViewControl.EditTrainingBeschreibungTextBox.Text = training.Beschreibung;
        TrainingsViewControl.EditTrainingZeitpunktPicker.SelectedDate = training.Zeitpunkt.ToDateTime(TimeOnly.MinValue);
        TrainingsViewControl.EditTrainingAbgeschlossenCheckBox.IsChecked = training.TrainingAbgeschlossen;
        TrainingsViewControl.EditTrainingIstVeroeffentlichtCheckBox.IsChecked = training.IstVeroeffentlicht;
        TrainingsViewControl.EditTrainingVereinComboBox.SelectedValue = training.VereinId;
        TrainingsViewControl.EditTrainingDisziplinComboBox.SelectedValue = training.DisziplinId;
        TrainingsViewControl.EditTrainingWetterComboBox.SelectedValue = training.WetterId;
        ShowTrainingDialog(TrainingDialogMode.Edit);
    }

    internal async void SaveEditTraining_OnClick(object sender, RoutedEventArgs e)
    {
        if (_editTrainingId is null)
        {
            return;
        }

        var name = TrainingsViewControl.EditTrainingNameTextBox.Text.Trim();
        var beschreibung = TrainingsViewControl.EditTrainingBeschreibungTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(beschreibung) ||
            !TrainingsViewControl.EditTrainingZeitpunktPicker.SelectedDate.HasValue ||
            TrainingsViewControl.EditTrainingVereinComboBox.SelectedValue is not int vereinId ||
            TrainingsViewControl.EditTrainingDisziplinComboBox.SelectedValue is not int disziplinId ||
            TrainingsViewControl.EditTrainingWetterComboBox.SelectedValue is not int wetterId)
        {
            MessageBox.Show("Bitte alle Felder ausfuellen.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var training = await dbContext.Trainings.FirstOrDefaultAsync(x => x.Id == _editTrainingId.Value);
            if (training is null)
            {
                ShowTrainingDialog(TrainingDialogMode.None);
                await LoadTrainingsAsync();
                return;
            }

            training.Name = name;
            training.Beschreibung = beschreibung;
            training.Zeitpunkt = DateOnly.FromDateTime(TrainingsViewControl.EditTrainingZeitpunktPicker.SelectedDate.Value);
            training.TrainingAbgeschlossen = TrainingsViewControl.EditTrainingAbgeschlossenCheckBox.IsChecked == true;
            training.IstVeroeffentlicht = TrainingsViewControl.EditTrainingIstVeroeffentlichtCheckBox.IsChecked == true;
            training.VereinId = vereinId;
            training.DisziplinId = disziplinId;
            training.WetterId = wetterId;
            await dbContext.SaveChangesAsync();

            _editTrainingId = null;
            ShowTrainingDialog(TrainingDialogMode.None);
            await LoadTrainingsAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Bearbeiten eines Trainings.");
            MessageBox.Show("Training konnte nicht gespeichert werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    internal async void OpenDeleteTrainingPage_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out var trainingId))
        {
            return;
        }

        await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
        var training = await dbContext.Trainings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == trainingId);
        if (training is null)
        {
            return;
        }

        _deleteTrainingId = training.Id;
        TrainingsViewControl.DeleteTrainingTextBlock.Text = training.Name;
        ShowTrainingDialog(TrainingDialogMode.Delete);
    }

    internal async void ConfirmDeleteTraining_OnClick(object sender, RoutedEventArgs e)
    {
        if (_deleteTrainingId is null)
        {
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var training = await dbContext.Trainings.FirstOrDefaultAsync(x => x.Id == _deleteTrainingId.Value);
            if (training is not null)
            {
                dbContext.Trainings.Remove(training);
                await dbContext.SaveChangesAsync();
            }

            _deleteTrainingId = null;
            ShowTrainingDialog(TrainingDialogMode.None);
            await LoadTrainingsAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Loeschen eines Trainings.");
            MessageBox.Show("Training konnte nicht geloescht werden. Es wird vermutlich noch verwendet.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    internal void CancelTrainingPage_OnClick(object sender, RoutedEventArgs e)
    {
        _editTrainingId = null;
        _deleteTrainingId = null;
        ShowTrainingDialog(TrainingDialogMode.None);
    }

    private void ShowTrainingDialog(TrainingDialogMode mode)
    {
        TrainingsViewControl.TrainingCreatePage.Visibility = mode == TrainingDialogMode.Create ? Visibility.Visible : Visibility.Collapsed;
        TrainingsViewControl.TrainingEditPage.Visibility = mode == TrainingDialogMode.Edit ? Visibility.Visible : Visibility.Collapsed;
        TrainingsViewControl.TrainingDeletePage.Visibility = mode == TrainingDialogMode.Delete ? Visibility.Visible : Visibility.Collapsed;
    }
}
