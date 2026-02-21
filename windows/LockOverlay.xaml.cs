using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DeskLock;

public partial class LockOverlay : Window
{
    private readonly DispatcherTimer _clockTimer;

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

    public LockOverlay()
    {
        InitializeComponent();
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClock();
    }

    public void ApplySettings(AppSettings settings)
    {
        try
        {
            BackgroundRect.Fill = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(settings.BackgroundColor));
        }
        catch { BackgroundRect.Fill = new SolidColorBrush(Color.FromRgb(13, 13, 31)); }

        BackgroundRect.Opacity = settings.BackgroundAlpha;

        Brush textBrush;
        try { textBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(settings.TextColor)); }
        catch { textBrush = Brushes.White; }

        var textColor = (textBrush as SolidColorBrush)!.Color;

        LockIcon.Foreground = new SolidColorBrush(SetOpacity(textColor, 0.6));
        LockTextBlock.Foreground = textBrush;
        LockTextBlock.FontSize = settings.FontSize;
        LockTextBlock.Text = settings.LockText;

        if (!string.IsNullOrWhiteSpace(settings.SubtitleText))
        {
            SubtitleBlock.Text = settings.SubtitleText;
            SubtitleBlock.Foreground = new SolidColorBrush(SetOpacity(textColor, 0.6));
            SubtitleBlock.Visibility = Visibility.Visible;
        }
        else
        {
            SubtitleBlock.Visibility = Visibility.Collapsed;
        }

        if (settings.ShowClock)
        {
            ClockBlock.Foreground = new SolidColorBrush(SetOpacity(textColor, 0.5));
            ClockBlock.Visibility = Visibility.Visible;
            UpdateClock();
        }
        else
        {
            ClockBlock.Visibility = Visibility.Collapsed;
        }

        if (settings.ShowUnlockHint)
        {
            HintBlock.Text = $"Press  {NativeInterop.GetHotkeyDisplayString()}  to unlock";
            HintBlock.Foreground = new SolidColorBrush(SetOpacity(textColor, 0.25));
            HintBlock.Visibility = Visibility.Visible;
        }
        else
        {
            HintBlock.Visibility = Visibility.Collapsed;
        }

        if (!string.IsNullOrEmpty(settings.BackgroundImagePath) && File.Exists(settings.BackgroundImagePath))
        {
            try
            {
                var bitmap = new BitmapImage(new Uri(settings.BackgroundImagePath));
                var brush = new ImageBrush(bitmap)
                {
                    Stretch = Stretch.UniformToFill,
                    AlignmentX = AlignmentX.Center,
                    AlignmentY = AlignmentY.Center
                };
                BackgroundImageRect.Fill = brush;
                BackgroundImageRect.Opacity = settings.BackgroundImageOpacity;
                BackgroundImageRect.Visibility = Visibility.Visible;
            }
            catch
            {
                BackgroundImageRect.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            BackgroundImageRect.Visibility = Visibility.Collapsed;
        }
    }

    public void ShowOnPrimaryScreen()
    {
        var screen = System.Windows.Forms.Screen.PrimaryScreen!;
        var bounds = screen.Bounds;

        // Get DPI scale safely
        double scaleX = 1.0, scaleY = 1.0;
        try
        {
            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget != null)
            {
                scaleX = source.CompositionTarget.TransformToDevice.M11;
                scaleY = source.CompositionTarget.TransformToDevice.M22;
            }
            else
            {
                // Window not yet shown — query monitor DPI directly
                var hMonitor = MonitorFromWindow(IntPtr.Zero, 1); // MONITOR_DEFAULTTOPRIMARY
                if (GetDpiForMonitor(hMonitor, 0, out uint dpiX, out uint dpiY) == 0)
                {
                    scaleX = dpiX / 96.0;
                    scaleY = dpiY / 96.0;
                }
            }
        }
        catch { /* fallback to 1.0 */ }

        Left = bounds.Left / scaleX;
        Top = bounds.Top / scaleY;
        Width = bounds.Width / scaleX;
        Height = bounds.Height / scaleY;

        Opacity = 0;
        Show();
        Activate();
        Focus();

        // Apply blur-behind if enabled
        var settings = AppSettings.Load();
        if (settings.BackgroundBlur)
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            NativeInterop.EnableWindowBlur(hwnd);
        }

        _clockTimer.Start();

        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300))
        {
            EasingFunction = new QuadraticEase()
        };
        BeginAnimation(OpacityProperty, fadeIn);
    }

    public void DismissOverlay(Action? onComplete = null)
    {
        _clockTimer.Stop();

        // Disable blur
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        NativeInterop.DisableWindowBlur(hwnd);

        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300))
        {
            EasingFunction = new QuadraticEase()
        };
        fadeOut.Completed += (_, _) =>
        {
            Hide();
            onComplete?.Invoke();
        };
        BeginAnimation(OpacityProperty, fadeOut);
    }

    private void UpdateClock()
    {
        ClockBlock.Text = DateTime.Now.ToString("HH:mm:ss");
    }

    private static Color SetOpacity(Color color, double opacity)
    {
        return Color.FromArgb((byte)(255 * opacity), color.R, color.G, color.B);
    }
}
