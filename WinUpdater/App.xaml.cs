using System.Windows;

namespace WinUpdater;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Startet das Hauptfenster komplett parameterlos nach neuem Standard
        var mainWindow = new MainWindow();
        mainWindow.Show();
    }
}