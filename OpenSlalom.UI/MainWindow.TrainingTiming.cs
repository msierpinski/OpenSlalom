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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;
using Microsoft.Win32;

namespace OpenSlalom.UI;

public partial class MainWindow
{
    private (int TrainingId, int FahrerId)? GetActiveTrainingDriverContext()
    {
        if (_selectedTrainingDetailId is null)
        {
            return null;
        }

        var activeDriver = TrainingStarterListItems.FirstOrDefault(x => x.IsAktiv);
        if (activeDriver is null)
        {
            return null;
        }

        return (_selectedTrainingDetailId.Value, activeDriver.FahrerId);
    }

    private TrainingStintState GetOrCreateTrainingStintState((int TrainingId, int FahrerId) context)
    {
        if (_trainingStintsByDriver.TryGetValue(context, out var existingState))
        {
            return existingState;
        }

        var state = new TrainingStintState();
        _trainingStintsByDriver[context] = state;
        return state;
    }

    private static string FormatTrainingTime(TimeSpan elapsed)
    {
        return $"{(int)elapsed.TotalSeconds:00}.{elapsed.Milliseconds:000}";
    }

    /// <summary>
    /// Formats a time gap in seconds with the specified number of decimals.
    /// </summary>
    private static string FormatGap(TimeSpan time, int decimals = 3)
    {
        return time.TotalSeconds.ToString($"F{decimals}").Replace(",", ".");
    }

    private double CalculateLapPenaltySeconds(TrainingLapTimeListItem lap)
    {
        var raw = (lap.Tore * _selectedTrainingTorfehlerPenaltySeconds) + (lap.Pylonen * _selectedTrainingPylonenfehlerPenaltySeconds);
        return Math.Round(Math.Max(0d, raw), 3, MidpointRounding.AwayFromZero);
    }

    private void RecalculateLapPenaltiesForCurrentContext()
    {
        if (_trainingStopwatchContext is null ||
            !_trainingStintsByDriver.TryGetValue(_trainingStopwatchContext.Value, out var state))
        {
            return;
        }

        foreach (var lap in state.LapRecords)
        {
            lap.ZeitstrafeSekunden = CalculateLapPenaltySeconds(lap);
        }
    }

    private void UpdateTrainingLapProgressDisplay()
    {
        if (TrainingsViewControl.TrainingLapCounterTextBlock is null)
        {
            return;
        }

        if (_selectedTrainingDetailId is null || _trainingStopwatchContext is null)
        {
            TrainingsViewControl.TrainingLapCounterTextBlock.Text = "Runde: -/-";
            return;
        }

        var roundsTarget = GetRoundsTargetForTraining(_selectedTrainingDetailId.Value);
        var state = GetOrCreateTrainingStintState(_trainingStopwatchContext.Value);
        var currentLap = roundsTarget > 0 && state.LapRecords.Count >= roundsTarget
            ? roundsTarget
            : state.LapRecords.Count + 1;

        TrainingsViewControl.TrainingLapCounterTextBlock.Text = roundsTarget > 0
            ? $"Runde: {currentLap}/{roundsTarget}"
            : $"Runde: {currentLap}/-";
    }

    private void UpdateTrainingLapSummaryDisplay()
    {
        if (TrainingsViewControl.TrainingTotalTimeTextBlock is null || TrainingsViewControl.TrainingAverageTimeTextBlock is null)
        {
            return;
        }

        if (_trainingStopwatchContext is null ||
            !_trainingStintsByDriver.TryGetValue(_trainingStopwatchContext.Value, out var state) ||
            state.LapRecords.Count == 0)
        {
            TrainingsViewControl.TrainingTotalTimeTextBlock.Text = "-";
            TrainingsViewControl.TrainingAverageTimeTextBlock.Text = "-";
            return;
        }

        var validLaps = state.LapRecords.Where(x => !x.Ungueltig).ToList();
        if (validLaps.Count == 0)
        {
            TrainingsViewControl.TrainingTotalTimeTextBlock.Text = "-";
            TrainingsViewControl.TrainingAverageTimeTextBlock.Text = "-";
            return;
        }

        var totalSeconds = validLaps.Sum(x => x.Rundenzeit.TotalSeconds + x.ZeitstrafeSekunden);
        var totalTime = TimeSpan.FromSeconds(totalSeconds);
        var avgTime = TimeSpan.FromSeconds(totalSeconds / validLaps.Count);

        TrainingsViewControl.TrainingTotalTimeTextBlock.Text = FormatTrainingTime(totalTime);
        TrainingsViewControl.TrainingAverageTimeTextBlock.Text = FormatTrainingTime(avgTime);
    }

