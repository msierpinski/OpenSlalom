using System.Windows;
using System.Windows.Controls;

namespace OpenSlalom.UI.Views;

public partial class DisziplinenView : UserControl
{
    internal MainWindow? Host { get; set; }

    public DisziplinenView()
    {
        InitializeComponent();
    }

    private void OpenCreateDisziplinPage_OnClick(object sender, RoutedEventArgs e) => Host?.OpenCreateDisziplinPage_OnClick(sender, e);
    private void OpenEditDisziplinPage_OnClick(object sender, RoutedEventArgs e) => Host?.OpenEditDisziplinPage_OnClick(sender, e);
    private void OpenDeleteDisziplinPage_OnClick(object sender, RoutedEventArgs e) => Host?.OpenDeleteDisziplinPage_OnClick(sender, e);
    private void AddCreateDisziplinAltersklasse_OnClick(object sender, RoutedEventArgs e) => Host?.AddCreateDisziplinAltersklasse_OnClick(sender, e);
    private void RemoveCreateDisziplinAltersklasse_OnClick(object sender, RoutedEventArgs e) => Host?.RemoveCreateDisziplinAltersklasse_OnClick(sender, e);
    private void CancelDisziplinPage_OnClick(object sender, RoutedEventArgs e) => Host?.CancelDisziplinPage_OnClick(sender, e);
    private void SaveCreateDisziplin_OnClick(object sender, RoutedEventArgs e) => Host?.SaveCreateDisziplin_OnClick(sender, e);
    private void AddEditDisziplinAltersklasse_OnClick(object sender, RoutedEventArgs e) => Host?.AddEditDisziplinAltersklasse_OnClick(sender, e);
    private void RemoveEditDisziplinAltersklasse_OnClick(object sender, RoutedEventArgs e) => Host?.RemoveEditDisziplinAltersklasse_OnClick(sender, e);
    private void SaveEditDisziplin_OnClick(object sender, RoutedEventArgs e) => Host?.SaveEditDisziplin_OnClick(sender, e);
    private void ConfirmDeleteDisziplin_OnClick(object sender, RoutedEventArgs e) => Host?.ConfirmDeleteDisziplin_OnClick(sender, e);
}
