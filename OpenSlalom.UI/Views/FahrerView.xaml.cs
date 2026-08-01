using System.Windows;
using System.Windows.Controls;

namespace OpenSlalom.UI.Views;

public partial class FahrerView : UserControl
{
    internal MainWindow? Host { get; set; }

    public FahrerView()
    {
        InitializeComponent();
    }

    private void OpenCreateFahrerPage_OnClick(object sender, RoutedEventArgs e) => Host?.OpenCreateFahrerPage_OnClick(sender, e);
    private void OpenEditFahrerPage_OnClick(object sender, RoutedEventArgs e) => Host?.OpenEditFahrerPage_OnClick(sender, e);
    private void OpenDeleteFahrerPage_OnClick(object sender, RoutedEventArgs e) => Host?.OpenDeleteFahrerPage_OnClick(sender, e);
    private void CancelFahrerPage_OnClick(object sender, RoutedEventArgs e) => Host?.CancelFahrerPage_OnClick(sender, e);
    private void SaveCreateFahrer_OnClick(object sender, RoutedEventArgs e) => Host?.SaveCreateFahrer_OnClick(sender, e);
    private void SaveEditFahrer_OnClick(object sender, RoutedEventArgs e) => Host?.SaveEditFahrer_OnClick(sender, e);
    private void ConfirmDeleteFahrer_OnClick(object sender, RoutedEventArgs e) => Host?.ConfirmDeleteFahrer_OnClick(sender, e);
}
