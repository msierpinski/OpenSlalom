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
using QRCoder;

namespace OpenSlalom.UI;

public partial class MainWindow
{
    private static string ResolveAltersklasse(DateOnly? geburtsdatum, DateOnly? trainingDate, IReadOnlyList<DisziplinAltersklasse> altersklassen)
    {
        if (!geburtsdatum.HasValue || !trainingDate.HasValue || altersklassen.Count == 0)
        {
            return "-";
        }

        var age = trainingDate.Value.Year - geburtsdatum.Value.Year;
        if (trainingDate.Value < geburtsdatum.Value.AddYears(age))
        {
            age--;
        }

        if (age < 0)
        {
            return "-";
        }

        var klasse = altersklassen.FirstOrDefault(x => age >= x.AlterVon && (!x.AlterBis.HasValue || age <= x.AlterBis.Value));
        return string.IsNullOrWhiteSpace(klasse?.Bezeichnung) ? "-" : klasse.Bezeichnung;
    }

    private static string NormalizeAltersklasseSnapshot(string? altersklasse)
    {
        if (string.IsNullOrWhiteSpace(altersklasse) || altersklasse == "-")
        {
            return string.Empty;
        }

        return altersklasse.Trim();
    }

    private static async Task<(DateOnly Zeitpunkt, List<DisziplinAltersklasse> Altersklassen)?> LoadTrainingAltersklassenContextAsync(OpenSlalomDbContext dbContext, int trainingId)
    {
        var trainingMeta = await dbContext.Trainings
            .AsNoTracking()
            .Where(x => x.Id == trainingId)
            .Select(x => new
            {
                x.Zeitpunkt,
                x.DisziplinId
            })
            .FirstOrDefaultAsync();

        if (trainingMeta is null)
        {
            return null;
        }

        var altersklassen = await dbContext.DisziplinAltersklassen
            .AsNoTracking()
            .Where(x => x.DisziplinId == trainingMeta.DisziplinId)
            .OrderBy(x => x.AlterVon)
            .ThenBy(x => x.AlterBis ?? int.MaxValue)
            .ToListAsync();

        return (trainingMeta.Zeitpunkt, altersklassen);
    }

