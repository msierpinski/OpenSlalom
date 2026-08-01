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
    private async Task LoadWetterAsync()
    {
        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();

            var wetter = await dbContext.Wetterlagen
                .AsNoTracking()
                .OrderBy(x => x.Bezeichnung)
                .Select(x => new WetterListItem
                {
                    Id = x.Id,
                    Name = x.Bezeichnung
                })
                .ToListAsync();

            WetterItems.Clear();
            foreach (var item in wetter)
            {
                WetterItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Laden der Wetterdaten aus SQLite.");
        }
    }

    internal void OpenCreateWetterPage_OnClick(object sender, RoutedEventArgs e)
    {
        WetterViewControl.CreateWetterNameTextBox.Text = string.Empty;
        ShowWetterDialog(WetterDialogMode.Create);
    }

    internal async void SaveCreateWetter_OnClick(object sender, RoutedEventArgs e)
    {
        var name = WetterViewControl.CreateWetterNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Bitte eine Wetterbezeichnung eingeben.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            dbContext.Wetterlagen.Add(new Wetter { Bezeichnung = name });
            await dbContext.SaveChangesAsync();

            ShowWetterDialog(WetterDialogMode.None);
            await LoadWetterAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Anlegen eines Wettereintrags.");
            MessageBox.Show("Wetter konnte nicht angelegt werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    internal async void OpenEditWetterPage_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out var wetterId))
        {
            return;
        }

        await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
        var wetter = await dbContext.Wetterlagen.FirstOrDefaultAsync(x => x.Id == wetterId);
        if (wetter is null)
        {
            return;
        }

        _editWetterId = wetter.Id;
        WetterViewControl.EditWetterNameTextBox.Text = wetter.Bezeichnung;
        ShowWetterDialog(WetterDialogMode.Edit);
    }

    internal async void SaveEditWetter_OnClick(object sender, RoutedEventArgs e)
    {
        if (_editWetterId is null)
        {
            return;
        }

        var name = WetterViewControl.EditWetterNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Bitte eine Wetterbezeichnung eingeben.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var wetter = await dbContext.Wetterlagen.FirstOrDefaultAsync(x => x.Id == _editWetterId.Value);
            if (wetter is null)
            {
                ShowWetterDialog(WetterDialogMode.None);
                await LoadWetterAsync();
                return;
            }

            wetter.Bezeichnung = name;
            await dbContext.SaveChangesAsync();

            _editWetterId = null;
            ShowWetterDialog(WetterDialogMode.None);
            await LoadWetterAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Bearbeiten eines Wettereintrags.");
            MessageBox.Show("Wetter konnte nicht gespeichert werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    internal async void OpenDeleteWetterPage_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out var wetterId))
        {
            return;
        }

        await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
        var wetter = await dbContext.Wetterlagen.AsNoTracking().FirstOrDefaultAsync(x => x.Id == wetterId);
        if (wetter is null)
        {
            return;
        }

        _deleteWetterId = wetter.Id;
        WetterViewControl.DeleteWetterTextBlock.Text = wetter.Bezeichnung;
        ShowWetterDialog(WetterDialogMode.Delete);
    }

    internal async void ConfirmDeleteWetter_OnClick(object sender, RoutedEventArgs e)
    {
        if (_deleteWetterId is null)
        {
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var wetter = await dbContext.Wetterlagen.FirstOrDefaultAsync(x => x.Id == _deleteWetterId.Value);
            if (wetter is not null)
            {
                dbContext.Wetterlagen.Remove(wetter);
                await dbContext.SaveChangesAsync();
            }

            _deleteWetterId = null;
            ShowWetterDialog(WetterDialogMode.None);
            await LoadWetterAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Loeschen eines Wettereintrags.");
            MessageBox.Show("Wetter konnte nicht geloescht werden. Es wird vermutlich noch verwendet.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    internal void CancelWetterPage_OnClick(object sender, RoutedEventArgs e)
    {
        _editWetterId = null;
        _deleteWetterId = null;
        ShowWetterDialog(WetterDialogMode.None);
    }

    private void ShowWetterDialog(WetterDialogMode mode)
    {
        WetterViewControl.WetterCreatePage.Visibility = mode == WetterDialogMode.Create ? Visibility.Visible : Visibility.Collapsed;
        WetterViewControl.WetterEditPage.Visibility = mode == WetterDialogMode.Edit ? Visibility.Visible : Visibility.Collapsed;
        WetterViewControl.WetterDeletePage.Visibility = mode == WetterDialogMode.Delete ? Visibility.Visible : Visibility.Collapsed;
    }
}
