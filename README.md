# Notify

Windows companion for Firefox page capture, Claude Code screen OCR, and Obsidian research packs.

## Build

Install the .NET 8 SDK, then publish from the repository root:

```powershell
dotnet publish .\src\Notify.Desktop\Notify.Desktop.csproj -c Release -r win-x64 --self-contained true -o .\bin\Notify
```

The current executable is `bin\Notify\Notify.Desktop.exe`. Build output is ignored by Git.

## Firefox capture

For a persistent private install, submit `dist/notify-firefox.zip` to [AMO](https://addons.mozilla.org/developers/) and choose **On your own** for self-distribution. Download the signed `.xpi` AMO returns, then install it in Firefox from `about:addons` using **Install Add-on From File**. Firefox will keep it across restarts. Mozilla signing requires an AMO developer account and review; submit each updated package for signing.

For development, load the extension temporarily from `about:debugging#/runtime/this-firefox` using **Load Temporary Add-on** and select `src/firefox-extension/manifest.json`. Temporary installs need to be loaded again after Firefox restarts.

1. In Notify, select **Firefox capture**, then **Link desktop app to Firefox**.
2. Click the Notify toolbar button, choose **Main content**, **Selected text**, or **Entire page**, then click **Send to Notify**.

The extension sends the captured page through Firefox Native Messaging. Notify opens the desktop capture view, runs Claude Code Haiku, and displays editable Markdown there. Select **Save processed note** to write it to the Firefox processed-notes folder configured in Settings; Notify shows the full saved path when it succeeds. Firefox starts a temporary native host which forwards captures to the desktop app.

## Screen OCR

Run `claude` in a terminal once and sign in with your Claude subscription. Claude Code saves its login for later launches, so Notify does not ask you to sign in each time. Select **Screen OCR**, then select a screen region (or use the configured global hotkey). Notify sets Claude Code to Haiku for every call, writes the crop to a temporary PNG, asks the local CLI to transcribe it into Obsidian-compatible Markdown, then deletes the PNG. The global shortcut reopens Notify as soon as the region is selected and copies the finished OCR text to the clipboard. Screenshot image data is processed by Claude; research-pack processing stays local. No OpenAI API key is needed.

## Research packs

Configure the Obsidian vault and research output directory in **Settings**. Search by text and tag-template filenames. Literal tables in a tag template constrain the matching vault notes. Templates embedding an Obsidian Base with `list(tag).contains(this)` filter notes by the template’s `tag` frontmatter; multiple tags intersect. Add and remove filtered notes from the pack selection, then create the pack. Notify compiles those notes into a Markdown file in a new folder and leaves source notes unchanged.

## Settings

The settings window configures the Firefox processed-notes location, vault, research-pack folder, tag-template directory, and screenshot hotkey. To record the hotkey, click its field and press any key, with or without modifiers. A bare key is global and will intercept that key in other apps while Notify is running.

## Troubleshooting

- **Claude Code OCR fails:** install Claude Code CLI, run `claude` once to sign in, then check the error shown in the OCR view.
- **Firefox reports native host unavailable:** use the in-app link action and verify the temporary extension is loaded with the configured extension ID.
- **Capture cannot save:** choose a writable notes folder in Settings.
