using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OpenSlalom.UI;

public partial class MainWindow
{
    private async Task LoadTrainingStoredStintDriversAsync(int trainingId)
    {
        _loadingTrainingStoredStintDrivers = true;
        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var drivers = await dbContext.Tstints
                .AsNoTracking()
                .Where(x => x.TrainingId == trainingId)
                .Select(x => new
                {
                    x.FahrerId,
                    x.Fahrer.Vorname,
                    Nachname = x.Fahrer.Nachname ?? string.Empty
                })
                .Distinct()
                .OrderBy(x => x.Vorname)
                .ThenBy(x => x.Nachname)
                .ToListAsync();

            TrainingStoredStintDriverItems.Clear();
            foreach (var driver in drivers)
            {
                TrainingStoredStintDriverItems.Add(new TrainingStoredStintDriverListItem
                {
                    FahrerId = driver.FahrerId,
                    Fahrer = string.IsNullOrWhiteSpace(driver.Nachname)
                        ? driver.Vorname
                        : $"{driver.Vorname} {driver.Nachname}"
                });
            }

            if (_selectedTrainingStoredStintDriverId is not null &&
                TrainingStoredStintDriverItems.Any(x => x.FahrerId == _selectedTrainingStoredStintDriverId.Value))
            {
                TrainingsViewControl.TrainingStoredStintDriverComboBox.SelectedValue = _selectedTrainingStoredStintDriverId.Value;
                await LoadTrainingStoredStintsAsync(trainingId, _selectedTrainingStoredStintDriverId.Value);
                return;
            }

            _selectedTrainingStoredStintDriverId = null;
            TrainingsViewControl.TrainingStoredStintDriverComboBox.SelectedIndex = -1;
            TrainingStoredStintItems.Clear();
            UpdateTrainingStoredStintsEmptyState("Bitte einen Fahrer auswählen.");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fahrer mit gespeicherten Trainingsstints konnten nicht geladen werden.");
            TrainingStoredStintDriverItems.Clear();
            TrainingStoredStintItems.Clear();
            UpdateTrainingStoredStintsEmptyState("Gespeicherte Stints konnten nicht geladen werden.");
        }
        finally
        {
            _loadingTrainingStoredStintDrivers = false;
        }
    }

    internal async void TrainingStoredStintDriverComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingTrainingStoredStintDrivers || _selectedTrainingDetailId is null || sender is not ComboBox comboBox)
        {
            return;
        }

        if (comboBox.SelectedValue is not int driverId)
        {
            _selectedTrainingStoredStintDriverId = null;
            TrainingStoredStintItems.Clear();
            UpdateTrainingStoredStintsEmptyState("Bitte einen Fahrer auswählen.");
            return;
        }

        _selectedTrainingStoredStintDriverId = driverId;
        await LoadTrainingStoredStintsAsync(_selectedTrainingDetailId.Value, driverId);
    }

    private async Task LoadTrainingStoredStintsAsync(int trainingId, int driverId)
    {
        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var stints = await dbContext.Tstints
                .AsNoTracking()
                .Where(x => x.TrainingId == trainingId && x.FahrerId == driverId)
                .Include(x => x.Kart)
                .Include(x => x.Trunden)
                .OrderByDescending(x => x.Datum)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            if (_selectedTrainingDetailId != trainingId || _selectedTrainingStoredStintDriverId != driverId)
            {
                return;
            }

            TrainingStoredStintItems.Clear();
            foreach (var stint in stints)
            {
                var item = new TrainingStoredStintListItem
                {
                    StintId = stint.Id,
                    Titel = $"Stint #{stint.Id}",
                    ZeitpunktText = stint.Datum.ToString("dd.MM.yyyy HH:mm:ss"),
                    Kart = string.IsNullOrWhiteSpace(stint.Kart?.Name) ? "-" : stint.Kart.Name,
                    Altersklasse = string.IsNullOrWhiteSpace(stint.AltersklasseSnapshot) ? "-" : stint.AltersklasseSnapshot
                };

                foreach (var lap in stint.Trunden.OrderBy(x => x.Runde ?? int.MaxValue).ThenBy(x => x.Id))
                {
                    var lapItem = new TrainingStoredStintLapListItem
                    {
                        RundenId = lap.Id,
                        StintId = stint.Id,
                        Runde = lap.Runde ?? 0,
                        RundenzeitSekunden = lap.Rundenzeit ?? 0d,
                        RundenzeitText = lap.Rundenzeit is > 0d
                            ? FormatTrainingTime(TimeSpan.FromSeconds(lap.Rundenzeit.Value))
                            : "-",
                        Pylonen = Math.Max(0, lap.Pf ?? 0),
                        Tore = Math.Max(0, lap.Tf ?? 0),
                        Ungueltig = lap.Ungueltig
                    };
                    RecalculateStoredTrainingLapPenalty(lapItem);
                    item.Runden.Add(lapItem);
                }

                RecalculateStoredTrainingStintSummary(item);
                TrainingStoredStintItems.Add(item);
            }

            UpdateTrainingStoredStintsEmptyState(stints.Count == 0 ? "Für diesen Fahrer sind keine Stints gespeichert." : string.Empty);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Gespeicherte Trainingsstints konnten nicht geladen werden.");
            TrainingStoredStintItems.Clear();
            UpdateTrainingStoredStintsEmptyState("Gespeicherte Stints konnten nicht geladen werden.");
        }
    }

    private void RecalculateStoredTrainingLapPenalty(TrainingStoredStintLapListItem lap)
    {
        var penalty = (lap.Tore * _selectedTrainingTorfehlerPenaltySeconds) +
                      (lap.Pylonen * _selectedTrainingPylonenfehlerPenaltySeconds);
        lap.ZeitstrafeSekunden = Math.Round(Math.Max(0d, penalty), 3, MidpointRounding.AwayFromZero);
    }

    private static void RecalculateStoredTrainingStintSummary(TrainingStoredStintListItem stint)
    {
        var validLaps = stint.Runden.Where(x => !x.Ungueltig && x.RundenzeitSekunden > 0d).ToList();
        if (validLaps.Count == 0)
        {
            stint.GesamtzeitText = "-";
            stint.DurchschnittszeitText = "-";
            return;
        }

        var totalSeconds = validLaps.Sum(x => x.RundenzeitSekunden + x.ZeitstrafeSekunden);
        stint.GesamtzeitText = FormatTrainingTime(TimeSpan.FromSeconds(totalSeconds));
        stint.DurchschnittszeitText = FormatTrainingTime(TimeSpan.FromSeconds(totalSeconds / validLaps.Count));
    }

    internal async void StoredTrainingLapNumericAdjust_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not TrainingStoredStintLapListItem lap)
        {
            return;
        }

        var tagParts = button.Tag?.ToString()?.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tagParts is null || tagParts.Length != 2 || !int.TryParse(tagParts[1], out var delta))
        {
            return;
        }

        switch (tagParts[0])
        {
            case "Pylonen":
                lap.Pylonen = Math.Max(0, lap.Pylonen + delta);
                break;
            case "Tore":
                lap.Tore = Math.Max(0, lap.Tore + delta);
                break;
            default:
                return;
        }

        RecalculateStoredTrainingLapAndStint(lap);
        await SaveStoredTrainingLapAsync(lap);
    }

    internal async void StoredTrainingLapInvalidCheckBox_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox || checkBox.DataContext is not TrainingStoredStintLapListItem lap)
        {
            return;
        }

        lap.Ungueltig = checkBox.IsChecked == true;
        RecalculateStoredTrainingLapAndStint(lap);
        await SaveStoredTrainingLapAsync(lap);
    }

    private void RecalculateStoredTrainingLapAndStint(TrainingStoredStintLapListItem lap)
    {
        RecalculateStoredTrainingLapPenalty(lap);
        var stint = TrainingStoredStintItems.FirstOrDefault(x => x.StintId == lap.StintId);
        if (stint is not null)
        {
            RecalculateStoredTrainingStintSummary(stint);
        }

        TrainingsViewControl.TrainingStoredStintsItemsControl.Items.Refresh();
    }

    private async Task SaveStoredTrainingLapAsync(TrainingStoredStintLapListItem lap)
    {
        await _trainingStoredLapSaveLock.WaitAsync();
        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var storedLap = await dbContext.Trunden.FirstOrDefaultAsync(x => x.Id == lap.RundenId);
            if (storedLap is null)
            {
                throw new InvalidOperationException($"Gespeicherte Runde #{lap.RundenId} wurde nicht gefunden.");
            }

            storedLap.Pf = lap.Pylonen;
            storedLap.Tf = lap.Tore;
            storedLap.Ungueltig = lap.Ungueltig;
            await dbContext.SaveChangesAsync();

            if (_selectedTrainingDetailId is not null)
            {
                await LoadTrainingFastestLapsAsync(_selectedTrainingDetailId.Value);
            }

            await RefreshSyncStatusAsync();
            if (_selectedTrainingDetailId is not null && IsAutomaticRemoteSyncEnabled(_selectedTrainingDetailId.Value))
            {
                await SynchronizeAsync(reloadData: false);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Gespeicherte Trainingsrunde konnte nicht aktualisiert werden.");
            MessageBox.Show("Die Runde konnte nicht gespeichert werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            if (_selectedTrainingDetailId is not null && _selectedTrainingStoredStintDriverId is not null)
            {
                await LoadTrainingStoredStintsAsync(_selectedTrainingDetailId.Value, _selectedTrainingStoredStintDriverId.Value);
            }
        }
        finally
        {
            _trainingStoredLapSaveLock.Release();
        }
    }

    private void UpdateTrainingStoredStintsEmptyState(string message)
    {
        TrainingsViewControl.TrainingStoredStintsEmptyTextBlock.Text = message;
        TrainingsViewControl.TrainingStoredStintsEmptyTextBlock.Visibility = string.IsNullOrWhiteSpace(message)
            ? Visibility.Collapsed
            : Visibility.Visible;
    }
}
