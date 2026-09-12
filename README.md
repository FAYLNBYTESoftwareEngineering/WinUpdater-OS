# 🚀 WinUpdater

<p align="center">
  <strong>Ein modernes, schlankes Windows-Tool zur Verwaltung und Aktualisierung von Software via Windows Package Manager (winget).</strong>
</p>

<p align="center">
  <a href="https://github.com/FAYLNBYTESoftwareEngineering/WinUpdater/releases"><img src="https://img.shields.io/github/v/release/FAYLNBYTESoftwareEngineering/WinUpdater?style=flat-square" alt="Release"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square" alt="License: MIT"></a>
  <a href="https://dotnet.microsoft.com/"><img src="https://img.shields.io/badge/.NET-8.0%20%7C%20WPF-purple?style=flat-square" alt=".NET"></a>
  <a href="https://github.com/FAYLNBYTESoftwareEngineering/WinUpdater/issues"><img src="https://img.shields.io/github/issues/FAYLNBYTESoftwareEngineering/WinUpdater?style=flat-square" alt="Issues"></a>
</p>

---

## 📖 Über WinUpdater

**WinUpdater** bietet eine benutzerfreundliche grafische Oberfläche (GUI) für den Windows Package Manager (`winget`). Es ermöglicht Benutzern und Administratoren, installierte Anwendungen mit wenigen Klicks zu überprüfen und auf den neuesten Stand zu bringen.

### ✨ Features

- 🔍 **Automatischer Scan:** Erkennt installierte Anwendungen und prüft verfügbare Updates über `winget`.
- ⚡ **Batch-Updates:** Aktualisiere einzelne oder alle Programme gleichzeitig mit einem Klick.
- ⚙️ **Autostart-Integration:** Optionale Hintergrundprüfung über System-Autostart (`HKLM` / `HKCU`).
- 🎨 **Moderne Oberfläche:** Klare, responsive MVVM-Architektur auf Basis von WPF/XAML.
- 📦 **Inno Setup Installer:** Saubere Installation inklusive Administrator- und Deinstallations-Routinen.

---

## 📥 Installation

### Option 1: Installer (Empfohlen)
1. Lade das neueste Setup unter **[Releases](https://github.com/FAYLNBYTESoftwareEngineering/WinUpdater/releases)** herunter (`WinUpdater-Setup.exe`).
2. Führe den Installer aus und folge den Anweisungen auf dem Bildschirm.

### Option 2: Aus dem Quellcode bauen
Voraussetzungen:
- Windows 10/11
- [.NET SDK](https://dotnet.microsoft.com/download) (Version 8.0 oder höher)
- Visual Studio 2022 mit dem Workload *.NET Desktop-Entwicklung*

```bash
# Repository klonen
git clone https://github.com/FAYLNBYTESoftwareEngineering/WinUpdater.git

# In das Projektverzeichnis wechseln
cd WinUpdater

# Projekt bauen
dotnet build -c Release