# RealSnag Media Downloader

A media downloader application built with Avalonia UI.

## Features

- Download media content in MP4 and MP3 formats
- Automatic metadata fetching and thumbnail display
- Progress tracking with real-time updates
- Bundled tools - No need to install yt-dlp or ffmpeg separately
- Multi-language support (English/German)
- Dark/Light theme switching
- Modern, responsive UI design

## Prerequisites

### For Development

- .NET 9.0 SDK
- Visual Studio 2022 or VS Code (optional)

### For End Users

No additional software required! The application comes with bundled tools:
- **yt-dlp** - Media downloader (bundled)
- **ffmpeg** - Audio/video processing (bundled)

The application automatically uses the bundled tools, but can also detect system-installed tools if available.

## Usage

1. Paste a media link in the input field
2. Select your preferred format (MP4 or MP3)
3. Click Download to start the process
4. Monitor progress in the logs section

### Settings

Access the settings panel by clicking the settings button to:
- Switch between Dark and Light themes
- Change language between English and German

## Technical Details

- Built with .NET 9.0 and Avalonia UI
- Self-contained deployment with bundled tools
- Auto-detects tool locations (bundled tools take priority)
- Cross-platform tool detection for maximum compatibility
- Version: v1.1.0

## License

Copyright (c) 2025 Realgar. Licensed under the MIT License.

This project is for educational purposes. Please respect the terms of service of the platforms you download from.