    private async Task LoadTrainingDetailAsync(int trainingId)
    {
        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var training = await dbContext.Trainings
                .AsNoTracking()
                .Include(x => x.Verein)
                .Include(x => x.Disziplin)
                .Include(x => x.Wetter)
                .FirstOrDefaultAsync(x => x.Id == trainingId);

            if (training is null)
            {
                _selectedTrainingTorfehlerPenaltySeconds = 0d;
                _selectedTrainingPylonenfehlerPenaltySeconds = 0d;
                TrainingStarterListItems.Clear();
                TrainingFastestLapItems.Clear();
                TrainingStoredStintDriverItems.Clear();
                TrainingStoredStintItems.Clear();
                UpdateTrainingDriverButtonsState();
                TrainingsViewControl.TrainingDetailTitleTextBlock.Text = "Training nicht gefunden";
                TrainingsViewControl.TrainingDetailSubtitleTextBlock.Text = "Das ausgewaehlte Training ist nicht mehr verfuegbar.";
                TrainingsViewControl.TrainingDetailStatusTextBlock.Text = "Status: -";
                TrainingsViewControl.TrainingDetailZeitpunktTextBlock.Text = "Datum: -";
                TrainingsViewControl.TrainingDetailVereinTextBlock.Text = "Verein: -";
                TrainingsViewControl.TrainingDetailDisziplinTextBlock.Text = "Disziplin: -";
                TrainingsViewControl.TrainingDetailWetterTextBlock.Text = "Wetter: -";
                TrainingsViewControl.TrainingDetailBeschreibungTextBlock.Text = "Beschreibung: -";
                TrainingsViewControl.TrainingDetailQrCodeImage.Source = null;
                ApplyTrainingRoundsToUi();
                return;
            }

            TrainingsViewControl.TrainingDetailTitleTextBlock.Text = training.Name;
            TrainingsViewControl.TrainingDetailSubtitleTextBlock.Text = $"Training #{training.Id}";
            TrainingsViewControl.TrainingDetailStatusTextBlock.Text = $"Status: {(training.TrainingAbgeschlossen ? "Abgeschlossen" : "Offen")}";
            TrainingsViewControl.TrainingDetailZeitpunktTextBlock.Text = $"Datum: {training.Zeitpunkt:dd.MM.yyyy}";
            TrainingsViewControl.TrainingDetailVereinTextBlock.Text = $"Verein: {training.Verein.Vereinsname}";
            TrainingsViewControl.TrainingDetailDisziplinTextBlock.Text = $"Disziplin: {training.Disziplin.Name}";
            TrainingsViewControl.TrainingDetailWetterTextBlock.Text = $"Wetter: {training.Wetter.Bezeichnung}";
            TrainingsViewControl.TrainingDetailBeschreibungTextBlock.Text = $"Beschreibung: {training.Beschreibung}";
            TrainingsViewControl.TrainingDetailQrCodeImage.Source = CreateTrainingQrCodeImage(training.Uuid);
            _selectedTrainingTorfehlerPenaltySeconds = training.Disziplin.ZeitstrafeTorfehler;
            _selectedTrainingPylonenfehlerPenaltySeconds = training.Disziplin.ZeitstrafePylonenfehler;
            RecalculateLapPenaltiesForCurrentContext();
            UpdateTrainingLapSummaryDisplay();
            await LoadTrainingStarterListAsync(training.Id);
            await LoadTrainingFastestLapsAsync(training.Id);
            await LoadTrainingStoredStintDriversAsync(training.Id);
            ApplyTrainingRoundsToUi();
        }
        catch (Exception ex)
        {
            _selectedTrainingTorfehlerPenaltySeconds = 0d;
            _selectedTrainingPylonenfehlerPenaltySeconds = 0d;
            Logger.Error(ex, "Fehler beim Laden der Trainingsdetailansicht.");
            TrainingStarterListItems.Clear();
            TrainingFastestLapItems.Clear();
            TrainingStoredStintDriverItems.Clear();
            TrainingStoredStintItems.Clear();
            UpdateTrainingDriverButtonsState();
            TrainingsViewControl.TrainingDetailTitleTextBlock.Text = "Trainingsdetail nicht verfuegbar";
            TrainingsViewControl.TrainingDetailSubtitleTextBlock.Text = "Fehler beim Laden der Daten.";
            TrainingsViewControl.TrainingDetailStatusTextBlock.Text = "Status: -";
            TrainingsViewControl.TrainingDetailZeitpunktTextBlock.Text = "Datum: -";
            TrainingsViewControl.TrainingDetailVereinTextBlock.Text = "Verein: -";
            TrainingsViewControl.TrainingDetailDisziplinTextBlock.Text = "Disziplin: -";
            TrainingsViewControl.TrainingDetailWetterTextBlock.Text = "Wetter: -";
            TrainingsViewControl.TrainingDetailBeschreibungTextBlock.Text = "Beschreibung: -";
            TrainingsViewControl.TrainingDetailQrCodeImage.Source = null;
            ApplyTrainingRoundsToUi();
        }
    }

    private async Task LoadTrainingStarterListAsync(int trainingId)
    {
        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var trainingContext = await LoadTrainingAltersklassenContextAsync(dbContext, trainingId);
            var altersklassen = trainingContext?.Altersklassen ?? [];
            var trainingDate = trainingContext?.Zeitpunkt;

            var starterRows = await dbContext.FahrerImTrainings
                .AsNoTracking()
                .Where(x => x.TrainingId == trainingId)
                .Include(x => x.Fahrer)
                .ThenInclude(x => x.Verein)
                .OrderBy(x => x.Reihenfolge)
                .ThenBy(x => x.Fahrer.Vorname)
                .ThenBy(x => x.Fahrer.Nachname)
                .Select(x => new
                {
                    FahrerId = x.FahrerId,
                    Reihenfolge = x.Reihenfolge,
                    Vorname = x.Fahrer.Vorname,
                    Nachname = x.Fahrer.Nachname ?? string.Empty,
                    VereinName = x.Fahrer.Verein.Vereinsname,
                    Geburtsdatum = x.Fahrer.Geburtsdatum
                })
                .ToListAsync();

            var starter = starterRows
                .Select(x => new TrainingStarterListItem
                {
                    FahrerId = x.FahrerId,
                    Reihenfolge = x.Reihenfolge,
                    Vorname = x.Vorname,
                    Nachname = x.Nachname,
                    VereinName = x.VereinName,
                    Altersklasse = ResolveAltersklasse(x.Geburtsdatum, trainingDate, altersklassen)
                })
                .ToList();

            if (starter.Count == 0)
            {
                _trainingActiveDriverByTrainingId.Remove(trainingId);
                _trainingSecondActiveDriverByTrainingId.Remove(trainingId);
            }
            else
            {
                foreach (var item in starter)
                {
                    if (_trainingDriverEnabledByDriver.TryGetValue((trainingId, item.FahrerId), out var enabled))
                    {
                        item.FahrerFaehrt = enabled;
                    }
                    else
                    {
                        item.FahrerFaehrt = true;
                    }

                    if (_trainingKartSelectionByDriver.TryGetValue((trainingId, item.FahrerId), out var selectedKartId))
                    {
                        item.KartId = selectedKartId;
                    }
                }

                var enabledStarter = starter.Where(x => x.FahrerFaehrt).ToList();
                var reservedSecondDriverId = IsSecondTrainingTimingEnabled(trainingId) &&
                                             _trainingSecondActiveDriverByTrainingId.TryGetValue(trainingId, out var reservedSecondId) &&
                                             enabledStarter.Any(x => x.FahrerId == reservedSecondId)
                    ? reservedSecondId
                    : (int?)null;
                if (!_trainingActiveDriverByTrainingId.TryGetValue(trainingId, out var activeFahrerId) ||
                    enabledStarter.All(x => x.FahrerId != activeFahrerId))
                {
                    var replacement = enabledStarter.FirstOrDefault(x => x.FahrerId != reservedSecondDriverId);
                    if (replacement is not null)
                    {
                        activeFahrerId = replacement.FahrerId;
                        _trainingActiveDriverByTrainingId[trainingId] = activeFahrerId;
                    }
                    else
                    {
                        _trainingActiveDriverByTrainingId.Remove(trainingId);
                    }
                }

                if (!IsSecondTrainingTimingEnabled(trainingId) ||
                    !_trainingSecondActiveDriverByTrainingId.TryGetValue(trainingId, out var secondActiveFahrerId) ||
                    enabledStarter.All(x => x.FahrerId != secondActiveFahrerId) ||
                    secondActiveFahrerId == activeFahrerId)
                {
                    _trainingSecondActiveDriverByTrainingId.Remove(trainingId);
                }

                for (var i = 0; i < starter.Count; i++)
                {
                    var item = starter[i];
                    item.Nummer = i + 1;
                    item.IsAktiv = _trainingActiveDriverByTrainingId.TryGetValue(trainingId, out var currentActiveId) && item.FahrerId == currentActiveId;
                    item.IsAktivZweiteZeitnahme = _trainingSecondActiveDriverByTrainingId.TryGetValue(trainingId, out var currentSecondActiveId) && item.FahrerId == currentSecondActiveId;
                }
            }

            TrainingStarterListItems.Clear();
            foreach (var item in starter)
            {
                TrainingStarterListItems.Add(item);
            }

            UpdateTrainingDriverButtonsState();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Laden der Starterliste.");
            TrainingStarterListItems.Clear();
            UpdateTrainingDriverButtonsState();
        }
    }

    private async Task LoadTrainingFastestLapsAsync(int trainingId)
    {
        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var penalties = await dbContext.Trainings
                .AsNoTracking()
                .Where(x => x.Id == trainingId)
                .Select(x => new
                {
                    Torfehler = x.Disziplin.ZeitstrafeTorfehler,
                    Pylonenfehler = x.Disziplin.ZeitstrafePylonenfehler
                })
                .FirstOrDefaultAsync();

            var torfehlerPenalty = penalties?.Torfehler ?? 0d;
            var pylonenfehlerPenalty = penalties?.Pylonenfehler ?? 0d;

            var lapRows = await dbContext.Trunden
                .AsNoTracking()
                .Where(x => x.Tstint != null && x.Tstint.TrainingId == trainingId)
                .Select(x => new
                {
                    FahrerId = x.Tstint!.FahrerId,
                    x.Tstint.Fahrer.Vorname,
                    Nachname = x.Tstint.Fahrer.Nachname ?? string.Empty,
                    AltersklasseSnapshot = x.Tstint.AltersklasseSnapshot,
                    KartName = x.Tstint.Kart != null ? x.Tstint.Kart.Name : null,
                    Zeitpunkt = x.Tstint.Datum,
                    Runde = x.Runde ?? 0,
                    Rundenzeit = x.Rundenzeit,
                    Pylonen = x.Pf ?? 0,
                    Tore = x.Tf ?? 0,
                    x.Ungueltig
                })
                .ToListAsync();

            var perDriver = lapRows
                .GroupBy(x => x.FahrerId)
                .Select(group =>
                {
                    var validLaps = group
                        .Where(x => !x.Ungueltig && x.Rundenzeit.HasValue && x.Rundenzeit.Value > 0)
                        .Select(x => new
                        {
                            Row = x,
                            PenaltySeconds = (x.Tore * torfehlerPenalty) + (x.Pylonen * pylonenfehlerPenalty),
                        })
                        .Select(x => new
                        {
                            x.Row,
                            PenaltySeconds = Math.Max(0d, x.PenaltySeconds),
                            EffectiveSeconds = x.Row.Rundenzeit!.Value + Math.Max(0d, x.PenaltySeconds)
                        })
                        .OrderBy(x => x.EffectiveSeconds)
                        .ThenBy(x => x.Row.Zeitpunkt)
                        .ToList();

                    if (validLaps.Count == 0)
                    {
                        return null;
                    }

                    var fastest = validLaps[0];
                    var fahrerName = string.IsNullOrWhiteSpace(fastest.Row.Nachname)
                        ? fastest.Row.Vorname
                        : $"{fastest.Row.Vorname} {fastest.Row.Nachname}";

                    return new
                    {
                        fastest.EffectiveSeconds,
                        Fahrer = fahrerName,
                        Altersklasse = string.IsNullOrWhiteSpace(fastest.Row.AltersklasseSnapshot) ? "-" : fastest.Row.AltersklasseSnapshot,
                        Kart = string.IsNullOrWhiteSpace(fastest.Row.KartName) ? "-" : fastest.Row.KartName!,
                        RundenzeitText = FormatTrainingTime(TimeSpan.FromSeconds(fastest.EffectiveSeconds)),
                        StrafenText = $"{fastest.Row.Pylonen}P {fastest.Row.Tore}T (+{FormatSecondsValue(fastest.PenaltySeconds)}s)",
                        ZeitpunktText = fastest.Row.Zeitpunkt.ToString("HH:mm:ss"),
                        Runden = group.Count()
                    };
                })
                .Where(x => x is not null)
                .Select(x => x!)
                .OrderBy(x => x.EffectiveSeconds)
                .ThenBy(x => x.Fahrer)
                .ToList();

            TrainingFastestLapItems.Clear();
            if (perDriver.Count == 0)
            {
                return;
            }

            var bestSeconds = perDriver[0].EffectiveSeconds;
            for (var i = 0; i < perDriver.Count; i++)
            {
                var row = perDriver[i];
                var diff = row.EffectiveSeconds - bestSeconds;

                TrainingFastestLapItems.Add(new TrainingFastestLapListItem
                {
                    Position = i + 1,
                    Fahrer = row.Fahrer,
                    Altersklasse = row.Altersklasse,
                    Kart = row.Kart,
                    RundenzeitText = row.RundenzeitText,
                    DiffText = i == 0 ? "-" : $"+{FormatTrainingTime(TimeSpan.FromSeconds(diff))}",
                    StrafenText = row.StrafenText,
                    ZeitpunktText = row.ZeitpunktText,
                    Runden = row.Runden
                });
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Laden der schnellsten Runden pro Fahrer.");
            TrainingFastestLapItems.Clear();
        }
    }

    internal async void SkipTrainingDriver_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedTrainingDetailId is null || TrainingStarterListItems.Count == 0)
        {
            return;
        }

        var station = GetAvailableTrainingTimingStation();
        if (station == 0)
        {
            return;
        }

        var ordered = TrainingStarterListItems
            .Where(x => x.FahrerFaehrt)
            .OrderBy(x => x.Reihenfolge)
            .ThenBy(x => x.FahrerId)
            .ToList();

        if (ordered.Count == 0)
        {
            return;
        }

        var referenceDriver = GetActiveTrainingDriver(station) ?? GetActiveTrainingDriver(1);
        var referenceIndex = referenceDriver is null ? -1 : ordered.FindIndex(x => x.FahrerId == referenceDriver.FahrerId);
        for (var offset = 1; offset <= ordered.Count; offset++)
        {
            var candidateIndex = (referenceIndex + offset + ordered.Count) % ordered.Count;
            var candidate = ordered[candidateIndex];
            if (candidate.IsAktiv || candidate.IsAktivZweiteZeitnahme)
            {
                continue;
            }

            await SwitchTrainingDriverAsync(candidate.FahrerId, station);
            return;
        }
    }

    private static BitmapImage CreateTrainingQrCodeImage(Guid trainingUuid)
    {
        var url = $"https://www.openslalom.de/training/{trainingUuid:D}";
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(10);
        var image = new BitmapImage();
        using var stream = new MemoryStream(png);
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    internal async void TrainingStarterRow_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGridRow row || row.DataContext is not TrainingStarterListItem targetDriver)
        {
            return;
        }

        for (var source = e.OriginalSource as DependencyObject; source is not null && source != row; source = GetTrainingStarterParent(source))
        {
            if (source is CheckBox or ComboBox)
            {
                return;
            }
        }

        e.Handled = true;
        if (targetDriver.IsAktiv || targetDriver.IsAktivZweiteZeitnahme)
        {
            return;
        }

        var station = GetAvailableTrainingTimingStation();
        if (station > 0)
        {
            await SwitchTrainingDriverAsync(targetDriver.FahrerId, station);
        }
    }

    internal void TrainingStarterDataGrid_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _trainingStarterDraggedItem = null;
        _trainingStarterDragOriginalIndex = -1;
        if (sender is not DataGrid dataGrid || e.OriginalSource is not DependencyObject source)
        {
            return;
        }

        var row = ItemsControl.ContainerFromElement(dataGrid, source) as DataGridRow;
        if (row?.DataContext is not TrainingStarterListItem item || IsTrainingStarterInteractiveControl(source, row))
        {
            return;
        }

        _trainingStarterDragStartPoint = e.GetPosition(dataGrid);
        _trainingStarterDraggedItem = item;
        _trainingStarterDragOriginalIndex = TrainingStarterListItems.IndexOf(item);
    }

    internal void TrainingStarterDataGrid_OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not DataGrid dataGrid ||
            e.LeftButton != MouseButtonState.Pressed ||
            _trainingStarterDraggedItem is null ||
            _trainingStarterOrderSaveInProgress)
        {
            return;
        }

        var currentPosition = e.GetPosition(dataGrid);
        if (Math.Abs(currentPosition.X - _trainingStarterDragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(currentPosition.Y - _trainingStarterDragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var draggedItem = _trainingStarterDraggedItem;
        _trainingStarterDraggedItem = null;
        draggedItem.IsBeingDragged = true;
        dataGrid.Items.Refresh();

        var result = DragDrop.DoDragDrop(dataGrid, draggedItem, DragDropEffects.Move);
        draggedItem.IsBeingDragged = false;

        if (result != DragDropEffects.Move)
        {
            var currentIndex = TrainingStarterListItems.IndexOf(draggedItem);
            if (currentIndex >= 0 && _trainingStarterDragOriginalIndex >= 0 && currentIndex != _trainingStarterDragOriginalIndex)
            {
                TrainingStarterListItems.Move(currentIndex, _trainingStarterDragOriginalIndex);
            }

            UpdateTrainingStarterDisplayOrder(updatePersistentOrder: false);
        }

        _trainingStarterDragOriginalIndex = -1;
        dataGrid.Items.Refresh();
    }

    internal void TrainingStarterDataGrid_OnDragOver(object sender, DragEventArgs e)
    {
        if (_trainingStarterOrderSaveInProgress ||
            sender is not DataGrid dataGrid ||
            !e.Data.GetDataPresent(typeof(TrainingStarterListItem)) ||
            e.OriginalSource is not DependencyObject source ||
            ItemsControl.ContainerFromElement(dataGrid, source) is not DataGridRow targetRow ||
            targetRow.DataContext is not TrainingStarterListItem targetItem ||
            e.Data.GetData(typeof(TrainingStarterListItem)) is not TrainingStarterListItem draggedItem)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        var currentIndex = TrainingStarterListItems.IndexOf(draggedItem);
        var targetIndex = TrainingStarterListItems.IndexOf(targetItem);
        if (currentIndex >= 0 && targetIndex >= 0 && currentIndex != targetIndex)
        {
            TrainingStarterListItems.Move(currentIndex, targetIndex);
            UpdateTrainingStarterDisplayOrder(updatePersistentOrder: false);
            dataGrid.Items.Refresh();
        }

        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    internal async void TrainingStarterDataGrid_OnDrop(object sender, DragEventArgs e)
    {
        if (_trainingStarterOrderSaveInProgress ||
            _selectedTrainingDetailId is null ||
            sender is not DataGrid dataGrid ||
            e.Data.GetData(typeof(TrainingStarterListItem)) is not TrainingStarterListItem draggedItem ||
            e.OriginalSource is not DependencyObject source ||
            ItemsControl.ContainerFromElement(dataGrid, source) is not DataGridRow)
        {
            return;
        }

        var currentIndex = TrainingStarterListItems.IndexOf(draggedItem);
        if (currentIndex < 0)
        {
            return;
        }

        var trainingId = _selectedTrainingDetailId.Value;
        _trainingStarterOrderSaveInProgress = true;
        UpdateTrainingStarterDisplayOrder(updatePersistentOrder: true);
        dataGrid.Items.Refresh();
        e.Effects = DragDropEffects.Move;
        e.Handled = true;

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var assignments = await dbContext.FahrerImTrainings
                .Where(x => x.TrainingId == trainingId)
                .ToDictionaryAsync(x => x.FahrerId);

            foreach (var item in TrainingStarterListItems)
            {
                if (assignments.TryGetValue(item.FahrerId, out var assignment))
                {
                    assignment.Reihenfolge = item.Reihenfolge;
                }
            }

            await dbContext.SaveChangesAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Reihenfolge der Trainingsfahrer konnte nicht gespeichert werden.");
            MessageBox.Show("Die neue Fahrerreihenfolge konnte nicht gespeichert werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            if (_selectedTrainingDetailId == trainingId)
            {
                await LoadTrainingStarterListAsync(trainingId);
            }
        }
        finally
        {
            _trainingStarterOrderSaveInProgress = false;
        }
    }

    private void UpdateTrainingStarterDisplayOrder(bool updatePersistentOrder)
    {
        for (var index = 0; index < TrainingStarterListItems.Count; index++)
        {
            TrainingStarterListItems[index].Nummer = index + 1;
            if (updatePersistentOrder)
            {
                TrainingStarterListItems[index].Reihenfolge = index + 1;
            }
        }
    }

    private static bool IsTrainingStarterInteractiveControl(DependencyObject source, DataGridRow row)
    {
        for (var current = source; current is not null && current != row; current = GetTrainingStarterParent(current))
        {
            if (current is CheckBox or ComboBox)
            {
                return true;
            }
        }

        return false;
    }

    private static DependencyObject? GetTrainingStarterParent(DependencyObject element)
    {
        if (element is Visual)
        {
            return VisualTreeHelper.GetParent(element);
        }

        if (element is FrameworkContentElement contentElement)
        {
            return contentElement.Parent ?? ContentOperations.GetParent(contentElement);
        }

        return LogicalTreeHelper.GetParent(element);
    }

    internal async void FinishTraining_OnClick(object sender, RoutedEventArgs e)
    {
        if (_finishTrainingInProgress)
        {
            return;
        }

        if (_selectedTrainingDetailId is null)
        {
            return;
        }

        _finishTrainingInProgress = true;
        UpdateTrainingDriverButtonsState();

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var training = await dbContext.Trainings.FirstOrDefaultAsync(x => x.Id == _selectedTrainingDetailId.Value);
            if (training is null)
            {
                return;
            }

            if (!training.TrainingAbgeschlossen)
            {
                training.TrainingAbgeschlossen = true;
                await dbContext.SaveChangesAsync();
            }

            await RefreshSyncStatusAsync();
            NavigateTo("Trainings");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Training konnte nicht als abgeschlossen markiert werden.");
            MessageBox.Show("Training konnte nicht beendet werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _finishTrainingInProgress = false;
            UpdateTrainingDriverButtonsState();
        }
    }

    private TrainingStarterListItem? GetActiveTrainingDriver(int station)
    {
        return station == 2
            ? TrainingStarterListItems.FirstOrDefault(x => x.IsAktivZweiteZeitnahme && x.FahrerFaehrt)
            : TrainingStarterListItems.FirstOrDefault(x => x.IsAktiv && x.FahrerFaehrt);
    }

    private bool IsCurrentStintFinished(int station = 1)
    {
        if (_selectedTrainingDetailId is null)
        {
            return false;
        }

        var activeDriver = GetActiveTrainingDriver(station);
        if (activeDriver is null)
        {
            return false;
        }

        var context = (_selectedTrainingDetailId.Value, activeDriver.FahrerId);
        if (!_trainingStintsByDriver.TryGetValue(context, out var state) || state.Stopwatch.IsRunning)
        {
            return false;
        }

        var roundsTarget = GetRoundsTargetForTraining(_selectedTrainingDetailId.Value);
        var canExceedRoundsTarget = CanExceedRoundsTargetForTraining(_selectedTrainingDetailId.Value);
        return state.IsFinished ||
               (!canExceedRoundsTarget && roundsTarget > 0 && state.LapRecords.Count >= roundsTarget);
    }

    private bool IsCurrentStintNotStarted(int station = 1)
    {
        if (_selectedTrainingDetailId is null)
        {
            return false;
        }

        var activeDriver = GetActiveTrainingDriver(station);
        if (activeDriver is null)
        {
            return false;
        }

        var context = (_selectedTrainingDetailId.Value, activeDriver.FahrerId);
        return !_trainingStintsByDriver.TryGetValue(context, out var state) ||
               (!state.IsFinished && !state.Stopwatch.IsRunning && state.Stopwatch.Elapsed == TimeSpan.Zero && state.LapRecords.Count == 0);
    }

    private bool CanReplaceTrainingTimingDriver(int station)
    {
        return GetActiveTrainingDriver(station) is null || IsCurrentStintNotStarted(station) || IsCurrentStintFinished(station);
    }

    private int GetAvailableTrainingTimingStation()
    {
        if (CanReplaceTrainingTimingDriver(1))
        {
            return 1;
        }

        if (_selectedTrainingDetailId is not null &&
            IsSecondTrainingTimingEnabled(_selectedTrainingDetailId.Value) &&
            CanReplaceTrainingTimingDriver(2))
        {
            return 2;
        }

        return 0;
    }

    private int GetNextDriverTrainingTimingStation()
    {
        if (IsCurrentStintFinished(1))
        {
            return 1;
        }

        if (GetActiveTrainingDriver(1) is null)
        {
            return 1;
        }

        if (_selectedTrainingDetailId is not null &&
            IsSecondTrainingTimingEnabled(_selectedTrainingDetailId.Value) &&
            (IsCurrentStintFinished(2) || GetActiveTrainingDriver(2) is null))
        {
            return 2;
        }

        return 0;
    }

    private TrainingStarterListItem? GetTimingStationReferenceDriver(int station)
    {
        var activeDriver = GetActiveTrainingDriver(station);
        if (activeDriver is not null || _selectedTrainingDetailId is null)
        {
            return activeDriver;
        }

        return _trainingLastDriverByTimingStation.TryGetValue((_selectedTrainingDetailId.Value, station), out var driverId)
            ? TrainingStarterListItems.FirstOrDefault(x => x.FahrerId == driverId)
            : null;
    }

    private bool HasNextAssignableTrainingDriver(int station)
    {
        var referenceDriverId = GetTimingStationReferenceDriver(station)?.FahrerId;
        return TrainingStarterListItems.Any(x =>
            x.FahrerFaehrt &&
            !x.IsAktiv &&
            !x.IsAktivZweiteZeitnahme &&
            x.FahrerId != referenceDriverId);
    }

    private bool CanDisableSecondTrainingTiming()
    {
        return GetActiveTrainingDriver(2) is null || IsCurrentStintNotStarted(2);
    }

    internal async void NextTrainingDriver_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedTrainingDetailId is null)
        {
            return;
        }

        var station = GetNextDriverTrainingTimingStation();
        if (station == 0)
        {
            return;
        }

        var ordered = TrainingStarterListItems
            .Where(x => x.FahrerFaehrt)
            .OrderBy(x => x.Reihenfolge)
            .ThenBy(x => x.FahrerId)
            .ToList();

        if (ordered.Count == 0)
        {
            return;
        }

        var referenceDriver = GetTimingStationReferenceDriver(station) ?? GetActiveTrainingDriver(1);
        var currentIndex = referenceDriver is null ? -1 : ordered.FindIndex(x => x.FahrerId == referenceDriver.FahrerId);
        if (currentIndex < 0)
        {
            return;
        }

        for (var offset = 1; offset <= ordered.Count; offset++)
        {
            var candidate = ordered[(currentIndex + offset) % ordered.Count];
            if (candidate.IsAktiv || candidate.IsAktivZweiteZeitnahme)
            {
                continue;
            }

            if (candidate.FahrerId == referenceDriver?.FahrerId)
            {
                continue;
            }

            await SwitchTrainingDriverAsync(candidate.FahrerId, station);
            return;
        }
    }

    private async Task SwitchTrainingDriverAsync(int targetDriverId, int station = 1)
    {
        if (_nextDriverSwitchInProgress || _selectedTrainingDetailId is null)
        {
            return;
        }

        var targetDriver = TrainingStarterListItems.FirstOrDefault(x => x.FahrerId == targetDriverId && x.FahrerFaehrt);
        var currentDriver = GetActiveTrainingDriver(station);
        var targetUsedByOtherStation = station == 1 ? targetDriver?.IsAktivZweiteZeitnahme == true : targetDriver?.IsAktiv == true;
        if (targetDriver is null || targetUsedByOtherStation || targetDriver.FahrerId == currentDriver?.FahrerId)
        {
            return;
        }

        _nextDriverSwitchInProgress = true;
        UpdateTrainingDriverButtonsState();

        try
        {
            var lapGrid = station == 2 ? TrainingsViewControl.TrainingSecondLapTimesDataGrid : TrainingsViewControl.TrainingLapTimesDataGrid;
            lapGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            lapGrid.CommitEdit(DataGridEditingUnit.Row, true);

            var trainingId = _selectedTrainingDetailId.Value;
            (int TrainingId, int FahrerId)? currentContext = currentDriver is null ? null : (trainingId, currentDriver.FahrerId);
            TrainingStintState? currentState = null;
            if (currentContext is not null)
            {
                _trainingStintsByDriver.TryGetValue(currentContext.Value, out currentState);
            }

            var stintNotStarted = currentDriver is null || currentState is null ||
                                  (!currentState.IsFinished && !currentState.Stopwatch.IsRunning && currentState.Stopwatch.Elapsed == TimeSpan.Zero && currentState.LapRecords.Count == 0);
            var roundsTarget = GetRoundsTargetForTraining(trainingId);
            var canExceedRoundsTarget = CanExceedRoundsTargetForTraining(trainingId);
            var stintFinished = currentState is not null &&
                                !currentState.Stopwatch.IsRunning &&
                                (currentState.IsFinished ||
                                 (!canExceedRoundsTarget && roundsTarget > 0 && currentState.LapRecords.Count >= roundsTarget));

            if (!stintNotStarted && !stintFinished)
            {
                return;
            }

            if (stintFinished)
            {
                await SaveTrainingStintAsync(trainingId, currentDriver!, currentState!);
            }

            if (currentContext is not null)
            {
                _trainingStintsByDriver.Remove(currentContext.Value);
            }
            _trainingStintsByDriver.Remove((trainingId, targetDriver.FahrerId));

            if (station == 2)
            {
                _trainingSecondActiveDriverByTrainingId[trainingId] = targetDriver.FahrerId;
            }
            else
            {
                _trainingActiveDriverByTrainingId[trainingId] = targetDriver.FahrerId;
            }
            foreach (var item in TrainingStarterListItems)
            {
                if (station == 2) item.IsAktivZweiteZeitnahme = item.FahrerId == targetDriver.FahrerId;
                else item.IsAktiv = item.FahrerId == targetDriver.FahrerId;
            }

            TrainingsViewControl.TrainingStarterDataGrid.Items.Refresh();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Stint konnte nicht gespeichert werden.");
            MessageBox.Show("Stint konnte nicht gespeichert werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _nextDriverSwitchInProgress = false;
            UpdateTrainingDriverButtonsState();
        }
    }

    internal async void SaveFirstTrainingStint_OnClick(object sender, RoutedEventArgs e)
    {
        await SaveAndReleaseTrainingTimingStationAsync(1);
    }

    internal async void SaveSecondTrainingStint_OnClick(object sender, RoutedEventArgs e)
    {
        await SaveAndReleaseTrainingTimingStationAsync(2);
    }

    private async Task SaveAndReleaseTrainingTimingStationAsync(int station)
    {
        if (_nextDriverSwitchInProgress || _selectedTrainingDetailId is null || !IsCurrentStintFinished(station))
        {
            return;
        }

        var driver = GetActiveTrainingDriver(station);
        if (driver is null)
        {
            return;
        }

        var trainingId = _selectedTrainingDetailId.Value;
        var context = (trainingId, driver.FahrerId);
        if (!_trainingStintsByDriver.TryGetValue(context, out var state) || state.Stopwatch.IsRunning)
        {
            return;
        }

        _nextDriverSwitchInProgress = true;
        UpdateTrainingDriverButtonsState();
        try
        {
            var lapGrid = station == 2 ? TrainingsViewControl.TrainingSecondLapTimesDataGrid : TrainingsViewControl.TrainingLapTimesDataGrid;
            lapGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            lapGrid.CommitEdit(DataGridEditingUnit.Row, true);

            await SaveTrainingStintAsync(trainingId, driver, state);
            _trainingLastDriverByTimingStation[(trainingId, station)] = driver.FahrerId;
            _trainingStintsByDriver.Remove(context);

            if (station == 2)
            {
                _trainingSecondActiveDriverByTrainingId.Remove(trainingId);
                driver.IsAktivZweiteZeitnahme = false;
            }
            else
            {
                _trainingActiveDriverByTrainingId.Remove(trainingId);
                driver.IsAktiv = false;
            }

            TrainingsViewControl.TrainingStarterDataGrid.Items.Refresh();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Stint konnte nicht gespeichert und die Zeitnahme nicht freigegeben werden.");
            MessageBox.Show("Stint konnte nicht gespeichert werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _nextDriverSwitchInProgress = false;
            UpdateTrainingDriverButtonsState();
        }
    }

    private async Task SaveTrainingStintAsync(int trainingId, TrainingStarterListItem driver, TrainingStintState state)
    {
        await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
        var stint = new Tstint
        {
            TrainingId = trainingId,
            FahrerId = driver.FahrerId,
            KartId = driver.KartId,
            AltersklasseSnapshot = NormalizeAltersklasseSnapshot(driver.Altersklasse),
            Datum = DateTime.Now
        };
        dbContext.Tstints.Add(stint);

        foreach (var lap in state.LapRecords.OrderBy(x => x.Nummer))
        {
            dbContext.Trunden.Add(new Trunde
            {
                Tstint = stint,
                Runde = lap.Nummer,
                Rundenzeit = lap.Rundenzeit.TotalSeconds,
                Pf = lap.Pylonen,
                Tf = lap.Tore,
                Ungueltig = lap.Ungueltig
            });
        }

        await dbContext.SaveChangesAsync();
        await LoadTrainingFastestLapsAsync(trainingId);
        await LoadTrainingStoredStintDriversAsync(trainingId);
        await RefreshSyncStatusAsync();

        if (IsAutomaticRemoteSyncEnabled(trainingId))
        {
            await SynchronizeAsync(reloadData: false);
        }
    }

    internal void TrainingStarterFaehrtCheckBox_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedTrainingDetailId is null || sender is not CheckBox checkBox || checkBox.DataContext is not TrainingStarterListItem selected)
        {
            return;
        }

        var willDrive = checkBox.IsChecked == true;
        var activeStation = selected.IsAktiv ? 1 : selected.IsAktivZweiteZeitnahme ? 2 : 0;
        if (!willDrive && activeStation > 0 && !IsCurrentStintNotStarted(activeStation))
        {
            checkBox.IsChecked = true;
            selected.FahrerFaehrt = true;
            MessageBox.Show("Ein bereits begonnener oder beendeter Stint muss vor dem Abwählen des Fahrers gespeichert oder gelöscht werden.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        selected.FahrerFaehrt = willDrive;
        _trainingDriverEnabledByDriver[(_selectedTrainingDetailId.Value, selected.FahrerId)] = selected.FahrerFaehrt;
        if (!selected.FahrerFaehrt && selected.IsAktivZweiteZeitnahme)
        {
            _trainingSecondActiveDriverByTrainingId.Remove(_selectedTrainingDetailId.Value);
            selected.IsAktivZweiteZeitnahme = false;
        }

        var enabledOrdered = TrainingStarterListItems
            .Where(x => x.FahrerFaehrt)
            .OrderBy(x => x.Reihenfolge)
            .ThenBy(x => x.FahrerId)
            .ToList();

        if (enabledOrdered.Count == 0)
        {
            _trainingActiveDriverByTrainingId.Remove(_selectedTrainingDetailId.Value);
            _trainingSecondActiveDriverByTrainingId.Remove(_selectedTrainingDetailId.Value);
            foreach (var item in TrainingStarterListItems)
            {
                item.IsAktiv = false;
                item.IsAktivZweiteZeitnahme = false;
            }

            TrainingsViewControl.TrainingStarterDataGrid.Items.Refresh();
            UpdateTrainingDriverButtonsState();
            return;
        }

        if (!_trainingActiveDriverByTrainingId.TryGetValue(_selectedTrainingDetailId.Value, out var activeId) ||
            enabledOrdered.All(x => x.FahrerId != activeId))
        {
            var replacement = enabledOrdered.FirstOrDefault(x => !x.IsAktivZweiteZeitnahme);
            activeId = replacement?.FahrerId ?? 0;
            if (replacement is null) _trainingActiveDriverByTrainingId.Remove(_selectedTrainingDetailId.Value);
            else _trainingActiveDriverByTrainingId[_selectedTrainingDetailId.Value] = activeId;
        }

        foreach (var item in TrainingStarterListItems)
        {
            item.IsAktiv = item.FahrerId == activeId;
        }

        TrainingsViewControl.TrainingStarterDataGrid.Items.Refresh();
        UpdateTrainingDriverButtonsState();
    }

    internal void TrainingStarterKartComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_selectedTrainingDetailId is null || sender is not ComboBox comboBox || comboBox.DataContext is not TrainingStarterListItem item)
        {
            return;
        }

        item.KartId = comboBox.SelectedValue as int?;
        _trainingKartSelectionByDriver[(_selectedTrainingDetailId.Value, item.FahrerId)] = item.KartId;

        if (comboBox.IsKeyboardFocusWithin)
        {
            comboBox.Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
            {
                comboBox.IsDropDownOpen = false;
                Keyboard.ClearFocus();
                TrainingsViewControl.TrainingStarterDataGrid.Focus();
            });
        }
    }

    private void UpdateTrainingDriverButtonsState()
    {
        var hasStarter = TrainingStarterListItems.Count > 0;
        var hasEnabled = TrainingStarterListItems.Any(x => x.FahrerFaehrt);
        var stopwatchRunning = IsCurrentTrainingStopwatchRunning();
        var secondStopwatchRunning = IsSecondTrainingStopwatchRunning();
        var assignableStation = GetAvailableTrainingTimingStation();
        var hasUnassignedDriver = TrainingStarterListItems.Any(x => x.FahrerFaehrt && !x.IsAktiv && !x.IsAktivZweiteZeitnahme);
        var nextDriverStation = GetNextDriverTrainingTimingStation();
        var canSwitchToNextDriver = !_nextDriverSwitchInProgress &&
                                    hasStarter &&
                                    nextDriverStation > 0 &&
                                    HasNextAssignableTrainingDriver(nextDriverStation);
        var canSkipDriver = !_nextDriverSwitchInProgress && hasStarter && hasEnabled && assignableStation > 0 && hasUnassignedDriver;
        var canFinishTraining = !_finishTrainingInProgress && _selectedTrainingDetailId is not null && !stopwatchRunning && !secondStopwatchRunning;

        TrainingsViewControl.NextDriverButton.IsEnabled = canSwitchToNextDriver;
        TrainingsViewControl.NextDriverButton.Background = canSwitchToNextDriver
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F84DE"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));

        TrainingsViewControl.SkipDriverButton.IsEnabled = canSkipDriver;
        TrainingsViewControl.SkipDriverButton.Background = canSkipDriver
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));

        TrainingsViewControl.FinishTrainingButton.IsEnabled = canFinishTraining;
        TrainingsViewControl.FinishTrainingButton.Background = canFinishTraining
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));

        UpdateActiveDriverDisplay();
        UpdateSecondTrainingStopwatchContextWithActiveDriver();
    }

    private bool IsCurrentTrainingStopwatchRunning()
    {
        SyncTrainingStopwatchContextWithActiveDriver(resetIfContextChanges: false);
        UpdateSecondTrainingStopwatchContextWithActiveDriver();
        if (_trainingStopwatchContext is null)
        {
            return false;
        }

        return _trainingStintsByDriver.TryGetValue(_trainingStopwatchContext.Value, out var state) && state.Stopwatch.IsRunning;
    }

    private void UpdateActiveDriverDisplay()
    {
        var activeDriver = TrainingStarterListItems.FirstOrDefault(x => x.IsAktiv);
        if (activeDriver is null)
        {
            TrainingsViewControl.TrainingActiveDriverTextBlock.Text = "-";
            SyncTrainingStopwatchContextWithActiveDriver(resetIfContextChanges: false);
            return;
        }

        TrainingsViewControl.TrainingActiveDriverTextBlock.Text = string.IsNullOrWhiteSpace(activeDriver.Nachname)
            ? activeDriver.Vorname
            : $"{activeDriver.Vorname} {activeDriver.Nachname}";
        SyncTrainingStopwatchContextWithActiveDriver(resetIfContextChanges: false);
    }

    internal async void OpenAddTrainingFahrerDialog_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedTrainingDetailId is null)
        {
            MessageBox.Show("Bitte zuerst ein Training auswaehlen.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();

            var assignedDrivers = await dbContext.FahrerImTrainings
                .AsNoTracking()
                .Where(x => x.TrainingId == _selectedTrainingDetailId.Value)
                .Select(x => new { x.FahrerId, x.Reihenfolge })
                .ToListAsync();

            var assignedOrderMap = assignedDrivers.ToDictionary(x => x.FahrerId, x => x.Reihenfolge);

            var availableDrivers = await dbContext.Fahrer
                .AsNoTracking()
                .Include(x => x.Verein)
                .OrderBy(x => x.Vorname)
                .ThenBy(x => x.Nachname)
                .Select(x => new
                {
                    x.Id,
                    DisplayName = string.IsNullOrWhiteSpace(x.Nachname)
                        ? $"{x.Vorname} ({x.Verein.Vereinsname})"
                        : $"{x.Vorname} {x.Nachname} ({x.Verein.Vereinsname})"
                })
                .ToListAsync();

            TrainingDriverSelectionItems.Clear();
            foreach (var driver in availableDrivers)
            {
                TrainingDriverSelectionItems.Add(new TrainingDriverSelectionItem
                {
                    FahrerId = driver.Id,
                    DisplayName = driver.DisplayName,
                    IsSelected = assignedOrderMap.ContainsKey(driver.Id),
                    SelectionOrder = assignedOrderMap.TryGetValue(driver.Id, out var reihenfolge) ? reihenfolge : 0
                });
            }

            _trainingDriverSearchTerm = string.Empty;
            _trainingDriverSelectionOrderCounter = TrainingDriverSelectionItems
                .Select(x => x.SelectionOrder)
                .DefaultIfEmpty(0)
                .Max();
            TrainingsViewControl.TrainingFahrerSearchTextBox.Text = string.Empty;
            ApplyTrainingDriverSelectionFilter();
            TrainingsViewControl.TrainingFahrerDialogOverlay.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Laden der verfuegbaren Fahrer fuer das Training.");
            MessageBox.Show("Fahrer konnten nicht geladen werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    internal async void SaveAddTrainingFahrerDialog_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedTrainingDetailId is null)
        {
            CloseTrainingDriverSelectionDialog();
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();

            var existingAssignments = await dbContext.FahrerImTrainings
                .IgnoreQueryFilters()
                .Where(x => x.TrainingId == _selectedTrainingDetailId.Value)
                .ToListAsync();

            var existingMap = existingAssignments.ToDictionary(x => x.FahrerId);
            var selectedInOrder = TrainingDriverSelectionItems
                .Where(x => x.IsSelected)
                .OrderBy(x => x.SelectionOrder)
                .ThenBy(x => x.DisplayName)
                .ToList();

            var reihenfolge = 1;
            foreach (var selected in selectedInOrder)
            {
                existingMap.TryGetValue(selected.FahrerId, out var existing);

                if (existing is null)
                {
                    dbContext.FahrerImTrainings.Add(new FahrerImTraining
                    {
                        TrainingId = _selectedTrainingDetailId.Value,
                        FahrerId = selected.FahrerId,
                        Reihenfolge = reihenfolge
                    });
                    reihenfolge++;
                    continue;
                }

                if (existing.IsDeleted)
                {
                    existing.IsDeleted = false;
                    existing.DeletedAtUtc = null;
                }

                existing.Reihenfolge = reihenfolge;
                reihenfolge++;
            }

            foreach (var selection in TrainingDriverSelectionItems)
            {
                existingMap.TryGetValue(selection.FahrerId, out var existing);

                if (!selection.IsSelected && existing is not null && !existing.IsDeleted)
                {
                    dbContext.FahrerImTrainings.Remove(existing);
                    _trainingDriverEnabledByDriver.Remove((_selectedTrainingDetailId.Value, selection.FahrerId));
                    _trainingKartSelectionByDriver.Remove((_selectedTrainingDetailId.Value, selection.FahrerId));
                    _trainingStintsByDriver.Remove((_selectedTrainingDetailId.Value, selection.FahrerId));
                }
            }

            await dbContext.SaveChangesAsync();

            CloseTrainingDriverSelectionDialog();
            await LoadTrainingStarterListAsync(_selectedTrainingDetailId.Value);
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Hinzufuegen von Fahrern zum Training.");
            MessageBox.Show("Fahrer konnten nicht zum Training hinzugefuegt werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    internal void CancelAddTrainingFahrerDialog_OnClick(object sender, RoutedEventArgs e)
    {
        CloseTrainingDriverSelectionDialog();
    }

    internal void TrainingFahrerSearchTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        _trainingDriverSearchTerm = TrainingsViewControl.TrainingFahrerSearchTextBox.Text.Trim();
        ApplyTrainingDriverSelectionFilter();
    }

    internal void TrainingDriverSelectionCheckBox_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox || checkBox.DataContext is not TrainingDriverSelectionItem item)
        {
            return;
        }

        var isChecked = checkBox.IsChecked == true;
        item.IsSelected = isChecked;

        if (isChecked)
        {
            if (item.SelectionOrder <= 0)
            {
                _trainingDriverSelectionOrderCounter++;
                item.SelectionOrder = _trainingDriverSelectionOrderCounter;
            }

            return;
        }

        item.SelectionOrder = 0;
    }

    private void ApplyTrainingDriverSelectionFilter()
    {
        var view = CollectionViewSource.GetDefaultView(TrainingDriverSelectionItems);
        if (view is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_trainingDriverSearchTerm))
        {
            view.Filter = null;
            view.Refresh();
            return;
        }

        var term = _trainingDriverSearchTerm;
        view.Filter = item =>
        {
            if (item is not TrainingDriverSelectionItem driver)
            {
                return false;
            }

            return driver.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase);
        };
        view.Refresh();
    }

    private void CloseTrainingDriverSelectionDialog()
    {
        _trainingDriverSearchTerm = string.Empty;
        _trainingDriverSelectionOrderCounter = 0;
        if (TrainingsViewControl.TrainingFahrerSearchTextBox is not null)
        {
            TrainingsViewControl.TrainingFahrerSearchTextBox.Text = string.Empty;
        }

        var view = CollectionViewSource.GetDefaultView(TrainingDriverSelectionItems);
        if (view is not null)
        {
            view.Filter = null;
            view.Refresh();
        }

        TrainingDriverSelectionItems.Clear();
        TrainingsViewControl.TrainingFahrerDialogOverlay.Visibility = Visibility.Collapsed;
    }
}
