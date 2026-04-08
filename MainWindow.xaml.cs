using System.Runtime.CompilerServices;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;

namespace CodeRaider
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        // Raider settings
        private int _raiders = 1;
        private int _myPosition = 0;

        public int Raiders
        {
            get => _raiders;
            set
            {
                _raiders = Math.Max(1, value);
                OnPropertyChanged();
                RefreshAllProperties();
                UpdatePositionComboBox();
            }
        }

        public int MyPosition
        {
            get => _myPosition;
            set
            {
                _myPosition = Math.Clamp(value, 0, _raiders - 1);
                OnPropertyChanged();
                RefreshAllProperties();
            }
        }

        // Current logical index in the full list (your personal progress)
        private int _myIndex = 0;

        // Binding Properties (now respect raiders + position)
        public string Prev3 => GetPersonalCodeAt(_myIndex - 3);
        public string Prev2 => GetPersonalCodeAt(_myIndex - 2);
        public string Prev1 => GetPersonalCodeAt(_myIndex - 1);
        public string Current => GetPersonalCodeAt(_myIndex);
        public string Next1 => GetPersonalCodeAt(_myIndex + 1);
        public string Next2 => GetPersonalCodeAt(_myIndex + 2);
        public string Next3 => GetPersonalCodeAt(_myIndex + 3);

        private string _hotkey = "E";
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

            Loaded += OnWindowLoaded;

            KeyDown += MainWindow_KeyDown;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
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

            // Ensure the UI shows the first set of codes on launch
            RefreshAllProperties();
        }

        private void UpdatePositionComboBox()
        {
            if (!IsLoaded)
                return;

            cmbPosition.Items.Clear();
            for (int i = 0; i < _raiders; i++)
            {
                cmbPosition.Items.Add(new ComboBoxItem { Content = i.ToString() });
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
            // Check if we have more codes for this raider
            int nextGlobal = _myPosition + ((_myIndex + 1) * _raiders);
            if (nextGlobal >= Helper.Codes.Length)
                return;

            _myIndex++;
            RefreshAllProperties();
            PlayScrollAnimation(100);   // slide in from right
        }

        private void UndoAttempt()
        {
            if (_myIndex <= 0)
                return;

            _myIndex--;
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
        #region Event Handlers
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
            {
                MyPosition = cmbPosition.SelectedIndex;
            }
        }

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