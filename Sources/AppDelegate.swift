import Cocoa
import SwiftUI
import Carbon.HIToolbox

class AppDelegate: NSObject, NSApplicationDelegate {
    private var statusItem: NSStatusItem!
    private var overlayWindow: OverlayWindow?
    private var settingsWindow: NSWindow?
    private var isLocked = false
    private var hotKeyRef: EventHotKeyRef?
    private var carbonHandlerRef: EventHandlerRef?
    private var reactivationTimer: Timer?

    func applicationDidFinishLaunching(_ notification: Notification) {
        NSApp.setActivationPolicy(.accessory)

        setupStatusBar()
        registerGlobalHotkey()
        setupEventInterceptor()

        NotificationCenter.default.addObserver(
            self,
            selector: #selector(hotkeySettingsChanged),
            name: .hotkeyChanged,
            object: nil
        )
    }

    // MARK: - Status Bar

    private func setupStatusBar() {
        statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)

        if let button = statusItem.button {
            button.image = NSImage(systemSymbolName: "lock.desktopcomputer", accessibilityDescription: "DeskLock")
        }

        rebuildMenu()
    }

    private func rebuildMenu() {
        let settings = SettingsManager.shared
        let menu = NSMenu()

        let hotkeyStr = settings.hotkeyDisplayString
        let lockTitle = isLocked ? "Unlock Desk  (\(hotkeyStr))" : "Lock Desk  (\(hotkeyStr))"
        let lockItem = NSMenuItem(title: lockTitle, action: #selector(toggleLock), keyEquivalent: "")
        lockItem.target = self
        menu.addItem(lockItem)

        menu.addItem(NSMenuItem.separator())

        let settingsItem = NSMenuItem(title: "Settings...", action: #selector(openSettings), keyEquivalent: ",")
        settingsItem.target = self
        menu.addItem(settingsItem)

        menu.addItem(NSMenuItem.separator())

        let quitItem = NSMenuItem(title: "Quit DeskLock", action: #selector(quitApp), keyEquivalent: "q")
        quitItem.target = self
        menu.addItem(quitItem)

        statusItem.menu = menu
    }

    // MARK: - Global Hotkey (Carbon)

    private func registerGlobalHotkey() {
        if let ref = hotKeyRef {
            UnregisterEventHotKey(ref)
            hotKeyRef = nil
        }

        let settings = SettingsManager.shared
        let hotkeyID = EventHotKeyID(signature: OSType(0x444C4B00), id: 1)
        var ref: EventHotKeyRef?

        let status = RegisterEventHotKey(
            settings.hotkeyKeyCode,
            settings.hotkeyModifiers,
            hotkeyID,
            GetApplicationEventTarget(),
            0,
            &ref
        )

        if status == noErr {
            hotKeyRef = ref
            print("Hotkey registered: \(settings.hotkeyDisplayString)")
        } else {
            print("Failed to register hotkey: \(status)")
        }

        if carbonHandlerRef == nil {
            var eventType = EventTypeSpec(eventClass: OSType(kEventClassKeyboard), eventKind: UInt32(kEventHotKeyPressed))
            let userData = Unmanaged.passUnretained(self).toOpaque()

            InstallEventHandler(
                GetApplicationEventTarget(),
                { (_, event, userData) -> OSStatus in
                    guard let userData = userData else { return OSStatus(eventNotHandledErr) }
                    let delegate = Unmanaged<AppDelegate>.fromOpaque(userData).takeUnretainedValue()
                    DispatchQueue.main.async {
                        delegate.toggleLock()
                    }
                    return noErr
                },
                1,
                &eventType,
                userData,
                &carbonHandlerRef
            )
        }
    }

    @objc private func hotkeySettingsChanged() {
        registerGlobalHotkey()
        rebuildMenu()
    }

    // MARK: - Event Interceptor

    private func setupEventInterceptor() {
        let interceptor = EventInterceptor.shared

        interceptor.onToggleLock = { [weak self] in
            self?.toggleLock()
        }

        if !EventInterceptor.checkAccessibility() {
            DispatchQueue.main.asyncAfter(deadline: .now() + 1.0) {
                let alert = NSAlert()
                alert.messageText = "Accessibility Access Required"
                alert.informativeText = "DeskLock needs Accessibility permissions to fully block keyboard and mouse input when locked.\n\nGrant access in:\nSystem Settings > Privacy & Security > Accessibility\n\nThen restart DeskLock."
                alert.alertStyle = .warning
                alert.addButton(withTitle: "Open System Settings")
                alert.addButton(withTitle: "Continue Without Full Blocking")

                let response = alert.runModal()
                if response == .alertFirstButtonReturn {
                    if let url = URL(string: "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility") {
                        NSWorkspace.shared.open(url)
                    }
                }

                _ = interceptor.start()
            }
        } else {
            if !interceptor.start() {
                print("Warning: Could not start event interceptor")
            }
        }

        // Local monitor: eat ALL events when locked, only allow unlock hotkey
        NSEvent.addLocalMonitorForEvents(matching: [.keyDown, .keyUp, .flagsChanged,
                                                     .leftMouseDown, .leftMouseUp,
                                                     .rightMouseDown, .rightMouseUp,
                                                     .otherMouseDown, .otherMouseUp,
                                                     .mouseMoved, .scrollWheel]) { [weak self] event in
            guard let self = self, self.isLocked else { return event }

            // Only allow the unlock hotkey through
            if event.type == .keyDown {
                let settings = SettingsManager.shared
                let pressedMods = SettingsManager.carbonModifiers(from: event.modifierFlags)
                if UInt32(event.keyCode) == settings.hotkeyKeyCode && pressedMods == settings.hotkeyModifiers {
                    self.toggleLock()
                }
            }

            return nil // eat everything
        }
    }

    // MARK: - Lock/Unlock

    @objc func toggleLock() {
        if isLocked {
            unlock()
        } else {
            lock()
        }
    }

    private func lock() {
        guard !isLocked else { return }
        isLocked = true
        EventInterceptor.shared.isLocked = true

        // Block system shortcuts: Cmd+Tab, Force Quit, Cmd+H, hide Dock & Menu Bar
        NSApp.presentationOptions = [
            .disableProcessSwitching,
            .disableForceQuit,
            .disableHideApplication,
            .disableSessionTermination,
            .hideMenuBar,
            .hideDock,
        ]

        if overlayWindow == nil {
            overlayWindow = OverlayWindow()
        }
        overlayWindow?.showOnMainScreen()

        // Timer to keep our window on top and re-grab focus
        reactivationTimer = Timer.scheduledTimer(withTimeInterval: 0.5, repeats: true) { [weak self] _ in
            guard let self = self, self.isLocked else { return }
            self.overlayWindow?.orderFrontRegardless()
            self.overlayWindow?.makeKeyAndOrderFront(nil)
            NSApp.activate(ignoringOtherApps: true)
        }

        NSApp.activate(ignoringOtherApps: true)
        rebuildMenu()
        print("DESK LOCKED")
    }

    private func unlock() {
        guard isLocked else { return }
        isLocked = false
        EventInterceptor.shared.isLocked = false

        reactivationTimer?.invalidate()
        reactivationTimer = nil

        // Restore normal presentation
        NSApp.presentationOptions = []

        overlayWindow?.dismiss()
        rebuildMenu()
        print("DESK UNLOCKED")
    }

    // MARK: - Settings

    @objc func openSettings() {
        if isLocked { return }

        if settingsWindow == nil {
            let settingsView = SettingsView()
            let hostingController = NSHostingController(rootView: settingsView)

            let window = NSWindow(contentViewController: hostingController)
            window.title = "DeskLock Settings"
            window.styleMask = [.titled, .closable]
            window.center()
            window.isReleasedWhenClosed = false
            settingsWindow = window
        }

        settingsWindow?.makeKeyAndOrderFront(nil)
        NSApp.activate(ignoringOtherApps: true)
    }

    // MARK: - Quit

    @objc func quitApp() {
        if isLocked { unlock() }
        if let ref = hotKeyRef { UnregisterEventHotKey(ref) }
        EventInterceptor.shared.stop()
        NSApp.terminate(nil)
    }
}
