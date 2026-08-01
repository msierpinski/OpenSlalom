using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenSlalom.UI.Views;

public partial class TrainingsView : UserControl
{
    internal MainWindow? Host { get; set; }

    public TrainingsView()
    {
        InitializeComponent();
    }

    private void OpenCreateTrainingPage_OnClick(object sender, RoutedEventArgs e) => Host?.OpenCreateTrainingPage_OnClick(sender, e);
    private void SaveCreateTraining_OnClick(object sender, RoutedEventArgs e) => Host?.SaveCreateTraining_OnClick(sender, e);
    private void OpenDetailTrainingPage_OnClick(object sender, RoutedEventArgs e) => Host?.OpenDetailTrainingPage_OnClick(sender, e);
    private void OpenEditTrainingPage_OnClick(object sender, RoutedEventArgs e) => Host?.OpenEditTrainingPage_OnClick(sender, e);
    private void SaveEditTraining_OnClick(object sender, RoutedEventArgs e) => Host?.SaveEditTraining_OnClick(sender, e);
    private void OpenDeleteTrainingPage_OnClick(object sender, RoutedEventArgs e) => Host?.OpenDeleteTrainingPage_OnClick(sender, e);
    private void ConfirmDeleteTraining_OnClick(object sender, RoutedEventArgs e) => Host?.ConfirmDeleteTraining_OnClick(sender, e);
    private void CancelTrainingPage_OnClick(object sender, RoutedEventArgs e) => Host?.CancelTrainingPage_OnClick(sender, e);
    private void TrainingStatisticsDataGrid_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e) => Host?.TrainingStatisticsDataGrid_OnPreviewMouseWheel(sender, e);
    private void NextTrainingDriver_OnClick(object sender, RoutedEventArgs e) => Host?.NextTrainingDriver_OnClick(sender, e);
    private void SkipTrainingDriver_OnClick(object sender, RoutedEventArgs e) => Host?.SkipTrainingDriver_OnClick(sender, e);
    private void TrainingStarterRow_OnMouseDoubleClick(object sender, MouseButtonEventArgs e) => Host?.TrainingStarterRow_OnMouseDoubleClick(sender, e);
    private void TrainingStarterDataGrid_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => Host?.TrainingStarterDataGrid_OnPreviewMouseLeftButtonDown(sender, e);
    private void TrainingStarterDataGrid_OnPreviewMouseMove(object sender, MouseEventArgs e) => Host?.TrainingStarterDataGrid_OnPreviewMouseMove(sender, e);
    private void TrainingStarterDataGrid_OnDragOver(object sender, DragEventArgs e) => Host?.TrainingStarterDataGrid_OnDragOver(sender, e);
    private void TrainingStarterDataGrid_OnDrop(object sender, DragEventArgs e) => Host?.TrainingStarterDataGrid_OnDrop(sender, e);
    private void FinishTraining_OnClick(object sender, RoutedEventArgs e) => Host?.FinishTraining_OnClick(sender, e);
    private void TrainingStarterFaehrtCheckBox_OnClick(object sender, RoutedEventArgs e) => Host?.TrainingStarterFaehrtCheckBox_OnClick(sender, e);
    private void TrainingStarterKartComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => Host?.TrainingStarterKartComboBox_OnSelectionChanged(sender, e);
    private void TrainingRoundsTextBox_OnTextChanged(object sender, TextChangedEventArgs e) => Host?.TrainingRoundsTextBox_OnTextChanged(sender, e);
    private void TrainingAllowExtraRoundsCheckBox_OnClick(object sender, RoutedEventArgs e) => Host?.TrainingAllowExtraRoundsCheckBox_OnClick(sender, e);
    private void TrainingSecondTimingCheckBox_OnClick(object sender, RoutedEventArgs e) => Host?.TrainingSecondTimingCheckBox_OnClick(sender, e);
    private void TrainingSecondStart_OnClick(object sender, RoutedEventArgs e) => Host?.TrainingSecondStart_OnClick(sender, e);
    private void TrainingSecondLap_OnClick(object sender, RoutedEventArgs e) => Host?.TrainingSecondLap_OnClick(sender, e);
    private void TrainingSecondClear_OnClick(object sender, RoutedEventArgs e) => Host?.TrainingSecondClear_OnClick(sender, e);
    private void SaveSecondTrainingStint_OnClick(object sender, RoutedEventArgs e) => Host?.SaveSecondTrainingStint_OnClick(sender, e);
    private void SecondLapNumericAdjust_OnClick(object sender, RoutedEventArgs e) => Host?.SecondLapNumericAdjust_OnClick(sender, e);
    private void SecondLapInvalidCheckBox_OnClick(object sender, RoutedEventArgs e) => Host?.SecondLapInvalidCheckBox_OnClick(sender, e);
    private void TrainingStoredStintDriverComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => Host?.TrainingStoredStintDriverComboBox_OnSelectionChanged(sender, e);
    private void StoredTrainingLapNumericAdjust_OnClick(object sender, RoutedEventArgs e) => Host?.StoredTrainingLapNumericAdjust_OnClick(sender, e);
    private void StoredTrainingLapInvalidCheckBox_OnClick(object sender, RoutedEventArgs e) => Host?.StoredTrainingLapInvalidCheckBox_OnClick(sender, e);
    private void OpenAddTrainingFahrerDialog_OnClick(object sender, RoutedEventArgs e) => Host?.OpenAddTrainingFahrerDialog_OnClick(sender, e);
    private void TrainingStopwatchStart_OnClick(object sender, RoutedEventArgs e) => Host?.TrainingStopwatchStart_OnClick(sender, e);
    private void TrainingStopwatchStop_OnClick(object sender, RoutedEventArgs e) => Host?.TrainingStopwatchStop_OnClick(sender, e);
    private void ClearTrainingStint_OnClick(object sender, RoutedEventArgs e) => Host?.ClearTrainingStint_OnClick(sender, e);
    private void SaveFirstTrainingStint_OnClick(object sender, RoutedEventArgs e) => Host?.SaveFirstTrainingStint_OnClick(sender, e);
    private void LapNumericAdjust_OnClick(object sender, RoutedEventArgs e) => Host?.LapNumericAdjust_OnClick(sender, e);
    private void LapInvalidCheckBox_OnClick(object sender, RoutedEventArgs e) => Host?.LapInvalidCheckBox_OnClick(sender, e);
    private void TrainingDriverSelectionCheckBox_OnClick(object sender, RoutedEventArgs e) => Host?.TrainingDriverSelectionCheckBox_OnClick(sender, e);
    private void TrainingFahrerSearchTextBox_OnTextChanged(object sender, TextChangedEventArgs e) => Host?.TrainingFahrerSearchTextBox_OnTextChanged(sender, e);
    private void CancelAddTrainingFahrerDialog_OnClick(object sender, RoutedEventArgs e) => Host?.CancelAddTrainingFahrerDialog_OnClick(sender, e);
    private void SaveAddTrainingFahrerDialog_OnClick(object sender, RoutedEventArgs e) => Host?.SaveAddTrainingFahrerDialog_OnClick(sender, e);
}
