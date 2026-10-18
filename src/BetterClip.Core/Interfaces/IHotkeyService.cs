using System;

namespace BetterClip.Core.Interfaces
{
    public interface IHotkeyService : IDisposable
    {
        event EventHandler HotkeyPressed;
        bool RegisterHotKey(string shortcutText);
        void UnregisterHotKey();
    }
}