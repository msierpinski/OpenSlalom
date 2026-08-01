using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace OpenSlalom.UI;

public partial class MainWindow
{
    private (int TrainingId, int FahrerId)? GetSecondActiveTrainingDriverContext()
    {
        if (_selectedTrainingDetailId is null || !IsSecondTrainingTimingEnabled(_selectedTrainingDetailId.Value)) return null;
        var driver = TrainingStarterListItems.FirstOrDefault(x => x.IsAktivZweiteZeitnahme && x.FahrerFaehrt);
        return driver is null ? null : (_selectedTrainingDetailId.Value, driver.FahrerId);
    }

    private void UpdateSecondTrainingStopwatchContextWithActiveDriver()
    {
        var newContext = GetSecondActiveTrainingDriverContext();
        if (_trainingSecondStopwatchContext == newContext)
        {
            RefreshSecondTrainingLapTimesTable();
            UpdateSecondTrainingStopwatchDisplay();
            UpdateSecondTrainingStopwatchButtonsState();
            return;
        }

        if (_trainingSecondStopwatchContext is not null &&
            _trainingStintsByDriver.TryGetValue(_trainingSecondStopwatchContext.Value, out var previous) && previous.Stopwatch.IsRunning)
        {
            previous.Stopwatch.Stop();
        }

        _trainingSecondStopwatchContext = newContext;
        if (newContext is null)
        {
            TrainingSecondLapTimeItems.Clear();
            TrainingsViewControl.TrainingSecondActiveDriverTextBlock.Text = "-";
            TrainingsViewControl.TrainingSecondStopwatchTextBlock.Text = "00.000";
            UpdateSecondTrainingLapSummaryDisplay();
            UpdateSecondTrainingLapProgressDisplay();
            UpdateSecondTrainingStopwatchButtonsState();
            StopTrainingStopwatchTimerIfIdle();
            return;
        }

        var driver = TrainingStarterListItems.First(x => x.FahrerId == newContext.Value.FahrerId);
        TrainingsViewControl.TrainingSecondActiveDriverTextBlock.Text = string.IsNullOrWhiteSpace(driver.Nachname) ? driver.Vorname : $"{driver.Vorname} {driver.Nachname}";
        var state = GetOrCreateTrainingStintState(newContext.Value);
        RefreshSecondTrainingLapTimesTable();
        UpdateSecondTrainingStopwatchDisplay();
        if (state.Stopwatch.IsRunning && !_trainingStopwatchTimer.IsEnabled) _trainingStopwatchTimer.Start();
        UpdateSecondTrainingStopwatchButtonsState();
    }

    private void ResetSecondTrainingStopwatchView()
    {
        if (_trainingSecondStopwatchContext is not null &&
            _trainingStintsByDriver.TryGetValue(_trainingSecondStopwatchContext.Value, out var state) && state.Stopwatch.IsRunning)
        {
            state.Stopwatch.Stop();
        }

        _trainingSecondStopwatchContext = null;
        TrainingSecondLapTimeItems.Clear();
        if (TrainingsViewControl.TrainingSecondStopwatchTextBlock is not null)
        {
            TrainingsViewControl.TrainingSecondActiveDriverTextBlock.Text = "-";
            TrainingsViewControl.TrainingSecondStopwatchTextBlock.Text = "00.000";
            UpdateSecondTrainingLapSummaryDisplay();
            UpdateSecondTrainingLapProgressDisplay();
            UpdateSecondTrainingStopwatchButtonsState();
        }
        StopTrainingStopwatchTimerIfIdle();
    }

    internal void TrainingSecondStart_OnClick(object sender, RoutedEventArgs e)
    {
        UpdateSecondTrainingStopwatchContextWithActiveDriver();
        if (_trainingSecondStopwatchContext is null) return;
        var state = GetOrCreateTrainingStintState(_trainingSecondStopwatchContext.Value);
        if (state.Stopwatch.IsRunning)
        {
            state.Stopwatch.Stop();
            state.IsFinished = true;
            StopTrainingStopwatchTimerIfIdle();
        }
        else if (!state.IsFinished)
        {
            state.Stopwatch.Start();
            if (!_trainingStopwatchTimer.IsEnabled) _trainingStopwatchTimer.Start();
        }
        UpdateSecondTrainingStopwatchDisplay();
        UpdateSecondTrainingStopwatchButtonsState();
        UpdateTrainingDriverButtonsState();
    }