    private void RefreshTrainingLapTimesTable()
    {
        TrainingLapTimeItems.Clear();
        if (_trainingStopwatchContext is null)
        {
            UpdateTrainingLapSummaryDisplay();
            return;
        }

        if (!_trainingStintsByDriver.TryGetValue(_trainingStopwatchContext.Value, out var state))
        {
            UpdateTrainingLapSummaryDisplay();
            return;
        }

        foreach (var lap in state.LapRecords)
        {
            TrainingLapTimeItems.Add(lap);
        }

        UpdateTrainingLapSummaryDisplay();
    }

    private void SyncTrainingStopwatchContextWithActiveDriver(bool resetIfContextChanges)
    {
        var newContext = GetActiveTrainingDriverContext();
        if (_trainingStopwatchContext == newContext)
        {
            RefreshTrainingLapTimesTable();
            UpdateTrainingStopwatchDisplay();
            UpdateTrainingStopwatchButtonsState();
            return;
        }

        if (_trainingStopwatchContext is not null &&
            _trainingStintsByDriver.TryGetValue(_trainingStopwatchContext.Value, out var previousState) &&
            previousState.Stopwatch.IsRunning)
        {
            previousState.Stopwatch.Stop();
        }

        _trainingStopwatchContext = newContext;

        if (_trainingStopwatchContext is null)
        {
            StopTrainingStopwatchTimerIfIdle();

            TrainingLapTimeItems.Clear();
            TrainingsViewControl.TrainingStopwatchTextBlock.Text = "00.000";
            UpdateTrainingLapSummaryDisplay();
            UpdateTrainingLapProgressDisplay();
            UpdateTrainingStopwatchButtonsState();
            return;
        }

        var currentState = GetOrCreateTrainingStintState(_trainingStopwatchContext.Value);
        if (resetIfContextChanges)
        {
            currentState.Stopwatch.Reset();
            currentState.LapRecords.Clear();
            currentState.LastLapCheckpoint = TimeSpan.Zero;
            currentState.IsFinished = false;
        }

        RefreshTrainingLapTimesTable();
        UpdateTrainingStopwatchDisplay();

        if (currentState.Stopwatch.IsRunning)
        {
            if (!_trainingStopwatchTimer.IsEnabled)
            {
                _trainingStopwatchTimer.Start();
            }
        }
        else
        {
            StopTrainingStopwatchTimerIfIdle();
        }

        UpdateTrainingStopwatchButtonsState();
    }

    private void ResetTrainingStopwatchView()
    {
        if (_trainingStopwatchContext is not null &&
            _trainingStintsByDriver.TryGetValue(_trainingStopwatchContext.Value, out var currentState) &&
            currentState.Stopwatch.IsRunning)
        {
            currentState.Stopwatch.Stop();
        }

        _trainingStopwatchContext = null;
        StopTrainingStopwatchTimerIfIdle();

        TrainingLapTimeItems.Clear();
        TrainingsViewControl.TrainingStopwatchTextBlock.Text = "00.000";
        UpdateTrainingLapSummaryDisplay();
        UpdateTrainingLapProgressDisplay();
        UpdateTrainingStopwatchButtonsState();
        ResetSecondTrainingStopwatchView();
    }

    private void TrainingStopwatchTimer_OnTick(object? sender, EventArgs e)
    {
        UpdateTrainingStopwatchDisplay();
        UpdateSecondTrainingStopwatchDisplay();
    }

    internal void TrainingStopwatchStart_OnClick(object sender, RoutedEventArgs e)
    {
        SyncTrainingStopwatchContextWithActiveDriver(resetIfContextChanges: false);
        if (_trainingStopwatchContext is null)
        {
            return;
        }

        var state = GetOrCreateTrainingStintState(_trainingStopwatchContext.Value);
        if (state.Stopwatch.IsRunning)
        {
            state.Stopwatch.Stop();
            state.IsFinished = true;
            StopTrainingStopwatchTimerIfIdle();

            UpdateTrainingStopwatchDisplay();
            UpdateTrainingStopwatchButtonsState();
            UpdateTrainingDriverButtonsState();
            return;
        }

        if (state.IsFinished)
        {
            return;
        }

        state.Stopwatch.Start();
        if (!_trainingStopwatchTimer.IsEnabled)
        {
            _trainingStopwatchTimer.Start();
        }

        UpdateTrainingStopwatchDisplay();
        UpdateTrainingStopwatchButtonsState();
        UpdateTrainingDriverButtonsState();
    }

