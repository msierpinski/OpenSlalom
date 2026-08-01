using System.Windows.Controls;

namespace OpenSlalom.UI.Views;

public partial class MeisterschaftenView : UserControl
{
    internal MainWindow? Host { get; set; }

    public MeisterschaftenView()
    {
        InitializeComponent();
    }
}
