using System.Windows.Input;

namespace CodeRaider.Models
{
    public class Hotkey
    {
        public Key Key { get; }
        public ModifierKeys Modifiers { get; }

        public Action? Action { get; set; }

        internal bool IsTriggered { get; set; }

        public Hotkey(Key key, ModifierKeys modifiers = ModifierKeys.None)
        {
            Key = key;
            Modifiers = modifiers;
        }

        internal bool IsPressed(HashSet<Key> keysDown)
        {
            // Main key must be pressed
            if (!keysDown.Contains(Key))
                return false;

            // Check each modifier group — only one side needs to be pressed
            if (Modifiers.HasFlag(ModifierKeys.Control) &&
                !HasEither(keysDown, Key.LeftCtrl, Key.RightCtrl))
                return false;

            if (Modifiers.HasFlag(ModifierKeys.Shift) &&
                !HasEither(keysDown, Key.LeftShift, Key.RightShift))
                return false;

            if (Modifiers.HasFlag(ModifierKeys.Alt) &&
                !HasEither(keysDown, Key.LeftAlt, Key.RightAlt))
                return false;

            if (Modifiers.HasFlag(ModifierKeys.Windows) &&
                !HasEither(keysDown, Key.LWin, Key.RWin))
                return false;

            return true;
        }

        private static bool HasEither(HashSet<Key> keysDown, Key left, Key right)
        {
            return keysDown.Contains(left) || keysDown.Contains(right);
        }

        internal bool ContainsKey(Key key)
        {
            if (key == Key) return true;

            if (Modifiers.HasFlag(ModifierKeys.Control) && (key == Key.LeftCtrl || key == Key.RightCtrl)) return true;
            if (Modifiers.HasFlag(ModifierKeys.Shift) && (key == Key.LeftShift || key == Key.RightShift)) return true;
            if (Modifiers.HasFlag(ModifierKeys.Alt) && (key == Key.LeftAlt || key == Key.RightAlt)) return true;
            if (Modifiers.HasFlag(ModifierKeys.Windows) && (key == Key.LWin || key == Key.RWin)) return true;

            return false;
        }
    }
}