using System;
using System.Collections.Generic;

namespace realsnag_media_downloader.Services;

public class LocalizationService
{
    private static readonly Lazy<LocalizationService> _instance = new(() => new LocalizationService());
    public static LocalizationService Instance => _instance.Value;

    public event EventHandler<LanguageChangedEventArgs>? LanguageChanged;

    private readonly Dictionary<string, Dictionary<string, string>> _translations;
    private string _currentLanguage = "en";

    public LocalizationService()
    {
        _translations = new Dictionary<string, Dictionary<string, string>>
        {
            ["en"] = new()
            {
                ["AppTitle"] = "Realgar Media Downloader",
                ["EnterLink"] = "Enter media URL",
                ["MediaMetadata"] = "Paste a link above to see media info",
                ["SelectFormat"] = "Format",
                ["Quality"] = "Quality",
                ["BestAvailable"] = "Best Available",
                ["AudioOnly"] = "Audio Only",
                ["Status"] = "Status",
                ["Idle"] = "Ready",
                ["Downloading"] = "Downloading...",
                ["Complete"] = "Complete",
                ["Error"] = "Error",
                ["Logs"] = "Logs",
                ["Download"] = "Download",
                ["Cancel"] = "Cancel",
                ["ClearLogs"] = "Clear Logs",
                ["ErrorValidLink"] = "Please enter a valid media link.",
                ["DownloadCancelled"] = "Download cancelled.",
                ["NoActiveDownload"] = "No active download to cancel.",
                ["DownloadCancelledError"] = "Download was cancelled.",
                ["ErrorDuringDownload"] = "Error during download:",
                ["ErrorFetchingMetadata"] = "Error fetching metadata:",
                ["Settings"] = "Settings",
                ["Theme"] = "Theme",
                ["DarkTheme"] = "Dark",
                ["LightTheme"] = "Light",
                ["Language"] = "Language",
                ["English"] = "English",
                ["German"] = "Deutsch",
                ["DownloadPath"] = "Download Path",
                ["Browse"] = "Browse",
                ["TrimVideo"] = "Trim video",
                ["StartTime"] = "Start",
                ["EndTime"] = "End",
                ["TimeFormat"] = "HH:MM:SS",
                ["UpdateYtDlp"] = "Update yt-dlp",
                ["SettingUp"] = "Setting up...",
                ["InstallingYtDlp"] = "Installing yt-dlp...",
                ["SetupComplete"] = "Setup complete",
                ["SetupFailed"] = "Setup failed",
                ["FetchingInfo"] = "Fetching info...",
                ["YtDlpVersion"] = "yt-dlp Version",
                ["AutoUpdate"] = "Auto-update yt-dlp",
                ["FfmpegStatus"] = "ffmpeg",
                ["Found"] = "Found",
                ["NotFound"] = "Not found",
                ["About"] = "About",
                ["SaveTo"] = "Save to",
                ["UrlPlaceholder"] = "https://www.youtube.com/watch?v=..."
            },
            ["de"] = new()
            {
                ["AppTitle"] = "Realgar Media Downloader",
                ["EnterLink"] = "Medien-URL eingeben",
                ["MediaMetadata"] = "Einen Link oben einfügen, um Medieninfo zu sehen",
                ["SelectFormat"] = "Format",
                ["Quality"] = "Qualität",
                ["BestAvailable"] = "Beste verfügbar",
                ["AudioOnly"] = "Nur Audio",
                ["Status"] = "Status",
                ["Idle"] = "Bereit",
                ["Downloading"] = "Lädt herunter...",
                ["Complete"] = "Abgeschlossen",
                ["Error"] = "Fehler",
                ["Logs"] = "Logs",
                ["Download"] = "Herunterladen",
                ["Cancel"] = "Abbrechen",
                ["ClearLogs"] = "Logs leeren",
                ["ErrorValidLink"] = "Bitte einen gültigen Medien-Link eingeben.",
                ["DownloadCancelled"] = "Download abgebrochen.",
                ["NoActiveDownload"] = "Kein aktiver Download.",
                ["DownloadCancelledError"] = "Download wurde abgebrochen.",
                ["ErrorDuringDownload"] = "Fehler beim Download:",
                ["ErrorFetchingMetadata"] = "Fehler beim Abrufen der Metadaten:",
                ["Settings"] = "Einstellungen",
                ["Theme"] = "Design",
                ["DarkTheme"] = "Dunkel",
                ["LightTheme"] = "Hell",
                ["Language"] = "Sprache",
                ["English"] = "English",
                ["German"] = "Deutsch",
                ["DownloadPath"] = "Download-Pfad",
                ["Browse"] = "Durchsuchen",
                ["TrimVideo"] = "Video zuschneiden",
                ["StartTime"] = "Start",
                ["EndTime"] = "Ende",
                ["TimeFormat"] = "HH:MM:SS",
                ["UpdateYtDlp"] = "yt-dlp aktualisieren",
                ["SettingUp"] = "Einrichtung...",
                ["InstallingYtDlp"] = "yt-dlp wird installiert...",
                ["SetupComplete"] = "Einrichtung abgeschlossen",
                ["SetupFailed"] = "Einrichtung fehlgeschlagen",
                ["FetchingInfo"] = "Infos werden geladen...",
                ["YtDlpVersion"] = "yt-dlp Version",
                ["AutoUpdate"] = "yt-dlp automatisch aktualisieren",
                ["FfmpegStatus"] = "ffmpeg",
                ["Found"] = "Gefunden",
                ["NotFound"] = "Nicht gefunden",
                ["About"] = "Über",
                ["SaveTo"] = "Speichern unter",
                ["UrlPlaceholder"] = "https://www.youtube.com/watch?v=..."
            }
        };
    }

    public string GetString(string key)
    {
        if (_translations.TryGetValue(_currentLanguage, out var languageDict) &&
            languageDict.TryGetValue(key, out var translation))
            return translation;

        if (_translations.TryGetValue("en", out var englishDict) &&
            englishDict.TryGetValue(key, out var englishTranslation))
            return englishTranslation;

        return key;
    }

    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value && _translations.ContainsKey(value))
            {
                _currentLanguage = value;
                LanguageChanged?.Invoke(this, new LanguageChangedEventArgs(value));
            }
        }
    }

    public IEnumerable<string> AvailableLanguages => _translations.Keys;
}
