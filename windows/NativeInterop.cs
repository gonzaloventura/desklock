using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DeskLock;

public static class NativeInterop
{
    // --- Hook types ---
    private const int WH_KEYBOARD_LL = 13;
    private const int WH_MOUSE_LL = 14;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;

    // --- Virtual key codes ---
    public const int VK_CONTROL = 0x11;
    public const int VK_SHIFT = 0x10;
    public const int VK_MENU = 0x12; // Alt
    public const int VK_LWIN = 0x5B;
    public const int VK_RWIN = 0x5C;

    private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    // --- State ---
    private static IntPtr _keyboardHook = IntPtr.Zero;
    private static IntPtr _mouseHook = IntPtr.Zero;
    private static LowLevelProc? _keyboardProc;
    private static LowLevelProc? _mouseProc;

    public static bool IsLocked { get; set; }

    // Hotkey config — read from AppSettings
    public static int HotkeyVirtualKey { get; set; } = 0xC0; // VK_OEM_3 (backtick/tilde)
    public static bool HotkeyCtrl { get; set; } = true;
    public static bool HotkeyShift { get; set; } = true;
    public static bool HotkeyAlt { get; set; }
    public static bool HotkeyWin { get; set; }

    public static event Action? OnToggleLock;

    // --- Install / Uninstall ---

    public static void InstallHooks()
    {
        _keyboardProc = KeyboardHookCallback;
        _mouseProc = MouseHookCallback;

        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule!;
        var hModule = GetModuleHandle(module.ModuleName);

        _keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, hModule, 0);
        _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, hModule, 0);
    }

    public static void UninstallHooks()
    {
        if (_keyboardHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = IntPtr.Zero;
        }
        if (_mouseHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHook);
            _mouseHook = IntPtr.Zero;
        }
    }

    // --- Callbacks ---

    private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
            {
                var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                if (IsHotkeyCombination(hookStruct.vkCode))
                {
                    System.Windows.Application.Current?.Dispatcher.BeginInvoke(() => OnToggleLock?.Invoke());
                    return (IntPtr)1; // block the hotkey itself
                }
            }

            if (IsLocked)
            {
                return (IntPtr)1; // block everything when locked
            }
        }

        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && IsLocked)
        {
            return (IntPtr)1; // block all mouse input when locked
        }

        return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private static bool IsHotkeyCombination(uint vkCode)
    {
        if (vkCode != (uint)HotkeyVirtualKey)
            return false;

        bool ctrl = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;
        bool shift = (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;
        bool alt = (GetAsyncKeyState(VK_MENU) & 0x8000) != 0;
        bool win = (GetAsyncKeyState(VK_LWIN) & 0x8000) != 0 || (GetAsyncKeyState(VK_RWIN) & 0x8000) != 0;

        return ctrl == HotkeyCtrl
            && shift == HotkeyShift
            && alt == HotkeyAlt
            && win == HotkeyWin;
    }

    // --- Display helpers ---

    public static string GetKeyName(int virtualKey)
    {
        return virtualKey switch
        {
            >= 0x30 and <= 0x39 => ((char)virtualKey).ToString(), // 0-9
            >= 0x41 and <= 0x5A => ((char)virtualKey).ToString(), // A-Z
            0xC0 => "|",   // OEM_3 — backtick/tilde (pipe on ES keyboards)
            0xBD => "-",
            0xBB => "=",
            0xDB => "[",
            0xDD => "]",
            0xDC => "\\",
            0xBA => ";",
            0xDE => "'",
            0xBC => ",",
            0xBE => ".",
            0xBF => "/",
            0x20 => "Space",
            0x09 => "Tab",
            0x0D => "Enter",
            0x08 => "Backspace",
            0x2E => "Del",
            0x1B => "Esc",
            0x70 => "F1", 0x71 => "F2", 0x72 => "F3", 0x73 => "F4",
            0x74 => "F5", 0x75 => "F6", 0x76 => "F7", 0x77 => "F8",
            0x78 => "F9", 0x79 => "F10", 0x7A => "F11", 0x7B => "F12",
            _ => $"Key(0x{virtualKey:X2})"
        };
    }

    public static string GetHotkeyDisplayString()
    {
        var parts = new System.Collections.Generic.List<string>();
        if (HotkeyCtrl)  parts.Add("Ctrl");
        if (HotkeyAlt)   parts.Add("Alt");
        if (HotkeyShift) parts.Add("Shift");
        if (HotkeyWin)   parts.Add("Win");
        parts.Add(GetKeyName(HotkeyVirtualKey));
        return string.Join("+", parts);
    }
}
