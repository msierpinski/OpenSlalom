using System.Windows;
using System.Windows.Controls;

namespace OpenSlalom.UI.Views;

public partial class KartsView : UserControl
{
    internal MainWindow? Host { get; set; }

    public KartsView()
    {
        InitializeComponent();
    }

    private void OpenCreateKartPage_OnClick(object sender, RoutedEventArgs e) => Host?.OpenCreateKartPage_OnClick(sender, e);
    private void OpenEditKartPage_OnClick(object sender, RoutedEventArgs e) => Host?.OpenEditKartPage_OnClick(sender, e);
    private void OpenDeleteKartPage_OnClick(object sender, RoutedEventArgs e) => Host?.OpenDeleteKartPage_OnClick(sender, e);
    private void CancelKartPage_OnClick(object sender, RoutedEventArgs e) => Host?.CancelKartPage_OnClick(sender, e);
    private void SaveCreateKart_OnClick(object sender, RoutedEventArgs e) => Host?.SaveCreateKart_OnClick(sender, e);
    private void SaveEditKart_OnClick(object sender, RoutedEventArgs e) => Host?.SaveEditKart_OnClick(sender, e);
    private void ConfirmDeleteKart_OnClick(object sender, RoutedEventArgs e) => Host?.ConfirmDeleteKart_OnClick(sender, e);
}
