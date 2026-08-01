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
    private async Task LoadVereineAsync()
    {
        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();

            var vereine = await dbContext.Vereine
                .AsNoTracking()
                .OrderBy(x => x.Vereinsname)
                .ToListAsync();

            var vereinItems = vereine.Select(x => new VereinListItem
                {
                    Id = x.Id,
                    Vereinsname = x.Vereinsname,
                    MitgliedsNummer = x.MitgliedsNummer,
                    Postleitzahl = x.Postleitzahl,
                    Ort = x.Ort,
                    Adresse = x.Adresse,
                    LogoPreview = CreateImageSourceFromBytes(x.Logo)
                })
                .ToList();

            VereineItems.Clear();
            foreach (var verein in vereinItems)
            {
                VereineItems.Add(verein);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Laden der Vereine aus SQLite.");
        }
    }

    internal void OpenCreateVereinPage_OnClick(object sender, RoutedEventArgs e)
    {
        VereineViewControl.CreateMitgliedsNummerTextBox.Text = string.Empty;
        VereineViewControl.CreateVereinsnameTextBox.Text = string.Empty;
        VereineViewControl.CreatePostleitzahlTextBox.Text = string.Empty;
        VereineViewControl.CreateOrtTextBox.Text = string.Empty;
        VereineViewControl.CreateAdresseTextBox.Text = string.Empty;
        _createVereinLogoBytes = null;
        UpdateCreateLogoPreview();
        ShowVereinDialog(VereinDialogMode.Create);
    }

    internal async void SaveCreateVerein_OnClick(object sender, RoutedEventArgs e)
    {
        var mitgliedsNummer = VereineViewControl.CreateMitgliedsNummerTextBox.Text.Trim();
        var vereinsname = VereineViewControl.CreateVereinsnameTextBox.Text.Trim();
        var postleitzahl = VereineViewControl.CreatePostleitzahlTextBox.Text.Trim();
        var ort = VereineViewControl.CreateOrtTextBox.Text.Trim();
        var adresse = VereineViewControl.CreateAdresseTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(mitgliedsNummer) || string.IsNullOrWhiteSpace(vereinsname))
        {
            MessageBox.Show("Bitte Mitgliedsnummer und Vereinsname ausfuellen.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            dbContext.Vereine.Add(new Verein
            {
                MitgliedsNummer = mitgliedsNummer,
                Vereinsname = vereinsname,
                Postleitzahl = postleitzahl,
                Ort = ort,
                Adresse = adresse,
                Logo = _createVereinLogoBytes
            });

            await dbContext.SaveChangesAsync();
            ShowVereinDialog(VereinDialogMode.None);
            await LoadVereineAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Anlegen eines Vereins.");
            MessageBox.Show("Verein konnte nicht angelegt werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    internal async void OpenEditVereinPage_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out var vereinId))
        {
            return;
        }

        await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
        var verein = await dbContext.Vereine.FirstOrDefaultAsync(x => x.Id == vereinId);
        if (verein is null)
        {
            return;
        }

        _editVereinId = verein.Id;
        VereineViewControl.EditMitgliedsNummerTextBox.Text = verein.MitgliedsNummer;
        VereineViewControl.EditVereinsnameTextBox.Text = verein.Vereinsname;
        VereineViewControl.EditPostleitzahlTextBox.Text = verein.Postleitzahl;
        VereineViewControl.EditOrtTextBox.Text = verein.Ort;
        VereineViewControl.EditAdresseTextBox.Text = verein.Adresse;
        _editVereinLogoBytes = verein.Logo;
        UpdateEditLogoPreview();
        ShowVereinDialog(VereinDialogMode.Edit);
    }

    internal async void SaveEditVerein_OnClick(object sender, RoutedEventArgs e)
    {
        if (_editVereinId is null)
        {
            return;
        }

        var mitgliedsNummer = VereineViewControl.EditMitgliedsNummerTextBox.Text.Trim();
        var vereinsname = VereineViewControl.EditVereinsnameTextBox.Text.Trim();
        var postleitzahl = VereineViewControl.EditPostleitzahlTextBox.Text.Trim();
        var ort = VereineViewControl.EditOrtTextBox.Text.Trim();
        var adresse = VereineViewControl.EditAdresseTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(mitgliedsNummer) || string.IsNullOrWhiteSpace(vereinsname))
        {
            MessageBox.Show("Bitte Mitgliedsnummer und Vereinsname ausfuellen.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var verein = await dbContext.Vereine.FirstOrDefaultAsync(x => x.Id == _editVereinId.Value);
            if (verein is null)
            {
                ShowVereinDialog(VereinDialogMode.None);
                await LoadVereineAsync();
                return;
            }

            verein.MitgliedsNummer = mitgliedsNummer;
            verein.Vereinsname = vereinsname;
            verein.Postleitzahl = postleitzahl;
            verein.Ort = ort;
            verein.Adresse = adresse;
            verein.Logo = _editVereinLogoBytes;
            await dbContext.SaveChangesAsync();

            _editVereinId = null;
            ShowVereinDialog(VereinDialogMode.None);
            await LoadVereineAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Bearbeiten eines Vereins.");
            MessageBox.Show("Verein konnte nicht gespeichert werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    internal async void OpenDeleteVereinPage_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out var vereinId))
        {
            return;
        }

        await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
        var verein = await dbContext.Vereine.FirstOrDefaultAsync(x => x.Id == vereinId);
        if (verein is null)
        {
            return;
        }

        _deleteVereinId = verein.Id;
        VereineViewControl.DeleteVereinTextBlock.Text = $"{verein.MitgliedsNummer} - {verein.Vereinsname}";
        ShowVereinDialog(VereinDialogMode.Delete);
    }

    internal async void ConfirmDeleteVerein_OnClick(object sender, RoutedEventArgs e)
    {
        if (_deleteVereinId is null)
        {
            return;
        }

        try
        {
            await using var dbContext = await _localDbContextFactory.CreateDbContextAsync();
            var verein = await dbContext.Vereine.FirstOrDefaultAsync(x => x.Id == _deleteVereinId.Value);
            if (verein is not null)
            {
                dbContext.Vereine.Remove(verein);
                await dbContext.SaveChangesAsync();
            }

            _deleteVereinId = null;
            ShowVereinDialog(VereinDialogMode.None);
            await LoadVereineAsync();
            await RefreshSyncStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fehler beim Loeschen eines Vereins.");
            MessageBox.Show("Verein konnte nicht geloescht werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    internal void CancelVereinPage_OnClick(object sender, RoutedEventArgs e)
    {
        _editVereinId = null;
        _deleteVereinId = null;
        _createVereinLogoBytes = null;
        _editVereinLogoBytes = null;
        ShowVereinDialog(VereinDialogMode.None);
    }

    internal void SelectCreateVereinLogo_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TryLoadLogoFromDialog(out var logoBytes))
        {
            return;
        }

        _createVereinLogoBytes = logoBytes;
        UpdateCreateLogoPreview();
    }

    internal void ClearCreateVereinLogo_OnClick(object sender, RoutedEventArgs e)
    {
        _createVereinLogoBytes = null;
        UpdateCreateLogoPreview();
    }

    internal void SelectEditVereinLogo_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TryLoadLogoFromDialog(out var logoBytes))
        {
            return;
        }

        _editVereinLogoBytes = logoBytes;
        UpdateEditLogoPreview();
    }

    internal void ClearEditVereinLogo_OnClick(object sender, RoutedEventArgs e)
    {
        _editVereinLogoBytes = null;
        UpdateEditLogoPreview();
    }

    private bool TryLoadLogoFromDialog(out byte[] logoBytes)
    {
        logoBytes = [];

        var dialog = new OpenFileDialog
        {
            Filter = "Bilddateien (*.bmp;*.jpg;*.jpeg;*.png)|*.bmp;*.jpg;*.jpeg;*.png",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return false;
        }

        var extension = Path.GetExtension(dialog.FileName);
        if (!AllowedLogoExtensions.Contains(extension))
        {
            MessageBox.Show("Bitte ein Logo im Format BMP, JPG oder PNG auswaehlen.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        try
        {
            logoBytes = File.ReadAllBytes(dialog.FileName);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Logo-Datei konnte nicht gelesen werden.");
            MessageBox.Show("Logo konnte nicht geladen werden. Details stehen im Log.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private void UpdateCreateLogoPreview()
    {
        VereineViewControl.CreateLogoStatusTextBlock.Text = _createVereinLogoBytes is { Length: > 0 }
            ? "Logo ausgewaehlt"
            : "Kein Logo ausgewaehlt";
        VereineViewControl.CreateVereinLogoPreviewImage.Source = CreateImageSourceFromBytes(_createVereinLogoBytes);
    }

    private void UpdateEditLogoPreview()
    {
        VereineViewControl.EditLogoStatusTextBlock.Text = _editVereinLogoBytes is { Length: > 0 }
            ? "Logo ausgewaehlt"
            : "Kein Logo ausgewaehlt";
        VereineViewControl.EditVereinLogoPreviewImage.Source = CreateImageSourceFromBytes(_editVereinLogoBytes);
    }

    private static ImageSource? CreateImageSourceFromBytes(byte[]? logoBytes)
    {
        if (logoBytes is null || logoBytes.Length == 0)
        {
            return null;
        }

        try
        {
            using var memoryStream = new MemoryStream(logoBytes);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = memoryStream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }

    private void ShowVereinDialog(VereinDialogMode mode)
    {
        VereineViewControl.VereinCreatePage.Visibility = mode == VereinDialogMode.Create ? Visibility.Visible : Visibility.Collapsed;
        VereineViewControl.VereinEditPage.Visibility = mode == VereinDialogMode.Edit ? Visibility.Visible : Visibility.Collapsed;
        VereineViewControl.VereinDeletePage.Visibility = mode == VereinDialogMode.Delete ? Visibility.Visible : Visibility.Collapsed;
    }
}
