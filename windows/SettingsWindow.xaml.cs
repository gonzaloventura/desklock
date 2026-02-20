using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace DeskLock;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private bool _isRecording;
    private bool _initialized;

    public SettingsWindow(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();
        LoadFromSettings();
        PreviewKeyDown += OnPreviewKeyDown;
        _initialized = true;
    }

    private void LoadFromSettings()
    {
        BgColorBox.Text = _settings.BackgroundColor;
        TxtColorBox.Text = _settings.TextColor;
        FontSizeSlider.Value = _settings.FontSize;
        LockTextBox.Text = _settings.LockText;
        SubtitleBox.Text = _settings.SubtitleText;
        ShowClockCheck.IsChecked = _settings.ShowClock;
        ShowHintCheck.IsChecked = _settings.ShowUnlockHint;
        ImageOpacitySlider.Value = _settings.BackgroundImageOpacity;
        BgAlphaSlider.Value = _settings.BackgroundAlpha;
        BgBlurCheck.IsChecked = _settings.BackgroundBlur;
        UpdateShortcutDisplay();
        UpdateImageUI();
    }

    // --- Shortcut ---

    private void RecordButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isRecording)
        {
            StopRecording();
        }
        else
        {
            _isRecording = true;
            RecordButton.Content = "Cancel";
            RecordingHint.Text = "Press your new shortcut...";
        }
    }

    private void ResetShortcut_Click(object sender, RoutedEventArgs e)
    {
        StopRecording();
        _settings.HotkeyVirtualKey = 0x2E;
        _settings.HotkeyCtrl = true;
        _settings.HotkeyShift = true;
        _settings.HotkeyAlt = false;
        _settings.HotkeyWin = false;
        _settings.ApplyToNativeInterop();
        _settings.Save();
        UpdateShortcutDisplay();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_isRecording) return;
        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        // Escape cancels
        if (key == Key.Escape)
        {
            StopRecording();
            return;
        }

        // Ignore modifier-only presses
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift
            or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin)
            return;

        bool ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
        bool shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        bool alt = Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);
        bool win = Keyboard.Modifiers.HasFlag(ModifierKeys.Windows);

        // Require at least Ctrl or Alt
        if (!ctrl && !alt)
            return;

        int vk = KeyInterop.VirtualKeyFromKey(key);
        _settings.HotkeyVirtualKey = vk;
        _settings.HotkeyCtrl = ctrl;
        _settings.HotkeyShift = shift;
        _settings.HotkeyAlt = alt;
        _settings.HotkeyWin = win;
        _settings.ApplyToNativeInterop();
        _settings.Save();

        StopRecording();
        UpdateShortcutDisplay();
    }

    private void StopRecording()
    {
        _isRecording = false;
        RecordButton.Content = "Record New Shortcut";
        RecordingHint.Text = "";
    }

    private void UpdateShortcutDisplay()
    {
        NativeInterop.HotkeyVirtualKey = _settings.HotkeyVirtualKey;
        NativeInterop.HotkeyCtrl = _settings.HotkeyCtrl;
        NativeInterop.HotkeyShift = _settings.HotkeyShift;
        NativeInterop.HotkeyAlt = _settings.HotkeyAlt;
        NativeInterop.HotkeyWin = _settings.HotkeyWin;
        ShortcutDisplay.Text = NativeInterop.GetHotkeyDisplayString();
    }

    // --- Appearance ---

    private void BgColorBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (!_initialized) return;
        _settings.BackgroundColor = BgColorBox.Text;
        _settings.Save();
    }

    private void TxtColorBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (!_initialized) return;
        _settings.TextColor = TxtColorBox.Text;
        _settings.Save();
    }

    private void PickBgColor_Click(object sender, RoutedEventArgs e)
    {
        var color = ShowColorDialog(_settings.BackgroundColor);
        if (color != null)
        {
            BgColorBox.Text = color;
            _settings.BackgroundColor = color;
            _settings.Save();
        }
    }

    private void PickTxtColor_Click(object sender, RoutedEventArgs e)
    {
        var color = ShowColorDialog(_settings.TextColor);
        if (color != null)
        {
            TxtColorBox.Text = color;
            _settings.TextColor = color;
            _settings.Save();
        }
    }

    private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_initialized || FontSizeLabel == null) return;
        FontSizeLabel.Text = $"{(int)e.NewValue}";
        _settings.FontSize = e.NewValue;
        _settings.Save();
    }

    private void BgAlphaSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_initialized || BgAlphaLabel == null) return;
        BgAlphaLabel.Text = $"{(int)(e.NewValue * 100)}%";
        _settings.BackgroundAlpha = e.NewValue;
        _settings.Save();
    }

    private void BgBlurCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;
        _settings.BackgroundBlur = BgBlurCheck.IsChecked == true;
        _settings.Save();
    }

    private void ResetDefaults_Click(object sender, RoutedEventArgs e)
    {
        _settings.ResetToDefaults();
        _settings.ApplyToNativeInterop();
        _settings.Save();
        LoadFromSettings();
    }

    // --- Content ---

    private void LockTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (!_initialized) return;
        _settings.LockText = LockTextBox.Text;
        _settings.Save();
    }

    private void SubtitleBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (!_initialized) return;
        _settings.SubtitleText = SubtitleBox.Text;
        _settings.Save();
    }

    private void ShowClockCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;
        _settings.ShowClock = ShowClockCheck.IsChecked == true;
        _settings.Save();
    }

    private void ShowHintCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;
        _settings.ShowUnlockHint = ShowHintCheck.IsChecked == true;
        _settings.Save();
    }

    // --- Background ---

    private void ChooseImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Choose Background Image",
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            _settings.BackgroundImagePath = dialog.FileName;
            _settings.Save();
            UpdateImageUI();
        }
    }

    private void RemoveImage_Click(object sender, RoutedEventArgs e)
    {
        _settings.BackgroundImagePath = null;
        _settings.Save();
        UpdateImageUI();
    }

    private void ImageOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_initialized || OpacityLabel == null) return;
        OpacityLabel.Text = $"{(int)(e.NewValue * 100)}%";
        _settings.BackgroundImageOpacity = e.NewValue;
        _settings.Save();
    }

    private void UpdateImageUI()
    {
        if (!string.IsNullOrEmpty(_settings.BackgroundImagePath))
        {
            ImagePathLabel.Text = System.IO.Path.GetFileName(_settings.BackgroundImagePath);
            ImagePathLabel.Foreground = System.Windows.Media.Brushes.Black;
            RemoveImageBtn.Visibility = Visibility.Visible;
            OpacityGrid.Visibility = Visibility.Visible;
        }
        else
        {
            ImagePathLabel.Text = "No image selected";
            ImagePathLabel.Foreground = System.Windows.Media.Brushes.Gray;
            RemoveImageBtn.Visibility = Visibility.Collapsed;
            OpacityGrid.Visibility = Visibility.Collapsed;
        }
    }

    // --- Helpers ---

    private static string? ShowColorDialog(string currentHex)
    {
        var dialog = new System.Windows.Forms.ColorDialog();
        try
        {
            var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(currentHex);
            dialog.Color = System.Drawing.Color.FromArgb(color.R, color.G, color.B);
        }
        catch { }

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            return $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
        }
        return null;
    }
}
