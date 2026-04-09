using System.Windows.Input;
using CodeRaider.Models;
using System.Text.Json;
using System.IO;

namespace CodeRaider.Managers
{
    public static class SettingsService
    {
        private static readonly string Path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NextHotkey.crc");

        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true
        };

        public static async Task<HotkeyDto> LoadAsync()
        {
            if (!File.Exists(Path))
                return new HotkeyDto(); // default (no hotkey)

            try
            {
                string json = await File.ReadAllTextAsync(Path);
                return JsonSerializer.Deserialize<HotkeyDto>(json, Options) ?? new HotkeyDto();
            }
            catch
            {
                return new HotkeyDto(); // default
            }
        }

        public static async Task SaveAsync(Hotkey? hotkey)
        {
            HotkeyDto dto = new HotkeyDto
            {
                Key = (int)(hotkey?.Key ?? Key.None),
                Modifiers = (int)(hotkey?.Modifiers ?? ModifierKeys.None)
            };

            string json = JsonSerializer.Serialize(dto, Options);
            await File.WriteAllTextAsync(Path, json);
        }
    }
}