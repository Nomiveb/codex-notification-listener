using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Text;
using System.Runtime.InteropServices;
using Windows.ApplicationModel;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace CodexNotificationListener;

public sealed partial class MainWindow : Window
{
    private const string RelativeSoundPath = @"Sounds\codex.wav";
    private const string LogPath = @"C:\AHK\codex_notification_listener.log";
    private const string CodexDisplayName = "Codex";
    private const string CodexPackageName = "OpenAI.Codex";
    private const string CodexAumid = "OpenAI.Codex_2p2nqsd0c76g0!App";
    private readonly UserNotificationListener _listener = UserNotificationListener.Current;
    private readonly HashSet<string> _seenNotifications = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _scanLock = new(1, 1);
    private readonly StringBuilder _recentLog = new();
    private readonly string _soundPath = ResolveSoundPath();
    private bool _baselineComplete;

    public MainWindow()
    {
        InitializeComponent();
        SetWindowIcon();
        SetStatus("Starting listener...");
        _ = StartAsync();
    }

    private async Task StartAsync()
    {
        try
        {
            Log("App started.");
            SetDetails($"Sound: {_soundPath}\nLog: {LogPath}");

            UserNotificationListenerAccessStatus accessStatus = await _listener.RequestAccessAsync();
            Log($"Notification access status: {accessStatus}");

            if (accessStatus != UserNotificationListenerAccessStatus.Allowed)
            {
                SetStatus("Notification access is not allowed.");
                SetDetails("Open Settings > Privacy & security > App permissions > Notifications > user notification access, then allow this app and restart it.");
                return;
            }

            SetStatus("Permission granted. Reading existing notifications...");
            await ScanNotificationsAsync(NotificationScanReason.Baseline);
            _baselineComplete = true;

            _listener.NotificationChanged += OnNotificationChanged;
            SetStatus("Listening. Old notifications were marked as seen. New Codex notifications will play the WAV once.");
            Log("Listener subscribed. Baseline is complete.");
        }
        catch (Exception ex)
        {
            LogError("Startup failed.", ex);
            SetStatus("Startup failed. Check the log file.");
        }
    }

    private void OnNotificationChanged(UserNotificationListener sender, UserNotificationChangedEventArgs args)
    {
        DispatcherQueue.TryEnqueue(async () =>
        {
            Log($"NotificationChanged event: ChangeKind={args.ChangeKind}, UserNotificationId={args.UserNotificationId}");
            await ScanNotificationsAsync(NotificationScanReason.ChangeEvent);
        });
    }

    private async Task ScanNotificationsAsync(NotificationScanReason reason)
    {
        await _scanLock.WaitAsync();
        try
        {
            IReadOnlyList<UserNotification> notifications = await _listener.GetNotificationsAsync(NotificationKinds.Toast);
            Log($"Scan started. Reason={reason}; Count={notifications.Count}; BaselineComplete={_baselineComplete}");

            foreach (UserNotification notification in notifications.OrderBy(n => n.CreationTime))
            {
                NotificationIdentity identity = NotificationIdentity.From(notification);
                string key = identity.DeduplicationKey;
                bool isCodex = identity.IsCodex();

                if (_seenNotifications.Contains(key))
                {
                    Log($"Seen duplicate skipped. {identity.ToLogString()}");
                    continue;
                }

                _seenNotifications.Add(key);

                if (reason == NotificationScanReason.Baseline || !_baselineComplete)
                {
                    Log($"Baseline seen without sound. IsCodex={isCodex}. {identity.ToLogString()}");
                    continue;
                }

                if (!isCodex)
                {
                    Log($"Non-Codex notification skipped. {identity.ToLogString()}");
                    continue;
                }

                bool played = NativeAudio.PlayWav(_soundPath);
                Log($"Codex notification detected. Played={played}. {identity.ToLogString()}");
                SetStatus(played
                    ? "New Codex notification detected. Sound played."
                    : "New Codex notification detected, but the WAV could not be played. Check the path.");
            }
        }
        catch (Exception ex)
        {
            LogError($"Scan failed. Reason={reason}", ex);
            SetStatus("Notification scan failed. Check the log file.");
        }
        finally
        {
            _scanLock.Release();
        }
    }

    private void SetStatus(string text)
    {
        StatusText.Text = text;
    }

