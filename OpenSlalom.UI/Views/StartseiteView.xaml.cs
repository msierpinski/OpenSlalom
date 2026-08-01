using System.Windows;
using System.Windows.Controls;

namespace OpenSlalom.UI.Views;

public partial class StartseiteView : UserControl
{
    internal MainWindow? Host { get; set; }

    public StartseiteView()
    {
        InitializeComponent();
    }

    private void LogoImage_OnImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        LogoImage.Visibility = Visibility.Collapsed;
    }
}
