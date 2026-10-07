# Notify

Local Windows companion for capturing Firefox pages and screen regions into Obsidian.

## Build and run

Install the .NET 8 SDK and Python 3.11 or 3.12. From the repository root:

```powershell
dotnet publish .\src\Notify.Desktop\Notify.Desktop.csproj -c Release -r win-x64 --self-contained true
& .\src\Notify.Desktop\bin\Release\net8.0-windows\win-x64\publish\Notify.Desktop.exe
```

The app defaults to `C:\Users\<you>\iCloudDrive\iCloud~md~obsidian\Obsidian Vault\3. Rough Notes`; change it in the folder field to save that choice locally.

## Offline OCR setup

While online, install Pix2Text and download its models once. Run from the repository root:

```powershell
python -m pip install pix2text
python .\src\setup_ocr.py
```

The warm-up downloads model assets; after it finishes, OCR runs locally without network access. Region selection copies the image directly to the clipboard and passes PNG bytes in memory to the worker; Notify does not save screenshots or Pix2Text debug images. Python and the Pix2Text models must remain installed on this machine.

## Firefox page capture

1. Open `about:debugging#/runtime/this-firefox`, choose **Load Temporary Add-on**, and select `src/firefox-extension/manifest.json`.
2. Publish the app as above. Create `%LOCALAPPDATA%\Notify\native-host.json` with this content, replacing `<absolute-repo-path>` with the repository path:

   ```json
   {"name":"com.notify.desktop","description":"Notify local capture","path":"<absolute-repo-path>\\src\\Notify.Desktop\\bin\\Release\\net8.0-windows\\win-x64\\publish\\Notify.Desktop.exe","type":"stdio","allowed_extensions":["notify-capture@local"]}
   ```

3. Register it in PowerShell:

   ```powershell
   New-Item -Force HKCU:\Software\Mozilla\NativeMessagingHosts\com.notify.desktop | Out-Null
   Set-ItemProperty HKCU:\Software\Mozilla\NativeMessagingHosts\com.notify.desktop '(default)' "$env:LOCALAPPDATA\Notify\native-host.json"
   ```

Click the extension toolbar button on a page. Notify writes a timestamped Markdown file with the required frontmatter and source link. Existing notes are kept; name collisions get a numbered suffix.

## Troubleshooting

- **OCR says Python/Pix2Text is missing:** confirm `python` is on `PATH`, then rerun the offline OCR setup above.
- **Model download fails:** run `python .\src\setup_ocr.py` while online; first-use setup requires access to the model host.
- **Firefox reports native host unavailable:** verify the registry entry points to the JSON manifest and that its executable path and extension ID match this README.
- **Capture cannot save:** choose an existing writable folder in the app and retry.
