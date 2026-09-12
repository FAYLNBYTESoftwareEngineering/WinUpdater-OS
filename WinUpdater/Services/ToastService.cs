using System.Windows;
using WinUpdater.Views;

namespace WinUpdater.Services;

/// <summary>
/// Zeigt eine einfache Windows-Benachrichtigung nach Abschluss eines Updates.
/// Nutzt WPF MessageBox als Fallback – kein extra NuGet-Paket nötig.
/// Für echte Toast-Notifications kann Microsoft. Toolkit. Uwp. Notifications eingebunden werden.
/// </summary>
public static class ToastService
{
    /// <summary>
    /// Zeigt eine nicht-blockierende Info-Benachrichtigung.
    /// Läuft auf dem UI-Thread via Dispatcher.
    /// </summary>
    public static void Show(string title, string message)
    {
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            // Ballon-Tooltip über System-Tray würde NotifyIcon benötigen.
            // Als saubere Alternative: kleines eigenes Pop-up-Fenster.
            var popup = new ToastPopupWindow(title, message);
            popup.Show();
        });
    }
}