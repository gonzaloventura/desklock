using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DeskLock;

public partial class LockOverlay : Window
{
    private readonly DispatcherTimer _clockTimer;

    public LockOverlay()
    {
        InitializeComponent();
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClock();
    }

    public void ApplySettings(AppSettings settings)
    {
        // Background color
        try
        {
            BackgroundRect.Fill = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(settings.BackgroundColor));
        }
        catch { BackgroundRect.Fill = new SolidColorBrush(Color.FromRgb(13, 13, 31)); }

        // Text color
        Brush textBrush;
        try { textBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(settings.TextColor)); }
        catch { textBrush = Brushes.White; }

        LockIcon.Foreground = new SolidColorBrush(
            SetOpacity((textBrush as SolidColorBrush)!.Color, 0.6));
        LockTextBlock.Foreground = textBrush;
        LockTextBlock.FontSize = settings.FontSize;
        LockTextBlock.Text = settings.LockText;

        // Character spacing (tracking)
        LockTextBlock.SetValue(TextOptions.TextFormattingModeProperty, TextFormattingMode.Ideal);

        // Subtitle
        if (!string.IsNullOrWhiteSpace(settings.SubtitleText))
        {
            SubtitleBlock.Text = settings.SubtitleText;
            SubtitleBlock.Foreground = new SolidColorBrush(
                SetOpacity((textBrush as SolidColorBrush)!.Color, 0.6));
            SubtitleBlock.Visibility = Visibility.Visible;
        }
        else
        {
            SubtitleBlock.Visibility = Visibility.Collapsed;
        }

        // Clock
        if (settings.ShowClock)
        {
            ClockBlock.Foreground = new SolidColorBrush(
                SetOpacity((textBrush as SolidColorBrush)!.Color, 0.5));
            ClockBlock.Visibility = Visibility.Visible;
            UpdateClock();
        }
        else
        {
            ClockBlock.Visibility = Visibility.Collapsed;
        }

        // Hint
        if (settings.ShowUnlockHint)
        {
            HintBlock.Text = $"Press  {NativeInterop.GetHotkeyDisplayString()}  to unlock";
            HintBlock.Foreground = new SolidColorBrush(
                SetOpacity((textBrush as SolidColorBrush)!.Color, 0.25));
            HintBlock.Visibility = Visibility.Visible;
        }
        else
        {
            HintBlock.Visibility = Visibility.Collapsed;
        }

        // Background image
        if (!string.IsNullOrEmpty(settings.BackgroundImagePath) && File.Exists(settings.BackgroundImagePath))
        {
            try
            {
                var bitmap = new BitmapImage(new Uri(settings.BackgroundImagePath));
                BackgroundImage.Source = bitmap;
                BackgroundImage.Opacity = settings.BackgroundImageOpacity;
                BackgroundImage.Visibility = Visibility.Visible;
            }
            catch
            {
                BackgroundImage.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            BackgroundImage.Visibility = Visibility.Collapsed;
        }
    }

    public void ShowOnPrimaryScreen()
    {
        var screen = System.Windows.Forms.Screen.PrimaryScreen!;
        var bounds = screen.Bounds;

        // Convert physical pixels to WPF logical units
        var dpiScale = VisualTreeHelper.GetDpi(this);
        Left = bounds.Left / dpiScale.DpiScaleX;
        Top = bounds.Top / dpiScale.DpiScaleY;
        Width = bounds.Width / dpiScale.DpiScaleX;
        Height = bounds.Height / dpiScale.DpiScaleY;

        Opacity = 0;
        Show();
        Activate();
        Focus();

        _clockTimer.Start();

        // Fade in
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300))
        {
            EasingFunction = new QuadraticEase()
        };
        BeginAnimation(OpacityProperty, fadeIn);
    }

    public void DismissOverlay(Action? onComplete = null)
    {
        _clockTimer.Stop();

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