    internal void TrainingStopwatchStop_OnClick(object sender, RoutedEventArgs e)
    {
        SyncTrainingStopwatchContextWithActiveDriver(resetIfContextChanges: false);
        if (_trainingStopwatchContext is null)
        {
            return;
        }

        var state = GetOrCreateTrainingStintState(_trainingStopwatchContext.Value);
        if (!state.Stopwatch.IsRunning)
        {
            return;
        }

        var elapsed = state.Stopwatch.Elapsed;
        var lapTime = elapsed - state.LastLapCheckpoint;
        if (lapTime <= TimeSpan.Zero)
        {
            return;
        }

        state.LapRecords.Add(new TrainingLapTimeListItem
        {
            Nummer = state.LapRecords.Count + 1,
            Rundenzeit = lapTime,
            RundenzeitText = FormatTrainingTime(lapTime),
            ZeitstrafeSekunden = 0d,
            Pylonen = 0,
            Tore = 0,
            Ungueltig = false
        });
        var lastLap = state.LapRecords[^1];
        lastLap.ZeitstrafeSekunden = CalculateLapPenaltySeconds(lastLap);
        state.LastLapCheckpoint = elapsed;

        var roundsTarget = _selectedTrainingDetailId is null
            ? 0
            : GetRoundsTargetForTraining(_selectedTrainingDetailId.Value);
        var canExceedRoundsTarget = _selectedTrainingDetailId is not null &&
                                    CanExceedRoundsTargetForTraining(_selectedTrainingDetailId.Value);
        if (!canExceedRoundsTarget && roundsTarget > 0 && state.LapRecords.Count >= roundsTarget)
        {
            state.Stopwatch.Stop();
            state.IsFinished = true;
            StopTrainingStopwatchTimerIfIdle();
        }

        RefreshTrainingLapTimesTable();
        UpdateTrainingStopwatchDisplay();
        UpdateTrainingStopwatchButtonsState();
        UpdateTrainingDriverButtonsState();
    }

    internal void ClearTrainingStint_OnClick(object sender, RoutedEventArgs e)
    {
        SyncTrainingStopwatchContextWithActiveDriver(resetIfContextChanges: false);
        if (_trainingStopwatchContext is null)
        {
            return;
        }

        if (_trainingStintsByDriver.TryGetValue(_trainingStopwatchContext.Value, out var state))
        {
            state.Stopwatch.Reset();
            state.LapRecords.Clear();
            state.LastLapCheckpoint = TimeSpan.Zero;
            state.IsFinished = false;
        }

        StopTrainingStopwatchTimerIfIdle();

        RefreshTrainingLapTimesTable();
        UpdateTrainingStopwatchDisplay();
        UpdateTrainingStopwatchButtonsState();
        UpdateTrainingDriverButtonsState();
    }

    private void UpdateTrainingStopwatchDisplay()
    {
        if (_trainingStopwatchContext is null)
        {
            TrainingsViewControl.TrainingStopwatchTextBlock.Text = "00.000";
            UpdateTrainingLapProgressDisplay();
            return;
        }

        var state = GetOrCreateTrainingStintState(_trainingStopwatchContext.Value);
        var currentLapElapsed = state.Stopwatch.Elapsed - state.LastLapCheckpoint;
        if (currentLapElapsed < TimeSpan.Zero)
        {
            currentLapElapsed = TimeSpan.Zero;
        }

        TrainingsViewControl.TrainingStopwatchTextBlock.Text = FormatTrainingTime(currentLapElapsed);
        UpdateTrainingLapProgressDisplay();
    }

