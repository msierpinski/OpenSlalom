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
    private async Task LoadGeneralStatisticsAsync()
    {
        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();

            var fahrerCountTask = dbContext.Fahrer.AsNoTracking().CountAsync();
            var kartsCountTask = dbContext.Karts.AsNoTracking().CountAsync();
            var trainingsCountTask = dbContext.Trainings.AsNoTracking().CountAsync();
            var stintsCountTask = dbContext.Tstints.AsNoTracking().CountAsync();

            var roundRows = await dbContext.Trunden
                .AsNoTracking()
                .Where(x => x.Rundenzeit.HasValue && x.Rundenzeit.Value > 0)
                .Select(x => new
                {
                    FahrerId = x.Tstint != null ? x.Tstint.FahrerId : 0,
                    TrainingId = x.Tstint != null ? x.Tstint.TrainingId : 0,
                    Sekunden = x.Rundenzeit!.Value,
                    Pylonen = x.Pf ?? 0,
                    Tore = x.Tf ?? 0,
                    x.Ungueltig
                })
                .ToListAsync();

            var rundenCount = roundRows.Count;
            var totalSeconds = roundRows.Sum(x => x.Sekunden);
            var totalPylonen = roundRows.Sum(x => x.Pylonen);
            var totalTore = roundRows.Sum(x => x.Tore);
            var fehlerfreieRunden = roundRows.Count(x => !x.Ungueltig && x.Pylonen == 0 && x.Tore == 0);

            var fahrerCount = await fahrerCountTask;
            var kartsCount = await kartsCountTask;
            var trainingsCount = await trainingsCountTask;
            var stintsCount = await stintsCountTask;

            var avgPylonen = rundenCount > 0 ? (double)totalPylonen / rundenCount : 0d;
            var avgTore = rundenCount > 0 ? (double)totalTore / rundenCount : 0d;
            var fehlerfreiPercent = rundenCount > 0 ? (double)fehlerfreieRunden / rundenCount * 100d : 0d;

            StatistikenView.StatsFahrerCountTextBlock.Text = fahrerCount.ToString(CultureInfo.InvariantCulture);
            StatistikenView.StatsKartsCountTextBlock.Text = kartsCount.ToString(CultureInfo.InvariantCulture);
            StatistikenView.StatsTrainingsCountTextBlock.Text = trainingsCount.ToString(CultureInfo.InvariantCulture);
            StatistikenView.StatsRundenCountTextBlock.Text = rundenCount.ToString(CultureInfo.InvariantCulture);
            StatistikenView.StatsStintsCountTextBlock.Text = stintsCount.ToString(CultureInfo.InvariantCulture);
            StatistikenView.StatsGesamteFahrzeitTextBlock.Text = FormatDuration(totalSeconds);
            StatistikenView.StatsPylonenfehlerCountTextBlock.Text = totalPylonen.ToString(CultureInfo.InvariantCulture);
            StatistikenView.StatsTorfehlerCountTextBlock.Text = totalTore.ToString(CultureInfo.InvariantCulture);
            StatistikenView.StatsAvgPylonenTextBlock.Text = avgPylonen.ToString("0.##", CultureInfo.InvariantCulture);
            StatistikenView.StatsAvgTorfehlerTextBlock.Text = avgTore.ToString("0.##", CultureInfo.InvariantCulture);
            StatistikenView.StatsFehlerfreieRundenPercentTextBlock.Text = $"{fehlerfreiPercent:0.##}%";

            var stintsByDriver = await dbContext.Tstints
                .AsNoTracking()
                .GroupBy(x => x.FahrerId)
                .Select(g => new
                {
                    FahrerId = g.Key,
                    Stints = g.Count(),
                    Trainings = g.Select(x => x.TrainingId).Distinct().Count()
                })
                .ToListAsync();

            var stintsMap = stintsByDriver.ToDictionary(x => x.FahrerId, x => (Stints: x.Stints, Trainings: x.Trainings));

            var roundsByDriver = roundRows
                .Where(x => x.FahrerId > 0)
                .GroupBy(x => x.FahrerId)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Runden = g.Count(),
                        FahrzeitSeconds = g.Sum(x => x.Sekunden),
                        Fehlerfrei = g.Count(x => !x.Ungueltig && x.Pylonen == 0 && x.Tore == 0),
                        Pylonen = g.Sum(x => x.Pylonen),
                        Tore = g.Sum(x => x.Tore)
                    });

            var fahrer = await dbContext.Fahrer
                .AsNoTracking()
                .OrderBy(x => x.Vorname)
                .ThenBy(x => x.Nachname)
                .Select(x => new
                {
                    x.Id,
                    x.Vorname,
                    Nachname = x.Nachname ?? string.Empty
                })
                .ToListAsync();

            DriverStatisticsItems.Clear();
            foreach (var driver in fahrer)
            {
                var driverName = string.IsNullOrWhiteSpace(driver.Nachname)
                    ? driver.Vorname
                    : $"{driver.Vorname} {driver.Nachname}";

                var hasRounds = roundsByDriver.TryGetValue(driver.Id, out var roundStats);
                var hasStints = stintsMap.TryGetValue(driver.Id, out var stintStats);

                var rounds = hasRounds ? roundStats!.Runden : 0;
                var fehlerfrei = hasRounds ? roundStats!.Fehlerfrei : 0;
                var fehlerfreiPct = rounds > 0 ? (double)fehlerfrei / rounds * 100d : 0d;

                DriverStatisticsItems.Add(new DriverStatisticsListItem
                {
                    Fahrer = driverName,
                    Fahrzeit = FormatDuration(hasRounds ? roundStats!.FahrzeitSeconds : 0d),
                    Trainings = hasStints ? stintStats.Trainings : 0,
                    Runden = rounds,
                    FehlerfreieRunden = $"{fehlerfrei} ({fehlerfreiPct:0.##}%)",
                    Stints = hasStints ? stintStats.Stints : 0,
                    Pylonenfehler = hasRounds ? roundStats!.Pylonen : 0,
                    Torfehler = hasRounds ? roundStats!.Tore : 0,
                    DurchschnittPylonenProRunde = rounds > 0 ? $"{((double)(hasRounds ? roundStats!.Pylonen : 0) / rounds):0.##}" : "0",
                    DurchschnittTorfehlerProRunde = rounds > 0 ? $"{((double)(hasRounds ? roundStats!.Tore : 0) / rounds):0.##}" : "0"
                });
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Laden der allgemeinen Statistik.");
            StatistikenView.StatsFahrerCountTextBlock.Text = "-";
            StatistikenView.StatsKartsCountTextBlock.Text = "-";
            StatistikenView.StatsTrainingsCountTextBlock.Text = "-";
            StatistikenView.StatsRundenCountTextBlock.Text = "-";
            StatistikenView.StatsStintsCountTextBlock.Text = "-";
            StatistikenView.StatsGesamteFahrzeitTextBlock.Text = "-";
            StatistikenView.StatsPylonenfehlerCountTextBlock.Text = "-";
            StatistikenView.StatsTorfehlerCountTextBlock.Text = "-";
            StatistikenView.StatsAvgPylonenTextBlock.Text = "-";
            StatistikenView.StatsAvgTorfehlerTextBlock.Text = "-";
            StatistikenView.StatsFehlerfreieRundenPercentTextBlock.Text = "-";
            DriverStatisticsItems.Clear();
        }
    }

    private static string FormatDuration(double totalSeconds)
    {
        var span = TimeSpan.FromSeconds(Math.Max(0d, totalSeconds));
        var hours = (int)span.TotalHours;
        return $"{hours:00}:{span.Minutes:00}:{span.Seconds:00}";
    }
}