    internal void TrainingSecondLap_OnClick(object sender, RoutedEventArgs e)
    {
        UpdateSecondTrainingStopwatchContextWithActiveDriver();
        if (_trainingSecondStopwatchContext is null) return;
        var state = GetOrCreateTrainingStintState(_trainingSecondStopwatchContext.Value);
        if (!state.Stopwatch.IsRunning) return;
        var elapsed = state.Stopwatch.Elapsed;
        var lapTime = elapsed - state.LastLapCheckpoint;
        if (lapTime <= TimeSpan.Zero) return;
        var lap = new TrainingLapTimeListItem
        {
            Nummer = state.LapRecords.Count + 1,
            Rundenzeit = lapTime,
            RundenzeitText = FormatTrainingTime(lapTime),
            ZeitstrafeSekunden = 0d,
            Pylonen = 0,
            Tore = 0,
            Ungueltig = false
        };
        lap.ZeitstrafeSekunden = CalculateLapPenaltySeconds(lap);
        state.LapRecords.Add(lap);
        state.LastLapCheckpoint = elapsed;
        var trainingId = _trainingSecondStopwatchContext.Value.TrainingId;
        var target = GetRoundsTargetForTraining(trainingId);
        if (!CanExceedRoundsTargetForTraining(trainingId) && target > 0 && state.LapRecords.Count >= target)
        {
            state.Stopwatch.Stop();
            state.IsFinished = true;
            StopTrainingStopwatchTimerIfIdle();
        }
        RefreshSecondTrainingLapTimesTable();
        UpdateSecondTrainingStopwatchDisplay();
        UpdateSecondTrainingStopwatchButtonsState();
        UpdateTrainingDriverButtonsState();
    }

    internal void TrainingSecondClear_OnClick(object sender, RoutedEventArgs e)
    {
        UpdateSecondTrainingStopwatchContextWithActiveDriver();
        if (_trainingSecondStopwatchContext is null) return;
        var state = GetOrCreateTrainingStintState(_trainingSecondStopwatchContext.Value);
        state.Stopwatch.Reset();
        state.LapRecords.Clear();
        state.LastLapCheckpoint = TimeSpan.Zero;
        state.IsFinished = false;
        StopTrainingStopwatchTimerIfIdle();
        RefreshSecondTrainingLapTimesTable();
        UpdateSecondTrainingStopwatchDisplay();
        UpdateSecondTrainingStopwatchButtonsState();
        UpdateTrainingDriverButtonsState();
    }

    private void RefreshSecondTrainingLapTimesTable()
    {
        TrainingSecondLapTimeItems.Clear();
        if (_trainingSecondStopwatchContext is not null && _trainingStintsByDriver.TryGetValue(_trainingSecondStopwatchContext.Value, out var state))
            foreach (var lap in state.LapRecords) TrainingSecondLapTimeItems.Add(lap);
        UpdateSecondTrainingLapSummaryDisplay();
    }

    private void UpdateSecondTrainingLapSummaryDisplay()
    {
        if (_trainingSecondStopwatchContext is null || !_trainingStintsByDriver.TryGetValue(_trainingSecondStopwatchContext.Value, out var state))
        {
            TrainingsViewControl.TrainingSecondTotalTimeTextBlock.Text = "-";
            TrainingsViewControl.TrainingSecondAverageTimeTextBlock.Text = "-";
            return;
        }
        var laps = state.LapRecords.Where(x => !x.Ungueltig).ToList();
        if (laps.Count == 0)
        {
            TrainingsViewControl.TrainingSecondTotalTimeTextBlock.Text = "-";
            TrainingsViewControl.TrainingSecondAverageTimeTextBlock.Text = "-";
            return;
        }
        var total = laps.Sum(x => x.Rundenzeit.TotalSeconds + x.ZeitstrafeSekunden);
        TrainingsViewControl.TrainingSecondTotalTimeTextBlock.Text = FormatTrainingTime(TimeSpan.FromSeconds(total));
        TrainingsViewControl.TrainingSecondAverageTimeTextBlock.Text = FormatTrainingTime(TimeSpan.FromSeconds(total / laps.Count));
    }

    private void UpdateSecondTrainingLapProgressDisplay()
    {
        if (_trainingSecondStopwatchContext is null)
        {
            TrainingsViewControl.TrainingSecondLapCounterTextBlock.Text = "Runde: -/-";
            return;
        }
        var state = GetOrCreateTrainingStintState(_trainingSecondStopwatchContext.Value);
        var target = GetRoundsTargetForTraining(_trainingSecondStopwatchContext.Value.TrainingId);
        var current = target > 0 && state.LapRecords.Count >= target ? state.LapRecords.Count : state.LapRecords.Count + 1;
        TrainingsViewControl.TrainingSecondLapCounterTextBlock.Text = target > 0 ? $"Runde: {current}/{target}" : $"Runde: {current}/-";
    }

    private void UpdateSecondTrainingStopwatchDisplay()
    {
        if (_trainingSecondStopwatchContext is null)
        {
            if (TrainingsViewControl.TrainingSecondStopwatchTextBlock is not null) TrainingsViewControl.TrainingSecondStopwatchTextBlock.Text = "00.000";
            return;
        }
        var state = GetOrCreateTrainingStintState(_trainingSecondStopwatchContext.Value);
        var elapsed = state.Stopwatch.Elapsed - state.LastLapCheckpoint;
        TrainingsViewControl.TrainingSecondStopwatchTextBlock.Text = FormatTrainingTime(elapsed < TimeSpan.Zero ? TimeSpan.Zero : elapsed);
        UpdateSecondTrainingLapProgressDisplay();
    }

