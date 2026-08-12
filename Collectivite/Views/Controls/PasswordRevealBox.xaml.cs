using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;

namespace Collectivite.Views.Controls
{
    public partial class PasswordRevealBox : UserControl
    {
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register(
                nameof(Password),
                typeof(string),
                typeof(PasswordRevealBox),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPasswordPropertyChanged));

        public static readonly DependencyProperty HintProperty =
            DependencyProperty.Register(
                nameof(Hint),
                typeof(string),
                typeof(PasswordRevealBox),
                new PropertyMetadata("Mot de passe"));

        private bool _isSyncing;

        public string Password
        {
            get => (string)GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }

        public string Hint
        {
            get => (string)GetValue(HintProperty);
            set => SetValue(HintProperty, value);
        }

        public PasswordRevealBox()
        {
            InitializeComponent();
        }

        private static void OnPasswordPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PasswordRevealBox)d).SyncControlsFromProperty();
        }

        private void SyncControlsFromProperty()
        {
            if (_isSyncing) return;
            _isSyncing = true;
            var value = Password ?? string.Empty;
            if (PwdBox.Password != value) PwdBox.Password = value;
            if (TxtBox.Text != value) TxtBox.Text = value;
            _isSyncing = false;
        }

        private void PwdBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncing) return;
            _isSyncing = true;
            Password = PwdBox.Password;
            TxtBox.Text = PwdBox.Password;
            _isSyncing = false;
        }

        private void TxtBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncing) return;
            _isSyncing = true;
            Password = TxtBox.Text;
            PwdBox.Password = TxtBox.Text;
            _isSyncing = false;
        }

        private void ToggleVisibilityButton_Click(object sender, RoutedEventArgs e)
        {
            bool showingText = TxtBox.Visibility == Visibility.Visible;
            if (showingText)
            {
                TxtBox.Visibility = Visibility.Collapsed;
                PwdBox.Visibility = Visibility.Visible;
                ToggleIcon.Kind = PackIconKind.Eye;
                PwdBox.Focus();
            }
            else
            {
                PwdBox.Visibility = Visibility.Collapsed;
                TxtBox.Visibility = Visibility.Visible;
                ToggleIcon.Kind = PackIconKind.EyeOff;
                TxtBox.Focus();
                TxtBox.CaretIndex = TxtBox.Text.Length;
            }
        }
    }
}
