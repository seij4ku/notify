# Notify

Windows companion for Firefox page capture, OpenAI screen OCR, and Obsidian research packs.

## Build

Install the .NET 8 SDK, then publish from the repository root:

```powershell
dotnet publish .\src\Notify.Desktop\Notify.Desktop.csproj -c Release -r win-x64 --self-contained true
```

The executable is written under `src\Notify.Desktop\bin\Release\net8.0-windows\win-x64\publish`. Build output is ignored by Git.

## Firefox capture

1. In Firefox, open `about:debugging#/runtime/this-firefox`, choose **Load Temporary Add-on**, and select `src/firefox-extension/manifest.json`.
2. In Notify, select **Firefox capture**, then **Link desktop app to Firefox**.
3. Click the Notify toolbar button on a web page.

The extension injects a small script into the active tab. It captures the title, URL, and the inner HTML and visible text from the first `article` or `main` element, falling back to the page body. It sends those fields over Firefox Native Messaging to the Notify executable. Notify removes common page chrome and converts the remaining HTML to Markdown, then saves a timestamped note in the configured processed-notes folder. There is no localhost server. Reload the temporary extension after Firefox restarts.

## Screen OCR

Set an OpenAI API key under **Settings**, select **Screen OCR**, then select a screen region (or use the configured global hotkey). Notify copies the crop to the clipboard and sends its PNG bytes to OpenAI for transcription and basic cleanup. No screenshot file is created. The API key is encrypted for the current Windows account. Screenshot image data is sent to OpenAI; research-pack processing stays local.

## Research packs

Configure the Obsidian vault and research output directory in **Settings**. Search by text and tag-template filenames. Each selected tag template’s Markdown table constrains the matching vault notes; multiple tags intersect. Add and remove filtered notes from the pack selection, then create the pack. Notify compiles those notes into a Markdown file in a new folder and leaves source notes unchanged.

## Settings

The settings window configures the Firefox processed-notes location, vault, research-pack folder, tag-template directory, screenshot hotkey, and OpenAI API key. To record the hotkey, click its field and press any key, with or without modifiers. A bare key is global and will intercept that key in other apps while Notify is running.

## Troubleshooting

- **OpenAI OCR fails:** verify the API key in Settings and check the error shown in the OCR view.
- **Firefox reports native host unavailable:** use the in-app link action and verify the temporary extension is loaded with the configured extension ID.
- **Capture cannot save:** choose a writable notes folder in Settings.