    private void UpdateSecondTrainingStopwatchButtonsState()
    {
        if (TrainingsViewControl.TrainingSecondStartButton is null) return;
        if (_trainingSecondStopwatchContext is null)
        {
            TrainingsViewControl.TrainingSecondStartButton.IsEnabled = false;
            TrainingsViewControl.TrainingSecondLapButton.IsEnabled = false;
            TrainingsViewControl.TrainingSecondSaveStintButton.IsEnabled = false;
            TrainingsViewControl.TrainingSecondSaveStintButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
            UpdateSecondTrainingRoundsTargetPulse();
            return;
        }
        var state = GetOrCreateTrainingStintState(_trainingSecondStopwatchContext.Value);
        var trainingId = _trainingSecondStopwatchContext.Value.TrainingId;
        var target = GetRoundsTargetForTraining(trainingId);
        var canExceed = CanExceedRoundsTargetForTraining(trainingId);
        var lastLap = !canExceed && target > 0 && state.LapRecords.Count >= target - 1;
        var finished = state.IsFinished || (!canExceed && target > 0 && state.LapRecords.Count >= target);
        TrainingsViewControl.TrainingSecondStartButton.IsEnabled = state.Stopwatch.IsRunning || !finished;
        TrainingsViewControl.TrainingSecondStartButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(state.Stopwatch.IsRunning ? "#DC2626" : finished ? "#BBBBBB" : "#00AA00"));
        TrainingsViewControl.TrainingSecondStartButton.Content = BuildShortcutButtonContent(state.Stopwatch.IsRunning ? "Stop" : "Start", "A");
        TrainingsViewControl.TrainingSecondLapButton.IsEnabled = state.Stopwatch.IsRunning;
        TrainingsViewControl.TrainingSecondLapButton.Content = BuildShortcutButtonContent(lastLap ? "Stop" : "Runde", "S");
        TrainingsViewControl.TrainingSecondLapButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(lastLap ? "#DC2626" : "#1F84DE"));
        var canSaveStint = !_nextDriverSwitchInProgress && !state.Stopwatch.IsRunning && finished;
        TrainingsViewControl.TrainingSecondSaveStintButton.IsEnabled = canSaveStint;
        TrainingsViewControl.TrainingSecondSaveStintButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(canSaveStint ? "#16A34A" : "#64748B"));
        UpdateSecondTrainingRoundsTargetPulse();
    }

    internal void SecondLapNumericAdjust_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not TrainingLapTimeListItem lap) return;
        var parts = button.Tag?.ToString()?.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts is null || parts.Length != 2 || !int.TryParse(parts[1], out var delta)) return;
        if (parts[0] == "Pylonen") lap.Pylonen = Math.Max(0, lap.Pylonen + delta);
        if (parts[0] == "Tore") lap.Tore = Math.Max(0, lap.Tore + delta);
        lap.ZeitstrafeSekunden = CalculateLapPenaltySeconds(lap);
        UpdateSecondTrainingLapSummaryDisplay();
    }

    internal void SecondLapInvalidCheckBox_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.DataContext is TrainingLapTimeListItem lap) lap.Ungueltig = checkBox.IsChecked == true;
        UpdateSecondTrainingLapSummaryDisplay();
    }

    private bool IsSecondTrainingStopwatchRunning() => _trainingSecondStopwatchContext is not null && _trainingStintsByDriver.TryGetValue(_trainingSecondStopwatchContext.Value, out var state) && state.Stopwatch.IsRunning;

    private void StopTrainingStopwatchTimerIfIdle()
    {
        var firstRunning = _trainingStopwatchContext is not null && _trainingStintsByDriver.TryGetValue(_trainingStopwatchContext.Value, out var first) && first.Stopwatch.IsRunning;
        if (!firstRunning && !IsSecondTrainingStopwatchRunning() && _trainingStopwatchTimer.IsEnabled) _trainingStopwatchTimer.Stop();
    }

    private void UpdateSecondTrainingRoundsTargetPulse()
    {
        var reached = false;
        if (_trainingSecondStopwatchContext is not null && _trainingStintsByDriver.TryGetValue(_trainingSecondStopwatchContext.Value, out var state))
        {
            var target = GetRoundsTargetForTraining(_trainingSecondStopwatchContext.Value.TrainingId);
            reached = target > 0 && state.LapRecords.Count >= target;
        }
        if (_trainingSecondRoundsTargetPulseActive == reached) return;
        _trainingSecondRoundsTargetPulseActive = reached;
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(reached ? "#DCFCE7" : "#FFF7ED"));
        TrainingsViewControl.TrainingSecondStopwatchPanel.Background = brush;
        if (reached)
            brush.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation { From = (Color)ColorConverter.ConvertFromString("#DCFCE7"), To = (Color)ColorConverter.ConvertFromString("#4ADE80"), Duration = TimeSpan.FromMilliseconds(700), AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
    }
}
