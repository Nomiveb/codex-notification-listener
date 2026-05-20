# Agents.md global file

- Don't forget to use Plugins, MCP servers, tools, and connectors when they fit the task, for example GitHub, Vercel, or Neon.
- Explain everything in simple words, like the user builds products but does not know code.
- Don't fight errors. Every time the same error appears twice, research it online, find 3-5 possible fixes, choose the strongest one, and implement it.
- Always update `AGENTS.md` and `README.md` after code changes work, if the user asked to make a commit, push, publish a release, pull changes on a server, or do similar release/workflow actions.

## Project Notes

- This repo contains a minimal packaged WinUI / Windows App SDK app.
- The app listens to Windows toast notifications while it is running.
- It plays `Sounds\codex.wav` only for new Codex Desktop notifications, not for old notifications already present when the app starts.
- The app writes its decisions and errors to `C:\AHK\codex_notification_listener.log`.
- The app uses `CodexNotificationListener\Assets\AppIcon.ico` for the taskbar/window icon.
- v1 intentionally has no tray icon and no background wake-up task.
- Normal users should download the portable GitHub Release asset, extract it, and run `CodexNotificationListener.exe`.
- The release archive should be named like `CodexNotificationListener-v1.0.0-windows-x64-portable.zip`.
- The release archive should contain the portable app files, including `CodexNotificationListener.exe`, `Assets\`, and `Sounds\codex.wav`.
- Do not ship a PS1 installer as the main user path. A release archive with a directly runnable `.exe` is the intended user-facing format.
- Generated package zips should be published through GitHub Releases, not committed to the repository.
