using System;
using System.IO;
using System.Windows;

namespace DeskLock;

public static class Program
{
    private static readonly string LogDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeskLock");
    private static readonly string LogFile = Path.Combine(LogDir, "startup.log");

    [STAThread]
    public static void Main()
    {
        try
        {
            Directory.CreateDirectory(LogDir);
            File.WriteAllText(LogFile, $"[{DateTime.Now}] DeskLock starting...\r\n");

            File.AppendAllText(LogFile, $"[{DateTime.Now}] Creating WPF App...\r\n");
            var app = new App();

            File.AppendAllText(LogFile, $"[{DateTime.Now}] InitializeComponent...\r\n");
            app.InitializeComponent();

            File.AppendAllText(LogFile, $"[{DateTime.Now}] Running...\r\n");
            app.Run();
        }
        catch (Exception ex)
        {
            var msg = $"DeskLock failed to start:\n\n{ex.Message}\n\n{ex.StackTrace}";
            try { File.AppendAllText(LogFile, $"[{DateTime.Now}] CRASH: {ex}\r\n"); }
            catch { }
            MessageBox.Show(msg, "DeskLock Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
