using System.Runtime.CompilerServices;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;
using CodeRaider.Managers;
using CodeRaider.Models;
using System.IO.Pipes;
using System.Windows;
using System.IO;

namespace CodeRaider
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private KeyboardService _keyboard = new();

        // Raider settings
        private int _raiders = 1;
        private int _myPosition = 0;

        public int Raiders
        {
            get => _raiders;
            set
            {
                _raiders = Math.Max(1, value);
                _myIndex = 0;
                OnPropertyChanged();
                RefreshAllProperties();
                UpdatePositionComboBox();
                UpdateProgressString();
            }
        }

        public int MyPosition
        {
            get => _myPosition;
            set
            {
                _myPosition = Math.Clamp(value, 0, _raiders - 1);
                _myIndex = 0;
                OnPropertyChanged();
                RefreshAllProperties();
                UpdateProgressString();
            }
        }

        // Current logical index in the full list
        private int _myIndex = 0;

        // Binding Properties (now respect raiders + position)
        public string Prev3 => GetPersonalCodeAt(_myIndex - 3);
        public string Prev2 => GetPersonalCodeAt(_myIndex - 2);
        public string Prev1 => GetPersonalCodeAt(_myIndex - 1);
        public string Current => GetPersonalCodeAt(_myIndex);
        public string Next1 => GetPersonalCodeAt(_myIndex + 1);
        public string Next2 => GetPersonalCodeAt(_myIndex + 2);
        public string Next3 => GetPersonalCodeAt(_myIndex + 3);

        private string _hotkeyString = "None";
        public string HotkeyString
        {
            get => _hotkeyString;
            set
            {
                _hotkeyString = value;
                OnPropertyChanged();
            }
        }

        private string? _progressString = "0 / 10.000 (0%)";

        public string? ProgressString
        {
            get => _progressString;
            set
            {
                _progressString = value;
                OnPropertyChanged();
            }
        }


        private bool _isReadingHotkey = false;
        private Hotkey? oldHotkey = null;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            Loaded += OnWindowLoaded;

            KeyDown += OnKeyDown;
        }

        private void UpdatePositionComboBox()
        {
            if (!IsLoaded)
                return;

            cmbPosition.Items.Clear();
            for (int i = 0; i < _raiders; i++)
            {
                cmbPosition.Items.Add(new ComboBoxItem { Content = (i + 1).ToString() });
            }
            cmbPosition.SelectedIndex = Math.Min(_myPosition, _raiders - 1);
        }

        private string GetPersonalCodeAt(int personalIndex)
        {
            if (personalIndex < 0) return "----";

            int globalIndex = _myPosition + (personalIndex * _raiders);

            if (globalIndex < 0 || globalIndex >= Helper.Codes.Length)
                return "----";

            return Helper.Codes[globalIndex].ToString("D4");
        }

        private void RefreshAllProperties()
        {
            // This tells WPF that every property might have changed
            OnPropertyChanged(string.Empty);
        }

        private void PerformNewAttempt()
        {
            WriteCode();

            // Check if we have more codes for this raider
            int nextGlobal = _myPosition + ((_myIndex + 1) * _raiders);
            if (nextGlobal >= Helper.Codes.Length)
                return;

            _myIndex++;

            // Update progress
            UpdateProgressString();

            RefreshAllProperties();
            PlayScrollAnimation(100);   // slide in from right
        }

        private void WriteCode()
        {
            _keyboard.Write(Current, 100);
        }

        private void UpdateProgressString()
        {
            if (Helper.Codes == null || Helper.Codes.Length == 0)
            {
                ProgressString = "0 / 0 (0%)";
                return;
            }

            int totalCodes = Helper.Codes.Length;
            int currentGlobalIndex = _myPosition + (_myIndex * _raiders);

            // How many attempts this raider has already done (including current)
            int attemptsDone = _myIndex + 1;

            // Total attempts this raider will need to do
            int totalAttemptsForRaider = (int)Math.Ceiling((double)totalCodes / _raiders);

            double percentage = totalAttemptsForRaider > 0
                ? (attemptsDone * 100.0) / totalAttemptsForRaider
                : 0;

            ProgressString = $"{attemptsDone} / {totalAttemptsForRaider:0,0} ({percentage:0}%)";
        }

        private void UndoAttempt()
        {
            if (_myIndex <= 0)
                return;

            _myIndex--;

            // Update progress
            UpdateProgressString();

            RefreshAllProperties();
            PlayScrollAnimation(-100);  // slide in from left
        }

        private void PlayScrollAnimation(double startOffset)
        {
            DoubleAnimation slideAnim = new DoubleAnimation
            {
                From = startOffset,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
            };

            if (CodeStack.RenderTransform is TranslateTransform trans)
            {
                trans.BeginAnimation(TranslateTransform.XProperty, null);
                trans.BeginAnimation(TranslateTransform.XProperty, slideAnim);
            }
        }

        /// <summary>
        /// Brings the window to the foreground, restoring it if minimized and ensuring it is visible and active.
        /// </summary>
        /// <remarks>This method restores the window from a minimized state if necessary, makes it
        /// visible, and activates it. It also adjusts the window's Z-order to ensure it appears above other windows,
        /// addressing platform-specific behavior on Windows.</remarks>
        private void BringToFront()
        {
            // Always ensure visible and focused
            if (!IsVisible)
                Show();

            WindowState = WindowState.Normal; // In case it was maximized/minimized

            Activate();
            Topmost = true;
            Topmost = false; // Focus steal fix
        }
        /// <summary>
        /// Starts an asynchronous server that listens for activation messages on a named pipe and brings the
        /// application window to the foreground when an activation request is received.
        /// </summary>
        /// <remarks>This method runs the activation server in a background task and does not block the
        /// calling thread. The server continuously waits for incoming connections and responds to activation messages.
        /// This is typically used to allow external processes to activate the application window. The method should be
        /// called once during application startup to enable activation functionality.</remarks>
        private void StartActivationServer()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    using NamedPipeServerStream server = new NamedPipeServerStream(
                        App.PipeName,
                        PipeDirection.In,
                        1,
                        PipeTransmissionMode.Message,
                        PipeOptions.Asynchronous);

                    await server.WaitForConnectionAsync();

                    using StreamReader reader = new StreamReader(server);
                    string? message = await reader.ReadLineAsync();

                    if (message == "ACTIVATE")
                    {
                        Dispatcher.Invoke(BringToFront);
                    }
                }
            });
        }

        private string GetHotkeyDisplayString(Hotkey? hotkey)
        {
            if (hotkey == null)
                return "None";

            string mods = "";
            if (hotkey.Modifiers.HasFlag(ModifierKeys.Control)) mods += "Ctrl+";
            if (hotkey.Modifiers.HasFlag(ModifierKeys.Shift)) mods += "Shift+";
            if (hotkey.Modifiers.HasFlag(ModifierKeys.Alt)) mods += "Alt+";
            if (hotkey.Modifiers.HasFlag(ModifierKeys.Windows)) mods += "Win+";

            return mods + hotkey.Key;
        }

        private static bool IsModifierKey(Key key)
        {
            return key is Key.LeftCtrl or Key.RightCtrl or
                   Key.LeftAlt or Key.RightAlt or
                   Key.LeftShift or Key.RightShift or
                   Key.LWin or Key.RWin or
                   Key.System; // AltGr often appears as System
        }

        #region Event Handlers
        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            StartActivationServer();

            // Populate Raiders (1 to 20)
            cmbRaiders.Items.Clear();

            for (int i = 1; i <= 20; i++)
                cmbRaiders.Items.Add(new ComboBoxItem
                {
                    Content = i.ToString()
                });

            cmbRaiders.SelectedIndex = 0; // start with 1 raider

            UpdatePositionComboBox();

            cmbRaiders.SelectedIndex = 0; // Default to 1 raider
            UpdatePositionComboBox();

            App.Settings.NextHotkeyPressed += PerformNewAttempt;
            App.Settings.NextHotkeyChanged += OnNextHotkeyChanged;

            // Initial display
            HotkeyString = GetHotkeyDisplayString(App.Settings.NextHotkey);

            // Ensure the UI shows the first set of codes on launch
            RefreshAllProperties();
        }

        private void OnNextHotkeyChanged()
        {
            HotkeyString = GetHotkeyDisplayString(App.Settings.NextHotkey);
        }

        private void OnRaidersChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbRaiders.SelectedItem is ComboBoxItem item && int.TryParse(item.Content.ToString(), out int r))
            {
                Raiders = r;
            }
        }

        private void OnPositionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbPosition.SelectedIndex >= 0)
                MyPosition = cmbPosition.SelectedIndex;
        }

        private async void OnChangeHotkeyButtonClicked(object sender, RoutedEventArgs e)
        {
            _isReadingHotkey = true;

            // Save old hotkey in case user cancels
            oldHotkey = App.Settings.NextHotkey;

            // Set hotkey to null to avoid conflicts while dialog is open
            App.Settings.SetNextHotkey(Key.None, ModifierKeys.None);

            HotkeyString = "[Press key]";
        }

        private async void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!_isReadingHotkey)
                return;

            // Ignore modifier keys by themselves (Ctrl, Alt, Shift, etc.)
            if (IsModifierKey(e.Key))
                return;

            // Escape cancels hotkey change
            if (e.Key == Key.Escape)
            {
                App.Settings.SetNextHotkey(oldHotkey?.Key ?? Key.None, oldHotkey?.Modifiers ?? ModifierKeys.None);
                _isReadingHotkey = false;
                return;
            }

            _isReadingHotkey = false;

            ModifierKeys modifiers = Keyboard.Modifiers;

            App.Settings.SetNextHotkey(e.Key, modifiers);
            await SettingsService.SaveAsync(App.Settings.NextHotkey);
        }

        private void OnUndoButtonClicked(object sender, RoutedEventArgs e)
        {
            UndoAttempt();
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion
    }
}