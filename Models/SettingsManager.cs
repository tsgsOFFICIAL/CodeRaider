using System.Windows.Input;
using CodeRaider.Managers;

namespace CodeRaider.Models
{
    public class SettingsManager : IDisposable
    {
        private readonly HotkeyHandlerService _hotkeyManager = new HotkeyHandlerService();

        public Hotkey? NextHotkey { get; private set; }

        public event Action? NextHotkeyPressed;
        public event Action? NextHotkeyChanged;

        public async Task LoadAndApplyAsync()
        {
            HotkeyDto dto = await SettingsService.LoadAsync();

            NextHotkey = dto.Key != (int)Key.None
                ? new Hotkey((Key)dto.Key, (ModifierKeys)dto.Modifiers)
                : null;

            RegisterHotkey();
            NextHotkeyChanged?.Invoke();
        }

        private void RegisterHotkey()
        {
            if (NextHotkey == null)
                return;

            NextHotkey.Action = () => NextHotkeyPressed?.Invoke();
            _hotkeyManager.RegisterHotkey(NextHotkey);
        }

        public void UnregisterHotkey()
        {
            if (NextHotkey != null)
                _hotkeyManager.UnregisterHotkey(NextHotkey);
        }

        public void SetNextHotkey(Key key, ModifierKeys modifiers)
        {
            UnregisterHotkey();

            NextHotkey = new Hotkey(key, modifiers);
            RegisterHotkey();

            NextHotkeyChanged?.Invoke();
        }

        public void Shutdown()
        {
            UnregisterHotkey();
            _hotkeyManager.Dispose();
        }

        public void Dispose() => Shutdown();
    }

    // DTOs
    public class HotkeyDto
    {
        public int Key { get; set; }
        public int Modifiers { get; set; }
    }
}