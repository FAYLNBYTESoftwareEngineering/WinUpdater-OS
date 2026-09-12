using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using WinUpdater.Models;

namespace WinUpdater.Services;

public class WingetService
{
    private readonly string logFile;

    // ── Pfad-Auflösung mit Fallback-Kette ────────────────────────────────────
    private static readonly string WingetPath = ResolveWingetPath();

    private static string ResolveWingetPath()
    {
        // Primär: Standard Store-Installationspfad
        var storePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Microsoft\WindowsApps\winget.exe");
        if (File.Exists(storePath)) return storePath;

        // Fallback: PATH-Suche (bewusster Fallback, wenn Store-Pfad nicht existiert)
        return "winget";
    }

    private const int ProcessTimeoutMs = 300_000; // 5 Minuten

    public WingetService()
    {
        string appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinUpdater");

        Directory.CreateDirectory(appDataFolder);
        logFile = Path.Combine(appDataFolder, "UpdateLog.txt");
    }

    public string LogFilePath => logFile;

    /// <summary>
    /// Diagnosemethode: Zeigt genau, was winget zurückgibt und warum der Parser
    /// evtl. scheitert. Das Ergebnis wird als strukturierter String zurückgegeben.
    /// </summary>
    public async Task<string> DiagnoseAsync()
    {
        var sb = new StringBuilder();
        sb.AppendLine("╔══════════════════════════════════════════════════════╗");
        sb.AppendLine("║           WinUpdater – Diagnose-Report               ║");
        sb.AppendLine("╚══════════════════════════════════════════════════════╝");
        sb.AppendLine();

        // 1. winget-Pfad prüfen
        sb.AppendLine("── 1. winget-Pfad ───────────────────────────────────────");
        sb.AppendLine($"  Auflösung: {WingetPath}");
        bool wingetExists = WingetPath != "winget" && File.Exists(WingetPath);
        sb.AppendLine(WingetPath == "winget"
            ? "  ⚠ Kein absoluter Pfad gefunden – nutze PATH-Fallback"
            : wingetExists
                ? "  ✅ Datei existiert"
                : "  ❌ Datei existiert NICHT – winget nicht installiert?");
        sb.AppendLine();

        // 2. winget --version prüfen
        sb.AppendLine("── 2. winget --version ──────────────────────────────────");
        try
        {
            string version = await GetOutputAsync(["--version"]);
            sb.AppendLine($"  ✅ Antwort: {version.Trim()}");
        }
        catch (Exception ex)
        {
            sb.AppendLine($"  ❌ Fehler: {ex.GetType().Name} – {ex.Message}");
        }

        sb.AppendLine();

        // 3. Rohe winget-Ausgabe von "upgrade" holen
        sb.AppendLine("── 3. Rohe winget upgrade-Ausgabe ───────────────────────");
        string rawOutput;
        try
        {
            rawOutput = await GetOutputAsync(["upgrade"]);

            if (string.IsNullOrWhiteSpace(rawOutput))
            {
                sb.AppendLine("  ❌ Ausgabe ist LEER – winget hat nichts zurückgegeben");
            }
            else
            {
                sb.AppendLine($"  ✅ {rawOutput.Length} Zeichen empfangen");
                sb.AppendLine($"  Erste 500 Zeichen (RAW mit sichtbaren Sonderzeichen):");
                sb.AppendLine();

                // Steuerzeichen sichtbar machen
                var visible = rawOutput.Length > 500 ? rawOutput[..500] : rawOutput;
                visible = visible
                    .Replace("\r", "·CR·")
                    .Replace("\u001b", "·ESC·") // ANSI-Escape
                    .Replace("\0", "·NUL·");
                sb.AppendLine(visible);
            }
        }
        catch (Exception ex)
        {
            rawOutput = string.Empty;
            sb.AppendLine($"  ❌ Fehler: {ex.GetType().Name} – {ex.Message}");
        }

        sb.AppendLine();

        // 4. Trennlinie suchen
        sb.AppendLine("── 4. Parser-Analyse ────────────────────────────────────");
        if (!string.IsNullOrWhiteSpace(rawOutput))
        {
            var lines = rawOutput.Split('\n');
            sb.AppendLine($"  Zeilen gesamt: {lines.Length}");

            bool foundSeparator = false;
            int separatorLine = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim().StartsWith("---"))
                {
                    foundSeparator = true;
                    separatorLine = i;
                    sb.AppendLine($"  ✅ Trennlinie '---' gefunden in Zeile {i}: \"{lines[i].Trim()}\"");
                    break;
                }
            }

