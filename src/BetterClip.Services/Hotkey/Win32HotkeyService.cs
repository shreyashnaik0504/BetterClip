using System;
using BetterClip.Core.Interfaces;

namespace BetterClip.Services.Hotkey
{
    public class Win32HotkeyService : IHotkeyService
    {
        public event EventHandler? HotkeyPressed;

        public bool RegisterHotKey(string shortcutText)
        {
            // Placeholder for Win32 RegisterHotKey API integration (Ctrl + Space)
            return true;
        }

        public void UnregisterHotKey()
        {
            // Clean up hotkey binding
        }

        public void Dispose()
        {
            UnregisterHotKey();
        }
    }
}