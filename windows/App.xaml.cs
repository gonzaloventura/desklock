using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace DeskLock;

public partial class App : Application
{
    private Mutex? _singleInstanceMutex;
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private AppSettings _settings = null!;
    private LockOverlay? _overlay;
    private SettingsWindow? _settingsWindow;
    private bool _isLocked;
    private DispatcherTimer? _focusTimer;

    private static readonly string LogFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DeskLock", "error.log");

    private static void Log(string msg)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogFile)!);
            File.AppendAllText(LogFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}\n");
        }
        catch { }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            base.OnStartup(e);
            Log("Starting DeskLock...");

            DispatcherUnhandledException += (_, args) =>
            {
                Log($"Unhandled: {args.Exception}");
                MessageBox.Show($"DeskLock error:\n{args.Exception.Message}",
                    "DeskLock Error", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                Log($"Fatal: {ex}");
                MessageBox.Show($"DeskLock fatal error:\n{ex?.Message}",
                    "DeskLock Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            _singleInstanceMutex = new Mutex(true, "DeskLock_SingleInstance", out bool isNew);
            if (!isNew)
            {
                Log("Already running, exiting.");
                MessageBox.Show("DeskLock is already running.", "DeskLock",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            Log("Loading settings...");
            _settings = AppSettings.Load();
            _settings.ApplyToNativeInterop();

            Log("Setting up tray icon...");
            SetupTrayIcon();

            Log("Installing hooks...");
            NativeInterop.OnToggleLock += ToggleLock;
            NativeInterop.InstallHooks();

            Log("DeskLock started successfully.");
        }
        catch (Exception ex)
        {
            Log($"Startup crash: {ex}");
            MessageBox.Show($"DeskLock failed to start:\n\n{ex.Message}\n\n{ex.StackTrace}",
                "DeskLock Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        NativeInterop.UninstallHooks();
        _trayIcon?.Dispose();
        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    // --- Tray Icon ---

    private void SetupTrayIcon()
    {
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = CreateLockIcon(),
            Text = "DeskLock",
            Visible = true,
            ContextMenuStrip = BuildTrayMenu()
        };
    }

    private System.Windows.Forms.ContextMenuStrip BuildTrayMenu()
    {
        var menu = new System.Windows.Forms.ContextMenuStrip();

        var hotkeyStr = NativeInterop.GetHotkeyDisplayString();
        var lockLabel = _isLocked
            ? $"Unlock Desk  ({hotkeyStr})"
            : $"Lock Desk  ({hotkeyStr})";
        menu.Items.Add(lockLabel, null, (_, _) => ToggleLock());
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("Settings...", null, (_, _) => OpenSettings());
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("Quit DeskLock", null, (_, _) => QuitApp());

        return menu;
    }

    private void RefreshTrayMenu()
    {
        if (_trayIcon == null) return;
        _trayIcon.ContextMenuStrip?.Dispose();
        _trayIcon.ContextMenuStrip = BuildTrayMenu();
    }

    private static Icon CreateLockIcon()
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        using var pen = new Pen(Color.White, 1.5f);
        g.DrawArc(pen, 4, 0, 8, 8, 180, 180);

        using var brush = new SolidBrush(Color.White);
        g.FillRectangle(brush, 2, 7, 12, 8);

        return Icon.FromHandle(bmp.GetHicon());
    }

    // --- Lock / Unlock ---

    private void ToggleLock()
    {
        if (_isLocked)
            Unlock();
        else
            Lock();
    }

    private void Lock()
    {
        if (_isLocked) return;
        _isLocked = true;
        NativeInterop.IsLocked = true;

        _settings = AppSettings.Load();
        _settings.ApplyToNativeInterop();

        _overlay ??= new LockOverlay();
        _overlay.ApplySettings(_settings);
        _overlay.ShowOnPrimaryScreen();

        // Timer to keep overlay on top and focused
        _focusTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _focusTimer.Tick += (_, _) =>
        {
            if (!_isLocked) return;
            _overlay?.Activate();
            _overlay?.Focus();
        };
        _focusTimer.Start();

        RefreshTrayMenu();
    }

    private void Unlock()
    {
        if (!_isLocked) return;
        _isLocked = false;
        NativeInterop.IsLocked = false;

        _focusTimer?.Stop();
        _focusTimer = null;

        _overlay?.DismissOverlay();
        RefreshTrayMenu();
    }

    // --- Settings ---

    private void OpenSettings()
    {
        if (_isLocked) return;

        if (_settingsWindow == null || !_settingsWindow.IsLoaded)
        {
            _settingsWindow = new SettingsWindow(_settings);
            _settingsWindow.Closed += (_, _) =>
            {
                _settings = AppSettings.Load();
                _settings.ApplyToNativeInterop();
                RefreshTrayMenu();
            };
        }

        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    // --- Quit ---

    private void QuitApp()
    {
        if (_isLocked) Unlock();
        Shutdown();
    }
}
