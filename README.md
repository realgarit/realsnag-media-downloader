# RealSnag Media Downloader

A cross-platform media downloader application built with Avalonia UI.

## Features

- Download media content in MP4 and MP3 formats
- Automatic metadata fetching and thumbnail display
- Progress tracking with real-time updates
- Cross-platform support (Windows, Linux, macOS)
- Auto-detection of required tools

## Prerequisites

### Required Tools

The application automatically detects and uses the following tools:

1. **yt-dlp** - Media downloader tool
2. **ffmpeg** - Audio/video processing tool

### Installation

#### Windows

1. Install Python 3.10+ from [python.org](https://python.org)
2. Install required tools via pip:
   ```bash
   pip install yt-dlp imageio-ffmpeg
   ```

#### Linux/macOS

1. Install Python 3.10+
2. Install required tools:
   ```bash
   pip install yt-dlp
   # For ffmpeg, use your package manager:
   # Ubuntu/Debian: sudo apt install ffmpeg
   # macOS: brew install ffmpeg
   ```

## Building

```bash
dotnet build
```

## Running

```bash
dotnet run
```

## Usage

1. Paste a media link in the input field
2. Select your preferred format (MP4 or MP3)
3. Click Download to start the process
4. Monitor progress in the logs section

## Technical Details

- Built with .NET 9.0 and Avalonia UI
- Auto-detects tool locations across different platforms
- Uses environment variables for portable paths
- Generic tool references for maximum compatibility

## License

This project is for educational purposes. Please respect the terms of service of the platforms you download from.
