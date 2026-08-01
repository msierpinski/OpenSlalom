using System.Windows;
using System.Windows.Controls;

namespace OpenSlalom.UI.Views;

public partial class EinstellungenView : UserControl
{
    internal MainWindow? Host { get; set; }

    public EinstellungenView()
    {
        InitializeComponent();
    }

    private void SaveSettings_OnClick(object sender, RoutedEventArgs e)
    {
        Host?.SaveSettings_OnClick(sender, e);
    }

    private void ReconnectRemote_OnClick(object sender, RoutedEventArgs e)
    {
        Host?.ReconnectRemote_OnClick(sender, e);
    }
}