            if (!foundSeparator)
            {
                sb.AppendLine("  ❌ Keine Trennlinie '---' gefunden!");
                sb.AppendLine("  ── Alle Zeilen (für manuelle Prüfung): ──");
                for (int i = 0; i < Math.Min(lines.Length, 30); i++)
                {
                    var l = lines[i].Replace("\r", "·CR·").Replace("\u001b", "·ESC·");
                    sb.AppendLine($"  [{i,2}] \"{l}\"");
                }
            }
            else
            {
                // Zeilen nach Trennlinie parsen
                int parsed = 0;
                int skipped = 0;
                for (int i = separatorLine + 1; i < lines.Length; i++)
                {
                    var trimmed = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(trimmed)) continue;
                    if (trimmed.StartsWith("💡") ||
                        trimmed.StartsWith("Anleitung") ||
                        trimmed.StartsWith("Mindestens") ||
                        Regex.IsMatch(trimmed, @"^\d+ Aktualisierung"))
                        continue;

                    var tokens = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length >= 4)
                    {
                        parsed++;
                    }
                    else
                    {
                        skipped++;
                        if (skipped <= 5)
                            sb.AppendLine(
                                $"  ⚠ Zeile übersprungen ({tokens.Length} Tokens): \"{trimmed[..Math.Min(80, trimmed.Length)]}\"");
                    }
                }

                sb.AppendLine($"  ✅ Erfolgreich geparste Einträge: {parsed}");
                if (skipped > 0)
                    sb.AppendLine($"  ⚠ Übersprungene Zeilen: {skipped} (zu wenig Spalten-Abstände)");
            }
        }

        sb.AppendLine();

        // 5. Encoding-Check
        sb.AppendLine("── 5. Encoding ──────────────────────────────────────────");
        sb.AppendLine($"  Konsolen-Encoding: {Console.OutputEncoding.EncodingName}");
        sb.AppendLine($"  System-Encoding:   {Encoding.Default.EncodingName}");
        sb.AppendLine();

        sb.AppendLine("── Ende des Diagnose-Reports ────────────────────────────");
        return sb.ToString();
    }

    public async Task<bool> RunCommandAsync(
        IReadOnlyList<string> args,
        Action<string>? onOutputReceived,
        Action<string>? onErrorReceived,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return false;

        var startInfo = new ProcessStartInfo
        {
            FileName = WingetPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardErrorEncoding = Encoding.UTF8,
            StandardOutputEncoding = Encoding.UTF8
        };

        foreach (var arg in args)
            startInfo.ArgumentList.Add(arg);

        var process = new Process { StartInfo = startInfo };
        var fullOutput = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                fullOutput.AppendLine(e.Data);
                onOutputReceived?.Invoke(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                onErrorReceived?.Invoke(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await using (cancellationToken.Register(() =>
                     {
                         try
                         {
                             if (!process.HasExited) process.Kill();
                         }
                         catch
                         {
                             // Fehler beim Kill ignorieren
                         }
                     }))
        {
            try
            {
                bool exited = await Task.Run(
                    () => process.WaitForExit(ProcessTimeoutMs),
                    cancellationToken);

                if (!exited && !process.HasExited)
                {
                    process.Kill();
                    onErrorReceived?.Invoke("⚠ Winget hat das Zeitlimit überschritten und wurde beendet.");
                    LogToFile(args, "[TIMEOUT] Prozess nach 5 Minuten beendet.");
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        LogToFile(args, fullOutput.ToString());
        return process.ExitCode == 0;
    }

    public async Task<string> GetOutputAsync(IReadOnlyList<string> args)
    {
        return await Task.Run(() =>
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = WingetPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                };

                foreach (var arg in args)
                    startInfo.ArgumentList.Add(arg);

                var process = new Process { StartInfo = startInfo };
                process.Start();

                string output = process.StandardOutput.ReadToEnd();

                bool exited = process.WaitForExit(ProcessTimeoutMs);
                if (!exited && !process.HasExited) process.Kill();

                return output;
            }
            catch (Exception ex)
            {
                return $"[ERR] winget konnte nicht gestartet werden: {ex.GetType().Name}";
            }
        });
    }

    public List<WingetItem> ParseUpgradeOutput(string text)
    {
        var list = new List<WingetItem>();
        bool pastSeparator = false;

        foreach (var rawLine in text.Split('\n'))
        {
            // \r entfernen + ANSI-Escape-Codes strippen
            string line = rawLine.Replace("\r", "");
            line = Regex.Replace(line, @"\x1B\[[0-9;?]*[a-zA-Z]", "");
            string trimmed = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            // Trennlinie (-------) markiert den Beginn der Datenzeilen (und weitere Trennlinien überspringen)
            if (trimmed.StartsWith("---") || trimmed.StartsWith("==="))
            {
                pastSeparator = true;
                continue;
            }

            if (!pastSeparator) continue;

            // Header-Zeilen überspringen (Name, ID, Version, Verfügbar, Quelle)
            if (trimmed.StartsWith("Name", StringComparison.OrdinalIgnoreCase) &&
                (trimmed.Contains("ID", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("Id")) &&
                trimmed.Contains("Version", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Hinweistexte und nicht-relevante Zeilen überspringen
            if (trimmed.StartsWith("\ud83d\udca1") || // 💡
                trimmed.StartsWith("Anleitung", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Mindestens", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("At least", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Für die folgenden", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("The following", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Keine ", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("No ", StringComparison.OrdinalIgnoreCase) ||
                Regex.IsMatch(trimmed, @"^\d+ (Aktualisierung|update)", RegexOptions.IgnoreCase))
            {
                continue;
            }

            // An JEDEM Leerzeichen trennen, leere Einträge ignorieren
            var tokens = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Mindestens Name, ID, Version, verfügbar werden benötigt
            if (tokens.Length < 4) continue;

            // Prüfen, ob die optionale "Quelle" (Source) am Ende steht.
            // Quellen (wie "winget", "msstore") enthalten in der Regel keine Ziffern.
            string lastToken = tokens[^1];
            bool hasSource = lastToken.Equals("winget", StringComparison.OrdinalIgnoreCase) ||
                             lastToken.Equals("msstore", StringComparison.OrdinalIgnoreCase) ||
                             (!lastToken.Any(char.IsDigit) && !lastToken.Contains('.'));

            // Fallback, falls die Tokens nicht für eine Source reichen
            if (tokens.Length < 5 && hasSource)
            {
                hasSource = false;
            }

            // Von rechts nach links parsen (da diese Felder nie Leerzeichen enthalten, außer Version mit "<")
            string available;
            string version;
            string id;
            int nameTokensCount;

            if (hasSource)
            {
                available = tokens[^2];
                if (tokens is [.., _, _, "<", _, _, _])
                {
                    version = "< " + tokens[^3];
                    id = tokens[^5];
                    nameTokensCount = tokens.Length - 5;
                }
                else
                {
                    version = tokens[^3];
                    id = tokens[^4];
                    nameTokensCount = tokens.Length - 4;
                }
            }
            else
            {
                available = tokens[^1];
                if (tokens is [.., _, _, "<", _, _])
                {
                    version = "< " + tokens[^2];
                    id = tokens[^4];
                    nameTokensCount = tokens.Length - 4;
                }
                else
                {
                    version = tokens[^2];
                    id = tokens[^3];
                    nameTokensCount = tokens.Length - 3;
                }
            }

            // Sicherstellen, dass mindestens Version oder Available Ziffern enthält (um Fließtext zu verwerfen)
            if (!version.Any(char.IsDigit) && !available.Any(char.IsDigit))
            {
                continue;
            }

            // Der Name ist alles, was vor der ID steht, wieder zusammengefügt
            string name = string.Join(" ", tokens.Take(nameTokensCount));

            list.Add(new WingetItem
            {
                IsSelected = true,
                Name = name,
                Id = id,
                Version = version,
                AvailableVersion = available
            });
        }

        return list;
    }

    private void LogToFile(IReadOnlyList<string> args, string output)
    {
        try
        {
            var safeArgs = string.Join(" ", args).Replace("\r", "\\r").Replace("\n", "\\n");
            var safeOutput = output.Replace("\r\n", "\n")
                .Replace("=========================", "- - - - - - - - -");

            var logEntry =
                $"\n=========================\n" +
                $"🕒 {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                $"Befehl: winget {safeArgs}\n" +
                $"Ergebnis:\n{safeOutput}" +
                $"=========================\n";

            File.AppendAllText(logFile, logEntry, Encoding.UTF8);
        }
        catch
        {
            // Fehler beim Loggen ignorieren (z. B. Dateizugriff)
        }
    }
}