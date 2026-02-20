import Cocoa
import SwiftUI

// NSView that becomes first responder and swallows all keyboard/mouse events
class EventEatingView: NSView {
    override var acceptsFirstResponder: Bool { true }
    override func keyDown(with event: NSEvent) {}
    override func keyUp(with event: NSEvent) {}
    override func mouseDown(with event: NSEvent) {}
    override func mouseUp(with event: NSEvent) {}
    override func rightMouseDown(with event: NSEvent) {}
    override func rightMouseUp(with event: NSEvent) {}
    override func otherMouseDown(with event: NSEvent) {}
    override func otherMouseUp(with event: NSEvent) {}
    override func mouseMoved(with event: NSEvent) {}
    override func mouseDragged(with event: NSEvent) {}
    override func scrollWheel(with event: NSEvent) {}
    override func flagsChanged(with event: NSEvent) {}
}

class OverlayWindow: NSWindow {

    private let eatingView = EventEatingView()
    private let blurView = NSVisualEffectView()

    init() {
        let screen = NSScreen.main ?? NSScreen.screens[0]
        super.init(
            contentRect: screen.frame,
            styleMask: .borderless,
            backing: .buffered,
            defer: false
        )

        self.level = NSWindow.Level(rawValue: Int(CGWindowLevelForKey(.maximumWindow)) + 1)
        self.isOpaque = false
        self.hasShadow = false
        self.isMovable = false
        self.isMovableByWindowBackground = false
        self.ignoresMouseEvents = false
        self.acceptsMouseMovedEvents = true
        self.collectionBehavior = [.canJoinAllSpaces, .fullScreenAuxiliary, .stationary]
        self.backgroundColor = .clear
        self.hidesOnDeactivate = false

        // Blur behind window
        blurView.material = .fullScreenUI
        blurView.blendingMode = .behindWindow
        blurView.state = .active
        blurView.translatesAutoresizingMaskIntoConstraints = false

        let hostingView = NSHostingView(rootView: LockScreenView())
        hostingView.translatesAutoresizingMaskIntoConstraints = false
        eatingView.translatesAutoresizingMaskIntoConstraints = false

        let container = NSView(frame: screen.frame)
        container.addSubview(blurView)
        container.addSubview(eatingView)
        container.addSubview(hostingView)

        NSLayoutConstraint.activate([
            blurView.leadingAnchor.constraint(equalTo: container.leadingAnchor),
            blurView.trailingAnchor.constraint(equalTo: container.trailingAnchor),
            blurView.topAnchor.constraint(equalTo: container.topAnchor),
            blurView.bottomAnchor.constraint(equalTo: container.bottomAnchor),
            eatingView.leadingAnchor.constraint(equalTo: container.leadingAnchor),
            eatingView.trailingAnchor.constraint(equalTo: container.trailingAnchor),
            eatingView.topAnchor.constraint(equalTo: container.topAnchor),
            eatingView.bottomAnchor.constraint(equalTo: container.bottomAnchor),
            hostingView.leadingAnchor.constraint(equalTo: container.leadingAnchor),
            hostingView.trailingAnchor.constraint(equalTo: container.trailingAnchor),
            hostingView.topAnchor.constraint(equalTo: container.topAnchor),
            hostingView.bottomAnchor.constraint(equalTo: container.bottomAnchor),
        ])

        self.contentView = container
    }

    func applySettings() {
        let settings = SettingsManager.shared
        blurView.isHidden = !settings.backgroundBlur
        if !settings.backgroundBlur {
            self.isOpaque = settings.backgroundAlpha >= 1.0
            self.backgroundColor = settings.backgroundAlpha >= 1.0 ? .black : .clear
        } else {
            self.isOpaque = false
            self.backgroundColor = .clear
        }
    }

    func showOnMainScreen() {
        applySettings()
        let screen = NSScreen.main ?? NSScreen.screens[0]
        self.setFrame(screen.frame, display: true)
        self.alphaValue = 0

        self.orderFrontRegardless()
        self.makeKey()
        self.makeMain()
        self.makeFirstResponder(eatingView)

        NSAnimationContext.runAnimationGroup { context in
            context.duration = 0.3
            context.timingFunction = CAMediaTimingFunction(name: .easeInEaseOut)
            self.animator().alphaValue = 1.0
        }
    }

    func dismiss(completion: (() -> Void)? = nil) {
        NSAnimationContext.runAnimationGroup({ context in
            context.duration = 0.3
            context.timingFunction = CAMediaTimingFunction(name: .easeInEaseOut)
            self.animator().alphaValue = 0
        }, completionHandler: { [weak self] in
            self?.orderOut(nil)
            completion?()
        })
    }

    override var canBecomeKey: Bool { true }
    override var canBecomeMain: Bool { true }

    // Prevent losing key status
    override func resignKey() {
        // Re-take key immediately if we're supposed to be locked
        DispatchQueue.main.async { [weak self] in
            guard let self = self, self.isVisible else { return }
            self.makeKeyAndOrderFront(nil)
            self.makeFirstResponder(self.eatingView)
        }
    }
}