    private void UpdateTrainingStopwatchButtonsState()
    {
        if (_trainingStopwatchContext is null)
        {
            TrainingsViewControl.TrainingStopwatchStartButton.IsEnabled = false;
            TrainingsViewControl.TrainingStopwatchStartButton.Content = BuildShortcutButtonContent("Start", "Q");
            TrainingsViewControl.TrainingStopwatchStopButton.IsEnabled = false;
            TrainingsViewControl.TrainingStopwatchStopButton.Content = BuildShortcutButtonContent("Runde", "W");
            TrainingsViewControl.TrainingSaveStintButton.IsEnabled = false;
            TrainingsViewControl.TrainingSaveStintButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
            UpdateTrainingRoundsTargetPulse();
            return;
        }

        var state = GetOrCreateTrainingStintState(_trainingStopwatchContext.Value);
        var roundsTarget = _selectedTrainingDetailId is null ? 0 : GetRoundsTargetForTraining(_selectedTrainingDetailId.Value);
        var canExceedRoundsTarget = _selectedTrainingDetailId is not null &&
                                    CanExceedRoundsTargetForTraining(_selectedTrainingDetailId.Value);
        var isLastLap = !canExceedRoundsTarget && roundsTarget > 0 && state.LapRecords.Count >= roundsTarget - 1;
        var stintFinished = state.IsFinished ||
                            (!canExceedRoundsTarget && roundsTarget > 0 && state.LapRecords.Count >= roundsTarget);

        if (state.Stopwatch.IsRunning)
        {
            TrainingsViewControl.TrainingStopwatchStartButton.IsEnabled = true;
            TrainingsViewControl.TrainingStopwatchStartButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
            TrainingsViewControl.TrainingStopwatchStartButton.Content = BuildShortcutButtonContent("Stop", "Q");
        }
        else if (!stintFinished)
        {
            TrainingsViewControl.TrainingStopwatchStartButton.IsEnabled = true;
            TrainingsViewControl.TrainingStopwatchStartButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00AA00"));
            TrainingsViewControl.TrainingStopwatchStartButton.Content = BuildShortcutButtonContent("Start", "Q");
        }
        else
        {
            TrainingsViewControl.TrainingStopwatchStartButton.IsEnabled = false;
            TrainingsViewControl.TrainingStopwatchStartButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BBBBBB"));
            TrainingsViewControl.TrainingStopwatchStartButton.Content = BuildShortcutButtonContent("Start", "Q");
        }
        TrainingsViewControl.TrainingStopwatchStopButton.IsEnabled = state.Stopwatch.IsRunning;
        TrainingsViewControl.TrainingStopwatchStopButton.Content = BuildShortcutButtonContent(isLastLap ? "Stop" : "Runde", "W");
        var canSaveStint = !_nextDriverSwitchInProgress && !state.Stopwatch.IsRunning && stintFinished;
        TrainingsViewControl.TrainingSaveStintButton.IsEnabled = canSaveStint;
        TrainingsViewControl.TrainingSaveStintButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(canSaveStint ? "#16A34A" : "#64748B"));

        if (isLastLap)
        {
            TrainingsViewControl.TrainingStopwatchStopButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
            if (!state.Stopwatch.IsRunning)
                TrainingsViewControl.TrainingStopwatchStopButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BBBBBB"));
        }
        else
        {
            TrainingsViewControl.TrainingStopwatchStopButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F84DE"));
        }