    private void SetDetails(string text)
    {
        DetailsText.Text = text;
    }

    private void Log(string message)
    {
        string line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} | {message}";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.AppendAllText(LogPath, line + Environment.NewLine, Encoding.UTF8);
        }
        catch
        {
            // Logging must never crash the listener.
        }

        DispatcherQueue.TryEnqueue(() =>
        {
            _recentLog.Insert(0, line + Environment.NewLine);
            if (_recentLog.Length > 8000)
            {
                _recentLog.Remove(8000, _recentLog.Length - 8000);
            }

            RecentLogText.Text = _recentLog.ToString();
        });
    }

    private void LogError(string message, Exception ex)
    {
        Log($"{message} Error={ex.GetType().Name}: {ex.Message}");
    }

    private void SetWindowIcon()
    {
        string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        if (!File.Exists(iconPath))
        {
            return;
        }

        IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
        AppWindow appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.SetIcon(iconPath);
    }

    private static string ResolveSoundPath()
    {
        string? current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            string candidate = Path.Combine(current, RelativeSoundPath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            if (File.Exists(Path.Combine(current, "CodexNotificationListener.sln")))
            {
                return candidate;
            }

            current = Directory.GetParent(current)?.FullName;
        }

        return Path.Combine(AppContext.BaseDirectory, RelativeSoundPath);
    }

    private enum NotificationScanReason
    {
        Baseline,
        ChangeEvent
    }

    private sealed record NotificationIdentity(
        uint Id,
        DateTimeOffset CreationTime,
        string DisplayName,
        string AppUserModelId,
        string PackageName,
        string PackageFamilyName,
        string PackageFullName)
    {
        public string DeduplicationKey =>
            string.Join("|", AppUserModelId, PackageFamilyName, PackageName, Id, CreationTime.ToUnixTimeMilliseconds());

        public static NotificationIdentity From(UserNotification notification)
        {
            AppInfo? appInfo = notification.AppInfo;
            string displayName = SafeRead(() => appInfo?.DisplayInfo.DisplayName) ?? "";
            string appUserModelId = SafeRead(() => appInfo?.AppUserModelId) ?? "";
            string packageFamilyName = SafeRead(() => appInfo?.PackageFamilyName) ?? "";
            string packageName = SafeRead(() => appInfo?.Package?.Id.Name) ?? "";
            string packageFullName = SafeRead(() => appInfo?.Package?.Id.FullName) ?? "";

            return new NotificationIdentity(
                notification.Id,
                notification.CreationTime,
                displayName,
                appUserModelId,
                packageName,
                packageFamilyName,
                packageFullName);
        }

        public bool IsCodex()
        {
            return EqualsAny(DisplayName, CodexDisplayName)
                || ContainsAny(PackageName, CodexPackageName)
                || ContainsAny(PackageFamilyName, CodexPackageName)
                || ContainsAny(PackageFullName, CodexPackageName)
                || EqualsAny(AppUserModelId, CodexAumid)
                || ContainsAny(AppUserModelId, "OpenAI.Codex");
        }

        public string ToLogString()
        {
            return $"Id={Id}; Created={CreationTime:O}; DisplayName='{DisplayName}'; AUMID='{AppUserModelId}'; PackageName='{PackageName}'; PackageFamilyName='{PackageFamilyName}'; PackageFullName='{PackageFullName}'";
        }

        private static bool EqualsAny(string value, string expected)
        {
            return string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ContainsAny(string value, string expected)
        {
            return value.Contains(expected, StringComparison.OrdinalIgnoreCase);
        }

        private static string? SafeRead(Func<string?> read)
        {
            try
            {
                return read();
            }
            catch
            {
                return null;
            }
        }
    }

    private static class NativeAudio
    {
        private const uint SndAsync = 0x0001;
        private const uint SndFilename = 0x00020000;
        private const uint SndNodefault = 0x0002;

        public static bool PlayWav(string path)
        {
            if (!File.Exists(path))
            {
                return false;
            }

            return PlaySound(path, IntPtr.Zero, SndAsync | SndFilename | SndNodefault);
        }

        [DllImport("winmm.dll", EntryPoint = "PlaySoundW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PlaySound(string pszSound, IntPtr hmod, uint fdwSound);
    }
}
