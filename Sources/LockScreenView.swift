import SwiftUI

struct LockScreenView: View {
    @ObservedObject private var settings = SettingsManager.shared
    @State private var currentTime = Date()

    private let timer = Timer.publish(every: 1, on: .main, in: .common).autoconnect()

    var body: some View {
        GeometryReader { geometry in
            ZStack {
                Color(nsColor: settings.backgroundColor)
                    .ignoresSafeArea()

                if let imagePath = settings.backgroundImagePath,
                   let nsImage = NSImage(contentsOfFile: imagePath) {
                    Image(nsImage: nsImage)
                        .resizable()
                        .aspectRatio(contentMode: .fill)
                        .frame(width: geometry.size.width, height: geometry.size.height)
                        .clipped()
                        .opacity(settings.backgroundImageOpacity)
                }

                VStack(spacing: 24) {
                    Spacer()

                    Image(systemName: "lock.fill")
                        .font(.system(size: 56, weight: .thin))
                        .foregroundColor(Color(nsColor: settings.textColor).opacity(0.6))

                    Text(settings.lockText)
                        .font(.system(size: settings.fontSize, weight: .bold, design: .default))
                        .foregroundColor(Color(nsColor: settings.textColor))
                        .tracking(settings.fontSize * 0.12)
                        .shadow(color: .black.opacity(0.5), radius: 10, x: 0, y: 4)

                    if !settings.subtitleText.isEmpty {
                        Text(settings.subtitleText)
                            .font(.system(size: 20, weight: .light))
                            .foregroundColor(Color(nsColor: settings.textColor).opacity(0.6))
                    }

                    if settings.showClock {
                        Text(timeString)
                            .font(.system(size: 42, weight: .ultraLight, design: .monospaced))
                            .foregroundColor(Color(nsColor: settings.textColor).opacity(0.5))
                            .padding(.top, 16)
                    }

                    Spacer()

                    if settings.showUnlockHint {
                        Text("Press  \(settings.hotkeyDisplayString)  to unlock")
                            .font(.system(size: 14, weight: .medium))
                            .foregroundColor(Color(nsColor: settings.textColor).opacity(0.25))
                            .padding(.bottom, 48)
                    }
                }
                .frame(maxWidth: .infinity, maxHeight: .infinity)
            }
        }
        .onReceive(timer) { time in
            currentTime = time
        }
    }

    private var timeString: String {
        let formatter = DateFormatter()
        formatter.dateFormat = "HH:mm:ss"
        return formatter.string(from: currentTime)
    }
}
