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
    private async Task LoadLookupDataAsync()
    {
        await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();

        var vereine = await dbContext.Vereine
            .AsNoTracking()
            .OrderBy(x => x.Vereinsname)
            .Select(x => new LookupItem { Id = x.Id, Name = x.Vereinsname })
            .ToListAsync();

        var disziplinen = await dbContext.Disziplinen
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new LookupItem { Id = x.Id, Name = x.Name })
            .ToListAsync();

        var wetter = await dbContext.Wetterlagen
            .AsNoTracking()
            .OrderBy(x => x.Bezeichnung)
            .Select(x => new LookupItem { Id = x.Id, Name = x.Bezeichnung })
            .ToListAsync();

        var karts = await dbContext.Karts
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Select(x => new LookupItem
            {
                Id = x.Id,
                Name = string.IsNullOrWhiteSpace(x.Name) ? $"Kart #{x.Id}" : x.Name
            })
            .ToListAsync();

        VereinLookupItems.Clear();
        foreach (var item in vereine)
        {
            VereinLookupItems.Add(item);
        }

        DisziplinLookupItems.Clear();
        foreach (var item in disziplinen)
        {
            DisziplinLookupItems.Add(item);
        }

        WetterLookupItems.Clear();
        foreach (var item in wetter)
        {
            WetterLookupItems.Add(item);
        }

        KartLookupItems.Clear();
        foreach (var item in karts)
        {
            KartLookupItems.Add(item);
        }

        FahrerViewControl.CreateFahrerVereinComboBox.ItemsSource = VereinLookupItems;
        FahrerViewControl.EditFahrerVereinComboBox.ItemsSource = VereinLookupItems;
        KartsViewControl.CreateKartVereinComboBox.ItemsSource = VereinLookupItems;
        KartsViewControl.EditKartVereinComboBox.ItemsSource = VereinLookupItems;
        KartsViewControl.CreateKartDisziplinComboBox.ItemsSource = DisziplinLookupItems;
        KartsViewControl.EditKartDisziplinComboBox.ItemsSource = DisziplinLookupItems;
        TrainingsViewControl.CreateTrainingVereinComboBox.ItemsSource = VereinLookupItems;
        TrainingsViewControl.EditTrainingVereinComboBox.ItemsSource = VereinLookupItems;
        TrainingsViewControl.CreateTrainingDisziplinComboBox.ItemsSource = DisziplinLookupItems;
        TrainingsViewControl.EditTrainingDisziplinComboBox.ItemsSource = DisziplinLookupItems;
        TrainingsViewControl.CreateTrainingWetterComboBox.ItemsSource = WetterLookupItems;
        TrainingsViewControl.EditTrainingWetterComboBox.ItemsSource = WetterLookupItems;
    }
}
