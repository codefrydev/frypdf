using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PdfEditorApp.ViewModels;

namespace PdfEditorApp.Views;

public partial class SettingsPageView : UserControl
{
    public SettingsPageView()
    {
        InitializeComponent();

        AddHandler(KeyDownEvent, (sender, e) =>
        {
            if (DataContext is not SettingsViewModel vm || vm.RecordingItem == null) return;

            // Don't commit standalone modifier presses
            if (e.Key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin)
            {
                return;
            }

            if (e.Key == Key.Escape)
            {
                vm.CancelRecording();
                e.Handled = true;
                return;
            }

            var gesture = BuildGestureString(e.Key, e.KeyModifiers);
            if (!string.IsNullOrWhiteSpace(gesture))
            {
                vm.CommitRecordedGesture(gesture);
                e.Handled = true;
            }
        }, RoutingStrategies.Tunnel);
    }

    private static string BuildGestureString(Key key, KeyModifiers modifiers)
    {
        var parts = new List<string>();
        bool isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        if (modifiers.HasFlag(KeyModifiers.Control))
        {
            parts.Add(isMac ? "Cmd" : "Ctrl");
        }
        if (modifiers.HasFlag(KeyModifiers.Meta) && !isMac)
        {
            parts.Add("Win");
        }
        else if (modifiers.HasFlag(KeyModifiers.Meta) && isMac && !parts.Contains("Cmd"))
        {
            parts.Add("Cmd");
        }
        if (modifiers.HasFlag(KeyModifiers.Alt))
        {
            parts.Add("Alt");
        }
        if (modifiers.HasFlag(KeyModifiers.Shift))
        {
            parts.Add("Shift");
        }

        string keyName = key switch
        {
            Key.OemPlus => "+",
            Key.OemMinus => "-",
            Key.D0 => "0",
            Key.D1 => "1",
            Key.D2 => "2",
            Key.D3 => "3",
            Key.D4 => "4",
            Key.D5 => "5",
            Key.D6 => "6",
            Key.D7 => "7",
            Key.D8 => "8",
            Key.D9 => "9",
            Key.OemComma => ",",
            Key.OemPeriod => ".",
            Key.OemOpenBrackets => "[",
            Key.OemCloseBrackets => "]",
            _ => key.ToString()
        };

        parts.Add(keyName);
        return string.Join("+", parts);
    }
}
