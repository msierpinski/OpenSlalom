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
    private void NavigateTo(string page)
    {
        _selectedTrainingDetailId = null;
        CloseTrainingDriverSelectionDialog();
        TrainingStarterListItems.Clear();
        TrainingFastestLapItems.Clear();
        TrainingStatisticsBestLapItems.Clear();
        TrainingStatisticsDriverSections.Clear();
        ResetTrainingStopwatchView();
        StartseiteView.Visibility = Visibility.Collapsed;
        TrainingsViewControl.Visibility = Visibility.Collapsed;
        TrainingsViewControl.TrainingsPage.Visibility = Visibility.Collapsed;
        TrainingsViewControl.TrainingStatisticsPage.Visibility = Visibility.Collapsed;
        TrainingsViewControl.TrainingDetailPage.Visibility = Visibility.Collapsed;
        MeisterschaftenView.Visibility = Visibility.Collapsed;
        VereineViewControl.Visibility = Visibility.Collapsed;
        DisziplinenViewControl.Visibility = Visibility.Collapsed;
        FahrerViewControl.Visibility = Visibility.Collapsed;
        KartsViewControl.Visibility = Visibility.Collapsed;
        WetterViewControl.Visibility = Visibility.Collapsed;
        StatistikenView.Visibility = Visibility.Collapsed;
        EinstellungenView.Visibility = Visibility.Collapsed;

        switch (page)
        {
            case "Startseite":
                StartseiteView.Visibility = Visibility.Visible;
                break;
            case "Trainings":
                TrainingsViewControl.Visibility = Visibility.Visible;
                TrainingsViewControl.TrainingsPage.Visibility = Visibility.Visible;
                _ = LoadTrainingsAsync();
                break;
            case "Meisterschaften":
                MeisterschaftenView.Visibility = Visibility.Visible;
                break;
            case "Vereine":
                VereineViewControl.Visibility = Visibility.Visible;
                _ = LoadVereineAsync();
                break;
            case "Disziplin":
                DisziplinenViewControl.Visibility = Visibility.Visible;
                _ = LoadDisziplinenAsync();
                break;
            case "Fahrer":
                FahrerViewControl.Visibility = Visibility.Visible;
                _ = LoadFahrerAsync();
                break;
            case "Karts":
                KartsViewControl.Visibility = Visibility.Visible;
                _ = LoadKartsAsync();
                break;
            case "Wetter":
                WetterViewControl.Visibility = Visibility.Visible;
                _ = LoadWetterAsync();
                break;
            case "Statistiken":
                StatistikenView.Visibility = Visibility.Visible;
                _ = LoadGeneralStatisticsAsync();
                break;
            case "Einstellungen":
                EinstellungenView.Visibility = Visibility.Visible;
                ApplySettingsToUi();
                break;
            default:
                if (TryParseTrainingDetailTag(page, out var trainingId))
                {
                    _selectedTrainingDetailId = trainingId;
                    TrainingsViewControl.Visibility = Visibility.Visible;
                    TrainingsViewControl.TrainingDetailPage.Visibility = Visibility.Visible;
                    _ = LoadTrainingDetailAsync(trainingId);
                    break;
                }

                if (TryParseTrainingStatisticsTag(page, out var trainingStatisticsId))
                {
                    TrainingsViewControl.Visibility = Visibility.Visible;
                    TrainingsViewControl.TrainingStatisticsPage.Visibility = Visibility.Visible;
                    _ = LoadTrainingStatisticsAsync(trainingStatisticsId);
                    break;
                }

                StartseiteView.Visibility = Visibility.Visible;
                page = "Startseite";
                break;
        }

        ApplyTrainingRoundsToUi();
        ApplyMenuSelection(page);
    }

    private static bool TryParseTrainingDetailTag(string value, out int trainingId)
    {
        trainingId = 0;
        if (!value.StartsWith(TrainingDetailTagPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        return int.TryParse(value[TrainingDetailTagPrefix.Length..], out trainingId);
    }

    private static bool TryParseTrainingStatisticsTag(string value, out int trainingId)
    {
        trainingId = 0;
        if (!value.StartsWith(TrainingStatisticsTagPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        return int.TryParse(value[TrainingStatisticsTagPrefix.Length..], out trainingId);
    }

    private void ApplyMenuSelection(string selectedTag)
    {
        UpdateSettingsSubmenuVisibility(selectedTag);

        _selectedMenuTag = selectedTag;

        foreach (var button in GetMenuButtons())
        {
            if (button.Tag is not string tag)
            {
                continue;
            }

            if (tag == selectedTag)
            {
                button.Background = ActiveMenuBackgroundBrush;
                button.Foreground = ActiveMenuForegroundBrush;
            }
            else
            {
                button.Background = DefaultMenuBackgroundBrush;
                button.Foreground = DefaultMenuForegroundBrush;
            }
        }
    }

    private void ConfigureMenuButtons()
    {
        foreach (var button in GetMenuButtons())
        {
            ConfigureMenuButton(button);
        }

        UpdateSettingsSubmenuVisibility(_selectedMenuTag);
    }

    private void UpdateSettingsSubmenuVisibility(string selectedTag)
    {
        var show = string.Equals(selectedTag, "Einstellungen", StringComparison.Ordinal) ||
                   SettingsSubmenuTags.Contains(selectedTag);
        var visibility = show ? Visibility.Visible : Visibility.Collapsed;

        MenuSettingsVereine.Visibility = visibility;
        MenuSettingsFahrer.Visibility = visibility;
        MenuSettingsDisziplin.Visibility = visibility;
        MenuSettingsKarts.Visibility = visibility;
        MenuSettingsWetter.Visibility = visibility;
    }

    private void ConfigureMenuButton(Button button)
    {
        if (button.Tag is not string)
        {
            return;
        }

        button.MouseEnter -= MenuButton_OnMouseEnter;
        button.MouseLeave -= MenuButton_OnMouseLeave;
        button.MouseEnter += MenuButton_OnMouseEnter;
        button.MouseLeave += MenuButton_OnMouseLeave;
        button.Background = DefaultMenuBackgroundBrush;
        button.Foreground = DefaultMenuForegroundBrush;
    }

    private void ToggleSidebar_OnClick(object sender, RoutedEventArgs e)
    {
        _isSidebarCollapsed = !_isSidebarCollapsed;
        UpdateSidebarState();
    }

    private void UpdateSidebarState()
    {
        SidebarColumn.Width = new GridLength(_isSidebarCollapsed ? 22 : 220);
        MenuPanel.Visibility = _isSidebarCollapsed ? Visibility.Collapsed : Visibility.Visible;
        SidebarToggleIcon.Text = _isSidebarCollapsed ? "▶" : "◀";
        SidebarToggleButton.Margin = _isSidebarCollapsed
            ? new Thickness(0, 0, -11, 0)
            : new Thickness(0, 0, -11, 0);
    }

    private IEnumerable<Button> GetMenuButtons()
    {
        return MenuPanel.Children.OfType<Button>();
    }

    private void RefreshOpenTrainingMenuButtons()
    {
        foreach (var button in _dynamicTrainingMenuButtons)
        {
            MenuPanel.Children.Remove(button);
        }

        _dynamicTrainingMenuButtons.Clear();

        var trainingsButtonIndex = MenuPanel.Children.IndexOf(MenuTrainings);
        if (trainingsButtonIndex < 0)
        {
            return;
        }

        var insertIndex = trainingsButtonIndex + 1;
        var openTrainings = TrainingItems
            .Where(x => !x.TrainingAbgeschlossen)
            .OrderBy(x => x.Zeitpunkt)
            .ThenBy(x => x.Name)
            .ToList();

        foreach (var training in openTrainings)
        {
            var button = new Button
            {
                Style = (Style)FindResource("MenuButtonStyle"),
                Padding = new Thickness(28, 8, 10, 8),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Content = $"- {training.Name}",
                Tag = $"{TrainingDetailTagPrefix}{training.Id}"
            };

            button.Click += MenuButton_OnClick;
            ConfigureMenuButton(button);
            MenuPanel.Children.Insert(insertIndex, button);
            _dynamicTrainingMenuButtons.Add(button);
            insertIndex++;
        }

        if (_selectedMenuTag.StartsWith(TrainingDetailTagPrefix, StringComparison.Ordinal) &&
            _dynamicTrainingMenuButtons.All(x => !string.Equals(x.Tag as string, _selectedMenuTag, StringComparison.Ordinal)))
        {
            _selectedMenuTag = "Trainings";
            NavigateTo("Trainings");
            return;
        }

        ApplyMenuSelection(_selectedMenuTag);
    }

    private void MenuButton_OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string)
        {
            return;
        }

        var baseColor = ResolveMenuBackgroundColor(button);
        var hoverColor = DarkenColor(baseColor, HoverDarkenFactor);

        button.Background = new SolidColorBrush(hoverColor);
        button.Foreground = GetReadableForeground(hoverColor);
    }

    private void MenuButton_OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string tag)
        {
            return;
        }

        if (tag == _selectedMenuTag)
        {
            button.Background = ActiveMenuBackgroundBrush;
            button.Foreground = ActiveMenuForegroundBrush;
            return;
        }

        button.Background = DefaultMenuBackgroundBrush;
        button.Foreground = DefaultMenuForegroundBrush;
    }

    private static Color ResolveMenuBackgroundColor(Button button)
    {
        if (button.Background is SolidColorBrush solid && solid.Color.A > 0)
        {
            return solid.Color;
        }

        return MenuFallbackBackgroundColor;
    }

    private static Color DarkenColor(Color color, double factor)
    {
        var normalizedFactor = Math.Clamp(factor, 0, 1);
        var multiplier = 1 - normalizedFactor;

        return Color.FromArgb(
            color.A,
            (byte)(color.R * multiplier),
            (byte)(color.G * multiplier),
            (byte)(color.B * multiplier));
    }

    private static Brush GetReadableForeground(Color backgroundColor)
    {
        var brightness = ((backgroundColor.R * 299) + (backgroundColor.G * 587) + (backgroundColor.B * 114)) / 1000.0;
        return brightness < 140 ? Brushes.White : Brushes.Black;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject dependencyObject) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(dependencyObject);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(dependencyObject, i);
            if (child is T typedChild)
            {
                yield return typedChild;
            }

            foreach (var descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }

    private void MenuButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string tag)
        {
            NavigateTo(tag);
        }
    }

    private void BackToTrainingsPage_OnClick(object sender, RoutedEventArgs e)
    {
        NavigateTo("Trainings");
    }

}
