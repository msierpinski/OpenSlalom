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
    private async Task LoadFahrerAsync()
    {
        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();

            var fahrer = await dbContext.Fahrer
                .AsNoTracking()
                .Include(x => x.Verein)
                .OrderBy(x => x.Vorname)
                .ThenBy(x => x.Nachname)
                .Select(x => new FahrerListItem
                {
                    Id = x.Id,
                    VereinId = x.VereinId,
                    Geschlecht = x.Geschlecht,
                    GeschlechtIconPath = GetGeschlechtIconPath(x.Geschlecht),
                    Vorname = x.Vorname,
                    Nachname = x.Nachname ?? string.Empty,
                    MitgliedsNummer = x.MitgliedsNummer,
                    Geburtsdatum = x.Geburtsdatum,
                    GeburtsdatumText = x.Geburtsdatum.HasValue ? x.Geburtsdatum.Value.ToString("dd.MM.yyyy") : string.Empty,
                    VereinName = x.Verein.Vereinsname
                })
                .ToListAsync();

            FahrerItems.Clear();
            foreach (var item in fahrer)
            {
                FahrerItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Laden der Fahrer aus SQLite.");
        }
    }

    private static string GetGeschlechtIconPath(string? geschlecht)
    {
        return geschlecht?.Trim().ToLowerInvariant() switch
        {
            "m" => "/icons/m.svg",
            "w" => "/icons/w.svg",
            "d" => "/icons/d.svg",
            _ => string.Empty
        };
    }

    private static string GetSelectedGeschlechtValue(ComboBox comboBox)
    {
        if (comboBox.SelectedItem is ComboBoxItem item && item.Tag is string value)
        {
            return value;
        }

        return string.Empty;
    }

    private static void SetSelectedGeschlechtValue(ComboBox comboBox, string? geschlecht)
    {
        var normalized = geschlecht?.Trim().ToLowerInvariant() ?? string.Empty;
        foreach (var rawItem in comboBox.Items)
        {
            if (rawItem is ComboBoxItem item && string.Equals(item.Tag as string, normalized, StringComparison.OrdinalIgnoreCase))
            {
                comboBox.SelectedItem = item;
                return;
            }
        }

        comboBox.SelectedIndex = -1;
    }

    internal async void OpenCreateFahrerPage_OnClick(object sender, RoutedEventArgs e)
    {
        await LoadLookupDataAsync();
        FahrerViewControl.CreateFahrerVornameTextBox.Text = string.Empty;
        FahrerViewControl.CreateFahrerNachnameTextBox.Text = string.Empty;
        FahrerViewControl.CreateFahrerMitgliedsNummerTextBox.Text = string.Empty;
        FahrerViewControl.CreateFahrerGeburtsdatumPicker.SelectedDate = null;
        FahrerViewControl.CreateFahrerGeschlechtComboBox.SelectedIndex = -1;
        FahrerViewControl.CreateFahrerVereinComboBox.SelectedIndex = -1;
        ShowFahrerDialog(FahrerDialogMode.Create);
    }

    internal async void SaveCreateFahrer_OnClick(object sender, RoutedEventArgs e)
    {
        var vorname = FahrerViewControl.CreateFahrerVornameTextBox.Text.Trim();
        var nachname = FahrerViewControl.CreateFahrerNachnameTextBox.Text.Trim();
        var mitgliedsNummer = FahrerViewControl.CreateFahrerMitgliedsNummerTextBox.Text.Trim();
        var geburtsdatum = FahrerViewControl.CreateFahrerGeburtsdatumPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(FahrerViewControl.CreateFahrerGeburtsdatumPicker.SelectedDate.Value)
            : (DateOnly?)null;
        var geschlecht = GetSelectedGeschlechtValue(FahrerViewControl.CreateFahrerGeschlechtComboBox);
        if (string.IsNullOrWhiteSpace(vorname) || FahrerViewControl.CreateFahrerVereinComboBox.SelectedValue is not int vereinId)
        {
            MessageBox.Show("Bitte Vorname und Verein ausfuellen.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            dbContext.Fahrer.Add(new Fahrer
            {
                Vorname = vorname,
                Nachname = string.IsNullOrWhiteSpace(nachname) ? null : nachname,
                MitgliedsNummer = mitgliedsNummer,
                Geburtsdatum = geburtsdatum,
                Geschlecht = geschlecht,
                VereinId = vereinId
            });
            await dbContext.SaveChangesAsync();

            ShowFahrerDialog(FahrerDialogMode.None);
            await LoadFahrerAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Anlegen eines Fahrers.");
            MessageBox.Show("Fahrer konnte nicht angelegt werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    internal async void OpenEditFahrerPage_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out var fahrerId))
        {
            return;
        }

        await LoadLookupDataAsync();
        await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
        var fahrer = await dbContext.Fahrer.FirstOrDefaultAsync(x => x.Id == fahrerId);
        if (fahrer is null)
        {
            return;
        }

        _editFahrerId = fahrer.Id;
        FahrerViewControl.EditFahrerVornameTextBox.Text = fahrer.Vorname;
        FahrerViewControl.EditFahrerNachnameTextBox.Text = fahrer.Nachname ?? string.Empty;
        FahrerViewControl.EditFahrerMitgliedsNummerTextBox.Text = fahrer.MitgliedsNummer;
        FahrerViewControl.EditFahrerGeburtsdatumPicker.SelectedDate = fahrer.Geburtsdatum?.ToDateTime(TimeOnly.MinValue);
        SetSelectedGeschlechtValue(FahrerViewControl.EditFahrerGeschlechtComboBox, fahrer.Geschlecht);
        FahrerViewControl.EditFahrerVereinComboBox.SelectedValue = fahrer.VereinId;
        ShowFahrerDialog(FahrerDialogMode.Edit);
    }

    internal async void SaveEditFahrer_OnClick(object sender, RoutedEventArgs e)
    {
        if (_editFahrerId is null)
        {
            return;
        }

        var vorname = FahrerViewControl.EditFahrerVornameTextBox.Text.Trim();
        var nachname = FahrerViewControl.EditFahrerNachnameTextBox.Text.Trim();
        var mitgliedsNummer = FahrerViewControl.EditFahrerMitgliedsNummerTextBox.Text.Trim();
        var geburtsdatum = FahrerViewControl.EditFahrerGeburtsdatumPicker.SelectedDate.HasValue
            ? DateOnly.FromDateTime(FahrerViewControl.EditFahrerGeburtsdatumPicker.SelectedDate.Value)
            : (DateOnly?)null;
        var geschlecht = GetSelectedGeschlechtValue(FahrerViewControl.EditFahrerGeschlechtComboBox);
        if (string.IsNullOrWhiteSpace(vorname) || FahrerViewControl.EditFahrerVereinComboBox.SelectedValue is not int vereinId)
        {
            MessageBox.Show("Bitte Vorname und Verein ausfuellen.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var fahrer = await dbContext.Fahrer.FirstOrDefaultAsync(x => x.Id == _editFahrerId.Value);
            if (fahrer is null)
            {
                ShowFahrerDialog(FahrerDialogMode.None);
                await LoadFahrerAsync();
                return;
            }

            fahrer.Vorname = vorname;
            fahrer.Nachname = string.IsNullOrWhiteSpace(nachname) ? null : nachname;
            fahrer.MitgliedsNummer = mitgliedsNummer;
            fahrer.Geburtsdatum = geburtsdatum;
            fahrer.Geschlecht = geschlecht;
            fahrer.VereinId = vereinId;
            await dbContext.SaveChangesAsync();

            _editFahrerId = null;
            ShowFahrerDialog(FahrerDialogMode.None);
            await LoadFahrerAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Bearbeiten eines Fahrers.");
            MessageBox.Show("Fahrer konnte nicht gespeichert werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    internal async void OpenDeleteFahrerPage_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out var fahrerId))
        {
            return;
        }

        await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
        var fahrer = await dbContext.Fahrer.AsNoTracking().FirstOrDefaultAsync(x => x.Id == fahrerId);
        if (fahrer is null)
        {
            return;
        }

        _deleteFahrerId = fahrer.Id;
        var fahrerName = $"{fahrer.Vorname} {fahrer.Nachname}".Trim();
        FahrerViewControl.DeleteFahrerTextBlock.Text = string.IsNullOrWhiteSpace(fahrer.MitgliedsNummer)
            ? fahrerName
            : $"{fahrerName} ({fahrer.MitgliedsNummer})";
        ShowFahrerDialog(FahrerDialogMode.Delete);
    }

    internal async void ConfirmDeleteFahrer_OnClick(object sender, RoutedEventArgs e)
    {
        if (_deleteFahrerId is null)
        {
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var fahrer = await dbContext.Fahrer.FirstOrDefaultAsync(x => x.Id == _deleteFahrerId.Value);
            if (fahrer is not null)
            {
                dbContext.Fahrer.Remove(fahrer);
                await dbContext.SaveChangesAsync();
            }

            _deleteFahrerId = null;
            ShowFahrerDialog(FahrerDialogMode.None);
            await LoadFahrerAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Loeschen eines Fahrers.");
            MessageBox.Show("Fahrer konnte nicht geloescht werden. Er wird vermutlich noch verwendet.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    internal void CancelFahrerPage_OnClick(object sender, RoutedEventArgs e)
    {
        _editFahrerId = null;
        _deleteFahrerId = null;
        ShowFahrerDialog(FahrerDialogMode.None);
    }

    private void ShowFahrerDialog(FahrerDialogMode mode)
    {
        FahrerViewControl.FahrerCreatePage.Visibility = mode == FahrerDialogMode.Create ? Visibility.Visible : Visibility.Collapsed;
        FahrerViewControl.FahrerEditPage.Visibility = mode == FahrerDialogMode.Edit ? Visibility.Visible : Visibility.Collapsed;
        FahrerViewControl.FahrerDeletePage.Visibility = mode == FahrerDialogMode.Delete ? Visibility.Visible : Visibility.Collapsed;
    }
}
