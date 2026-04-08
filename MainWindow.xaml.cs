using System.Runtime.CompilerServices;
using System.Windows.Media.Animation;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;

namespace CodeRaider
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        // Current index in the Helper.Codes array
        private int _currentIndex = 0;

        // Binding Properties
        public string Prev3 => GetCodeAt(_currentIndex - 3);
        public string Prev2 => GetCodeAt(_currentIndex - 2);
        public string Prev1 => GetCodeAt(_currentIndex - 1);
        public string Current => GetCodeAt(_currentIndex);
        public string Next1 => GetCodeAt(_currentIndex + 1);
        public string Next2 => GetCodeAt(_currentIndex + 2);
        public string Next3 => GetCodeAt(_currentIndex + 3);

        private string _hotkey = "F8";
        public string Hotkey
        {
            get => _hotkey;
            set
            {
                _hotkey = value;
                OnPropertyChanged();
            }
        }

        private bool isRecordingHotkey = false;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            // Ensure the UI shows the first set of codes on launch
            RefreshAllProperties();

            KeyDown += MainWindow_KeyDown;
        }

        /// <summary>
        /// Safely fetches a code from the helper array or returns placeholders
        /// </summary>
        private static string GetCodeAt(int index)
        {
            if (index < 0 || index >= Helper.Codes.Length)
                return "----";

            return Helper.Codes[index].ToString("D4");
        }

        private void RefreshAllProperties()
        {
            // This tells WPF that every property might have changed
            OnPropertyChanged(string.Empty);
        }

        private void PerformNewAttempt()
        {
            if (_currentIndex >= Helper.Codes.Length - 1)
                return;

            _currentIndex++;
            RefreshAllProperties();

            // Play animation from the RIGHT (100) to 0
            PlayScrollAnimation(100);
        }

        private void UndoAttempt()
        {
            if (_currentIndex <= 0)
                return;

            _currentIndex--;
            RefreshAllProperties();

            // Play animation from the LEFT (-100) to 0
            PlayScrollAnimation(-100);
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
        #region Event Handlers
        private void OnChangeHotkeyButtonClicked(object sender, RoutedEventArgs e)
        {
            isRecordingHotkey = true;
            Hotkey = "[Press key]";
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (isRecordingHotkey)
            {
                Hotkey = e.Key.ToString().Replace("D", "").Replace("NumPad", "");
                isRecordingHotkey = false;
                return;
            }

            // Logic to check if the pressed key matches the hotkey string
            string pressed = e.Key.ToString();
            if (pressed == Hotkey || pressed == $"D{Hotkey}" || pressed == $"NumPad{Hotkey}")
            {
                PerformNewAttempt();
            }
        }

        private void OnUndoButtonClicked(object sender, RoutedEventArgs e)
        {
            UndoAttempt();
        }

        #endregion

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}