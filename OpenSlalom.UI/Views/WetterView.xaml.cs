using System.Windows;
using System.Windows.Controls;

namespace OpenSlalom.UI.Views;

public partial class WetterView : UserControl
{
    internal MainWindow? Host { get; set; }

    public WetterView()
    {
        InitializeComponent();
    }

    private void OpenCreateWetterPage_OnClick(object sender, RoutedEventArgs e) => Host?.OpenCreateWetterPage_OnClick(sender, e);
    private void OpenEditWetterPage_OnClick(object sender, RoutedEventArgs e) => Host?.OpenEditWetterPage_OnClick(sender, e);
    private void OpenDeleteWetterPage_OnClick(object sender, RoutedEventArgs e) => Host?.OpenDeleteWetterPage_OnClick(sender, e);
    private void CancelWetterPage_OnClick(object sender, RoutedEventArgs e) => Host?.CancelWetterPage_OnClick(sender, e);
    private void SaveCreateWetter_OnClick(object sender, RoutedEventArgs e) => Host?.SaveCreateWetter_OnClick(sender, e);
    private void SaveEditWetter_OnClick(object sender, RoutedEventArgs e) => Host?.SaveEditWetter_OnClick(sender, e);
    private void ConfirmDeleteWetter_OnClick(object sender, RoutedEventArgs e) => Host?.ConfirmDeleteWetter_OnClick(sender, e);
}
