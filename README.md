# Notify

Windows companion for Firefox page capture, Claude Code screen OCR, and Obsidian research packs.

## Build

Install the .NET 8 SDK, then publish from the repository root:

```powershell
dotnet publish .\src\Notify.Desktop\Notify.Desktop.csproj -c Release -r win-x64 --self-contained true -o .\bin\Notify
```

The current executable is `bin\Notify\Notify.Desktop.exe`. Build output is ignored by Git.

## Firefox capture

1. In Firefox, open `about:debugging#/runtime/this-firefox`, choose **Load Temporary Add-on**, and select `src/firefox-extension/manifest.json`.
2. In Notify, select **Firefox capture**, then **Link desktop app to Firefox**.
3. Click the Notify toolbar button, choose **Main content**, **Selected text**, or **Entire page**, then click **Process with Notify**.

The extension captures the selected range and sends its HTML and visible text through Firefox Native Messaging. Notify runs the page through Claude Code Haiku, converts it to Obsidian-friendly Markdown, saves the note in the configured processed-notes folder, then shows a success confirmation. Firefox starts Notify with stdin/stdout pipes and no special command-line argument; Notify detects that launch mode. Reload the temporary extension after Firefox restarts.

## Screen OCR

Run `claude` in a terminal once and sign in with your Claude subscription. Claude Code saves its login for later launches, so Notify does not ask you to sign in each time. Select **Screen OCR**, then select a screen region (or use the configured global hotkey). Notify sets Claude Code to Haiku for every call, writes the crop to a temporary PNG, asks the local CLI to transcribe it into Obsidian-compatible Markdown, then deletes the PNG. Screenshot image data is processed by Claude; research-pack processing stays local. No OpenAI API key is needed.

## Research packs

Configure the Obsidian vault and research output directory in **Settings**. Search by text and tag-template filenames. Literal tables in a tag template constrain the matching vault notes. Templates embedding an Obsidian Base with `list(tag).contains(this)` filter notes by the template’s `tag` frontmatter; multiple tags intersect. Add and remove filtered notes from the pack selection, then create the pack. Notify compiles those notes into a Markdown file in a new folder and leaves source notes unchanged.

## Settings

The settings window configures the Firefox processed-notes location, vault, research-pack folder, tag-template directory, and screenshot hotkey. To record the hotkey, click its field and press any key, with or without modifiers. A bare key is global and will intercept that key in other apps while Notify is running.

## Troubleshooting

- **Claude Code OCR fails:** install Claude Code CLI, run `claude` once to sign in, then check the error shown in the OCR view.
- **Firefox reports native host unavailable:** use the in-app link action and verify the temporary extension is loaded with the configured extension ID.
- **Capture cannot save:** choose a writable notes folder in Settings.
