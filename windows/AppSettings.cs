using System;
using System.IO;
using System.Text.Json;

namespace DeskLock;

public class AppSettings
{
    public string BackgroundColor { get; set; } = "#0D0D1F";
    public string TextColor { get; set; } = "#FFFFFF";
    public string LockText { get; set; } = "DESK LOCKED";
    public string SubtitleText { get; set; } = "";
    public double FontSize { get; set; } = 72;
    public string? BackgroundImagePath { get; set; }
    public double BackgroundImageOpacity { get; set; } = 0.3;
    public bool ShowClock { get; set; } = true;
    public bool ShowUnlockHint { get; set; } = true;
    public int HotkeyVirtualKey { get; set; } = 0xC0; // VK_OEM_3 (` / | key left of 1)
    public bool HotkeyCtrl { get; set; } = true;
    public bool HotkeyShift { get; set; } = true;
    public bool HotkeyAlt { get; set; }
    public bool HotkeyWin { get; set; }

    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DeskLock");
    private static readonly string SettingsFile = Path.Combine(SettingsDir, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            // If settings are corrupt, return defaults
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }
        catch
        {
            // Silently fail — settings will use defaults next time
        }
    }

    public void ApplyToNativeInterop()
    {
        NativeInterop.HotkeyVirtualKey = HotkeyVirtualKey;
        NativeInterop.HotkeyCtrl = HotkeyCtrl;
        NativeInterop.HotkeyShift = HotkeyShift;
        NativeInterop.HotkeyAlt = HotkeyAlt;
        NativeInterop.HotkeyWin = HotkeyWin;
    }

    public void ResetToDefaults()
    {
        BackgroundColor = "#0D0D1F";
        TextColor = "#FFFFFF";
        LockText = "DESK LOCKED";
        SubtitleText = "";
        FontSize = 72;
        BackgroundImagePath = null;
        BackgroundImageOpacity = 0.3;
        ShowClock = true;
        ShowUnlockHint = true;
        HotkeyVirtualKey = 0xC0;
        HotkeyCtrl = true;
        HotkeyShift = true;
        HotkeyAlt = false;
        HotkeyWin = false;
    }
}
