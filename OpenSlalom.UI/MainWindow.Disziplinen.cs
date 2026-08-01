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
    private async Task LoadDisziplinenAsync()
    {
        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();

            var disziplinen = await dbContext.Disziplinen
                .AsNoTracking()
                .Include(x => x.Altersklassen)
                .OrderBy(x => x.Name)
                .ToListAsync();

            var items = disziplinen.Select(x => new DisziplinListItem
            {
                Id = x.Id,
                Name = x.Name,
                ZeitstrafeTorfehler = x.ZeitstrafeTorfehler,
                ZeitstrafePylonenfehler = x.ZeitstrafePylonenfehler,
                ZeitstrafeTorfehlerText = FormatSecondsValue(x.ZeitstrafeTorfehler),
                ZeitstrafePylonenfehlerText = FormatSecondsValue(x.ZeitstrafePylonenfehler),
                AltersklassenText = FormatAltersklassenText(x.Altersklassen)
            }).ToList();

            DisziplinItems.Clear();
            foreach (var item in items)
            {
                DisziplinItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Laden der Disziplinen aus SQLite.");
        }
    }

    private static string FormatSecondsValue(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static bool TryParseSecondsValue(string input, out double value)
    {
        var normalized = input.Trim().Replace(',', '.');
        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static string FormatAltersklassenText(IEnumerable<DisziplinAltersklasse> altersklassen)
    {
        return string.Join(
            ", ",
            altersklassen
                .OrderBy(x => x.AlterVon)
                .ThenBy(x => x.AlterBis ?? int.MaxValue)
                .ThenBy(x => x.Bezeichnung)
                .Select(x => $"{x.Bezeichnung} ({x.AlterVon}-{(x.AlterBis.HasValue ? x.AlterBis.Value.ToString(CultureInfo.InvariantCulture) : "offen")})"));
    }

    private static bool TryParseAlterValue(string input, out int value)
    {
        return int.TryParse(input.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryValidateAltersklasseInput(
        string bezeichnung,
        string alterVonText,
        string alterBisText,
        out int alterVon,
        out int? alterBis,
        out string? validationError)
    {
        alterVon = 0;
        alterBis = null;
        validationError = null;

        if (string.IsNullOrWhiteSpace(bezeichnung))
        {
            validationError = "Bitte eine Bezeichnung fuer die Altersklasse eingeben.";
            return false;
        }

        if (!TryParseAlterValue(alterVonText, out alterVon) || alterVon < 0)
        {
            validationError = "Bitte ein gueltiges, nicht negatives Alter fuer 'Alter von' eingeben.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(alterBisText))
        {
            return true;
        }

        if (!TryParseAlterValue(alterBisText, out var parsedAlterBis) || parsedAlterBis < 0)
        {
            validationError = "Bitte ein gueltiges, nicht negatives Alter fuer 'Alter bis' eingeben oder leer lassen.";
            return false;
        }

        if (parsedAlterBis < alterVon)
        {
            validationError = "'Alter bis' muss groesser oder gleich 'Alter von' sein.";
            return false;
        }

        alterBis = parsedAlterBis;
        return true;
    }

    private static bool HasAltersklassenOverlap(int alterVon, int? alterBis, IEnumerable<CreateDisziplinAltersklasseItem> existing)
    {
        var newMax = alterBis ?? int.MaxValue;

        foreach (var item in existing)
        {
            var existingMax = item.AlterBis ?? int.MaxValue;
            var overlaps = alterVon <= existingMax && item.AlterVon <= newMax;
            if (overlaps)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAltersklassenOverlap(int alterVon, int? alterBis, IEnumerable<EditDisziplinAltersklasseItem> existing)
    {
        var newMax = alterBis ?? int.MaxValue;

        foreach (var item in existing)
        {
            var existingMax = item.AlterBis ?? int.MaxValue;
            var overlaps = alterVon <= existingMax && item.AlterVon <= newMax;
            if (overlaps)
            {
                return true;
            }
        }

        return false;
    }

    private void ResetCreateDisziplinAltersklasseInputs()
    {
        DisziplinenViewControl.CreateDisziplinKlasseBezeichnungTextBox.Text = string.Empty;
        DisziplinenViewControl.CreateDisziplinKlasseAlterVonTextBox.Text = string.Empty;
        DisziplinenViewControl.CreateDisziplinKlasseAlterBisTextBox.Text = string.Empty;
    }

    private void ResetEditDisziplinAltersklasseInputs()
    {
        DisziplinenViewControl.EditDisziplinKlasseBezeichnungTextBox.Text = string.Empty;
        DisziplinenViewControl.EditDisziplinKlasseAlterVonTextBox.Text = string.Empty;
        DisziplinenViewControl.EditDisziplinKlasseAlterBisTextBox.Text = string.Empty;
    }

    internal void OpenCreateDisziplinPage_OnClick(object sender, RoutedEventArgs e)
    {
        DisziplinenViewControl.CreateDisziplinNameTextBox.Text = string.Empty;
        DisziplinenViewControl.CreateDisziplinTfTextBox.Text = "0";
        DisziplinenViewControl.CreateDisziplinPfTextBox.Text = "0";
        CreateDisziplinAltersklassenItems.Clear();
        ResetCreateDisziplinAltersklasseInputs();
        ShowDisziplinDialog(DisziplinDialogMode.Create);
    }

    internal void AddCreateDisziplinAltersklasse_OnClick(object sender, RoutedEventArgs e)
    {
        var bezeichnung = DisziplinenViewControl.CreateDisziplinKlasseBezeichnungTextBox.Text.Trim();
        if (!TryValidateAltersklasseInput(
                bezeichnung,
                DisziplinenViewControl.CreateDisziplinKlasseAlterVonTextBox.Text,
                DisziplinenViewControl.CreateDisziplinKlasseAlterBisTextBox.Text,
                out var alterVon,
                out var alterBis,
                out var validationError))
        {
            MessageBox.Show(validationError ?? "Ungueltige Eingabe.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (HasAltersklassenOverlap(alterVon, alterBis, CreateDisziplinAltersklassenItems))
        {
            MessageBox.Show("Altersklassen duerfen sich nicht ueberlappen.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        CreateDisziplinAltersklassenItems.Add(new CreateDisziplinAltersklasseItem
        {
            Bezeichnung = bezeichnung,
            AlterVon = alterVon,
            AlterBis = alterBis,
            AlterBisText = alterBis?.ToString(CultureInfo.InvariantCulture) ?? "offen"
        });

        ResetCreateDisziplinAltersklasseInputs();
    }

    internal void RemoveCreateDisziplinAltersklasse_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not CreateDisziplinAltersklasseItem item)
        {
            return;
        }

        CreateDisziplinAltersklassenItems.Remove(item);
    }

    internal void AddEditDisziplinAltersklasse_OnClick(object sender, RoutedEventArgs e)
    {
        var bezeichnung = DisziplinenViewControl.EditDisziplinKlasseBezeichnungTextBox.Text.Trim();
        if (!TryValidateAltersklasseInput(
                bezeichnung,
                DisziplinenViewControl.EditDisziplinKlasseAlterVonTextBox.Text,
                DisziplinenViewControl.EditDisziplinKlasseAlterBisTextBox.Text,
                out var alterVon,
                out var alterBis,
                out var validationError))
        {
            MessageBox.Show(validationError ?? "Ungueltige Eingabe.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (HasAltersklassenOverlap(alterVon, alterBis, EditDisziplinAltersklassenItems))
        {
            MessageBox.Show("Altersklassen duerfen sich nicht ueberlappen.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        EditDisziplinAltersklassenItems.Add(new EditDisziplinAltersklasseItem
        {
            Bezeichnung = bezeichnung,
            AlterVon = alterVon,
            AlterBis = alterBis,
            AlterBisText = alterBis?.ToString(CultureInfo.InvariantCulture) ?? "offen"
        });

        ResetEditDisziplinAltersklasseInputs();
    }

    internal void RemoveEditDisziplinAltersklasse_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not EditDisziplinAltersklasseItem item)
        {
            return;
        }

        EditDisziplinAltersklassenItems.Remove(item);
    }

    internal async void SaveCreateDisziplin_OnClick(object sender, RoutedEventArgs e)
    {
        var name = DisziplinenViewControl.CreateDisziplinNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Bitte einen Namen eingeben.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!TryParseSecondsValue(DisziplinenViewControl.CreateDisziplinTfTextBox.Text, out var zeitstrafeTorfehler) || zeitstrafeTorfehler < 0)
        {
            MessageBox.Show("Bitte eine gueltige, nicht negative Zeitstrafe fuer Torfehler eingeben.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!TryParseSecondsValue(DisziplinenViewControl.CreateDisziplinPfTextBox.Text, out var zeitstrafePylonenfehler) || zeitstrafePylonenfehler < 0)
        {
            MessageBox.Show("Bitte eine gueltige, nicht negative Zeitstrafe fuer Pylonenfehler eingeben.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var disziplin = new Disziplin
            {
                Name = name,
                ZeitstrafeTorfehler = zeitstrafeTorfehler,
                ZeitstrafePylonenfehler = zeitstrafePylonenfehler
            };

            foreach (var altersklasse in CreateDisziplinAltersklassenItems
                         .OrderBy(x => x.AlterVon)
                         .ThenBy(x => x.AlterBis ?? int.MaxValue)
                         .ThenBy(x => x.Bezeichnung))
            {
                disziplin.Altersklassen.Add(new DisziplinAltersklasse
                {
                    Bezeichnung = altersklasse.Bezeichnung,
                    AlterVon = altersklasse.AlterVon,
                    AlterBis = altersklasse.AlterBis
                });
            }

            dbContext.Disziplinen.Add(disziplin);
            await dbContext.SaveChangesAsync();

            CreateDisziplinAltersklassenItems.Clear();
            ResetCreateDisziplinAltersklasseInputs();
            ShowDisziplinDialog(DisziplinDialogMode.None);
            await LoadDisziplinenAsync();
            await LoadLookupDataAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Anlegen einer Disziplin.");
            MessageBox.Show("Disziplin konnte nicht angelegt werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    internal async void OpenEditDisziplinPage_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out var disziplinId))
        {
            return;
        }

        await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
        var disziplin = await dbContext.Disziplinen
            .Include(x => x.Altersklassen)
            .FirstOrDefaultAsync(x => x.Id == disziplinId);
        if (disziplin is null)
        {
            return;
        }

        _editDisziplinId = disziplin.Id;
        DisziplinenViewControl.EditDisziplinNameTextBox.Text = disziplin.Name;
        DisziplinenViewControl.EditDisziplinTfTextBox.Text = FormatSecondsValue(disziplin.ZeitstrafeTorfehler);
        DisziplinenViewControl.EditDisziplinPfTextBox.Text = FormatSecondsValue(disziplin.ZeitstrafePylonenfehler);
        EditDisziplinAltersklassenItems.Clear();
        foreach (var altersklasse in disziplin.Altersklassen
                     .OrderBy(x => x.AlterVon)
                     .ThenBy(x => x.AlterBis ?? int.MaxValue)
                     .ThenBy(x => x.Bezeichnung))
        {
            EditDisziplinAltersklassenItems.Add(new EditDisziplinAltersklasseItem
            {
                Bezeichnung = altersklasse.Bezeichnung,
                AlterVon = altersklasse.AlterVon,
                AlterBis = altersklasse.AlterBis,
                AlterBisText = altersklasse.AlterBis?.ToString(CultureInfo.InvariantCulture) ?? "offen"
            });
        }

        ResetEditDisziplinAltersklasseInputs();
        ShowDisziplinDialog(DisziplinDialogMode.Edit);
    }

    internal async void SaveEditDisziplin_OnClick(object sender, RoutedEventArgs e)
    {
        if (_editDisziplinId is null)
        {
            return;
        }

        var name = DisziplinenViewControl.EditDisziplinNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Bitte einen Namen eingeben.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!TryParseSecondsValue(DisziplinenViewControl.EditDisziplinTfTextBox.Text, out var zeitstrafeTorfehler) || zeitstrafeTorfehler < 0)
        {
            MessageBox.Show("Bitte eine gueltige, nicht negative Zeitstrafe fuer Torfehler eingeben.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!TryParseSecondsValue(DisziplinenViewControl.EditDisziplinPfTextBox.Text, out var zeitstrafePylonenfehler) || zeitstrafePylonenfehler < 0)
        {
            MessageBox.Show("Bitte eine gueltige, nicht negative Zeitstrafe fuer Pylonenfehler eingeben.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var disziplin = await dbContext.Disziplinen
                .Include(x => x.Altersklassen)
                .FirstOrDefaultAsync(x => x.Id == _editDisziplinId.Value);
            if (disziplin is null)
            {
                ShowDisziplinDialog(DisziplinDialogMode.None);
                await LoadDisziplinenAsync();
                return;
            }

            disziplin.Name = name;
            disziplin.ZeitstrafeTorfehler = zeitstrafeTorfehler;
            disziplin.ZeitstrafePylonenfehler = zeitstrafePylonenfehler;

            dbContext.DisziplinAltersklassen.RemoveRange(disziplin.Altersklassen);
            foreach (var altersklasse in EditDisziplinAltersklassenItems
                         .OrderBy(x => x.AlterVon)
                         .ThenBy(x => x.AlterBis ?? int.MaxValue)
                         .ThenBy(x => x.Bezeichnung))
            {
                disziplin.Altersklassen.Add(new DisziplinAltersklasse
                {
                    Bezeichnung = altersklasse.Bezeichnung,
                    AlterVon = altersklasse.AlterVon,
                    AlterBis = altersklasse.AlterBis
                });
            }

            await dbContext.SaveChangesAsync();

            _editDisziplinId = null;
            EditDisziplinAltersklassenItems.Clear();
            ResetEditDisziplinAltersklasseInputs();
            ShowDisziplinDialog(DisziplinDialogMode.None);
            await LoadDisziplinenAsync();
            await LoadLookupDataAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Bearbeiten einer Disziplin.");
            MessageBox.Show("Disziplin konnte nicht gespeichert werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    internal async void OpenDeleteDisziplinPage_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out var disziplinId))
        {
            return;
        }

        await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
        var disziplin = await dbContext.Disziplinen.FirstOrDefaultAsync(x => x.Id == disziplinId);
        if (disziplin is null)
        {
            return;
        }

        _deleteDisziplinId = disziplin.Id;
        DisziplinenViewControl.DeleteDisziplinTextBlock.Text = disziplin.Name;
        ShowDisziplinDialog(DisziplinDialogMode.Delete);
    }

    internal async void ConfirmDeleteDisziplin_OnClick(object sender, RoutedEventArgs e)
    {
        if (_deleteDisziplinId is null)
        {
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var disziplin = await dbContext.Disziplinen.FirstOrDefaultAsync(x => x.Id == _deleteDisziplinId.Value);
            if (disziplin is not null)
            {
                dbContext.Disziplinen.Remove(disziplin);
                await dbContext.SaveChangesAsync();
            }

            _deleteDisziplinId = null;
            ShowDisziplinDialog(DisziplinDialogMode.None);
            await LoadDisziplinenAsync();
            await LoadLookupDataAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Loeschen einer Disziplin.");
            MessageBox.Show("Disziplin konnte nicht geloescht werden. Sie wird vermutlich noch verwendet.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    internal void CancelDisziplinPage_OnClick(object sender, RoutedEventArgs e)
    {
        _editDisziplinId = null;
        _deleteDisziplinId = null;
        CreateDisziplinAltersklassenItems.Clear();
        EditDisziplinAltersklassenItems.Clear();
        ResetCreateDisziplinAltersklasseInputs();
        ResetEditDisziplinAltersklasseInputs();
        ShowDisziplinDialog(DisziplinDialogMode.None);
    }

    private void ShowDisziplinDialog(DisziplinDialogMode mode)
    {
        DisziplinenViewControl.DisziplinCreatePage.Visibility = mode == DisziplinDialogMode.Create ? Visibility.Visible : Visibility.Collapsed;
        DisziplinenViewControl.DisziplinEditPage.Visibility = mode == DisziplinDialogMode.Edit ? Visibility.Visible : Visibility.Collapsed;
        DisziplinenViewControl.DisziplinDeletePage.Visibility = mode == DisziplinDialogMode.Delete ? Visibility.Visible : Visibility.Collapsed;
    }
}
