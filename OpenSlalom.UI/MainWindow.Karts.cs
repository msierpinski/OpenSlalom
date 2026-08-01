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
    private async Task LoadKartsAsync()
    {
        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();

            var karts = await dbContext.Karts
                .AsNoTracking()
                .Include(x => x.Verein)
                .Include(x => x.Disziplin)
                .OrderBy(x => x.Name)
                .Select(x => new KartListItem
                {
                    Id = x.Id,
                    VereinId = x.VereinId,
                    DisziplinId = x.DisziplinId,
                    Name = x.Name ?? string.Empty,
                    Motor = x.Motor ?? string.Empty,
                    Chassis = x.Chassis ?? string.Empty,
                    VereinName = x.Verein.Vereinsname,
                    DisziplinName = x.Disziplin.Name
                })
                .ToListAsync();

            KartItems.Clear();
            foreach (var item in karts)
            {
                KartItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Laden der Karts aus SQLite.");
        }
    }

    internal async void OpenCreateKartPage_OnClick(object sender, RoutedEventArgs e)
    {
        await LoadLookupDataAsync();
        KartsViewControl.CreateKartNameTextBox.Text = string.Empty;
        KartsViewControl.CreateKartMotorTextBox.Text = string.Empty;
        KartsViewControl.CreateKartChassisTextBox.Text = string.Empty;
        KartsViewControl.CreateKartVereinComboBox.SelectedIndex = -1;
        KartsViewControl.CreateKartDisziplinComboBox.SelectedIndex = -1;
        ShowKartDialog(KartDialogMode.Create);
    }

    internal async void SaveCreateKart_OnClick(object sender, RoutedEventArgs e)
    {
        if (KartsViewControl.CreateKartVereinComboBox.SelectedValue is not int vereinId ||
            KartsViewControl.CreateKartDisziplinComboBox.SelectedValue is not int disziplinId)
        {
            MessageBox.Show("Bitte Verein und Disziplin auswaehlen.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            dbContext.Karts.Add(new Kart
            {
                Name = string.IsNullOrWhiteSpace(KartsViewControl.CreateKartNameTextBox.Text) ? null : KartsViewControl.CreateKartNameTextBox.Text.Trim(),
                Motor = string.IsNullOrWhiteSpace(KartsViewControl.CreateKartMotorTextBox.Text) ? null : KartsViewControl.CreateKartMotorTextBox.Text.Trim(),
                Chassis = string.IsNullOrWhiteSpace(KartsViewControl.CreateKartChassisTextBox.Text) ? null : KartsViewControl.CreateKartChassisTextBox.Text.Trim(),
                VereinId = vereinId,
                DisziplinId = disziplinId
            });

            await dbContext.SaveChangesAsync();
            ShowKartDialog(KartDialogMode.None);
            await LoadKartsAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Anlegen eines Karts.");
            MessageBox.Show("Kart konnte nicht angelegt werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    internal async void OpenEditKartPage_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out var kartId))
        {
            return;
        }

        await LoadLookupDataAsync();
        await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
        var kart = await dbContext.Karts.FirstOrDefaultAsync(x => x.Id == kartId);
        if (kart is null)
        {
            return;
        }

        _editKartId = kart.Id;
        KartsViewControl.EditKartNameTextBox.Text = kart.Name ?? string.Empty;
        KartsViewControl.EditKartMotorTextBox.Text = kart.Motor ?? string.Empty;
        KartsViewControl.EditKartChassisTextBox.Text = kart.Chassis ?? string.Empty;
        KartsViewControl.EditKartVereinComboBox.SelectedValue = kart.VereinId;
        KartsViewControl.EditKartDisziplinComboBox.SelectedValue = kart.DisziplinId;
        ShowKartDialog(KartDialogMode.Edit);
    }

    internal async void SaveEditKart_OnClick(object sender, RoutedEventArgs e)
    {
        if (_editKartId is null)
        {
            return;
        }

        if (KartsViewControl.EditKartVereinComboBox.SelectedValue is not int vereinId ||
            KartsViewControl.EditKartDisziplinComboBox.SelectedValue is not int disziplinId)
        {
            MessageBox.Show("Bitte Verein und Disziplin auswaehlen.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var kart = await dbContext.Karts.FirstOrDefaultAsync(x => x.Id == _editKartId.Value);
            if (kart is null)
            {
                ShowKartDialog(KartDialogMode.None);
                await LoadKartsAsync();
                return;
            }

            kart.Name = string.IsNullOrWhiteSpace(KartsViewControl.EditKartNameTextBox.Text) ? null : KartsViewControl.EditKartNameTextBox.Text.Trim();
            kart.Motor = string.IsNullOrWhiteSpace(KartsViewControl.EditKartMotorTextBox.Text) ? null : KartsViewControl.EditKartMotorTextBox.Text.Trim();
            kart.Chassis = string.IsNullOrWhiteSpace(KartsViewControl.EditKartChassisTextBox.Text) ? null : KartsViewControl.EditKartChassisTextBox.Text.Trim();
            kart.VereinId = vereinId;
            kart.DisziplinId = disziplinId;
            await dbContext.SaveChangesAsync();

            _editKartId = null;
            ShowKartDialog(KartDialogMode.None);
            await LoadKartsAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Bearbeiten eines Karts.");
            MessageBox.Show("Kart konnte nicht gespeichert werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    internal async void OpenDeleteKartPage_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out var kartId))
        {
            return;
        }

        await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
        var kart = await dbContext.Karts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == kartId);
        if (kart is null)
        {
            return;
        }

        _deleteKartId = kart.Id;
        KartsViewControl.DeleteKartTextBlock.Text = string.IsNullOrWhiteSpace(kart.Name) ? $"Kart #{kart.Id}" : kart.Name;
        ShowKartDialog(KartDialogMode.Delete);
    }

    internal async void ConfirmDeleteKart_OnClick(object sender, RoutedEventArgs e)
    {
        if (_deleteKartId is null)
        {
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var kart = await dbContext.Karts.FirstOrDefaultAsync(x => x.Id == _deleteKartId.Value);
            if (kart is not null)
            {
                dbContext.Karts.Remove(kart);
                await dbContext.SaveChangesAsync();
            }

            _deleteKartId = null;
            ShowKartDialog(KartDialogMode.None);
            await LoadKartsAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Loeschen eines Karts.");
            MessageBox.Show("Kart konnte nicht geloescht werden. Es wird vermutlich noch verwendet.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    internal void CancelKartPage_OnClick(object sender, RoutedEventArgs e)
    {
        _editKartId = null;
        _deleteKartId = null;
        ShowKartDialog(KartDialogMode.None);
    }

    private void ShowKartDialog(KartDialogMode mode)
    {
        KartsViewControl.KartCreatePage.Visibility = mode == KartDialogMode.Create ? Visibility.Visible : Visibility.Collapsed;
        KartsViewControl.KartEditPage.Visibility = mode == KartDialogMode.Edit ? Visibility.Visible : Visibility.Collapsed;
        KartsViewControl.KartDeletePage.Visibility = mode == KartDialogMode.Delete ? Visibility.Visible : Visibility.Collapsed;
    }
}
