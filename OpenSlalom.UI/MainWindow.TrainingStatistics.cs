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
    private async Task LoadTrainingStatisticsAsync(int trainingId)
    {
        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();

            var training = await dbContext.Trainings
                .AsNoTracking()
                .Include(x => x.Disziplin)
                .FirstOrDefaultAsync(x => x.Id == trainingId);

            if (training is null)
            {
                TrainingsViewControl.TrainingStatisticsTitleTextBlock.Text = "Training nicht gefunden";
                TrainingsViewControl.TrainingStatisticsParticipantsTextBlock.Text = "Teilnehmer: -";
                TrainingsViewControl.TrainingStatisticsTimeRangeTextBlock.Text = "Uhrzeit: -";
                TrainingStatisticsBestLapItems.Clear();
                TrainingStatisticsDriverSections.Clear();
                return;
            }

            TrainingsViewControl.TrainingStatisticsTitleTextBlock.Text = $"{training.Name} ({training.Zeitpunkt:dd.MM.yyyy})";

            var allStints = await dbContext.Tstints
                .AsNoTracking()
                .Where(x => x.TrainingId == trainingId)
                .Select(x => new
                {
                    x.FahrerId,
                    x.Datum
                })
                .ToListAsync();

            var participantsCount = allStints.Select(x => x.FahrerId).Distinct().Count();
            var minTime = allStints.Count > 0 ? allStints.Min(x => x.Datum) : (DateTime?)null;
            var maxTime = allStints.Count > 0 ? allStints.Max(x => x.Datum) : (DateTime?)null;

            TrainingsViewControl.TrainingStatisticsParticipantsTextBlock.Text = $"Teilnehmer: {participantsCount}";
            TrainingsViewControl.TrainingStatisticsTimeRangeTextBlock.Text = minTime.HasValue && maxTime.HasValue
                ? $"Gestartet: {minTime.Value:HH:mm:ss} - Beendet: {maxTime.Value:HH:mm:ss}"
                : "";

            var tfPenalty = training.Disziplin.ZeitstrafeTorfehler;
            var pfPenalty = training.Disziplin.ZeitstrafePylonenfehler;

            var lapRows = await dbContext.Trunden
                .AsNoTracking()
                .Where(x => x.Tstint != null && x.Tstint.TrainingId == trainingId)
                .Select(x => new
                {
                    FahrerId = x.Tstint!.FahrerId,
                    StintId = x.Tstint.Id,
                    x.Tstint.Fahrer.Vorname,
                    Nachname = x.Tstint.Fahrer.Nachname ?? string.Empty,
                    KartName = x.Tstint.Kart != null ? x.Tstint.Kart.Name : null,
                    Altersklasse = x.Tstint.AltersklasseSnapshot,
                    Zeitpunkt = x.Tstint.Datum,
                    Runde = x.Runde,
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
                    var driverRows = group.ToList();
                    var validLaps = group
                        .Where(x => !x.Ungueltig && x.Rundenzeit.HasValue && x.Rundenzeit.Value > 0)
                        .Select(x => new
                        {
                            Row = x,
                            EffectiveSeconds = x.Rundenzeit!.Value + Math.Max(0d, (x.Tore * tfPenalty) + (x.Pylonen * pfPenalty))
                        })
                        .OrderBy(x => x.EffectiveSeconds)
                        .ThenBy(x => x.Row.Zeitpunkt)
                        .ToList();

                    if (validLaps.Count == 0)
                    {
                        return null;
                    }

                    var best = validLaps[0];
                    var avg = validLaps.Average(x => x.EffectiveSeconds);
                    var lastDrive = driverRows.Max(x => x.Zeitpunkt);

                    var fahrerName = string.IsNullOrWhiteSpace(best.Row.Nachname)
                        ? best.Row.Vorname
                        : $"{best.Row.Vorname} {best.Row.Nachname}";

                    return new
                    {
                        FahrerId = group.Key,
                        BestSeconds = best.EffectiveSeconds,
                        Klasse = string.IsNullOrWhiteSpace(best.Row.Altersklasse) ? "-" : best.Row.Altersklasse,
                        Fahrer = fahrerName,
                        Kart = string.IsNullOrWhiteSpace(best.Row.KartName) ? "-" : best.Row.KartName!,
                        AverageSeconds = avg,
                        GefahreneRunden = validLaps.Count,
                        LastDriveTime = lastDrive
                    };
                })
                .Where(x => x is not null)
                .Select(x => x!)
                .OrderBy(x => x.BestSeconds)
                .ThenBy(x => x.Fahrer)
                .ToList();

            TrainingStatisticsBestLapItems.Clear();
            TrainingStatisticsDriverSections.Clear();
            if (perDriver.Count == 0)
            {
                return;
            }

            var bestOverall = perDriver[0].BestSeconds;
            for (var i = 0; i < perDriver.Count; i++)
            {
                var row = perDriver[i];
                var diff = row.BestSeconds - bestOverall;

                TrainingStatisticsBestLapItems.Add(new TrainingStatisticsBestLapListItem
                {
                    Position = i + 1,
                    Klasse = row.Klasse,
                    Fahrer = row.Fahrer,
                    Kart = row.Kart,
                    Bestzeit = FormatTrainingTime(TimeSpan.FromSeconds(row.BestSeconds)),
                    Abstand = i == 0 ? "-" : $"+{FormatTrainingTime(TimeSpan.FromSeconds(diff))}",
                    Durchschnittszeit = FormatTrainingTime(TimeSpan.FromSeconds(row.AverageSeconds)),
                    GefahreneRunden = row.GefahreneRunden,
                    ZeitpunktLetzteFahrt = row.LastDriveTime.ToString("HH:mm:ss")
                });
            }

            var driverOrderMap = perDriver
                .Select((x, index) => new { x.FahrerId, Position = index + 1 })
                .ToDictionary(x => x.FahrerId, x => x.Position);

            var sections = lapRows
                .GroupBy(x => x.FahrerId)
                .Select(group =>
                {
                    var orderedRows = group
                        .OrderBy(x => x.Zeitpunkt)
                        .ThenBy(x => x.StintId)
                        .ThenBy(x => x.Runde ?? int.MaxValue)
                        .ToList();

                    if (orderedRows.Count == 0)
                    {
                        return null;
                    }

                    var first = orderedRows[0];
                    var fahrerName = string.IsNullOrWhiteSpace(first.Nachname)
                        ? first.Vorname
                        : $"{first.Vorname} {first.Nachname}";
                    var klasse = orderedRows
                        .Select(x => x.Altersklasse)
                        .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "-";

                    var stintOrderMap = orderedRows
                        .Select(x => x.StintId)
                        .Distinct()
                        .OrderBy(x => orderedRows.First(r => r.StintId == x).Zeitpunkt)
                        .Select((stintId, idx) => new { stintId, Number = idx + 1 })
                        .ToDictionary(x => x.stintId, x => x.Number);

                    var lapItems = orderedRows
                        .Select((row, idx) =>
                        {
                            var penalty = Math.Max(0d, (row.Tore * tfPenalty) + (row.Pylonen * pfPenalty));
                            return new TrainingStatisticsDriverLapItem
                            {
                                Nummer = idx + 1,
                                Stint = stintOrderMap[row.StintId],
                                Runde = row.Runde ?? 0,
                                Kart = string.IsNullOrWhiteSpace(row.KartName) ? "-" : row.KartName!,
                                Zeit = row.Rundenzeit.HasValue && row.Rundenzeit.Value > 0d
                                    ? FormatTrainingTime(TimeSpan.FromSeconds(row.Rundenzeit.Value))
                                    : "-",
                                StrafeSekunden = penalty,
                                StrafeText = penalty > 0d ? $"{FormatSecondsValue(penalty)}s" : string.Empty,
                                P = row.Pylonen,
                                T = row.Tore,
                                Zeitpunkt = row.Zeitpunkt.ToString("HH:mm:ss")
                            };
                        })
                        .ToList();

                    return new TrainingStatisticsDriverSectionItem
                    {
                        FahrerId = group.Key,
                        Titel = $"{fahrerName} ({klasse})",
                        LapItems = new ObservableCollection<TrainingStatisticsDriverLapItem>(lapItems)
                    };
                })
                .Where(x => x is not null)
                .Select(x => x!)
                .OrderBy(x => driverOrderMap.TryGetValue(x.FahrerId, out var position) ? position : int.MaxValue)
                .ThenBy(x => x.Titel)
                .ToList();

            foreach (var section in sections)
            {
                TrainingStatisticsDriverSections.Add(section);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Laden der Trainingsstatistik.");
            TrainingsViewControl.TrainingStatisticsTitleTextBlock.Text = "Trainingsstatistik nicht verfuegbar";
            TrainingsViewControl.TrainingStatisticsParticipantsTextBlock.Text = "Teilnehmer: -";
            TrainingsViewControl.TrainingStatisticsTimeRangeTextBlock.Text = "Uhrzeit: -";
            TrainingStatisticsBestLapItems.Clear();
            TrainingStatisticsDriverSections.Clear();
        }
    }

    internal void TrainingStatisticsDataGrid_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        TrainingsViewControl.TrainingStatisticsScrollViewer.ScrollToVerticalOffset(TrainingsViewControl.TrainingStatisticsScrollViewer.VerticalOffset - (e.Delta / 3.0));
        e.Handled = true;
    }
}
