using System;
using System.Collections.Generic;

namespace realsnag_media_downloader.Services;

public interface ILocalizationService
{
    string CurrentLanguage { get; set; }
    IEnumerable<string> AvailableLanguages { get; }
    string GetString(string key);
    event EventHandler<LanguageChangedEventArgs>? LanguageChanged;
}