        UpdateTrainingRoundsTargetPulse();
    }

    private void UpdateTrainingRoundsTargetPulse()
    {
        var targetReached = false;
        if (_selectedTrainingDetailId is not null &&
            _trainingStopwatchContext is not null &&
            _trainingStintsByDriver.TryGetValue(_trainingStopwatchContext.Value, out var state))
        {
            var roundsTarget = GetRoundsTargetForTraining(_selectedTrainingDetailId.Value);
            targetReached = roundsTarget > 0 && state.LapRecords.Count >= roundsTarget;
        }

        if (_trainingRoundsTargetPulseActive == targetReached)
        {
            return;
        }

        _trainingRoundsTargetPulseActive = targetReached;
        var background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(targetReached ? "#DCFCE7" : "#FFFFFF"));
        TrainingsViewControl.TrainingStopwatchBorder.Background = background;

        if (!targetReached)
        {
            return;
        }

        background.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation
        {
            From = (Color)ColorConverter.ConvertFromString("#DCFCE7"),
            To = (Color)ColorConverter.ConvertFromString("#4ADE80"),
            Duration = TimeSpan.FromMilliseconds(700),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        });
    }

    internal void LapNumericAdjust_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not TrainingLapTimeListItem lapItem)
        {
            return;
        }

        var tagParts = button.Tag?.ToString()?.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tagParts is null || tagParts.Length != 2 || !int.TryParse(tagParts[1], out var delta))
        {
            return;
        }

        switch (tagParts[0])
        {
            case "Pylonen":
                lapItem.Pylonen = Math.Max(0, lapItem.Pylonen + delta);
                lapItem.ZeitstrafeSekunden = CalculateLapPenaltySeconds(lapItem);
                UpdateTrainingLapSummaryDisplay();
                break;
            case "Tore":
                lapItem.Tore = Math.Max(0, lapItem.Tore + delta);
                lapItem.ZeitstrafeSekunden = CalculateLapPenaltySeconds(lapItem);
                UpdateTrainingLapSummaryDisplay();
                break;
        }
    }

    internal void LapInvalidCheckBox_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.DataContext is TrainingLapTimeListItem lap)
        {
            lap.Ungueltig = checkBox.IsChecked == true;
        }

        UpdateTrainingLapSummaryDisplay();
    }

    private void MainWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (TrainingsViewControl.TrainingDetailPage.Visibility != Visibility.Visible || IsTypingInEditableControl())
        {
            return;
        }

        var modifiers = Keyboard.Modifiers;
        var isCtrl = (modifiers & ModifierKeys.Control) == ModifierKeys.Control;

        if (isCtrl && e.Key == Key.S && TrainingsViewControl.NextDriverButton.IsEnabled)
        {
            TrainingsViewControl.NextDriverButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            e.Handled = true;
            return;
        }

        if (isCtrl && e.Key == Key.D && TrainingsViewControl.SkipDriverButton.IsEnabled)
        {
            TrainingsViewControl.SkipDriverButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            e.Handled = true;
            return;
        }

        if (modifiers != ModifierKeys.None)
        {
            return;
        }

        if (e.Key == Key.A && TrainingsViewControl.TrainingSecondStartButton.IsEnabled && TrainingsViewControl.TrainingSecondStopwatchPanel.Visibility == Visibility.Visible)
        {
            TrainingsViewControl.TrainingSecondStartButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            e.Handled = true;
            return;
        }

        if (e.Key == Key.S && TrainingsViewControl.TrainingSecondLapButton.IsEnabled && TrainingsViewControl.TrainingSecondStopwatchPanel.Visibility == Visibility.Visible)
        {
            TrainingsViewControl.TrainingSecondLapButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            e.Handled = true;
            return;
        }

        if (e.Key == Key.D && TrainingsViewControl.TrainingSecondClearButton.IsEnabled && TrainingsViewControl.TrainingSecondStopwatchPanel.Visibility == Visibility.Visible)
        {
            TrainingsViewControl.TrainingSecondClearButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Q && TrainingsViewControl.TrainingStopwatchStartButton.IsEnabled)
        {
            TrainingsViewControl.TrainingStopwatchStartButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            e.Handled = true;
            return;
        }

        if (e.Key == Key.W && TrainingsViewControl.TrainingStopwatchStopButton.IsEnabled)
        {
            TrainingsViewControl.TrainingStopwatchStopButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            e.Handled = true;
            return;
        }

        if (e.Key == Key.E && TrainingsViewControl.TrainingClearStintButton.IsEnabled)
        {
            TrainingsViewControl.TrainingClearStintButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            e.Handled = true;
        }
    }

    private static object BuildShortcutButtonContent(string label, string shortcut)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        panel.Children.Add(new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Text = label
        });

        panel.Children.Add(new Border
        {
            Margin = new Thickness(8, 0, 0, 0),
            Padding = new Thickness(6, 1, 6, 1),
            CornerRadius = new CornerRadius(2),
            Background = Brushes.White,
            Child = new TextBlock
            {
                Foreground = Brushes.Black,
                FontSize = 11,
                Text = shortcut
            }
        });

        return panel;
    }

    private static bool IsTypingInEditableControl()
    {
        if (Keyboard.FocusedElement is TextBoxBase)
        {
            return true;
        }

        if (Keyboard.FocusedElement is ComboBox comboBox && comboBox.IsEditable)
        {
            return true;
        }

        return false;
    }
}
