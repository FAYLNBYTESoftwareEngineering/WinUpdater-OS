using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using WinUpdater.Models;
using WinUpdater.Services;
using WinUpdater.Views;

namespace WinUpdater.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly WingetService wingetService = new();
    private readonly IgnoreListService ignoreService = new();
    private CancellationTokenSource? cancellationTokenSource;

    private string installedOutputText = string.Empty;
    private string logOutputText = string.Empty;
    private string statusText = "Bereit";
    private double progressValue;
    private string filterText = string.Empty;
    private readonly string appVersion = "2.0.0";
    private bool isExecuting;
    private bool isOperationCompleted;
    private object? currentView;

    // Installations-Fortschritt
    private string installCurrentApp = string.Empty;
    private string installPhase = string.Empty;
    private double installProgress;
    private double installOverallProgress;
    private string installOverallText = string.Empty;
    private bool hasMultiplePackages;

    private readonly CollectionViewSource updateViewSource = new();

    public MainViewModel()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var infoVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(infoVersion))
            appVersion = infoVersion.Split('+')[0];
        else
        {
            var v = assembly.GetName().Version;
            if (v != null) appVersion = $"{v.Major}.{v.Minor}.{v.Build}";
        }

        updateViewSource.Source = UpdateList;
        updateViewSource.Filter += OnUpdateFilter;

        HomeViewInstance = new HomeView { DataContext = this };
        UpdatesViewInstance = new UpdatesView { DataContext = this };
        InstalledViewInstance = new InstalledView { DataContext = this };
        HistoryViewInstance = new HistoryView { DataContext = this };
        LogPanelInstance = new LogPanel { DataContext = this };
        IgnoreViewInstance = new IgnoreView { DataContext = this };

        currentView = HomeViewInstance;
    }

    // ── View-Instanzen ────────────────────────────────────────────────────────
    public object HomeViewInstance { get; }
    public object UpdatesViewInstance { get; }
    public object InstalledViewInstance { get; }
    public object HistoryViewInstance { get; }
    public object LogPanelInstance { get; }
    public object IgnoreViewInstance { get; }

    // ── Titel ─────────────────────────────────────────────────────────────────
    public string AppVersion => appVersion;

    public string AppTitle => string.IsNullOrWhiteSpace(statusText) || statusText == "Bereit"
        ? $"WinUpdater v{appVersion}"
        : $"WinUpdater v{appVersion}  –  {statusText}";

    // ── Navigation ────────────────────────────────────────────────────────────
    public object? CurrentView
    {
        get => currentView;
        set
        {
            currentView = value;
            OnPropertyChanged();
        }
    }

    // ── Daten ─────────────────────────────────────────────────────────────────
    private ObservableCollection<WingetItem> UpdateList { get; } = new();

    public ICollectionView UpdateView => updateViewSource.View;
    public ObservableCollection<string> IgnoredPackages { get; } = new();

    // ── Properties ────────────────────────────────────────────────────────────
    public string InstalledOutputText
    {
        get => installedOutputText;
        set
        {
            installedOutputText = value;
            OnPropertyChanged();
        }
    }

    public string LogOutputText
    {
        get => logOutputText;
        set
        {
            logOutputText = value;
            OnPropertyChanged();
        }
    }

    public string StatusText
    {
        get => statusText;
        set
        {
            statusText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AppTitle));
        }
    }

    public double ProgressValue
    {
        get => progressValue;
        set
        {
            progressValue = value;
            OnPropertyChanged();
        }
    }

    public string FilterText
    {
        get => filterText;
        set
        {
            filterText = value;
            OnPropertyChanged();
            updateViewSource.View.Refresh();
        }
    }

    public bool IsExecuting
    {
        get => isExecuting;
        set
        {
            isExecuting = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanStop));
            OnPropertyChanged(nameof(IsOperationCompleted));
        }
    }

    public bool IsOperationCompleted
    {
        get => isOperationCompleted;
        set
        {
            isOperationCompleted = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanStop));
        }
    }

    // Schalten die Sichtbarkeiten für Abbrechen/Schließen sauber
    public bool CanStop => isExecuting && !isOperationCompleted;
    public string LogFilePath => wingetService.LogFilePath;

    public ICommand CloseCommand => new RelayCommand(dummy => CloseDialog());

    // ── Fortschritts-Properties ───────────────────────────────────────────────
    public string InstallCurrentApp
    {
        get => installCurrentApp;
        set
        {
            installCurrentApp = value;
            OnPropertyChanged();
        }
    }

    public string InstallPhase
    {
        get => installPhase;
        set
        {
            installPhase = value;
            OnPropertyChanged();
        }
    }

    public double InstallProgress
    {
        get => installProgress;
        set
        {
            installProgress = value;
            OnPropertyChanged();
        }
    }

    public double InstallOverallProgress
    {
        get => installOverallProgress;
        set
        {
            installOverallProgress = value;
            OnPropertyChanged();
        }
    }

    public string InstallOverallText
    {
        get => installOverallText;
        set
        {
            installOverallText = value;
            OnPropertyChanged();
        }
    }

    public bool HasMultiplePackages
    {
        get => hasMultiplePackages;
        set
        {
            hasMultiplePackages = value;
            OnPropertyChanged();
        }
    }

    public ICommand CancelCommand => new RelayCommand(dummy => CancelCurrentOperation());

    public async Task<string> RunDiagnoseAsync() => await wingetService.DiagnoseAsync();

    // ── Filter ────────────────────────────────────────────────────────────────
    private void OnUpdateFilter(object sender, FilterEventArgs e)
    {
        if (e.Item is not WingetItem item)
        {
            e.Accepted = false;
            return;
        }

        if (ignoreService.IsIgnored(item.Id))
        {
            e.Accepted = false;
            return;
        }

        if (!string.IsNullOrWhiteSpace(filterText))
        {
            var f = filterText.Trim();
            e.Accepted = item.Name.Contains(f, StringComparison.OrdinalIgnoreCase)
                         || item.Id.Contains(f, StringComparison.OrdinalIgnoreCase);
            return;
        }

        e.Accepted = true;
    }

    // ── Operationen ───────────────────────────────────────────────────────────
    private void CancelCurrentOperation()
    {
        if (cancellationTokenSource is { IsCancellationRequested: false })
        {
            cancellationTokenSource.Cancel();
            StatusText = "🛑 Abbruch angefordert… bitte warten.";
        }
    }

    private void CloseDialog()
    {
        IsExecuting = false;
        IsOperationCompleted = false;
        ProgressValue = 0;
        InstallProgress = 0;
        InstallOverallProgress = 0;
        InstallCurrentApp = string.Empty;
        InstallPhase = string.Empty;
        InstallOverallText = string.Empty;
        StatusText = "Bereit";

        // Updates im Hintergrund neu laden, NACHDEM der Dialog geschlossen wurde
        _ = CheckForUpdatesAsync();
    }

    public async Task LoadInstalledProgramsAsync()
    {
        InstalledOutputText = string.Empty;
        LogOutputText = string.Empty;
        StatusText = "Installierte Programme werden geladen…";
        ProgressValue = 0;
        IsExecuting = true;
        IsOperationCompleted = false;
        cancellationTokenSource = new CancellationTokenSource();

        try
        {
            await wingetService.RunCommandAsync(
                ["list"],
                outData =>
                {
                    InstalledOutputText += outData + Environment.NewLine;
                    LogOutputText += outData + Environment.NewLine;
                    ProgressValue = Math.Min(95, ProgressValue + 0.5);
                },
                errData => { LogOutputText += $"⚠ {errData}\n"; },
                cancellationTokenSource.Token);

            StatusText = cancellationTokenSource.Token.IsCancellationRequested
                ? "❌ Abgebrochen."
                : $"✅ Liste geladen ({DateTime.Now:T})";
            ProgressValue = 100;
        }
        catch (Exception ex)
        {
            LogOutputText += $"[ERR] {ex}\n";
            StatusText = "❌ Fehler beim Laden. Details im Log.";
        }
        finally
        {
            IsOperationCompleted = true;
        }
    }

    public async Task CheckForUpdatesAsync()
    {
        StatusText = "🔍 Suche nach Updates…";

        try
        {
            string rawOutput = await wingetService.GetOutputAsync(["upgrade"]);
            var updateResults = wingetService.ParseUpgradeOutput(rawOutput);

            UpdateList.Clear();
            foreach (var item in updateResults)
            {
                UpdateList.Add(item);
            }

            StatusText = $"✅ {updateResults.Count} Update(s) gefunden.";
        }
        catch (Exception ex)
        {
            StatusText = "❌ Fehler bei der Updatesuche.";
            LogOutputText += $"\n[Fehler] {ex.Message}";
        }
    }

    public async Task UpdateSelectedAsync()
    {
        var selectedApps = UpdateView.Cast<WingetItem>()
            .Where(x => x.IsSelected)
            .ToList();

        if (!selectedApps.Any())
        {
            MessageBox.Show(
                "Bitte hake mindestens ein Programm in der Liste an.",
                "Keine Auswahl",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        IsExecuting = true;
        IsOperationCompleted = false;
        LogOutputText = string.Empty;
        HasMultiplePackages = selectedApps.Count > 1;
        InstallProgress = 0;
        InstallOverallProgress = 0;
        cancellationTokenSource = new CancellationTokenSource();

        try
        {
            for (int i = 0; i < selectedApps.Count; i++)
            {
                if (cancellationTokenSource.Token.IsCancellationRequested)
                    break;

                var app = selectedApps[i];
                StatusText = $"Installiere: {app.Name}…";
                InstallCurrentApp = app.Name;
                InstallPhase = "⏳ Wird vorbereitet…";
                InstallProgress = 0;
                InstallOverallText = $"Paket {i + 1} von {selectedApps.Count}";
                InstallOverallProgress = (double)i / selectedApps.Count * 100;

                var args = new List<string>
                {
                    "upgrade", "--id", app.Id,
                    "--accept-source-agreements", "--accept-package-agreements", "-h"
                };
                bool success = await wingetService.RunCommandAsync(
                    args,
                    outData =>
                    {
                        LogOutputText += outData + Environment.NewLine;
                        ParseAndApplyProgress(outData);
                    },
                    errData => { LogOutputText += $"⚠ {errData}\n"; },
                    cancellationTokenSource.Token);

                if (!success)
                {
                    StatusText = $"❌ Fehler beim Update von {app.Name}. Siehe Log für Details.";
                    LogOutputText += $"[ERR] Winget returned non‑zero exit code for package {app.Id}\n";
                    ToastService.Show("WinUpdater", $"❌ Update von {app.Name} fehlgeschlagen.");
                    break;
                }

                InstallProgress = 100;
                InstallPhase = "✅ Fertig";
                InstallOverallProgress = (double)(i + 1) / selectedApps.Count * 100;
            }

            bool cancelled = cancellationTokenSource.Token.IsCancellationRequested;
            if (cancelled)
            {
                StatusText = "❌ Vorgang gestoppt.";
            }
            else
            {
                StatusText = "✅ Ausgewählte Updates abgeschlossen.";
                ToastService.Show("WinUpdater",
                    $"✅ {selectedApps.Count} Update(s) erfolgreich installiert.");
            }
        }
        catch (Exception ex)
        {
            LogOutputText += $"[ERR] {ex}\n";
            StatusText = "❌ Fehler beim Update. Details im Log.";
        }
        finally
        {
            IsOperationCompleted = true;
        }
    }

    private void ParseAndApplyProgress(string line)
    {
        var l = line.Trim().ToLowerInvariant();

        var pctMatch = Regex.Match(l, @"(\d{1,3})\s*%");
        if (pctMatch.Success && double.TryParse(pctMatch.Groups[1].Value, out double pct))
        {
            InstallProgress = Math.Clamp(pct, 0, 99);
            return;
        }

        if (l.Contains("wird heruntergeladen") || l.Contains("downloading"))
        {
            InstallPhase = "⬇ Herunterladen…";
            InstallProgress = Math.Max(InstallProgress, 10);
        }
        else if (l.Contains("hash") || l.Contains("wird überprüft") || l.Contains("verifying"))
        {
            InstallPhase = "🔍 Prüfe Integrität…";
            InstallProgress = Math.Max(InstallProgress, 60);
        }
        else if (l.Contains("wird installiert") || l.Contains("installing") || l.Contains("startet"))
        {
            InstallPhase = "⚙ Installieren…";
            InstallProgress = Math.Max(InstallProgress, 70);
        }
        else if (l.Contains("erfolgreich") || l.Contains("successfully installed") || l.Contains("wurde installiert"))
        {
            InstallPhase = "✅ Fertig";
            InstallProgress = 99;
        }
        else if (l.Contains("fehlgeschlagen") || l.Contains("failed"))
        {
            InstallPhase = "❌ Fehlgeschlagen";
        }
    }

    // ── Auswahl-Helfer ────────────────────────────────────────────────────────
    public void SelectAll()
    {
        foreach (var item in UpdateView.Cast<WingetItem>())
            item.IsSelected = true;
    }

    public void SelectNone()
    {
        foreach (var item in UpdateView.Cast<WingetItem>())
            item.IsSelected = false;
    }

    // ── Ignore-Liste ──────────────────────────────────────────────────────────
    public void RefreshIgnoreList()
    {
        IgnoredPackages.Clear();
        foreach (var id in ignoreService.IgnoredIds.OrderBy(x => x))
            IgnoredPackages.Add(id);
    }

    public void IgnorePackage(WingetItem item)
    {
        ignoreService.Add(item.Id);
        updateViewSource.View.Refresh();
        RefreshIgnoreList();
        int visible = UpdateView.Cast<WingetItem>().Count();
        StatusText = $"🚫 \"{item.Name}\" wird ignoriert. Noch {visible} Update(s) sichtbar.";
    }

    public void UnignorePackage(string id)
    {
        ignoreService.Remove(id);
        updateViewSource.View.Refresh();
        RefreshIgnoreList();
        StatusText = $"✅ \"{id}\" wird nicht mehr ignoriert.";
    }

    // ── INotifyPropertyChanged ────────────────────────────────────────────────
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}