# Windows port

Tinycast on Windows is a tray command palette. It is a separate app under `windows/`, not a
recompile of the macOS Swift sources — AppKit, Accessibility, JavaScriptCore and Liquid Glass
have no equivalent on this OS.

## What it is

- **Summon** with `Alt+Space` (rebind in Settings). Lives in the notification area, no taskbar button.
- **Surface.** Windows 11 **Acrylic** (`DWMSBT_TRANSIENTWINDOW`) plus Avalonia's acrylic blur stand in
  for Liquid Glass. The palette is still a 750×475 rounded panel over a 40% black scrim.
- **AI.** Only the **Mistral API**. Settings → AI stores a key with DPAPI. Chat Completions at
  `https://api.mistral.ai/v1`. Quick Actions (Fix Grammar / Rewrite / Summarize / Translate) use the
  same key. Apple Intelligence, Codex, Claude CLI, OpenAI, Anthropic, Gemini and OpenRouter are not
  on this port.

### Enable AI

Select **AI Chat** in the palette, or open **☰ → AI · Mistral**. Turn on **Enable AI Chat**, paste a
key from `https://console.mistral.ai`, and choose a model. The key stays encrypted for the current
Windows user through DPAPI.

Quick Actions use the same encrypted key but require their own explicit switch under
**Settings → Quick Actions**. Their model can be chosen independently from AI Chat.

## What works

App launcher (Start Menu `.lnk`), clipboard history, calculator, notes, snippets, quicklinks,
file search (live walk of chosen folders), window snapping, system actions (lock, sleep, restart,
volume, recycle bin, dark mode, Task Manager, …), emoji picker, global hotkey, launch at login.

## What does not (macOS-only)

Raycast extensions, Apple Shortcuts, EventKit calendar, Continuity Camera, Stage Manager, Spaces,
Hyper Key / hidutil, Accessibility menu search, on-device Foundation Models.

## Download

[`windows/dist/TinycastSetup.exe`](dist/TinycastSetup.exe) is the NSIS installer (Windows 10/11,
64-bit). SHA-256: [`TinycastSetup.exe.sha256`](dist/TinycastSetup.exe.sha256).

## Build the installer

From a machine with the .NET 8 SDK (cross-compilation from Linux or macOS is supported):

```sh
./windows/Scripts/build-installer.sh
```

That publishes a self-contained `win-x64` build and, when `makensis` is on `PATH`, writes
`windows/dist/TinycastSetup.exe`.
