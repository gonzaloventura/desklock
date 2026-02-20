import Cocoa
import SwiftUI
import Carbon.HIToolbox

extension Notification.Name {
    static let hotkeyChanged = Notification.Name("DeskLockHotkeyChanged")
}

class SettingsManager: ObservableObject {
    static let shared = SettingsManager()

    // MARK: - Appearance

    @Published var backgroundColor: NSColor {
        didSet { saveColor(backgroundColor, forKey: "backgroundColor") }
    }
    @Published var textColor: NSColor {
        didSet { saveColor(textColor, forKey: "textColor") }
    }
    @Published var lockText: String {
        didSet { UserDefaults.standard.set(lockText, forKey: "lockText") }
    }
    @Published var subtitleText: String {
        didSet { UserDefaults.standard.set(subtitleText, forKey: "subtitleText") }
    }
    @Published var fontSize: CGFloat {
        didSet { UserDefaults.standard.set(Double(fontSize), forKey: "fontSize") }
    }
    @Published var backgroundImagePath: String? {
        didSet { UserDefaults.standard.set(backgroundImagePath, forKey: "backgroundImagePath") }
    }
    @Published var backgroundImageOpacity: Double {
        didSet { UserDefaults.standard.set(backgroundImageOpacity, forKey: "backgroundImageOpacity") }
    }
    @Published var showClock: Bool {
        didSet { UserDefaults.standard.set(showClock, forKey: "showClock") }
    }
    @Published var showUnlockHint: Bool {
        didSet { UserDefaults.standard.set(showUnlockHint, forKey: "showUnlockHint") }
    }

    // MARK: - Hotkey

    @Published var hotkeyKeyCode: UInt32 {
        didSet {
            UserDefaults.standard.set(hotkeyKeyCode, forKey: "hotkeyKeyCode")
            NotificationCenter.default.post(name: .hotkeyChanged, object: nil)
        }
    }
    @Published var hotkeyModifiers: UInt32 {
        didSet {
            UserDefaults.standard.set(hotkeyModifiers, forKey: "hotkeyModifiers")
            NotificationCenter.default.post(name: .hotkeyChanged, object: nil)
        }
    }

    private init() {
        let defaults = UserDefaults.standard

        self.backgroundColor = Self.loadColor(forKey: "backgroundColor") ?? NSColor(red: 0.05, green: 0.05, blue: 0.12, alpha: 1.0)
        self.textColor = Self.loadColor(forKey: "textColor") ?? NSColor.white
        self.lockText = defaults.string(forKey: "lockText") ?? "DESK LOCKED"
        self.subtitleText = defaults.string(forKey: "subtitleText") ?? ""
        self.fontSize = CGFloat(defaults.object(forKey: "fontSize") as? Double ?? 72.0)
        self.backgroundImagePath = defaults.string(forKey: "backgroundImagePath")
        self.backgroundImageOpacity = defaults.object(forKey: "backgroundImageOpacity") as? Double ?? 0.3
        self.showClock = defaults.object(forKey: "showClock") as? Bool ?? true
        self.showUnlockHint = defaults.object(forKey: "showUnlockHint") as? Bool ?? true

        // Default: Cmd+Shift+| (key left of 1)
        let defaultKeyCode = UInt32(kVK_ANSI_Grave)
        let defaultModifiers = UInt32(cmdKey | shiftKey)
        self.hotkeyKeyCode = defaults.object(forKey: "hotkeyKeyCode") as? UInt32 ?? defaultKeyCode
        self.hotkeyModifiers = defaults.object(forKey: "hotkeyModifiers") as? UInt32 ?? defaultModifiers
    }

    func resetToDefaults() {
        backgroundColor = NSColor(red: 0.05, green: 0.05, blue: 0.12, alpha: 1.0)
        textColor = NSColor.white
        lockText = "DESK LOCKED"
        subtitleText = ""
        fontSize = 72.0
        backgroundImagePath = nil
        backgroundImageOpacity = 0.3
        showClock = true
        showUnlockHint = true
        hotkeyKeyCode = UInt32(kVK_ANSI_Grave)
        hotkeyModifiers = UInt32(cmdKey | shiftKey)
    }

