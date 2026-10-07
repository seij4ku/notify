# Notify

A Windows-first Obsidian capture and study app.

## Project layout

- `src/Notify.Desktop/` — WPF desktop app, including the Firefox Native Messaging host.
- `src/firefox-extension/` — Firefox page-capture extension.
- `docs/architecture/` — architecture notes and decisions.

Page captures are intended to save Markdown into the configured Obsidian folder using the rough-notes template. Region captures are intended to copy the image to the clipboard and support OCR-to-Markdown.
