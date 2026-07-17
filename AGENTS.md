# realsnag-media-downloader — Agent instructions

> Canonical instructions for all coding agents (Claude Code, Codex, GitHub Copilot). Claude loads this via the CLAUDE.md stub.

RealSnag Media Downloader is a cross-platform desktop media downloader built with Avalonia UI and yt-dlp. It lets users download video (MP4) and audio (MP3) from supported sites, with quality selection, trimming, metadata/thumbnail preview, and progress logging.

- Language/stack: C#, .NET 10.0, Avalonia UI 11.3.7 (MVVM via CommunityToolkit.Mvvm), Semi.Avalonia theme, Microsoft.Extensions.DependencyInjection/Logging. Bundles yt-dlp and requires ffmpeg for merging video+audio streams.
- Layout: `Views`/`ViewModels` (MVVM UI), `Services` (core logic), `Models`, `Assets`, `tests/` (excluded from the main build via `realsnag-media-downloader.csproj`).
- Build & run: `dotnet restore` then `dotnet run`.
- Publish: `dotnet publish -c Release -r <rid> --self-contained true -o ./publish/<rid>` for `osx-arm64`, `win-x64`, `linux-x64`, etc.
- CI/release: `.github/workflows/release.yml`.
- License: MIT (see `LICENSE`); project is for educational purposes.

## Cross-agent conventions

- This file (`AGENTS.md`) is the single source of truth for agent instructions in this repo. `CLAUDE.md` and `.github/copilot-instructions.md` are pointers to it — never edit them, never duplicate content into them.
- Reusable skills live in `.claude/skills/` (one folder per skill with a `SKILL.md`). GitHub Copilot reads that directory natively; Codex sees it via the `.agents/skills` symlink. New skills always go in `.claude/skills/`.
- Claude-specific subagent definitions live in `.claude/agents/`. If you are not Claude Code, you may read them as role/process guidance.
- Session continuity across tools: before ending substantial work in ANY tool (Claude Code, Codex, Copilot), record durable context — decisions made, gotchas discovered, in-progress state worth resuming — in the "Working notes" section below, or fold it into the relevant section above. This is the shared memory between agents.

## Working notes

<!-- Any agent: append short dated notes here (YYYY-MM-DD — note). Prune notes when stale or once folded into the sections above. -->
