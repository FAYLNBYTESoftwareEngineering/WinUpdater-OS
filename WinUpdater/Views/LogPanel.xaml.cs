using System.Windows.Controls;

namespace WinUpdater.Views;

public partial class LogPanel
{
    public LogPanel()
    {
        InitializeComponent();
    }

    // Auto-Scroll: bei neuem Text automatisch ans Ende scrollen
    private void LogTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        LogScrollViewer.ScrollToEnd();
    }
}