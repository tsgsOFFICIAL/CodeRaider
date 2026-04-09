using WindowsInput.Native;
using WindowsInput;

namespace CodeRaider.Models
{
    public class KeyboardService
    {
        private readonly IKeyboardSimulator _keyboard;

        public KeyboardService()
        {
            // Create the simulator once
            InputSimulator inputSimulator = new InputSimulator();
            _keyboard = inputSimulator.Keyboard;
        }

        /// <summary>
        /// Types the given text into the currently focused window.
        /// </summary>
        /// <param name="text">The text to type</param>
        /// <param name="delayMs">Delay between each character (recommended 30-60ms for reliability)</param>
        public void Write(string text, int delayMs = 15)
        {
            if (string.IsNullOrEmpty(text))
                return;

            foreach (char c in text)
            {
                _keyboard.KeyPress((VirtualKeyCode)char.ToUpper(c));
                Thread.Sleep(delayMs);
            }
        }
    }
}