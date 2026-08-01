using System.Windows;
using System.Windows.Controls;

namespace OpenSlalom.UI.Views;

public partial class VereineView : UserControl
{
    internal MainWindow? Host { get; set; }

    public VereineView()
    {
        InitializeComponent();
    }

    private void OpenCreateVereinPage_OnClick(object sender, RoutedEventArgs e) => Host?.OpenCreateVereinPage_OnClick(sender, e);
    private void OpenEditVereinPage_OnClick(object sender, RoutedEventArgs e) => Host?.OpenEditVereinPage_OnClick(sender, e);
    private void OpenDeleteVereinPage_OnClick(object sender, RoutedEventArgs e) => Host?.OpenDeleteVereinPage_OnClick(sender, e);
    private void SelectCreateVereinLogo_OnClick(object sender, RoutedEventArgs e) => Host?.SelectCreateVereinLogo_OnClick(sender, e);
    private void ClearCreateVereinLogo_OnClick(object sender, RoutedEventArgs e) => Host?.ClearCreateVereinLogo_OnClick(sender, e);
    private void CancelVereinPage_OnClick(object sender, RoutedEventArgs e) => Host?.CancelVereinPage_OnClick(sender, e);
    private void SaveCreateVerein_OnClick(object sender, RoutedEventArgs e) => Host?.SaveCreateVerein_OnClick(sender, e);
    private void SelectEditVereinLogo_OnClick(object sender, RoutedEventArgs e) => Host?.SelectEditVereinLogo_OnClick(sender, e);
    private void ClearEditVereinLogo_OnClick(object sender, RoutedEventArgs e) => Host?.ClearEditVereinLogo_OnClick(sender, e);
    private void SaveEditVerein_OnClick(object sender, RoutedEventArgs e) => Host?.SaveEditVerein_OnClick(sender, e);
    private void ConfirmDeleteVerein_OnClick(object sender, RoutedEventArgs e) => Host?.ConfirmDeleteVerein_OnClick(sender, e);
}