    // MARK: - Hotkey Display

    var hotkeyDisplayString: String {
        return Self.displayString(keyCode: hotkeyKeyCode, modifiers: hotkeyModifiers)
    }

    static func displayString(keyCode: UInt32, modifiers: UInt32) -> String {
        var s = ""
        if modifiers & UInt32(controlKey) != 0 { s += "\u{2303}" }   // ⌃
        if modifiers & UInt32(optionKey) != 0  { s += "\u{2325}" }   // ⌥
        if modifiers & UInt32(shiftKey) != 0   { s += "\u{21E7}" }   // ⇧
        if modifiers & UInt32(cmdKey) != 0     { s += "\u{2318}" }   // ⌘
        s += keyName(for: keyCode)
        return s
    }

    static func carbonModifiers(from flags: NSEvent.ModifierFlags) -> UInt32 {
        var mods: UInt32 = 0
        if flags.contains(.command) { mods |= UInt32(cmdKey) }
        if flags.contains(.shift)   { mods |= UInt32(shiftKey) }
        if flags.contains(.option)  { mods |= UInt32(optionKey) }
        if flags.contains(.control) { mods |= UInt32(controlKey) }
        return mods
    }

    static func cgEventFlags(fromCarbon mods: UInt32) -> CGEventFlags {
        var flags: CGEventFlags = []
        if mods & UInt32(cmdKey) != 0     { flags.insert(.maskCommand) }
        if mods & UInt32(shiftKey) != 0   { flags.insert(.maskShift) }
        if mods & UInt32(optionKey) != 0  { flags.insert(.maskAlternate) }
        if mods & UInt32(controlKey) != 0 { flags.insert(.maskControl) }
        return flags
    }

    private static func keyName(for keyCode: UInt32) -> String {
        let names: [UInt32: String] = [
            0x00: "A", 0x01: "S", 0x02: "D", 0x03: "F", 0x04: "H",
            0x05: "G", 0x06: "Z", 0x07: "X", 0x08: "C", 0x09: "V",
            0x0B: "B", 0x0C: "Q", 0x0D: "W", 0x0E: "E", 0x0F: "R",
            0x10: "Y", 0x11: "T", 0x12: "1", 0x13: "2", 0x14: "3",
            0x15: "4", 0x16: "6", 0x17: "5", 0x18: "=", 0x19: "9",
            0x1A: "7", 0x1B: "-", 0x1C: "8", 0x1D: "0", 0x1E: "]",
            0x1F: "O", 0x20: "U", 0x21: "[", 0x22: "I", 0x23: "P",
            0x25: "L", 0x26: "J", 0x27: "'", 0x28: "K", 0x29: ";",
            0x2A: "\\", 0x2B: ",", 0x2C: "/", 0x2D: "N", 0x2E: "M",
            0x2F: ".", 0x24: "Return", 0x30: "Tab", 0x31: "Space",
            0x32: "|", 0x33: "Delete", 0x35: "Esc",
            0x7A: "F1", 0x78: "F2", 0x63: "F3", 0x76: "F4",
            0x60: "F5", 0x61: "F6", 0x62: "F7", 0x64: "F8",
            0x65: "F9", 0x6D: "F10", 0x67: "F11", 0x6F: "F12",
        ]
        return names[keyCode] ?? "Key(\(keyCode))"
    }

    // MARK: - Color Persistence

    private func saveColor(_ color: NSColor, forKey key: String) {
        if let data = try? NSKeyedArchiver.archivedData(withRootObject: color, requiringSecureCoding: true) {
            UserDefaults.standard.set(data, forKey: key)
        }
    }

    private static func loadColor(forKey key: String) -> NSColor? {
        guard let data = UserDefaults.standard.data(forKey: key) else { return nil }
        return try? NSKeyedUnarchiver.unarchivedObject(ofClass: NSColor.self, from: data)
    }
}
