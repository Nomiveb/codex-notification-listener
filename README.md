# Codex Toast Sound Listener

Small Windows app that listens for Codex Desktop toast notifications and plays:

```text
Sounds\codex.wav
```

Put your custom WAV file at:

```text
Sounds\codex.wav
```

It writes a plain log here:

```text
C:\AHK\codex_notification_listener.log
```

For normal use, download the release archive, extract it, and run `CodexNotificationListener.exe`.

## Download The App

For normal users, do not download **Source code (zip)** or **Source code (tar.gz)**.

Download the portable app from **GitHub Releases** instead:

1. Open the latest release.
2. Download `CodexNotificationListener-v1.0.0-windows-x64-portable.zip`.
3. Extract the zip.
4. Open the extracted folder.
5. Run `CodexNotificationListener.exe`.

You can move the extracted folder anywhere, for example Desktop, Downloads, or `C:\Tools\CodexNotificationListener`.

The source code zip is only for developers who want to inspect or change the code.

The release archive is the user build. It is not the same as GitHub's automatic **Source code (zip)** download.

## Change The Sound

In the extracted app folder, replace this file with your own WAV file:

```text
Sounds\codex.wav
```

The file must stay named `codex.wav`.

## What It Does

- Starts a minimal WinUI status window.
- Shows a custom app icon in Windows and the taskbar.
- Asks Windows for notification listener permission.
- Registers as a full-trust packaged desktop app, because WinUI desktop apps need `runFullTrust` in the manifest.
- Reads all current toast notifications once on startup and marks them as already seen.
- Does not play sound for old notifications that already existed before the app started.
- Watches for notification changes.
- Checks each toast's app display name, package name, package family name, AUMID, and AppInfo fields.
- Plays the WAV once when a new Codex Desktop notification appears.
- Skips duplicates and non-Codex notifications.

Known Codex identity used by v1:

```text
Display name: Codex
Package: OpenAI.Codex
AUMID: OpenAI.Codex_2p2nqsd0c76g0!App
```

## Requirements For Users

- Windows 11.
- `Sounds\codex.wav` must exist.

## Requirements For Developers

- Windows 11.
- .NET SDK.
- Visual Studio 2022 with Windows App SDK / WinUI development tools is the easiest route if you want to build or modify the app.

The project is manually scaffolded because this machine did not have the WinUI `dotnet new` template installed.

## Build From Source

From PowerShell:

```powershell
dotnet build .\CodexNotificationListener.sln -c Debug -p:Platform=x64
```

This command was tested in this folder and builds successfully.

You can also use Visual Studio:

Open this solution in Visual Studio:

```text
CodexNotificationListener.sln
```

Then choose:

```text
Build > Build Solution
```

Use `x64`.

The project includes `<EnableMsixTooling>true</EnableMsixTooling>` because Windows App SDK packaging can fail without it on CLI builds.

## Build The Release Archive

From PowerShell:

```powershell
dotnet publish .\CodexNotificationListener\CodexNotificationListener.csproj -c Release -p:Platform=x64 -p:WindowsPackageType=None -p:AppxPackage=false -p:WindowsAppSDKSelfContained=true -p:SelfContained=true -p:PublishSingleFile=false -o .\dist\CodexNotificationListener-portable
Compress-Archive -Path .\dist\CodexNotificationListener-portable\* -DestinationPath .\CodexNotificationListener-v1.0.0-windows-x64-portable.zip -Force
```

This creates a normal app folder with `CodexNotificationListener.exe` inside it, then zips that folder for GitHub Releases.

For GitHub, attach `CodexNotificationListener-v1.0.0-windows-x64-portable.zip` to a GitHub Release. Do not commit generated package zips into the repository.

## Run

Run the app from the portable publish folder:

```text
dist\CodexNotificationListener-portable\CodexNotificationListener.exe
```

The first run should show a Windows permission prompt.

## Windows Permission Setup

If Windows blocks access, open:

```text
Settings > Privacy & security > App permissions > Notifications > user notification access
```

Allow `Codex Notification Listener`, then restart the app.

If access is denied, the app will keep running but will show instructions and log the denial.

## Test Plan

1. Start the app while old Codex notifications are already visible.
2. Confirm the log says those notifications were marked as baseline/seen.
3. Confirm no sound plays during startup.
4. Trigger a new Codex Desktop notification.
5. Confirm `Sounds\codex.wav` plays once.
6. Watch the log and confirm duplicate listener events are skipped.
7. Trigger a non-Codex notification and confirm it is logged but skipped.
8. Deny notification access and confirm the app logs the denial and shows permission instructions.

## Scope

v1 listens only while the app window is running.

A true background wake-up task is possible later, but it is intentionally out of scope for this minimal first version.

App icons live in:

```text
CodexNotificationListener\Assets
```

The taskbar icon is set from:

```text
CodexNotificationListener\Assets\AppIcon.ico
```

## Microsoft References

- [Notification Listener](https://learn.microsoft.com/en-us/windows/apps/design/shell/tiles-and-notifications/notification-listener)
- [WinUI setup](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/create-your-first-winui3-app?pivots=winui3-packaged-csharp)
- [AppInfo](https://learn.microsoft.com/en-us/uwp/api/windows.applicationmodel.appinfo)
