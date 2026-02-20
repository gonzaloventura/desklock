import SwiftUI
import Carbon.HIToolbox

struct SettingsView: View {
    @ObservedObject private var settings = SettingsManager.shared
    @State private var bgColor: Color
    @State private var txtColor: Color
    @State private var isRecordingHotkey = false
    @State private var hotkeyMonitor: Any?

    init() {
        let s = SettingsManager.shared
        _bgColor = State(initialValue: Color(nsColor: s.backgroundColor))
        _txtColor = State(initialValue: Color(nsColor: s.textColor))
    }

    var body: some View {
        TabView {
            shortcutTab
                .tabItem {
                    Label("Shortcut", systemImage: "keyboard")
                }

            appearanceTab
                .tabItem {
                    Label("Appearance", systemImage: "paintbrush")
                }

            contentTab
                .tabItem {
                    Label("Content", systemImage: "textformat")
                }

            backgroundTab
                .tabItem {
                    Label("Background", systemImage: "photo")
                }
        }
        .frame(width: 480, height: 400)
        .padding()
    }

    // MARK: - Shortcut Tab

    private var shortcutTab: some View {
        Form {
            Section("Lock / Unlock Shortcut") {
                VStack(spacing: 20) {
                    // Current shortcut display
                    Text(settings.hotkeyDisplayString)
                        .font(.system(size: 36, weight: .medium, design: .rounded))
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 20)
                        .background(
                            RoundedRectangle(cornerRadius: 12)
                                .fill(isRecordingHotkey
                                      ? Color.accentColor.opacity(0.1)
                                      : Color(nsColor: .controlBackgroundColor))
                                .overlay(
                                    RoundedRectangle(cornerRadius: 12)
                                        .strokeBorder(isRecordingHotkey
                                                      ? Color.accentColor
                                                      : Color(nsColor: .separatorColor),
                                                      lineWidth: isRecordingHotkey ? 2 : 1)
                                )
                        )

                    if isRecordingHotkey {
                        Text("Press your new shortcut...")
                            .foregroundColor(.accentColor)
                            .font(.system(size: 13, weight: .medium))
                    }

                    HStack(spacing: 12) {
                        Button(isRecordingHotkey ? "Cancel" : "Record New Shortcut") {
                            if isRecordingHotkey {
                                stopRecording()
                            } else {
                                startRecording()
                            }
                        }
                        .keyboardShortcut(.defaultAction)

                        Button("Reset to Default") {
                            stopRecording()
                            settings.hotkeyKeyCode = UInt32(kVK_ANSI_Grave)
                            settings.hotkeyModifiers = UInt32(cmdKey | shiftKey)
                        }
                    }
                }
                .padding(.vertical, 8)
            }

            Section {
                Text("The shortcut must include at least \u{2318} Command or \u{2303} Control as a modifier.")
                    .font(.caption)
                    .foregroundColor(.secondary)
            }
        }
        .padding()
    }

    private func startRecording() {
        isRecordingHotkey = true

        hotkeyMonitor = NSEvent.addLocalMonitorForEvents(matching: [.keyDown]) { event in
            let mods = SettingsManager.carbonModifiers(from: event.modifierFlags)
            let hasRequired = (mods & UInt32(cmdKey) != 0) || (mods & UInt32(controlKey) != 0)

            if event.keyCode == 53 { // Escape — cancel
                stopRecording()
                return nil
            }

            if hasRequired {
                settings.hotkeyKeyCode = UInt32(event.keyCode)
                settings.hotkeyModifiers = mods
                stopRecording()
            }

            return nil // eat the event while recording
        }
    }

    private func stopRecording() {
        isRecordingHotkey = false
        if let monitor = hotkeyMonitor {
            NSEvent.removeMonitor(monitor)
            hotkeyMonitor = nil
        }
    }

    // MARK: - Appearance Tab

    private var appearanceTab: some View {
        Form {
            Section {
                ColorPicker("Background Color", selection: $bgColor, supportsOpacity: false)
                    .onChange(of: bgColor) { newValue in
                        settings.backgroundColor = NSColor(newValue)
                    }

                ColorPicker("Text Color", selection: $txtColor, supportsOpacity: false)
                    .onChange(of: txtColor) { newValue in
                        settings.textColor = NSColor(newValue)
                    }

                HStack {
                    Text("Font Size")
                    Slider(value: $settings.fontSize, in: 24...200, step: 2)
                    Text("\(Int(settings.fontSize)) pt")
                        .frame(width: 50)
                        .monospacedDigit()
                }
            }

            Section {
                Button("Reset to Defaults") {
                    settings.resetToDefaults()
                    bgColor = Color(nsColor: settings.backgroundColor)
                    txtColor = Color(nsColor: settings.textColor)
                }
            }
        }
        .padding()
    }

    // MARK: - Content Tab

    private var contentTab: some View {
        Form {
            Section {
                HStack {
                    Text("Lock Text")
                    TextField("DESK LOCKED", text: $settings.lockText)
                        .textFieldStyle(.roundedBorder)
                }

                HStack {
                    Text("Subtitle")
                    TextField("Optional subtitle...", text: $settings.subtitleText)
                        .textFieldStyle(.roundedBorder)
                }
            }

            Section {
                Toggle("Show Clock", isOn: $settings.showClock)
                Toggle("Show Unlock Hint", isOn: $settings.showUnlockHint)
            }
        }
        .padding()
    }

    // MARK: - Background Tab

    private var backgroundTab: some View {
        Form {
            Section {
                HStack {
                    Text("Background Image")
                    Spacer()
                    if let path = settings.backgroundImagePath {
                        Text(URL(fileURLWithPath: path).lastPathComponent)
                            .foregroundColor(.secondary)
                            .lineLimit(1)
                            .truncationMode(.middle)
                    } else {
                        Text("None")
                            .foregroundColor(.secondary)
                    }
                }

                HStack {
                    Button("Choose Image...") {
                        chooseImage()
                    }
                    if settings.backgroundImagePath != nil {
                        Button("Remove") {
                            settings.backgroundImagePath = nil
                        }
                        .foregroundColor(.red)
                    }
                }

                if settings.backgroundImagePath != nil {
                    HStack {
                        Text("Image Opacity")
                        Slider(value: $settings.backgroundImageOpacity, in: 0.05...1.0, step: 0.05)
                        Text("\(Int(settings.backgroundImageOpacity * 100))%")
                            .frame(width: 40)
                            .monospacedDigit()
                    }
                }
            }

            Section("Preview") {
                ZStack {
                    Color(nsColor: settings.backgroundColor)

                    if let path = settings.backgroundImagePath,
                       let nsImage = NSImage(contentsOfFile: path) {
                        Image(nsImage: nsImage)
                            .resizable()
                            .aspectRatio(contentMode: .fill)
                            .opacity(settings.backgroundImageOpacity)
                    }

                    Text(settings.lockText)
                        .font(.system(size: 18, weight: .bold))
                        .foregroundColor(Color(nsColor: settings.textColor))
                        .tracking(2)
                }
                .frame(height: 120)
                .clipShape(RoundedRectangle(cornerRadius: 8))
            }
        }
        .padding()
    }

    private func chooseImage() {
        let panel = NSOpenPanel()
        panel.allowedContentTypes = [.image]
        panel.canChooseFiles = true
        panel.canChooseDirectories = false
        panel.allowsMultipleSelection = false
        panel.title = "Choose Background Image"

        if panel.runModal() == .OK, let url = panel.url {
            settings.backgroundImagePath = url.path
        }
    }
}
