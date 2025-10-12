using System;
using System.Collections.Generic;
using System.Globalization;

namespace realsnag_media_downloader.Services;

public class LocalizationService
{
    private static readonly Lazy<LocalizationService> _instance = new(() => new LocalizationService());
    public static LocalizationService Instance => _instance.Value;

    public event EventHandler<LanguageChangedEventArgs>? LanguageChanged;

    private readonly Dictionary<string, Dictionary<string, string>> _translations;
    private string _currentLanguage = "de";

    public LocalizationService()
    {
        _translations = new Dictionary<string, Dictionary<string, string>>
        {
            ["en"] = new Dictionary<string, string>
            {
                ["AppTitle"] = "RealSnag Media Downloader",
                ["EnterLink"] = "Enter Link:",
                ["MediaMetadata"] = "Media metadata will appear here after you paste the link",
                ["SelectFormat"] = "Select Format:",
                ["Status"] = "Status:",
                ["Idle"] = "Idle",
                ["Downloading"] = "Downloading...",
                ["Complete"] = "Complete",
                ["Error"] = "Error",
                ["Logs"] = "Logs:",
                ["Download"] = "Download",
                ["Cancel"] = "Cancel",
                ["ClearLogs"] = "Clear Logs",
                ["ErrorValidLink"] = "Error: Please enter a valid media link.",
                ["DownloadCancelled"] = "Download cancelled by user.",
                ["NoActiveDownload"] = "No active download to cancel.",
                ["DownloadCancelledError"] = "Download operation was cancelled.",
                ["ErrorDuringDownload"] = "Error during download:",
                ["ErrorFetchingMetadata"] = "Error fetching metadata:",
                ["Settings"] = "Settings",
                ["Theme"] = "Theme",
                ["DarkTheme"] = "Dark",
                ["LightTheme"] = "Light",
                ["Language"] = "Language",
                ["English"] = "English",
                ["German"] = "Deutsch"
            },
            ["de"] = new Dictionary<string, string>
            {
                ["AppTitle"] = "RealSnag Media Downloader",
                ["EnterLink"] = "Link eingeben:",
                ["MediaMetadata"] = "Medien-Metadaten werden hier angezeigt, nachdem Sie den Link eingefügt haben",
                ["SelectFormat"] = "Format wählen:",
                ["Status"] = "Status:",
                ["Idle"] = "Bereit",
                ["Downloading"] = "Lädt herunter...",
                ["Complete"] = "Abgeschlossen",
                ["Error"] = "Fehler",
                ["Logs"] = "Logs:",
                ["Download"] = "Herunterladen",
                ["Cancel"] = "Abbrechen",
                ["ClearLogs"] = "Logs leeren",
                ["ErrorValidLink"] = "Fehler: Bitte geben Sie einen gültigen Medien-Link ein.",
                ["DownloadCancelled"] = "Download vom Benutzer abgebrochen.",
                ["NoActiveDownload"] = "Kein aktiver Download zum Abbrechen.",
                ["DownloadCancelledError"] = "Download-Vorgang wurde abgebrochen.",
                ["ErrorDuringDownload"] = "Fehler beim Download:",
                ["ErrorFetchingMetadata"] = "Fehler beim Abrufen der Metadaten:",
                ["Settings"] = "Einstellungen",
                ["Theme"] = "Design",
                ["DarkTheme"] = "Dunkel",
                ["LightTheme"] = "Hell",
                ["Language"] = "Sprache",
                ["English"] = "English",
                ["German"] = "Deutsch"
            }
        };
    }

    public string GetString(string key)
    {
        if (_translations.TryGetValue(_currentLanguage, out var languageDict) &&
            languageDict.TryGetValue(key, out var translation))
        {
            return translation;
        }

        // Fallback to English if key not found
        if (_translations.TryGetValue("en", out var englishDict) &&
            englishDict.TryGetValue(key, out var englishTranslation))
        {
            return englishTranslation;
        }

        return key; // Return key if no translation found
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
