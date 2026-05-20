# Agents.md global file

- Don't forget to use Plugins, mcp servers, tools etc. for example, github plugin, vercel plugin, neon database plugin.
- Explain everything in simple words, just like I am not a coder but just a man that builds products without knowing code.
- Don’t fight errors. Every time you encounter the same error twice, research it online and find 3–5 possible ways to fix it. Then choose the most effective solution and implement it.
- Always update `AGENTS.md` and `README.md` after something was changed in code and it all works, if the user asked to make commit, pull changes on server, or similar release/workflow actions.

## Project Notes

- This repo contains a minimal packaged WinUI / Windows App SDK app.
- The app listens to Windows toast notifications while it is running.
- It plays `Sounds\codex.wav` only for new Codex Desktop notifications, not for old notifications already present when the app starts.
- The app writes its decisions and errors to `C:\AHK\codex_notification_listener.log`.
- The app uses `CodexNotificationListener\Assets\AppIcon.ico` for the taskbar/window icon.
- v1 intentionally has no tray icon and no background wake-up task.
- Generated package zips should be published through GitHub Releases, not committed to the repository.
