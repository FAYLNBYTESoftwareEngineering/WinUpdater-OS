using System.Windows;
using System.Windows.Threading;

namespace WinUpdater.Views;

public partial class ToastPopupWindow
{
    public ToastPopupWindow(string title, string message)
    {
        InitializeComponent();

        TitleText.Text = title;
        MessageText.Text = message;

        // Unten rechts positionieren
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 16;
        Top = workArea.Bottom - Height - 16;

        // Nach 4 Sekunden automatisch schließen
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            Close();
        };
        timer.Start();
    }
}